using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 장면에 하나만 둔다. 설정 데이터를 가장 먼저 읽어 PlayArea를 준비한다.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        public GameConfig Config => config;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[GameRoot] GameConfig가 연결되지 않았습니다. 메뉴 Bouncy UFO > Build Game Scene을 다시 실행하세요.");
                config = ScriptableObject.CreateInstance<GameConfig>();
            }
            PlayArea.Init(config);
            Application.targetFrameRate = 60;
        }
    }
}
