using System;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 지구. 이 게임에서 체력이 있는 건 지구뿐이다. 0이 되면 판이 끝난다.
    /// </summary>
    public sealed class Earth : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SquashStretch visual;

        public int MaxHp { get; private set; }
        public int Hp { get; private set; }

        /// <summary>(현재 체력, 최대 체력)</summary>
        public event Action<int, int> HpChanged;
        public event Action Died;

        private float hurtTimer;

        private void Awake()
        {
            MaxHp = Mathf.Max(1, PlayArea.Config.earthMaxHp);
            Hp = MaxHp;
        }

        private void Start() => HpChanged?.Invoke(Hp, MaxHp);

        public void TakeDamage(int amount)
        {
            if (Hp <= 0 || amount <= 0) return;
            Hp = Mathf.Max(0, Hp - amount);
            hurtTimer = 0.15f;
            if (visual != null) visual.Punch(0.12f);
            HpChanged?.Invoke(Hp, MaxHp);
            if (Hp == 0) Died?.Invoke();
        }

        public void Heal(int amount)
        {
            if (Hp <= 0 || amount <= 0) return;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            HpChanged?.Invoke(Hp, MaxHp);
        }

        private void Update()
        {
            if (spriteRenderer == null) return;
            if (hurtTimer > 0f) hurtTimer -= Time.unscaledDeltaTime;
            spriteRenderer.color = hurtTimer > 0f ? new Color(1f, 0.55f, 0.55f) : Color.white;
        }
    }
}
