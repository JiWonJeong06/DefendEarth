using System;
using System.IO;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 기기 안 저장 데이터. 개인정보는 넣지 않는다(점수·골드·튜토리얼 여부만).
    /// version 번호로 저장 형식을 구분해서, 나중에 항목이 늘어도 옛 저장을 그대로 읽는다.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int bestScore;
        public int bestWave;
        /// <summary>모아 둔 골드(판에서 져도 유지). 1월에 UFO 해금·강화에 쓴다.</summary>
        public int gold;
        public int totalRuns;
        public bool tutorialDone;
    }

    /// <summary>
    /// 저장·불러오기. 앱 전용 폴더에 JSON 파일로 저장한다(PlayerPrefs 사용 안 함).
    /// 쓰는 도중 앱이 꺼져도 파일이 깨지지 않게 임시 파일에 먼저 쓰고 바꿔 끼운다.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";
        private static SaveData data;

        /// <summary>에디터에서 Play를 다시 누를 때 예전 값이 남지 않게(도메인 다시 불러오기를 끈 경우 대비).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => data = null;

        private static string PathOf(string name) => Path.Combine(Application.persistentDataPath, name);

        public static SaveData Data
        {
            get
            {
                if (data == null) data = Load();
                return data;
            }
        }

        private static SaveData Load()
        {
            try
            {
                string path = PathOf(FileName);
                if (File.Exists(path))
                {
                    var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                    if (loaded != null) return Upgrade(loaded);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] 저장 파일을 읽지 못해 새로 시작합니다: " + e.Message);
            }
            return new SaveData();
        }

        /// <summary>옛 형식 저장을 지금 형식으로 맞춘다(항목이 늘 때 여기에 추가).</summary>
        private static SaveData Upgrade(SaveData d)
        {
            if (d.version < 1) d.version = 1;
            d.version = SaveData.CurrentVersion;
            return d;
        }

        public static void Save()
        {
            if (data == null) return;
            try
            {
                string path = PathOf(FileName);
                string temp = PathOf(FileName + ".tmp");
                File.WriteAllText(temp, JsonUtility.ToJson(data));
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] 저장하지 못했습니다: " + e.Message);
            }
        }
    }
}
