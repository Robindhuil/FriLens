using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FriLens.EditorTools
{
    /// <summary>
    /// FriLens &gt; Extract Nav Meshes.
    ///
    /// navmesh.blend holds 310 hand-drawn navigation polygons for the whole faculty next to
    /// 33 terrain and road objects that carry 92% of the triangles and nothing this project
    /// needs. This window picks the polygons of one floor by name prefix and welds them into
    /// a single mesh asset.
    ///
    /// Two things are easy to get wrong here:
    ///
    /// The importer rotates the model root by 270 degrees around X to turn Blender's Z-up
    /// into Unity's Y-up. Combining through localToWorldMatrix bakes that rotation into the
    /// vertices. Skip it and the overlay lands on its side in AR with nothing on screen to
    /// explain why.
    ///
    /// Vertices stay in model coordinates and the mesh is not re-centred. Alignment moves the
    /// whole overlay at runtime and the marker pose lives in the same frame, so shifting the
    /// mesh to the origin here would only have to be undone later.
    ///
    /// Floors are selected by name, never by height: they overlap in Y because a staircase
    /// belongs to the floor below and the floor above at the same time.
    /// </summary>
    public class NavMeshExtractorWindow : EditorWindow
    {
        const string NavToken = "_nav_";
        const string DefaultModelPath = "Assets/Models/navmesh.blend";
        const string DefaultOutputFolder = "Assets/_Game/Generated/Nav";

        /// <summary>
        /// The nine indoor floors. Basement, terraces and the outdoor areas are left out on
        /// purpose: they are not part of this test and each would need its own decision about
        /// what counts as one surface.
        /// </summary>
        static readonly string[] FloorPrefixes =
        {
            "ra0", "ra1", "ra2", "ra3",
            "rb0", "rb1", "rb2", "rb3",
            "rc0"
        };

        GameObject m_Model;
        string m_Prefix = "ra0";
        string m_OutputFolder = DefaultOutputFolder;
        Vector2 m_Scroll;
        string m_Report = "";

        [MenuItem("FriLens/Extract Nav Meshes")]
        static void Open()
        {
            var window = GetWindow<NavMeshExtractorWindow>(false, "Nav Mesh Extractor");
            window.minSize = new Vector2(420, 320);
            window.Show();
        }

        /// <summary>
        /// Rebuilds every floor from the default model in one go. Each floor is a couple of
        /// thousand triangles, so extracting all nine costs nothing and keeps the generated
        /// assets from drifting apart when the source model changes.
        /// </summary>
        [MenuItem("FriLens/Extract Nav Meshes (all floors)")]
        static void ExtractAllFloors()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultModelPath);
            if (model == null)
            {
                Debug.LogError("Extract Nav Meshes: nothing at " + DefaultModelPath
                    + ". The Blender source is not versioned; see docs/decisions/002-verzovanie-modelov.md.");
                return;
            }

            var report = new StringBuilder();
            foreach (var prefix in FloorPrefixes)
                report.AppendLine(Extract(model, prefix, DefaultOutputFolder));

            Debug.Log(report.ToString());
        }

        void OnEnable()
        {
            if (m_Model == null)
                m_Model = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultModelPath);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            m_Model = (GameObject)EditorGUILayout.ObjectField("Model", m_Model, typeof(GameObject), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            m_Prefix = EditorGUILayout.TextField(new GUIContent("Name prefix",
                "Objects whose name starts with this and contains \"_nav_\". "
                + "Examples: ra0, rb2, rc0, rb_basement, outside, terrace."), m_Prefix);
            m_OutputFolder = EditorGUILayout.TextField("Output folder", m_OutputFolder);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(m_Model == null))
            {
                if (GUILayout.Button("List groups"))
                    m_Report = ListGroups(m_Model);

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(m_Prefix)))
                {
                    if (GUILayout.Button("Extract"))
                        m_Report = Extract(m_Model, m_Prefix.Trim(), m_OutputFolder.Trim());
                }
            }

            if (m_Model == null)
                EditorGUILayout.HelpBox("Assign the imported navmesh model. Default is " + DefaultModelPath
                    + ", which is kept out of the repository; see docs/decisions/002-verzovanie-modelov.md.",
                    MessageType.Info);

            EditorGUILayout.Space();
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            EditorGUILayout.TextArea(m_Report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Groups every navigation object by building and floor so the prefix to extract can be
        /// read off instead of guessed. Most names follow r&lt;building&gt;&lt;floor&gt;&lt;room&gt;_&lt;label&gt;_nav_&lt;n&gt;,
        /// but a few (rb_basement, terrace, outside) do not, so those fall back to whatever
        /// stands before the first underscore.
        /// </summary>
        static string ListGroups(GameObject model)
        {
            var groups = new Dictionary<string, List<MeshFilter>>();

            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var name = filter.gameObject.name;
                if (!name.Contains(NavToken))
                    continue;

                var head = name.Substring(0, name.IndexOf('_'));
                var key = head.Length == 5 && head[0] == 'r' ? head.Substring(0, 3) : head;

                if (!groups.TryGetValue(key, out var list))
                    groups[key] = list = new List<MeshFilter>();
                list.Add(filter);
            }

            var report = new StringBuilder();
            report.AppendLine("prefix   meshes    tris   Y range");
            foreach (var pair in groups.OrderBy(p => p.Key))
            {
                int triangles = pair.Value.Sum(f => f.sharedMesh == null ? 0 : f.sharedMesh.triangles.Length / 3);
                float minY = pair.Value.Min(f => f.GetComponent<Renderer>().bounds.min.y);
                float maxY = pair.Value.Max(f => f.GetComponent<Renderer>().bounds.max.y);
                report.AppendLine(string.Format("{0,-8} {1,6} {2,7}   {3,6:F2} .. {4,6:F2}",
                    pair.Key, pair.Value.Count, triangles, minY, maxY));
            }

            return report.ToString();
        }

        static string Extract(GameObject model, string prefix, string outputFolder)
        {
            var parts = new List<CombineInstance>();
            int sources = 0;
            long vertexBudget = 0;

            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var name = filter.gameObject.name;
                if (!name.Contains(NavToken) || !name.StartsWith(prefix))
                    continue;

                var mesh = filter.sharedMesh;
                if (mesh == null)
                    continue;

                sources++;
                vertexBudget += mesh.vertexCount;

                // localToWorldMatrix walks up to the model root, so the importer's 270 degree
                // X rotation is folded in along with every child transform.
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    parts.Add(new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = sub,
                        transform = filter.transform.localToWorldMatrix
                    });
                }
            }

            if (sources == 0)
                return "Nothing matched prefix \"" + prefix + "\". Run List groups to see what exists.";

            var combined = new Mesh
            {
                name = prefix + "_nav",
                indexFormat = vertexBudget > 60000 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            combined.CombineMeshes(parts.ToArray(), true, true);
            combined.RecalculateBounds();
            combined.Optimize();

            EnsureFolder(outputFolder);
            var assetPath = outputFolder + "/" + prefix + "_nav.asset";
            AssetDatabase.CreateAsset(combined, assetPath);
            AssetDatabase.SaveAssets();

            var bounds = combined.bounds;
            var report = new StringBuilder();
            report.AppendLine("Wrote " + assetPath);
            report.AppendLine("  source objects : " + sources);
            report.AppendLine("  vertices       : " + combined.vertexCount);
            report.AppendLine("  triangles      : " + combined.triangles.Length / 3);
            report.AppendLine("  index format   : " + combined.indexFormat);
            report.AppendLine(string.Format("  size   (m)     : {0:F2} x {1:F2} x {2:F2}",
                bounds.size.x, bounds.size.y, bounds.size.z));
            report.AppendLine(string.Format("  centre (model) : {0:F2}, {1:F2}, {2:F2}",
                bounds.center.x, bounds.center.y, bounds.center.z));
            report.AppendLine(string.Format("  floor Y        : {0:F2} .. {1:F2}", bounds.min.y, bounds.max.y));
            report.AppendLine();
            report.AppendLine("A size Y of several metres means stairs came along, which is expected on a");
            report.AppendLine("full floor. A size Y near zero on a whole floor means the import rotation was");
            report.AppendLine("lost and the overlay would be standing on edge.");

            return report.ToString();
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var segments = folder.Split('/');
            var built = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                var next = built + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(built, segments[i]);
                built = next;
            }
        }
    }
}
