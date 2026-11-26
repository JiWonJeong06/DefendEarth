using System.Collections;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 따로 만든 튜토리얼(처음 한 번, 또는 설정의 "튜토리얼 다시 보기" 뒤). 게임 씬에서 웨이브 대신 정해진 순서로 진행한다.
    /// 1) 조작법: UFO를 네모로 강조하고 나머지는 어둡게, 글 상자에 한 글자씩 타이핑 → 두 번 날아 보기
    /// 2) 몬스터 3종을 하나씩: 나타나면 멈추고 강조·설명 → 들이받아 처치(놓치면 다시)
    /// 3) 골드 운석: 골드를 주고 지구엔 피해 없음
    /// 4) 증강(레벨업 카드): 경험치 막대 강조·설명 → 레벨업 시켜 직접 하나 고르기
    /// 5) 보스(체력 약하게): 강조·설명 → 처치
    /// 6) "이제부터 웨이브가 끝없이 이어져요" + 로비 강화 안내 → 로비로
    /// 오른쪽 위(일시정지 아래) "건너뛰기": 누르면 멈추고 예/아니요. 예 = 끝내고 로비, 아니요 = 계속.
    /// 튜토리얼 중에는 지구 체력이 1 아래로 내려가지 않고, 로비 강화는 모두 최대로 적용된다(Upgrades).
    /// 처치 경험치는 없고 증강은 4)에서 한 번만 고른다. 얻은 골드는 저장된다.
    /// </summary>
    public sealed class TutorialDirector : MonoBehaviour
    {
        private enum Outcome { None, Killed, Escaped }

        private const int DimOrder = 1500;            // HUD(1000) 위, 확인 창(Overlay 2000) 아래
        private const float TypeSpeed = 36f;          // 글자/초
        private const float TapDelay = 0.25f;         // 글 상자가 뜨자마자 잘못 넘기지 않게
        private const float BoxWidthPx = 168f;
        private const float BoxHeightPx = 72f;
        private const float HintYFromBottomPx = 104f;

        [SerializeField] private UfoController ufo;
        [SerializeField] private WaveDirector waves;
        [SerializeField] private LevelSystem levels;

        private GameConfig Config => PlayArea.Config;
        private float U(float px) => Config.PixelsToUnits(px);
        private GameSession Session => GameSession.Instance;

        private float pixelSize;
        private readonly SpriteRenderer[] dim = new SpriteRenderer[4];
        private readonly SpriteRenderer[] frame = new SpriteRenderer[4];
        private Transform boxRoot;
        private TextMeshPro boxText;
        private TextMeshPro boxArrow;
        private TextMeshPro hint;
        private PixelButton skipButton;
        private ConfirmPanel skipConfirm;
        private TapInput input;

        private Transform target;
        private Vector2 targetHalfPx;
        private bool dimOn;

        private bool dialogOpen;
        private string[] lines;
        private int lineIndex;
        private float typed;
        private int lineLength;
        private float openedAt;
        private int dialogLayer = -1;

        private int dashes;
        private Monster watching;
        private Outcome outcome;
        private bool bossDone;
        private bool bossKilled;
        private bool cardPicked;
        private bool finished;

        private void Start()
        {
            if (!SceneFlow.IsTutorial) { gameObject.SetActive(false); return; }
            if (waves != null) waves.Scripted = true;
            BuildOverlay();

            input = TapInput.Main;
            if (input != null) input.Released += OnTap;
            if (ufo != null) ufo.Dashed += OnDashed;
            Monster.Killed += OnKilled;
            Monster.Escaped += OnEscaped;
            if (waves != null) waves.BossEnded += OnBossEnded;
            if (levels != null) levels.CardPicked += OnCardPicked;

            StartCoroutine(Run());
        }

        private void OnDestroy()
        {
            if (input != null) input.Released -= OnTap;
            if (ufo != null) ufo.Dashed -= OnDashed;
            Monster.Killed -= OnKilled;
            Monster.Escaped -= OnEscaped;
            if (waves != null) waves.BossEnded -= OnBossEnded;
            if (levels != null) levels.CardPicked -= OnCardPicked;
        }

        // ================= 진행 순서 =================

        private IEnumerator Run()
        {
            yield return null; // 다른 부품들이 Start를 마치도록 한 프레임 쉰다

            // 1) 조작법
            yield return Say(ufo.transform, 16f, "tut.intro1", "tut.intro2", "tut.ctrl1", "tut.ctrl2", "tut.ctrl3", "tut.ctrl4");
            dashes = 0;
            while (dashes < 2)
            {
                SetHint(Localization.Get("tut.hint.dash") + " (" + dashes + "/2)");
                yield return null;
            }
            SetHint(null);
            yield return Wait(0.8f);

            // 2) 몬스터 3종 (GameAssets.monsters: 떼거리형, 돌진형, 덩치형)
            var monsters = GameAssets.Instance.monsters;
            yield return Encounter(monsters[0], false, "tut.hint.ram", "tut.swarm1", "tut.swarm2", "tut.swarm3");
            yield return Encounter(monsters[1], false, "tut.hint.ram", "tut.charger1", "tut.charger2");
            yield return Encounter(monsters[2], false, "tut.hint.ram", "tut.tank1", "tut.tank2", "tut.tank3");

            // 3) 골드 운석(놓쳐도 다음으로)
            yield return Encounter(GameAssets.Instance.meteor, true, "tut.hint.meteor", "tut.meteor1", "tut.meteor2", "tut.meteor3");

            // 4) 증강(레벨업 카드)
            yield return Augment();

            // 5) 보스
            bool firstBoss = true;
            while (true)
            {
                bossDone = false;
                var boss = waves.StartBoss(Config.tutorialBossHpMultiplier);
                if (boss == null) break;
                float patrolY = PlayArea.Top - boss.Definition.patrolStartFraction * PlayArea.Size.y;
                while (boss.IsAlive && boss.transform.position.y > patrolY + U(2f)) yield return null;
                if (firstBoss)
                {
                    firstBoss = false;
                    yield return Say(boss.transform, 22f, "tut.boss1", "tut.boss2", "tut.boss3", "tut.boss4");
                }
                SetTarget(boss.transform, 22f);
                SetHint(Localization.Get("tut.hint.boss"));
                while (!bossDone) yield return null;
                SetTarget(null, 0f);
                SetHint(null);
                if (bossKilled) break;
                SetHint(Localization.Get("tut.retry"));
                yield return Wait(1.5f);
            }
            yield return Wait(1.6f); // 보상 골드가 날아오는 동안

            // 6) 끝
            yield return Say(null, 0f, "tut.end1", "tut.end2", "tut.end3", "tut.end4");
            Finish();
        }

        /// <summary>몬스터 하나를 가운데 위에서 내보내 처음 보일 때 멈추고 설명, 처치할 때까지(놓치면 다시).</summary>
        private IEnumerator Encounter(MonsterDefinition def, bool escapeIsFine, string hintKey, params string[] keys)
        {
            if (def == null) yield break;
            bool first = true;
            float half = Mathf.Max(12f, def.hitRadiusPx * def.scale + 6f);
            while (true)
            {
                float r = U(def.hitRadiusPx) * def.scale;
                var m = waves.Spawner.SpawnAt(def, new Vector2(0f, PlayArea.Top + r), 1f, 1f);
                if (m == null) yield break;
                watching = m;
                outcome = Outcome.None;

                // 화면 위쪽 1/3쯤 내려와 잘 보일 때
                float showY = PlayArea.Top - PlayArea.Size.y * 0.3f;
                while (outcome == Outcome.None && m.transform.position.y > showY) yield return null;
                if (first && outcome == Outcome.None)
                {
                    first = false;
                    yield return Say(m.transform, half, keys);
                }
                SetTarget(m.transform, half);
                SetHint(Localization.Get(hintKey));
                while (outcome == Outcome.None) yield return null;
                SetTarget(null, 0f);
                SetHint(null);
                watching = null;

                if (outcome == Outcome.Killed || escapeIsFine) break;
                SetHint(Localization.Get("tut.retry"));
                yield return Wait(1.2f);
                SetHint(null);
            }
            yield return Wait(0.8f);
        }

        /// <summary>경험치 막대를 강조해 설명하고, 바로 레벨업시켜 증강 하나를 직접 고르게 한다.</summary>
        private IEnumerator Augment()
        {
            if (levels == null) yield break;
            var xpBar = FindFirstObjectByType<XpBar>();
            float halfW = Config.playAreaPixels.x * 0.5f - 4f;
            if (xpBar != null) yield return Say(xpBar.transform, new Vector2(halfW, 5f), "tut.aug1", "tut.aug2");
            yield return Say(null, 0f, "tut.aug3");

            cardPicked = false;
            levels.AddXp(Mathf.Max(0.01f, levels.XpToNext - levels.Xp)); // 레벨업 → 증강 3개 고르기 창
            while (!cardPicked) yield return null;
            yield return Wait(0.4f);
            yield return Say(null, 0f, "tut.aug4");
            yield return Wait(0.6f);
        }

        private void OnCardPicked(CardDefinition card, int stacks) => cardPicked = true;

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.unscaledTime + seconds;
            while (Time.unscaledTime < until) yield return null;
        }

        private void OnDashed(Vector2 aim) => dashes++;

        private void OnKilled(Monster m)
        {
            if (m == watching) outcome = Outcome.Killed;
        }

        private void OnEscaped(Monster m)
        {
            if (m == watching) outcome = Outcome.Escaped;
        }

        private void OnBossEnded(bool killed)
        {
            bossDone = true;
            bossKilled = killed;
        }

        /// <summary>튜토리얼 끝(다 봤거나 건너뜀): 다시 안 나오게 저장하고 로비로.</summary>
        private void Finish()
        {
            if (finished) return;
            finished = true;
            StopAllCoroutines();
            if (Session != null) Session.FinishTutorial(true);
        }

        // ================= 글 상자 (멈추고 한 글자씩) =================

        private IEnumerator Say(Transform focus, float halfPx, params string[] keys) => Say(focus, new Vector2(halfPx, halfPx), keys);

        /// <summary>멈추고 대상을 강조(halfPx = 네모의 반 너비·반 높이)한 채 글을 한 줄씩 보여 준다.</summary>
        private IEnumerator Say(Transform focus, Vector2 halfPx, params string[] keys)
        {
            OpenDialog(focus, halfPx, keys);
            while (dialogOpen) yield return null;
        }

        private void OpenDialog(Transform focus, Vector2 halfPx, string[] keys)
        {
            lines = keys;
            lineIndex = 0;
            dialogOpen = true;
            dimOn = true;
            SetTarget(focus, halfPx);
            SetHint(null);
            if (Session != null) Session.Pause();
            dialogLayer = UiLayer.Push();      // 뒤의 버튼(일시정지)은 막고
            skipButton.Layer = dialogLayer;     // 건너뛰기만 누를 수 있게

            // 강조한 것과 겹치지 않게: 아래쪽을 강조하면 글 상자는 위, 위쪽이면 아래
            float y = focus == null ? 0f : (focus.position.y < 0f ? 40f : -36f);
            boxRoot.position = new Vector3(0f, U(y), 0f);
            boxRoot.gameObject.SetActive(true);
            ShowLine();
        }

        private void ShowLine()
        {
            GameText.SetKey(boxText, lines[lineIndex]);
            boxText.maxVisibleCharacters = 0;
            boxText.ForceMeshUpdate();
            lineLength = boxText.textInfo.characterCount;
            typed = 0f;
            openedAt = Time.unscaledTime;
            boxArrow.gameObject.SetActive(false);
        }

        private void CloseDialog()
        {
            dialogOpen = false;
            dimOn = false;
            SetTarget(null, Vector2.zero);
            boxRoot.gameObject.SetActive(false);
            UiLayer.Pop(dialogLayer);
            skipButton.Layer = 0;
            if (Session != null) Session.Resume();
        }

        private void OnTap(Vector2 pos, float held)
        {
            if (!dialogOpen || finished) return;
            if (skipConfirm != null && skipConfirm.IsOpen) return;
            if (PixelButton.AnyAt(pos)) return; // 건너뛰기 버튼
            if (Time.unscaledTime - openedAt < TapDelay) return;

            if (typed < lineLength) { typed = lineLength; return; } // 타이핑 중이면 한 번에 다 보이기
            lineIndex++;
            if (lineIndex < lines.Length) ShowLine();
            else CloseDialog();
        }

        // ================= 건너뛰기 =================

        private void OnSkipClicked()
        {
            if (finished || skipConfirm.IsOpen) return;
            if (Session != null) Session.Pause();
            skipConfirm.Ask("tut.skip_confirm", Finish);
        }

        private void OnSkipConfirmClosed()
        {
            if (Session != null) Session.Resume();
            openedAt = Time.unscaledTime; // '아니요'를 누른 손가락으로 글 상자가 넘어가지 않게
        }

        // ================= 화면 만들기 =================

        private void BuildOverlay()
        {
            var a = GameAssets.Instance;
            pixelSize = a.pixelSprite.bounds.size.x;

            for (int i = 0; i < 4; i++)
            {
                dim[i] = a.CreateSprite("Dim", a.pixelSprite, DimOrder, transform);
                dim[i].color = new Color(0f, 0f, 0.04f, 0.62f);
                frame[i] = a.CreateSprite("Highlight", a.pixelSprite, DimOrder + 1, transform);
                frame[i].color = new Color(1f, 0.9f, 0.3f);
            }

            boxRoot = new GameObject("TextBox").transform;
            boxRoot.SetParent(transform, false);
            var border = a.CreateSprite("Border", a.pixelSprite, DimOrder + 2, boxRoot);
            border.color = new Color(0.45f, 0.6f, 1f);
            border.transform.localScale = new Vector3(U(BoxWidthPx) / pixelSize, U(BoxHeightPx) / pixelSize, 1f);
            var back = a.CreateSprite("Back", a.pixelSprite, DimOrder + 3, boxRoot);
            back.color = new Color(0.06f, 0.07f, 0.15f);
            back.transform.localScale = new Vector3(U(BoxWidthPx - 4f) / pixelSize, U(BoxHeightPx - 4f) / pixelSize, 1f);

            float textWidth = BoxWidthPx - 14f;
            boxText = GameText.Create("Text", boxRoot, GameText.Style.Body, DimOrder + 4, TextAlignmentOptions.TopLeft, textWidth, true);
            // 글 상자 칸 높이는 64픽셀(4유닛), 위쪽 맞춤 → 글자 위 끝이 상자 위에서 7픽셀 아래
            boxText.transform.localPosition = new Vector3(0f, U(BoxHeightPx * 0.5f - 7f - 32f), 0f);
            boxArrow = GameText.Create("Next", boxRoot, GameText.Style.Body, DimOrder + 4);
            GameText.SetPlain(boxArrow, "▼");
            boxArrow.color = new Color(1f, 0.9f, 0.3f);
            boxArrow.transform.localPosition = new Vector3(U(BoxWidthPx * 0.5f - 9f), U(-BoxHeightPx * 0.5f + 8f), 0f);
            boxRoot.gameObject.SetActive(false);

            hint = GameText.Create("Hint", transform, GameText.Style.Body, SortOrder.Hud + 10, TextAlignmentOptions.Center, 170f, true);
            hint.transform.position = new Vector3(0f, PlayArea.Bottom + U(HintYFromBottomPx), 0f);
            hint.color = new Color(1f, 0.9f, 0.45f);
            hint.gameObject.SetActive(false);

            // 오른쪽 위, 일시정지 버튼 아래
            float halfW = Config.playAreaPixels.x * 0.5f;
            float halfH = Config.playAreaPixels.y * 0.5f;
            skipButton = PixelButton.Create("Skip", transform, new Vector2(halfW - 25f, halfH - 36f), new Vector2(44f, 13f),
                "tut.skip", new Color(0.35f, 0.4f, 0.6f), DimOrder + 10, 0, GameText.Style.Body);
            skipButton.Clicked += OnSkipClicked;
            skipConfirm = ConfirmPanel.Create(transform, SortOrder.Overlay);
            skipConfirm.Closed += OnSkipConfirmClosed;

            LayoutOverlay();
        }

        private void SetTarget(Transform t, float halfPx) => SetTarget(t, new Vector2(halfPx, halfPx));

        private void SetTarget(Transform t, Vector2 halfPx)
        {
            target = t;
            targetHalfPx = halfPx;
        }

        private void SetHint(string text)
        {
            if (string.IsNullOrEmpty(text)) { hint.gameObject.SetActive(false); return; }
            hint.gameObject.SetActive(true);
            GameText.SetPlain(hint, text);
        }

        private void Update()
        {
            if (finished) return;
            if (dialogOpen)
            {
                if (typed < lineLength)
                {
                    int before = Mathf.FloorToInt(typed);
                    typed = Mathf.Min(lineLength, typed + TypeSpeed * Time.unscaledDeltaTime);
                    if (Mathf.FloorToInt(typed) > before) Sound.Play(Sfx.Click, 1.6f, 0.25f); // 타자 소리
                }
                boxText.maxVisibleCharacters = Mathf.FloorToInt(typed);
                bool done = typed >= lineLength;
                boxArrow.gameObject.SetActive(done && Mathf.PingPong(Time.unscaledTime * 2.5f, 1f) > 0.4f);
            }
            if (hint.gameObject.activeSelf) hint.alpha = 0.65f + 0.35f * Mathf.PingPong(Time.unscaledTime * 1.5f, 1f);
        }

        private void LateUpdate()
        {
            if (!finished) LayoutOverlay();
        }

        /// <summary>어둡게 덮는 4장(구멍 둘레)과 노란 네모 테두리를 강조 대상에 맞춘다.</summary>
        private void LayoutOverlay()
        {
            bool hasTarget = target != null && target.gameObject.activeInHierarchy;
            float onePx = U(1f);
            float cx = 0f, cy = -100f, hx = 0f, hy = 0f;
            if (hasTarget)
            {
                // 도트 칸에 맞춘다
                cx = Mathf.Round(target.position.x / onePx) * onePx;
                cy = Mathf.Round(target.position.y / onePx) * onePx;
                hx = U(targetHalfPx.x);
                hy = U(targetHalfPx.y);
            }
            const float far = 40f;
            for (int i = 0; i < 4; i++) dim[i].gameObject.SetActive(dimOn);
            if (dimOn)
            {
                Place(dim[0], -far, far, cy + hy, far);        // 위
                Place(dim[1], -far, far, -far, cy - hy);       // 아래
                Place(dim[2], -far, cx - hx, cy - hy, cy + hy); // 왼쪽
                Place(dim[3], cx + hx, far, cy - hy, cy + hy);  // 오른쪽
            }

            bool showFrame = hasTarget && Mathf.PingPong(Time.unscaledTime * 3f, 1f) > 0.25f;
            for (int i = 0; i < 4; i++) frame[i].gameObject.SetActive(showFrame);
            if (showFrame)
            {
                float ex = hx + onePx, ey = hy + onePx;
                Place(frame[0], cx - ex, cx + ex, cy + hy, cy + ey);
                Place(frame[1], cx - ex, cx + ex, cy - ey, cy - hy);
                Place(frame[2], cx - ex, cx - hx, cy - hy, cy + hy);
                Place(frame[3], cx + hx, cx + ex, cy - hy, cy + hy);
            }
        }

        private void Place(SpriteRenderer sr, float x0, float x1, float y0, float y1)
        {
            float w = Mathf.Max(0f, x1 - x0), hgt = Mathf.Max(0f, y1 - y0);
            sr.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
            sr.transform.localScale = new Vector3(w / pixelSize, hgt / pixelSize, 1f);
        }
    }
}
