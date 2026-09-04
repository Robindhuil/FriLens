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
            var navCollider = EnsureNavCollider(report);

            Set(probe, "m_Camera", arCamera.transform);
            Set(probe, "m_AnchorManager", anchors);
            Set(probe, "m_Material", material);
            Set(probe, "m_NavCollider", navCollider);
            Set(hud, "m_FloorProbe", probe);

            // The log reads these directly rather than through the HUD, so it keeps recording
            // them even if the HUD fails to build.
            var logger = Object.FindFirstObjectByType<SessionLogger>(FindObjectsInactive.Include);
            var anchoredRoot = Object.FindFirstObjectByType<AnchoredRoot>(FindObjectsInactive.Include);
            if (logger != null)
            {
                Set(logger, "m_FloorProbe", probe);
                Set(logger, "m_AnchoredRoot", anchoredRoot);
            }

            // A default in the source only reaches components that did not exist yet; one already
            // in the scene keeps whatever was serialised, which is how the first shipped guess of
            // 1.70 m outlived being corrected. It is rewritten here only when it still holds that
            // old guess, so a value somebody has since tuned by hand is left alone.
            var eye = new SerializedObject(probe).FindProperty("m_EyeHeightMeters");
            if (eye != null && Mathf.Approximately(eye.floatValue, 1.70f))
            {
                SetFloat(probe, "m_EyeHeightMeters", 1.25f);
                report.AppendLine("probe height 1.70 -> 1.25 m (measured at the moment of a drop)");
            }

            report.AppendLine("wired: probe(camera, anchors, material, nav collider), "
                + "hud.m_FloorProbe, logger.m_FloorProbe, logger.m_AnchoredRoot");

            var scene = hud.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.AppendLine("scene saved: " + scene.path);

            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Puts a collider on the navigation overlay so a disc can be dropped onto it.
        ///
        /// Nothing in the app does physics, and this collider is never used for collision — it
        /// exists so one ray a session can ask the model where its floor is. That is why it goes
        /// on quietly here rather than being something to remember in the inspector.
        /// </summary>
        static Collider EnsureNavCollider(StringBuilder report)
        {
            var overlay = GameObject.Find("NavOverlay");
            if (overlay == null)
            {
                report.AppendLine("no NavOverlay in the scene; probes will use the height instead.");
                return null;
            }

            var collider = overlay.GetComponent<MeshCollider>();
            if (collider != null)
                return collider;

            collider = Undo.AddComponent<MeshCollider>(overlay);
            report.AppendLine("added MeshCollider to NavOverlay");
            return collider;
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

        static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"FriLens: {target.GetType().Name} has no field '{field}'.");
                return;
            }

            property.floatValue = value;
            so.ApplyModifiedProperties();
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
