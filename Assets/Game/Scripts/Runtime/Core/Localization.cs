using System;
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

        public static Language Current { get; private set; } = FromSystem();

        /// <summary>언어가 바뀜. 화면 글자들이 듣고 다시 그린다.</summary>
        public static event Action Changed;

        /// <summary>언어를 바꾼다. -1(또는 범위 밖)이면 폰 언어를 따른다.</summary>
        public static void SetLanguage(int index)
        {
            var next = index >= 0 && index <= 2 ? (Language)index : FromSystem();
            if (next == Current) return;
            Current = next;
            Changed?.Invoke();
        }

        public static Language FromSystem()
        {
            switch (Application.systemLanguage)
            {
                case UnityEngine.SystemLanguage.Korean: return Language.Korean;
                case UnityEngine.SystemLanguage.Japanese: return Language.Japanese;
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
                // 줄바꿈은 표 안에서 \n 으로 적는다
                table[cols[0].Trim()] = new[] { Unescape(cols[1]), Unescape(cols[2]), Unescape(cols[3]) };
            }
        }

        private static string Unescape(string s) => s.Replace("\\n", "\n");

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
