using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using ComfortUI;
class CameraPolicyTests
{
    static int passed;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); passed++; Console.WriteLine("PASS " + message); }
    static int Main(string[] args)
    {
        try
        {
            var labels = new CameraLabels(new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(args[0])));
            var packs = new JavaScriptSerializer { MaxJsonLength = 4000000 }.Deserialize<Dictionary<string,Dictionary<string,Dictionary<string,string>>>>(File.ReadAllText(args[1]));
            var all = new Dictionary<string,Dictionary<string,string>> { ["zh"] = new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(File.ReadAllText(args[0])) };
            foreach(var code in new[]{"ja","ko","es"}) all[code]=packs[code]["camera"];
            var multi = new CameraLabels(all);
            foreach(var code in new[]{"zh","ja","ko","es"}){
                Check(all[code].Count==79 && multi.Text("Field of View",code)==all[code]["Field of View"] && multi.Text("Field of View","en")=="Field of View", code+" camera contains all 79 words and restores original English");
                Check(multi.Source(multi.Text("Screen Mirror\n(F11 in Desktop)",code))=="Screen Mirror\n(F11 in Desktop)" && multi.Text("<sprite=10>Look Around   <sprite=137>Capture",code).Contains("<sprite=137>"),code+" multiline hints retain original source and native glyph tags");
            }
            const string prefix = "ABI_RC.Systems.Camera.VisualMods.";
            Check(CameraPolicy.Group(prefix + "AutoFocus") == CameraGroup.Common && CameraPolicy.Group(prefix + "ImageStabilization") == CameraGroup.Common, "Focus and stabilization remain on the main camera controls");
            Check(new[] { "AutoExposure", "Grayscale", "Greenscreen", "NightVision", "Sepia" }.All(n => CameraPolicy.Group(prefix + n) == CameraGroup.Effects), "All built-in colour and lighting filters share the effects group");
            Check(new[] { "AttachToPlayer", "AttachToWorld", "AutoOrbit", "DroneMode", "MaskLocalPlayer", "MaskNetworkPlayers", "MaskUserInterface", "MaskWorld", "PlayerFaceTracking" }.All(n => CameraPolicy.Group(prefix + n) == CameraGroup.Framing), "Composition, visibility and tracking tools share one group");
            Check(CameraPolicy.Group("ThirdParty.Grayscale") == CameraGroup.Framing, "Third-party type names are not mistaken for built-in filters and remain accessible");
            foreach (var group in new[] { CameraGroup.Common, CameraGroup.Effects, CameraGroup.Framing })
                foreach (CameraGroup? selected in new CameraGroup?[] { null, CameraGroup.Effects, CameraGroup.Framing })
                {
                    if (CameraPolicy.Visible(group, selected, false)) throw new Exception("Unsupported mode became visible");
                    if (CameraPolicy.Visible(group, selected, true) != (group == CameraGroup.Common || selected == group)) throw new Exception("Unexpected visibility");
                }
            Check(true, "Every group honours capture-mode support and closing a group only changes control visibility");
            Check(labels.Text("Field of View", true) == "变焦（视野）" && labels.Text("Field of View", false) == "Field of View", "Zoom label follows Chinese and English without changing its native identifier");
            Check(labels.Source("  Screen Mirror\n(F11 in Desktop) ") == "Screen Mirror\n(F11 in Desktop)" && labels.Source("画面投屏\n桌面模式按 F11") == "Screen Mirror\n(F11 in Desktop)", "Multiline native hints round-trip to their exact original source");
            Check(labels.Source("Custom effect from another mod") == null && labels.Text("Custom effect from another mod", true) == "Custom effect from another mod", "Unknown third-party names remain intact");
            Check(labels.Text("<sprite=10>Look Around   <sprite=137>Capture", true).Contains("<sprite=137>"), "Native input glyph tags survive camera hint translation");
            Check(new[] { "Picture", "Stereoscopic", "Panoramic", "Portal Panoramic", "Unknown", "Reset", "Take Picture" }.All(n => labels.Source(labels.Text(n, true)) == n), "Capture modes and common actions translate and restore without losing their source");
            Console.WriteLine("TOTAL " + passed + " camera policy checks passed; no Unity runtime interaction claimed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
