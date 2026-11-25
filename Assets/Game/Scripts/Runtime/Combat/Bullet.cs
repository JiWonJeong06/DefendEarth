using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 내 탄. 위로 날아가며 몬스터와 원형 판정으로 부딪힌다(물리 엔진 없이 직접 계산).
    /// 관통 수가 남아 있으면 맞힌 뒤에도 계속 날아간다.
    /// </summary>
    public sealed class Bullet : MonoBehaviour
    {
        private Weapon owner;
        private Vector2 velocity;
        private int damage;
        private float radius;
        private int pierceLeft;
        private readonly List<Monster> alreadyHit = new List<Monster>(4);

        public void Launch(Weapon weapon, Vector2 position, Vector2 vel, int dmg, float hitRadius, int pierce)
        {
            owner = weapon;
            transform.position = position;
            velocity = vel;
            damage = dmg;
            radius = hitRadius;
            pierceLeft = pierce;
            alreadyHit.Clear();
            float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            Vector2 pos = (Vector2)transform.position + velocity * Time.deltaTime;
            transform.position = pos;

            if (pos.y > PlayArea.Top + 1f || pos.x < PlayArea.Left - 1f || pos.x > PlayArea.Right + 1f)
            {
                owner.ReleaseBullet(this);
                return;
            }

            var monsters = MonsterRegistry.Active;
            for (int i = monsters.Count - 1; i >= 0; i--)
            {
                var m = monsters[i];
                if (!m.IsAlive || alreadyHit.Contains(m)) continue;
                float r = radius + m.HitRadius;
                if (((Vector2)m.transform.position - pos).sqrMagnitude > r * r) continue;

                m.TakeHit(damage, pos);
                if (pierceLeft <= 0)
                {
                    owner.ReleaseBullet(this);
                    return;
                }
                pierceLeft--;
                alreadyHit.Add(m);
            }
        }
    }
}
