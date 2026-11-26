namespace BouncyUFO
{
    /// <summary>
    /// 닉네임 규칙 (11/27 사용자 결정).
    /// 쓸 수 있는 글자: 영어 대소문자, 숫자, 한글(완성된 글자), 일본어(히라가나·가타카나·장음 ー·한자). 특수문자·띄어쓰기는 안 됨.
    /// 한자는 갈무리 글꼴에 있는 것만(없는 한자는 화면에 □로 나오므로).
    /// 길이(숫자 포함, 최소 2자): 한글이 한 글자라도 있으면 8자, 일본어(가나·한자)가 있으면 10자, 영어·숫자만이면 12자. 섞으면 가장 엄한 규칙.
    /// </summary>
    public static class Nickname
    {
        public const int MinLength = 2;
        public const int MaxHangul = 8;
        public const int MaxJapanese = 10;
        public const int MaxLatin = 12;
        /// <summary>입력 칸이 받는 최대 글자 수(가장 느슨한 규칙).</summary>
        public const int InputLimit = MaxLatin;

        public static bool IsLatinOrDigit(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
        public static bool IsHangul(char c) => c >= '가' && c <= '힣';
        public static bool IsKana(char c) =>
            (c >= 'ぁ' && c <= 'ゖ') || c == 'ゝ' || c == 'ゞ' ||  // 히라가나
            (c >= 'ァ' && c <= 'ヺ') || c == 'ー' || c == 'ヽ' || c == 'ヾ'; // 가타카나·장음(ー)
        public static bool IsKanji(char c) => (c >= '一' && c <= '鿿') || c == '々'; // 々
        public static bool IsJapanese(char c) => IsKana(c) || IsKanji(c);

        /// <summary>이 글자 종류가 섞였을 때 최대 길이.</summary>
        public static int MaxLengthFor(string s)
        {
            bool hangul = false, japanese = false;
            foreach (char c in s)
            {
                if (IsHangul(c)) hangul = true;
                else if (IsJapanese(c)) japanese = true;
            }
            return hangul ? MaxHangul : japanese ? MaxJapanese : MaxLatin;
        }

        /// <summary>문제가 없으면 null, 있으면 안내 문구의 번역 키.</summary>
        public static string Check(string s)
        {
            if (string.IsNullOrEmpty(s)) return "nick.err.short";
            foreach (char c in s)
            {
                if (IsLatinOrDigit(c) || IsHangul(c) || IsKana(c)) continue;
                if (IsKanji(c))
                {
                    if (!GameText.HasGlyph(c)) return "nick.err.font";
                    continue;
                }
                return "nick.err.char";
            }
            if (s.Length < MinLength) return "nick.err.short";
            int max = MaxLengthFor(s);
            if (s.Length > max) return max == MaxHangul ? "nick.err.long_ko" : max == MaxJapanese ? "nick.err.long_ja" : "nick.err.long_en";
            return null;
        }

        /// <summary>저장된 닉네임(없으면 빈 문자열).</summary>
        public static string Current => SaveSystem.Data.nickname ?? string.Empty;
        public static bool IsSet => !string.IsNullOrEmpty(SaveSystem.Data.nickname);
    }
}
