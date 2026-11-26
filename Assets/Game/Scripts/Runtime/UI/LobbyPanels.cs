using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 캐릭터 꾸미기(프로필용, 게임에 영향 없음): 머리 모양·머리색·머리 장식·눈 모양·눈 색·피부색·옷.
    /// 바꾸는 동안은 미리보기만 바뀌고, "적용"을 눌러야 저장된다(11/27). "되돌리기"는 창을 열었을 때(또는 마지막 적용) 모습으로.
    /// 적용하지 않고 닫으면 "적용하지 않고 나갈까요?"를 묻는다. 자유 그림·글 입력은 넣지 않는다(정해진 부품만).
    /// </summary>
    public sealed class CustomizePanel : ModalPanel
    {
        private const int Parts = 7;
        private PilotView preview;
        private readonly TextMeshPro[] values = new TextMeshPro[Parts];
        private static readonly string[] Keys =
        {
            "ui.custom.hairstyle", "ui.custom.haircolor", "ui.custom.headwear", "ui.custom.eyes",
            "ui.custom.eyecolor", "ui.custom.skin", "ui.custom.outfit",
        };

        private CharacterLook saved;    // 창을 열 때(또는 마지막 적용) 모습
        private CharacterLook working;  // 지금 고르는 중인 모습
        private PixelButton applyButton;
        private PixelButton revertButton;
        private ConfirmPanel confirm;
        private bool forceClose;

        private bool Dirty => !working.SameAs(saved);

        public static CustomizePanel Create(Transform parent, int sortingOrder)
        {
            var go = new GameObject("CustomizePanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<CustomizePanel>();
            p.Build(new Vector2(170f, 290f), "ui.custom", sortingOrder);
            p.BuildContent();
            p.confirm = ConfirmPanel.Create(parent, sortingOrder + 40);
            return p;
        }

        private void BuildContent()
        {
            preview = PilotView.Create(Content, new Vector3(0f, U(90f), 0f), 2, Order + 4);

            for (int i = 0; i < Parts; i++)
            {
                int index = i;
                float y = 44f - i * 19f;
                var label = CreateText("Label" + i, new Vector2(0f, y), GameText.Style.Body, TextAlignmentOptions.MidlineLeft, 150f);
                GameText.SetKey(label, Keys[i]);
                label.color = new Color(0.75f, 0.8f, 1f);

                var prev = CreateButton("Prev" + i, new Vector2(14f, y), new Vector2(18f, 15f), null, new Color(0.3f, 0.4f, 0.7f));
                GameText.SetPlain(GameText.Create("L", prev.transform, GameText.Style.Title, Order + 8), "<");
                prev.Clicked += () => Change(index, -1);
                var next = CreateButton("Next" + i, new Vector2(66f, y), new Vector2(18f, 15f), null, new Color(0.3f, 0.4f, 0.7f));
                GameText.SetPlain(GameText.Create("R", next.transform, GameText.Style.Title, Order + 8), ">");
                next.Clicked += () => Change(index, 1);

                values[i] = CreateText("Value" + i, new Vector2(40f, y), GameText.Style.Body);
            }

            revertButton = CreateButton("Revert", new Vector2(-38f, -98f), new Vector2(68f, 18f), "ui.custom.revert", new Color(0.45f, 0.5f, 0.6f), GameText.Style.Body);
            revertButton.Clicked += Revert;
            applyButton = CreateButton("Apply", new Vector2(38f, -98f), new Vector2(68f, 18f), "ui.custom.apply", new Color(0.3f, 0.75f, 0.45f), GameText.Style.Body);
            applyButton.Clicked += ApplyLook;

            var note = CreateText("Note", new Vector2(0f, -122f), GameText.Style.Body, TextAlignmentOptions.Center, 150f, true);
            GameText.SetKey(note, "ui.custom.note");
            note.color = new Color(0.6f, 0.65f, 0.8f);
        }

        private static int Count(int part)
        {
            switch (part)
            {
                case 0: return PilotView.HairStyleCount;
                case 1: return PilotView.HairColors.Length;
                case 2: return PilotView.HeadwearCount;
                case 3: return PilotView.EyeStyleCount;
                case 4: return PilotView.EyeColors.Length;
                case 5: return PilotView.SkinColors.Length;
                default: return PilotView.OutfitCount;
            }
        }

        private void Change(int part, int delta)
        {
            working.Set(part, PilotView.Mod(working.Get(part) + delta, Count(part)));
            Refresh();
        }

        private void ApplyLook()
        {
            working.WriteTo(SaveSystem.Data);
            SaveSystem.Save();
            saved = working;
            Sound.Play(Sfx.CardPick);
            Refresh();
        }

        private void Revert()
        {
            working = saved;
            Refresh();
        }

        /// <summary>적용 안 한 바뀐 것이 있으면 먼저 묻는다(예 = 버리고 닫기, 아니요 = 계속 꾸미기).</summary>
        public override void Close()
        {
            if (IsOpen && Dirty && !forceClose)
            {
                confirm.Ask("ui.custom.discard_confirm", () =>
                {
                    working = saved;
                    forceClose = true;
                    Close();
                    forceClose = false;
                });
                return;
            }
            base.Close();
        }

        private void Refresh()
        {
            for (int i = 0; i < Parts; i++)
            {
                int n = Count(i);
                int v = PilotView.Mod(working.Get(i), n);
                // 머리 장식 0번은 "없음"
                string text = i == 2 && v == 0 ? Localization.Get("ui.custom.none") : (v + 1) + " / " + n;
                GameText.SetPlain(values[i], text);
            }
            preview.Refresh(working);
            bool dirty = Dirty;
            applyButton.Interactable = dirty;
            revertButton.Interactable = dirty;
            applyButton.SetColor(dirty ? new Color(0.3f, 0.75f, 0.45f) : new Color(0.25f, 0.32f, 0.3f));
            revertButton.SetColor(dirty ? new Color(0.45f, 0.5f, 0.6f) : new Color(0.25f, 0.27f, 0.32f));
        }

        protected override void OnOpened()
        {
            saved = CharacterLook.FromSave();
            working = saved;
            Refresh();
        }
    }

    /// <summary>
    /// 순위표(데모): 이 기기 최고 기록 TOP 10. 온라인 순위표는 1월에 Google Play 게임즈로(SDK 추가 전 확인).
    /// </summary>
    public sealed class LeaderboardPanel : ModalPanel
    {
        private readonly List<TextMeshPro[]> lines = new List<TextMeshPro[]>();
        private TextMeshPro empty;

        public static LeaderboardPanel Create(Transform parent, int sortingOrder)
        {
            var go = new GameObject("LeaderboardPanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<LeaderboardPanel>();
            p.Build(new Vector2(170f, 250f), "ui.ranking", sortingOrder);
            p.BuildContent();
            return p;
        }

        private void BuildContent()
        {
            var sub = CreateText("Sub", new Vector2(0f, 98f), GameText.Style.Body, TextAlignmentOptions.Center, 156f, true);
            GameText.SetKey(sub, "ui.ranking.local");
            sub.color = new Color(0.6f, 0.65f, 0.8f);

            var hRank = CreateText("HRank", new Vector2(0f, 80f), GameText.Style.Body, TextAlignmentOptions.MidlineLeft, 150f);
            GameText.SetPlain(hRank, "#");
            var hScore = CreateText("HScore", new Vector2(-6f, 80f), GameText.Style.Body, TextAlignmentOptions.MidlineRight, 90f);
            GameText.SetKey(hScore, "ui.score");
            var hWave = CreateText("HWave", new Vector2(0f, 80f), GameText.Style.Body, TextAlignmentOptions.MidlineRight, 150f);
            GameText.SetKey(hWave, "ui.wave");
            foreach (var h in new[] { hRank, hScore, hWave }) h.color = new Color(0.75f, 0.8f, 1f);

            for (int i = 0; i < 10; i++)
            {
                float y = 64f - i * 16f;
                var rank = CreateText("Rank" + i, new Vector2(0f, y), GameText.Style.Body, TextAlignmentOptions.MidlineLeft, 150f);
                var score = CreateText("Score" + i, new Vector2(-6f, y), GameText.Style.Title, TextAlignmentOptions.MidlineRight, 90f);
                var wave = CreateText("Wave" + i, new Vector2(0f, y), GameText.Style.Body, TextAlignmentOptions.MidlineRight, 150f);
                lines.Add(new[] { rank, score, wave });
            }
            empty = CreateText("Empty", new Vector2(0f, 20f), GameText.Style.Body, TextAlignmentOptions.Center, 150f, true);
            GameText.SetKey(empty, "ui.ranking.empty");
        }

        protected override void OnOpened()
        {
            var list = SaveSystem.Data.topScores;
            for (int i = 0; i < lines.Count; i++)
            {
                bool on = i < list.Count;
                foreach (var t in lines[i]) t.gameObject.SetActive(on);
                if (!on) continue;
                var color = i == 0 ? new Color(1f, 0.86f, 0.4f) : i < 3 ? new Color(0.85f, 0.9f, 1f) : new Color(0.7f, 0.72f, 0.8f);
                GameText.SetPlain(lines[i][0], (i + 1).ToString());
                GameText.SetPlain(lines[i][1], list[i].score.ToString());
                GameText.SetPlain(lines[i][2], list[i].wave.ToString());
                foreach (var t in lines[i]) t.color = color;
            }
            empty.gameObject.SetActive(list.Count == 0);
        }
    }
}
