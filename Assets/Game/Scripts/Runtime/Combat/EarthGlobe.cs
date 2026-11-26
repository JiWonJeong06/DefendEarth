using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 스스로 도는 도트 지구. 평평한 세계 지도(코드로 만든 바다·땅·구름)를 공 모양에 감아
    /// 매 프레임 지도를 옆으로 밀어서 자전하는 3D처럼 보이게 한다.
    /// 화면 아래를 꽉 채우는 큰 지구(윗부분만 보임)와, 작은 지구 전체 둘 다 만들 수 있다.
    /// (아트가 나오면 지도 그림만 바꾸면 된다)
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EarthGlobe : MonoBehaviour
    {
        private const int MapW = 256;
        private const int MapH = 128;

        private static readonly Color32 Sea = new Color32(74, 128, 232, 255);
        private static readonly Color32 SeaDeep = new Color32(52, 96, 204, 255);
        private static readonly Color32 Land = new Color32(118, 214, 40, 255);
        private static readonly Color32 LandDark = new Color32(76, 160, 30, 255);
        private static readonly Color32 Ice = new Color32(236, 242, 250, 255);
        private static readonly Color32 Cloud = new Color32(240, 240, 244, 255);
        private static readonly Color32 Rim = new Color32(150, 210, 255, 255);
        private static readonly Color32 Glow = new Color32(120, 190, 255, 110);
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        private static Color32[] groundMap; // 바다·땅(모든 지구가 같이 씀)
        private static bool[] cloudMap;

        private Texture2D texture;
        private Color32[] pixels;
        private int[] mapV;        // 픽셀마다 지도 세로 위치 (-1 = 지구 밖)
        private float[] mapU;      // 픽셀마다 지도 가로 위치(회전 전)
        private byte[] shade;      // 0 밝음, 1 보통, 2 어두움, 3 테두리, 4 대기
        private float spinSeconds = 90f;
        private int lastOffset = int.MinValue;

        /// <summary>화면 아래를 꽉 채우는 큰 지구(윗부분만). 아래 가운데가 parent 기준 bottomY.</summary>
        public static EarthGlobe CreateHorizon(string name, Transform parent, int order, float bottomY)
        {
            var config = PlayArea.Config;
            int w = config.playAreaPixels.x;
            int h = Mathf.CeilToInt(config.earthVisiblePx) + 2;
            var globe = Create(name, parent, order, w, h, w * 0.5f, config.earthVisiblePx - config.earthRadiusPx, config.earthRadiusPx, new Vector2(0.5f, 0f));
            globe.transform.localPosition = new Vector3(0f, bottomY, 0f);
            globe.spinSeconds = config.earthSpinSeconds;
            return globe;
        }

        /// <summary>작은 지구 전체(가운데 기준).</summary>
        public static EarthGlobe CreateWhole(string name, Transform parent, int order, float radiusPx)
        {
            int size = Mathf.CeilToInt(radiusPx * 2f) + 4;
            var globe = Create(name, parent, order, size, size, size * 0.5f, size * 0.5f, radiusPx, new Vector2(0.5f, 0.5f));
            globe.spinSeconds = PlayArea.Config.earthSpinSeconds * 0.5f;
            return globe;
        }

        /// <summary>
        /// 텍스처 w×h 픽셀, 지구 중심은 텍스처 픽셀 좌표 (cx, cy)(밖이어도 됨), 반지름 r 픽셀.
        /// </summary>
        private static EarthGlobe Create(string name, Transform parent, int order, int w, int h, float cx, float cy, float r, Vector2 pivot)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            var assets = GameAssets.Instance;
            if (assets != null && assets.spriteMaterial != null) sr.sharedMaterial = assets.spriteMaterial;
            var globe = go.AddComponent<EarthGlobe>();
            globe.Build(sr, w, h, cx, cy, r, pivot);
            return globe;
        }

        private void Build(SpriteRenderer sr, int w, int h, float cx, float cy, float r, Vector2 pivot)
        {
            EnsureMaps();
            texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "EarthGlobe",
            };
            pixels = new Color32[w * h];
            mapV = new int[w * h];
            mapU = new float[w * h];
            shade = new byte[w * h];

            // 빛은 왼쪽 위 앞에서
            Vector3 light = new Vector3(-0.45f, 0.6f, 0.65f).normalized;
            for (int ty = 0; ty < h; ty++)
            {
                for (int tx = 0; tx < w; tx++)
                {
                    int i = ty * w + tx;
                    float x = tx + 0.5f - cx;
                    float y = ty + 0.5f - cy;
                    float d = Mathf.Sqrt(x * x + y * y);
                    mapV[i] = -1;
                    if (d > r)
                    {
                        if (d <= r + 1.5f) shade[i] = 4; // 대기 빛
                        else shade[i] = 255;
                        continue;
                    }
                    float z = Mathf.Sqrt(Mathf.Max(0f, r * r - x * x - y * y));
                    float lon = Mathf.Atan2(x, z);
                    float lat = Mathf.Asin(Mathf.Clamp(y / r, -1f, 1f));
                    mapU[i] = lon / (Mathf.PI * 2f) * MapW;
                    mapV[i] = Mathf.Clamp(Mathf.FloorToInt((lat / Mathf.PI + 0.5f) * MapH), 0, MapH - 1);
                    if (d > r - 1f) { shade[i] = 3; continue; }
                    float lit = Vector3.Dot(new Vector3(x, y, z) / r, light);
                    shade[i] = (byte)(lit > 0.55f ? 0 : lit > 0.05f ? 1 : 2);
                }
            }

            sr.sprite = Sprite.Create(texture, new Rect(0, 0, w, h), pivot, PlayArea.Config.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            Redraw(0);
        }

        private void Update()
        {
            if (texture == null) return;
            float turns = Time.time / Mathf.Max(1f, spinSeconds);
            int offset = Mathf.FloorToInt(turns * MapW * 2f); // 반 픽셀 단위
            if (offset == lastOffset) return;
            Redraw(offset);
        }

        private void Redraw(int offset)
        {
            lastOffset = offset;
            float ground = offset * 0.5f;
            float cloud = ground * 1.35f + MapW * 0.3f; // 구름은 조금 더 빨리
            for (int i = 0; i < pixels.Length; i++)
            {
                int v = mapV[i];
                if (v < 0)
                {
                    pixels[i] = shade[i] == 4 ? Glow : Clear;
                    continue;
                }
                if (shade[i] == 3) { pixels[i] = Rim; continue; }
                int u = Wrap(Mathf.FloorToInt(mapU[i] + ground));
                int cu = Wrap(Mathf.FloorToInt(mapU[i] + cloud));
                Color32 c = cloudMap[v * MapW + cu] ? Cloud : groundMap[v * MapW + u];
                byte s = shade[i];
                if (s == 1) c = Darken(c, 0.88f);
                else if (s == 2) c = Darken(c, 0.68f);
                pixels[i] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        private static int Wrap(int u)
        {
            u %= MapW;
            return u < 0 ? u + MapW : u;
        }

        private static Color32 Darken(Color32 c, float k) =>
            new Color32((byte)(c.r * k), (byte)(c.g * k), (byte)(c.b * k), c.a);

        private void OnDestroy()
        {
            if (texture != null) Destroy(texture);
        }

        // ---- 세계 지도 만들기 (한 번만) ----

        private static void EnsureMaps()
        {
            if (groundMap != null) return;
            groundMap = new Color32[MapW * MapH];
            cloudMap = new bool[MapW * MapH];
            for (int y = 0; y < MapH; y++)
            {
                float lat = Mathf.Abs((y + 0.5f) / MapH - 0.5f) * 2f; // 0 적도 ~ 1 극
                for (int x = 0; x < MapW; x++)
                {
                    float h = Fbm(x, y, 11, 32f);
                    Color32 c;
                    if (lat > 0.86f) c = Ice;
                    else if (h > 0.52f) c = h > 0.63f ? LandDark : Land;
                    else c = h < 0.38f ? SeaDeep : Sea;
                    groundMap[y * MapW + x] = c;
                    cloudMap[y * MapW + x] = Fbm(x, y, 47, 16f) > 0.64f;
                }
            }
        }

        /// <summary>가로로 이어지는(지도 끝과 처음이 맞물리는) 값 노이즈 0~1.</summary>
        private static float Fbm(int x, int y, int seed, float cell)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < 4; o++)
            {
                sum += amp * Noise(x / cell, y / cell, Mathf.RoundToInt(MapW / cell), seed + o * 101);
                norm += amp;
                amp *= 0.5f;
                cell *= 0.5f;
            }
            return sum / norm;
        }

        private static float Noise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(x0, y0, period, seed), b = Hash(x0 + 1, y0, period, seed);
            float c = Hash(x0, y0 + 1, period, seed), d = Hash(x0 + 1, y0 + 1, period, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float Hash(int x, int y, int period, int seed)
        {
            if (period > 0) { x %= period; if (x < 0) x += period; }
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }
    }
}
