#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OrpheusBelow.EditorTools
{
    public static class OrpheusEditorMenu
    {
        private const string SceneFolder = "Assets/OrpheusBelow/Scenes";
        private const string ScenePath = "Assets/OrpheusBelow/Scenes/OrpheusPrototype.unity";

        [MenuItem("Orpheus Below/Build & Open Base Prototype Scene", false, 1)]
        public static void BuildAndSavePrototypeScene()
        {
            if (!Directory.Exists(SceneFolder))
            {
                Directory.CreateDirectory(SceneFolder);
                AssetDatabase.Refresh();
            }

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            RenderPipeline2DSetup.ConfigureCameraForIsometric25D(cam, new Color(0.038f, 0.020f, 0.056f, 1f));

            GameObject worldRoot = OrpheusPrototypeBuilder.BuildPrototypeWorld();

            // Frame Scene View in 2.5D Isometric Orthographic view on Chamber I!
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView sv = SceneView.lastActiveSceneView;
                sv.in2DMode = false;
                sv.orthographic = true;
                sv.LookAt(Vector3.zero, RenderPipeline2DSetup.IsoCameraRotation, 13f, true);
            }

            EditorSceneManager.SaveScene(newScene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            Selection.activeGameObject = worldRoot;

            Debug.Log("[Orpheus Below] Built & saved 2.5D Isometric Hades Chamber-Crawler Scene at " + ScenePath + " — Press PLAY!");
        }

        [MenuItem("Orpheus Below/Rebuild Chambers in Current Scene", false, 2)]
        public static void RebuildInCurrentScene()
        {
            GameObject root = OrpheusPrototypeBuilder.BuildPrototypeWorld();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("[Orpheus Below] Rebuilt 2.5D Isometric Chambers in active scene.");
        }

        private static void AddSceneToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene s in scenes)
            {
                if (s.path == path) return;
            }
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
