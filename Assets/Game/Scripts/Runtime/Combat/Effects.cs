using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 맞았을 때 튀는 작은 불꽃 같은 짧은 효과. 풀링해서 쓴다.
    /// </summary>
    public sealed class Effects : MonoBehaviour
    {
        private static Effects instance;
        private ComponentPool<Spark> sparks;

        private void Awake()
        {
            instance = this;
            sparks = new ComponentPool<Spark>(CreateSpark, 16);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private Spark CreateSpark()
        {
            var sr = GameAssets.Instance.CreateSprite("Spark", GameAssets.Instance.sparkSprite, SortOrder.Effect, transform);
            var spark = sr.gameObject.AddComponent<Spark>();
            spark.Setup(sr, this);
            return spark;
        }

        /// <summary>위치에 불꽃을 튀긴다. size 1 = 기본(9픽셀).</summary>
        public static void Hit(Vector2 position, float size = 1f, Color? color = null)
        {
            if (instance == null) return;
            var spark = instance.sparks.Get();
            spark.Play(position, size, color ?? Color.white);
        }

        internal void Release(Spark spark) => sparks.Release(spark);

        public sealed class Spark : MonoBehaviour
        {
            private const float Life = 0.14f;
            private SpriteRenderer sr;
            private Effects owner;
            private float t;
            private float size;
            private Color color;

            public void Setup(SpriteRenderer renderer, Effects effects)
            {
                sr = renderer;
                owner = effects;
            }

            public void Play(Vector2 position, float s, Color c)
            {
                transform.position = position;
                transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0, 4) * 90f); // 90도 단위만(비스듬히 돌리면 도트가 깨짐)
                size = s;
                color = c;
                t = 0f;
                Apply();
            }

            private void Update()
            {
                t += Time.deltaTime;
                if (t >= Life) { owner.Release(this); return; }
                Apply();
            }

            private void Apply()
            {
                float k = t / Life;
                transform.localScale = Vector3.one * size * Mathf.Lerp(0.6f, 1.3f, k);
                var c = color;
                c.a = 1f - k * k;
                sr.color = c;
            }
        }
    }
}
