using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 짧은 효과. 모두 풀링해서 쓴다. 도트가 깨지지 않게 정수 픽셀 크기·도트 칸에 맞춰 그린다.
    /// - Hit: 맞은 자리 반짝(불꽃 그림).
    /// - Burst: 흰 네모 파편이 사방으로(+ 처치 때는 몇 개가 지구 쪽으로 천천히 떨어지며 남는다).
    /// - Ring: 흰 도트 원(충격파)이 빠르게 커지며 사라진다.
    /// - Ghost: 그림 하나를 잠깐 띄운다(UFO 잔상, 처치 순간 2배로 하얗게 커지는 모습).
    /// - DamageNumber: 도트 숫자가 통 튀어 오르며 사라진다.
    /// 몬스터가 하얗게 번쩍이는 건 Monster(SpriteFlash 셰이더)가, 역경직·흔들림·진동은 HitFeedback이 한다.
    /// </summary>
    public sealed class Effects : MonoBehaviour
    {
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static Effects instance;
        private ComponentPool<Spark> sparks;
        private ComponentPool<Shard> shards;
        private ComponentPool<RingFx> rings;
        private ComponentPool<GhostFx> ghosts;
        private ComponentPool<FloatingNumber> numbers;

        private void Awake()
        {
            instance = this;
            sparks = new ComponentPool<Spark>(CreateSpark, 16);
            shards = new ComponentPool<Shard>(CreateShard, 128);
            rings = new ComponentPool<RingFx>(CreateRing, 8);
            ghosts = new ComponentPool<GhostFx>(CreateGhost, 16);
            numbers = new ComponentPool<FloatingNumber>(CreateNumber, 12);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private T Make<T>(string name, Sprite sprite, int order, Material mat = null) where T : Component, IPooledEffect
        {
            var sr = GameAssets.Instance.CreateSprite(name, sprite, order, transform, mat);
            var c = sr.gameObject.AddComponent<T>();
            c.Setup(sr, this);
            return c;
        }

        private Spark CreateSpark() => Make<Spark>("Spark", GameAssets.Instance.sparkSprite, SortOrder.Effect);
        private Shard CreateShard() => Make<Shard>("Shard", GameAssets.Instance.pixelSprite, SortOrder.Effect + 1);
        private RingFx CreateRing() => Make<RingFx>("Ring", null, SortOrder.Effect + 2);
        private GhostFx CreateGhost() => Make<GhostFx>("Ghost", null, SortOrder.Effect, GameAssets.Instance.flashMaterial);

        private FloatingNumber CreateNumber()
        {
            var go = new GameObject("DamageNumber");
            go.transform.SetParent(transform, false);
            var n = go.AddComponent<FloatingNumber>();
            n.Setup(null, this);
            return n;
        }

        // ================= 부르는 곳 =================

        /// <summary>위치에 불꽃을 튀긴다. size 1 = 기본(9픽셀).</summary>
        public static void Hit(Vector2 position, float size = 1f, Color? color = null)
        {
            if (instance == null) return;
            instance.sparks.Get().Play(position, size, color ?? Color.white);
        }

        /// <summary>
        /// 흰 네모 파편을 사방으로 터뜨린다. 방향을 고르게 나눠(살짝 흔들어) 뭉치지 않게 하고,
        /// 빠른 것·느린 것을 섞어 안쪽까지 채운다. power 1 = 맞음, 2 이상 = 처치·보스.
        /// linger: 그중 몇 개는 천천히 지구 쪽으로 떨어지며 오래 남는다(여운).
        /// </summary>
        public static void Burst(Vector2 position, int count, float power = 1f, int linger = 0)
        {
            if (instance == null || count <= 0) return;
            var config = PlayArea.Config;
            float start = Random.Range(0f, 360f);
            float step = 360f / count;
            for (int i = 0; i < count; i++)
            {
                float rad = (start + step * i + Random.Range(-step * 0.4f, step * 0.4f)) * Mathf.Deg2Rad;
                float speed = config.PixelsToUnits((Random.value < 0.65f ? Random.Range(170f, 300f) : Random.Range(80f, 150f)) * power);
                Vector2 v = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
                float r = Random.value;
                int sizePx = r < 0.25f ? 4 : r < 0.6f ? 3 : 2;
                float life = Random.Range(0.3f, 0.5f) * Mathf.Lerp(1f, 1.25f, Mathf.Clamp01(power - 1f));
                instance.shards.Get().Play(position, v, sizePx, life, 4f, 120f);
            }
            for (int i = 0; i < linger; i++)
            {
                // 여운: 위·옆으로 살짝 튀었다가 천천히 아래(지구)로 떨어지며 깜빡이다 사라진다
                float rad = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                Vector2 v = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * config.PixelsToUnits(Random.Range(40f, 90f));
                instance.shards.Get().Play(position, v, Random.value < 0.5f ? 2 : 1, Random.Range(1.2f, 1.9f), 1.2f, 70f);
            }
        }

        /// <summary>흰 도트 원(충격파). maxRadiusPx까지 seconds 동안 커지며 사라진다(실제 시간, 역경직 중에도 보임).</summary>
        public static void Ring(Vector2 position, int maxRadiusPx, float seconds)
        {
            if (instance == null) return;
            instance.rings.Get().Play(position, maxRadiusPx, seconds);
        }

        /// <summary>
        /// 그림 하나를 잠깐 띄운다. white면 완전히 하얗게(번쩍), 아니면 color로 물들인다. 실제 시간 기준.
        /// </summary>
        public static void Ghost(Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale, Color color, float seconds, bool white, int order)
        {
            if (instance == null || sprite == null) return;
            instance.ghosts.Get().Play(sprite, position, rotation, scale, color, seconds, white, order);
        }

        /// <summary>피해 숫자가 통 튀어 오르며 사라진다.</summary>
        public static void DamageNumber(Vector2 position, int amount, Color color)
        {
            if (instance == null || amount <= 0) return;
            instance.numbers.Get().Play(position, amount, color);
        }

        internal void Release(Spark e) => sparks.Release(e);
        internal void Release(Shard e) => shards.Release(e);
        internal void Release(RingFx e) => rings.Release(e);
        internal void Release(GhostFx e) => ghosts.Release(e);
        internal void Release(FloatingNumber e) => numbers.Release(e);

        // ================= 효과들 =================

        public interface IPooledEffect
        {
            void Setup(SpriteRenderer renderer, Effects effects);
        }

        public sealed class Spark : MonoBehaviour, IPooledEffect
        {
            private const float Life = 0.14f;
            private SpriteRenderer sr;
            private Effects owner;
            private float t;
            private float size;
            private Color color;

            public void Setup(SpriteRenderer renderer, Effects effects) { sr = renderer; owner = effects; }

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

        /// <summary>흰 네모 파편 하나: 튀어 나가며 느려지고, 중력으로 떨어지고, 끝에서 한 칸씩 작아지며 사라진다.</summary>
        public sealed class Shard : MonoBehaviour, IPooledEffect
        {
            private SpriteRenderer sr;
            private Effects owner;
            private Vector2 pos;
            private Vector2 vel;
            private int sizePx;
            private float life;
            private float t;
            private float drag;
            private float gravityPx;

            public void Setup(SpriteRenderer renderer, Effects effects) { sr = renderer; owner = effects; }

            public void Play(Vector2 position, Vector2 velocity, int size, float lifetime, float dragPerSecond, float gravity)
            {
                pos = position;
                vel = velocity;
                sizePx = size;
                life = lifetime;
                drag = dragPerSecond;
                gravityPx = gravity;
                t = 0f;
                Apply();
            }

            private void Update()
            {
                float dt = Time.deltaTime;
                t += dt;
                if (t >= life) { owner.Release(this); return; }
                vel *= Mathf.Exp(-drag * dt);
                vel.y -= PlayArea.Config.PixelsToUnits(gravityPx) * dt;
                pos += vel * dt;
                Apply();
            }

            private void Apply()
            {
                var config = PlayArea.Config;
                float k = t / life;
                int s = Mathf.Max(1, Mathf.CeilToInt(sizePx * (1f - k * k)));
                float onePx = config.PixelsToUnits(1f);
                float px = sr.sprite.bounds.size.x;
                transform.localScale = new Vector3(onePx * s / px, onePx * s / px, 1f);
                transform.position = new Vector3(Mathf.Round(pos.x / onePx) * onePx, Mathf.Round(pos.y / onePx) * onePx, 0f);
                // 끝 30%는 깜빡이며 사라진다(오래 남는 파편이 자연스럽게 꺼지게)
                bool blinkOff = k > 0.7f && Mathf.FloorToInt(t * 20f) % 2 == 0;
                sr.color = new Color(1f, 1f, 1f, blinkOff ? 0f : 1f);
            }
        }

        /// <summary>충격파: 1픽셀 두께 흰 도트 원이 반지름 1부터 최대까지 커지며 옅어진다.</summary>
        public sealed class RingFx : MonoBehaviour, IPooledEffect
        {
            private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();
            private SpriteRenderer sr;
            private Effects owner;
            private float started;
            private float duration;
            private int maxRadius;

            public void Setup(SpriteRenderer renderer, Effects effects) { sr = renderer; owner = effects; }

            public void Play(Vector2 position, int maxRadiusPx, float seconds)
            {
                float onePx = PlayArea.Config.PixelsToUnits(1f);
                transform.position = new Vector3(Mathf.Round(position.x / onePx) * onePx, Mathf.Round(position.y / onePx) * onePx, 0f);
                maxRadius = Mathf.Max(2, maxRadiusPx);
                duration = Mathf.Max(0.05f, seconds);
                started = Time.unscaledTime;
                Apply();
            }

            private void Update()
            {
                if (Time.unscaledTime - started >= duration) { owner.Release(this); return; }
                Apply();
            }

            private void Apply()
            {
                float k = Mathf.Clamp01((Time.unscaledTime - started) / duration);
                float eased = 1f - (1f - k) * (1f - k); // 처음 빠르게 커진다
                int r = Mathf.Max(2, Mathf.RoundToInt(Mathf.Lerp(2f, maxRadius, eased)));
                sr.sprite = RingSprite(r);
                sr.color = new Color(1f, 1f, 1f, 1f - k * 0.8f);
            }

            /// <summary>반지름 r 픽셀짜리 1픽셀 두께 원 그림(처음 쓸 때 한 번 만들어 둔다).</summary>
            private static Sprite RingSprite(int r)
            {
                if (Cache.TryGetValue(r, out var s) && s != null) return s;
                int size = r * 2 + 3;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[size * size];
                float c = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                        px[y * size + x] = Mathf.Abs(d - r) < 0.55f ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                    }
                tex.SetPixels32(px);
                tex.Apply(false);
                s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PlayArea.Config.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                Cache[r] = s;
                return s;
            }
        }

        /// <summary>그림 하나를 잠깐: UFO 잔상(옅어짐), 처치 순간 2배 흰 모습(번쩍).</summary>
        public sealed class GhostFx : MonoBehaviour, IPooledEffect
        {
            private SpriteRenderer sr;
            private Effects owner;
            private MaterialPropertyBlock block;
            private float started;
            private float duration;
            private Color color;

            public void Setup(SpriteRenderer renderer, Effects effects)
            {
                sr = renderer;
                owner = effects;
                block = new MaterialPropertyBlock();
            }

            public void Play(Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale, Color c, float seconds, bool white, int order)
            {
                sr.sprite = sprite;
                sr.sortingOrder = order;
                transform.SetPositionAndRotation(position, rotation);
                transform.localScale = scale;
                color = c;
                duration = Mathf.Max(0.02f, seconds);
                started = Time.unscaledTime;
                block.Clear();
                block.SetFloat(FlashId, white ? 1f : 0f);
                sr.SetPropertyBlock(block);
                sr.color = color;
            }

            private void Update()
            {
                float k = (Time.unscaledTime - started) / duration;
                if (k >= 1f) { owner.Release(this); return; }
                var c = color;
                c.a = color.a * (1f - k);
                sr.color = c;
            }
        }

        /// <summary>피해 숫자: 2배 크기 도트 숫자 + 1픽셀 그림자. 위로 통 튀어 오르다 멈추고 깜빡이며 사라진다.</summary>
        public sealed class FloatingNumber : MonoBehaviour, IPooledEffect
        {
            private const float Life = 0.6f;
            private const int Scale = 2;          // 3×5 글자 → 6×10 (정수 배율)
            private const float RisePx = 14f;
            private PixelNumber number;
            private PixelNumber shadow;
            private Effects owner;
            private Vector2 start;
            private float t;
            private Color color;

            public void Setup(SpriteRenderer renderer, Effects effects)
            {
                owner = effects;
                float one = PlayArea.Config.PixelsToUnits(1f);
                shadow = Child("Shadow", SortOrder.Effect + 5, new Vector3(one, -one, 0f));
                number = Child("Number", SortOrder.Effect + 6, Vector3.zero);
            }

            private PixelNumber Child(string name, int order, Vector3 offset)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = offset;
                go.transform.localScale = Vector3.one * Scale;
                var n = go.AddComponent<PixelNumber>();
                n.SetSortingOrder(order);
                return n;
            }

            public void Play(Vector2 position, int amount, Color c)
            {
                start = position;
                color = c;
                t = 0f;
                number.Configure(PixelNumber.Align.Center, c);
                number.SetValue(amount);
                shadow.Configure(PixelNumber.Align.Center, new Color(0.05f, 0.03f, 0.12f, 0.9f));
                shadow.SetValue(amount);
                Apply();
            }

            private void Update()
            {
                t += Time.unscaledDeltaTime;
                if (t >= Life) { owner.Release(this); return; }
                Apply();
            }

            private void Apply()
            {
                var config = PlayArea.Config;
                float k = t / Life;
                // 빠르게 10픽셀 올라갔다가 멈춘다(정수 픽셀)
                float up = Mathf.Round(RisePx * (1f - (1f - Mathf.Min(1f, k * 2.5f)) * (1f - Mathf.Min(1f, k * 2.5f))));
                float onePx = config.PixelsToUnits(1f);
                Vector2 p = start + new Vector2(0f, up * onePx);
                transform.position = new Vector3(Mathf.Round(p.x / onePx) * onePx, Mathf.Round(p.y / onePx) * onePx, 0f);
                bool off = k > 0.65f && Mathf.FloorToInt(t * 20f) % 2 == 0;
                number.Configure(PixelNumber.Align.Center, new Color(color.r, color.g, color.b, off ? 0f : 1f));
                shadow.Configure(PixelNumber.Align.Center, new Color(0.05f, 0.03f, 0.12f, off ? 0f : 0.9f));
            }
        }
    }
}
