using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BouncyUFO.EditorTools
{
    /// <summary>
    /// 메뉴 [Bouncy UFO > Build All Scenes] 한 번으로 5개 씬(타이틀·로비·로딩·게임·결과)을 새로 만들고
    /// 빌드 목록에 순서대로 넣는다. 씬 파일을 손으로 고치지 않고 코드로 다시 만들 수 있게 해 두었다.
    /// </summary>
    public static class GameSceneBuilder
    {
        private const string SceneFolder = "Assets/Game/Scenes/";
        private const string ConfigPath = "Assets/Game/Data/GameConfig.asset";
        private const string UfoSheetPath = "Assets/Art/Temp/Enemy.png";
        private const string EarthSheetPath = "Assets/Art/Temp/Earth-Star.png";
        private const string ArtPath = "Assets/Art/Temp/";
        private const string DummyPath = "Assets/Art/Temp/Dummy/";
        private const string CardDataFolder = "Assets/Game/Data/Cards";
        private const string StringsPath = "Assets/Game/Data/Localization/strings.csv";
        private const string FontPath = "Assets/Art/Fonts/Galmuri/";
        private const string TextShaderPath = "Assets/TextMesh Pro/Shaders/TMP_Bitmap-Mobile.shader";
        private const string MonsterDataFolder = "Assets/Game/Data/Monsters";
        private const string FlashShaderPath = "Assets/Game/Shaders/SpriteFlash.shader";
        private const string FlashMaterialPath = "Assets/Game/Data/SpriteFlash.mat";
        private const string SpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        /// <summary>빌드 순서. 0번(타이틀)이 앱을 켜면 처음 나오는 씬.</summary>
        private static readonly string[] SceneOrder = { SceneFlow.Title, SceneFlow.Lobby, SceneFlow.Loading, SceneFlow.Game, SceneFlow.Result };

        private static Material spriteMat;

        [MenuItem("Bouncy UFO/Build All Scenes")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("Bouncy UFO",
                    "5개 씬(Title, Lobby, Loading, Game, Result)을 새로 만들까요?\n있던 씬 파일은 덮어씁니다.", "새로 만들기", "취소"))
                return;

            BuildScene(SceneFlow.Title, BuildTitle);
            BuildScene(SceneFlow.Lobby, BuildLobby);
            BuildScene(SceneFlow.Loading, BuildLoading);
            BuildScene(SceneFlow.Game, BuildGame);
            BuildScene(SceneFlow.Result, BuildResult);

            EditorBuildSettings.scenes = SceneOrder.Select(n => new EditorBuildSettingsScene(SceneFolder + n + ".unity", true)).ToArray();
            EditorSceneManager.OpenScene(SceneFolder + SceneFlow.Title + ".unity");
            Debug.Log("[Bouncy UFO] 5개 씬을 만들고 빌드 목록에 넣었습니다. Title 씬에서 Play 하세요.");
        }

        private static void BuildScene(string name, System.Action<GameConfig, TapInput> content)
        {
            GameConfig config = LoadOrCreateConfig();
            PlayArea.Init(config);
            spriteMat = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var input = CreateCommon(config);
            content(config, input);
            EditorSceneManager.SaveScene(scene, SceneFolder + name + ".unity");
        }

        /// <summary>모든 씬 공통: 설정·그림 모음, 카메라, 배경, 입력.</summary>
        private static TapInput CreateCommon(GameConfig config)
        {
            var rootGo = new GameObject("GameRoot");
            var root = rootGo.AddComponent<GameRoot>();
            SetField(root, "config", config);
            var assets = rootGo.AddComponent<GameAssets>();
            assets.sparkSprite = LoadSprite(ArtPath + "Spark.png");
            assets.pixelSprite = LoadSprite(ArtPath + "Pixel.png");
            assets.spriteMaterial = spriteMat;
            assets.flashMaterial = LoadOrCreateFlashMaterial();
            assets.monsters = LoadOrCreateMonsters();
            assets.boss = LoadOrCreateBoss();
            assets.meteor = LoadOrCreateMeteor();
            assets.coinSprite = LoadSprite(DummyPath + "Coin.png");
            assets.digitSprites = new Sprite[10];
            for (int i = 0; i < 10; i++) assets.digitSprites[i] = LoadSprite(DummyPath + "Digit" + i + ".png");
            assets.cards = LoadOrCreateCards();
            assets.strings = AssetDatabase.LoadAssetAtPath<TextAsset>(StringsPath);
            assets.titleFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath + "Galmuri11.ttf");
            assets.bodyFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath + "Galmuri9.ttf");
            assets.textShader = AssetDatabase.LoadAssetAtPath<Shader>(TextShaderPath);
            assets.fontLicense = AssetDatabase.LoadAssetAtPath<TextAsset>(FontPath + "Galmuri-OFL.txt");
            assets.ufoSprite = FindSprite(UfoSheetPath, "Enemy_0");
            assets.earthSprite = FindSprite(EarthSheetPath, "Earth-Star_0");
            assets.starSprite = FindSprite(EarthSheetPath, "Earth-Star_2");
            assets.pilotSuit = LoadSprite(DummyPath + "PilotSuit.png");
            assets.pilotFace = LoadSprite(DummyPath + "PilotFace.png");
            assets.pilotEyes = LoadSprite(DummyPath + "PilotEyes.png");
            assets.pilotHair = new[] { LoadSprite(DummyPath + "PilotHair0.png"), LoadSprite(DummyPath + "PilotHair1.png"), LoadSprite(DummyPath + "PilotHair2.png") };
            if (assets.titleFont == null || assets.bodyFont == null || assets.textShader == null)
                Debug.LogWarning("[Bouncy UFO] 폰트나 글자 셰이더를 찾지 못했습니다. Assets/Art/Fonts/Galmuri, " + TextShaderPath + " 확인");
            if (assets.strings == null) Debug.LogWarning("[Bouncy UFO] 번역표가 없습니다: " + StringsPath);

            // 카메라: 플레이 영역 전체가 보이게(정수 배율 도트)
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

            // 플레이 영역 배경 (여백과 구분되는 남색)
            Sprite bgSprite = FindSprite(EarthSheetPath, "Earth-Star_9");
            var bg = CreateSprite("PlayAreaBackground", bgSprite, spriteMat, SortOrder.Background);
            if (bgSprite != null)
            {
                Vector2 s = bgSprite.bounds.size;
                bg.transform.localScale = new Vector3(PlayArea.Size.x / s.x, PlayArea.Size.y / s.y, 1f);
            }

            // 입력(터치·마우스). 버튼과 UFO가 함께 듣는다
            return new GameObject("Input").AddComponent<TapInput>();
        }

        private static void BuildTitle(GameConfig config, TapInput input) => new GameObject("TitleScreen").AddComponent<TitleScreen>();
        private static void BuildLobby(GameConfig config, TapInput input) => new GameObject("LobbyScreen").AddComponent<LobbyScreen>();
        private static void BuildLoading(GameConfig config, TapInput input) => new GameObject("LoadingScreen").AddComponent<LoadingScreen>();
        private static void BuildResult(GameConfig config, TapInput input) => new GameObject("ResultScene").AddComponent<ResultScene>();

        private static void BuildGame(GameConfig config, TapInput input)
        {
            var session = GameObject.Find("GameRoot").AddComponent<GameSession>();
            new GameObject("Effects").AddComponent<Effects>();

            // 지구 (임시 그림, 아래쪽 가운데) + 체력 바
            var earthGo = CreateSprite("Earth", FindSprite(EarthSheetPath, "Earth-Star_0"), spriteMat, SortOrder.Earth);
            earthGo.transform.position = new Vector3(0f, PlayArea.Bottom, 0f);
            var earth = earthGo.AddComponent<Earth>();
            SetField(earth, "spriteRenderer", earthGo.GetComponent<SpriteRenderer>());
            SetField(earth, "visual", earthGo.AddComponent<SquashStretch>());
            SetField(session, "earth", earth);
            var bar = new GameObject("EarthHealthBar").AddComponent<EarthHealthBar>();
            SetField(bar, "earth", earth);

            // UFO (조준·돌진·몸통 박치기 + 통통 연출)
            var ufo = new GameObject("UFO");
            var controller = ufo.AddComponent<UfoController>();
            var visual = CreateSprite("Visual", FindSprite(UfoSheetPath, "Enemy_0"), spriteMat, SortOrder.Ufo);
            visual.transform.SetParent(ufo.transform, false);
            var squash = visual.AddComponent<SquashStretch>();
            SetField(controller, "input", input);
            SetField(controller, "visual", squash);
            ufo.transform.position = new Vector3(0f, PlayArea.Bottom + config.PixelsToUnits(config.ufoHeightFromBottomPx), 0f);

            // 몬스터 등장
            var spawner = new GameObject("MonsterSpawner").AddComponent<MonsterSpawner>();
            var warning = new GameObject("WarningFlash").AddComponent<WarningFlash>();
            var gold = new GameObject("GoldDrops").AddComponent<GoldDrops>();
            SetField(gold, "target", ufo.transform);
            var waves = new GameObject("WaveDirector").AddComponent<WaveDirector>();
            SetField(waves, "spawner", spawner);
            SetField(waves, "warning", warning);
            SetField(waves, "gold", gold);

            // 위쪽 표시: 골드·점수·웨이브, 보스 체력 바
            var hud = new GameObject("TopHud").AddComponent<TopHud>();
            SetField(hud, "waves", waves);
            var bossBar = new GameObject("BossHealthBar").AddComponent<BossHealthBar>();
            SetField(bossBar, "waves", waves);

            // 판 안 성장: 경험치·레벨업 카드
            var levels = new GameObject("LevelSystem").AddComponent<LevelSystem>();
            SetField(levels, "ufo", controller);
            SetField(levels, "ufoVisual", squash);
            var xpBar = new GameObject("XpBar").AddComponent<XpBar>();
            SetField(xpBar, "levels", levels);
            var cardUi = new GameObject("CardChoiceUI").AddComponent<CardChoiceUI>();
            SetField(cardUi, "input", input);
            SetField(cardUi, "levels", levels);

            // 간단 튜토리얼(처음 한 번), 일시정지
            var tutorial = new GameObject("TutorialHints").AddComponent<TutorialHints>();
            SetField(tutorial, "ufo", controller);
            SetField(tutorial, "waves", waves);
            new GameObject("PauseMenu").AddComponent<PauseMenu>();
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning("[Bouncy UFO] 스프라이트를 찾지 못했습니다: " + path);
            return sprite;
        }

        private static Material LoadOrCreateFlashMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath);
            if (mat != null) return mat;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(FlashShaderPath);
            if (shader == null) { Debug.LogError("[Bouncy UFO] 셰이더가 없습니다: " + FlashShaderPath); return null; }
            mat = new Material(shader) { name = "SpriteFlash" };
            AssetDatabase.CreateAsset(mat, FlashMaterialPath);
            return mat;
        }

        /// <summary>첫 빌드 몬스터 3종. 이미 있으면 그대로 쓴다(Inspector에서 고친 값 유지).</summary>
        private static MonsterDefinition[] LoadOrCreateMonsters()
        {
            if (!AssetDatabase.IsValidFolder(MonsterDataFolder))
                AssetDatabase.CreateFolder("Assets/Game/Data", "Monsters");

            var swarm = LoadOrCreateMonster("Swarm", d =>
            {
                d.nameKey = "monster.swarm"; d.sprite = FindSprite(UfoSheetPath, "Enemy_3");
                d.scale = 0.8f; d.hitRadiusPx = 7f;
                d.maxHp = 2; d.earthDamage = 3; d.speed = 0.09f; d.knockbackScale = 1f;
                d.wobbleAmplitudePx = 6f; d.wobbleFrequency = 0.8f;
                d.groupSize = 5; d.groupSpreadPx = 56f; d.spawnWeight = 4f; d.unlockWave = 1;
                d.exp = 1; d.score = 10;
            });
            var charger = LoadOrCreateMonster("Charger", d =>
            {
                d.nameKey = "monster.charger"; d.sprite = FindSprite(UfoSheetPath, "Enemy_8");
                d.scale = 1f; d.hitRadiusPx = 8f;
                d.maxHp = 4; d.earthDamage = 6; d.speed = 0.08f; d.knockbackScale = 0.9f;
                d.pattern = MonsterDefinition.MovePattern.Charge;
                d.chargeTriggerFraction = 0.3f; d.chargeWindup = 0.5f; d.chargeSpeed = 0.6f;
                d.groupSize = 1; d.spawnWeight = 3f; d.unlockWave = 2;
                d.exp = 2; d.score = 25;
            });
            var tank = LoadOrCreateMonster("Tank", d =>
            {
                d.nameKey = "monster.tank"; d.sprite = FindSprite(UfoSheetPath, "Enemy_11");
                d.scale = 1.5f; d.hitRadiusPx = 10f;
                d.maxHp = 10; d.earthDamage = 15; d.speed = 0.04f; d.knockbackScale = 0.5f;
                d.groupSize = 1; d.spawnWeight = 1.5f; d.unlockWave = 3;
                d.exp = 6; d.score = 80;
            });
            AssetDatabase.SaveAssets();
            return new[] { swarm, charger, tank };
        }

        /// <summary>보스 1종(더미 그림). 회차마다 체력이 늘어난다(GameConfig).</summary>
        private static MonsterDefinition LoadOrCreateBoss()
        {
            if (!AssetDatabase.IsValidFolder(MonsterDataFolder))
                AssetDatabase.CreateFolder("Assets/Game/Data", "Monsters");
            var boss = LoadOrCreateMonster("Boss", d =>
            {
                d.kind = MonsterDefinition.Kind.Boss;
                d.nameKey = "monster.boss1"; d.sprite = LoadSprite(DummyPath + "Boss.png");
                d.scale = 1f; d.hitRadiusPx = 16f;
                d.maxHp = 40; d.earthDamage = 30; d.speed = 0.08f; d.knockbackScale = 0.25f;
                d.pattern = MonsterDefinition.MovePattern.Patrol;
                d.patrolStartFraction = 0.22f; d.patrolSpeed = 0.18f; d.patrolDescendSpeed = 0.006f;
                d.spawnWeight = 0f; d.unlockWave = 1;
                d.exp = 20; d.score = 500;
            });
            AssetDatabase.SaveAssets();
            return boss;
        }

        /// <summary>재화 운석 1종(더미 그림). 지나가도 지구 피해 없음, 부수면 골드.</summary>
        private static MonsterDefinition LoadOrCreateMeteor()
        {
            var meteor = LoadOrCreateMonster("Meteor", d =>
            {
                d.kind = MonsterDefinition.Kind.Meteor;
                d.nameKey = "meteor.gold"; d.sprite = LoadSprite(DummyPath + "Meteor.png");
                d.scale = 1f; d.hitRadiusPx = 7f;
                d.maxHp = 3; d.earthDamage = 0; d.speed = 0.08f;
                d.spawnWeight = 0f; d.unlockWave = 1;
                d.exp = 0; d.score = 5; d.gold = 8;
            });
            AssetDatabase.SaveAssets();
            return meteor;
        }

        /// <summary>첫 빌드 카드 5장. 이미 있으면 그대로 쓴다(Inspector에서 고친 값 유지).</summary>
        private static CardDefinition[] LoadOrCreateCards()
        {
            if (!AssetDatabase.IsValidFolder(CardDataFolder))
                AssetDatabase.CreateFolder("Assets/Game/Data", "Cards");

            var list = new[]
            {
                LoadOrCreateCard("DashPower", c =>
                {
                    c.nameKey = "card.dash.name"; c.descKey = "card.dash.desc"; c.icon = LoadSprite(DummyPath + "IconDamage.png");
                    c.color = new Color(1f, 0.45f, 0.4f); c.effect = CardDefinition.Effect.DashPower; c.value = 1f; c.maxStacks = 5; c.weight = 1f;
                }),
                LoadOrCreateCard("ChargeSpeed", c =>
                {
                    c.nameKey = "card.charge.name"; c.descKey = "card.charge.desc"; c.icon = LoadSprite(DummyPath + "IconFireRate.png");
                    c.color = new Color(1f, 0.85f, 0.3f); c.effect = CardDefinition.Effect.ChargeSpeed; c.value = 0.15f; c.maxStacks = 5; c.weight = 1f;
                }),
                LoadOrCreateCard("BodySize", c =>
                {
                    c.nameKey = "card.body.name"; c.descKey = "card.body.desc"; c.icon = LoadSprite(DummyPath + "IconMultiShot.png");
                    c.color = new Color(0.4f, 0.9f, 1f); c.effect = CardDefinition.Effect.BodySize; c.value = 0.2f; c.maxStacks = 3; c.weight = 0.8f;
                }),
                LoadOrCreateCard("ChainDamage", c =>
                {
                    c.nameKey = "card.chain.name"; c.descKey = "card.chain.desc"; c.icon = LoadSprite(DummyPath + "IconPierce.png");
                    c.color = new Color(0.6f, 1f, 0.5f); c.effect = CardDefinition.Effect.ChainDamage; c.value = 1f; c.maxStacks = 3; c.weight = 0.8f;
                }),
                LoadOrCreateCard("HealEarth", c =>
                {
                    c.nameKey = "card.heal.name"; c.descKey = "card.heal.desc"; c.icon = LoadSprite(DummyPath + "IconHeal.png");
                    c.color = new Color(0.45f, 0.65f, 1f); c.effect = CardDefinition.Effect.HealEarth; c.value = 25f; c.maxStacks = 0; c.weight = 0.7f;
                }),
            };
            AssetDatabase.SaveAssets();
            return list;
        }

        private static CardDefinition LoadOrCreateCard(string name, System.Action<CardDefinition> init)
        {
            string path = CardDataFolder + "/" + name + ".asset";
            var card = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
            if (card != null) return card;
            card = ScriptableObject.CreateInstance<CardDefinition>();
            init(card);
            AssetDatabase.CreateAsset(card, path);
            return card;
        }

        private static MonsterDefinition LoadOrCreateMonster(string name, System.Action<MonsterDefinition> init)
        {
            string path = MonsterDataFolder + "/" + name + ".asset";
            var def = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(path);
            if (def != null) return def;
            def = ScriptableObject.CreateInstance<MonsterDefinition>();
            init(def);
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        /// <summary>테스트용: 이 PC의 저장(최고 기록·골드·튜토리얼 완료)을 지운다. 폰 저장은 그대로.</summary>
        [MenuItem("Bouncy UFO/Reset Save Data (this PC)")]
        public static void ResetSave()
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "save.json");
            if (!System.IO.File.Exists(path)) { EditorUtility.DisplayDialog("Bouncy UFO", "지울 저장 파일이 없습니다.", "확인"); return; }
            if (!EditorUtility.DisplayDialog("Bouncy UFO", "이 PC의 저장(최고 기록·골드·튜토리얼 완료)을 지울까요?\n" + path, "지우기", "취소")) return;
            System.IO.File.Delete(path);
            Debug.Log("[Bouncy UFO] 저장을 지웠습니다. 다음 Play부터 튜토리얼이 다시 나옵니다.");
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
    }
}
