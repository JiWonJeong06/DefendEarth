using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 캐릭터(인간 조종사) 얼굴 그림. 프로필용이라 게임 플레이에는 영향이 없다.
    /// 흰색 부품 그림(옷·얼굴·머리)에 저장된 색을 입혀 겹쳐 그린다. (더미 그림, 아트가 나오면 교체)
    /// </summary>
    public sealed class PilotView : MonoBehaviour
    {
        public static readonly Color[] HairColors =
        {
            new Color(0.22f, 0.16f, 0.12f), new Color(0.08f, 0.08f, 0.1f), new Color(0.85f, 0.65f, 0.3f),
            new Color(0.75f, 0.3f, 0.2f), new Color(0.45f, 0.6f, 1f), new Color(0.9f, 0.5f, 0.75f),
        };
        public static readonly Color[] SkinColors =
        {
            new Color(1f, 0.87f, 0.75f), new Color(0.96f, 0.78f, 0.62f), new Color(0.82f, 0.62f, 0.45f),
            new Color(0.6f, 0.42f, 0.3f), new Color(0.4f, 0.27f, 0.2f),
        };
        public static readonly Color[] SuitColors =
        {
            new Color(0.35f, 0.55f, 1f), new Color(0.95f, 0.45f, 0.35f), new Color(0.4f, 0.8f, 0.45f),
            new Color(0.95f, 0.8f, 0.3f), new Color(0.65f, 0.45f, 0.9f), new Color(0.85f, 0.88f, 0.92f),
        };

        private SpriteRenderer suit;
        private SpriteRenderer face;
        private SpriteRenderer eyes;
        private SpriteRenderer hair;

        /// <summary>얼굴 그림을 만든다. scale은 정수 배.</summary>
        public static PilotView Create(Transform parent, Vector3 localPos, int scale, int sortingOrder)
        {
            var go = new GameObject("Pilot");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * Mathf.Max(1, scale);
            var view = go.AddComponent<PilotView>();
            var a = GameAssets.Instance;
            view.suit = a.CreateSprite("Suit", a.pilotSuit, sortingOrder, go.transform);
            view.face = a.CreateSprite("Face", a.pilotFace, sortingOrder + 1, go.transform);
            view.eyes = a.CreateSprite("Eyes", a.pilotEyes, sortingOrder + 2, go.transform);
            view.hair = a.CreateSprite("Hair", null, sortingOrder + 3, go.transform);
            view.Refresh();
            return view;
        }

        /// <summary>저장된 꾸미기대로 다시 그린다.</summary>
        public void Refresh()
        {
            var d = SaveSystem.Data;
            var a = GameAssets.Instance;
            int styles = a.pilotHair != null ? a.pilotHair.Length : 0;
            hair.sprite = styles > 0 ? a.pilotHair[Mod(d.hairStyle, styles)] : null;
            hair.color = HairColors[Mod(d.hairColor, HairColors.Length)];
            face.color = SkinColors[Mod(d.skinColor, SkinColors.Length)];
            suit.color = SuitColors[Mod(d.suitColor, SuitColors.Length)];
        }

        public static int Mod(int v, int n) => n <= 0 ? 0 : ((v % n) + n) % n;
    }
}
