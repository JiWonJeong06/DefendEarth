using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 강화(판 밖 성장): 골드로 지구 체력·연사·운석 골드를 단계별로 올린다. 뽑기 없음.
    /// (데모용 간단 버전. 1월에 UFO별 3트랙 강화로 확장)
    /// </summary>
    public sealed class UpgradePanel : ModalPanel
    {
        private sealed class Row
        {
            public int index;
            public readonly List<SpriteRenderer> pips = new List<SpriteRenderer>();
            public PixelButton buy;
        }

        private readonly List<Row> rows = new List<Row>();
        private TextMeshPro gold;

        /// <summary>골드를 썼을 때(로비의 골드 숫자를 갱신).</summary>
        public event Action GoldSpent;

        public static UpgradePanel Create(Transform parent, int sortingOrder)
        {
            var go = new GameObject("UpgradePanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<UpgradePanel>();
            p.Build(new Vector2(170f, 250f), "ui.upgrade", sortingOrder);
            p.BuildContent();
            return p;
        }

        private void BuildContent()
        {
            var a = GameAssets.Instance;
            // 가진 골드: 동전 + 숫자 (숫자 상자는 왼쪽 정렬, 상자 왼쪽 끝 = 동전 오른쪽)
            CreateSprite("Coin", a.coinSprite, new Vector2(-24f, 96f));
            gold = CreateText("Gold", new Vector2(-18f + 40f, 96f), GameText.Style.Title, TextAlignmentOptions.MidlineLeft, 80f);
            gold.color = new Color(1f, 0.86f, 0.4f);

            var ups = Config.upgrades;
            for (int i = 0; i < ups.Length; i++)
            {
                var u = ups[i];
                float y = 66f - i * 62f;
                var row = new Row { index = i };

                var name = CreateText("Name" + i, new Vector2(0f, y), GameText.Style.Title, TextAlignmentOptions.MidlineLeft, 150f);
                GameText.SetKey(name, u.nameKey);
                var desc = CreateText("Desc" + i, new Vector2(0f, y - 14f), GameText.Style.Body, TextAlignmentOptions.MidlineLeft, 150f);
                GameText.SetKey(desc, u.descKey);
                desc.color = new Color(0.75f, 0.8f, 1f);

                for (int p = 0; p < u.MaxLevel; p++)
                {
                    var pip = CreateSprite("Pip", a.pixelSprite, new Vector2(-71f + p * 7f, y - 30f));
                    pip.transform.localScale = new Vector3(U(5f) / a.pixelSprite.bounds.size.x, U(5f) / a.pixelSprite.bounds.size.y, 1f);
                    row.pips.Add(pip);
                }

                row.buy = CreateButton("Buy" + i, new Vector2(42f, y - 30f), new Vector2(66f, 16f), null, new Color(0.35f, 0.7f, 0.4f), GameText.Style.Body);
                var label = GameText.Create("Label", row.buy.transform, GameText.Style.Body, Order + 8);
                int index = i;
                row.buy.Clicked += () => Buy(index);
                rows.Add(row);
                labelOf[row] = label; // 가격·"최대"는 Refresh에서 채움
            }
        }

        private readonly Dictionary<Row, TextMeshPro> labelOf = new Dictionary<Row, TextMeshPro>();

        private void Buy(int index)
        {
            var u = Config.upgrades[index];
            var save = SaveSystem.Data;
            int level = save.UpgradeLevel(index);
            if (level >= u.MaxLevel) return;
            int cost = u.costs[level];
            if (save.gold < cost) return;
            save.gold -= cost;
            save.SetUpgradeLevel(index, level + 1);
            SaveSystem.Save();
            GoldSpent?.Invoke();
            Refresh();
        }

        private void Refresh()
        {
            var save = SaveSystem.Data;
            GameText.SetPlain(gold, save.gold.ToString());
            foreach (var row in rows)
            {
                var u = Config.upgrades[row.index];
                int level = save.UpgradeLevel(row.index);
                for (int p = 0; p < row.pips.Count; p++)
                    row.pips[p].color = p < level ? new Color(0.45f, 0.9f, 1f) : new Color(0.25f, 0.27f, 0.4f);

                var label = labelOf[row];
                if (level >= u.MaxLevel)
                {
                    GameText.SetKey(label, "ui.upgrade.max");
                    row.buy.Interactable = false;
                }
                else
                {
                    int cost = u.costs[level];
                    GameText.SetPlain(label, cost + " G");
                    row.buy.Interactable = save.gold >= cost;
                }
            }
        }

        protected override void OnOpened() => Refresh();
    }

    /// <summary>
    /// 캐릭터 꾸미기(프로필용, 게임에 영향 없음): 머리 모양·머리색·머리 장식·눈 모양·눈 색·피부색·옷을 고른다. 데모는 모두 무료.
    /// 자유 그림·글 입력은 넣지 않는다(정해진 부품만). 프로필 창의 "꾸미기"로 연다.
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

        public static CustomizePanel Create(Transform parent, int sortingOrder)
        {
            var go = new GameObject("CustomizePanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<CustomizePanel>();
            p.Build(new Vector2(170f, 290f), "ui.custom", sortingOrder);
            p.BuildContent();
            return p;
        }

        private void BuildContent()
        {
            preview = PilotView.Create(Content, new Vector3(0f, U(90f), 0f), 2, Order + 4);

            for (int i = 0; i < Parts; i++)
            {
                int index = i;
                float y = 40f - i * 20f;
                var label = CreateText("Label" + i, new Vector2(0f, y), GameText.Style.Body, TextAlignmentOptions.MidlineLeft, 150f);
                GameText.SetKey(label, Keys[i]);
                label.color = new Color(0.75f, 0.8f, 1f);

                var prev = CreateButton("Prev" + i, new Vector2(14f, y), new Vector2(18f, 16f), null, new Color(0.3f, 0.4f, 0.7f));
                GameText.SetPlain(GameText.Create("L", prev.transform, GameText.Style.Title, Order + 8), "<");
                prev.Clicked += () => Change(index, -1);
                var next = CreateButton("Next" + i, new Vector2(66f, y), new Vector2(18f, 16f), null, new Color(0.3f, 0.4f, 0.7f));
                GameText.SetPlain(GameText.Create("R", next.transform, GameText.Style.Title, Order + 8), ">");
                next.Clicked += () => Change(index, 1);

                values[i] = CreateText("Value" + i, new Vector2(40f, y), GameText.Style.Body);
            }

            var note = CreateText("Note", new Vector2(0f, -116f), GameText.Style.Body, TextAlignmentOptions.Center, 150f, true);
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

        private static int Get(SaveData d, int part)
        {
            switch (part)
            {
                case 0: return d.hairStyle;
                case 1: return d.hairColor;
                case 2: return d.headwear;
                case 3: return d.eyeStyle;
                case 4: return d.eyeColor;
                case 5: return d.skinColor;
                default: return d.outfit;
            }
        }

        private static void Set(SaveData d, int part, int v)
        {
            switch (part)
            {
                case 0: d.hairStyle = v; break;
                case 1: d.hairColor = v; break;
                case 2: d.headwear = v; break;
                case 3: d.eyeStyle = v; break;
                case 4: d.eyeColor = v; break;
                case 5: d.skinColor = v; break;
                default: d.outfit = v; break;
            }
        }

        private void Change(int part, int delta)
        {
            var d = SaveSystem.Data;
            Set(d, part, PilotView.Mod(Get(d, part) + delta, Count(part)));
            SaveSystem.Save();
            Refresh();
        }

        private void Refresh()
        {
            var d = SaveSystem.Data;
            for (int i = 0; i < Parts; i++)
            {
                int n = Count(i);
                int v = PilotView.Mod(Get(d, i), n);
                // 머리 장식 0번은 "없음"
                string text = i == 2 && v == 0 ? Localization.Get("ui.custom.none") : (v + 1) + " / " + n;
                GameText.SetPlain(values[i], text);
            }
            preview.Refresh();
        }

        protected override void OnOpened() => Refresh();
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
