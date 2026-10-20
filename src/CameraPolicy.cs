using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ComfortUI
{
    internal enum CameraGroup { Common, Effects, Framing }

    internal static class CameraPolicy
    {
        internal struct LayoutPlan
        {
            internal float ToolbarY, ToolbarScale, DrawerY, DrawerScale, CloseX, CloseY;
        }
        // Desktop camera placement fits its original preview rectangle. Adding rows
        // below that rectangle is invisible in the native landscape placement mode.
        internal static LayoutPlan Layout(bool vr, float canvasWidth, float canvasHeight)
        {
            var width = canvasWidth > 480 ? Math.Min(1920f, canvasWidth - 480f) : 1920f;
            var height = canvasHeight >= 360 ? Math.Min(1080f, canvasHeight) : 1080f;
            var toolbarScale = Math.Min(width / 2440f, height * .19f / 220f);
            var toolbarY = -height / 2 + 14 + 110 * toolbarScale;
            var drawerScale = Math.Min(.85f, Math.Min(width / 2200f, (height - 220 * toolbarScale - 50) / 520f));
            return new LayoutPlan {
                ToolbarY = vr ? -710 : toolbarY, ToolbarScale = vr ? 1 : toolbarScale,
                DrawerY = vr ? -1110 : toolbarY + 110 * toolbarScale + 22 + 260 * drawerScale,
                DrawerScale = vr ? 1 : drawerScale, CloseX = width / 2 - 142, CloseY = height / 2 - 72
            };
        }

        internal static bool ReplaceHoverCallback(string method, bool ownBackground)
        {
            return ownBackground && (method == "set_sprite" || method == "set_color");
        }
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
