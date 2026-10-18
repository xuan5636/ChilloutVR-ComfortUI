using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ABI_RC.Core.Player;
using ABI_RC.Systems.PlayerColors;
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
        private static int playerColoursChanged;

        internal static void Install(HarmonyLib.Harmony harmony, Func<ComfortMod.Preferences> source)
        {
            current = source();
            harmony.Patch(RefreshNative, postfix: new HarmonyMethod(typeof(NameplateUI), nameof(AfterSettings)));
            harmony.Patch(AccessTools.Method(typeof(PlayerNameplate), "UpdateNamePlateState"), prefix: new HarmonyMethod(typeof(NameplateUI), nameof(BeforeState)), postfix: new HarmonyMethod(typeof(NameplateUI), nameof(AfterState)));
            harmony.Patch(AccessTools.Method(typeof(PlayerNameplate), "OnDestroy"), prefix: new HarmonyMethod(typeof(NameplateUI), nameof(BeforeDestroy)));
            PlayerColorsManager.OnRemotePlayerColorChanged += OnPlayerColoursChanged;
            installed = true;
        }

        // Network notifications only mark work; Unity components are updated by Tick on the main thread.
        private static void OnPlayerColoursChanged(string playerId, PlayerColors colours) { Interlocked.Exchange(ref playerColoursChanged, 1); }

        private static bool Eligible(PlayerNameplate plate)
        {
            return installed && !restoring && current != null && current.theme && current.nameplateTheme && plate != null && plate.Player != null && !plate.Player.IsLocalPlayer && plate.usrNameText != null && plate.nameplateBackground != null;
        }

        internal static void AfterSettings(PlayerNameplate __instance) { Apply(__instance, true); }
        private static void Apply(PlayerNameplate __instance, bool nativeRefresh, bool colourOnly = false)
        {
            if (!Eligible(__instance) || failed.Contains(__instance)) return;
            Session state = null;
            try
            {
                if (!sessions.TryGetValue(__instance, out state)) { sessions.Add(__instance, state = new Session(__instance)); state.Layout(current, nativeRefresh); }
                else if (colourOnly) state.RefreshTrim();
                else state.Layout(current, nativeRefresh);
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
            var coloursChanged = Interlocked.Exchange(ref playerColoursChanged, 0) != 0;
            var changed = !ReferenceEquals(value, current); current = value;
            if (changed && (!value.theme || !value.nameplateTheme)) RestoreSessions();
            if (!value.theme || !value.nameplateTheme || (!changed && !coloursChanged && Time.realtimeSinceStartup < nextScan)) return;
            nextScan = Time.realtimeSinceStartup + 1f;
            // The game's own live collection excludes unloaded players and inactive scene prefabs.
            foreach (var plate in PlayerNameplate.PlayerNameplates)
                if (Eligible(plate)) Apply(plate, false, !changed);
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
            PlayerColorsManager.OnRemotePlayerColorChanged -= OnPlayerColoursChanged;
            Interlocked.Exchange(ref playerColoursChanged, 0);
            installed = false; RestoreSessions(); current = null;
            if (card != null) Object.Destroy(card);
            if (texture != null) Object.Destroy(texture);
            card = null; texture = null;
        }

        private static Sprite Card()
        {
            if (card != null) return card;
            const int size = 64, radius = 8;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "ComfortUI-Nameplate-Card", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            {
                var dx = Mathf.Max(radius - x, x - (size - radius - 1), 0);
                var dy = Mathf.Max(radius - y, y - (size - radius - 1), 0);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy)) * 255));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            // Native world Canvas referencePixelsPerUnit is 100. At sprite PPU=100,
            // an 8px border becomes 8 world units and consumes the whole tiny card.
            // 64 effective pixels/unit keeps each corner at .125 world units.
            card = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 6400, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
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
            private readonly Outline trim;
            private readonly bool ownsTrim;
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
                    trim = plate.nameplateBackground.GetComponent<Outline>();
                    if (trim != null)
                    {
                        var enabled = trim.enabled; var colour = trim.effectColor;
                        var distance = trim.effectDistance; var useAlpha = trim.useGraphicAlpha;
                        restore.Add(() => { if (trim != null) { trim.enabled = enabled; trim.effectColor = colour; trim.effectDistance = distance; trim.useGraphicAlpha = useAlpha; } });
                    }
                    else
                    {
                        trim = plate.nameplateBackground.gameObject.AddComponent<Outline>();
                        ownsTrim = true;
                    }
                    go.layer = plate.nameplateBackground.gameObject.layer;
                    go.transform.SetParent(plate.nameplateBackground.transform, false);
                    speaking = go.GetComponent<Image>(); speaking.raycastTarget = false;
                    speaking.material = plate.nameplateBackground.material;
                    speaking.sprite = Card(); speaking.type = Image.Type.Sliced;
                    speaking.pixelsPerUnitMultiplier = 12;
                    speaking.enabled = false;
                }
                catch { if (ownsTrim && trim != null) Object.Destroy(trim); Object.Destroy(go); throw; }
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
                var sprite = image.sprite; var type = image.type; var colour = image.color; var density = image.pixelsPerUnitMultiplier;
                restore.Add(() => { if (image != null) { image.sprite = sprite; image.type = type; image.color = colour; image.pixelsPerUnitMultiplier = density; } });
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
                Plate.nameplateBackground.pixelsPerUnitMultiplier = 1;
                var name = Plate.usrNameText;
                name.margin = Vector4.zero; name.alignment = TextAlignmentOptions.Left;
                name.enableAutoSizing = true; name.fontSize = name.fontSizeMax = value.largeText ? .27f : .25f; name.fontSizeMin = .12f; name.overflowMode = TextOverflowModes.Ellipsis;
                var withRank = Plate.rankText != null && Plate.rankText.enabled && Plate.rankText.gameObject.activeSelf && !string.IsNullOrWhiteSpace(Plate.rankText.text);
                var leading = withImage ? .61f : .16f;
                var width = Math.Max(1.55f, Math.Min(3.2f, name.GetPreferredValues(name.text).x + leading + .38f));
                var left = -width / 2f; var textWidth = width - leading - .28f;
                var textX = left + leading + textWidth / 2f;
                Place(Plate.nameplateBackground.rectTransform, 0, .06f, width, withRank ? .56f : .48f);
                Place(name.rectTransform, textX, withRank ? .15f : .06f, textWidth, .29f);
                Place(speaking.rectTransform, 0, withRank ? -.25f : -.21f, width - .24f, .022f);
                // Synthesized bold uses the original atlas and billboard material. Never switch to an alternate font face.
                var weights = name.font != null ? name.font.fontWeightTable : null;
                if (weights != null && weights.Length > 7 && weights[7].regularTypeface == null && weights[7].italicTypeface == null) { name.fontWeight = FontWeight.Bold; name.fontStyle |= FontStyles.Bold; }
                name.color = foreground;
                if (Plate.rankText != null)
                {
                    Place(Plate.rankText.rectTransform, textX, -.075f, textWidth, .15f);
                    Plate.rankText.margin = Vector4.zero; Plate.rankText.alignment = TextAlignmentOptions.Left; Plate.rankText.overflowMode = TextOverflowModes.Ellipsis;
                    Plate.rankText.fontSize = .105f;
                    Plate.rankText.color = foreground;
                }
                if (Plate.playerImage != null) Place(Plate.playerImage.rectTransform, left + .30f, 0, .40f, .40f);
                if (Plate.friendsIndicator != null) { Place(Plate.friendsIndicator.rectTransform, width / 2f - .14f, 0, .17f, .22f); Plate.friendsIndicator.color = accent; }
                if (Plate.staffplateBackground != null)
                {
                    Plate.staffplateBackground.sprite = Card(); Plate.staffplateBackground.type = Image.Type.Sliced;
                    Plate.staffplateBackground.pixelsPerUnitMultiplier = 2;
                    Place(Plate.staffplateBackground.rectTransform, left + .47f, withRank ? .40f : .36f, .94f, .22f);
                }
                if (Plate.staffText != null)
                {
                    Place(Plate.staffText.rectTransform, 0, 0, .84f, .18f); Plate.staffText.rectTransform.localRotation = Quaternion.identity;
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
                if (nativeRepainted) RefreshTrim();
            }
            internal void RefreshTrim()
            {
                if (trim == null) return;
                // My Profile colours are separate from descriptor rank/role colours.
                // The game supplies the remote player's cached, network-synchronized selection.
                var colour = accent;
                var playerId = Plate.Player.PlayerId;
                if (!string.IsNullOrEmpty(playerId))
                    try { colour = PlayerColorsManager.GetPlayerColors(playerId).PrimaryColor; }
                    catch { /* The player cache may be unavailable during joining or teardown. Tick retries. */ }
                if (!Finite(colour.r) || !Finite(colour.g) || !Finite(colour.b)) colour = accent;
                colour = new Color(Mathf.Clamp01(colour.r), Mathf.Clamp01(colour.g), Mathf.Clamp01(colour.b), .65f);
                if (!trim.effectColor.Equals(colour)) trim.effectColor = colour;
                var distance = trim.effectDistance;
                if (distance.x != .015f || distance.y != .015f) trim.effectDistance = new Vector2(.015f, .015f);
                if (trim.useGraphicAlpha) trim.useGraphicAlpha = false;
                if (!trim.enabled) trim.enabled = true;
            }
            private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
            internal void Restore() { foreach (var action in restore) action(); restore.Clear(); Release(); }
            internal void Release() { if (speaking != null) Object.Destroy(speaking.gameObject); if (ownsTrim && trim != null) Object.Destroy(trim); }
        }
    }
}
