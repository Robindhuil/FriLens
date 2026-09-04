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
                }

                return;
            }

            m_Tracking = tracking;

            if (!tracking)
            {
                m_LostSince = Time.time;
                m_LostReason = ARSession.notTrackingReason;
                Lost?.Invoke(m_LostReason);
                return;
            }

            // Tracking came back. Whether it came back in the right place is exactly what cannot be
            // known from here, which is the point of counting these at all.
            if (m_LostSince >= 0f)
            {
                var gone = Time.time - m_LostSince;
                if (gone >= m_IgnoreShorterThanSeconds)
                {
                    Losses++;
                    Regained?.Invoke(gone, m_LostReason);
                }
            }

            m_LostSince = -1f;
        }
    }
}
