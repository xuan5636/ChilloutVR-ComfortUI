using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ABI_RC.Systems.ChatBox;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ComfortUI
{
    // Keep the native billboard shader, opacity, visibility, text and message lifetime.
    internal static class ChatBubbleUI
    {
        private static readonly FieldInfo Bubbles = AccessTools.Field(typeof(ChatBoxBubbleBehavior), "ChatBoxes");
        private static readonly FieldInfo Output = AccessTools.Field(typeof(ChatBoxBubbleBehavior), "textBubbleOutputTMP");
        private static readonly FieldInfo Hexagon = AccessTools.Field(typeof(ChatBoxBubbleBehavior), "textBubbleHexagonImg");
        private static readonly FieldInfo Round = AccessTools.Field(typeof(ChatBoxBubbleBehavior), "textBubbleRoundImg");
        private static readonly FieldInfo MessageRoot = AccessTools.Field(typeof(ChatBoxBubbleBehavior), "textBubbleTransform");
        private static readonly FieldInfo TypingRoot = AccessTools.Field(typeof(ChatBoxBubbleBehavior), "typingTransform");
        private static readonly FieldInfo TypingBackground = AccessTools.Field(typeof(ChatBoxBubbleBehavior), "typingBackground");
        private static readonly FieldInfo TypingFrames = AccessTools.Field(typeof(ChatBoxBubbleBehavior), "typingGoChildren");
        private static readonly Dictionary<ChatBoxBubbleBehavior, Session> sessions = new Dictionary<ChatBoxBubbleBehavior, Session>();
        private static readonly HashSet<ChatBoxBubbleBehavior> failed = new HashSet<ChatBoxBubbleBehavior>();
        private static readonly List<ChatBoxBubbleBehavior> removed = new List<ChatBoxBubbleBehavior>();
        private static ComfortMod.Preferences current;
        private static Sprite card;
        private static Texture2D texture;
        private static Sprite pointer;
        private static Texture2D pointerTexture;
        private static readonly Sprite[] dots = new Sprite[6];
        private static readonly Texture2D[] dotTextures = new Texture2D[6];
        private static float nextScan;
        private static bool installed;

        internal static void Install(HarmonyLib.Harmony harmony, Func<ComfortMod.Preferences> source)
        {
            if (Bubbles == null || Output == null || Hexagon == null || Round == null) throw new MissingFieldException("Native ChatBox UI unavailable");
            current = source();
            harmony.Patch(AccessTools.Method(typeof(ChatBoxBubbleBehavior), "Start"), postfix: new HarmonyMethod(typeof(ChatBubbleUI), nameof(Refresh)));
            harmony.Patch(AccessTools.Method(typeof(ChatBoxBubbleBehavior), "SetColor"), postfix: new HarmonyMethod(typeof(ChatBubbleUI), nameof(AfterColor)));
            harmony.Patch(AccessTools.Method(typeof(ChatBoxBubbleBehavior), "OnMessage"), postfix: new HarmonyMethod(typeof(ChatBubbleUI), nameof(Refresh)));
            harmony.Patch(AccessTools.Method(typeof(ChatBoxBubbleBehavior), "OnDestroy"), prefix: new HarmonyMethod(typeof(ChatBubbleUI), nameof(Remove)));
            harmony.Patch(AccessTools.Method(typeof(ChatBoxBubbleBehavior), "OnTyping"), postfix: new HarmonyMethod(typeof(ChatBubbleUI), nameof(Refresh)));
            harmony.Patch(AccessTools.Method(typeof(ChatBoxBubbleBehavior), "StopTyping"), postfix: new HarmonyMethod(typeof(ChatBubbleUI), nameof(Refresh)));
            harmony.Patch(AccessTools.Method(typeof(ChatBoxBubbleBehavior), "UpdateChatBox"), postfix: new HarmonyMethod(typeof(ChatBubbleUI), nameof(AfterNativeLayout)));
            installed = true;
        }
        internal static void Tick(ComfortMod.Preferences preferences)
        {
            if (!installed) return;
            var changed = !ReferenceEquals(current, preferences); current = preferences;
            if (!preferences.theme || !preferences.chatBubbleTheme) { RestoreSessions(); return; }
            if (!changed && Time.realtimeSinceStartup < nextScan) return;
            nextScan = Time.realtimeSinceStartup + .25f;
            var bubbles = Bubbles.GetValue(null) as IDictionary;
            if (bubbles != null) foreach (var value in bubbles.Values) Refresh(value as ChatBoxBubbleBehavior);
            removed.Clear();
            foreach (var pair in sessions) if (pair.Key == null) removed.Add(pair.Key);
            foreach (var bubble in removed) Remove(bubble);
        }
        internal static void Refresh(ChatBoxBubbleBehavior __instance)
        {
            if (!installed || current == null || !current.theme || !current.chatBubbleTheme || __instance == null || failed.Contains(__instance)) return;
            try
            {
                Session state;
                if (!sessions.TryGetValue(__instance, out state))
                {
                    var text = Output.GetValue(__instance) as TMP_Text;
                    var hexagon = Hexagon.GetValue(__instance) as Image; var round = Round.GetValue(__instance) as Image;
                    if (text == null || hexagon == null || round == null) return;
                    state = new Session(__instance, text, hexagon, round); sessions.Add(__instance, state);
                }
                state.Apply(current);
            }
            catch (Exception ex)
            {
                Remove(__instance); failed.Add(__instance);
                if (failed.Count <= 3) ComfortMod.Instance?.LoggerInstance.Warning("Chat bubble styling skipped: " + ex.GetType().Name);
            }
        }
        private static void AfterColor(ChatBoxBubbleBehavior __instance)
        {
            Session state;
            if (sessions.TryGetValue(__instance, out state)) state.NativeColorChanged();
            Refresh(__instance);
        }
        private static void AfterNativeLayout(ChatBoxBubbleBehavior __instance)
        {
            Session state;
            if (sessions.TryGetValue(__instance, out state)) state.NativeLayoutChanged();
            Refresh(__instance);
        }
        internal static void PrepareFontChange(ChatBoxBubbleBehavior bubble)
        {
            Session state;
            if (bubble != null && sessions.TryGetValue(bubble, out state)) state.PrepareFontChange();
        }
        private static void Remove(ChatBoxBubbleBehavior __instance)
        {
            if (ReferenceEquals(__instance, null)) return;
            Session state;
            if (sessions.TryGetValue(__instance, out state)) { state.Restore(); sessions.Remove(__instance); }
            failed.Remove(__instance);
        }
        private static void RestoreSessions() { foreach (var state in sessions.Values) state.Restore(); sessions.Clear(); failed.Clear(); }
        internal static void Reset()
        {
            installed = false; RestoreSessions(); current = null; nextScan = 0;
            if (card != null) Object.Destroy(card); if (texture != null) Object.Destroy(texture);
            card = null; texture = null;
            if (pointer != null) Object.Destroy(pointer); if (pointerTexture != null) Object.Destroy(pointerTexture);
            pointer = null; pointerTexture = null;
            for (var i = 0; i < dots.Length; i++)
            {
                if (dots[i] != null) Object.Destroy(dots[i]); if (dotTextures[i] != null) Object.Destroy(dotTextures[i]);
                dots[i] = null; dotTextures[i] = null;
            }
        }
        private static Sprite Card()
        {
            if (card != null) return card;
            const int size = 64, radius = 12;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "ComfortUI-Chat-Card", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            {
                var dx = Mathf.Max(radius - x, x - (size - radius - 1), 0); var dy = Mathf.Max(radius - y, y - (size - radius - 1), 0);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy)) * 255));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            card = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return card;
        }
        private static void Place(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(width, height);
        }
        private static Sprite Pointer()
        {
            if (pointer != null) return pointer;
            const int width = 40, height = 24;
            pointerTexture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "ComfortUI-Chat-Pointer", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var halfWidth = (y + .5f) * width / (height * 2f);
                var alpha = Mathf.Clamp01(halfWidth + .5f - Math.Abs(x + .5f - width / 2f));
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            pointerTexture.SetPixels32(pixels); pointerTexture.Apply(false, true);
            pointer = Sprite.Create(pointerTexture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, Vector4.zero);
            return pointer;
        }
        private static Sprite Dots(int frame)
        {
            frame %= dots.Length;
            if (dots[frame] != null) return dots[frame];
            const int width = 120, height = 40;
            var t = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "ComfortUI-Typing-Dots-" + frame, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var opacity = 0f;
                for (var i = 0; i < 3; i++)
                {
                    var dx = x + .5f - (28 + i * 32); var dy = y + .5f - height / 2f;
                    var edge = Mathf.Clamp01(8.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    var emphasis = .62f + .38f * (float)Math.Cos((frame - i * 2) * Math.PI / 3);
                    opacity = Math.Max(opacity, edge * emphasis);
                }
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)(opacity * 255));
            }
            t.SetPixels32(pixels); t.Apply(false, true); dotTextures[frame] = t;
            dots[frame] = Sprite.Create(t, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, Vector4.zero);
            return dots[frame];
        }
        private static float Linear(float channel) { return channel <= .04045f ? channel / 12.92f : (float)Math.Pow((channel + .055f) / 1.055f, 2.4); }

        private sealed class Session
        {
            private readonly ChatBoxBubbleBehavior bubble;
            private readonly TMP_Text text;
            private readonly Image[] backgrounds;
            private readonly Transform messageRoot, typingRoot;
            private readonly Image typingBackground;
            private readonly List<Image> typingFrames = new List<Image>();
            private readonly List<Image> arrows = new List<Image>();
            private Image typingArrow;
            private Vector3 nativeMessagePosition;
            private bool hasMessage, hasTyping;
            private float cardHeight = 90;
            private readonly List<Action> restore = new List<Action>();
            private readonly Color[] nativeColors = new Color[2];
            private ComfortMod.Preferences applied;
            private string content;
            private TMP_FontAsset font;
            private readonly FontStyles nativeStyle;
            private readonly FontWeight nativeWeight;
            internal Session(ChatBoxBubbleBehavior owner, TMP_Text label, Image hexagon, Image round)
            {
                bubble = owner;
                messageRoot = MessageRoot?.GetValue(owner) as Transform;
                typingRoot = TypingRoot?.GetValue(owner) as Transform;
                typingBackground = TypingBackground?.GetValue(owner) as Image;
                text = label; backgrounds = new[] { hexagon, round }; SaveRect(text.rectTransform);
                var margin = text.margin; var alignment = text.alignment; var color = text.color;
                var style = text.fontStyle; var weight = text.fontWeight; var size = text.fontSize;
                nativeStyle = style; nativeWeight = weight;
                var min = text.fontSizeMin; var max = text.fontSizeMax; var auto = text.enableAutoSizing;
                var wrapping = text.enableWordWrapping; var overflow = text.overflowMode;
                restore.Add(() => { if (text != null) { text.margin = margin; text.alignment = alignment; text.color = color; text.fontStyle = style; text.fontWeight = weight; text.fontSize = size; text.fontSizeMin = min; text.fontSizeMax = max; text.enableAutoSizing = auto; text.enableWordWrapping = wrapping; text.overflowMode = overflow; } });
                for (var i = 0; i < backgrounds.Length; i++)
                {
                    var index = i; var image = backgrounds[i]; SaveRect(image.rectTransform); nativeColors[i] = image.color;
                    var sprite = image.sprite; var type = image.type; var density = image.pixelsPerUnitMultiplier; var raycast = image.raycastTarget;
                    restore.Add(() => { if (image != null) { image.sprite = sprite; image.type = type; image.color = nativeColors[index]; image.pixelsPerUnitMultiplier = density; image.raycastTarget = raycast; } });
                }
                if (messageRoot != null)
                {
                    nativeMessagePosition = messageRoot.localPosition;
                    restore.Add(() => { if (messageRoot != null) messageRoot.localPosition = nativeMessagePosition; });
                }
                if (typingRoot != null)
                {
                    var position = typingRoot.localPosition; var scale = typingRoot.localScale;
                    restore.Add(() => { if (typingRoot != null) { typingRoot.localPosition = position; typingRoot.localScale = scale; } });
                }
                if (typingBackground != null) SaveImage(typingBackground);
                var frames = TypingFrames?.GetValue(owner) as IList;
                if (frames != null) foreach (var value in frames)
                {
                    var go = value as GameObject; var image = go != null ? go.GetComponent<Image>() : null;
                    if (image != null) { SaveImage(image); typingFrames.Add(image); }
                }
            }
            private void SaveImage(Image image)
            {
                SaveRect(image.rectTransform);
                var sprite = image.sprite; var type = image.type; var color = image.color;
                var density = image.pixelsPerUnitMultiplier; var raycast = image.raycastTarget;
                restore.Add(() => { if (image != null) { image.sprite = sprite; image.type = type; image.color = color; image.pixelsPerUnitMultiplier = density; image.raycastTarget = raycast; } });
            }
            internal void NativeLayoutChanged()
            {
                if (messageRoot != null) nativeMessagePosition = messageRoot.localPosition;
                applied = null;
            }
            private void SaveRect(RectTransform rect)
            {
                var min = rect.anchorMin; var max = rect.anchorMax; var pivot = rect.pivot; var position = rect.anchoredPosition; var size = rect.sizeDelta;
                restore.Add(() => { if (rect != null) { rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; } });
            }
            internal void NativeColorChanged()
            {
                for (var i = 0; i < backgrounds.Length; i++) if (backgrounds[i] != null) nativeColors[i] = backgrounds[i].color;
                applied = null;
            }
            internal void PrepareFontChange()
            {
                if (text != null) { text.fontStyle = nativeStyle; text.fontWeight = nativeWeight; }
                applied = null;
            }
            internal void Apply(ComfortMod.Preferences value)
            {
                if (text == null) return;
                var layout = !ReferenceEquals(applied, value) || content != text.text || font != text.font;
                var state = hasMessage != bubble.IsChatBubbleActive || hasTyping != bubble.IsTypingIndicatorActive;
                if (!layout && !state) return;
                var bg = value.background;
                switch (value.palette) { case "graphite": bg = "#282c33"; break; case "warm": bg = "#302d29"; break; case "mist": bg = "#29323a"; break; case "lavender": bg = "#302c37"; break; }
                Color background;
                if (!ColorUtility.TryParseHtmlString(bg, out background)) background = new Color(.16f, .17f, .20f);
                var luminance = .2126f * Linear(background.r) + .7152f * Linear(background.g) + .0722f * Linear(background.b);
                text.color = luminance > .179f ? new Color(.07f, .09f, .11f) : new Color(.97f, .98f, 1f); background.a = .94f;
                if (layout)
                {
                    text.margin = new Vector4(24, 16, 24, 16); text.alignment = TextAlignmentOptions.MidlineLeft; text.enableWordWrapping = true;
                    text.enableAutoSizing = true; text.fontSizeMin = 18; text.fontSize = text.fontSizeMax = value.largeText ? 46 : 42;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                    text.fontStyle = nativeStyle; text.fontWeight = nativeWeight;
                    var weights = text.font != null ? text.font.fontWeightTable : null;
                    if (weights != null && weights.Length > 7 && weights[7].regularTypeface == null && weights[7].italicTypeface == null) { text.fontWeight = FontWeight.Bold; text.fontStyle |= FontStyles.Bold; }
                    var natural = text.GetPreferredValues(text.text ?? "", float.PositiveInfinity, float.PositiveInfinity);
                    var width = Math.Max(200, Math.Min(740, natural.x + 48));
                    var wrapped = text.GetPreferredValues(text.text ?? "", width - 48, float.PositiveInfinity);
                    var height = Math.Max(90, Math.Min(360, wrapped.y + 32));
                    cardHeight = height;
                    Place(text.rectTransform, width, height);
                    foreach (var image in backgrounds) if (image != null)
                    {
                        image.sprite = Card(); image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 1; image.color = background; image.raycastTarget = false;
                        Place(image.rectTransform, width, height);
                        var arrow = ArrowFor(image); arrow.color = background; arrow.material = image.material;
                        Place(arrow.rectTransform, 40, 24); arrow.rectTransform.anchoredPosition = new Vector2(0, -height / 2f - 11);
                    }
                }
                // Keep the bottom pointer at a steady height over the native nameplate, even for long messages.
                var scale = messageRoot != null ? messageRoot.localScale : Vector3.one;
                var position = nativeMessagePosition;
                position.y += (cardHeight - 90) * .5f * scale.y;
                if (messageRoot != null) messageRoot.localPosition = position;
                if (typingBackground != null)
                {
                    typingBackground.sprite = Card(); typingBackground.type = Image.Type.Sliced; typingBackground.pixelsPerUnitMultiplier = 1;
                    typingBackground.color = background; typingBackground.raycastTarget = false; Place(typingBackground.rectTransform, 190, 88);
                    for (var i = 0; i < typingFrames.Count; i++) if (typingFrames[i] != null)
                    {
                        var image = typingFrames[i]; image.sprite = Dots(value.reducedMotion ? 0 : i); image.type = Image.Type.Simple;
                        image.color = text.color; image.raycastTarget = false; Place(image.rectTransform, 120, 40);
                    }
                    typingArrow = ArrowFor(typingBackground); typingArrow.color = background; typingArrow.material = typingBackground.material;
                    Place(typingArrow.rectTransform, 36, 22); typingArrow.rectTransform.anchoredPosition = new Vector2(0, -54);
                    typingArrow.gameObject.SetActive(!bubble.IsChatBubbleActive);
                    if (typingRoot != null)
                    {
                        // A lone typing hint sits on the same vertical axis as the player; when a message is visible it sits above it.
                        var typingPosition = nativeMessagePosition;
                        if (bubble.IsChatBubbleActive) typingPosition.y = position.y + (cardHeight / 2f + 64) * scale.y;
                        typingRoot.localPosition = typingPosition; typingRoot.localScale = scale;
                    }
                }
                hasMessage = bubble.IsChatBubbleActive; hasTyping = bubble.IsTypingIndicatorActive;
                content = text.text; font = text.font; applied = value;
            }
            private Image ArrowFor(Image background)
            {
                foreach (var arrow in arrows) if (arrow != null && arrow.transform.parent == background.transform) return arrow;
                var go = new GameObject("ComfortUI-Chat-Pointer", typeof(RectTransform), typeof(Image));
                go.layer = background.gameObject.layer; go.transform.SetParent(background.transform, false);
                var image = go.GetComponent<Image>(); image.sprite = Pointer(); image.raycastTarget = false; image.material = background.material;
                arrows.Add(image); return image;
            }
            internal void Restore()
            {
                foreach (var action in restore) try { action(); } catch { }
                foreach (var arrow in arrows) if (arrow != null) { arrow.gameObject.SetActive(false); Object.Destroy(arrow.gameObject); }
                arrows.Clear();
            }
        }
    }
}
