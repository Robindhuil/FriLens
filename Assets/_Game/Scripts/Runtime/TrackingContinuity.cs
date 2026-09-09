using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FriLens
{
    /// <summary>
    /// Records whether the tracking has been continuous since the last alignment, and how badly it
    /// has not been.
    ///
    /// This exists because of the one failure the rest of the instrument cannot see. When ARCore
    /// loses tracking and then recognises where it is, it corrects its pose in one step — a jump,
    /// which <see cref="CameraTravel"/> counts and separates from walking. When it loses tracking
    /// and then fails to recognise where it is, there is no jump at all. The pose simply carries
    /// on from wherever it was left, the overlay is wrong by however far the tester moved while
    /// blind, and nothing in the log says a word about it.
    ///
    /// That silent case is worse than the loud one, because on screen it is indistinguishable from
    /// the model being inaccurate — which is the very thing the test is supposed to measure. So the
    /// instrument stops claiming to measure after a loss: everything from that point is
    /// <see cref="IsVerified"/> false until somebody re-anchors on a printed marker, which is the
    /// one source of truth that does not depend on ARCore's own map.
    ///
    /// This does not detect a failed recovery. It marks the window in which one could have
    /// happened, which is the most any amount of pose data can honestly say.
    /// </summary>
    public class TrackingContinuity : MonoBehaviour
    {
        [Tooltip("Losses shorter than this are ignored. A frame or two between states is the "
            + "session breathing, not an interruption a tester needs to know about.")]
        [SerializeField] float m_IgnoreShorterThanSeconds = 0.3f;

        bool m_Tracking;
        float m_LostSince = -1f;
        bool m_LossCounted;
        NotTrackingReason m_LostReason;

        /// <summary>
        /// Raised when tracking drops out, with the reason ARCore gave.
        ///
        /// The state column already records this four times a second, but scanning a thousand
        /// rows for the moment a value changed is how a loss gets missed. An event puts the two
        /// moments that matter on their own lines.
        /// </summary>
        public event Action<NotTrackingReason> Lost;

        /// <summary>Raised when tracking comes back, with how long it was gone.</summary>
        public event Action<float, NotTrackingReason> Regained;

        /// <summary>Seconds spent not tracking since the last alignment.</summary>
        public float BlindSeconds { get; private set; }

        /// <summary>Number of tracking losses since the last alignment.</summary>
        public int Losses { get; private set; }

        /// <summary>
        /// Whether everything measured since the last alignment can be trusted. False from the
        /// first tracking loss until the next alignment.
        /// </summary>
        public bool IsVerified => Losses == 0;

        /// <summary>
        /// Clears the record. Called when an alignment is applied: a marker seen and averaged is
        /// independent of whatever ARCore believed a moment earlier, so it settles the question.
        /// </summary>
        public void MarkVerified()
        {
            BlindSeconds = 0f;
            Losses = 0;

            // A loss still in progress is not settled by an alignment that cannot be happening:
            // a marker cannot be averaged while the session is down. Clearing the flag lets the
            // loss be counted again if it is somehow still running, rather than being forgotten.
            m_LossCounted = false;
        }

        void Update()
        {
            var tracking = ARSession.state == ARSessionState.SessionTracking;

            if (tracking == m_Tracking)
            {
                if (!tracking && m_LostSince >= 0f)
                {
                    BlindSeconds += Time.deltaTime;

                    // The reason is refreshed for as long as the session is down. On the frame
                    // tracking drops it is usually still None — ARCore works out why a frame or
                    // two later — so a reason captured only at the transition would report
                    // nothing useful for most losses.
                    var reason = ARSession.notTrackingReason;
                    if (reason != NotTrackingReason.None)
                        m_LostReason = reason;

                    // The loss is counted here, while it is still going on, rather than when
                    // tracking comes back. Counting it on the way back left IsVerified true for
                    // the whole blind period — so a session that lost its place and had not yet
                    // found it wrote verified=1 into every row, which is the one moment the flag
                    // exists for. A loss that never ends never came back at all, and that is the
                    // worst case of the lot.
                    if (!m_LossCounted && Time.time - m_LostSince >= m_IgnoreShorterThanSeconds)
                    {
                        m_LossCounted = true;
                        Losses++;
                        Lost?.Invoke(m_LostReason);
                    }
                }

                return;
            }

            m_Tracking = tracking;

            if (!tracking)
            {
                m_LostSince = Time.time;
                m_LossCounted = false;
                m_LostReason = ARSession.notTrackingReason;
                return;
            }

            // Tracking came back. Whether it came back in the right place is exactly what cannot be
            // known from here, which is the point of counting these at all.
            // Only losses that were reported get a matching regain. Otherwise the log carried
            // "tracking-lost" lines with nothing closing them, and a run looked far worse than it
            // was: a frame or two between states is the session breathing, not an interruption.
            if (m_LostSince >= 0f && m_LossCounted)
                Regained?.Invoke(Time.time - m_LostSince, m_LostReason);

            m_LostSince = -1f;
            m_LossCounted = false;
        }
    }
}
