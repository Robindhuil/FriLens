using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FriLens
{
    /// <summary>
    /// Reads the state of the test and hands it to <see cref="DiagnosticsHudView"/>.
    ///
    /// Without numbers on screen the test degrades into "it looked a bit off", which answers
    /// nothing. Every row exists because reading the result depends on it: tracking state says
    /// whether a jump was drift or a lost session, distance walked is the axis drift grows
    /// along, and the alignment's sample spread separates a real offset at the marker from
    /// tracker noise.
    ///
    /// This class knows what the numbers mean; the view knows how they look. Neither knows the
    /// other's job.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DiagnosticsHud : MonoBehaviour
    {
        [SerializeField] SessionModeController m_Mode;
        [SerializeField] MarkerAlignment m_Alignment;
        [SerializeField] CameraTravel m_Travel;
        [SerializeField] SessionLogger m_Logger;
        [SerializeField] ProvisionalPlacement m_Placement;
        [SerializeField] TrackingContinuity m_Continuity;

        [Tooltip("Renderer switched off by the overlay button, so you can see what is under it.")]
        [SerializeField] Renderer m_Overlay;

        [Tooltip("Seconds of SessionInitializing after which the HUD stops waiting politely and "
            + "says the session is stuck.")]
        [SerializeField] float m_InitializingPatienceSeconds = 20f;

        DiagnosticsHudView m_View;

        int m_MarkCount;
        float m_InitializingSince = -1f;
        SessionModeController.SessionMode m_ShownMode = (SessionModeController.SessionMode)(-1);

        void OnDisable()
        {
            if (m_View != null)
            {
                m_View.Reanchor -= OnReanchor;
                m_View.Mark -= OnMark;
                m_View.OverlayToggled -= OnOverlayToggled;
                m_View = null;
            }

            if (m_Alignment != null)
                m_Alignment.Aligned -= OnAligned;
        }

        /// <summary>
        /// Builds the view on the first frame the document has actually been populated.
        ///
        /// UIDocument fills its root in its own OnEnable, and the order between two components
        /// on the same object is not defined. Constructing eagerly would work most of the time
        /// and throw on the times it did not, which is the worst of both.
        /// </summary>
        bool EnsureView()
        {
            if (m_View != null)
                return true;

            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null || root.Q("hud-root") == null)
                return false;

            m_View = new DiagnosticsHudView(root);
            m_View.Reanchor += OnReanchor;
            m_View.Mark += OnMark;
            m_View.OverlayToggled += OnOverlayToggled;

            if (m_Overlay != null)
                m_View.SetOverlayVisible(m_Overlay.enabled);

            if (m_Alignment != null)
                m_Alignment.Aligned += OnAligned;

            // A freshly built view shows whatever the UXML declared, so push the mode through
            // even if it has not changed since last frame.
            m_ShownMode = (SessionModeController.SessionMode)(-1);
            return true;
        }

        void Update()
        {
            if (!EnsureView())
                return;

            UpdateMode();
            UpdateTracking();
            UpdateMarker();
            UpdateAlignment();
            UpdateTravel();
            UpdateDevice();
            UpdateLog();
        }

        void UpdateMode()
        {
            if (m_Mode == null || m_ShownMode == m_Mode.Mode)
                return;

            m_ShownMode = m_Mode.Mode;

            m_View.SetMode(m_ShownMode switch
            {
                SessionModeController.SessionMode.Ar => HudMode.Ar,
                SessionModeController.SessionMode.Preview => HudMode.Preview,
                _ => HudMode.Checking
            }, m_Mode.Explanation);
        }

        void UpdateTracking()
        {
            if (m_Mode != null && m_Mode.Mode == SessionModeController.SessionMode.Preview)
                return;

            var state = ARSession.state;
            var reason = ARSession.notTrackingReason;

            // A session that never leaves SessionInitializing reports no failure at all —
            // notTrackingReason stays None because nothing went wrong, tracking simply never
            // converges. Shown as a plain warning it looks like "still working on it" forever.
            // After a while it is a finding, and the HUD should say so on its own rather than
            // needing a CSV pulled off the phone to notice.
            if (state == ARSessionState.SessionInitializing)
            {
                if (m_InitializingSince < 0f)
                    m_InitializingSince = Time.time;
            }
            else
            {
                m_InitializingSince = -1f;
            }

            if (state == ARSessionState.SessionTracking)
            {
                m_View.SetRow(HudRow.Tracking, "tracking", ValueState.Ok);
                return;
            }

            var stuckFor = m_InitializingSince < 0f ? 0f : Time.time - m_InitializingSince;
            if (stuckFor > m_InitializingPatienceSeconds)
            {
                m_View.SetRow(HudRow.Tracking, $"stuck initializing {stuckFor:F0} s", ValueState.Bad);
                return;
            }

            // What the tester needs is the remedy, not the enum. ARCore's own guidance is to turn
            // a failure into an instruction — the reason is kept alongside it because this is a
            // measuring instrument and the log has to stay readable against the screen.
            var advice = TrackingAdvice.For(reason);
            if (reason != NotTrackingReason.None)
                m_View.SetRow(HudRow.Tracking,
                    advice.Length > 0 ? advice : $"{state} · {reason}",
                    TrackingAdvice.IsActionable(reason) ? ValueState.Bad : ValueState.Warn);
            else
                m_View.SetRow(HudRow.Tracking, state.ToString(), ValueState.Warn);
        }

        void UpdateMarker()
        {
            if (m_Alignment == null || InPreview)
                return;

            var marker = m_Alignment.TrackedMarker;
            if (marker == null)
            {
                m_View.SetRow(HudRow.Marker, "none in view", ValueState.Idle);
                return;
            }

            var tracking = marker.trackingState == TrackingState.Tracking;
            m_View.SetRow(HudRow.Marker,
                tracking ? "in view" : marker.trackingState.ToString(),
                tracking ? ValueState.Ok : ValueState.Warn);
        }

        void UpdateAlignment()
        {
            if (m_Alignment == null || InPreview)
                return;

            // A tracking loss outranks everything else this row could say. If ARCore lost its place
            // and failed to find it again, the overlay is wrong by however far the tester moved
            // while blind, there is no jump in the log to show it, and on screen it looks exactly
            // like the model being inaccurate. Saying "aligned 90 s ago" over that would be a lie
            // of omission about the one number the whole test produces.
            if (m_Continuity != null && !m_Continuity.IsVerified)
            {
                var losses = m_Continuity.Losses;
                m_View.SetRow(HudRow.Alignment,
                    $"unverified · {losses} loss{(losses == 1 ? "" : "es")} {m_Continuity.BlindSeconds:F0} s",
                    ValueState.Bad);
                return;
            }

            switch (m_Alignment.State)
            {
                case MarkerAlignment.AlignmentState.Sampling:
                    m_View.SetRow(HudRow.Alignment,
                        $"sampling {m_Alignment.SamplesCollected}/{m_Alignment.SampleTarget}",
                        ValueState.Warn);
                    break;

                case MarkerAlignment.AlignmentState.Aligned:
                    m_View.SetRow(HudRow.Alignment,
                        $"{m_Alignment.TimeSinceAlignment:F0} s ago · "
                        + $"±{m_Alignment.SampleSpreadMeters * 100f:F1} cm / {m_Alignment.SampleSpreadDegrees:F1}°",
                        ValueState.Ok);
                    break;

                default:
                    // "none" read as a blank on the phone. The row has to say what the state
                    // costs the tester: an unanchored overlay is scenery, and the offsets you
                    // can see between it and the wall are not measurements of anything.
                    m_View.SetRow(HudRow.Alignment,
                        m_Placement != null && m_Placement.IsProvisional
                            ? "dropped, not measured"
                            : "waiting for marker",
                        ValueState.Idle);
                    break;
            }
        }

        void UpdateTravel()
        {
            if (m_Travel == null || InPreview)
                return;

            if (!m_Travel.HasOrigin)
            {
                m_View.SetWalked(0f, ValueState.Idle);
                m_View.SetWalkedNote("");
                m_View.SetRow(HudRow.FromMarker, "—", ValueState.Idle);
                return;
            }

            // Warn rather than bad when the session is not tracking: the figure is stale, not
            // wrong, and painting it red during the ordinary few seconds before tracking starts
            // would cry wolf every single run.
            var tracking = ARSession.state == ARSessionState.SessionTracking;

            // The raw sum rides under the headline figure rather than replacing it. Seeing both
            // is what turns "the distance looks too high" into a number: the gap is the hand
            // movement and tracker noise that the resampled figure leaves out, and a tester who
            // watches it grow while standing still learns more about the method in ten seconds
            // than the documentation can tell them.
            m_View.SetWalked(m_Travel.DistanceWalked, tracking ? ValueState.Ok : ValueState.Warn);
            m_View.SetWalkedNote($"raw {m_Travel.PathRawMeters:F1} m");

            // Jumps ride along on the straight-line row because they are the same kind of fact:
            // how much of what you are looking at came from the tracker rather than from walking.
            var jumps = m_Travel.RelocalisationJumps;
            if (jumps > 0)
                m_View.SetRow(HudRow.FromMarker,
                    $"{m_Travel.DistanceFromOrigin:F1} m · {jumps} jump{(jumps == 1 ? "" : "s")} "
                    + $"{m_Travel.JumpedMeters:F1} m",
                    ValueState.Warn);
            else
                m_View.SetRow(HudRow.FromMarker, $"{m_Travel.DistanceFromOrigin:F1} m", ValueState.Ok);
        }

        /// <summary>
        /// Hardware ARCore needs, read straight off the device.
        ///
        /// Motion tracking is visual-inertial: without a gyroscope ARCore can open the camera and
        /// still never initialise, which on screen is indistinguishable from a session that is
        /// merely slow. Naming the missing part turns a mystery into a fact.
        /// </summary>
        void UpdateDevice()
        {
            if (!SystemInfo.supportsGyroscope)
                m_View.SetRow(HudRow.Device, "no gyroscope", ValueState.Bad);
            else if (!SystemInfo.supportsAccelerometer)
                m_View.SetRow(HudRow.Device, "no accelerometer", ValueState.Bad);
            else
                m_View.SetRow(HudRow.Device, "gyro + accel", ValueState.Ok);
        }

        void UpdateLog()
        {
            if (m_Logger == null)
                return;

            if (string.IsNullOrEmpty(m_Logger.FilePath))
                m_View.SetLogNotWriting();
            else
                m_View.SetLog(System.IO.Path.GetFileName(m_Logger.FilePath),
                    m_Logger.RowsWritten, m_MarkCount);
        }

        bool InPreview => m_Mode != null && m_Mode.Mode == SessionModeController.SessionMode.Preview;

        void OnReanchor()
        {
            m_Alignment?.Realign();
            m_Logger?.MarkEvent("realign-requested");
        }

        /// <summary>
        /// Distance walked restarts here, not at the button press: the sample burst takes a
        /// moment and whatever is walked during it belongs to the alignment being applied.
        /// </summary>
        void OnAligned()
        {
            m_Travel?.RestartFrom();

            // A marker seen and averaged does not depend on ARCore's map, so it settles whatever
            // question a tracking loss had opened. This is the only thing that clears the flag.
            m_Continuity?.MarkVerified();

            m_Logger?.MarkEvent("aligned");
        }

        void OnOverlayToggled(bool visible)
        {
            if (m_Overlay != null)
                m_Overlay.enabled = visible;

            m_Logger?.MarkEvent(visible ? "overlay-shown" : "overlay-hidden");
        }

        /// <summary>
        /// Drops a numbered marker into the log. Pressed at each measuring point on the walk so a
        /// photograph taken there can be matched to a row afterwards.
        /// </summary>
        void OnMark()
        {
            m_MarkCount++;
            m_Logger?.MarkEvent($"mark-{m_MarkCount}");
        }
    }
}
