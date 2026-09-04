using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FriLens
{
    /// <summary>
    /// Drops a disc on the floor directly below the camera, and leaves it there.
    ///
    /// This is a drift test that needs no printed marker, which is why it exists: until the
    /// markers are surveyed the app can measure the tracker but not the overlay, and the whole
    /// question is what the overlay does. A disc dropped on the floor is an overlay of one
    /// object whose correct position everybody in the room can see.
    ///
    /// Two things to read from it. Immediately: does the disc sit on the floor, or does it float
    /// or sink? That is the vertical estimate. Afterwards: walk away, come back, and look at
    /// whether the disc is still where you put it. That is drift, in the only units that matter —
    /// centimetres off a spot on a real floor.
    ///
    /// Where the floor is comes from one of two places, and which one it was is recorded with
    /// every drop because it changes what the disc means.
    ///
    /// **On the nav mesh**, once the overlay has been aligned to the building. The disc then sits
    /// where the *model* says the floor is, so looking at whether it rests on the real floor is
    /// the model-against-reality comparison this whole project exists to make, done one square
    /// foot at a time. This is what the test at the faculty will use.
    ///
    /// **Below the camera by a measured height**, when there is no aligned mesh under the phone —
    /// which is every run until the markers are surveyed. It measures the tracker rather than the
    /// model, and it is how the drift tests get run in the meantime.
    ///
    /// Plane detection is deliberately not a third option. It was removed in phase 2 because it
    /// drew squares over the very floor whose edges the test has to read, and bringing it back to
    /// answer "where is the floor" would mean checking ARCore's answer against ARCore's answer.
    ///
    /// In the height mode the floor is learned once and then held. The first field run showed why: the
    /// phone gets tilted down to look at the floor, and it gets *lowered* at the same time — the
    /// log has the camera 42 cm below where it started. Subtracting a fixed eye height from that
    /// put every disc well below the real floor, and a point below the floor projects lower in
    /// the image than the floor does, so from a distance the disc appeared closer than it was.
    /// The error is proportional: at 8 m a disc 42 cm too low reads about 2 m short. Underfoot
    /// it looks perfect, which is exactly why it went unnoticed.
    ///
    /// So only the first drop reads the camera's height. After that the floor is a known
    /// horizontal plane and every disc goes on it, however the phone is being held.
    ///
    /// <see cref="FloorOffsetMeters"/> is what that costs in information: the gap between the
    /// locked floor and the one the camera implies right now. It is vertical drift mixed with
    /// how the phone is held, and it is only worth reading while standing upright.
    /// </summary>
    public class FloorProbe : MonoBehaviour
    {
        [SerializeField] Transform m_Camera;

        [Tooltip("Optional. Anchors each disc so it follows ARCore's corrections instead of "
            + "sliding with the drift being measured.")]
        [SerializeField] ARAnchorManager m_AnchorManager;

        [Tooltip("Material for the discs. Unlit and obvious; this is not meant to look real.")]
        [SerializeField] Material m_Material;

        [Tooltip("Collider on the navigation overlay. When a drop lands on it the disc goes "
            + "where the model says the floor is, which is the comparison the test is for. "
            + "Empty, or not aligned yet, falls back to the measured height below.")]
        [SerializeField] Collider m_NavCollider;

        [Tooltip("Distance from the camera down to the floor at the moment of a drop. Measured, "
            + "not guessed: 1.25 m is a phone held to look at the floor, not one at eye level.")]
        [SerializeField] float m_EyeHeightMeters = 1.25f;

        [SerializeField] float m_DiscDiameterMeters = 0.30f;

        readonly List<Transform> m_Probes = new();

        float m_FloorY;
        bool m_FloorKnown;
        float m_WorstOffset;

        /// <summary>Discs dropped since the app started.</summary>
        public int Count => m_Probes.Count;

        /// <summary>Whether the floor's height has been fixed by a first drop.</summary>
        public bool FloorKnown => m_FloorKnown;

        /// <summary>
        /// How far the floor implied by the camera right now sits from the locked floor, in
        /// metres. Zero would mean the phone is exactly one eye height above the plane the discs
        /// are on; it never is, because nobody holds a phone that steadily.
        /// </summary>
        public float FloorOffsetMeters =>
            m_FloorKnown && m_Camera != null
                ? m_Camera.position.y - m_EyeHeightMeters - m_FloorY
                : 0f;

        /// <summary>Largest offset seen at the moment of a drop, in metres.</summary>
        public float WorstOffsetMeters => m_WorstOffset;

        public float EyeHeightMeters => m_EyeHeightMeters;

        /// <summary>Where the last disc's height came from.</summary>
        public enum FloorSource { Height, NavMesh }

        /// <summary>How the most recent disc was placed.</summary>
        public FloorSource LastSource { get; private set; } = FloorSource.Height;

        /// <summary>
        /// For a disc placed on the nav mesh: how far the model's floor sits below the floor the
        /// measured height implies, in metres. Positive means the model's floor is lower.
        ///
        /// This is the model-against-reality comparison reduced to one number, and it is the
        /// reason the nav mesh mode is worth having. The measured height is an independent
        /// reference — a tape measure, not ARCore — so the gap is the model's vertical error at
        /// that spot, plus however far off the height was and however much the tracker has
        /// drifted vertically since the session started. The last of those is not small: one run
        /// logged relocalisation jumps carrying more than two metres of vertical correction.
        /// </summary>
        public float LastNavGapMeters { get; private set; }

        /// <summary>Raised with the disc's number and where it was put, for the log.</summary>
        public event Action<int, Vector3> Dropped;

        void Awake()
        {
            if (m_Camera == null)
                Debug.LogError($"{nameof(FloorProbe)}: no camera assigned; the drop button will "
                    + "do nothing. Run FriLens > Wire Scene.", this);

            // CreatePrimitive hands out the built-in render pipeline's default material, which
            // under URP draws magenta. Silent in the editor's own scene view, obvious and
            // baffling on the phone.
            if (m_Material == null)
                Debug.LogWarning($"{nameof(FloorProbe)}: no material assigned; the discs will "
                    + "render magenta under URP. Run FriLens > Wire Scene.", this);
        }

        /// <summary>
        /// Puts a disc on the floor below the camera. Wired to the drop button.
        /// </summary>
        public void Drop()
        {
            if (m_Camera == null)
                return;

            var position = FindFloor();

            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = $"FloorProbe {m_Probes.Count + 1}";

            // A collider would let the disc be hit by anything else that ever raycasts, and this
            // object exists only to be looked at.
            Destroy(disc.GetComponent<Collider>());

            // Unity's cylinder is one unit across and two units tall, so the horizontal scale is
            // the diameter and the vertical one is half the thickness.
            disc.transform.localScale = new Vector3(m_DiscDiameterMeters, 0.005f, m_DiscDiameterMeters);
            disc.transform.position = position;

            if (m_Material != null)
                disc.GetComponent<Renderer>().sharedMaterial = m_Material;

            m_Probes.Add(disc.transform);

            AnchorTo(disc.transform, position);

            Dropped?.Invoke(m_Probes.Count, position);
        }

        /// <summary>
        /// Decides where the disc goes: on the nav mesh if there is one under the phone, on the
        /// remembered height otherwise.
        /// </summary>
        Vector3 FindFloor()
        {
            var camera = m_Camera.position;

            // Straight down from the camera. Aiming along the phone's forward axis would put the
            // disc wherever it happened to be pointing, and "directly below me" is the one place
            // a person can check by looking at their own feet.
            if (m_NavCollider != null && m_NavCollider.enabled
                && m_NavCollider.Raycast(new Ray(camera, Vector3.down), out var hit, 20f))
            {
                LastSource = FloorSource.NavMesh;
                LastNavGapMeters = camera.y - m_EyeHeightMeters - hit.point.y;
                return hit.point;
            }

            LastSource = FloorSource.Height;

            var implied = camera.y - m_EyeHeightMeters;

            // The first drop decides where the floor is. Every one after it uses that height, so
            // a disc lands on the same plane even when the phone was held differently.
            if (!m_FloorKnown)
            {
                m_FloorY = implied;
                m_FloorKnown = true;
            }
            else
            {
                m_WorstOffset = Mathf.Max(m_WorstOffset, Mathf.Abs(implied - m_FloorY));
            }

            return new Vector3(camera.x, m_FloorY, camera.z);
        }

        /// <summary>
        /// Changes the assumed height of the camera above the floor and forgets the locked floor,
        /// because the floor was derived from the old value.
        ///
        /// Adjustable at runtime rather than in the inspector because the error it corrects is
        /// invisible at the moment of the drop and only shows up metres away — which means it can
        /// only be tuned in the field, by walking away and watching whether the disc stays put.
        /// </summary>
        public void AdjustEyeHeight(float deltaMeters)
        {
            m_EyeHeightMeters = Mathf.Clamp(m_EyeHeightMeters + deltaMeters, 0.6f, 2.4f);
            m_FloorKnown = false;
            m_WorstOffset = 0f;
        }

        /// <summary>
        /// Hangs the disc off an anchor so ARCore keeps it on the same physical spot.
        ///
        /// Without this the disc would drift along with the pose and always look correct, which
        /// would make the test say nothing. The anchor is what turns it into a fixed point the
        /// drift can be seen against.
        /// </summary>
        async void AnchorTo(Transform disc, Vector3 position)
        {
            if (m_AnchorManager == null || !m_AnchorManager.enabled)
                return;

            try
            {
                var result = await m_AnchorManager.TryAddAnchorAsync(
                    new Pose(position, Quaternion.identity));

                if (!result.status.IsSuccess() || disc == null)
                    return;

                disc.SetParent(result.value.transform, true);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{nameof(FloorProbe)}: could not anchor a disc. "
                    + exception.Message, this);
            }
        }
    }
}
