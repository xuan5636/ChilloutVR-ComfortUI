using System;
using System.Collections.Concurrent;
using System.Reflection;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Player;
using ABI_RC.Core.UI;
using ABI_RC.Core.UI.UIRework;
using ABI_RC.Core.UI.UIRework.Managers;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ComfortUI
{
    internal static class KeyboardInput
    {
        private sealed class Command { internal CohtmlViewTarget Target; internal string Json; }
        private static readonly ConcurrentQueue<Command> commands = new ConcurrentQueue<Command>();
        private static readonly FieldInfo RendererField = AccessTools.Field(typeof(CVRUIManagerBase), "_uiRenderer");
        private static readonly FieldInfo ViewField = AccessTools.Field(typeof(CVRUIManagerBase), "cohtmlView");
        private static readonly WindowsKeyboardInput desktop = new WindowsKeyboardInput();
        private static KeyboardManager manager;
        private static CohtmlControlledView owner;
        private static CohtmlViewTarget target;
        private static string session, initialText, sentSession, desktopLanguage;
        private static int maximum, pendingCount, revision, lastRevision;
        private static bool multiline, hidden;
        private static IntPtr window;
        private static double rectX, rectY, rectWidth, rectHeight;
        private static bool hasRectangle;
        private static int browserRevision;

        internal static void Install(HarmonyLib.Harmony harmony)
        {
            if (ViewField == null || RendererField == null) throw new MissingFieldException("Keyboard view fields changed");
            harmony.Patch(AccessTools.Method(typeof(KeyboardManager), "ShowKeyboard"), postfix: new HarmonyMethod(typeof(KeyboardInput), nameof(Shown)));
            foreach (var name in new[] { "OnKeyboardClose", "OnKeyboardSubmit" })
                harmony.Patch(AccessTools.Method(typeof(KeyboardManager), name), prefix: new HarmonyMethod(typeof(KeyboardInput), nameof(Closed)));
        }
        private static void Shown(KeyboardManager __instance, object[] __args)
        {
            Reset(); manager = __instance;
            owner = ViewField.GetValue(manager) as CohtmlControlledView;
            if (owner == null || __args == null || __args.Length < 7) return;
            maximum = (int)__args[4]; hidden = (bool)__args[5]; multiline = (bool)__args[6];
            initialText = KeyboardPolicy.Clean(__args[0] as string, maximum, multiline);
            session = Guid.NewGuid().ToString("N"); revision = 1;
        }
        private static void Closed() { Reset(); }
        internal static void Queue(CohtmlViewTarget current, string json)
        {
            if (current == null || json == null || json.Length > 100000) return;
            if (System.Threading.Interlocked.Increment(ref pendingCount) > 32) { System.Threading.Interlocked.Decrement(ref pendingCount); return; }
            commands.Enqueue(new Command { Target = current, Json = json });
        }
        internal static void Tick(CohtmlControlledView view, CohtmlViewTarget current, ComfortMod.Preferences preferences)
        {
            if (manager == null || session == null || owner == null || !manager.IsViewShown || !current.IsCurrent || !ReferenceEquals(view, owner))
            { if (ReferenceEquals(view, owner) && session != null) Reset(); return; }
            if (!preferences.keyboardEnabled) { desktop.Dispose(); return; }
            target = current;
            if (sentSession != session || lastRevision != revision)
            {
                var json = UiDataJson.Serialize(new JObject { ["kind"] = "open", ["session"] = session, ["text"] = initialText, ["maximum"] = maximum <= 0 ? KeyboardPolicy.MaxText : Math.Min(maximum, KeyboardPolicy.MaxText), ["multiline"] = multiline, ["hidden"] = hidden, ["desktop"] = !InVr(), ["revision"] = revision });
                if (!target.Keyboard(json)) return;
                sentSession = session; lastRevision = revision;
            }
            Command command;
            for (var count = 0; count < 8 && commands.TryDequeue(out command); count++)
            {
                System.Threading.Interlocked.Decrement(ref pendingCount);
                if (!ReferenceEquals(command.Target, target)) continue;
                try { Process(JObject.Parse(command.Json), preferences); }
                catch { /* Invalid/unavailable text input is not logged, including passwords. */ }
            }
            if (!desktop.Active) return;
            if (InVr()) { desktop.Dispose(); return; }
            // Win+H can temporarily put the voice toolbar in the foreground.
            // Keep this window's text-service target alive without stealing focus.
            if (WindowsKeyboardInput.GameIsForeground(window))
            {
                WindowsKeyboardInput.Bounds bounds;
                if (!MapRectangle(out bounds) || !desktop.Move(bounds)) { desktop.Dispose(); return; }
            }
            // Dictation can update this owned control while its toolbar has focus.
            // Read only this control; never observe another process's input.
            var snapshot = desktop.Read();
            if (!snapshot.HasValue) return;
            var state = snapshot.Value;
            target.Keyboard(UiDataJson.Serialize(new JObject { ["kind"] = "text", ["session"] = session, ["text"] = KeyboardPolicy.Clean(state.Text, maximum, multiline), ["start"] = state.Start, ["end"] = state.End, ["composing"] = state.Composing, ["focused"] = state.Focused, ["action"] = state.Action ?? "", ["revision"] = browserRevision }));
            if (state.Action != null) desktop.Dispose();
        }
        private static bool InVr() { return MetaPort.Instance != null && MetaPort.Instance.isUsingVr; }
        private static void Process(JObject data, ComfortMod.Preferences preferences)
        {
            if ((string)data["session"] != session || !KeyboardPolicy.ValidSession(session) || data["action"]?.Type != JTokenType.String) return;
            var action = (string)data["action"];
            if (action == "copy")
            {
                if (!hidden && data["text"]?.Type == JTokenType.String && ((string)data["text"]).Length <= KeyboardPolicy.MaxText)
                    GUIUtility.systemCopyBuffer = KeyboardPolicy.Clean((string)data["text"], KeyboardPolicy.MaxText, true);
                return;
            }
            if (action == "ready") { sentSession = null; return; }
            if (action == "blur") { desktop.Blur(); return; }
            if (action == "closed") { desktop.Dispose(); return; }
            if (action != "focus" && action != "edit" && action != "layout") return;
            var rectangle = data["rectangle"] as JArray;
            if (rectangle == null || rectangle.Count != 4) return;
            foreach (var number in rectangle) if (number.Type != JTokenType.Float && number.Type != JTokenType.Integer) return;
            var x = (double)rectangle[0]; var y = (double)rectangle[1]; var width = (double)rectangle[2]; var height = (double)rectangle[3];
            if (!KeyboardPolicy.ValidRectangle(x, y, width, height) || x + width > owner.Width + 1 || y + height > owner.Height + 1) return;
            rectX = x; rectY = y; rectWidth = width; rectHeight = height; hasRectangle = true;
            if (action == "layout") return;
            if (data["text"]?.Type != JTokenType.String || data["start"]?.Type != JTokenType.Integer || data["end"]?.Type != JTokenType.Integer || data["revision"]?.Type != JTokenType.Integer) return;
            var text = (string)data["text"]; if (text.Length > KeyboardPolicy.MaxText) return;
            var start = (int)data["start"]; var end = (int)data["end"]; var incomingRevision = (int)data["revision"];
            if (start < 0 || end < start || end > text.Length || incomingRevision < browserRevision) return;
            browserRevision = incomingRevision;
            text = KeyboardPolicy.Clean(text, maximum, multiline);
            var language = (string)data["language"];
            if (!LanguageSupport.Supported(language)) return;
            if (action == "edit")
            {
                if (desktop.Active)
                {
                    desktop.Write(text, Math.Min(start, text.Length), Math.Min(end, text.Length));
                    if (desktopLanguage != language) { desktop.UseLanguage(language); desktopLanguage = language; }
                }
                return;
            }
            if (!preferences.keyboardSystemInput || InVr()) { Notice("unavailable"); return; }
            window = WindowsKeyboardInput.FindGameWindow();
            WindowsKeyboardInput.Bounds area;
            if (window == IntPtr.Zero || !MapRectangle(out area) || !desktop.Open(window, area, text, start, end, maximum <= 0 ? KeyboardPolicy.MaxText : maximum, hidden, multiline)) Notice("unavailable");
            else { desktop.UseLanguage(language); desktopLanguage = language; Notice("focused"); }
        }
        private static void Notice(string code)
        { target?.Keyboard(UiDataJson.Serialize(new JObject { ["kind"] = "status", ["session"] = session, ["code"] = code })); }

        private static bool MapRectangle(out WindowsKeyboardInput.Bounds result)
        {
            result = default(WindowsKeyboardInput.Bounds);
            if (!hasRectangle || owner == null || manager == null || owner.Width <= 0 || owner.Height <= 0) return false;
            var setup = PlayerSetup.Instance;
            var camera = setup == null ? Camera.main : setup.activeCam;
            if (camera == null) return false;
            var renderer = RendererField.GetValue(manager) as Renderer;
            if (renderer == null) return false;
            int clientWidth, clientHeight;
            if (!WindowsKeyboardInput.ClientSize(window, out clientWidth, out clientHeight) || Screen.width <= 0 || Screen.height <= 0) return false;
            var x = double.MaxValue; var y = double.MaxValue; var right = double.MinValue; var bottom = double.MinValue;
            foreach (var corner in new[] { new Vector2((float)rectX, (float)rectY), new Vector2((float)(rectX + rectWidth), (float)rectY), new Vector2((float)rectX, (float)(rectY + rectHeight)), new Vector2((float)(rectX + rectWidth), (float)(rectY + rectHeight)) })
            {
                // Same normalized plane convention as native RaycastCohtmlPlane.
                var world = renderer.transform.TransformPoint(new Vector3(corner.x / owner.Width - .5f, .5f - corner.y / owner.Height, 0));
                var screen = camera.WorldToScreenPoint(world);
                if (screen.z <= 0) return false;
                var px = screen.x * clientWidth / Screen.width; var py = (Screen.height - screen.y) * clientHeight / Screen.height;
                x = Math.Min(x, px); y = Math.Min(y, py); right = Math.Max(right, px); bottom = Math.Max(bottom, py);
            }
            var left = Math.Max(0, (int)Math.Ceiling(x)); var top = Math.Max(0, (int)Math.Ceiling(y));
            result = new WindowsKeyboardInput.Bounds { X = left, Y = top, Width = Math.Min(clientWidth, (int)Math.Floor(right)) - left, Height = Math.Min(clientHeight, (int)Math.Floor(bottom)) - top };
            return WindowsKeyboardInput.ValidBounds(window, result);
        }
        internal static void Reset()
        {
            desktop.Dispose(); manager = null; owner = null; target = null; window = IntPtr.Zero;
            session = initialText = sentSession = desktopLanguage = null; hasRectangle = false; revision = lastRevision = browserRevision = 0;
            Command ignored; while (commands.TryDequeue(out ignored)) System.Threading.Interlocked.Decrement(ref pendingCount);
        }
    }
}
