using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.UI;
using HarmonyLib;
using Newtonsoft.Json.Linq;

namespace ComfortUI
{
    internal static class NativeLocalization
    {
        private static JObject packs;
        private static Func<string> language;
        private static readonly List<KeyValuePair<string, Regex>> patterns = new List<KeyValuePair<string, Regex>>();
        internal static void Install(HarmonyLib.Harmony harmony, string json, Func<string> currentLanguage)
        {
            packs = JObject.Parse(json); language = currentLanguage;
            foreach (var pair in ((JObject)packs["zh"]["system"]).Properties())
                if (pair.Name.Contains("{value}")) patterns.Add(new KeyValuePair<string, Regex>(pair.Name, new Regex("^" + Regex.Escape(pair.Name).Replace("\\{value}", "(.+?)") + "$")));
            foreach (var type in new[] { typeof(CohtmlHud), typeof(ViewManager) })
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    if (type == typeof(CohtmlHud) ? method.Name.StartsWith("ViewDropText", StringComparison.Ordinal) : method.Name == "TriggerAlert" || method.Name == "TriggerPushNotification")
                        if (method.GetParameters().Length > 0) harmony.Patch(method, prefix: new HarmonyMethod(typeof(NativeLocalization), nameof(BeforeMessage)));
        }
        internal static string Text(string input, string code)
        {
            if (input == null || code == "en" || packs == null) return input;
            var words = packs[code]?["system"];
            var direct = words?[input];
            if (direct != null) return (string)direct;
            foreach (var pattern in patterns)
            {
                var match = pattern.Value.Match(input);
                var template = words?[pattern.Key];
                if (match.Success && template != null) return ((string)template).Replace("{value}", match.Groups[1].Value);
            }
            return input;
        }
        internal static string Label(string key, string code) { return (string)packs?[code]?["labels"]?[key] ?? key; }
        private static void BeforeMessage(object[] __args)
        {
            var code = language();
            for (var i = 0; i < Math.Min(3, __args.Length); i++)
                if (__args[i] is string) __args[i] = Text((string)__args[i], code);
        }
    }
}
