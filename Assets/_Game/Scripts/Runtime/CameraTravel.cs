using UnityEngine;
using UnityEngine.XR.ARFoundation;

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
    /// </summary>
    public class CameraTravel : MonoBehaviour
    {
        [SerializeField] Transform m_Camera;

        [Tooltip("Movement below this in one frame is treated as jitter, not travel.")]
        [SerializeField] float m_MinimumStepMeters = 0.004f;

        [Tooltip("Implied speed above which a step is a tracker relocalisation, not walking. "
            + "A brisk walk is about 2 m/s.")]
        [SerializeField] float m_MaximumStepSpeed = 4f;

        [Tooltip("Any single step longer than this is a relocalisation whatever the frame time "
            + "says, which catches jumps that arrive on a long frame.")]
        [SerializeField] float m_MaximumStepMeters = 1f;

        Vector3 m_LastPosition;
        bool m_Started;

        /// <summary>Seconds accumulated since the pose last actually moved.</summary>
        float m_TimeSinceMovement;

        Vector3 m_FirstTrackedPosition;
        float m_TrackingSince = -1f;

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
        ///
        /// Deliberately not called Reset: that is a Unity message name, and the editor calls it
        /// on a component when it is added or reset from the inspector.
        /// </summary>
        public void RestartFrom()
        {
            if (m_Camera == null)
                return;

            m_LastPosition = m_Camera.position;
            Origin = m_LastPosition;
            DistanceWalked = 0f;
            DistanceFromOrigin = 0f;
            RelocalisationJumps = 0;
            JumpedMeters = 0f;
            m_TimeSinceMovement = 0f;
            m_Started = true;
        }

        void Update()
        {
            if (m_Camera == null)
                return;

            // Counting starts on its own rather than waiting for an alignment, because without a
            // printed marker there is never an alignment and the distance would stay at zero for
            // the whole run — the one measurement that proves the pose reaches the app at all.
            //
            // It waits for tracking first, though. Before that the camera sits at the world
            // origin, so starting early would fix Origin at (0,0,0) and report the distance from
            // there, and the leap to the first real pose would be counted as a relocalisation.
            if (!m_Started)
            {
                WaitForFirstPose();
                return;
            }

            var position = m_Camera.position;
            var step = Vector3.Distance(position, m_LastPosition);

            // Time is accumulated whether or not the pose moved. ARCore delivers poses at the
            // camera's rate, which is well below the render rate, so on the frame a new pose
            // lands the whole interval's movement arrives at once. Dividing that by one frame's
            // deltaTime multiplies the apparent speed several times over — in one run it turned
            // twenty-four ordinary walking steps of 0.14 m into "relocalisations". Dividing by
            // the time since the pose last moved measures the speed that actually happened.
            m_TimeSinceMovement += Time.deltaTime;

            if (step >= m_MinimumStepMeters)
            {
                // A step implying more than a sprint is ARCore repositioning itself, not walking.
                // Adding those to the total inflates the very axis drift is measured against: in
                // one 77 m run three such jumps contributed 5.4 m, seven percent of the distance.
                //
                // Speed alone is not enough. A long frame — a hitch, or the first frame after the
                // app comes back from the background — makes a two metre leap look like a stroll,
                // so an absolute cap backs it up.
                var speed = m_TimeSinceMovement > 0f ? step / m_TimeSinceMovement : float.MaxValue;
                if (speed > m_MaximumStepSpeed || step > m_MaximumStepMeters)
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
                m_TimeSinceMovement = 0f;
            }

            DistanceFromOrigin = Vector3.Distance(position, Origin);
        }

        /// <summary>
        /// Holds off until a real pose has arrived.
        ///
        /// The session reports SessionTracking a frame or two before the pose driver writes the
        /// first pose, and until then the camera sits at the world origin. Starting on the state
        /// change alone pinned Origin to (0,0,0) in one run, so "from marker" measured from a
        /// place nobody had stood, and the leap to the first real pose counted as a jump.
        /// </summary>
        void WaitForFirstPose()
        {
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                m_TrackingSince = -1f;
                return;
            }

            if (m_TrackingSince < 0f)
            {
                m_TrackingSince = Time.time;
                m_FirstTrackedPosition = m_Camera.position;
                return;
            }

            // Either the pose has moved — so it is being driven — or enough time has passed that
            // waiting longer would lose measurements on a phone held very still.
            if (m_Camera.position != m_FirstTrackedPosition || Time.time - m_TrackingSince > 1f)
                RestartFrom();
        }
    }
}
