using System;
using System.Collections.Generic;
using System.Reflection;
using ABI_RC.Core.Player;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ComfortUI
{
    // Style the existing Canvas. Native visibility, profile loading, names and billboard materials stay native.
    internal static class NameplateUI
    {
        private static readonly Dictionary<PlayerNameplate, Session> sessions = new Dictionary<PlayerNameplate, Session>();
        private static readonly HashSet<PlayerNameplate> failed = new HashSet<PlayerNameplate>();
        private static readonly MethodInfo RefreshNative = AccessTools.Method(typeof(PlayerNameplate), "UpdateNamePlateSettings");
        private static ComfortMod.Preferences current;
        private static bool installed, restoring;
        private static float nextScan;
        private static Sprite card;
        private static Texture2D texture;
        private static int warnings;

        internal static void Install(HarmonyLib.Harmony harmony, Func<ComfortMod.Preferences> source)
        {
            current = source();
            harmony.Patch(RefreshNative, postfix: new HarmonyMethod(typeof(NameplateUI), nameof(AfterSettings)));
            harmony.Patch(AccessTools.Method(typeof(PlayerNameplate), "UpdateNamePlateState"), prefix: new HarmonyMethod(typeof(NameplateUI), nameof(BeforeState)), postfix: new HarmonyMethod(typeof(NameplateUI), nameof(AfterState)));
            harmony.Patch(AccessTools.Method(typeof(PlayerNameplate), "OnDestroy"), prefix: new HarmonyMethod(typeof(NameplateUI), nameof(BeforeDestroy)));
            installed = true;
        }

        private static bool Eligible(PlayerNameplate plate)
        {
            return installed && !restoring && current != null && current.theme && current.nameplateTheme && plate != null && plate.Player != null && !plate.Player.IsLocalPlayer && plate.usrNameText != null && plate.nameplateBackground != null;
        }

        internal static void AfterSettings(PlayerNameplate __instance) { Apply(__instance, true); }
        private static void Apply(PlayerNameplate __instance, bool nativeRefresh)
        {
            if (!Eligible(__instance) || failed.Contains(__instance)) return;
            Session state = null;
            try
            {
                if (!sessions.TryGetValue(__instance, out state)) sessions.Add(__instance, state = new Session(__instance));
                state.Layout(current, nativeRefresh);
            }
            catch (Exception ex)
            {
                state?.Restore(); sessions.Remove(__instance); failed.Add(__instance);
                if (warnings++ < 3) ComfortMod.Instance?.LoggerInstance.Warning("Nameplate theme restored: " + ex.GetType().Name);
            }
        }

        // Harmony reads the native flags directly. No per-frame reflection or scene searches.
        internal static void BeforeState(bool ____isColorDirty, out bool __state) { __state = ____isColorDirty; }
        internal static void AfterState(PlayerNameplate __instance, bool ____wasMicActive, bool __state)
        {
            if (restoring || !installed || current == null || !current.theme || !current.nameplateTheme) return;
            Session state;
            if (sessions.TryGetValue(__instance, out state)) state.Speaking(____wasMicActive, __state);
        }
        internal static void BeforeDestroy(PlayerNameplate __instance)
        {
            Session state;
            if (sessions.TryGetValue(__instance, out state)) { sessions.Remove(__instance); state.Release(); }
            failed.Remove(__instance);
        }

        internal static void Tick(ComfortMod.Preferences value)
        {
            if (!installed) return;
            var changed = !ReferenceEquals(value, current); current = value;
            if (changed && (!value.theme || !value.nameplateTheme)) RestoreSessions();
            if (!value.theme || !value.nameplateTheme || (!changed && Time.realtimeSinceStartup < nextScan)) return;
            nextScan = Time.realtimeSinceStartup + 1f;
            // The game's own live collection excludes unloaded players and inactive scene prefabs.
            foreach (var plate in PlayerNameplate.PlayerNameplates)
                if (Eligible(plate) && (changed || !sessions.ContainsKey(plate))) Apply(plate, false);
            var dead = new List<PlayerNameplate>();
            foreach (var pair in sessions) if (pair.Key == null) { pair.Value.Release(); dead.Add(pair.Key); }
            foreach (var plate in dead) sessions.Remove(plate);
            failed.RemoveWhere(plate => plate == null);
        }

        private static void RestoreSessions()
        {
            restoring = true;
            try
            {
                foreach (var state in sessions.Values)
                {
                    state.Restore();
                    // Reapply current native avatar/membership settings, rather than a stale initial setting.
                    if (state.Plate != null && state.Plate.Player != null) try { RefreshNative.Invoke(state.Plate, null); } catch { }
                }
                sessions.Clear(); failed.Clear();
            }
            finally { restoring = false; }
        }

        internal static void Reset()
        {
            installed = false; RestoreSessions(); current = null;
            if (card != null) Object.Destroy(card);
            if (texture != null) Object.Destroy(texture);
            card = null; texture = null;
        }

        private static Sprite Card()
        {
            if (card != null) return card;
            const int size = 48, radius = 9;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "ComfortUI-Nameplate-Card", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            {
                var dx = Mathf.Max(radius - x, x - (size - radius - 1), 0);
                var dy = Mathf.Max(radius - y, y - (size - radius - 1), 0);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy)) * 255));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            card = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return card;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
        }
        private static float Linear(float channel) { return channel <= .04045f ? channel / 12.92f : (float)Math.Pow((channel + .055f) / 1.055f, 2.4); }

        private sealed class Session
        {
            internal readonly PlayerNameplate Plate;
            private readonly List<Action> restore = new List<Action>();
            private readonly Image speaking;
            private Color background, accent, foreground, staffColour;
            private bool? active;

            internal Session(PlayerNameplate plate)
            {
                Plate = plate;
                staffColour = plate.staffplateBackground != null ? plate.staffplateBackground.color : Color.white;
                SaveImage(plate.nameplateBackground); SaveImage(plate.staffplateBackground); SaveImage(plate.friendsIndicator);
                SaveText(plate.usrNameText); SaveText(plate.rankText); SaveText(plate.staffText);
                if (plate.playerImage != null) SaveRect(plate.playerImage.rectTransform);
                var go = new GameObject("Comfort-Speaking", typeof(RectTransform), typeof(Image));
                try
                {
                    go.layer = plate.nameplateBackground.gameObject.layer;
                    go.transform.SetParent(plate.nameplateBackground.transform, false);
                    speaking = go.GetComponent<Image>(); speaking.raycastTarget = false;
                    speaking.material = plate.nameplateBackground.material;
                    speaking.sprite = Card(); speaking.type = Image.Type.Sliced;
                    Place(speaking.rectTransform, 0, -.268f, 2.22f, .025f);
                    speaking.enabled = false;
                }
                catch { Object.Destroy(go); throw; }
            }
            private void SaveRect(RectTransform rect)
            {
                var min = rect.anchorMin; var max = rect.anchorMax; var pivot = rect.pivot;
                var position = rect.anchoredPosition; var size = rect.sizeDelta; var rotation = rect.localRotation;
                restore.Add(() => { if (rect != null) { rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; rect.localRotation = rotation; } });
            }
            private void SaveImage(Image image)
            {
                if (image == null) return;
                SaveRect(image.rectTransform);
                var sprite = image.sprite; var type = image.type; var colour = image.color;
                restore.Add(() => { if (image != null) { image.sprite = sprite; image.type = type; image.color = colour; } });
            }
            private void SaveText(TMP_Text text)
            {
                if (text == null) return;
                SaveRect(text.rectTransform);
                var margin = text.margin; var style = text.fontStyle; var weight = text.fontWeight;
                var min = text.fontSizeMin; var max = text.fontSizeMax; var size = text.fontSize;
                var automatic = text.enableAutoSizing; var alignment = text.alignment; var overflow = text.overflowMode; var colour = text.color;
                restore.Add(() => { if (text != null) { text.margin = margin; text.fontStyle = style; text.fontWeight = weight; text.fontSizeMin = min; text.fontSizeMax = max; text.fontSize = size; text.enableAutoSizing = automatic; text.alignment = alignment; text.overflowMode = overflow; text.color = colour; } });
            }
            internal void Layout(ComfortMod.Preferences value, bool nativeRefresh)
            {
                var bg = value.background; var acc = value.accent;
                switch (value.palette)
                {
                    case "graphite": bg = "#282c33"; acc = "#c1cbdc"; break;
                    case "warm": bg = "#302d29"; acc = "#dfcdb5"; break;
                    case "mist": bg = "#29323a"; acc = "#bfd4e2"; break;
                    case "lavender": bg = "#302c37"; acc = "#d7c4e2"; break;
                }
                ColorUtility.TryParseHtmlString(bg, out background); ColorUtility.TryParseHtmlString(acc, out accent);
                var luminance = .2126f * Linear(background.r) + .7152f * Linear(background.g) + .0722f * Linear(background.b);
                foreground = luminance > .179f ? new Color(.07f, .09f, .11f) : new Color(.97f, .98f, 1f);
                background.a = .94f;
                if (nativeRefresh && Plate.staffplateBackground != null) staffColour = Plate.staffplateBackground.color;
                var withImage = Plate.playerImage != null && Plate.playerImage.gameObject.activeSelf;
                Plate.nameplateBackground.sprite = Card(); Plate.nameplateBackground.type = Image.Type.Sliced;
                Place(Plate.nameplateBackground.rectTransform, 0, .06f, 2.65f, .62f);
                var name = Plate.usrNameText;
                Place(name.rectTransform, withImage ? .10f : -.07f, .12f, withImage ? 1.72f : 2.06f, .30f);
                name.margin = Vector4.zero; name.alignment = TextAlignmentOptions.Left;
                name.enableAutoSizing = true; name.fontSizeMax = value.largeText ? .27f : .25f; name.fontSizeMin = .12f; name.overflowMode = TextOverflowModes.Ellipsis;
                // Synthesized bold uses the original atlas and billboard material. Never switch to an alternate font face.
                var weights = name.font != null ? name.font.fontWeightTable : null;
                if (weights != null && weights.Length > 7 && weights[7].regularTypeface == null && weights[7].italicTypeface == null) { name.fontWeight = FontWeight.Bold; name.fontStyle |= FontStyles.Bold; }
                name.color = foreground;
                if (Plate.rankText != null)
                {
                    Place(Plate.rankText.rectTransform, withImage ? .10f : -.07f, -.10f, withImage ? 1.72f : 2.06f, .17f);
                    Plate.rankText.margin = Vector4.zero; Plate.rankText.alignment = TextAlignmentOptions.Left; Plate.rankText.overflowMode = TextOverflowModes.Ellipsis;
                    Plate.rankText.fontSize = .115f;
                    Plate.rankText.color = foreground;
                }
                if (Plate.playerImage != null) Place(Plate.playerImage.rectTransform, -1.02f, 0, .45f, .45f);
                if (Plate.friendsIndicator != null) { Place(Plate.friendsIndicator.rectTransform, 1.16f, 0, .23f, .27f); Plate.friendsIndicator.color = accent; }
                if (Plate.staffplateBackground != null)
                {
                    Plate.staffplateBackground.sprite = Card(); Plate.staffplateBackground.type = Image.Type.Sliced;
                    Place(Plate.staffplateBackground.rectTransform, -.77f, .43f, 1.06f, .24f);
                }
                if (Plate.staffText != null)
                {
                    Place(Plate.staffText.rectTransform, 0, 0, .96f, .20f); Plate.staffText.rectTransform.localRotation = Quaternion.identity;
                    Plate.staffText.margin = Vector4.zero; Plate.staffText.alignment = TextAlignmentOptions.Center;
                    Plate.staffText.enableAutoSizing = true; Plate.staffText.fontSizeMax = .13f; Plate.staffText.fontSizeMin = .07f; Plate.staffText.overflowMode = TextOverflowModes.Ellipsis;
                }
                Speaking(Plate.Player.IsMicActive(), true);
            }
            internal void Speaking(bool value, bool nativeRepainted)
            {
                if (Plate == null || Plate.nameplateBackground == null || (!nativeRepainted && active == value)) return;
                active = value;
                Plate.nameplateBackground.color = background;
                if (Plate.staffplateBackground != null) Plate.staffplateBackground.color = Color.Lerp(background, staffColour, .22f);
                if (speaking != null) { speaking.color = accent; speaking.enabled = value; }
            }
            internal void Restore() { foreach (var action in restore) action(); restore.Clear(); Release(); }
            internal void Release() { if (speaking != null) Object.Destroy(speaking.gameObject); }
        }
    }
}
