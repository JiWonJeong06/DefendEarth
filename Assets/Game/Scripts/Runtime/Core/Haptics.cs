using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 폰 진동(타격감). 설정의 "진동"이 꺼져 있으면 울리지 않는다. 에디터·PC에서는 아무 일도 안 한다.
    /// 안드로이드 기본 Handheld.Vibrate는 길고(약 0.5초) 세기를 못 정해서, 안드로이드 진동기를 직접 불러
    /// 짧게(수십 ms)·세기(1~255)를 정해 울린다(안드로이드 8.0 이상. 7.1은 세기 없이 짧게).
    /// VIBRATE 권한(11/27 사용자 승인): Unity는 코드에 Handheld.Vibrate가 있으면 이 권한을 자동으로 넣는다.
    /// </summary>
    public static class Haptics
    {
        private const float MinInterval = 0.03f; // 여러 마리를 한꺼번에 쳐도 너무 촘촘히 울리지 않게

        private static float lastAt = -1f;
        // 실제로 true가 되는 일은 없다. Handheld.Vibrate를 코드에 남겨 VIBRATE 권한이 자동으로 들어가게 하는 용도
        private static bool useLegacyVibrate;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject vibrator;
        private static AndroidJavaClass effectClass;
        private static int sdk;
        private static bool ready;
        private static bool failed;

        private static void Init()
        {
            ready = true;
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION")) sdk = version.GetStatic<int>("SDK_INT");
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (sdk >= 26) effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                failed = vibrator == null;
            }
            catch (System.Exception)
            {
                failed = true;
            }
        }
#endif

        /// <summary>짧게 한 번 울린다. ms = 길이, strength 0~1 = 세기.</summary>
        public static void Pulse(int ms, float strength)
        {
            if (!SaveSystem.Data.vibration || ms <= 0) return;
            if (Time.unscaledTime - lastAt < MinInterval) return;
            lastAt = Time.unscaledTime;

            if (useLegacyVibrate) Handheld.Vibrate();

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!ready) Init();
            if (failed) return;
            try
            {
                if (effectClass != null)
                {
                    int amplitude = Mathf.Clamp(Mathf.RoundToInt(strength * 255f), 1, 255);
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)ms, amplitude))
                        vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", (long)ms);
                }
            }
            catch (System.Exception)
            {
                failed = true; // 한 번 실패하면 이번 실행 동안은 더 시도하지 않는다
            }
#endif
        }
    }
}
