using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 젤리처럼 늘었다 줄었다 하는 연출. 그림(자식 오브젝트)에만 붙이고 판정에는 영향이 없다.
    /// amount &gt; 0 : 옆으로 넓고 납작하게(눌림), amount &lt; 0 : 위아래로 길쭉하게(늘어남).
    /// </summary>
    public sealed class SquashStretch : MonoBehaviour
    {
        private Vector3 baseScale;
        private Vector3 baseLocalPos;
        private float amount;
        private float velocity;
        private float hopOffset;

        private void Awake()
        {
            baseScale = transform.localScale;
            baseLocalPos = transform.localPosition;
        }

        /// <summary>순간적으로 모양을 바꾼다. 스프링이 원래대로 출렁이며 돌려놓는다.</summary>
        public void Punch(float squash)
        {
            amount = squash;
            velocity = 0f;
        }

        /// <summary>지금 출렁임을 덮어쓰지 않고 살짝만 보탠다(발사 들썩임 등 자주 일어나는 작은 연출용).</summary>
        public void Nudge(float squash)
        {
            velocity += squash * 25f;
        }

        /// <summary>위로 튀어 오른 높이(유닛). 튀는 중 포물선에 쓴다.</summary>
        public void SetHop(float offset) => hopOffset = offset;

        private void LateUpdate()
        {
            var config = PlayArea.Config;
            float k = config != null ? config.squashStiffness : 260f;
            float d = config != null ? config.squashDamping : 12f;
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);

            velocity += (-k * amount - d * velocity) * dt;
            amount += velocity * dt;
            amount = Mathf.Clamp(amount, -0.6f, 0.6f);

            transform.localScale = new Vector3(baseScale.x * (1f + amount), baseScale.y * (1f - amount), baseScale.z);
            transform.localPosition = baseLocalPos + new Vector3(0f, hopOffset, 0f);
        }
    }
}
