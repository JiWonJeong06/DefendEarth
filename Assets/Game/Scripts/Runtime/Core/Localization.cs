using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 화면 글자 번역(한국어·영어·일본어). 글자는 코드에 쓰지 않고 키로 찾는다.
    /// 번역은 Assets/Game/Data/Localization/strings.csv (key,ko,en,ja)에서 고친다.
    /// 폰 언어가 셋 중 하나가 아니면 영어.
    /// </summary>
    public static class Localization
    {
        public enum Language { Korean = 0, English = 1, Japanese = 2 }

        private static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>();

        public static Language Current { get; set; } = FromSystem();

        public static Language FromSystem()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Korean: return Language.Korean;
                case SystemLanguage.Japanese: return Language.Japanese;
                default: return Language.English;
            }
        }

        public static void Load(TextAsset csv)
        {
            table.Clear();
            if (csv == null) return;
            var lines = csv.text.Split('\n');
            for (int i = 1; i < lines.Length; i++) // 첫 줄은 제목
            {
                var line = lines[i].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split(',');
                if (cols.Length < 4) continue;
                table[cols[0].Trim()] = new[] { cols[1], cols[2], cols[3] };
            }
        }

        /// <summary>키에 맞는 글자. 없으면 키를 그대로 돌려준다(빠진 번역을 찾기 쉽게).</summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (!table.TryGetValue(key, out var row)) return key;
            var text = row[(int)Current];
            return string.IsNullOrEmpty(text) ? row[(int)Language.English] : text;
        }
    }
}
