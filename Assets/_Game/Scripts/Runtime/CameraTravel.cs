using UnityEngine;

namespace FriLens
{
    /// <summary>
    /// Adds up how far the camera has physically moved since the last alignment.
    ///
    /// This is the x-axis of the whole test. VIO drift grows with distance walked, not with time
    /// and not with distance from the marker, so "the overlay was 30 cm out" only means something
    /// next to "after 47 m of walking". Straight-line distance from the marker is a different
    /// number and both are worth having: a walk down a corridor and back ends up near the marker
    /// again while having accumulated the full drift.
    ///
    /// Frames shorter than <see cref="m_MinimumStepMeters"/> are ignored so that tracker jitter
    /// while standing still does not quietly inflate the total.
    /// </summary>
    public class CameraTravel : MonoBehaviour
    {
        [SerializeField] Transform m_Camera;

        [Tooltip("Movement below this in one frame is treated as jitter, not travel.")]
        [SerializeField] float m_MinimumStepMeters = 0.004f;

        Vector3 m_LastPosition;
        bool m_Started;

        /// <summary>Path length walked since the last reset, in metres.</summary>
        public float DistanceWalked { get; private set; }

        /// <summary>Straight-line distance from where the count was last reset, in metres.</summary>
        public float DistanceFromOrigin { get; private set; }

        /// <summary>Position the count was last reset at.</summary>
        public Vector3 Origin { get; private set; }

        public bool HasOrigin => m_Started;

        /// <summary>
        /// Starts counting again from the camera's current position. Called when an alignment is
        /// applied, so the numbers on screen always refer to the alignment being tested.
        /// </summary>
        public void Reset()
        {
            if (m_Camera == null)
                return;

            m_LastPosition = m_Camera.position;
            Origin = m_LastPosition;
            DistanceWalked = 0f;
            DistanceFromOrigin = 0f;
            m_Started = true;
        }

        void Update()
        {
            if (m_Camera == null || !m_Started)
                return;

            var position = m_Camera.position;
            var step = Vector3.Distance(position, m_LastPosition);

            if (step >= m_MinimumStepMeters)
            {
                DistanceWalked += step;
                m_LastPosition = position;
            }

            DistanceFromOrigin = Vector3.Distance(position, Origin);
        }
    }
}
