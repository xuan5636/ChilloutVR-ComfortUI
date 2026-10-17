using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using ABI_RC.Systems.Camera;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ComfortUI
{
    // The camera is a native world-space Canvas, not a Cohtml page. Keep its event system.
    internal static class CameraUI
    {
        private static CameraLabels labels;
        private static Session session;
        private static ComfortMod.Preferences preferences;
        private static readonly Dictionary<string, TMP_FontAsset> fonts = new Dictionary<string, TMP_FontAsset>();
        private static readonly Dictionary<string, Font> systemFonts = new Dictionary<string, Font>();
        private static readonly HashSet<string> fontAttempts = new HashSet<string>();
        private static bool dirty = true;
        private static bool wasInactive;
        private static float nextCheck;
        private static Sprite panelSprite;
        private static int warnings;

        internal static void Install(HarmonyLib.Harmony harmony, string dictionary, string languagePacks)
        {
            var words = new Dictionary<string, Dictionary<string, string>> { ["zh"] = JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionary) };
            var packs = JObject.Parse(languagePacks);
            foreach (var code in new[] { "ja", "ko", "es" }) words[code] = packs[code]["camera"].ToObject<Dictionary<string, string>>();
            labels = new CameraLabels(words);
            foreach (var name in new[] { "Start", "OnEnable", "OnDisable", "SetCaptureMode", "UpdateOptionsDisplay", "OnPostVRModeSwitch", "ToggleLockCameraUI" })
            {
                var method = AccessTools.Method(typeof(PortableCamera), name);
                if (method != null) harmony.Patch(method, postfix: new HarmonyMethod(typeof(CameraUI), nameof(CameraChanged)));
            }
            var push = AccessTools.Method(typeof(PortableCameraInterface), "Push");
            if (push != null) harmony.Patch(push, postfix: new HarmonyMethod(typeof(CameraUI), nameof(ControlsChanged)));
            var load = AccessTools.Method(typeof(PortableCameraSetting), "Load");
            if (load != null) harmony.Patch(load, postfix: new HarmonyMethod(typeof(CameraUI), nameof(ControlsChanged)));
            var show = AccessTools.Method(typeof(VisualMod), "SetShow");
            if (show != null) harmony.Patch(show, postfix: new HarmonyMethod(typeof(CameraUI), nameof(ControlsChanged)));
            var flash = AccessTools.Method(typeof(PortableCamera), "FlashMainText");
            if (flash != null) harmony.Patch(flash, prefix: new HarmonyMethod(typeof(CameraUI), nameof(TranslateFlash)));
        }

        private static void CameraChanged(PortableCamera __instance)
        {
            dirty = true;
            // Mode labels are rewritten by the game. Translate in the same frame, without polling.
            if (session != null && session.Camera == __instance) session.TranslateKnownText(__instance.captureModeText);
        }
        private static void ControlsChanged() { dirty = true; }
        private static void TranslateFlash(ref string __0)
        {
            if (preferences == null || labels == null) return;
            var source = labels.Source(__0);
            if (source != null) __0 = labels.Text(source, CameraLanguage);
        }
        private static TMP_FontAsset LocalizedFont
        {
            get { TMP_FontAsset font; return preferences != null && fonts.TryGetValue(preferences.language, out font) ? font : null; }
        }
        private static string CameraLanguage { get { return LocalizedFont != null ? preferences.language : "en"; } }

        internal static void Tick(ComfortMod.Preferences value)
        {
            if (labels == null) return;
            if (!ReferenceEquals(preferences, value)) dirty = true;
            preferences = value;
            if (session != null) session.Animate();
            var now = Time.realtimeSinceStartup;
            if (!dirty && now < nextCheck) return;
            nextCheck = now + 1f;
            var camera = PortableCamera.Instance;
            if (session != null && (session.Camera == null || session.Camera != camera)) { session.Restore(); session = null; }
            if (camera == null || camera.@interface == null || !camera.gameObject.activeInHierarchy) { wasInactive = true; dirty = false; return; }
            FontFor(preferences.language);
            if (wasInactive) { wasInactive = false; dirty = true; }
            if (session == null)
            {
                var fov = camera.@interface.settings.FirstOrDefault(s => s.OriginType == "PortableCamera" && s.SettingIdentifier == "Fov");
                if (fov == null || fov.Slider == null || camera.cameraCanvasGroup == null || camera.@interface.modContentRow == null) return;
                try { session = new Session(camera, fov); }
                catch (Exception ex) { dirty = false; Warn("Camera Canvas unavailable: " + ex.GetType().Name); return; }
                dirty = true;
            }
            // Discovery checks references and collection counts only; no per-frame Canvas scan.
            if (session.ModCount != camera.loadedMods.Count || session.SettingCount != camera.@interface.settings.Count) dirty = true;
            if (!dirty) return;
            dirty = false;
            try { session.Refresh(); }
            catch (Exception ex)
            {
                session.RestoreLayout();
                session.LayoutFailed = true;
                Warn("Camera layout restored after " + ex.GetType().Name + ". Original controls remain available.");
            }
        }

        private static void Warn(string message)
        {
            if (warnings++ < 4) ComfortMod.Instance?.LoggerInstance.Warning(message);
        }
        internal static TMP_FontAsset FontFor(string code)
        {
            TMP_FontAsset existing;
            if (fonts.TryGetValue(code, out existing)) return existing;
            if (!fontAttempts.Add(code)) return null;
            // Font(string path) is supported by this game's actual Unity runtime.
            // OS fallback fonts can render in Unity yet expose no CJK face to TMP.
            var filename = code == "ja" ? "NotoSansJP.ttf" : code == "ko" ? "NotoSansKR.ttf" : code == "zh" ? "NotoSansSC.ttf" : "NotoSans.ttf";
            var path = Path.Combine(Application.streamingAssetsPath, "Cohtml", "UIResources", "Fonts", filename);
            string failure = "No usable font face";
            for (var attempt = 0; attempt < 2; attempt++)
            {
                if (attempt == 0 && !File.Exists(path)) continue;
                Font source = null;
                TMP_FontAsset font = null;
                try
                {
                    source = attempt == 0 ? new Font(path) : Font.CreateDynamicFontFromOSFont(LanguageSupport.FontNames(code), 48);
                    if (source == null) throw new InvalidOperationException("Font source unavailable");
                    font = TMP_FontAsset.CreateFontAsset(source);
                    if (font == null) throw new InvalidOperationException("TMP font face unavailable");
                    font.name = "ComfortUI-Local-" + code;
                    font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    font.isMultiAtlasTexturesEnabled = true;
                    string missing;
                    font.TryAddCharacters(labels == null ? LanguageSupport.FontSample(code) : labels.Characters(code), out missing);
                    foreach (var glyph in LanguageSupport.FontSample(code))
                        if (!font.HasCharacter(glyph, true)) throw new InvalidOperationException("Required glyphs unavailable");
                    fonts[code] = font;
                    systemFonts[code] = source;
                    ComfortMod.Instance?.LoggerInstance.Msg("Local TMP font loaded for " + code + " from " + (attempt == 0 ? filename : "OS font") + ".");
                    return font;
                }
                catch (Exception ex)
                {
                    failure = ex.GetType().Name + ": " + ex.Message;
                    if (font != null) Object.Destroy(font);
                    if (source != null) Object.Destroy(source);
                }
            }
            Warn("Local font unavailable for " + code + ": " + failure + ". Original labels retained.");
            return null;
        }

        private static Sprite RoundedPanel()
        {
            if (panelSprite != null) return panelSprite;
            const int size = 48, radius = 10;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "ComfortUI-Camera-Panel", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            {
                var dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0);
                var dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0);
                var alpha = Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            panelSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            panelSprite.name = "ComfortUI-Camera-Rounded";
            return panelSprite;
        }

        private sealed class TextState
        {
            internal TMP_Text Node;
            internal string Original, Applied, Source;
            internal TMP_FontAsset Font;
            internal Color Color;
            internal float Size;
        }
        private sealed class ModState
        {
            internal VisualMod Mod;
            internal RectTransform Wrapper;
            internal CameraGroup Group;
            internal TMP_Text Caption;
            internal string Name;
        }

        private sealed class Session
        {
            internal readonly PortableCamera Camera;
            private readonly PortableCameraSetting fov;
            private readonly RectTransform canvas;
            private readonly Dictionary<TMP_Text, TextState> text = new Dictionary<TMP_Text, TextState>();
            private readonly List<Action> restore = new List<Action>();
            private readonly HashSet<int> savedRects = new HashSet<int>(), savedImages = new HashSet<int>();
            private readonly List<GameObject> owned = new List<GameObject>();
            private readonly List<ModState> mods = new List<ModState>();
            private readonly List<KeyValuePair<TMP_Text, string>> captions = new List<KeyValuePair<TMP_Text, string>>();
            private readonly Dictionary<TMP_Text, float> captionSizes = new Dictionary<TMP_Text, float>();
            private RectTransform common, drawer;
            private ScrollRect scroll;
            private CanvasGroup drawerGroup;
            private BoxCollider toolbarCollider, drawerCollider;
            private Button effectsButton, framingButton;
            private CameraGroup? selected;
            private bool built, lastLayoutRequested = true, reported;
            private float animationFrom, animationTo, animationStart, animationDuration;
            private Color background, panel, accent, foreground;
            internal int ModCount = -1, SettingCount = -1;
            internal bool LayoutFailed;

            internal Session(PortableCamera camera, PortableCameraSetting zoom)
            {
                Camera = camera; fov = zoom; canvas = camera.cameraCanvasGroup.transform as RectTransform;
                if (canvas == null) throw new InvalidOperationException("Native camera Canvas missing");
                RememberTexts();
            }

            private void RememberTexts()
            {
                foreach (var node in canvas.GetComponentsInChildren<TMP_Text>(true))
                    if (!owned.Contains(node.gameObject) && !text.ContainsKey(node)) text[node] = new TextState { Node = node, Original = node.text, Font = node.font, Color = node.color, Size = node.fontSize };
            }
            internal void TranslateKnownText(TMP_Text node)
            {
                if (node == null) return;
                TextState state;
                if (!text.TryGetValue(node, out state)) return;
                var source = state.Applied == node.text && state.Source != null ? state.Source : labels.Source(node.text);
                if (source == null) return;
                state.Source = source;
                var desired = labels.Text(source, CameraLanguage);
                if (node.text != desired) node.text = desired;
                state.Applied = desired;
                var font = LocalizedFont ?? state.Font;
                if (node.font != font) node.font = font;
            }
            internal void Refresh()
            {
                if (!preferences.cameraLayout && lastLayoutRequested) { RestoreLayout(); LayoutFailed = false; }
                if (preferences.cameraLayout && !lastLayoutRequested) LayoutFailed = false;
                lastLayoutRequested = preferences.cameraLayout;
                if (preferences.cameraLayout && !LayoutFailed && !built) Build();
                RememberTexts();
                foreach (var state in text.Values) TranslateKnownText(state.Node);
                foreach (var header in Camera.@interface.headers) if (header != null) TranslateKnownText(header.HeaderText);
                foreach (var setting in Camera.@interface.settings) if (setting != null) TranslateKnownText(setting.DisplayNameText);
                if (built)
                {
                    AddMods(); Theme(); UpdateCaptions(); UpdateVisibility();
                    if (preferences.reducedMotion) FinishAnimation();
                }
                ModCount = Camera.loadedMods.Count; SettingCount = Camera.@interface.settings.Count;
                if (built && !reported)
                {
                    reported = true;
                    ComfortMod.Instance?.LoggerInstance.Msg("Camera UI attached: native FOV=1, native mods=" + mods.Count + ", grouped controls=2, layer=" + canvas.gameObject.layer + ", raycaster=" + (canvas.GetComponent<GraphicRaycaster>() != null) + ", language=" + CameraLanguage + ". Client interaction still requires user verification.");
                }
            }

            private void SaveRect(RectTransform node)
            {
                if (node == null || !savedRects.Add(node.GetInstanceID())) return;
                var parent = node.parent; var index = node.GetSiblingIndex();
                var min = node.anchorMin; var max = node.anchorMax; var pivot = node.pivot;
                var pos = node.anchoredPosition3D; var size = node.sizeDelta; var scale = node.localScale; var rotation = node.localRotation;
                restore.Add(() => { if (node == null) return; if (parent != null) { node.SetParent(parent, false); node.SetSiblingIndex(index); } node.anchorMin = min; node.anchorMax = max; node.pivot = pivot; node.sizeDelta = size; node.anchoredPosition3D = pos; node.localScale = scale; node.localRotation = rotation; });
            }
            private void SaveImage(Image image)
            {
                if (image == null || !savedImages.Add(image.GetInstanceID())) return;
                var sprite = image.sprite; var type = image.type; var color = image.color;
                restore.Add(() => { if (image != null) { image.sprite = sprite; image.type = type; image.color = color; } });
            }
            private RectTransform NewRect(string name, Transform parent)
            {
                var go = new GameObject(name, typeof(RectTransform)) { layer = canvas.gameObject.layer };
                owned.Add(go); var rect = (RectTransform)go.transform; rect.SetParent(parent, false); return rect;
            }
            private static void Place(RectTransform rect, float x, float y, float width, float height)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity; rect.anchoredPosition3D = new Vector3(x, y, 0); rect.sizeDelta = new Vector2(width, height);
            }
            private static void Stretch(RectTransform rect, float margin = 0)
            {
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f);
                rect.offsetMin = new Vector2(margin, margin); rect.offsetMax = new Vector2(-margin, -margin); rect.localScale = Vector3.one;
            }
            private Image Panel(RectTransform rect)
            {
                var image = rect.gameObject.AddComponent<Image>(); image.sprite = RoundedPanel(); image.type = Image.Type.Sliced;
                image.color = panel; return image;
            }
            private TMP_Text Caption(Transform parent, string source, float size = 36)
            {
                var rect = NewRect("Comfort-Label", parent);
                var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = LocalizedFont ?? fov.DisplayNameText.font;
                label.spriteAsset = fov.DisplayNameText.spriteAsset; label.raycastTarget = false; label.enableWordWrapping = true;
                label.fontSize = size; label.alignment = TextAlignmentOptions.Center; label.color = foreground;
                label.enableAutoSizing = true; label.fontSizeMin = 24; label.fontSizeMax = size;
                label.overflowMode = TextOverflowModes.Ellipsis; captionSizes[label] = size;
                label.text = labels.Text(source, CameraLanguage); captions.Add(new KeyValuePair<TMP_Text, string>(label, source));
                return label;
            }
            private Button GroupButton(string source, float x, float width, CameraGroup group)
            {
                var rect = NewRect("Comfort-" + group, common); Place(rect, x, 0, width, 196);
                var image = Panel(rect); var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
                var colours = button.colors; colours.normalColor = Color.white; colours.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1); colours.pressedColor = new Color(.8f, .8f, .8f, 1); colours.fadeDuration = .08f; button.colors = colours;
                var caption = Caption(rect, source, 39); Stretch(caption.rectTransform, 18);
                button.onClick.AddListener(() => SelectGroup(selected == group ? (CameraGroup?)null : group));
                return button;
            }

            private void Build()
            {
                scroll = Camera.@interface.modContentRow.GetComponentInParent<ScrollRect>();
                if (scroll == null || scroll.viewport == null || scroll.content == null || canvas.GetComponent<GraphicRaycaster>() == null)
                    throw new InvalidOperationException("Native camera interaction contract changed");
                ReadColours();
                common = NewRect("Comfort-Camera-Common", canvas); Place(common, 0, -710, 2440, 220); Panel(common).color = background;
                toolbarCollider = common.gameObject.AddComponent<BoxCollider>(); toolbarCollider.isTrigger = true; toolbarCollider.size = new Vector3(2440, 220, .025f);
                drawer = (RectTransform)scroll.transform.parent; SaveRect(drawer);
                var active = drawer.gameObject.activeSelf; restore.Add(() => { if (drawer != null) drawer.gameObject.SetActive(active); });
                Place(drawer, 0, -1110, 2200, 520);
                var sr = (RectTransform)scroll.transform; SaveRect(sr); Stretch(sr);
                SaveRect(scroll.viewport); Stretch(scroll.viewport);
                SaveRect(scroll.content); scroll.content.anchorMin = new Vector2(0, 1); scroll.content.anchorMax = Vector2.one;
                scroll.content.pivot = new Vector2(.5f, 1); scroll.content.anchoredPosition = Vector2.zero; scroll.content.sizeDelta = Vector2.zero;
                var oldHorizontal = scroll.horizontal; var oldVertical = scroll.vertical; var oldSensitivity = scroll.scrollSensitivity;
                restore.Add(() => { if (scroll != null) { scroll.horizontal = oldHorizontal; scroll.vertical = oldVertical; scroll.scrollSensitivity = oldSensitivity; } });
                scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 55;
                var grid = scroll.content.GetComponent<GridLayoutGroup>();
                if (grid == null) throw new InvalidOperationException("Native camera grid missing");
                var oldCell = grid.cellSize; var oldSpacing = grid.spacing; var oldConstraint = grid.constraint; var oldCount = grid.constraintCount; var oldPadding = grid.padding; var oldAlign = grid.childAlignment;
                restore.Add(() => { if (grid != null) { grid.cellSize = oldCell; grid.spacing = oldSpacing; grid.constraint = oldConstraint; grid.constraintCount = oldCount; grid.padding = oldPadding; grid.childAlignment = oldAlign; } });
                grid.cellSize = new Vector2(400, 200); grid.spacing = new Vector2(20, 20); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 5;
                grid.padding = new RectOffset(60, 60, 24, 24); grid.childAlignment = TextAnchor.UpperCenter;
                drawerCollider = scroll.GetComponent<BoxCollider>();
                if (drawerCollider != null)
                {
                    var size = drawerCollider.size; var center = drawerCollider.center; var enabled = drawerCollider.enabled;
                    restore.Add(() => { if (drawerCollider != null) { drawerCollider.size = size; drawerCollider.center = center; drawerCollider.enabled = enabled; } });
                    drawerCollider.size = new Vector3(2200, 520, .025f); drawerCollider.center = Vector3.zero;
                }
                // The native mask, Canvas and raycaster stay in the same hierarchy and layer.
                var viewportImage = scroll.viewport.GetComponent<Image>(); SaveImage(viewportImage);
                if (viewportImage != null) { viewportImage.sprite = RoundedPanel(); viewportImage.type = Image.Type.Sliced; viewportImage.color = background; }
                drawerGroup = drawer.gameObject.GetComponent<CanvasGroup>();
                if (drawerGroup == null) { drawerGroup = drawer.gameObject.AddComponent<CanvasGroup>(); restore.Add(() => { if (drawerGroup != null) Object.Destroy(drawerGroup); }); }
                else
                {
                    var alpha = drawerGroup.alpha; var interactable = drawerGroup.interactable; var block = drawerGroup.blocksRaycasts;
                    restore.Add(() => { if (drawerGroup != null) { drawerGroup.alpha = alpha; drawerGroup.interactable = interactable; drawerGroup.blocksRaycasts = block; } });
                }
                MoveZoom();
                var settingsImage = Camera.settingsMenu != null ? Camera.settingsMenu.GetComponent<Image>() : null;
                SaveImage(settingsImage); if (settingsImage != null) { settingsImage.sprite = RoundedPanel(); settingsImage.type = Image.Type.Sliced; }
                effectsButton = GroupButton("Filters & effects", 600, 360, CameraGroup.Effects);
                framingButton = GroupButton("Framing & tracking", 1000, 400, CameraGroup.Framing);
                AddButtonCaptions(); built = true; AddMods();
                drawerGroup.alpha = 0; drawerGroup.interactable = drawerGroup.blocksRaycasts = false; drawer.gameObject.SetActive(false);
            }

            private void MoveZoom()
            {
                var root = fov.settingsObject.transform as RectTransform;
                if (root == null || fov.DisplayNameText == null || fov.ValueText == null) throw new InvalidOperationException("Native FOV control missing");
                SaveRect(root); root.SetParent(common, false); Place(root, -640, 0, 1120, 200);
                var title = fov.DisplayNameText.rectTransform; var value = fov.ValueText.rectTransform; var slider = (RectTransform)fov.Slider.transform;
                SaveRect(title); SaveRect(value); SaveRect(slider);
                title.anchorMin = title.anchorMax = title.pivot = new Vector2(0, 1); title.anchoredPosition = new Vector2(26, -15); title.sizeDelta = new Vector2(800, 56);
                value.anchorMin = value.anchorMax = value.pivot = Vector2.one; value.anchoredPosition = new Vector2(-26, -15); value.sizeDelta = new Vector2(210, 56);
                slider.anchorMin = Vector2.zero; slider.anchorMax = new Vector2(1, 0); slider.pivot = new Vector2(.5f, .5f); slider.anchoredPosition = new Vector2(0, 57); slider.sizeDelta = new Vector2(-70, 78);
                SaveImage(root.GetComponent<Image>()); var image = root.GetComponent<Image>();
                if (image != null) { image.sprite = RoundedPanel(); image.type = Image.Type.Sliced; }
            }
            private void AddButtonCaptions()
            {
                var sources = new Dictionary<string, string> { { "ButtonCapture", "Capture" }, { "ButtonCaptureMode", "Mode" }, { "ButtonWorldSpace", "Pin view" }, { "ButtonLock", "Lock" }, { "ButtonTimer", "Timer" }, { "ButtonMirroring", "Mirror" }, { "ButtonSwitchSides", "Flip" }, { "ButtonSettings", "Settings" } };
                var backgroundRoot = canvas.Find("Background");
                if (backgroundRoot == null) return;
                foreach (var pair in sources)
                {
                    var control = backgroundRoot.Find(pair.Key) as RectTransform; if (control == null) continue;
                    var image = control.GetComponent<Image>(); SaveImage(image); if (image != null) { image.sprite = RoundedPanel(); image.type = Image.Type.Sliced; }
                    var label = Caption(control, pair.Value, 35); Place(label.rectTransform, 0, -86, 200, 62);
                    // Native left-side buttons are mirrored. Counter-mirror the readable label.
                    label.rectTransform.localScale = new Vector3(control.localScale.x < 0 ? -1 : 1, 1, 1);
                    foreach (Transform child in control)
                        if (child.name.StartsWith("Icon", StringComparison.Ordinal) && child is RectTransform)
                        { var icon = (RectTransform)child; SaveRect(icon); icon.anchoredPosition = new Vector2(icon.anchoredPosition.x, 26); }
                }
            }
            private void AddMods()
            {
                foreach (var pair in Camera.loadedMods)
                {
                    if (pair.Value == null || mods.Any(m => m.Mod == pair.Value)) continue;
                    var mod = pair.Value; var group = CameraPolicy.Group(pair.Key.GetType().FullName);
                    var root = mod.transform as RectTransform; if (root == null) continue;
                    SaveRect(root); var wrapper = NewRect("Comfort-Group-Item", group == CameraGroup.Common ? common : Camera.@interface.modContentRow);
                    if (group == CameraGroup.Common) Place(wrapper, pair.Key.GetType().Name == "AutoFocus" ? 50 : 290, 0, 220, 196);
                    root.SetParent(wrapper, false); Stretch(root);
                    var image = root.GetComponent<Image>(); SaveImage(image); if (image != null) { image.sprite = RoundedPanel(); image.type = Image.Type.Sliced; }
                    var icon = root.Find("Icon") as RectTransform; if (icon != null) { SaveRect(icon); icon.anchoredPosition = new Vector2(0, 26); }
                    if (mod.activeOverlay != null)
                    {
                        SaveRect(mod.activeOverlay.rectTransform); Stretch(mod.activeOverlay.rectTransform); SaveImage(mod.activeOverlay);
                        mod.activeOverlay.sprite = RoundedPanel(); mod.activeOverlay.type = Image.Type.Sliced;
                    }
                    string name; try { name = pair.Key.GetModName("en"); } catch { name = pair.Key.GetType().Name; }
                    var caption = Caption(root, name, 34); Place(caption.rectTransform, 0, -65, group == CameraGroup.Common ? 205 : 380, 70);
                    mods.Add(new ModState { Mod = mod, Wrapper = wrapper, Group = group, Caption = caption, Name = name });
                }
            }
            private void UpdateCaptions()
            {
                foreach (var pair in captions)
                {
                    if (pair.Key == null) continue;
                    var value = labels.Text(pair.Value, CameraLanguage); if (pair.Key.text != value) pair.Key.text = value;
                    pair.Key.font = LocalizedFont ?? fov.DisplayNameText.font;
                    pair.Key.fontSizeMax = captionSizes[pair.Key] * (preferences.largeText ? 1.08f : 1);
                    pair.Key.fontSize = pair.Key.fontSizeMax;
                    pair.Key.color = foreground;
                }
                foreach (var mod in mods) if (mod.Mod.tooltipText != null) TranslateKnownText(mod.Mod.tooltipText);
            }
            private void UpdateVisibility()
            {
                foreach (var mod in mods)
                    if (mod.Mod != null && mod.Wrapper != null)
                    {
                        var show = CameraPolicy.Visible(mod.Group, selected, mod.Mod.IsModShown);
                        if (mod.Wrapper.gameObject.activeSelf != show) mod.Wrapper.gameObject.SetActive(show);
                    }
                if (toolbarCollider != null) toolbarCollider.enabled = true;
                if (drawerCollider != null) drawerCollider.enabled = selected != null;
            }
            private void SelectGroup(CameraGroup? group)
            {
                if (!built) return;
                selected = group; UpdateVisibility(); Theme();
                scroll.verticalNormalizedPosition = 1;
                if (group != null) drawer.gameObject.SetActive(true);
                animationFrom = drawerGroup.alpha; animationTo = group == null ? 0 : 1; animationStart = Time.realtimeSinceStartup;
                animationDuration = preferences.reducedMotion ? 0 : preferences.motionSpeed == "fast" ? .12f : preferences.motionSpeed == "gentle" ? .26f : .18f;
                drawerGroup.interactable = drawerGroup.blocksRaycasts = group != null;
                if (animationDuration == 0) FinishAnimation();
            }
            internal void Animate()
            {
                if (!built || drawerGroup == null || animationDuration <= 0) return;
                var progress = Mathf.Clamp01((Time.realtimeSinceStartup - animationStart) / animationDuration);
                drawerGroup.alpha = Mathf.Lerp(animationFrom, animationTo, 1 - Mathf.Pow(1 - progress, 3));
                if (progress >= 1) FinishAnimation();
            }
            private void FinishAnimation()
            {
                animationDuration = 0; if (drawerGroup == null) return;
                drawerGroup.alpha = selected == null ? 0 : 1;
                if (selected == null && drawer != null) drawer.gameObject.SetActive(false);
            }

            private void ReadColours()
            {
                var bg = preferences.background; var pnl = preferences.panel; var acc = preferences.accent;
                switch (preferences.palette)
                {
                    case "graphite": bg = "#282c33"; pnl = "#343a43"; acc = "#c1cbdc"; break;
                    case "warm": bg = "#302d29"; pnl = "#3c3832"; acc = "#dfcdb5"; break;
                    case "mist": bg = "#29323a"; pnl = "#35424d"; acc = "#bfd4e2"; break;
                    case "lavender": bg = "#302c37"; pnl = "#3d3746"; acc = "#d7c4e2"; break;
                }
                ColorUtility.TryParseHtmlString(bg, out background); ColorUtility.TryParseHtmlString(pnl, out panel); ColorUtility.TryParseHtmlString(acc, out accent);
                var linear = panel.linear; var luminance = .2126f * linear.r + .7152f * linear.g + .0722f * linear.b;
                foreground = luminance > .179f ? new Color(.06f, .07f, .09f) : new Color(.96f, .97f, .98f);
            }
            private void Theme()
            {
                ReadColours();
                if (common != null) common.GetComponent<Image>().color = background;
                if (effectsButton != null) effectsButton.GetComponent<Image>().color = selected == CameraGroup.Effects ? Color.Lerp(panel, accent, .32f) : panel;
                if (framingButton != null) framingButton.GetComponent<Image>().color = selected == CameraGroup.Framing ? Color.Lerp(panel, accent, .32f) : panel;
                if (scroll != null && scroll.viewport != null && scroll.viewport.GetComponent<Image>() != null) scroll.viewport.GetComponent<Image>().color = background;
                foreach (var mod in mods)
                {
                    if (mod.Mod == null) continue; var image = mod.Mod.GetComponent<Image>(); if (image != null) image.color = panel;
                    if (mod.Mod.activeOverlay != null) mod.Mod.activeOverlay.color = new Color(accent.r, accent.g, accent.b, .3f);
                }
                var fovImage = fov.settingsObject.GetComponent<Image>(); if (fovImage != null) fovImage.color = panel;
                fov.DisplayNameText.color = fov.ValueText.color = foreground;
                if (Camera.settingsMenu != null)
                {
                    var image = Camera.settingsMenu.GetComponent<Image>(); if (image != null) image.color = background;
                    foreach (var state in text.Values)
                        if (state.Node != null && state.Node.transform.IsChildOf(Camera.settingsMenu.transform))
                        { state.Node.color = foreground; state.Node.fontSize = state.Size * (preferences.largeText ? 1.08f : 1); }
                }
                var backgroundRoot = canvas.Find("Background");
                if (backgroundRoot != null) foreach (Transform child in backgroundRoot)
                    if (child.name.StartsWith("Button", StringComparison.Ordinal)) { var image = child.GetComponent<Image>(); if (image != null) image.color = panel; }
            }

            internal void RestoreLayout()
            {
                for (var i = restore.Count - 1; i >= 0; i--) try { restore[i](); } catch { }
                foreach (var go in owned) if (go != null) Object.Destroy(go);
                restore.Clear(); savedRects.Clear(); savedImages.Clear(); owned.Clear(); mods.Clear(); captions.Clear(); captionSizes.Clear();
                common = null; drawer = null; effectsButton = framingButton = null; drawerGroup = null; toolbarCollider = drawerCollider = null;
                built = false; reported = false; selected = null; animationDuration = 0;
                foreach (var state in text.Values) if (state.Node != null) { state.Node.color = state.Color; state.Node.fontSize = state.Size; }
            }
            internal void Restore()
            {
                RestoreLayout();
                foreach (var state in text.Values)
                    if (state.Node != null) { if (state.Node.text == state.Applied) state.Node.text = state.Source ?? state.Original; state.Node.font = state.Font; state.Node.color = state.Color; }
                text.Clear();
            }
        }
    }
}
