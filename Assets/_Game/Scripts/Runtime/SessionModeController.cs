using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FriLens
{
    /// <summary>
    /// Decides at startup whether this phone can run the AR test or only preview the overlay,
    /// and turns on the matching rig.
    ///
    /// The app ships as AR Optional so it installs everywhere, which means the question
    /// "can this device do AR" moves from install time to runtime. Getting it wrong in either
    /// direction is bad: refusing AR on a capable phone wastes a trip to the building, and
    /// pretending to do AR on a phone without tracking would produce numbers that look like
    /// measurements and are not.
    ///
    /// Nothing seen in <see cref="SessionMode.Preview"/> says anything about alignment accuracy.
    /// It is a mesh viewer, not a test. The HUD has to keep saying so.
    /// </summary>
    public class SessionModeController : MonoBehaviour
    {
        public enum SessionMode
        {
            /// <summary>Availability check still running.</summary>
            Checking,

            /// <summary>ARCore is present and the session can track.</summary>
            Ar,

            /// <summary>No AR on this device. The overlay is drawn against a plain background.</summary>
            Preview
        }

        [SerializeField] ARSession m_Session;

        [Tooltip("XR Origin and everything that only makes sense while AR is running.")]
        [SerializeField] GameObject m_ArRig;

        [Tooltip("Camera and controls used when the device cannot do AR.")]
        [SerializeField] GameObject m_PreviewRig;

        [Header("Development")]
        [Tooltip("Auto asks the device. The other values skip the check and force a mode.")]
        [SerializeField] ModeOverride m_Override = ModeOverride.Auto;

        public enum ModeOverride
        {
            Auto,
            ForceAr,
            ForcePreview
        }

        public SessionMode Mode { get; private set; } = SessionMode.Checking;

        /// <summary>Session state at the moment the mode was decided.</summary>
        public ARSessionState DecidedFrom { get; private set; } = ARSessionState.None;

        /// <summary>Short sentence for the HUD explaining why this mode was chosen.</summary>
        public string Explanation { get; private set; } = "Checking what this device can do…";

        /// <summary>True when the phone could do AR but Google Play Services for AR is missing.</summary>
        public bool NeedsArServicesInstall { get; private set; }

        IEnumerator Start()
        {
            if (m_ArRig != null) m_ArRig.SetActive(false);
            if (m_PreviewRig != null) m_PreviewRig.SetActive(false);

            // In the editor, AR Foundation's XR Simulation answers the availability check with a
            // working session, so the preview path can never be reached by asking. Without a way
            // to force it, the mode a phone like the Redmi 14C actually lands in would only ever
            // be seen for the first time on that phone.
            if (m_Override != ModeOverride.Auto)
            {
                DecidedFrom = ARSession.state;
                if (m_Override == ModeOverride.ForcePreview) EnterPreview(); else EnterAr();
                Explanation = "Forced " + m_Override + " in the inspector. Not what this device reported.";
                yield break;
            }

            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
                yield return ARSession.CheckAvailability();

            // A device can be on Google's supported list and still have no ARCore, because
            // Google Play Services for AR is a separate app. That is worth telling apart from
            // "this phone cannot do it": the first is a trip to the Play Store, the second is not.
            if (ARSession.state == ARSessionState.NeedsInstall)
            {
                NeedsArServicesInstall = true;
                yield return ARSession.Install();
            }

            DecidedFrom = ARSession.state;

            if (ARSession.state == ARSessionState.Unsupported || ARSession.state == ARSessionState.NeedsInstall)
                EnterPreview();
            else
                EnterAr();
        }

        void EnterAr()
        {
            Mode = SessionMode.Ar;
            Explanation = NeedsArServicesInstall
                ? "AR ready after installing Google Play Services for AR."
                : "AR ready.";

            if (m_PreviewRig != null) m_PreviewRig.SetActive(false);
            if (m_ArRig != null) m_ArRig.SetActive(true);
            if (m_Session != null) m_Session.enabled = true;

            Debug.Log($"{nameof(SessionModeController)}: AR mode, session state {DecidedFrom}.", this);
        }

        void EnterPreview()
        {
            Mode = SessionMode.Preview;
            Explanation = DecidedFrom == ARSessionState.NeedsInstall
                ? "Google Play Services for AR is not installed, so AR cannot start."
                : "This device does not support ARCore. Showing the overlay without AR.";

            // The session is switched off rather than left idle: a running session on a device
            // that cannot track only burns battery and clutters the log with failure states.
            if (m_Session != null) m_Session.enabled = false;
            if (m_ArRig != null) m_ArRig.SetActive(false);
            if (m_PreviewRig != null) m_PreviewRig.SetActive(true);

            Debug.LogWarning($"{nameof(SessionModeController)}: preview mode. {Explanation} "
                + "Nothing shown here measures alignment accuracy.", this);
        }
    }
}
