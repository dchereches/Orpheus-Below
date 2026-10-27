using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrpheusBelow.EditorTools
{
    public static class PaintedChamberSceneBuilder
    {
        [MenuItem("Orpheus Below/Build Eight Painted Chamber Scenes", false, 3)]
        public static void BuildScenes()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/OrpheusBelow/Resources/ChamberLayouts");
            Directory.CreateDirectory("Assets/OrpheusBelow/Scenes");
            AssetDatabase.Refresh();
            for (int i = 0; i < UnderworldJourney.ChamberCount; i++)
                AssetDatabase.ImportAsset("Assets/OrpheusBelow/Resources/PaintedChambers/Chamber_" + i.ToString("D2") + ".jpeg", ImportAssetOptions.ForceUpdate);
            var paths = new List<string>();
            for (int i = 0; i < UnderworldJourney.ChamberCount; i++)
            {
                string path = "Assets/OrpheusBelow/Resources/ChamberLayouts/Chamber_" + i.ToString("D2") + ".asset";
                var layout = AssetDatabase.LoadAssetAtPath<PaintedChamberDefinition>(path);
                if (layout == null)
                {
                    layout = ScriptableObject.CreateInstance<PaintedChamberDefinition>();
                    AssetDatabase.CreateAsset(layout, path);
                }
                else
                {
                    // Only generated geometry embedded in this layout is replaced on an explicit rebuild.
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                        if (asset is Mesh || asset is Material || (asset is Sprite && asset.name.EndsWith("_Occlusion")) ||
                            (asset is Texture2D && asset.name.EndsWith("_OcclusionMask")))
                            Object.DestroyImmediate(asset, true);
                }
                DescentChamberCatalog.Author(layout, i);
                EditorUtility.SetDirty(layout);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var map = PaintedChamberMap.Build(null, layout, Vector3.zero);
                var entryObject = new GameObject("PaintedChamberSceneEntry");
                var entry = entryObject.AddComponent<PaintedChamberScene>();
                entry.chamberIndex = i; entry.map = map;
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                camera.orthographic = true; camera.orthographicSize = layout.zoom;
                camera.nearClipPlane = .1f; camera.farClipPlane = 160f;
                camera.transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
                Vector3 focus = map.ClampCameraFocus(map.ToWorld(layout.encounterCenter), layout.zoom, 16f / 9f);
                camera.transform.position = focus - camera.transform.forward * 36f;
                var saved = new HashSet<Object>();
                foreach (var filter in map.GetComponentsInChildren<MeshFilter>(true)) Save(filter.sharedMesh, layout, saved);
                foreach (var collider in map.GetComponentsInChildren<MeshCollider>(true)) Save(collider.sharedMesh, layout, saved);
                foreach (var renderer in map.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials) Save(material, layout, saved);
                foreach (var renderer in map.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.sprite == null) continue;
                    Save(renderer.sprite.texture, layout, saved);
                    Save(renderer.sprite, layout, saved);
                }
                map.MarkResourcesPersistent();
                string scenePath = PaintedChamberScene.ScenePath(i);
                EditorSceneManager.SaveScene(scene, scenePath);
                paths.Add(scenePath);
                Debug.Log("[Painted scenes] Authored " + layout.title + " / " + layout.cover.Length + " cover footprints / exit " + layout.exit);
            }
            AssetDatabase.SaveAssets();
            var builds = new List<EditorBuildSettingsScene>();
            foreach (string path in paths) builds.Add(new EditorBuildSettingsScene(path, true));
            foreach (var existing in EditorBuildSettings.scenes)
                if (!paths.Contains(existing.path)) builds.Add(existing);
            EditorBuildSettings.scenes = builds.ToArray();
            EditorSceneManager.OpenScene(paths[0]);
            Debug.Log("[Painted scenes] BUILD PASS: eight authored scenes and layouts saved; first scene is the build entry.");
        }

        private static void Save(Object asset, Object owner, HashSet<Object> saved)
        {
            if (asset == null || AssetDatabase.Contains(asset) || !saved.Add(asset)) return;
            AssetDatabase.AddObjectToAsset(asset, owner);
        }

        public static void UpdateStairZones()
        {
            for (int i = 0; i < UnderworldJourney.ChamberCount; i++)
            {
                var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + i.ToString("D2"));
                if (layout == null) continue;
                DescentChamberCatalog.AuthorStairs(layout, i);
                EditorUtility.SetDirty(layout);
            }
        }
    }
}
