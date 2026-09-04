using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FriLens.EditorTools
{
    /// <summary>
    /// Adds the components a new feature needs and fills in their references.
    ///
    /// Not glamorous, but the alternative is a list of "drag this onto that" in a document, and
    /// a scene wired by hand from a document is wired differently every time. A missed reference
    /// here is a null field that does nothing on the phone and says nothing about why.
    ///
    /// Safe to run repeatedly: it adds what is missing and re-points what is already there.
    /// </summary>
    public static class SceneWiring
    {
        const string ProbeMaterialPath = "Assets/_Game/Materials/FloorProbe.mat";

        [MenuItem("FriLens/Wire Scene")]
        public static void Wire()
        {
            var report = new StringBuilder();

            var hud = Object.FindFirstObjectByType<DiagnosticsHud>(FindObjectsInactive.Include);
            if (hud == null)
            {
                Debug.LogError("FriLens: no DiagnosticsHud in the open scene. Open FriLensTest.");
                return;
            }

            var travel = Object.FindFirstObjectByType<CameraTravel>(FindObjectsInactive.Include);
            var anchors = Object.FindFirstObjectByType<ARAnchorManager>(FindObjectsInactive.Include);
            var arCamera = Object.FindFirstObjectByType<ARCameraManager>(FindObjectsInactive.Include);

            if (travel == null || arCamera == null)
            {
                Debug.LogError("FriLens: scene is missing CameraTravel or the AR camera.");
                return;
            }

            var probe = Object.FindFirstObjectByType<FloorProbe>(FindObjectsInactive.Include);
            if (probe == null)
            {
                probe = Undo.AddComponent<FloorProbe>(travel.gameObject);
                report.AppendLine($"added FloorProbe to {travel.gameObject.name}");
            }

            var material = LoadOrCreateProbeMaterial(report);

            Set(probe, "m_Camera", arCamera.transform);
            Set(probe, "m_AnchorManager", anchors);
            Set(probe, "m_Material", material);
            Set(hud, "m_FloorProbe", probe);

            report.AppendLine("wired: probe(camera, anchors, material), hud.m_FloorProbe");

            var scene = hud.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.AppendLine("scene saved: " + scene.path);

            Debug.Log(report.ToString());
        }

        /// <summary>
        /// The discs need to read as instrument marks, not as scenery — unlit, saturated, and
        /// nothing like the overlay's colour, so a disc and the overlay are never confused for
        /// each other in a photograph taken afterwards.
        /// </summary>
        static Material LoadOrCreateProbeMaterial(StringBuilder report)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(ProbeMaterialPath);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                report.AppendLine("URP Unlit shader not found; probe material not created.");
                return null;
            }

            var material = new Material(shader) { name = "FloorProbe" };
            material.SetColor("_BaseColor", new Color(0.06f, 0.72f, 0.51f, 1f));

            AssetDatabase.CreateAsset(material, ProbeMaterialPath);
            AssetDatabase.SaveAssets();
            report.AppendLine("created " + ProbeMaterialPath);
            return material;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"FriLens: {target.GetType().Name} has no field '{field}'.");
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
