using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ComfortUI
{
    internal enum CameraGroup { Common, Effects, Framing }

    internal static class CameraPolicy
    {
        // Only classify built-in types by their full name; third-party effects stay accessible.
        internal static CameraGroup Group(string type)
        {
            const string prefix = "ABI_RC.Systems.Camera.VisualMods.";
            if (type == prefix + "AutoFocus" || type == prefix + "ImageStabilization") return CameraGroup.Common;
            if (type == prefix + "AutoExposure" || type == prefix + "Grayscale" || type == prefix + "Greenscreen" || type == prefix + "NightVision" || type == prefix + "Sepia") return CameraGroup.Effects;
            return CameraGroup.Framing;
        }

        // Filtering controls a wrapper, never VisualMod.Hide/Disable or the saved effect state.
        internal static bool Visible(CameraGroup group, CameraGroup? selected, bool supported)
        {
            return supported && (group == CameraGroup.Common || selected == group);
        }
    }

    internal sealed class CameraLabels
    {
        private readonly Dictionary<string, Dictionary<string, string>> languages;
        private readonly Dictionary<string, string> sources = new Dictionary<string, string>(StringComparer.Ordinal);
        internal CameraLabels(Dictionary<string, string> values) : this(new Dictionary<string, Dictionary<string, string>> { ["zh"] = values }) { }
        internal CameraLabels(Dictionary<string, Dictionary<string, string>> values)
        {
            languages = values;
            foreach (var words in languages.Values)
                foreach (var pair in words) sources[Normalize(pair.Key)] = pair.Key;
            foreach (var words in languages.Values)
                foreach (var pair in words)
                    if (!sources.ContainsKey(Normalize(pair.Value))) sources[Normalize(pair.Value)] = pair.Key;
        }
        private static string Normalize(string value) { return Regex.Replace(value ?? "", @"\s+", " ").Trim(); }
        internal string Source(string text)
        {
            string source;
            return sources.TryGetValue(Normalize(text), out source) ? source : null;
        }
        internal string Text(string source, bool chinese)
        {
            return Text(source, chinese ? "zh" : "en");
        }
        internal string Text(string source, string language)
        {
            string translated;
            Dictionary<string, string> words;
            return languages.TryGetValue(language, out words) && words.TryGetValue(source, out translated) ? translated : source;
        }
        internal string Characters(string language)
        {
            Dictionary<string, string> words;
            return languages.TryGetValue(language, out words) ? string.Join("", words.Values) : "";
        }
    }
}
