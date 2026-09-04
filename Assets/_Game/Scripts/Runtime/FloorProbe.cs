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
    /// Height comes from the tester, not from plane detection. Plane detection was removed in
    /// phase 2 because it drew squares over the very floor whose edges the test has to read, and
    /// bringing it back to answer "where is the floor" would mean measuring ARCore's answer with
    /// ARCore. A person who knows how tall they are is the independent reference.
    ///
    /// <see cref="FloorSpreadMeters"/> is the honest summary: on a flat floor every disc should
    /// land at the same height, so the spread between them is the vertical error accumulated
    /// between drops — mixed, unavoidably, with how consistently the phone was held.
    /// </summary>
    public class FloorProbe : MonoBehaviour
    {
        [SerializeField] Transform m_Camera;

        [Tooltip("Optional. Anchors each disc so it follows ARCore's corrections instead of "
            + "sliding with the drift being measured.")]
        [SerializeField] ARAnchorManager m_AnchorManager;

        [Tooltip("Material for the discs. Unlit and obvious; this is not meant to look real.")]
        [SerializeField] Material m_Material;

        [Tooltip("Distance from the camera down to the floor when the phone is held at eye "
            + "level. Measure it rather than guessing — it is the reference the test rests on.")]
        [SerializeField] float m_EyeHeightMeters = 1.70f;

        [SerializeField] float m_DiscDiameterMeters = 0.30f;

        readonly List<Transform> m_Probes = new();

        float m_MinY = float.MaxValue;
        float m_MaxY = float.MinValue;

        /// <summary>Discs dropped since the app started.</summary>
        public int Count => m_Probes.Count;

        /// <summary>
        /// Difference in height between the highest and lowest disc, in metres. On one flat floor
        /// this should be zero and is not.
        /// </summary>
        public float FloorSpreadMeters => m_Probes.Count < 2 ? 0f : m_MaxY - m_MinY;

        public float EyeHeightMeters => m_EyeHeightMeters;

        /// <summary>Raised with the disc's number and where it was put, for the log.</summary>
        public event Action<int, Vector3> Dropped;

        /// <summary>
        /// Puts a disc on the floor below the camera. Wired to the drop button.
        /// </summary>
        public void Drop()
        {
            if (m_Camera == null)
                return;

            var position = m_Camera.position + Vector3.down * m_EyeHeightMeters;

            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = $"FloorProbe {m_Probes.Count + 1}";

            // A collider would let the disc be hit by anything else that ever raycasts, and this
            // object exists only to be looked at.
            Destroy(disc.GetComponent<Collider>());

            // Unity's cylinder is two units tall, so the vertical scale is half the thickness.
            var radius = m_DiscDiameterMeters * 0.5f;
            disc.transform.localScale = new Vector3(radius * 2f, 0.005f, radius * 2f);
            disc.transform.position = position;

            if (m_Material != null)
                disc.GetComponent<Renderer>().sharedMaterial = m_Material;

            m_Probes.Add(disc.transform);
            m_MinY = Mathf.Min(m_MinY, position.y);
            m_MaxY = Mathf.Max(m_MaxY, position.y);

            AnchorTo(disc.transform, position);

            Dropped?.Invoke(m_Probes.Count, position);
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
