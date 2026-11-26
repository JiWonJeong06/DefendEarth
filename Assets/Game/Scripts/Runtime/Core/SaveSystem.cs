using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>이 기기 순위표 한 줄(점수·웨이브·날짜만, 개인정보 없음).</summary>
    [Serializable]
    public sealed class ScoreEntry
    {
        public int score;
        public int wave;
        public string date;
    }

    /// <summary>
    /// 기기 안 저장 데이터. 개인정보는 넣지 않는다.
    /// version 번호로 저장 형식을 구분해서, 나중에 항목이 늘어도 옛 저장을 그대로 읽는다.
    /// (v1: 기록·골드·튜토리얼 → v2: 설정, 계정 경험치, 강화, 꾸미기, 이 기기 순위표 추가)
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 9;

        public int version = CurrentVersion;
        public int bestScore;
        public int bestWave;
        /// <summary>모아 둔 골드(판에서 져도 유지).</summary>
        public int gold;
        public int totalRuns;
        public bool tutorialDone;

        // v2 ---------------------------------------------------------
        /// <summary>-1 = 폰 언어, 0 한국어, 1 영어, 2 일본어</summary>
        public int language = -1;
        public bool sound = true;
        public bool vibration = true;
        // v3 ---------------------------------------------------------
        /// <summary>소리 크기 0~1(효과음·배경음악 모두). 소리 끄기와 따로.</summary>
        public float volume = 1f;
        /// <summary>닉네임(처음 켤 때 정함, 설정에서 언제든 바꿈). 비어 있으면 아직 안 정함.</summary>
        public string nickname = "";
        /// <summary>지금까지 게임(튜토리얼 포함)을 한 시간 합계(초). 프로필에 보여 준다.</summary>
        public float playSeconds;
        // v4 ---------------------------------------------------------
        /// <summary>타격 때 화면 흔들림(설정에서 끄기).</summary>
        public bool screenShake = true;
        // v5 ---------------------------------------------------------
        /// <summary>조준 보조(맞을 몬스터에 노란 네모).</summary>
        public bool aimAssist = true;
        // v6 ---------------------------------------------------------
        /// <summary>해금한 UFO 번호(기본형 0은 늘 가진 것으로 친다). UfoTypes.All 순서.</summary>
        public List<int> ownedUfos = new List<int>();
        /// <summary>지금 타는 UFO 번호.</summary>
        public int selectedUfo;
        // v8 ---------------------------------------------------------
        /// <summary>해금한 직업 번호(신입 0은 늘 가진 것으로 친다). JobTypes.All 순서.</summary>
        public List<int> ownedJobs = new List<int>();
        /// <summary>지금 고른 직업 번호.</summary>
        public int selectedJob;
        // v9 ---------------------------------------------------------
        /// <summary>달 기지 마을에 놓은 꾸밈 물건.</summary>
        public List<PlacedDeco> villageItems = new List<PlacedDeco>();
        /// <summary>가방에 있는 꾸밈 물건 개수(Village.Items 순서).</summary>
        public List<int> villageBag = new List<int>();
        /// <summary>플레이어 계정 경험치(누적). 레벨은 계산해서 구한다.</summary>
        public int accountXp;
        /// <summary>강화 단계. 순서는 GameConfig.upgrades와 같다.</summary>
        public List<int> upgradeLevels = new List<int>();
        /// <summary>캐릭터 꾸미기(11/27 5부품): 머리 모양·머리색, 피부색, 눈 모양·눈 색, 옷, 머리 장식(0 = 없음)</summary>
        public int hairStyle;
        public int hairColor;
        public int skinColor;
        public int suitColor; // 옛 옷색(안 씀, 옛 저장 호환용)
        public int eyeStyle;
        public int eyeColor;
        public int outfit;
        public int headwear;
        /// <summary>이 기기 최고 기록 TOP 10</summary>
        public List<ScoreEntry> topScores = new List<ScoreEntry>();

        public int UpgradeLevel(int index) => index >= 0 && index < upgradeLevels.Count ? upgradeLevels[index] : 0;

        public void SetUpgradeLevel(int index, int level)
        {
            while (upgradeLevels.Count <= index) upgradeLevels.Add(0);
            upgradeLevels[index] = level;
        }
    }

    /// <summary>
    /// 저장·불러오기. 앱 전용 폴더에 JSON 파일로 저장한다(PlayerPrefs 사용 안 함).
    /// 쓰는 도중 앱이 꺼져도 파일이 깨지지 않게 임시 파일에 먼저 쓰고 바꿔 끼운다.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";
        private const int TopScoreCount = 10;
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
            if (d.version < 2)
            {
                // v1에는 설정이 없었다: 기본값으로 시작. JsonUtility가 없는 bool을 false로 읽으므로 켜 준다.
                d.language = -1;
                d.sound = true;
                d.vibration = true;
                if (d.bestScore > 0 && d.topScores.Count == 0)
                    d.topScores.Add(new ScoreEntry { score = d.bestScore, wave = d.bestWave, date = "-" });
            }
            if (d.version < 3) d.volume = 1f; // v2 이하 저장엔 없어서 0으로 읽힌다
            if (d.version < 4) d.screenShake = true; // 없으면 false로 읽힌다
            if (d.version < 5) d.aimAssist = true;
            if (d.upgradeLevels == null) d.upgradeLevels = new List<int>();
            if (d.ownedUfos == null) d.ownedUfos = new List<int>();
            if (d.ownedJobs == null) d.ownedJobs = new List<int>();
            if (d.villageItems == null) d.villageItems = new List<PlacedDeco>();
            if (d.villageBag == null) d.villageBag = new List<int>();
            if (d.version < 7) RefundUpgrades(d);
            if (d.topScores == null) d.topScores = new List<ScoreEntry>();
            d.version = SaveData.CurrentVersion;
            return d;
        }

        /// <summary>이 기기 순위표에 넣는다. 몇 등인지 돌려준다(1부터, 못 들면 0).</summary>
        public static int AddTopScore(int score, int wave)
        {
            var list = Data.topScores;
            var entry = new ScoreEntry { score = score, wave = wave, date = DateTime.Now.ToString("yyyy-MM-dd") };
            int index = list.FindIndex(e => score > e.score);
            if (index < 0) index = list.Count;
            if (index >= TopScoreCount) return 0;
            list.Insert(index, entry);
            if (list.Count > TopScoreCount) list.RemoveRange(TopScoreCount, list.Count - TopScoreCount);
            return index + 1;
        }

        /// <summary>
        /// 11/27 로비 강화를 없애면서 그동안 강화에 쓴 골드를 한 번 돌려준다(옛 가격표 그대로).
        /// 순서: 지구 체력, 멀리 날기, 운석 골드, 공격력.
        /// </summary>
        private static void RefundUpgrades(SaveData d)
        {
            int[][] oldCosts =
            {
                new[] { 40, 80, 150, 250, 400 },
                new[] { 60, 120, 220, 350, 500 },
                new[] { 50, 100, 180, 300, 450 },
                new[] { 120, 300, 600 },
            };
            int refund = 0;
            for (int i = 0; i < oldCosts.Length && i < d.upgradeLevels.Count; i++)
                for (int lv = 0; lv < d.upgradeLevels[i] && lv < oldCosts[i].Length; lv++)
                    refund += oldCosts[i][lv];
            d.gold += refund;
            d.upgradeLevels.Clear();
            if (refund > 0) Debug.Log("[Save] 로비 강화를 없애면서 골드 " + refund + "을 돌려줬습니다.");
        }

        /// <summary>데이터 초기화(설정의 계정 탭): 저장 파일을 지우고 처음 설치한 상태로(설정 포함).</summary>
        public static void ResetAll()
        {
            try
            {
                foreach (var name in new[] { FileName, FileName + ".tmp" })
                {
                    string path = PathOf(name);
                    if (File.Exists(path)) File.Delete(path);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] 초기화 중 파일을 지우지 못했습니다: " + e.Message);
            }
            data = new SaveData();
            Save();
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
