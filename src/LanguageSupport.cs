namespace ComfortUI
{
    internal static class LanguageSupport
    {
        internal static bool Supported(string code)
        {
            return code == "zh" || code == "en" || code == "ja" || code == "ko" || code == "es";
        }
        internal static string TranslationTarget(string code)
        {
            return code == "zh" ? "zh-CN" : Supported(code) ? code : null;
        }
        internal static string[] FontNames(string code)
        {
            switch (code)
            {
                case "ja": return new[] { "Noto Sans JP", "Yu Gothic", "Meiryo", "MS Gothic", "Arial Unicode MS" };
                case "ko": return new[] { "Noto Sans KR", "Malgun Gothic", "Gulim", "Arial Unicode MS" };
                case "en":
                case "es": return new[] { "Noto Sans", "Segoe UI", "Arial" };
                default: return new[] { "Noto Sans SC", "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS" };
            }
        }
        internal static string FontSample(string code)
        {
            return code == "ja" ? "写真カメラ" : code == "ko" ? "사진카메라" : code == "es" ? "Españoláéíóúü¿¡" : code == "en" ? "Camera" : "相机";
        }
    }
}
