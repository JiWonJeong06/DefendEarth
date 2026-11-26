using System;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 개발용 단축키(에디터와 Development Build에서만. 출시 빌드에는 코드가 아예 들어가지 않는다).
    /// F1: 골드 +1,000 (저장까지).
    /// </summary>
    public static class DevCheats
    {
        public const int GoldPerPress = 1000;

        /// <summary>개발 키로 저장 값이 바뀌었을 때(로비 숫자 갱신용).</summary>
#pragma warning disable 0067 // 출시 빌드에서는 부르는 곳이 없다
        public static event Action Changed;
#pragma warning restore 0067

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("DevCheats");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Listener>();
        }

        private sealed class Listener : MonoBehaviour
        {
            private void Update()
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb == null || !kb.f1Key.wasPressedThisFrame) return;
                SaveSystem.Data.gold += GoldPerPress;
                SaveSystem.Save();
                Sound.Play(Sfx.Coin);
                Debug.Log("[Dev] 골드 +" + GoldPerPress + " → " + SaveSystem.Data.gold);
                Changed?.Invoke();
            }
        }
#endif
    }
}
