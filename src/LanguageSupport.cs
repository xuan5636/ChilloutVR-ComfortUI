namespace ComfortUI
{
    internal static class LanguageSupport
    {
        internal static readonly string[] Codes = { "zh", "en", "ja", "ko", "es", "zh-Hant", "fr", "ru", "pt-BR", "it", "id", "pl", "tr" };
        internal static bool Supported(string code)
        {
            return System.Array.IndexOf(Codes, code) >= 0;
        }
        internal static string TranslationTarget(string code)
        {
            return code == "zh" ? "zh-CN" : code == "zh-Hant" ? "zh-TW" : code == "pt-BR" ? "pt" : Supported(code) ? code : null;
        }
        internal static string BingTarget(string code)
        {
            return code == "zh" || code == "zh-CN" ? "zh-Hans" : code == "zh-TW" ? "zh-Hant" : code == "pt-BR" ? "pt" : code;
        }
        internal static string TranslationName(string code)
        {
            switch (code)
            {
                case "zh": return "Simplified Chinese";
                case "zh-Hant": return "Traditional Chinese (Taiwan)";
                case "ja": return "Japanese";
                case "ko": return "Korean";
                case "es": return "Spanish";
                case "fr": return "French";
                case "ru": return "Russian";
                case "pt-BR": return "Portuguese (Brazil)";
                case "it": return "Italian";
                case "id": return "Indonesian";
                case "pl": return "Polish";
                case "tr": return "Turkish";
                default: return "English";
            }
        }
        internal static string FontFile(string code)
        {
            return code == "zh" ? "NotoSansSC.ttf" : code == "zh-Hant" ? "NotoSansTC.ttf" : code == "ja" ? "NotoSansJP.ttf" : code == "ko" ? "NotoSansKR.ttf" : "NotoSans.ttf";
        }
        internal static string[] FontNames(string code)
        {
            switch (code)
            {
                case "ja": return new[] { "Noto Sans JP", "Yu Gothic", "Meiryo", "MS Gothic", "Arial Unicode MS" };
                case "ko": return new[] { "Noto Sans KR", "Malgun Gothic", "Gulim", "Arial Unicode MS" };
                case "zh-Hant": return new[] { "Noto Sans TC", "Microsoft JhengHei", "PMingLiU", "Noto Sans CJK TC", "Arial Unicode MS" };
                case "zh": return new[] { "Noto Sans SC", "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS" };
                default: return new[] { "Noto Sans", "Segoe UI", "Arial" };
            }
        }
        internal static string FontSample(string code)
        {
            switch (code)
            {
                case "zh": return "相机";
                case "zh-Hant": return "相機麥克風體驗";
                case "ja": return "写真カメラ";
                case "ko": return "사진카메라";
                case "es": return "Españoláéíóúü¿¡";
                case "fr": return "Françaiséèêàçœ";
                case "ru": return "РусскийКамераЁё";
                case "pt-BR": return "Portuguêsãõáêç";
                case "it": return "Italianoàèéìòù";
                case "pl": return "Polskiąćęłńóśźż";
                case "tr": return "TürkçeİıŞşĞğÇç";
                default: return "Camera";
            }
        }
    }
}
