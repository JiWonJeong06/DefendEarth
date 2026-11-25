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
        private const string ArtPath = "Assets/Art/Temp/";
        private const string DummyPath = "Assets/Art/Temp/Dummy/";
        private const string CardDataFolder = "Assets/Game/Data/Cards";
        private const string StringsPath = "Assets/Game/Data/Localization/strings.csv";
        private const string MonsterDataFolder = "Assets/Game/Data/Monsters";
        private const string FlashShaderPath = "Assets/Game/Shaders/SpriteFlash.shader";
        private const string FlashMaterialPath = "Assets/Game/Data/SpriteFlash.mat";
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

            Material flashMat = LoadOrCreateFlashMaterial();
            Sprite pixel = LoadSprite(ArtPath + "Pixel.png");

            // 1) 설정 루트 + 공용 그림·재질 + 판 상태
            var rootGo = new GameObject("GameRoot");
            var root = rootGo.AddComponent<GameRoot>();
            SetField(root, "config", config);
            var assets = rootGo.AddComponent<GameAssets>();
            assets.bulletSprite = LoadSprite(ArtPath + "Bullet.png");
            assets.sparkSprite = LoadSprite(ArtPath + "Spark.png");
            assets.pixelSprite = pixel;
            assets.spriteMaterial = spriteMat;
            assets.flashMaterial = flashMat;
            assets.monsters = LoadOrCreateMonsters();
            assets.boss = LoadOrCreateBoss();
            assets.meteor = LoadOrCreateMeteor();
            assets.coinSprite = LoadSprite(DummyPath + "Coin.png");
            assets.digitSprites = new Sprite[10];
            for (int i = 0; i < 10; i++) assets.digitSprites[i] = LoadSprite(DummyPath + "Digit" + i + ".png");
            assets.cards = LoadOrCreateCards();
            assets.strings = AssetDatabase.LoadAssetAtPath<TextAsset>(StringsPath);
            if (assets.strings == null) Debug.LogWarning("[Bouncy UFO] 번역표가 없습니다: " + StringsPath);
            var session = rootGo.AddComponent<GameSession>();
            new GameObject("Effects").AddComponent<Effects>();

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
            var bg = CreateSprite("PlayAreaBackground", bgSprite, spriteMat, SortOrder.Background);
            if (bgSprite != null)
            {
                Vector2 s = bgSprite.bounds.size;
                bg.transform.localScale = new Vector3(PlayArea.Size.x / s.x, PlayArea.Size.y / s.y, 1f);
            }

            // 4) 지구 (임시 그림, 아래쪽 가운데) + 체력 바
            var earthGo = CreateSprite("Earth", FindSprite(EarthSheetPath, "Earth-Star_0"), spriteMat, SortOrder.Earth);
            earthGo.transform.position = new Vector3(0f, PlayArea.Bottom, 0f);
            var earth = earthGo.AddComponent<Earth>();
            SetField(earth, "spriteRenderer", earthGo.GetComponent<SpriteRenderer>());
            SetField(earth, "visual", earthGo.AddComponent<SquashStretch>());
            SetField(session, "earth", earth);
            var bar = new GameObject("EarthHealthBar").AddComponent<EarthHealthBar>();
            SetField(bar, "earth", earth);

            // 5) UFO (입력 + 조작 + 자동 발사 + 통통 연출)
            var ufo = new GameObject("UFO");
            var input = ufo.AddComponent<TapInput>();
            var controller = ufo.AddComponent<UfoController>();
            var weapon = ufo.AddComponent<Weapon>();
            var visual = CreateSprite("Visual", FindSprite(UfoSheetPath, "Enemy_0"), spriteMat, SortOrder.Ufo);
            visual.transform.SetParent(ufo.transform, false);
            var squash = visual.AddComponent<SquashStretch>();
            SetField(controller, "input", input);
            SetField(controller, "visual", squash);
            SetField(weapon, "visual", squash);
            ufo.transform.position = new Vector3(0f, PlayArea.Bottom + config.PixelsToUnits(config.ufoHeightFromBottomPx), 0f);

            // 6) 몬스터 등장 + 판 끝 화면
            var spawner = new GameObject("MonsterSpawner").AddComponent<MonsterSpawner>();
            var warning = new GameObject("WarningFlash").AddComponent<WarningFlash>();
            var gold = new GameObject("GoldDrops").AddComponent<GoldDrops>();
            SetField(gold, "target", ufo.transform);
            var waves = new GameObject("WaveDirector").AddComponent<WaveDirector>();
            SetField(waves, "spawner", spawner);
            SetField(waves, "warning", warning);
            SetField(waves, "gold", gold);

            // 7) 위쪽 표시: 골드·웨이브 번호, 보스 체력 바
            var hud = new GameObject("TopHud").AddComponent<TopHud>();
            SetField(hud, "waves", waves);
            var bossBar = new GameObject("BossHealthBar").AddComponent<BossHealthBar>();
            SetField(bossBar, "waves", waves);

            // 8) 판 안 성장: 경험치·레벨업 카드
            var levels = new GameObject("LevelSystem").AddComponent<LevelSystem>();
            SetField(levels, "weapon", weapon);
            SetField(levels, "ufoVisual", squash);
            var xpBar = new GameObject("XpBar").AddComponent<XpBar>();
            SetField(xpBar, "levels", levels);
            var cardUi = new GameObject("CardChoiceUI").AddComponent<CardChoiceUI>();
            SetField(cardUi, "input", input);
            SetField(cardUi, "levels", levels);
            var overlay = new GameObject("GameOverOverlay").AddComponent<GameOverOverlay>();
            SetField(overlay, "input", input);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("[Bouncy UFO] Game 장면을 만들었습니다: " + ScenePath);
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
                d.maxHp = 2; d.earthDamage = 3; d.speed = 0.09f;
                d.wobbleAmplitudePx = 6f; d.wobbleFrequency = 0.8f;
                d.groupSize = 5; d.groupSpreadPx = 56f; d.spawnWeight = 4f; d.unlockWave = 1;
                d.exp = 1; d.score = 10;
            });
            var charger = LoadOrCreateMonster("Charger", d =>
            {
                d.nameKey = "monster.charger"; d.sprite = FindSprite(UfoSheetPath, "Enemy_8");
                d.scale = 1f; d.hitRadiusPx = 8f;
                d.maxHp = 4; d.earthDamage = 6; d.speed = 0.08f;
                d.pattern = MonsterDefinition.MovePattern.Charge;
                d.chargeTriggerFraction = 0.3f; d.chargeWindup = 0.5f; d.chargeSpeed = 0.6f;
                d.groupSize = 1; d.spawnWeight = 3f; d.unlockWave = 2;
                d.exp = 2; d.score = 25;
            });
            var tank = LoadOrCreateMonster("Tank", d =>
            {
                d.nameKey = "monster.tank"; d.sprite = FindSprite(UfoSheetPath, "Enemy_11");
                d.scale = 1.5f; d.hitRadiusPx = 10f;
                d.maxHp = 22; d.earthDamage = 15; d.speed = 0.04f;
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
                d.maxHp = 120; d.earthDamage = 30; d.speed = 0.08f;
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
                d.maxHp = 4; d.earthDamage = 0; d.speed = 0.08f;
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
                LoadOrCreateCard("Damage", c =>
                {
                    c.nameKey = "card.damage.name"; c.descKey = "card.damage.desc"; c.icon = LoadSprite(DummyPath + "IconDamage.png");
                    c.color = new Color(1f, 0.45f, 0.4f); c.effect = CardDefinition.Effect.Damage; c.value = 1f; c.maxStacks = 5; c.weight = 1f;
                }),
                LoadOrCreateCard("FireRate", c =>
                {
                    c.nameKey = "card.firerate.name"; c.descKey = "card.firerate.desc"; c.icon = LoadSprite(DummyPath + "IconFireRate.png");
                    c.color = new Color(1f, 0.85f, 0.3f); c.effect = CardDefinition.Effect.FireRate; c.value = 0.15f; c.maxStacks = 5; c.weight = 1f;
                }),
                LoadOrCreateCard("MultiShot", c =>
                {
                    c.nameKey = "card.multishot.name"; c.descKey = "card.multishot.desc"; c.icon = LoadSprite(DummyPath + "IconMultiShot.png");
                    c.color = new Color(0.4f, 0.9f, 1f); c.effect = CardDefinition.Effect.MultiShot; c.value = 1f; c.maxStacks = 3; c.weight = 0.8f;
                }),
                LoadOrCreateCard("Pierce", c =>
                {
                    c.nameKey = "card.pierce.name"; c.descKey = "card.pierce.desc"; c.icon = LoadSprite(DummyPath + "IconPierce.png");
                    c.color = new Color(0.6f, 1f, 0.5f); c.effect = CardDefinition.Effect.Pierce; c.value = 1f; c.maxStacks = 3; c.weight = 0.8f;
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
