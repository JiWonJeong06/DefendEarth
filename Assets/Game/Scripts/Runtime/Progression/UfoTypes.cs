using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// UFO 종류 하나(11/27 박치기 기준으로 다시 정함). 강한 순서가 아니라 플레이 방식이 다르게.
    /// 직업 효과와 함께(곱하거나 더해서) 적용된다. 해금은 플레이 레벨 + 골드(로비 시작 → 출격 준비 창).
    /// </summary>
    public sealed class UfoType
    {
        public string nameKey;
        public string descKey;
        /// <summary>해금 조건(플레이 레벨 + 골드, 11/27). 기본형은 처음부터.</summary>
        public UnlockRule unlock = new UnlockRule();
        /// <summary>GameAssets.ufoSprites 번호(임시 그림).</summary>
        public int sprite;
        /// <summary>그림 색 곱하기(같은 그림을 다르게 보이게, 임시).</summary>
        public Color tint = Color.white;
        /// <summary>몸통 판정 배율.</summary>
        public float bodyScale = 1f;
        /// <summary>돌진 속도 배율(멀리 날기).</summary>
        public float dashSpeed = 1f;
        /// <summary>돌진 피해 +.</summary>
        public int damage;
        /// <summary>단단한 몬스터(덩치형·보스)에게 피해 +.</summary>
        public int heavyDamage;
        /// <summary>운석 골드 +비율.</summary>
        public float meteorGold;
        /// <summary>동전이 날아오는 속도 배율(자석).</summary>
        public float coinMagnet = 1f;
        /// <summary>연쇄 단계(-1 = 기본값 그대로).</summary>
        public int chainDepth = -1;
    }

    /// <summary>UFO 종류 목록과 지금 고른 UFO. 번호는 저장에 쓰이므로 순서를 바꾸지 말고 뒤에만 붙인다.</summary>
    public static class UfoTypes
    {
        public static readonly UfoType[] All =
        {
            // 0 기본형: 균형
            new UfoType { nameKey = "ufo.basic.name", descKey = "ufo.basic.desc", sprite = 0 },
            // 1 넓은접시형: 몸통 1.5배(맞히기 쉬움), 돌진 15% 짧게 → 떼거리형이 몰려올 때
            new UfoType { nameKey = "ufo.wide.name", descKey = "ufo.wide.desc", unlock = new UnlockRule { level = 2, gold = 400 }, sprite = 1, bodyScale = 1.5f, dashSpeed = 0.85f },
            // 2 자석형: 운석 골드 +30%, 동전 2배 빨리 끌어옴, 연쇄는 1단계만 → 골드 모으기
            new UfoType { nameKey = "ufo.magnet.name", descKey = "ufo.magnet.desc", unlock = new UnlockRule { level = 3, gold = 600 }, sprite = 0, tint = new Color(0.75f, 0.78f, 0.9f), meteorGold = 0.3f, coinMagnet = 2f, chainDepth = 1 },
            // 3 드릴형: 피해 +1, 덩치형·보스 +2 더, 돌진 15% 멀리, 몸통 0.8배(정확히 노려야) → 단단한 몬스터
            new UfoType { nameKey = "ufo.drill.name", descKey = "ufo.drill.desc", unlock = new UnlockRule { level = 5, gold = 900 }, sprite = 2, bodyScale = 0.8f, dashSpeed = 1.15f, damage = 1, heavyDamage = 2 },
        };

        public static bool IsOwned(int index) => index == 0 || SaveSystem.Data.ownedUfos.Contains(index);

        /// <summary>지금 타는 UFO 번호(가지고 있지 않으면 기본형).</summary>
        public static int SelectedIndex
        {
            get
            {
                int i = SaveSystem.Data.selectedUfo;
                return i >= 0 && i < All.Length && IsOwned(i) ? i : 0;
            }
        }

        public static UfoType Selected => All[SelectedIndex];

        public static Sprite SpriteOf(int index)
        {
            var a = GameAssets.Instance;
            var list = a != null ? a.ufoSprites : null;
            int s = All[index].sprite;
            if (list != null && s >= 0 && s < list.Length && list[s] != null) return list[s];
            return a != null ? a.ufoSprite : null;
        }

        /// <summary>그림 렌더러에 이 UFO 그림·색을 입힌다(타이틀·로비·로딩·게임 공용).</summary>
        public static void Apply(SpriteRenderer sr, int index)
        {
            if (sr == null) return;
            sr.sprite = SpriteOf(index);
            sr.color = All[index].tint;
        }
    }
}
