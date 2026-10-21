using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ComfortUI
{
    // Data only: these strings are TriggerEvent arguments, never JavaScript source.
    internal static class UiDataJson
    {
        internal const int TranslationTextLimit = 48000;
        internal static string Serialize(JToken value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            using (var buffer = new StringWriter(CultureInfo.InvariantCulture))
            using (var writer = new JsonTextWriter(buffer) { Formatting = Formatting.None, StringEscapeHandling = StringEscapeHandling.EscapeHtml })
            {
                value.WriteTo(writer);
                writer.Flush();
                return buffer.ToString();
            }
        }
        internal static JToken FromObject(object value)
        {
            // Create (not CreateDefault) ignores mutable defaults from other mods.
            return JToken.FromObject(value, JsonSerializer.Create(new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.None, MaxDepth = 64,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml
            }));
        }
        internal static bool RequestId(string id)
        {
            return id != null && id.Length <= 64 && Regex.IsMatch(id, "\\Acu-[0-9]+-[0-9]+\\z");
        }
        internal static string Translation(string id, string language, string[] texts, string error = null)
        {
            if (!RequestId(id) || !LanguageSupport.Supported(language)) throw new FormatException("Translation identity");
            var data = new JObject { ["id"] = id, ["language"] = language };
            if (error == null)
            {
                if (texts == null || texts.Length < 1 || texts.Length > 2) error = "response";
                else foreach (var text in texts) if (text == null || text.Length > TranslationTextLimit) { error = "response"; break; }
            }
            if (error == null) data["texts"] = new JArray(texts);
            else data["error"] = Status(error);
            return Serialize(data);
        }
        internal static string Status(string code)
        {
            switch (code)
            {
                case "network": case "quota": case "busy": case "disabled": case "invalid": case "length":
                case "response": case "timeout": case "proxy": case "auth": case "blocked": case "redirect":
                case "serviceConfig": case "missingKey": case "sourceLanguageError":
                case "testing": case "testSuccess": case "cacheCleared": case "keyCleared": case "keySaved": case "keySaveFailed":
                case "restored": case "chatEnabled": case "pendingChat":
                case "localIdle": case "localPlatform": case "localDownloadFailed": case "localIntegrity":
                case "localMissing": case "localStartFailed": case "localLength": case "localCancelled":
                case "localRuntimeDownload": case "localModelDownload": case "localGpuDownload": case "localCpuFallback":
                case "localReady": case "localVerifying": case "localStarting": case "localRunningGpu": case "localRunningCpu":
                    return code;
                default: return "network";
            }
        }
    }
}
