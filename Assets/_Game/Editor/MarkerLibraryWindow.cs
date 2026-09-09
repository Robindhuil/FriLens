using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace FriLens.EditorTools
{
    /// <summary>
    /// Fills the reference image library from the marker PNGs.
    ///
    /// Doing this by hand in the inspector is four drag-and-drops and one number typed four
    /// times, and the number is the one thing in the whole test that silently scales every
    /// result. A marker declared as 20 cm and printed at 18 cm makes the overlay ten percent
    /// too large everywhere, and nothing on screen says so — the alignment still looks clean,
    /// the sample spread is still small, and every distance is wrong by a tenth.
    ///
    /// So the size is entered once, here, with the reason attached.
    /// </summary>
    public class MarkerLibraryWindow : EditorWindow
    {
        const string LibraryPath = "Assets/_Game/AR/FriLensMarkers.asset";
        const string MarkerFolder = "Assets/_Game/AR/Markers";

        /// <summary>
        /// Only files starting with this are markers. The same folder holds the diagrams that
        /// explain how to measure and hang them, and those are textures too — handed to ARCore
        /// they would be tracked as markers, which is both wrong and slower.
        /// </summary>
        const string MarkerPrefix = "frilens-";

        /// <summary>
        /// Side of the printed marker in metres — the outer hairline boundary, which is the edge
        /// of the whole image file, caption strip included.
        ///
        /// Defaulted to the size the print PDFs are made at and measured at on paper. It is still
        /// a field rather than a constant because the number that counts is the one on the wall,
        /// not the one sent to the printer.
        /// </summary>
        float m_PrintedSizeMeters = 0.18f;

        string m_Report = "";

        [MenuItem("FriLens/Marker Library")]
        static void Open() => GetWindow<MarkerLibraryWindow>(true, "FriLens marker library");

        void OnGUI()
        {
            EditorGUILayout.LabelField("Reference image library", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Measure the printed marker with a ruler and enter that, not the size you sent "
                + "to the printer. Printers scale. An error of 5 % in this number is an error of "
                + "5 % in the scale of the whole overlay, and nothing on screen will show it.\n\n"
                + "Measure the thin OUTER boundary, the one that runs around the caption too. "
                + "That line is the edge of the image file, and the size given here is the size "
                + "of the whole file. The thick frame around the pattern is 9 % smaller, and "
                + "entering that instead scales the entire overlay by 9 %.",
                MessageType.Warning);

            m_PrintedSizeMeters = EditorGUILayout.FloatField(
                new GUIContent("Printed size (m)", "Side of the black frame as printed."),
                m_PrintedSizeMeters);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(m_PrintedSizeMeters <= 0f))
            {
                if (GUILayout.Button("Rebuild library from " + MarkerFolder))
                    m_Report = Rebuild(m_PrintedSizeMeters);
            }

            if (!string.IsNullOrEmpty(m_Report))
            {
                EditorGUILayout.Space();
                EditorGUILayout.TextArea(m_Report, GUILayout.ExpandHeight(true));
            }
        }

        /// <summary>
        /// Rebuilds the library from the marker PNGs at the given printed size. Public so it can
        /// be driven without the window, which is what a build script or a check would do.
        /// </summary>
        public static string Rebuild(float sizeMeters)
        {
            var report = new StringBuilder();

            var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
            if (library == null)
                return $"No library at {LibraryPath}.";

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { MarkerFolder });
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileName(p).StartsWith(MarkerPrefix))
                .OrderBy(p => p)
                .ToList();

            if (paths.Count == 0)
                return $"No {MarkerPrefix}* textures in {MarkerFolder}.";

            // Reference images have to be readable, and Unity imports PNGs without that. The
            // library would build anyway and then fail at runtime with nothing to point at.
            foreach (var path in paths)
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer || importer.isReadable)
                    continue;

                importer.isReadable = true;
                importer.SaveAndReimport();
                report.AppendLine($"made readable: {Path.GetFileName(path)}");
            }

            for (var i = library.count - 1; i >= 0; i--)
                library.RemoveAt(i);

            foreach (var path in paths)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var index = library.count;

                library.Add();

                // keepTexture: false. The texture is only needed to build the image database at
                // build time; carrying it into the player would add megabytes of pattern nobody
                // ever draws.
                library.SetTexture(index, texture, false);
                library.SetName(index, Path.GetFileNameWithoutExtension(path));
                library.SetSpecifySize(index, true);
                library.SetSize(index, new Vector2(sizeMeters, sizeMeters));

                report.AppendLine($"added: {Path.GetFileNameWithoutExtension(path)}  "
                    + $"{sizeMeters * 100f:F1} x {sizeMeters * 100f:F1} cm");
            }

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();

            report.AppendLine();
            report.AppendLine($"{library.count} images, each {sizeMeters * 100f:F1} cm.");
            report.AppendLine("Select the library asset to see the quality score ARCore gives "
                + "each image. Below about 75 the marker is worth regenerating.");

            Debug.Log(report.ToString());
            return report.ToString();
        }
    }
}
