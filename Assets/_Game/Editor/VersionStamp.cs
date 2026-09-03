using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FriLens.EditorTools
{
    /// <summary>
    /// Stamps <see cref="AndroidBuilder.Version"/> into Player Settings before every build,
    /// however that build was started.
    ///
    /// Keeping the version in code was meant to stop Player Settings from drifting, but on its
    /// own it only worked for builds started from the FriLens menu item. A build started from
    /// Unity's own Build dialog never ran that code and silently shipped the previous version
    /// number — 0.1.1-alpha's first APK went out labelled 0.1.0-alpha, with the fixes inside and
    /// the wrong name on the box. A build callback closes that gap: there is no way to build
    /// without it running.
    /// </summary>
    public class VersionStamp : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var previousVersion = PlayerSettings.bundleVersion;
            var previousCode = PlayerSettings.Android.bundleVersionCode;

            PlayerSettings.bundleVersion = AndroidBuilder.Version;
            PlayerSettings.Android.bundleVersionCode = AndroidBuilder.VersionCode;

            if (previousVersion == AndroidBuilder.Version && previousCode == AndroidBuilder.VersionCode)
                return;

            Debug.Log($"Version stamped from AndroidBuilder: {previousVersion} ({previousCode})"
                + $" -> {AndroidBuilder.Version} ({AndroidBuilder.VersionCode}).");
        }
    }
}
