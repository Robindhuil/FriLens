using UnityEngine;

namespace FriLens
{
    /// <summary>
    /// Holds the screen on for the whole session.
    ///
    /// The field test is a walk of tens of metres with long stretches where nobody touches the
    /// phone. Every screen lock ends the AR session, and a re-alignment after that measures the
    /// new session's drift, not the one being tested. The default timeout would quietly ruin a
    /// run that took twenty minutes to set up.
    ///
    /// The previous value is restored on destroy so the setting does not leak into the editor's
    /// next play session.
    /// </summary>
    public class KeepScreenAwake : MonoBehaviour
    {
        int m_PreviousTimeout;

        void OnEnable()
        {
            m_PreviousTimeout = Screen.sleepTimeout;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void OnDisable()
        {
            Screen.sleepTimeout = m_PreviousTimeout;
        }
    }
}
