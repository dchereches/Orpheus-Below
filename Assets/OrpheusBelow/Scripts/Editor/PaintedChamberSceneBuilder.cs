using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrpheusBelow.EditorTools
{
    public static class PaintedChamberSceneBuilder
    {
        private static Vector2[] P(params float[] xy)
        {
            var points = new Vector2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return points;
        }

        private static PaintedChamberDefinition.Cover Cover(string name, Vector2[] foot, Vector2[] outline, float height = 4f)
            => new PaintedChamberDefinition.Cover { name = name, footprint = foot, silhouette = outline, height = height };

        private static PaintedChamberDefinition.Cover Pillar(string name, float x, float y, float width, float top)
            => Cover(name, P(x - width, y - width * .35f, x, y - width * .7f, x + width, y - width * .35f, x, y + width * .2f),
                P(x - width * .7f, top, x + width * .7f, top, x + width * .6f, y - width,
                    x + width, y - width * .7f, x + width, y - width * .3f, x, y + width * .2f,
                    x - width, y - width * .3f, x - width, y - width * .7f, x - width * .6f, y - width));

        [MenuItem("Orpheus Below/Build Five Painted Chamber Scenes", false, 3)]
        public static void BuildScenes()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/OrpheusBelow/Resources/ChamberLayouts");
            Directory.CreateDirectory("Assets/OrpheusBelow/Scenes");
            AssetDatabase.Refresh();
            for (int i = 0; i < 5; i++)
                AssetDatabase.ImportAsset("Assets/OrpheusBelow/Resources/PaintedChambers/Chamber_" + i.ToString("D2") + ".jpeg", ImportAssetOptions.ForceUpdate);
            var paths = new List<string>();
            for (int i = 0; i < 5; i++)
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
                Author(layout, i);
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
            Debug.Log("[Painted scenes] BUILD PASS: five authored scenes and layouts saved; first scene is the build entry.");
        }

        private static void Save(Object asset, Object owner, HashSet<Object> saved)
        {
            if (asset == null || AssetDatabase.Contains(asset) || !saved.Add(asset)) return;
            AssetDatabase.AddObjectToAsset(asset, owner);
        }

        private static void Author(PaintedChamberDefinition d, int i)
        {
            d.index = i;
            d.name = "Chamber_" + i.ToString("D2");
            d.painting = Resources.Load<Texture2D>("PaintedChambers/Chamber_" + i.ToString("D2"));
            if (d.painting == null) throw new FileNotFoundException("Missing latest chamber image " + i);
            d.imageWidth = 72f; d.imageHeight = 48f; d.zoom = 11f;
            d.hasGuardian = i != 2;
            d.guardian = i == 0 ? BossIdentity.CantorOfFury : i == 1 ? BossIdentity.OarsmanOfStyx
                : i == 3 ? BossIdentity.SirenOfAcheron : BossIdentity.CerberusHoundOfHades;
            switch (i)
            {
                case 0:
                    d.title = "Asphodel Viaduct";
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 12.23.29 (2).jpeg";
                    d.walkableBoundary = P(42,752,205,680,1120,367,1300,310,1402,235,1480,276,1458,355,1190,480,
                        1010,545,682,688,428,815,361,859,268,820,248,908,145,891,45,828);
                    d.entrance = new Vector2(301,775); d.exit = new Vector2(1410,306);
                    d.well = new Vector2(345,756); d.encounterCenter = new Vector2(780,565);
                    d.enemySpawns = P(550,657,715,585,865,535,1005,480,1145,423,645,622,940,510);
                    d.cover = new[] {
                        Pillar("WesternColumn",139,711,48,438), Pillar("LowWesternPier",400,616,38,545),
                        Pillar("RearBridgeColumn",718,518,38,353),
                        Cover("BridgeStatue",P(940,394,978,378,1017,394,976,417),
                            P(936,340,944,330,946,285,957,262,955,236,968,204,978,194,987,202,988,245,997,284,1002,327,1018,340,1017,397,979,419,940,398)),
                        Pillar("NearBridgeColumn",605,894,53,746), Pillar("NearEasternPier",986,580,37,531),
                        Pillar("ExitStatuePlinth",1435,334,30,230), Pillar("OuterFrontColumn",720,981,48,787)
                    };
                    break;
                case 1:
                    d.title = "Court of the Unremembered";
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 12.23.29 (1).jpeg";
                    d.walkableBoundary = P(100,900,248,749,390,682,472,581,426,550,545,507,770,476,958,477,
                        1060,507,1140,540,1178,465,1248,466,1257,543,1376,659,1470,880,1366,1000,
                        1150,1000,967,948,870,862,721,864,565,971,344,995,189,941);
                    d.entrance = new Vector2(439,892); d.exit = new Vector2(1213,504);
                    d.well = new Vector2(535,877); d.encounterCenter = new Vector2(824,648);
                    d.enemySpawns = P(665,602,840,555,1010,602,1170,670,1020,718,760,755,580,737);
                    d.cover = new[] {
                        Pillar("WesternGreatColumn",111,689,52,331), Pillar("WestCourtPillar",225,688,36,510),
                        Cover("FallenWesternMasonry",P(378,757,432,730,514,777,484,827,405,797),P(371,710,421,687,516,751,520,781,484,827,402,799,373,775),1.8f),
                        Pillar("CenterBrokenColumn",806,826,51,736),
                        Cover("LowEasternRubble",P(964,824,1030,792,1092,824,1036,850),P(959,797,1004,770,1040,777,1092,816,1082,839,1036,850,964,825),1.5f),
                        Pillar("ForegroundLeftPier",1100,1000,57,838), Pillar("ForegroundRightColumn",1272,935,60,672),
                        Pillar("PortalLeftColumn",1087,534,38,326), Pillar("PortalRightPier",1282,564,34,433)
                    };
                    break;
                case 2:
                    d.title = "The Burial Terraces";
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 12.23.29.jpeg";
                    d.walkableBoundary = P(260,960,355,836,324,743,392,647,373,572,305,511,404,409,513,345,
                        640,258,690,130,760,75,862,79,930,158,1104,188,1215,264,1350,341,1320,437,
                        1267,512,1304,565,1190,627,1095,682,989,736,974,845,807,944,607,1000,435,991);
                    d.entrance = new Vector2(673,905); d.exit = new Vector2(802,118);
                    d.well = new Vector2(755,868); d.encounterCenter = new Vector2(815,493);
                    d.enemySpawns = P(545,694,734,594,977,515,1118,392,798,306,609,426,935,667);
                    d.cover = new[] {
                        Cover("WesternBurialHouse",P(280,744,344,713,425,772,382,826,307,817),
                            P(91,526,189,449,329,520,403,638,425,772,382,826,307,817,173,750,148,656,91,611),5f),
                        Pillar("WesternTerracePillar",450,633,35,439),
                        Pillar("FrontTerraceColumn",1010,837,47,665),
                        Cover("EasternBurialHouse",P(1267,881,1339,850,1448,908,1389,969,1292,939),
                            P(1183,714,1293,641,1422,707,1503,828,1463,936,1389,969,1292,939,1220,855),5f),
                        Pillar("RearFuneraryColumn",918,128,28,29),
                        Cover("RaisedTerraceTomb",P(1170,340,1232,313,1302,339,1257,382),P(1155,240,1250,181,1355,222,1350,310,1302,339,1257,382,1170,340),3f),
                        Pillar("EasternBrokenPier",1353,606,31,478), Pillar("WesternGateColumn",264,335,33,201)
                    };
                    break;
                case 3:
                    d.title = "Vault of the Painted Vessels";
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 12.23.28 (1).jpeg";
                    d.walkableBoundary = P(208,402,334,391,504,370,628,345,776,373,896,411,1100,413,1190,380,
                        1220,312,1300,290,1365,360,1363,451,1363,559,1468,618,1444,698,1284,746,
                        1100,833,968,896,827,932,713,914,569,977,483,951,374,917,288,854,226,775,214,681,160,634,141,525);
                    d.entrance = new Vector2(773,875); d.exit = new Vector2(1286,349);
                    d.well = new Vector2(854,840); d.encounterCenter = new Vector2(880,577);
                    d.enemySpawns = P(751,679,942,631,1114,518,997,440,633,438,350,483,1196,628);
                    d.cover = new[] {
                        Cover("GreatAmphora",P(621,335,706,312,795,343,743,382,665,371),
                            P(641,143,693,91,762,91,794,128,811,205,829,238,798,324,743,382,665,371,614,298,596,217),5f),
                        Cover("TiltedPaintedVessel",P(546,344,597,322,669,343,639,374,581,373),
                            P(540,211,582,185,625,183,668,215,692,276,668,343,639,374,581,373,547,331,530,269),3.8f),
                        Pillar("CentralColumn",786,439,35,321), Pillar("WesternBalustradePillar",350,652,36,451),
                        Cover("CentralBrokenDish",P(393,589,486,552,595,561,683,604,657,643,552,666,445,641),
                            P(393,557,481,516,580,512,663,540,699,581,683,604,657,643,552,666,445,641,388,606),1f),
                        Pillar("InnerEasternColumn",1084,659,33,532),
                        Cover("FrontFallenColumn",P(228,746,271,704,387,746,363,792,282,793),
                            P(230,690,258,649,337,669,389,732,387,746,363,792,282,793,222,755),2f),
                        Pillar("ForecourtColumn",952,823,37,733), Pillar("ExitRightPier",1364,431,35,301)
                    };
                    break;
                default:
                    d.title = "The Phlegethon Gate";
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 12.23.28.jpeg";
                    d.walkableBoundary = P(192,462,263,425,240,368,202,310,252,260,309,221,400,196,456,232,
                        474,305,573,370,748,374,900,423,1070,452,1135,477,1250,521,1260,600,
                        1170,653,1220,708,1370,765,1410,815,1330,875,1230,875,1110,820,1040,783,
                        981,745,880,716,758,705,674,690,578,640,450,608,350,562,260,530);
                    d.entrance = new Vector2(1262,802); d.exit = new Vector2(333,253);
                    d.well = new Vector2(1180,772); d.encounterCenter = new Vector2(747,539);
                    d.enemySpawns = P(585,527,760,450,909,519,984,601,798,643,517,451,1083,552);
                    d.cover = new[] {
                        Pillar("WesternArenaPillar",437,565,39,432), Pillar("RearArenaGreatColumn",705,337,45,37),
                        Cover("EasternFallenArch",P(929,458,1010,421,1128,466,1092,506,1005,505),
                            P(922,394,980,330,1044,329,1125,376,1166,452,1128,466,1092,506,1005,505,943,475),4f),
                        Pillar("FrontBrazierPlinth",551,676,45,550), Pillar("EastBrazierPlinth",1200,506,36,409),
                        Pillar("FrontStairColumn",924,786,42,649), Pillar("ExitRightBrazier",434,297,38,134),
                        Pillar("ExitLeftBrazier",232,375,39,259),
                        Cover("CenterRubble",P(644,462,677,448,718,466,691,488),P(645,442,665,424,695,432,718,466,691,488,644,462),1.4f)
                    };
                    break;
            }
        }
    }
}
