using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;

namespace FriLens
{
    /// <summary>
    /// The on-screen readout for the field test, built on UI Toolkit.
    ///
    /// Without numbers on screen the test degrades into "it looked a bit off", which is not an
    /// answer to anything. Every row here exists because reading the result depends on it:
    /// tracking state says whether a jump was drift or a lost session, distance walked is the
    /// axis drift grows along, and the alignment's sample spread separates a real offset at the
    /// marker from tracker noise.
    ///
    /// The mode banner is deliberately loud. Preview mode draws the same overlay with none of the
    /// meaning, and somebody eventually will try to read accuracy off it.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DiagnosticsHud : MonoBehaviour
    {
        [SerializeField] SessionModeController m_Mode;
        [SerializeField] MarkerAlignment m_Alignment;
        [SerializeField] CameraTravel m_Travel;
        [SerializeField] SessionLogger m_Logger;

        [Tooltip("Renderer switched off by the Hide overlay button, so you can see what is under it.")]
        [SerializeField] Renderer m_Overlay;

        [Tooltip("Seconds of SessionInitializing after which the HUD stops waiting politely and "
            + "says the session is stuck.")]
        [SerializeField] float m_InitializingPatienceSeconds = 20f;

        Label m_ModeLabel;
        Label m_ModeDetail;
        Label m_Tracking;
        Label m_Device;
        Label m_Marker;
        Label m_AlignmentValue;
        Label m_Walked;
        Label m_FromOrigin;
        Label m_Log;
        VisualElement m_Banner;
        Button m_RealignButton;
        Button m_OverlayButton;
        Button m_MarkButton;

        int m_MarkCount;
        SessionModeController.SessionMode m_ShownMode = (SessionModeController.SessionMode)(-1);
        float m_InitializingSince = -1f;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            m_Banner = root.Q<VisualElement>("banner");
            m_ModeLabel = root.Q<Label>("mode-label");
            m_ModeDetail = root.Q<Label>("mode-detail");
            m_Tracking = root.Q<Label>("tracking-value");
            m_Device = root.Q<Label>("device-value");
            m_Marker = root.Q<Label>("marker-value");
            m_AlignmentValue = root.Q<Label>("alignment-value");
            m_Walked = root.Q<Label>("walked-value");
            m_FromOrigin = root.Q<Label>("origin-value");
            m_Log = root.Q<Label>("log-value");

            m_RealignButton = root.Q<Button>("realign-button");
            m_OverlayButton = root.Q<Button>("overlay-button");
            m_MarkButton = root.Q<Button>("mark-button");

            m_RealignButton.clicked += OnRealign;
            m_OverlayButton.clicked += OnToggleOverlay;
            m_MarkButton.clicked += OnMark;

            if (m_Alignment != null)
                m_Alignment.Aligned += OnAligned;
        }

        void OnDisable()
        {
            if (m_RealignButton != null) m_RealignButton.clicked -= OnRealign;
            if (m_OverlayButton != null) m_OverlayButton.clicked -= OnToggleOverlay;
            if (m_MarkButton != null) m_MarkButton.clicked -= OnMark;

            if (m_Alignment != null)
                m_Alignment.Aligned -= OnAligned;
        }

        void Update()
        {
            UpdateMode();
            UpdateTracking();
            UpdateDevice();
            UpdateMarker();
            UpdateAlignment();
            UpdateTravel();
            UpdateLog();
        }

        void UpdateMode()
        {
            if (m_Mode == null)
                return;

            if (m_ShownMode == m_Mode.Mode)
                return;

            m_ShownMode = m_Mode.Mode;

            m_Banner.RemoveFromClassList("banner--ar");
            m_Banner.RemoveFromClassList("banner--preview");

            switch (m_ShownMode)
            {
                case SessionModeController.SessionMode.Ar:
                    m_ModeLabel.text = "AR";
                    m_Banner.AddToClassList("banner--ar");
                    break;

                case SessionModeController.SessionMode.Preview:
                    m_ModeLabel.text = "PREVIEW — NOT A TEST";
                    m_Banner.AddToClassList("banner--preview");
                    break;

                default:
                    m_ModeLabel.text = "CHECKING";
                    break;
            }

            m_ModeDetail.text = m_Mode.Explanation;

            // Everything on these two buttons needs a tracked marker, which preview mode has not
            // got. Leaving them live would invite the conclusion that alignment is broken.
            var arRunning = m_ShownMode == SessionModeController.SessionMode.Ar;
            SetEnabled(m_RealignButton, arRunning);
            SetEnabled(m_OverlayButton, true);
            SetEnabled(m_MarkButton, true);
        }

        void UpdateTracking()
        {
            if (m_Mode != null && m_Mode.Mode == SessionModeController.SessionMode.Preview)
            {
                Set(m_Tracking, "no session", "idle");
                return;
            }

            var state = ARSession.state;
            var reason = ARSession.notTrackingReason;

            // A session that never leaves SessionInitializing reports no failure at all —
            // notTrackingReason stays None because nothing went wrong, tracking simply never
            // converges. Shown as a plain orange word it looks like "still working on it"
            // forever. After a while it is a finding, and the HUD should say so on its own
            // rather than needing a CSV pulled off the phone to notice.
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
                Set(m_Tracking, "tracking", null);
                return;
            }

            var stuckFor = m_InitializingSince < 0f ? 0f : Time.time - m_InitializingSince;
            if (stuckFor > m_InitializingPatienceSeconds)
            {
                Set(m_Tracking, $"stuck initializing {stuckFor:F0} s", "bad");
                return;
            }

            if (reason != UnityEngine.XR.ARSubsystems.NotTrackingReason.None)
                Set(m_Tracking, $"{state} · {reason}", "bad");
            else
                Set(m_Tracking, state.ToString(), "warn");
        }

        /// <summary>
        /// Hardware ARCore needs, read straight off the device.
        ///
        /// Motion tracking is visual-inertial: without a gyroscope ARCore can open the camera and
        /// still never initialise, which is indistinguishable on screen from a session that is
        /// merely slow. Naming the missing part turns a mystery into a fact.
        /// </summary>
        void UpdateDevice()
        {
            var gyro = SystemInfo.supportsGyroscope;
            var accelerometer = SystemInfo.supportsAccelerometer;

            if (!gyro)
                Set(m_Device, "no gyroscope — AR cannot track", "bad");
            else if (!accelerometer)
                Set(m_Device, "no accelerometer", "bad");
            else
                Set(m_Device, "gyro + accel ok", null);
        }

        void UpdateMarker()
        {
            if (m_Alignment == null)
                return;

            var marker = m_Alignment.TrackedMarker;

            if (marker == null)
            {
                Set(m_Marker, "not seen", "idle");
                return;
            }

            var tracking = marker.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking;
            Set(m_Marker, tracking ? "in view" : marker.trackingState.ToString(), tracking ? null : "warn");
        }

        void UpdateAlignment()
        {
            if (m_Alignment == null)
                return;

            switch (m_Alignment.State)
            {
                case MarkerAlignment.AlignmentState.Sampling:
                    Set(m_AlignmentValue,
                        $"sampling {m_Alignment.SamplesCollected}/{m_Alignment.SampleTarget}", "warn");
                    break;

                case MarkerAlignment.AlignmentState.Aligned:
                    Set(m_AlignmentValue,
                        $"{m_Alignment.TimeSinceAlignment:F0} s ago · "
                        + $"±{m_Alignment.SampleSpreadMeters * 100f:F1} cm / {m_Alignment.SampleSpreadDegrees:F1}°",
                        null);
                    break;

                default:
                    Set(m_AlignmentValue, "none", "idle");
                    break;
            }
        }

        void UpdateTravel()
        {
            if (m_Travel == null)
                return;

            if (!m_Travel.HasOrigin)
            {
                Set(m_Walked, "—", "idle");
                Set(m_FromOrigin, "—", "idle");
                return;
            }

            Set(m_Walked, $"{m_Travel.DistanceWalked:F1} m", null);
            Set(m_FromOrigin, $"{m_Travel.DistanceFromOrigin:F1} m", null);
        }

        void UpdateLog()
        {
            if (m_Logger == null)
                return;

            m_Log.text = string.IsNullOrEmpty(m_Logger.FilePath)
                ? "log: not writing"
                : $"log: {System.IO.Path.GetFileName(m_Logger.FilePath)} · {m_Logger.RowsWritten} rows · {m_MarkCount} marks";
        }

        void OnRealign()
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
            m_Travel?.Reset();
            m_Logger?.MarkEvent("aligned");
        }

        void OnToggleOverlay()
        {
            if (m_Overlay == null)
                return;

            m_Overlay.enabled = !m_Overlay.enabled;
            m_OverlayButton.text = m_Overlay.enabled ? "Hide overlay" : "Show overlay";
            m_Logger?.MarkEvent(m_Overlay.enabled ? "overlay-shown" : "overlay-hidden");
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

        static void Set(Label label, string text, string modifier)
        {
            label.text = text;
            label.RemoveFromClassList("stat__value--warn");
            label.RemoveFromClassList("stat__value--bad");
            label.RemoveFromClassList("stat__value--idle");

            if (modifier != null)
                label.AddToClassList("stat__value--" + modifier);
        }

        static void SetEnabled(Button button, bool enabled)
        {
            button.SetEnabled(enabled);
            button.EnableInClassList("button--disabled", !enabled);
        }
    }
}
