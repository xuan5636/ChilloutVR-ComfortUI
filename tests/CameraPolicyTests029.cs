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
            foreach(var code in LanguageSupport.Codes)if(code!="zh"&&code!="en") all[code]=packs[code]["camera"];
            var multi = new CameraLabels(all);
            foreach(var code in all.Keys){
                Check(all[code].Count==80 && multi.Text("Field of View",code)==all[code]["Field of View"] && multi.Text("Field of View","en")=="Field of View", code+" camera contains all 80 words and restores original English");
                Check(multi.Source(multi.Text("Screen Mirror\n(F11 in Desktop)",code))=="Screen Mirror\n(F11 in Desktop)" && multi.Text("<sprite=10>Look Around   <sprite=137>Capture",code).Contains("<sprite=137>"),code+" multiline hints retain original source and native glyph tags");
            }
            Check(LanguageSupport.FontFile("zh-Hant")=="NotoSansTC.ttf" && LanguageSupport.FontNames("zh-Hant").Contains("Microsoft JhengHei"),"Traditional Chinese uses the bundled TC face and its regional OS fallback");
            Check(LanguageSupport.FontSample("ru").Contains("Ё") && LanguageSupport.FontSample("tr").Contains("İ") && LanguageSupport.FontSample("pl").Contains("ł"),"Native font probes include Cyrillic and language-specific Latin glyphs");
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
            foreach (var size in new[] { new[] { 2500f, 1080f }, new[] { 1920f, 1080f }, new[] { 1280f, 720f }, new[] { 960f, 480f }, new[] { 900f, 360f } })
            {
                var plan = CameraPolicy.Layout(false, size[0], size[1]);
                var halfHeight = Math.Min(1080f, size[1]) / 2;
                var halfWidth = Math.Min(1920f, size[0] - 480f) / 2;
                Check(plan.ToolbarY - 110 * plan.ToolbarScale >= -halfHeight && plan.DrawerY + 260 * plan.DrawerScale <= halfHeight,
                    "PC " + size[0] + "x" + size[1] + " toolbar and expanded drawer fit native camera height");
                Check(plan.ToolbarY + 110 * plan.ToolbarScale < plan.DrawerY - 260 * plan.DrawerScale && 1220 * plan.ToolbarScale <= halfWidth + .01f && 1100 * plan.DrawerScale <= halfWidth + .01f,
                    "PC " + size[0] + "x" + size[1] + " controls do not overlap and fit between original side buttons");
                Check(plan.CloseX + 120 < halfWidth && plan.CloseY + 52 < halfHeight,
                    "PC " + size[0] + "x" + size[1] + " close button stays inside preview bounds");
            }
            var vrPlan = CameraPolicy.Layout(true, 2500, 1080);
            Check(vrPlan.ToolbarScale == 1 && vrPlan.DrawerScale == 1 && vrPlan.ToolbarY == -710 && vrPlan.DrawerY == -1110,
                "VR keeps the existing large external controls instead of shrinking physical hit areas");
            Check(CameraPolicy.ReplaceHoverCallback("set_sprite", true) && CameraPolicy.ReplaceHoverCallback("set_color", true), "Only background appearance callbacks are suppressed for themed buttons");
            Check(!CameraPolicy.ReplaceHoverCallback("SetActive", true) && !CameraPolicy.ReplaceHoverCallback("MakePhoto", true) && !CameraPolicy.ReplaceHoverCallback("set_sprite", false), "Tooltip, click and other graphics keep native event routes");
            Check(multi.Text("Close camera", "zh") == "关闭相机" && multi.Source(multi.Text("Close camera", "ko")) == "Close camera", "Camera close caption translates and restores without changing its action");
            Console.WriteLine("TOTAL " + passed + " camera policy checks passed; no Unity runtime interaction claimed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
