using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FriLens
{
    /// <summary>
    /// Drops the overlay around the camera when there is no marker to align to, so there is
    /// something on screen before the printed marker exists.
    ///
    /// This is not an alignment and it measures nothing. The model is 80 m long and sits tens of
    /// metres from the world origin, while an AR session always starts its world at the camera —
    /// so with no marker the overlay was 19 m to one side and 5 m overhead, past a 20 m far
    /// plane. Nobody ever saw it. That made a whole class of problems invisible: the first field
    /// reports said the overlay button did nothing, when what it was hiding had never been drawn.
    ///
    /// Placement puts the model's floor under the camera and its footprint around it, which is
    /// enough to check that the overlay renders, that it can be hidden, and to watch it slide
    /// away while walking. Where it slides *to* means nothing until a surveyed marker exists.
    /// </summary>
    public class ProvisionalPlacement : MonoBehaviour
    {
        [SerializeField] Transform m_AlignmentRoot;
        [SerializeField] Renderer m_Overlay;
        [SerializeField] Transform m_Camera;
        [SerializeField] MarkerAlignment m_Alignment;

        [Tooltip("Height the camera is assumed to be held at. The model's floor is put this far "
            + "below the camera, so the overlay lies on the real floor rather than at eye level.")]
        [SerializeField] float m_EyeHeightMeters = 1.5f;

        [Tooltip("Turn off once a surveyed marker exists. Placement would then be a distraction "
            + "sitting on top of the only thing worth looking at.")]
        [SerializeField] bool m_PlaceWhenUnaligned = true;

        bool m_Placed;

        /// <summary>Whether the overlay is sitting where it was dropped rather than where it was measured.</summary>
        public bool IsProvisional => m_Placed && (m_Alignment == null || m_Alignment.LastAlignmentTime < 0f);

        void Update()
        {
            if (!m_PlaceWhenUnaligned || m_Placed)
                return;

            if (m_AlignmentRoot == null || m_Overlay == null || m_Camera == null)
                return;

            // A real alignment beats a guess, so if the marker has already been found this
            // never runs at all.
            if (m_Alignment != null && m_Alignment.LastAlignmentTime >= 0f)
            {
                m_Placed = true;
                return;
            }

            if (ARSession.state != ARSessionState.SessionTracking)
                return;

            Place();
            m_Placed = true;
        }

        void Place()
        {
            // Bounds are read in world space after any import rotation has been applied, which is
            // the only place the numbers are trustworthy: the mesh was baked out of a Blender
            // file with a 270 degree turn on X and its local bounds still carry that.
            var bounds = m_Overlay.bounds;
            var floorCentre = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            var target = m_Camera.position - new Vector3(0f, m_EyeHeightMeters, 0f);
            m_AlignmentRoot.position += target - floorCentre;

            Debug.Log($"{nameof(ProvisionalPlacement)}: overlay dropped around the camera. "
                + "This is not an alignment and the offsets on screen measure nothing.", this);
        }
    }
}
