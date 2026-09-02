using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FriLens
{
    /// <summary>
    /// Writes the run to a CSV next to the app's data, so the walk can be read back afterwards.
    ///
    /// Photographs taken during the test capture moments; they cannot say when tracking dropped,
    /// how far had been walked at that point, or how noisy the marker pose was at the alignment.
    /// Without a log those questions get answered from memory, which is how "it looked about
    /// right" ends up in a report instead of a number.
    ///
    /// Rows are written at a fixed cadence plus one on every marked event. The file is flushed on
    /// events and whenever the app goes to the background, because a phone in a pocket gets its
    /// process killed without warning.
    /// </summary>
    public class SessionLogger : MonoBehaviour
    {
        [SerializeField] SessionModeController m_Mode;
        [SerializeField] MarkerAlignment m_Alignment;
        [SerializeField] CameraTravel m_Travel;
        [SerializeField] Transform m_Camera;

        [Tooltip("Rows per second while the app is running.")]
        [SerializeField, Range(0.5f, 20f)] float m_SamplesPerSecond = 4f;

        StreamWriter m_Writer;
        float m_NextSampleTime;

        /// <summary>Full path of the file being written, empty if logging failed to start.</summary>
        public string FilePath { get; private set; } = "";

        public int RowsWritten { get; private set; }

        void Start()
        {
            var name = $"frilens-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            FilePath = Path.Combine(Application.persistentDataPath, name);

            try
            {
                m_Writer = new StreamWriter(FilePath, false, Encoding.UTF8);
                m_Writer.WriteLine("time_s,mode,session_state,not_tracking_reason,"
                    + "cam_x,cam_y,cam_z,cam_yaw,cam_pitch,cam_roll,"
                    + "walked_m,from_origin_m,since_align_s,spread_cm,spread_deg,event");
                m_Writer.Flush();
                Debug.Log($"{nameof(SessionLogger)}: writing {FilePath}", this);
            }
            catch (Exception exception)
            {
                // A missing log is bad but it is not a reason to lose the run, so the app carries
                // on without one and says so.
                m_Writer = null;
                FilePath = "";
                Debug.LogError($"{nameof(SessionLogger)}: could not open the log. {exception.Message}", this);
            }
        }

        void Update()
        {
            if (m_Writer == null || Time.time < m_NextSampleTime)
                return;

            m_NextSampleTime = Time.time + 1f / m_SamplesPerSecond;
            Write("");
        }

        /// <summary>Writes a row tagged with a label. Used by the HUD buttons and by alignments.</summary>
        public void MarkEvent(string label)
        {
            if (m_Writer == null)
                return;

            Write(label);
            m_Writer.Flush();
        }

        void Write(string label)
        {
            var culture = CultureInfo.InvariantCulture;

            var position = m_Camera != null ? m_Camera.position : Vector3.zero;
            var euler = m_Camera != null ? m_Camera.rotation.eulerAngles : Vector3.zero;

            var state = ARSession.state;
            var reason = ARSession.notTrackingReason;
            var mode = m_Mode != null ? m_Mode.Mode.ToString() : "Unknown";

            var walked = m_Travel != null ? m_Travel.DistanceWalked : 0f;
            var fromOrigin = m_Travel != null ? m_Travel.DistanceFromOrigin : 0f;

            var sinceAlign = m_Alignment != null ? m_Alignment.TimeSinceAlignment : -1f;
            var spreadCm = m_Alignment != null ? m_Alignment.SampleSpreadMeters * 100f : 0f;
            var spreadDeg = m_Alignment != null ? m_Alignment.SampleSpreadDegrees : 0f;

            m_Writer.WriteLine(string.Join(",",
                Time.time.ToString("F3", culture),
                mode,
                state.ToString(),
                reason.ToString(),
                position.x.ToString("F4", culture),
                position.y.ToString("F4", culture),
                position.z.ToString("F4", culture),
                euler.y.ToString("F2", culture),
                euler.x.ToString("F2", culture),
                euler.z.ToString("F2", culture),
                walked.ToString("F3", culture),
                fromOrigin.ToString("F3", culture),
                sinceAlign.ToString("F2", culture),
                spreadCm.ToString("F2", culture),
                spreadDeg.ToString("F3", culture),
                label));

            RowsWritten++;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                m_Writer?.Flush();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
                m_Writer?.Flush();
        }

        void OnDestroy()
        {
            m_Writer?.Flush();
            m_Writer?.Dispose();
            m_Writer = null;
        }
    }
}
