using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BouncyUFO.EditorTools
{
    /// <summary>
    /// 메뉴 [Bouncy UFO > Build Game Scene] 한 번으로 게임 장면을 새로 만든다.
    /// 장면 파일을 손으로 고치지 않고 코드로 다시 만들 수 있게 해 두었다.
    /// </summary>
    public static class GameSceneBuilder
    {
        private const string ScenePath = "Assets/Game/Scenes/Game.unity";
        private const string ConfigPath = "Assets/Game/Data/GameConfig.asset";
        private const string UfoSheetPath = "Assets/Art/Temp/Enemy.png";
        private const string EarthSheetPath = "Assets/Art/Temp/Earth-Star.png";
        private const string SpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        [MenuItem("Bouncy UFO/Build Game Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (System.IO.File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Bouncy UFO", "Game 장면을 새로 만들까요?\n기존 Game.unity는 덮어씁니다.", "새로 만들기", "취소"))
                return;

            GameConfig config = LoadOrCreateConfig();
            PlayArea.Init(config);
            Material spriteMat = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1) 설정 루트
            var root = new GameObject("GameRoot").AddComponent<GameRoot>();
            SetField(root, "config", config);

            // 2) 카메라: 플레이 영역 전체가 보이게(정수 배율 도트)
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.03f); // 여백(판정 없음) 색
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = config.pixelsPerUnit;
            ppc.refResolutionX = config.playAreaPixels.x;
            ppc.refResolutionY = config.playAreaPixels.y;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.None;
            camGo.AddComponent<PlayAreaCamera>();

            // 3) 플레이 영역 배경 (여백과 구분되는 남색)
            Sprite bgSprite = FindSprite(EarthSheetPath, "Earth-Star_9");
            var bg = CreateSprite("PlayAreaBackground", bgSprite, spriteMat, -100);
            if (bgSprite != null)
            {
                Vector2 s = bgSprite.bounds.size;
                bg.transform.localScale = new Vector3(PlayArea.Size.x / s.x, PlayArea.Size.y / s.y, 1f);
            }

            // 4) 지구 (임시 그림, 아래쪽 가운데)
            var earth = CreateSprite("Earth", FindSprite(EarthSheetPath, "Earth-Star_0"), spriteMat, -50);
            earth.transform.position = new Vector3(0f, PlayArea.Bottom, 0f);

            // 5) UFO (입력 + 조작 + 통통 연출)
            var ufo = new GameObject("UFO");
            var input = ufo.AddComponent<TapInput>();
            var controller = ufo.AddComponent<UfoController>();
            var visual = CreateSprite("Visual", FindSprite(UfoSheetPath, "Enemy_0"), spriteMat, 10);
            visual.transform.SetParent(ufo.transform, false);
            var squash = visual.AddComponent<SquashStretch>();
            SetField(controller, "input", input);
            SetField(controller, "visual", squash);
            ufo.transform.position = new Vector3(0f, PlayArea.Bottom + config.PixelsToUnits(config.ufoHeightFromBottomPx), 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("[Bouncy UFO] Game 장면을 만들었습니다: " + ScenePath);
        }

        private static GameConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static Sprite FindSprite(string sheetPath, string name)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().FirstOrDefault(s => s.name == name);
            if (sprite == null) Debug.LogWarning($"[Bouncy UFO] 스프라이트를 찾지 못했습니다: {sheetPath} / {name}");
            return sprite;
        }

        private static GameObject CreateSprite(string name, Sprite sprite, Material mat, int order)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = order;
            return go;
        }

        private static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSceneToBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
