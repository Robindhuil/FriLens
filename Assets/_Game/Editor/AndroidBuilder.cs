using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FriLens.EditorTools
{
    /// <summary>
    /// FriLens &gt; Build Android.
    ///
    /// One click: stamps the version, builds the APK, and drops it in a folder named after that
    /// version next to the other Unity builds in Documents.
    ///
    /// The version lives here as a constant rather than in Player Settings because this is the
    /// only thing that writes it. Player Settings can be overwritten by the editor at any save,
    /// and a build whose folder name and manifest disagree is worse than no version at all.
    ///
    /// To cut a new build: raise <see cref="Version"/> and <see cref="VersionCode"/> together,
    /// note the change in CHANGELOG.md, then run the menu item.
    /// </summary>
    public static class AndroidBuilder
    {
        /// <summary>Human-readable version. Also the name of the output folder.</summary>
        public const string Version = "0.1.7-alpha";

        /// <summary>Android versionCode. Must go up on every build Android is asked to install over another.</summary>
        public const int VersionCode = 8;

        const string ProductName = "FriLens";

        [MenuItem("FriLens/Build Android " + Version)]
        public static void Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.LogError("Build refused: active build target is "
                    + EditorUserBuildSettings.activeBuildTarget
                    + ". Switch to Android in File > Build Profiles first. "
                    + "Switching reimports every asset, so it is not done for you here.");
                return;
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("Build refused: no enabled scenes in Build Profiles.");
                return;
            }

            PlayerSettings.bundleVersion = Version;
            PlayerSettings.Android.bundleVersionCode = VersionCode;
            AssetDatabase.SaveAssets();

            var folder = OutputFolder();
            Directory.CreateDirectory(folder);
            // Hyphen, not a space: this file is uploaded to GitHub Releases as-is, and a space
            // in an asset name turns into a dot in the download URL. Keeping them identical
            // means the link on the web can be predicted from the version alone.
            var apk = Path.Combine(folder, $"{ProductName}-{Version}.apk");

            Debug.Log($"Building {Version} to {apk}. An IL2CPP build takes a while and the editor "
                + "is unresponsive until it finishes; the first one is by far the slowest.");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            });

            var summary = report.summary;
            var log = new StringBuilder();
            log.AppendLine($"Build {Version} ({VersionCode}): {summary.result}");
            log.AppendLine($"  output   : {apk}");
            log.AppendLine($"  duration : {summary.totalTime}");
            log.AppendLine($"  size     : {(File.Exists(apk) ? new FileInfo(apk).Length : 0L) / 1024f / 1024f:F1} MiB (apk), "
                + $"{summary.totalSize / 1024 / 1024} MB (whole output)");
            log.AppendLine($"  errors   : {summary.totalErrors}, warnings: {summary.totalWarnings}");

            foreach (var step in report.steps)
                foreach (var message in step.messages)
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                        log.AppendLine($"  ERROR in '{step.name}': {message.content}");

            if (summary.result == BuildResult.Succeeded)
            {
                WriteBuildInfo(folder, apk);
                Debug.Log(log.ToString());
                EditorUtility.RevealInFinder(apk);
            }
            else
            {
                Debug.LogError(log.ToString());
            }
        }

        /// <summary>
        /// Sits beside the other Unity builds in Documents, one folder per version. The layout
        /// matches the one the 0.1.0-alpha build was filed under by hand; FriLens only ships to
        /// Android, so there is no platform folder the way friworld has builds/web and builds/win.
        ///
        /// Derived from the OS documents folder rather than hard-coded, so the path is not tied
        /// to one machine.
        /// </summary>
        public static string OutputFolder()
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, "Robin", "unity", "frilens", Version);
        }

        /// <summary>
        /// A plain text note next to the APK. Once several versions pile up, "which commit was
        /// this and what did it have turned on" is the question nobody can answer from the file
        /// alone.
        /// </summary>
        static void WriteBuildInfo(string folder, string apk)
        {
            var android = NamedBuildTarget.Android;
            var info = new StringBuilder();

            info.AppendLine($"FriLens {Version} (versionCode {VersionCode})");
            info.AppendLine($"built      : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"unity      : {Application.unityVersion}");
            // The APK on disk, not BuildSummary.totalSize. That figure counts everything
            // the build produced, symbol folders included, and reported 973 MB for an APK
            // of 40 — a number nobody could act on and everybody would quote.
            var bytes = File.Exists(apk) ? new FileInfo(apk).Length : 0L;
            info.AppendLine($"size       : {bytes / 1024f / 1024f:F1} MiB");
            info.AppendLine();
            info.AppendLine($"bundle id  : {PlayerSettings.GetApplicationIdentifier(android)}");
            info.AppendLine($"min sdk    : {PlayerSettings.Android.minSdkVersion}");
            info.AppendLine($"target sdk : {PlayerSettings.Android.targetSdkVersion}");
            info.AppendLine($"backend    : {PlayerSettings.GetScriptingBackend(android)}");
            info.AppendLine($"abi        : {PlayerSettings.Android.targetArchitectures}");
            info.AppendLine($"graphics   : {string.Join(", ", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android))}");
            info.AppendLine($"orientation: {PlayerSettings.defaultInterfaceOrientation}");
            info.AppendLine();

            foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled))
                info.AppendLine($"scene      : {scene.path}");

            File.WriteAllText(Path.Combine(folder, "build-info.txt"), info.ToString());
        }
    }
}
