using System;
using System.Collections.Generic;
using System.Text;

namespace ComfortUI
{
    internal static class KeyboardPolicy
    {
        internal const int MaxText = 16000;
        internal static string[] Languages(IEnumerable<string> enabled, string uiLanguage)
        {
            var values = new List<string>();
            if (enabled != null) foreach (var code in enabled) { if (LanguageSupport.Supported(code) && !values.Contains(code)) values.Add(code); if (values.Count == 13) break; }
            if (values.Count == 0) { values.Add(LanguageSupport.Supported(uiLanguage) ? uiLanguage : "en"); if (values[0] != "en") values.Add("en"); }
            return values.ToArray();
        }
        internal static string Initial(string initial, string[] enabled)
        { return Array.IndexOf(enabled, initial) >= 0 ? initial : enabled[0]; }
        internal static string Clean(string text, int limit, bool multiline)
        {
            limit = limit <= 0 ? MaxText : Math.Min(MaxText, limit);
            var result = new StringBuilder(); text = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
            for (var i = 0; i < text.Length && result.Length < limit; i++)
            {
                var c = text[i];
                if (char.IsHighSurrogate(c)) { if (i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) { if (result.Length + 2 > limit) break; result.Append(c).Append(text[++i]); } continue; }
                if (char.IsLowSurrogate(c) || c < 32 && c != '\n' && c != '\t' || c == 127) continue;
                result.Append(!multiline && (c == '\n' || c == '\t') ? ' ' : c);
            }
            return result.ToString();
        }
        internal static bool ValidSession(string session)
        { Guid value; return session != null && session.Length == 32 && Guid.TryParseExact(session, "N", out value); }
        internal static bool ValidRectangle(double x, double y, double width, double height)
        { return Finite(x) && Finite(y) && Finite(width) && Finite(height) && x >= 0 && y >= 0 && width >= 30 && height >= 16 && x + width <= 16384 && y + height <= 16384; }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
