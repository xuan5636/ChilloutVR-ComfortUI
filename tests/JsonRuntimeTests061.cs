using System;
using System.IO;
using System.Reflection;
using ComfortUI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class JsonRuntimeTests061
{
    private static int checks;
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        checks++; Console.WriteLine("PASS " + description);
    }
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1) throw new ArgumentException("Supply the built mod DLL to check its embedded startup JSON.");
            var mod = Assembly.LoadFile(Path.GetFullPath(args[0]));
            foreach (var name in new[] { "settings-zh.json", "locales.json" })
            {
                using (var stream = mod.GetManifestResourceStream("ComfortUI." + name))
                using (var reader = new StreamReader(stream))
                {
                    var parsed = JObject.Parse(reader.ReadToEnd());
                    // These are the same parse/serialize calls used by startup.
                    var json = UiDataJson.Serialize(parsed);
                    Check(JToken.DeepEquals(parsed, JObject.Parse(json)), name + " semantic round-trip");
                    Check(!json.Contains("\r") && !json.Contains("\n"), name + " compact typed-event data");
                    Check(json.Length > 100 && json.Length < 8 * 1024 * 1024, name + " bounded nonempty startup data");
                }
            }
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, StringEscapeHandling = StringEscapeHandling.Default };
            var payload = UiDataJson.Serialize(UiDataJson.FromObject(new { language = "zh", text = "<script>&'\"中日韓", keyboardLanguages = new[] { "zh", "en" } }));
            var restored = JObject.Parse(payload);
            Check(restored["$type"] == null, "Shared serializer defaults cannot add metadata");
            Check(payload.Contains("\\u003c") && payload.Contains("\\u0026"), "HTML-safe escaping preserved");
            Check((string)restored["text"] == "<script>&'\"中日韓", "Unicode and literal text preserved");
            Check(restored["keyboardLanguages"] is JArray && (string)restored["keyboardLanguages"][1] == "en", "Preference arrays preserved");
            Check((string)JObject.Parse(UiDataJson.Translation("cu-1-1", "ja", new[] { "こんにちは" }))["texts"][0] == "こんにちは", "Translation event uses compatible writer");
            Console.WriteLine("Json.NET loaded: " + typeof(JToken).Assembly.Location);
            Console.WriteLine("TOTAL " + checks + " runtime startup JSON checks; no game launched");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { JsonConvert.DefaultSettings = null; }
    }
}
