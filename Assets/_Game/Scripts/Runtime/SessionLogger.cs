using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

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
        [SerializeField] TrackingContinuity m_Continuity;
        [SerializeField] FloorProbe m_FloorProbe;
        [SerializeField] AnchoredRoot m_AnchoredRoot;
        [SerializeField] Transform m_Camera;

        [Tooltip("Rows per second while the app is running.")]
        [SerializeField, Range(0.5f, 20f)] float m_SamplesPerSecond = 4f;

        [Tooltip("Riadkov za sekundu v surovom zázname značiek, pre každú sledovanú značku.")]
        [SerializeField, Range(1f, 60f)] float m_TraceSamplesPerSecond = 15f;

        StreamWriter m_Writer;
        float m_NextSampleTime;

        /// <summary>
        /// Surový záznam značiek: póza každého sledovaného obrázka a kamery po snímkach.
        ///
        /// Hlavný log nesie len priemer burstu, takže bránu na prvé čítanie značky nebolo na
        /// starých behoch ako overiť — každá zmena by stála cestu na fakultu. S týmto súborom sa
        /// ďalšie brány dajú ladiť doma na nahratých dátach.
        /// </summary>
        StreamWriter m_Trace;
        float m_NextTraceTime;
        float m_NextTraceFlushTime;

        int m_RecordingCount;

        /// <summary>Full path of the file being written, empty if logging failed to start.</summary>
        public string FilePath { get; private set; } = "";

        /// <summary>Beží nahrávanie relácie ARCore do mp4.</summary>
        public bool IsRecording { get; private set; }

        /// <summary>Cesta k poslednej nahrávke, prázdna ak žiadna nebola.</summary>
        public string RecordingPath { get; private set; } = "";

        public int RowsWritten { get; private set; }

        void Start()
        {
            var name = $"frilens-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            FilePath = Path.Combine(Application.persistentDataPath, name);

            try
            {
                m_Writer = new StreamWriter(FilePath, false, Encoding.UTF8);
                // walked_m is resampled, path_raw_m is the frame-by-frame sum. Both are logged
                // because the gap between them is the hand movement and tracker noise that the
                // resampling removed, and that gap is worth reading afterwards rather than
                // taking on trust.
                m_Writer.WriteLine("time_s,mode,session_state,not_tracking_reason,"
                    + "cam_x,cam_y,cam_z,cam_yaw,cam_pitch,cam_roll,"
                    + "walked_m,path_raw_m,from_origin_m,jumps,jumped_m,"
                    + "blind_s,losses,verified,origin_anchored,overlay_anchored,"
                    + "probes,eye_m,since_align_s,spread_cm,spread_deg,marker,event");
                m_Writer.Flush();
                Debug.Log($"{nameof(SessionLogger)}: writing {FilePath}", this);

                // Which phone, and does it have the sensors ARCore needs. Without this a log that
                // shows tracking never starting cannot be told apart from a log taken on hardware
                // that was never able to track in the first place. Commas are stripped because
                // this goes in a CSV field.
                // The app version goes in the first row. Without it a CSV read a month later
                // can only be dated by guessing which columns it has, and every fix in this file
                // changes what a column means.
                MarkEvent(("frilens " + Application.version
                    + "; device " + SystemInfo.deviceModel
                    + "; android " + SystemInfo.operatingSystem
                    + "; gyro " + SystemInfo.supportsGyroscope
                    + "; accel " + SystemInfo.supportsAccelerometer
                    + "; gfx " + SystemInfo.graphicsDeviceType).Replace(',', ' '));

                OpenTrace();
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

        /// <summary>
        /// Subscribes to the two moments worth a line of their own.
        ///
        /// Done here rather than through the HUD so that the log still records them if the HUD
        /// fails to build — the log is the artefact the test produces, and it should not depend
        /// on anything that only exists to be looked at.
        /// </summary>
        void OnEnable()
        {
            Application.logMessageReceived += OnUnityLog;

            if (m_Continuity == null)
                return;

            m_Continuity.Lost += OnTrackingLost;
            m_Continuity.Regained += OnTrackingRegained;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= OnUnityLog;

            if (m_Continuity == null)
                return;

            m_Continuity.Lost -= OnTrackingLost;
            m_Continuity.Regained -= OnTrackingRegained;
        }

        /// <summary>
        /// Každé varovanie, chybu a výnimku z Unity zapíše do CSV ako udalosť.
        ///
        /// Zamietnutý fit, zahodený burst či prepnutie značky uprostred burstu sa hlásia cez
        /// <c>Debug.LogWarning</c>, a to ide len do logcatu — na fakulte bez kábla k počítaču
        /// sa stratí. CSV je jediné, čo sa z telefónu prinesie späť, tak tam musí byť všetko.
        /// Bežné <c>Debug.Log</c> sa nepíše: zarovnanie má vlastný riadok a zvyšok je šum.
        /// </summary>
        void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log)
                return;

            var text = condition ?? "";
            if (type == LogType.Exception && !string.IsNullOrEmpty(stackTrace))
            {
                // Prvý riadok stacku stačí na to, aby sa výnimka dala nájsť v kóde.
                var firstFrame = stackTrace.Split('\n')[0];
                text += " @ " + firstFrame;
            }

            // To isté hlásenie po snímkach by zahltilo súbor a flush na každom riadku by bral
            // snímky. Opakovanie sa zapíše najviac raz za päť sekúnd, s počtom vynechaných.
            if (text == m_LastUnityLog && Time.unscaledTime - m_LastUnityLogTime < 5f)
            {
                m_RepeatedUnityLogs++;
                return;
            }

            var repeated = m_RepeatedUnityLogs > 0 && text == m_LastUnityLog
                ? $" (x{m_RepeatedUnityLogs + 1})" : "";
            m_LastUnityLog = text;
            m_LastUnityLogTime = Time.unscaledTime;
            m_RepeatedUnityLogs = 0;

            MarkEvent("log-" + type.ToString().ToLowerInvariant() + " "
                + text.Replace('\n', ' ').Replace('\r', ' ').Replace('"', '\'') + repeated);
        }

        string m_LastUnityLog = "";
        float m_LastUnityLogTime = float.NegativeInfinity;
        int m_RepeatedUnityLogs;

        void OnTrackingLost(UnityEngine.XR.ARSubsystems.NotTrackingReason reason)
        {
            MarkEvent($"tracking-lost {reason}");
        }

        void OnTrackingRegained(float goneSeconds, UnityEngine.XR.ARSubsystems.NotTrackingReason reason)
        {
            MarkEvent(string.Format(CultureInfo.InvariantCulture,
                "tracking-regained after {0:F1} s; was {1}", goneSeconds, reason));
        }

        void Update()
        {
            WriteTrace();

            if (m_Writer == null || Time.time < m_NextSampleTime)
                return;

            m_NextSampleTime = Time.time + 1f / m_SamplesPerSecond;
            Write("");
        }

        /// <summary>
        /// Otvorí <c>frilens-…-markers.csv</c> vedľa hlavného logu, s tým istým časovým razítkom,
        /// aby sa dali spárovať. Zlyhanie nezastaví hlavný log.
        /// </summary>
        void OpenTrace()
        {
            var path = Path.Combine(Application.persistentDataPath,
                Path.GetFileNameWithoutExtension(FilePath) + "-markers.csv");
            try
            {
                m_Trace = new StreamWriter(path, false, Encoding.UTF8);
                // jump_gen oddeľuje mapy: polohy z dvoch rôznych úsekov sa nesmú porovnávať.
                m_Trace.WriteLine("time_s,image,state,px,py,pz,qx,qy,qz,qw,"
                    + "cam_x,cam_y,cam_z,cam_qx,cam_qy,cam_qz,cam_qw,jump_gen,settled,sweep_deg");
                m_Trace.Flush();
            }
            catch (Exception exception)
            {
                m_Trace = null;
                Debug.LogError($"{nameof(SessionLogger)}: could not open the marker trace. {exception.Message}", this);
            }
        }

        void WriteTrace()
        {
            if (m_Trace == null || m_Alignment == null || m_Alignment.TrackedImageManager == null
                || Time.time < m_NextTraceTime)
                return;

            m_NextTraceTime = Time.time + 1f / m_TraceSamplesPerSecond;

            var culture = CultureInfo.InvariantCulture;
            var camPosition = m_Camera != null ? m_Camera.position : Vector3.zero;
            var camRotation = m_Camera != null ? m_Camera.rotation : Quaternion.identity;
            var segment = m_Travel != null ? m_Travel.JumpGeneration : 0;

            foreach (var image in m_Alignment.TrackedImageManager.trackables)
            {
                // Aj Limited sa píše: práve prechod Limited → Tracking a skok pózy pri ňom je to,
                // čo sa z tohto súboru má dať vyčítať.
                if (image.trackingState == TrackingState.None)
                    continue;

                var name = image.referenceImage.name;
                var p = image.transform.position;
                var q = image.transform.rotation;

                m_Trace.WriteLine(string.Join(",",
                    Time.time.ToString("F3", culture),
                    MarkerAlignment.ShortName(name),
                    image.trackingState.ToString(),
                    p.x.ToString("F4", culture), p.y.ToString("F4", culture), p.z.ToString("F4", culture),
                    q.x.ToString("F5", culture), q.y.ToString("F5", culture),
                    q.z.ToString("F5", culture), q.w.ToString("F5", culture),
                    camPosition.x.ToString("F4", culture), camPosition.y.ToString("F4", culture),
                    camPosition.z.ToString("F4", culture),
                    camRotation.x.ToString("F5", culture), camRotation.y.ToString("F5", culture),
                    camRotation.z.ToString("F5", culture), camRotation.w.ToString("F5", culture),
                    segment.ToString(culture),
                    m_Alignment.IsSettled(name) ? "1" : "0",
                    m_Alignment.SettleSweepDegrees(name).ToString("F1", culture)));
            }

            if (Time.time >= m_NextTraceFlushTime)
            {
                m_NextTraceFlushTime = Time.time + 1f;
                m_Trace.Flush();
            }
        }

        /// <summary>
        /// Zapne alebo vypne nahrávanie relácie ARCore (kamera a senzory) do mp4 vedľa logu.
        ///
        /// Nahrávka sa dá doma prehrať v telefóne cez tú istú appku, takže jedna cesta na fakultu
        /// dá dataset na opakované skúšanie namiesto jedného pokusu. ARCore pri štarte aj stope
        /// reláciu na 0,5 – 1 s pozastaví, čo log zapíše ako krátku stratu trackingu a zahodí
        /// observácie značiek — preto zapínať pred zarovnaním, nie po ňom.
        /// </summary>
        public bool SetRecording(bool on)
        {
            if (on == IsRecording)
                return IsRecording;

            var session = FindAnyObjectByType<ARSession>();
            if (session == null || session.subsystem is not ARCoreSessionSubsystem arcore)
            {
                MarkEvent("rec-unavailable; no ARCore session");
                return IsRecording;
            }

            if (!on)
            {
                var stopped = arcore.StopRecording();
                IsRecording = false;
                MarkEvent($"rec-stopped {stopped}; {Path.GetFileName(RecordingPath)}");
                return IsRecording;
            }

            // Pomenovaná podľa logu, aby sa dala spárovať. Keď sa log neotvoril, aspoň časom.
            m_RecordingCount++;
            var stem = FilePath.Length > 0
                ? Path.GetFileNameWithoutExtension(FilePath)
                : $"frilens-{DateTime.Now:yyyyMMdd-HHmmss}";
            var path = Path.Combine(Application.persistentDataPath, $"{stem}-rec{m_RecordingCount}.mp4");

            ArStatus status;
            using (var config = new ArRecordingConfig(arcore.session))
            {
                config.SetMp4DatasetUri(arcore.session, new Uri(path).AbsoluteUri);
                status = arcore.StartRecording(config);
            }

            IsRecording = status == ArStatus.Success;
            if (IsRecording)
                RecordingPath = path;

            MarkEvent($"rec-start {status}; {Path.GetFileName(path)}");
            return IsRecording;
        }

        /// <summary>Writes a row tagged with a label. Used by the HUD buttons and by alignments.</summary>
        public void MarkEvent(string label)
        {
            if (m_Writer == null)
                return;

            // Commas are stripped from every label, not just the device line. A label built with
            // string interpolation picks up the device's culture, and on a Slovak phone a decimal
            // point is a comma — which split "probe-1 eye 1.70 m" across two CSV columns and made
            // the rest of the row unreadable. The callers were fixed; this is the backstop, because
            // the next label will be written by somebody who has forgotten this ever happened.
            Write(label.Replace(',', ' '));
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
            var pathRaw = m_Travel != null ? m_Travel.PathRawMeters : 0f;
            var fromOrigin = m_Travel != null ? m_Travel.DistanceFromOrigin : 0f;
            var jumps = m_Travel != null ? m_Travel.RelocalisationJumps : 0;
            var jumped = m_Travel != null ? m_Travel.JumpedMeters : 0f;

            // Rows written while verified is 0 describe a session that lost its place at least once
            // and may or may not have found it again. Nothing else in the row can tell the
            // difference, so nothing after such a row should be quoted as a measurement until an
            // alignment clears it.
            var blind = m_Continuity != null ? m_Continuity.BlindSeconds : 0f;
            var losses = m_Continuity != null ? m_Continuity.Losses : 0;
            var verified = m_Continuity == null || m_Continuity.IsVerified ? 1 : 0;
            var originAnchored = m_Travel != null && m_Travel.OriginAnchored ? 1 : 0;
            var overlayAnchored = m_AnchoredRoot != null && m_AnchoredRoot.IsAnchored ? 1 : 0;

            // The assumed height can be retuned mid-run, so it is logged per row rather than
            // once. Without it a probe's gap figure cannot be reproduced afterwards.
            var probes = m_FloorProbe != null ? m_FloorProbe.Count : 0;
            var eye = m_FloorProbe != null ? m_FloorProbe.EyeHeightMeters : 0f;

            // Which marker the overlay is currently aligned from, on every row rather than
            // only on the row where the alignment happened. Two alignments in one run is the
            // whole point of a second marker, and telling their rows apart afterwards should not
            // require carrying a value forward from an event line by hand.
            var marker = m_Alignment != null ? m_Alignment.AlignedImageName : "";

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
                pathRaw.ToString("F3", culture),
                fromOrigin.ToString("F3", culture),
                jumps.ToString(culture),
                jumped.ToString("F3", culture),
                blind.ToString("F2", culture),
                losses.ToString(culture),
                verified.ToString(culture),
                originAnchored.ToString(culture),
                overlayAnchored.ToString(culture),
                probes.ToString(culture),
                eye.ToString("F2", culture),
                sinceAlign.ToString("F2", culture),
                spreadCm.ToString("F2", culture),
                spreadDeg.ToString("F3", culture),
                marker,
                label));

            RowsWritten++;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                m_Writer?.Flush();
                m_Trace?.Flush();
            }
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                m_Writer?.Flush();
                m_Trace?.Flush();
            }
        }

        void OnDestroy()
        {
            if (IsRecording)
                SetRecording(false);

            m_Writer?.Flush();
            m_Writer?.Dispose();
            m_Writer = null;

            m_Trace?.Flush();
            m_Trace?.Dispose();
            m_Trace = null;
        }
    }
}
