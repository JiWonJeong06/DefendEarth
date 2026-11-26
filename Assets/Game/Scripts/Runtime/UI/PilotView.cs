using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 꾸민 캐릭터(SD 전신 도트). 프로필용이라 게임 플레이에는 영향이 없다.
    /// 부품 5가지: 몸통(피부색), 머리카락(뒷머리·앞머리 한 쌍 + 머리색), 머리 장식, 눈(+ 눈 색), 옷.
    /// 모든 부품 그림은 같은 캔버스(24×36, 가운데 기준)라 겹치기만 하면 위치가 맞는다.
    /// 겹치는 순서(뒤 → 앞): 뒷머리, 몸통, 옷, 눈, 앞머리, 머리 장식.
    /// 몸통·머리카락·눈(·동물 귀 장식)은 흰/회색 그림에 색을 입히고, 옷·나머지 장식은 그림 색 그대로.
    /// 아트가 나오면 GameAssets의 부품 그림만 바꾸면 된다(Assets/Art/Character/에 넣으면 도트 설정 자동).
    /// </summary>
    public sealed class PilotView : MonoBehaviour
    {
        public static readonly Color[] HairColors =
        {
            new Color(0.24f, 0.18f, 0.14f), new Color(0.12f, 0.12f, 0.16f), new Color(0.9f, 0.6f, 0.25f),
            new Color(0.95f, 0.85f, 0.5f), new Color(0.45f, 0.6f, 1f), new Color(0.9f, 0.5f, 0.75f),
            new Color(0.85f, 0.85f, 0.9f), new Color(0.8f, 0.3f, 0.25f),
        };
        public static readonly Color[] SkinColors =
        {
            new Color(1f, 0.88f, 0.77f), new Color(0.98f, 0.8f, 0.66f), new Color(0.85f, 0.65f, 0.48f),
            new Color(0.64f, 0.45f, 0.32f), new Color(0.42f, 0.29f, 0.21f),
        };
        public static readonly Color[] EyeColors =
        {
            new Color(0.3f, 0.55f, 1f), new Color(0.25f, 0.7f, 0.35f), new Color(0.55f, 0.35f, 0.2f),
            new Color(0.85f, 0.3f, 0.55f), new Color(0.55f, 0.35f, 0.9f), new Color(0.2f, 0.2f, 0.25f),
        };

        /// <summary>캔버스 크기(기준 픽셀).</summary>
        public const int CanvasWidth = 24;
        public const int CanvasHeight = 36;

        private SpriteRenderer backHair;
        private SpriteRenderer body;
        private SpriteRenderer outfit;
        private SpriteRenderer eyes;
        private SpriteRenderer frontHair;
        private SpriteRenderer headwear;

        /// <summary>캐릭터를 만든다. scale은 정수 배(도트가 깨지지 않게). localPos는 캐릭터 가운데.</summary>
        public static PilotView Create(Transform parent, Vector3 localPos, int scale, int sortingOrder)
        {
            var go = new GameObject("Pilot");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * Mathf.Max(1, scale);
            var view = go.AddComponent<PilotView>();
            var a = GameAssets.Instance;
            view.backHair = a.CreateSprite("BackHair", null, sortingOrder, go.transform);
            view.body = a.CreateSprite("Body", a.charBody, sortingOrder + 1, go.transform);
            view.outfit = a.CreateSprite("Outfit", null, sortingOrder + 2, go.transform);
            view.eyes = a.CreateSprite("Eyes", null, sortingOrder + 3, go.transform);
            view.frontHair = a.CreateSprite("FrontHair", null, sortingOrder + 4, go.transform);
            view.headwear = a.CreateSprite("Headwear", null, sortingOrder + 5, go.transform);
            view.Refresh();
            return view;
        }

        // ---- 부품 개수(꾸미기 창이 쓴다) ----
        public static int HairStyleCount => Mathf.Max(1, Mathf.Min(Len(GameAssets.Instance.charHairBack), Len(GameAssets.Instance.charHairFront)));
        public static int EyeStyleCount => Mathf.Max(1, Len(GameAssets.Instance.charEyes));
        public static int OutfitCount => Mathf.Max(1, Len(GameAssets.Instance.charOutfits));
        /// <summary>머리 장식 개수(0번 = 없음 포함).</summary>
        public static int HeadwearCount => 1 + Len(GameAssets.Instance.charHeadwear);

        /// <summary>저장된 꾸미기대로 다시 그린다.</summary>
        public void Refresh() => Refresh(CharacterLook.FromSave());

        /// <summary>주어진 모습대로 그린다(꾸미기 창에서 적용 전 미리보기).</summary>
        public void Refresh(CharacterLook d)
        {
            var a = GameAssets.Instance;
            Color hairColor = HairColors[Mod(d.hairColor, HairColors.Length)];

            int hs = Mod(d.hairStyle, HairStyleCount);
            backHair.sprite = Pick(a.charHairBack, hs);
            frontHair.sprite = Pick(a.charHairFront, hs);
            backHair.color = frontHair.color = hairColor;

            body.sprite = a.charBody;
            body.color = SkinColors[Mod(d.skinColor, SkinColors.Length)];

            eyes.sprite = Pick(a.charEyes, Mod(d.eyeStyle, EyeStyleCount));
            eyes.color = EyeColors[Mod(d.eyeColor, EyeColors.Length)];

            outfit.sprite = Pick(a.charOutfits, Mod(d.outfit, OutfitCount));
            outfit.color = Color.white;

            int hw = Mod(d.headwear, HeadwearCount); // 0 = 없음
            headwear.sprite = hw == 0 ? null : Pick(a.charHeadwear, hw - 1);
            bool tinted = hw > 0 && a.charHeadwearUsesHairColor != null && hw - 1 < a.charHeadwearUsesHairColor.Length && a.charHeadwearUsesHairColor[hw - 1];
            headwear.color = tinted ? hairColor : Color.white;
        }

        private static int Len(Sprite[] list) => list != null ? list.Length : 0;
        private static Sprite Pick(Sprite[] list, int i) => list != null && i >= 0 && i < list.Length ? list[i] : null;

        public static int Mod(int v, int n) => n <= 0 ? 0 : ((v % n) + n) % n;
    }

    /// <summary>캐릭터 모습 7가지 값(저장과 꾸미기 미리보기 사이에서 주고받는다).</summary>
    public struct CharacterLook
    {
        public int hairStyle, hairColor, headwear, eyeStyle, eyeColor, skinColor, outfit;

        public static CharacterLook FromSave()
        {
            var d = SaveSystem.Data;
            return new CharacterLook
            {
                hairStyle = d.hairStyle, hairColor = d.hairColor, headwear = d.headwear, eyeStyle = d.eyeStyle,
                eyeColor = d.eyeColor, skinColor = d.skinColor, outfit = d.outfit,
            };
        }

        public void WriteTo(SaveData d)
        {
            d.hairStyle = hairStyle; d.hairColor = hairColor; d.headwear = headwear; d.eyeStyle = eyeStyle;
            d.eyeColor = eyeColor; d.skinColor = skinColor; d.outfit = outfit;
        }

        /// <summary>꾸미기 창 줄 번호(0 머리 모양 … 6 옷)로 읽고 쓰기.</summary>
        public int Get(int part)
        {
            switch (part)
            {
                case 0: return hairStyle;
                case 1: return hairColor;
                case 2: return headwear;
                case 3: return eyeStyle;
                case 4: return eyeColor;
                case 5: return skinColor;
                default: return outfit;
            }
        }

        public void Set(int part, int v)
        {
            switch (part)
            {
                case 0: hairStyle = v; break;
                case 1: hairColor = v; break;
                case 2: headwear = v; break;
                case 3: eyeStyle = v; break;
                case 4: eyeColor = v; break;
                case 5: skinColor = v; break;
                default: outfit = v; break;
            }
        }

        public bool SameAs(CharacterLook o) =>
            hairStyle == o.hairStyle && hairColor == o.hairColor && headwear == o.headwear && eyeStyle == o.eyeStyle &&
            eyeColor == o.eyeColor && skinColor == o.skinColor && outfit == o.outfit;
    }
}
