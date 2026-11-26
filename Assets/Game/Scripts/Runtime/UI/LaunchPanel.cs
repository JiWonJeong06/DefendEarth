using System;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 출격 준비 창(로비의 "시작"을 누르면, 11/27): 직업과 UFO를 고르고 출격한다. 옛 "UFO" 창을 합쳤다.
    /// 직업·UFO는 &lt; &gt;로 넘겨 보며, 없는 것은 "Lv N 필요" 또는 "해금 골드" 버튼이 보인다(플레이 레벨 + 골드).
    /// 둘 다 가진 것을 골라야 "출격"이 켜진다. 고른 것은 저장되어 다음에도 그대로.
    /// </summary>
    public sealed class LaunchPanel : ModalPanel
    {
        private sealed class Section
        {
            public TextMeshPro name;
            public TextMeshPro desc;
            public PixelButton action;
            public TextMeshPro actionLabel;
            public int index;
        }

        private readonly Section job = new Section();
        private readonly Section ufo = new Section();
        private TextMeshPro gold;
        private TextMeshPro level;
        private SpriteRenderer ufoIcon;
        private PilotView pilot;
        private PixelButton launchButton;

        /// <summary>골드를 쓰거나 고른 것이 바뀜(로비 갱신).</summary>
        public event Action Changed;

        private const float Height = 280f;
        private float Top => Height * 0.5f;

        public static LaunchPanel Create(Transform parent, int sortingOrder)
        {
            var go = new GameObject("LaunchPanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<LaunchPanel>();
            p.Build(new Vector2(172f, Height), "launch.title", sortingOrder);
            p.BuildContent();
            return p;
        }

        private void BuildContent()
        {
            var a = GameAssets.Instance;
            CreateSprite("Coin", a.coinSprite, new Vector2(-74f, Top - 28f));
            gold = CreateText("Gold", new Vector2(-68f + 30f, Top - 28f), GameText.Style.Body, TextAlignmentOptions.MidlineLeft, 60f);
            gold.color = new Color(1f, 0.86f, 0.4f);
            level = CreateText("Level", new Vector2(40f, Top - 28f), GameText.Style.Body, TextAlignmentOptions.MidlineRight, 60f);
            level.color = new Color(1f, 0.85f, 0.35f);

            // ---- 직업 ----
            float y = Top - 50f;
            SectionTitle("launch.job", y);
            pilot = PilotView.Create(Content, new Vector3(U(-62f), U(y - 30f), 0f), 1, Order + 4);
            BuildSection(job, y - 18f, -1, true);

            // ---- UFO ----
            y = Top - 140f;
            SectionTitle("launch.ufo", y);
            ufoIcon = CreateSprite("UfoIcon", null, new Vector2(-62f, y - 30f), 5);
            BuildSection(ufo, y - 18f, -1, false);

            launchButton = CreateButton("Launch", new Vector2(0f, -Top + 24f), new Vector2(140f, 24f), "launch.go", new Color(0.3f, 0.75f, 0.45f));
            launchButton.Clicked += Launch;
        }

        private void SectionTitle(string key, float y)
        {
            var t = CreateText("Title", new Vector2(0f, y), GameText.Style.Body, TextAlignmentOptions.MidlineLeft, 150f);
            GameText.SetKey(t, key);
            t.color = new Color(0.6f, 0.75f, 1f);
            var line = CreateSprite("Line", GameAssets.Instance.pixelSprite, new Vector2(10f, y - 7f), 3);
            float px = GameAssets.Instance.pixelSprite.bounds.size.x;
            line.transform.localScale = new Vector3(U(140f) / px, U(1f) / px, 1f);
            line.color = new Color(0.3f, 0.4f, 0.7f, 0.6f);
        }

        /// <summary>&lt; 이름 &gt; / 설명 / 상태 버튼 한 묶음. 왼쪽 30픽셀은 그림 자리.</summary>
        private void BuildSection(Section s, float y, int unused, bool isJob)
        {
            var prev = CreateButton("Prev", new Vector2(-36f, y), new Vector2(16f, 16f), null, new Color(0.3f, 0.4f, 0.7f));
            GameText.SetPlain(GameText.Create("L", prev.transform, GameText.Style.Title, Order + 8), "<");
            var next = CreateButton("Next", new Vector2(72f, y), new Vector2(16f, 16f), null, new Color(0.3f, 0.4f, 0.7f));
            GameText.SetPlain(GameText.Create("R", next.transform, GameText.Style.Title, Order + 8), ">");
            prev.Clicked += () => Browse(s, isJob, -1);
            next.Clicked += () => Browse(s, isJob, 1);

            s.name = CreateText("Name", new Vector2(18f, y), GameText.Style.Title, TextAlignmentOptions.Center, 90f);
            s.desc = CreateText("Desc", new Vector2(18f, y - 22f), GameText.Style.Body, TextAlignmentOptions.Center, 108f, true);
            s.desc.color = new Color(0.75f, 0.8f, 1f);
            s.action = CreateButton("Action", new Vector2(18f, y - 46f), new Vector2(100f, 16f), null, new Color(0.85f, 0.6f, 0.25f), GameText.Style.Body);
            s.actionLabel = GameText.Create("Label", s.action.transform, GameText.Style.Body, Order + 8, TextAlignmentOptions.Center, 98f);
            s.action.Clicked += () => Unlock(s, isJob);
        }

        private static int Count(bool isJob) => isJob ? JobTypes.All.Length : UfoTypes.All.Length;
        private static bool Owned(bool isJob, int i) => isJob ? JobTypes.IsOwned(i) : UfoTypes.IsOwned(i);
        private static UnlockRule Rule(bool isJob, int i) => isJob ? JobTypes.All[i].unlock : UfoTypes.All[i].unlock;

        private void Browse(Section s, bool isJob, int delta)
        {
            int n = Count(isJob);
            s.index = ((s.index + delta) % n + n) % n;
            // 가진 것으로 넘기면 바로 고른 것으로 저장
            if (Owned(isJob, s.index))
            {
                if (isJob) SaveSystem.Data.selectedJob = s.index; else SaveSystem.Data.selectedUfo = s.index;
                SaveSystem.Save();
                Changed?.Invoke();
            }
            Sound.Play(Sfx.Click);
            Refresh();
        }

        private void Unlock(Section s, bool isJob)
        {
            if (Owned(isJob, s.index)) return;
            var rule = Rule(isJob, s.index);
            var save = SaveSystem.Data;
            if (!rule.LevelMet || save.gold < rule.gold) { Sound.Play(Sfx.Hit, 0.7f); return; }
            save.gold -= rule.gold;
            if (isJob) { save.ownedJobs.Add(s.index); save.selectedJob = s.index; }
            else { save.ownedUfos.Add(s.index); save.selectedUfo = s.index; }
            SaveSystem.Save();
            Sound.Play(Sfx.LevelUp);
            Changed?.Invoke();
            Refresh();
        }

        private void Launch()
        {
            if (!Owned(true, job.index) || !Owned(false, ufo.index)) return;
            var save = SaveSystem.Data;
            save.selectedJob = job.index;
            save.selectedUfo = ufo.index;
            SaveSystem.Save();
            SceneFlow.StartGame();
        }

        private void Refresh()
        {
            var save = SaveSystem.Data;
            GameText.SetPlain(gold, save.gold.ToString());
            GameText.SetPlain(level, "Lv " + UnlockRule.PlayerLevel);
            pilot.Refresh();

            var j = JobTypes.All[job.index];
            GameText.SetKey(job.name, j.nameKey);
            GameText.SetKey(job.desc, j.descKey);
            RefreshAction(job, true);

            var u = UfoTypes.All[ufo.index];
            GameText.SetKey(ufo.name, u.nameKey);
            GameText.SetKey(ufo.desc, u.descKey);
            UfoTypes.Apply(ufoIcon, ufo.index);
            ufoIcon.color = Owned(false, ufo.index) ? u.tint : new Color(0.35f, 0.35f, 0.45f); // 없는 UFO는 어둡게
            RefreshAction(ufo, false);

            bool ready = Owned(true, job.index) && Owned(false, ufo.index);
            launchButton.Interactable = ready;
            launchButton.SetColor(ready ? new Color(0.3f, 0.75f, 0.45f) : new Color(0.3f, 0.32f, 0.38f));
        }

        private void RefreshAction(Section s, bool isJob)
        {
            var save = SaveSystem.Data;
            bool owned = Owned(isJob, s.index);
            var rule = Rule(isJob, s.index);
            if (owned)
            {
                s.action.Interactable = false;
                s.action.SetColor(new Color(0.25f, 0.45f, 0.32f));
                GameText.SetKey(s.actionLabel, "launch.owned");
            }
            else if (!rule.LevelMet)
            {
                s.action.Interactable = false;
                s.action.SetColor(new Color(0.3f, 0.3f, 0.38f));
                GameText.SetPlain(s.actionLabel, string.Format(Localization.Get("launch.need_level"), rule.level));
            }
            else
            {
                bool afford = save.gold >= rule.gold;
                s.action.Interactable = afford;
                s.action.SetColor(afford ? new Color(0.85f, 0.6f, 0.25f) : new Color(0.4f, 0.35f, 0.3f));
                GameText.SetPlain(s.actionLabel, string.Format(Localization.Get("launch.unlock"), rule.gold));
            }
        }

        protected override void OnOpened()
        {
            job.index = JobTypes.SelectedIndex;
            ufo.index = UfoTypes.SelectedIndex;
            Refresh();
        }
    }
}
