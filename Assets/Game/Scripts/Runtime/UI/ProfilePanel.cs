using System;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 프로필 창(로비 왼쪽 위 프로필을 누르면): 조종사 얼굴, 닉네임 + 바꾸기, 플레이 레벨·경험치,
    /// 플레이 시간·판 수·최고 점수·최고 웨이브, 꾸미기 버튼.
    /// </summary>
    public sealed class ProfilePanel : ModalPanel
    {
        private PilotView pilot;
        private TextMeshPro nickname;
        private TextMeshPro level;
        private TextMeshPro xpText;
        private Transform xpFill;
        private readonly TextMeshPro[] statValues = new TextMeshPro[4];
        private NicknamePanel nicknamePanel;

        private const float XpWidthPx = 100f;
        private const float XpY = 2f;
        private static readonly string[] StatKeys = { "profile.playtime", "profile.runs", "profile.best", "profile.bestwave" };

        /// <summary>"꾸미기"를 눌렀을 때(이 창은 닫힌다).</summary>
        public event Action CustomizeRequested;

        public static ProfilePanel Create(Transform parent, int sortingOrder)
        {
            var go = new GameObject("ProfilePanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<ProfilePanel>();
            p.Build(new Vector2(164f, 260f), "profile.title", sortingOrder);
            p.BuildContent();
            p.nicknamePanel = NicknamePanel.Create(parent, sortingOrder + 30);
            return p;
        }

        private void BuildContent()
        {
            var a = GameAssets.Instance;
            float px = a.pixelSprite.bounds.size.x;

            pilot = PilotView.Create(Content, new Vector3(0f, U(76f), 0f), 2, Order + 4);

            nickname = CreateText("Nickname", new Vector2(-14f, 30f), GameText.Style.Title, TextAlignmentOptions.Center, 104f);
            var rename = CreateButton("Rename", new Vector2(56f, 30f), new Vector2(38f, 16f), "ui.settings.rename", new Color(0.3f, 0.4f, 0.7f), GameText.Style.Body);
            rename.Clicked += () => nicknamePanel.Ask(true, _ => Refresh());

            // 레벨 + 경험치 바 + 숫자
            level = CreateText("Level", new Vector2(0f, 14f), GameText.Style.Body);
            level.color = new Color(1f, 0.85f, 0.35f);
            var back = CreateSprite("XpBack", a.pixelSprite, new Vector2(0f, XpY));
            back.color = new Color(0.1f, 0.12f, 0.25f);
            back.transform.localScale = new Vector3(U(XpWidthPx) / px, U(3f) / px, 1f);
            var fill = CreateSprite("XpFill", a.pixelSprite, new Vector2(0f, XpY), 5);
            fill.color = new Color(0.6f, 1f, 0.7f);
            xpFill = fill.transform;
            xpText = CreateText("XpText", new Vector2(0f, XpY - 10f), GameText.Style.Body);
            xpText.color = new Color(0.7f, 0.8f, 0.95f);

            // 기록
            float y = -26f;
            for (int i = 0; i < StatKeys.Length; i++)
            {
                var label = CreateText("Label" + i, new Vector2(-4f, y), GameText.Style.Body, TextAlignmentOptions.MidlineLeft, 140f);
                GameText.SetKey(label, StatKeys[i]);
                label.color = new Color(0.75f, 0.8f, 1f);
                statValues[i] = CreateText("Value" + i, new Vector2(4f, y), GameText.Style.Body, TextAlignmentOptions.MidlineRight, 140f);
                y -= 15f;
            }

            var custom = CreateButton("Customize", new Vector2(0f, -108f), new Vector2(120f, 18f), "ui.custom", new Color(0.75f, 0.45f, 0.8f), GameText.Style.Body);
            custom.Clicked += () =>
            {
                Close();
                CustomizeRequested?.Invoke();
            };
        }

        /// <summary>뒤로 가기: 닉네임 창 → 이 창 순서로 닫는다. 닫은 게 있으면 true.</summary>
        public bool Back()
        {
            if (nicknamePanel != null && nicknamePanel.IsOpen) { nicknamePanel.Close(); return true; }
            if (IsOpen) { Close(); return true; }
            return false;
        }

        protected override void OnOpened() => Refresh();

        private void Refresh()
        {
            var d = SaveSystem.Data;
            pilot.Refresh();
            GameText.SetPlain(nickname, Nickname.IsSet ? Nickname.Current : "-");

            var acc = Account.FromTotal(d.accountXp);
            GameText.SetPlain(level, "Lv " + acc.level);
            float k = acc.need > 0 ? Mathf.Clamp01((float)acc.xp / acc.need) : 0f;
            float px = GameAssets.Instance.pixelSprite.bounds.size.x;
            float w = Mathf.Round(XpWidthPx * k);
            xpFill.gameObject.SetActive(w > 0f);
            xpFill.localScale = new Vector3(U(w) / px, U(3f) / px, 1f);
            xpFill.localPosition = new Vector3(U(-XpWidthPx * 0.5f + w * 0.5f), U(XpY), 0f);
            GameText.SetPlain(xpText, acc.xp + " / " + acc.need + " XP");

            int minutes = Mathf.FloorToInt(d.playSeconds / 60f);
            GameText.SetPlain(statValues[0], string.Format(Localization.Get("profile.time_hm"), minutes / 60, minutes % 60));
            GameText.SetPlain(statValues[1], d.totalRuns.ToString());
            GameText.SetPlain(statValues[2], d.bestScore.ToString());
            GameText.SetPlain(statValues[3], d.bestWave.ToString());
        }
    }
}
