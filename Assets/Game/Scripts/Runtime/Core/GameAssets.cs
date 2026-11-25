using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 코드에서 만드는 오브젝트(탄·몬스터·효과)가 쓰는 그림과 재질 모음.
    /// 장면 만들기 메뉴가 연결해 준다.
    /// </summary>
    [DefaultExecutionOrder(-950)]
    public sealed class GameAssets : MonoBehaviour
    {
        public static GameAssets Instance { get; private set; }

        public Sprite bulletSprite;
        public Sprite sparkSprite;
        public Sprite pixelSprite;
        [Tooltip("빛 영향 없는 기본 스프라이트 재질")]
        public Material spriteMaterial;
        [Tooltip("맞으면 하얗게 번쩍이는 재질(몬스터용)")]
        public Material flashMaterial;
        [Tooltip("일반 몬스터들")]
        public MonsterDefinition[] monsters;
        public MonsterDefinition boss;
        public MonsterDefinition meteor;
        public Sprite coinSprite;
        [Tooltip("숫자 0~9 도트 그림(점수·골드 표시)")]
        public Sprite[] digitSprites = new Sprite[10];

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public SpriteRenderer CreateSprite(string name, Sprite sprite, int order, Transform parent = null, Material material = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            var mat = material != null ? material : spriteMaterial;
            if (mat != null) sr.sharedMaterial = mat;
            return sr;
        }
    }

    /// <summary>그리는 순서(클수록 앞).</summary>
    public static class SortOrder
    {
        public const int Background = -100;
        public const int Earth = -50;
        public const int Monster = 0;
        public const int Bullet = 5;
        public const int Ufo = 10;
        public const int Effect = 20;
        public const int Hud = 50;
        public const int Overlay = 100;
    }
}
