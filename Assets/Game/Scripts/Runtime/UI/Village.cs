using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>꾸밈 물건 한 가지(마을에 놓는 장식). 처음엔 능력 없는 순수 꾸밈.</summary>
    public sealed class DecoItem
    {
        public string nameKey;
        public int price;
        public int sprite; // GameAssets.decoSprites 번호
    }

    /// <summary>마을에 놓인 물건 하나(저장용).</summary>
    [Serializable]
    public struct PlacedDeco
    {
        public int id;
        public int col;
        public int row;
    }

    /// <summary>
    /// 달 기지 마을 로비(11/27): 위에서 내려다보는 한 화면 마을(11×10칸, 한 칸 16픽셀).
    /// - 화면 아무 데나 누르면 그 자리에 조이스틱이 생기고, 꾸민 캐릭터가 걷는다(물건·건물은 못 지나감).
    /// - 건물: 발사대(앞에 서면 출격 준비), 기록탑(앞에 서면 순위표). 화면 버튼도 그대로 있다.
    /// - 꾸미기 모드: 아래 줄에서 물건을 골드로 사고(가방에 들어감), 빈 칸을 누르면 놓고, 놓인 물건을 누르면 가방으로.
    /// 물건 목록 번호는 저장에 쓰이므로 뒤에만 붙인다.
    /// </summary>
    public sealed class Village : MonoBehaviour
    {
        public static readonly DecoItem[] Items =
        {
            new DecoItem { nameKey = "deco.rock", price = 20, sprite = 0 },
            new DecoItem { nameKey = "deco.flag", price = 30, sprite = 1 },
            new DecoItem { nameKey = "deco.antenna", price = 60, sprite = 2 },
            new DecoItem { nameKey = "deco.lamp", price = 50, sprite = 3 },
            new DecoItem { nameKey = "deco.plant", price = 40, sprite = 4 },
            new DecoItem { nameKey = "deco.dome", price = 120, sprite = 5 },
        };

        public const int Cols = 11;
        public const int Rows = 10;
        private const float Tile = 16f;
        private const float LeftPx = -88f;
        private const float BottomPx = -104f;
        private const float WalkSpeedPx = 48f;
        private const float StickRadiusPx = 14f;

        // 건물 자리(칸): 발사대 2×2, 기록탑 1×2. 앞(아래 줄) 칸이 들어가는 곳
        private static readonly RectInt PadTiles = new RectInt(4, 7, 2, 2);
        private static readonly RectInt TowerTiles = new RectInt(9, 7, 1, 2);
        private static readonly RectInt PadDoor = new RectInt(4, 6, 2, 1);
        private static readonly RectInt TowerDoor = new RectInt(9, 6, 1, 1);

        /// <summary>발사대 앞에 섰다(출격 준비를 연다).</summary>
        public event Action LaunchPadEntered;
        /// <summary>기록탑 앞에 섰다(순위표를 연다).</summary>
        public event Action TowerEntered;
        /// <summary>꾸미기로 골드가 바뀜.</summary>
        public event Action GoldChanged;
        /// <summary>꾸미기 모드가 켜지거나 꺼짐.</summary>
        public event Action<bool> DecorateChanged;

        public bool Decorating { get; private set; }

        private GameConfig Config => PlayArea.Config;
        private float U(float px) => Config.PixelsToUnits(px);

        private PilotView character;
        private Vector2 charPx;          // 캐릭터 가운데(기준 픽셀)
        private bool facingLeft;
        private float walkTime;
        private int lastDoor = -1;      // 0 발사대, 1 기록탑, -1 없음
        private SpriteRenderer padUfo;

        private readonly Dictionary<Vector2Int, SpriteRenderer> placed = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly List<SpriteRenderer> gridLines = new List<SpriteRenderer>();

        // 조이스틱
        private TapInput input;
        private bool stickActive;
        private Vector2 stickOriginPx;
        private Vector2 stickDir;
        private SpriteRenderer stickBase;
        private SpriteRenderer stickKnob;

        // 꾸미기 줄
        private Transform decoBar;
        private readonly List<SpriteRenderer> slotFrames = new List<SpriteRenderer>();
        private readonly List<TextMeshPro> slotCounts = new List<TextMeshPro>();
        private int selected;
        private TextMeshPro decoHint;

        public static Village Create(Transform parent)
        {
            var go = new GameObject("Village");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<Village>();
            v.Build();
            return v;
        }

        private Vector2 TileCenterPx(int c, int r) => new Vector2(LeftPx + Tile * 0.5f + c * Tile, BottomPx + Tile * 0.5f + r * Tile);
        private Vector3 World(Vector2 px) => new Vector3(U(px.x), U(px.y), 0f);
        private static int OrderFor(float footYPx) => 400 - Mathf.RoundToInt(footYPx); // 아래에 있을수록 앞

        private bool InGrid(int c, int r) => c >= 0 && c < Cols && r >= 0 && r < Rows;
        private bool IsBuilding(int c, int r) => PadTiles.Contains(new Vector2Int(c, r)) || TowerTiles.Contains(new Vector2Int(c, r));
        private bool Blocked(int c, int r) => !InGrid(c, r) || IsBuilding(c, r) || placed.ContainsKey(new Vector2Int(c, r));

        private Vector2Int TileAtPx(Vector2 px) => new Vector2Int(Mathf.FloorToInt((px.x - LeftPx) / Tile), Mathf.FloorToInt((px.y - BottomPx) / Tile));

        // ---------------- 만들기 ----------------

        private void Build()
        {
            var a = GameAssets.Instance;
            BuildGround();

            // 하늘의 지구
            var earth = EarthGlobe.CreateWhole("Earth", transform, SortOrder.Earth, 16f);
            earth.transform.position = World(new Vector2(0f, 84f));

            // 건물
            var pad = a.CreateSprite("LaunchPad", a.buildingPad, OrderFor(BottomPx + PadTiles.y * Tile), transform);
            pad.transform.position = World(new Vector2(LeftPx + (PadTiles.x + 1) * Tile, BottomPx + (PadTiles.y + 1) * Tile));
            padUfo = a.CreateSprite("PadUfo", null, OrderFor(BottomPx + PadTiles.y * Tile) + 1, transform);
            padUfo.transform.position = pad.transform.position + World(new Vector2(0f, 4f));
            RefreshUfo();
            var tower = a.CreateSprite("Tower", a.buildingTower, OrderFor(BottomPx + TowerTiles.y * Tile), transform);
            tower.transform.position = World(new Vector2(LeftPx + TowerTiles.x * Tile + Tile * 0.5f, BottomPx + (TowerTiles.y + 1) * Tile));

            // 놓아 둔 물건
            foreach (var d in SaveSystem.Data.villageItems)
                if (d.id >= 0 && d.id < Items.Length && InGrid(d.col, d.row) && !IsBuilding(d.col, d.row))
                    PlaceSprite(d.id, d.col, d.row);

            // 캐릭터: 가운데 아래쪽
            character = PilotView.Create(transform, Vector3.zero, 1, 0);
            charPx = TileCenterPx(5, 3) + new Vector2(0f, 10f);
            MoveCharacterTo(charPx);

            BuildStick();
            BuildDecoBar();

            input = TapInput.Main;
            if (input != null)
            {
                input.Pressed += OnPressed;
                input.Released += OnReleased;
            }
        }

        private void OnDestroy()
        {
            if (input == null) return;
            input.Pressed -= OnPressed;
            input.Released -= OnReleased;
        }

        /// <summary>달 바닥: 회색 모래에 작은 구덩이(코드로 그림).</summary>
        private void BuildGround()
        {
            int w = (int)(Cols * Tile), h = (int)(Rows * Tile);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            var rng = new System.Random(11);
            for (int i = 0; i < px.Length; i++)
            {
                int n = rng.Next(100);
                byte v = (byte)(n < 8 ? 96 : n < 30 ? 108 : 116);
                px[i] = new Color32(v, v, (byte)(v + 8), 255);
            }
            // 구덩이 몇 개(어두운 테두리 + 밝은 아래쪽)
            for (int k = 0; k < 14; k++)
            {
                int cx = rng.Next(w), cy = rng.Next(h), r = 2 + rng.Next(4);
                for (int y = -r - 1; y <= r + 1; y++)
                    for (int x = -r - 1; x <= r + 1; x++)
                    {
                        int xx = cx + x, yy = cy + y;
                        if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                        float d = Mathf.Sqrt(x * x + y * y);
                        if (d <= r - 0.5f) px[yy * w + xx] = new Color32(92, 92, 102, 255);
                        else if (d <= r + 0.5f) px[yy * w + xx] = y > 0 ? new Color32(80, 80, 90, 255) : new Color32(138, 138, 148, 255);
                    }
            }
            tex.SetPixels32(px);
            tex.Apply(false);
            var sr = GameAssets.Instance.CreateSprite("Ground", Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Config.pixelsPerUnit, 0, SpriteMeshType.FullRect), SortOrder.Background + 2, transform);
            sr.transform.position = World(new Vector2(LeftPx + w * 0.5f, BottomPx + h * 0.5f));

            // 꾸미기 때만 보이는 칸 선
            var a = GameAssets.Instance;
            float p = a.pixelSprite.bounds.size.x;
            for (int c = 0; c <= Cols; c++)
            {
                var l = a.CreateSprite("GridV", a.pixelSprite, SortOrder.Background + 3, transform);
                l.color = new Color(1f, 1f, 1f, 0.18f);
                l.transform.localScale = new Vector3(U(1f) / p, U(h) / p, 1f);
                l.transform.position = World(new Vector2(LeftPx + c * Tile, BottomPx + h * 0.5f));
                gridLines.Add(l);
            }
            for (int r = 0; r <= Rows; r++)
            {
                var l = a.CreateSprite("GridH", a.pixelSprite, SortOrder.Background + 3, transform);
                l.color = new Color(1f, 1f, 1f, 0.18f);
                l.transform.localScale = new Vector3(U(w) / p, U(1f) / p, 1f);
                l.transform.position = World(new Vector2(LeftPx + w * 0.5f, BottomPx + r * Tile));
                gridLines.Add(l);
            }
            foreach (var l in gridLines) l.gameObject.SetActive(false);
        }

        private void BuildStick()
        {
            stickBase = GameAssets.Instance.CreateSprite("StickBase", CircleSprite(14, false), SortOrder.Hud + 20, transform);
            stickBase.color = new Color(1f, 1f, 1f, 0.35f);
            stickKnob = GameAssets.Instance.CreateSprite("StickKnob", CircleSprite(6, true), SortOrder.Hud + 21, transform);
            stickKnob.color = new Color(1f, 1f, 1f, 0.7f);
            stickBase.gameObject.SetActive(false);
            stickKnob.gameObject.SetActive(false);
        }

        /// <summary>도트 원(테두리만 또는 꽉 찬).</summary>
        private Sprite CircleSprite(int r, bool filled)
        {
            int size = r * 2 + 3;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[size * size];
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    bool on = filled ? d <= r : Mathf.Abs(d - r) < 0.6f;
                    px[y * size + x] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            tex.SetPixels32(px);
            tex.Apply(false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), Config.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        /// <summary>꾸미기 줄(화면 아래, 꾸미기 모드에서만): 물건 6칸 = 그림 + 가진 개수, 아래 "+가격"으로 사기.</summary>
        private void BuildDecoBar()
        {
            var a = GameAssets.Instance;
            decoBar = new GameObject("DecoBar").transform;
            decoBar.SetParent(transform, false);
            float p = a.pixelSprite.bounds.size.x;
            var back = a.CreateSprite("Back", a.pixelSprite, SortOrder.Hud, decoBar);
            back.color = new Color(0.06f, 0.07f, 0.15f, 0.92f);
            back.transform.localScale = new Vector3(U(180f) / p, U(50f) / p, 1f);
            back.transform.position = World(new Vector2(0f, -133f));

            for (int i = 0; i < Items.Length; i++)
            {
                int index = i;
                float x = -75f + i * 30f;
                var slot = PixelButton.Create("Slot" + i, decoBar, new Vector2(x, -124f), new Vector2(26f, 24f), null, new Color(0.2f, 0.24f, 0.4f), SortOrder.Hud + 1, 0);
                slot.Clicked += () => { selected = index; RefreshBar(); };
                var icon = a.CreateSprite("Icon", a.decoSprites != null && i < a.decoSprites.Length ? a.decoSprites[i] : null, SortOrder.Hud + 5, slot.transform);
                icon.transform.localPosition = World(new Vector2(0f, 2f));
                var count = GameText.Create("Count", slot.transform, GameText.Style.Body, SortOrder.Hud + 6, TextAlignmentOptions.BottomRight, 24f);
                count.transform.localPosition = World(new Vector2(1f, -6f));
                slotCounts.Add(count);
                slotFrames.Add(slot.GetComponentInChildren<SpriteRenderer>());

                var buy = PixelButton.Create("Buy" + i, decoBar, new Vector2(x, -146f), new Vector2(26f, 12f), null, new Color(0.85f, 0.6f, 0.25f), SortOrder.Hud + 1, 0, GameText.Style.Body);
                var label = GameText.Create("Price", buy.transform, GameText.Style.Body, SortOrder.Hud + 5, TextAlignmentOptions.Center, 26f);
                GameText.SetPlain(label, "+" + Items[i].price);
                buy.Clicked += () => Buy(index);
            }

            decoHint = GameText.Create("Hint", decoBar, GameText.Style.Body, SortOrder.Hud + 2, TextAlignmentOptions.Center, 170f, true);
            GameText.SetKey(decoHint, "village.deco_hint");
            decoHint.color = new Color(1f, 0.9f, 0.5f);
            decoHint.transform.position = World(new Vector2(0f, -100f + 0f));
            decoBar.gameObject.SetActive(false);
        }

        // ---------------- 캐릭터·조이스틱 ----------------

        private void OnPressed(Vector2 screen)
        {
            if (Decorating || UiLayer.Current != 0 || PixelButton.AnyAt(screen)) return;
            Vector2 px = ScreenToPx(screen);
            // 위쪽 줄(프로필·골드·아이콘)과 맨 아래 시작 버튼 줄은 조이스틱을 만들지 않는다
            if (px.y > 70f || px.y < -126f) return;
            stickActive = true;
            stickOriginPx = px;
            stickDir = Vector2.zero;
            stickBase.gameObject.SetActive(true);
            stickKnob.gameObject.SetActive(true);
            stickBase.transform.position = World(px);
            stickKnob.transform.position = World(px);
        }

        private void OnReleased(Vector2 screen, float held)
        {
            if (stickActive)
            {
                stickActive = false;
                stickDir = Vector2.zero;
                stickBase.gameObject.SetActive(false);
                stickKnob.gameObject.SetActive(false);
                return;
            }
            if (Decorating && UiLayer.Current == 0 && !PixelButton.AnyAt(screen)) TapTile(ScreenToPx(screen));
        }

        private Vector2 ScreenToPx(Vector2 screen)
        {
            var cam = PlayAreaCamera.Main != null ? PlayAreaCamera.Main.Camera : null;
            if (cam == null) return Vector2.zero;
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            return new Vector2(w.x, w.y) * Config.pixelsPerUnit;
        }

        private void Update()
        {
            if (character == null) return;
            if (stickActive && input != null && input.IsHolding)
            {
                Vector2 d = ScreenToPx(input.Position) - stickOriginPx;
                if (d.magnitude > StickRadiusPx) d = d.normalized * StickRadiusPx;
                stickDir = d.magnitude < 2f ? Vector2.zero : d / StickRadiusPx;
                stickKnob.transform.position = World(stickOriginPx + new Vector2(Mathf.Round(d.x), Mathf.Round(d.y)));
            }
            if (UiLayer.Current != 0) stickDir = Vector2.zero; // 창이 열리면 멈춤

            float dt = Time.deltaTime;
            if (stickDir != Vector2.zero && !Decorating)
            {
                Vector2 step = stickDir * WalkSpeedPx * dt;
                // 가로·세로 따로 막혀서 벽을 따라 미끄러지게
                Vector2 next = charPx + new Vector2(step.x, 0f);
                if (!Blocked(FootTile(next).x, FootTile(next).y)) charPx = next;
                next = charPx + new Vector2(0f, step.y);
                if (!Blocked(FootTile(next).x, FootTile(next).y)) charPx = next;
                if (Mathf.Abs(stickDir.x) > 0.2f) facingLeft = stickDir.x < 0f;
                walkTime += dt;
            }
            else walkTime = 0f;
            MoveCharacterTo(charPx);
            CheckDoors();
        }

        private Vector2Int FootTile(Vector2 centerPx) => TileAtPx(centerPx + new Vector2(0f, -14f));

        private void MoveCharacterTo(Vector2 px)
        {
            // 걸을 때 1픽셀 통통
            float bob = walkTime > 0f && Mathf.FloorToInt(walkTime * 8f) % 2 == 0 ? 1f : 0f;
            Vector2 snapped = new Vector2(Mathf.Round(px.x), Mathf.Round(px.y) + bob);
            character.transform.position = World(snapped);
            character.transform.localScale = new Vector3(facingLeft ? -1f : 1f, 1f, 1f);
            character.SetSortingOrder(OrderFor(px.y - 14f));
        }

        private void CheckDoors()
        {
            var t = FootTile(charPx);
            int door = PadDoor.Contains(t) ? 0 : TowerDoor.Contains(t) ? 1 : -1;
            if (door == lastDoor) return;
            lastDoor = door;
            if (door == 0) LaunchPadEntered?.Invoke();
            else if (door == 1) TowerEntered?.Invoke();
        }

        /// <summary>창을 닫은 뒤 같은 문 앞에서 바로 다시 열리지 않게, 한 칸 아래로 비켜 세운다.</summary>
        public void StepBackFromDoor()
        {
            if (lastDoor < 0) return; // 문 앞이 아니면(버튼으로 연 창) 그대로
            charPx += new Vector2(0f, -Tile);
            var t = FootTile(charPx);
            if (Blocked(t.x, t.y)) charPx += new Vector2(0f, Tile);
            MoveCharacterTo(charPx);
            lastDoor = -1;
            CheckDoors();
        }

        public void RefreshUfo()
        {
            UfoTypes.Apply(padUfo, UfoTypes.SelectedIndex);
            character?.Refresh();
        }

        public void RefreshCharacter() => character?.Refresh();

        // ---------------- 꾸미기 ----------------

        public void SetDecorating(bool on)
        {
            Decorating = on;
            stickActive = false;
            stickDir = Vector2.zero;
            stickBase.gameObject.SetActive(false);
            stickKnob.gameObject.SetActive(false);
            decoBar.gameObject.SetActive(on);
            foreach (var l in gridLines) l.gameObject.SetActive(on);
            RefreshBar();
            DecorateChanged?.Invoke(on);
        }

        private List<int> Bag
        {
            get
            {
                var bag = SaveSystem.Data.villageBag;
                while (bag.Count < Items.Length) bag.Add(0);
                return bag;
            }
        }

        private void Buy(int id)
        {
            var save = SaveSystem.Data;
            if (save.gold < Items[id].price) { Sound.Play(Sfx.Hit, 0.7f); return; }
            save.gold -= Items[id].price;
            Bag[id]++;
            selected = id;
            SaveSystem.Save();
            Sound.Play(Sfx.Coin);
            RefreshBar();
            GoldChanged?.Invoke();
        }

        /// <summary>빈 칸이면 고른 물건을 놓고(가방에 있으면), 물건이 있으면 가방으로 되돌린다.</summary>
        private void TapTile(Vector2 px)
        {
            var t = TileAtPx(px);
            if (!InGrid(t.x, t.y) || IsBuilding(t.x, t.y)) return;
            var key = new Vector2Int(t.x, t.y);
            var save = SaveSystem.Data;
            if (placed.TryGetValue(key, out var sr))
            {
                int idx = save.villageItems.FindIndex(d => d.col == t.x && d.row == t.y);
                if (idx >= 0) { Bag[save.villageItems[idx].id]++; save.villageItems.RemoveAt(idx); }
                Destroy(sr.gameObject);
                placed.Remove(key);
                Sound.Play(Sfx.Click);
            }
            else
            {
                if (FootTile(charPx) == t) return; // 캐릭터가 선 칸은 비워 둔다
                if (Bag[selected] <= 0) { Sound.Play(Sfx.Hit, 0.7f); return; }
                Bag[selected]--;
                save.villageItems.Add(new PlacedDeco { id = selected, col = t.x, row = t.y });
                PlaceSprite(selected, t.x, t.y);
                Sound.Play(Sfx.CardPick);
            }
            SaveSystem.Save();
            RefreshBar();
        }

        private void PlaceSprite(int id, int c, int r)
        {
            var a = GameAssets.Instance;
            var sprite = a.decoSprites != null && id < a.decoSprites.Length ? a.decoSprites[id] : null;
            Vector2 center = TileCenterPx(c, r);
            var sr = a.CreateSprite("Deco" + id, sprite, OrderFor(center.y - Tile * 0.5f), transform);
            sr.transform.position = World(center + new Vector2(0f, 2f));
            placed[new Vector2Int(c, r)] = sr;
        }

        private void RefreshBar()
        {
            for (int i = 0; i < Items.Length && i < slotCounts.Count; i++)
            {
                GameText.SetPlain(slotCounts[i], "x" + Bag[i]);
                slotCounts[i].color = Bag[i] > 0 ? Color.white : new Color(0.6f, 0.6f, 0.7f);
                // 고른 칸은 밝게
                var frame = slotFrames[i];
                if (frame != null) frame.color = i == selected ? new Color(1f, 0.9f, 0.4f) : new Color(0f, 0f, 0f, 0.85f);
            }
        }
    }
}
