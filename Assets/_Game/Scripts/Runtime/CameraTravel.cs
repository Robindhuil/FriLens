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

        [Tooltip("Implied speed above which a step is a tracker relocalisation, not walking. "
            + "A brisk walk is about 2 m/s.")]
        [SerializeField] float m_MaximumStepSpeed = 4f;

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
        /// Steps discarded as too fast to be walking. Each one is ARCore correcting itself, and
        /// on screen it is the moment the overlay visibly jumps. Counting them separates "the
        /// overlay drifted away gradually" from "the tracker relocalised", which are different
        /// findings with different causes.
        /// </summary>
        public int RelocalisationJumps { get; private set; }

        /// <summary>Metres contained in the discarded jumps, for judging how much they mattered.</summary>
        public float JumpedMeters { get; private set; }

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
            RelocalisationJumps = 0;
            JumpedMeters = 0f;
            m_Started = true;
        }

        void Update()
        {
            if (m_Camera == null)
                return;

            // Counting starts on its own rather than waiting for an alignment. Without a marker
            // there is no alignment, so the old behaviour left the distance at zero for the whole
            // run — which made the one measurement that proves tracking reaches the app impossible
            // to take until the marker existed. An alignment still resets it, so the number the
            // test reads is still "since alignment".
            if (!m_Started)
            {
                Reset();
                return;
            }

            var position = m_Camera.position;
            var step = Vector3.Distance(position, m_LastPosition);

            if (step >= m_MinimumStepMeters)
            {
                // A step implying more than a sprint is ARCore repositioning itself, not walking.
                // Adding those to the total inflates the very axis drift is measured against: in
                // one 77 m run three such jumps contributed 5.4 m, seven percent of the distance.
                var speed = Time.deltaTime > 0f ? step / Time.deltaTime : 0f;
                if (speed > m_MaximumStepSpeed)
                {
                    RelocalisationJumps++;
                    JumpedMeters += step;
                }
                else
                {
                    DistanceWalked += step;
                }

                // The anchor moves either way. Holding it back across a relocalisation would turn
                // the jump into a slow fake walk spread over the following frames.
                m_LastPosition = position;
            }

            DistanceFromOrigin = Vector3.Distance(position, Origin);
        }
    }
}
