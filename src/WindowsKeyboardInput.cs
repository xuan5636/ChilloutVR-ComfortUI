using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ComfortUI
{
    // A Windows EDIT child of this process's game window, not a popup, global hook,
    // background key listener or clipboard bridge. All calls occur on the window thread.
    // Its native text services provide installed IMEs, accessibility and Win+H. The
    // Cohtml editor paints the text; a nearly transparent child receives desktop input.
    internal sealed class WindowsKeyboardInput : IDisposable
    {
        internal struct Snapshot { internal string Text, Action; internal int Start, End; internal bool Composing, Focused; }
        internal struct Bounds { internal int X, Y, Width, Height; }
        private IntPtr child, parent, previousProcedure, font;
        private WindowProcedure procedure;
        private bool dirty, composing, suppressEnter;
        private string pendingAction, lastText;
        private int lastStart = -1, lastEnd = -1;
        private bool lastComposing, lastFocused;
        private Bounds bounds;
        private IntPtr originalLayout;
        internal bool Active { get { return child != IntPtr.Zero && IsWindow(child); } }
        internal bool Focused { get { return Active && GetFocus() == child; } }

        internal static IntPtr FindGameWindow()
        {
            var process = (uint)Process.GetCurrentProcess().Id;
            IntPtr found = IntPtr.Zero;
            EnumWindows((window, ignored) => {
                uint owner; GetWindowThreadProcessId(window, out owner);
                if (owner != process || !IsWindowVisible(window) || GetWindow(window, 4) != IntPtr.Zero) return true;
                var type = new StringBuilder(128); GetClassName(window, type, type.Capacity);
                if (type.ToString() != "UnityWndClass") return true;
                found = window; return false;
            }, IntPtr.Zero);
            return found;
        }

        internal bool Open(IntPtr gameWindow, Bounds area, string text, int start, int end, int limit, bool hidden, bool multiline)
        {
            Dispose();
            uint process; var thread = GetWindowThreadProcessId(gameWindow, out process);
            if (gameWindow == IntPtr.Zero || !IsWindow(gameWindow) || process != (uint)Process.GetCurrentProcess().Id || thread != GetCurrentThreadId() || !ValidBounds(gameWindow, area)) return false;
            parent = gameWindow; bounds = area;
            const int Child = 0x40000000, Visible = 0x10000000, Multiline = 4, AutoVertical = 0x40, AutoHorizontal = 0x80, WantReturn = 0x1000, Password = 0x20;
            var style = Child | Visible | AutoHorizontal | (multiline ? Multiline | AutoVertical | WantReturn : 0) | (hidden ? Password : 0);
            child = CreateWindowEx(0x00080000, "EDIT", "", style, area.X, area.Y, area.Width, area.Height, parent, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (child == IntPtr.Zero) { parent = IntPtr.Zero; return false; }
            // Layered child windows are supported on Windows 8+. Alpha 1 retains a
            // real visible/focusable accessibility control without drawing another UI.
            if (!SetLayeredWindowAttributes(child, 0, 1, 2)) { Dispose(); return false; }
            SendMessage(child, 0x00c5, new IntPtr(Math.Max(1, Math.Min(16000, limit))), IntPtr.Zero); // EM_SETLIMITTEXT
            font = CreateFont(-Math.Max(18, Math.Min(36, area.Height / 3)), 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe UI");
            if (font != IntPtr.Zero) SendMessage(child, 0x0030, font, new IntPtr(1));
            procedure = HandleMessage;
            previousProcedure = SetWindowLongPointer(child, -4, Marshal.GetFunctionPointerForDelegate(procedure));
            if (previousProcedure == IntPtr.Zero) { Dispose(); return false; }
            Write(text, start, end); SetFocus(child); dirty = true;
            return true;
        }

        internal static bool ValidBounds(IntPtr window, Bounds area)
        {
            Rect client;
            return area.Width >= 60 && area.Height >= 22 && area.X >= 0 && area.Y >= 0 && GetClientRect(window, out client) &&
                (long)area.X + area.Width <= client.Right && (long)area.Y + area.Height <= client.Bottom;
        }
        internal bool Move(Bounds area)
        {
            if (!Active || !ValidBounds(parent, area)) return false;
            if (area.X != bounds.X || area.Y != bounds.Y || area.Width != bounds.Width || area.Height != bounds.Height)
            {
                SetWindowPos(child, IntPtr.Zero, area.X, area.Y, area.Width, area.Height, 0x0014); bounds = area;
            }
            return true;
        }
        internal void Write(string text, int start, int end)
        {
            if (!Active || composing) return;
            text = KeyboardPolicy.Clean(text, 16000, true);
            if (ReadText() != text) SetWindowText(child, text.Replace("\n", "\r\n"));
            // Rich IME composition must never be replaced by a stale browser echo.
            SendMessage(child, 0x00b1, new IntPtr(ToNativeOffset(text, start)), new IntPtr(ToNativeOffset(text, end)));
            lastText = null; dirty = true;
        }
        private static int ToNativeOffset(string text, int offset)
        { offset = Math.Max(0, Math.Min(offset, text.Length)); var extra = 0; for (var i = 0; i < offset; i++) if (text[i] == '\n') extra++; return offset + extra; }
        private static int FromNativeOffset(string text, int offset)
        { offset = Math.Max(0, Math.Min(offset, text.Length)); var extra = 0; for (var i = 0; i < offset; i++) if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n') extra++; return offset - extra; }
        internal Snapshot? Read()
        {
            if (!Active || !dirty && pendingAction == null) return null;
            dirty = false;
            var nativeText = ReadText();
            var text = KeyboardPolicy.Clean(nativeText, 16000, true);
            int start, end; SendMessageSelection(child, 0x00b0, out start, out end);
            start = FromNativeOffset(nativeText, start); end = FromNativeOffset(nativeText, end);
            // EDIT stores committed text separately from its IME preedit. Mirror
            // that preedit into the game's visible field without writing it back.
            if (composing)
            {
                var context = ImmGetContext(child);
                try
                {
                    if (context != IntPtr.Zero)
                    {
                        var length = ImmGetCompositionString(context, 8, null, 0); // GCS_COMPSTR
                        if (length > 0 && length <= 32000 && length % 2 == 0)
                        {
                            var bytes = new byte[length];
                            if (ImmGetCompositionString(context, 8, bytes, bytes.Length) == length)
                            {
                                var preview = KeyboardPolicy.Clean(Encoding.Unicode.GetString(bytes), 16000, true);
                                var caret = Math.Max(0, Math.Min(preview.Length, ImmGetCompositionString(context, 128, null, 0)));
                                start = Math.Min(start, text.Length); end = Math.Max(start, Math.Min(end, text.Length));
                                text = KeyboardPolicy.Clean(text.Substring(0, start) + preview + text.Substring(end), 16000, true);
                                start = end = Math.Min(text.Length, start + caret);
                            }
                        }
                    }
                }
                finally { if (context != IntPtr.Zero) ImmReleaseContext(child, context); }
            }
            var action = pendingAction; pendingAction = null;
            if (action == null && text == lastText && start == lastStart && end == lastEnd && composing == lastComposing && Focused == lastFocused) return null;
            lastText = text; lastStart = start; lastEnd = end; lastComposing = composing; lastFocused = Focused;
            return new Snapshot { Text = text, Start = start, End = end, Composing = composing, Focused = Focused, Action = action };
        }
        private string ReadText()
        {
            var length = Math.Min(32000, Math.Max(0, GetWindowTextLength(child)));
            var value = new StringBuilder(length + 1); GetWindowText(child, value, value.Capacity); return value.ToString();
        }
        internal void Blur()
        {
            if (Focused) SetFocus(parent);
            dirty = true;
        }
        internal bool UseLanguage(string language)
        {
            if (!Active) return false;
            var layouts = new IntPtr[64]; var count = Math.Min(64, GetKeyboardLayoutList(layouts.Length, layouts));
            for (var i = 0; i < count; i++)
            {
                var id = (int)(layouts[i].ToInt64() & 0xffff); var primary = id & 0x3ff;
                var match = language == "zh" ? id == 0x0804 || id == 0x1004 : language == "zh-Hant" ? id == 0x0404 || id == 0x0c04 || id == 0x1404 :
                    primary == (language == "en" ? 9 : language == "ja" ? 17 : language == "ko" ? 18 : language == "es" ? 10 : language == "fr" ? 12 : language == "ru" ? 25 : language == "pt-BR" ? 22 : language == "it" ? 16 : language == "id" ? 33 : language == "pl" ? 21 : language == "tr" ? 31 : -1);
                if (!match) continue;
                if (originalLayout == IntPtr.Zero) originalLayout = GetKeyboardLayout(0);
                // Activates only an already-installed layout on this UI thread.
                return ActivateKeyboardLayout(layouts[i], 0) != IntPtr.Zero;
            }
            return false;
        }
        private IntPtr HandleMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (message == 0x010d) { composing = true; dirty = true; } // WM_IME_STARTCOMPOSITION
                if (message == 0x010e) { composing = false; suppressEnter = true; dirty = true; }
                var key = wParam.ToInt64();
                if (message == 0x0100 && key == 13)
                {
                    if (composing) suppressEnter = true;
                    else if (!suppressEnter && (GetKeyState(16) & 0x8000) == 0) { pendingAction = "submit"; dirty = true; return IntPtr.Zero; }
                }
                if (message == 0x0101 && key == 13) suppressEnter = false;
                if (message == 0x0102 && key == 13 && !composing && (GetKeyState(16) & 0x8000) == 0) return IntPtr.Zero;
                if (message == 0x0100 && key == 27 && !composing) { pendingAction = "close"; dirty = true; return IntPtr.Zero; }
                // Edits from hardware, IME, UI Automation/voice, selection and paste
                // all flow through this one control. No process/window is observed.
                if (message == 0x000c || message >= 0x0100 && message <= 0x010f || message >= 0x0200 && message <= 0x020f || message == 0x00c2 || message == 0x0302 || message == 0x0303 || message == 0x0304 || message == 0x0008) dirty = true;
                return CallWindowProc(previousProcedure, window, message, wParam, lParam);
            }
            catch { return previousProcedure != IntPtr.Zero ? CallWindowProc(previousProcedure, window, message, wParam, lParam) : DefWindowProc(window, message, wParam, lParam); }
        }
        public void Dispose()
        {
            if (child != IntPtr.Zero)
            {
                if (IsWindow(child))
                {
                    if (GetFocus() == child && IsWindow(parent)) SetFocus(parent);
                    if (previousProcedure != IntPtr.Zero) SetWindowLongPointer(child, -4, previousProcedure);
                    SetWindowText(child, ""); DestroyWindow(child);
                }
                child = IntPtr.Zero;
            }
            if (font != IntPtr.Zero) { DeleteObject(font); font = IntPtr.Zero; }
            if (originalLayout != IntPtr.Zero) { ActivateKeyboardLayout(originalLayout, 0); originalLayout = IntPtr.Zero; }
            parent = previousProcedure = IntPtr.Zero; procedure = null; composing = suppressEnter = dirty = false; pendingAction = lastText = null;
            lastStart = lastEnd = -1;
        }
        internal static bool ClientSize(IntPtr window, out int width, out int height)
        { Rect rect; var ok = GetClientRect(window, out rect); width = rect.Right; height = rect.Bottom; return ok && width > 0 && height > 0; }
        internal static bool GameIsForeground(IntPtr gameWindow)
        { return gameWindow != IntPtr.Zero && GetAncestor(GetForegroundWindow(), 2) == gameWindow && !IsIconic(gameWindow); }

        [StructLayout(LayoutKind.Sequential)] private struct Rect { internal int Left, Top, Right, Bottom; }
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate IntPtr WindowProcedure(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        private delegate bool EnumProcedure(IntPtr window, IntPtr arg);
        private static IntPtr SetWindowLongPointer(IntPtr window, int index, IntPtr value)
        { return IntPtr.Size == 8 ? SetWindowLongPtr64(window, index, value) : new IntPtr(SetWindowLong32(window, index, value.ToInt32())); }
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateWindowEx(int exStyle, string type, string text, int style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProcedure callback, IntPtr arg);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr window, uint command);
        [DllImport("user32.dll")] private static extern IntPtr GetFocus();
        [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr window);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flag);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, out Rect rect);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder value, int size);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextLength(IntPtr window);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int size);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool SetWindowText(IntPtr window, string text);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern IntPtr SendMessageSelection(IntPtr window, uint message, out int start, out int end);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)] private static extern IntPtr SetWindowLongPtr64(IntPtr window, int index, IntPtr value);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)] private static extern int SetWindowLong32(IntPtr window, int index, int value);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CallWindowProc(IntPtr previous, IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr DefWindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr window, uint key, byte alpha, uint flags);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] private static extern short GetKeyState(int key);
        [DllImport("user32.dll")] private static extern int GetKeyboardLayoutList(int capacity, [Out] IntPtr[] layouts);
        [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint thread);
        [DllImport("user32.dll")] private static extern IntPtr ActivateKeyboardLayout(IntPtr layout, uint flags);
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CreateFont(int height, int width, int escapement, int orientation, int weight, uint italic, uint underline, uint strikeout, uint charset, uint precision, uint clip, uint quality, uint pitch, string face);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
        [DllImport("imm32.dll")] private static extern IntPtr ImmGetContext(IntPtr window);
        [DllImport("imm32.dll")] private static extern bool ImmReleaseContext(IntPtr window, IntPtr context);
        [DllImport("imm32.dll", EntryPoint = "ImmGetCompositionStringW")] private static extern int ImmGetCompositionString(IntPtr context, uint index, [Out] byte[] value, int length);
    }
}
