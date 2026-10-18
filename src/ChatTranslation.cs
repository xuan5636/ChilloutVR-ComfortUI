using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Systems.ChatBox;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComfortUI
{
    // A local display substitution. Never calls the chat network, TTS or message history.
    internal static class ChatTranslation
    {
        private sealed class Entry
        {
            internal ChatBoxBubbleBehavior Bubble;
            internal TMP_Text Label;
            internal string Original, Applied;
            internal TMP_FontAsset Font;
            internal Material Material, TranslatedMaterial;
            internal HashSet<Material> NativeMaterials;
            internal bool RichText;
            internal int Generation;
        }
        private sealed class Delivery { internal Entry Entry; internal string Text, Error; internal int Generation; internal CancellationTokenSource Completed; }
        private static readonly Dictionary<ChatBoxBubbleBehavior, Entry> entries = new Dictionary<ChatBoxBubbleBehavior, Entry>();
        private static readonly ConcurrentQueue<Delivery> deliveries = new ConcurrentQueue<Delivery>();
        private static readonly FieldInfo Bubbles = AccessTools.Field(typeof(ChatBoxBubbleBehavior), "ChatBoxes");
        private static readonly FieldInfo Output = AccessTools.Field(typeof(ChatBoxBubbleBehavior), "textBubbleOutputTMP");
        private static readonly FieldInfo Materials = AccessTools.Field(typeof(ChatBoxBubbleBehavior), "_materials");
        private static readonly FieldInfo InputField = AccessTools.Field(typeof(ViewManager), "_inputField");
        private static CancellationTokenSource cancellation;
        private static string settingsKey;
        private static ComfortMod.Preferences lastPreferences;
        private static int generation;
        private static bool trigger;
        private static bool keyboardOpen;
        private static bool enabled;
        private static float lastKey, nextScan;
        internal static bool IsEnabled { get { return enabled; } }

        internal static void Install(HarmonyLib.Harmony harmony)
        {
            if (Bubbles == null || Output == null) throw new MissingFieldException("Native ChatBox fields unavailable");
            harmony.Patch(AccessTools.Method(typeof(ChatBoxBubbleBehavior), "OnMessage"), prefix: new HarmonyMethod(typeof(ChatTranslation), nameof(BeforeMessage)));
            var destroyed = AccessTools.Method(typeof(ChatBoxBubbleBehavior), "OnDestroy");
            if (destroyed != null) harmony.Patch(destroyed, prefix: new HarmonyMethod(typeof(ChatTranslation), nameof(BeforeMessage)));
            var keyboard = AccessTools.TypeByName("ABI_RC.Core.UI.UIRework.Managers.KeyboardManager");
            if (keyboard != null)
            {
                harmony.Patch(AccessTools.Method(keyboard, "ShowKeyboard"), postfix: new HarmonyMethod(typeof(ChatTranslation), nameof(KeyboardShown)));
                foreach (var name in new[] { "OnKeyboardClose", "OnKeyboardSubmit", "OnMainMenuClose", "OnQuickMenuClose" })
                {
                    var method = AccessTools.Method(keyboard, name);
                    if (method != null) harmony.Patch(method, postfix: new HarmonyMethod(typeof(ChatTranslation), nameof(KeyboardClosed)));
                }
            }
        }
        private static void KeyboardShown() { keyboardOpen = true; }
        private static void KeyboardClosed() { keyboardOpen = false; }
        private static void BeforeMessage(ChatBoxBubbleBehavior __instance) { Forget(__instance); }
        internal static void Trigger() { trigger = true; }
        internal static bool ValidKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            KeyCode parsed;
            return ((key.Length == 1 && key[0] >= 'A' && key[0] <= 'Z') || System.Text.RegularExpressions.Regex.IsMatch(key, "^F(?:[1-9]|1[0-2])$") || System.Text.RegularExpressions.Regex.IsMatch(key, "^Alpha[0-9]$")) && Enum.TryParse(key, out parsed);
        }
        private static void Forget(ChatBoxBubbleBehavior bubble)
        {
            Entry entry;
            if (!entries.TryGetValue(bubble, out entry)) return;
            try
            {
                if (entry.Label != null)
                {
                    if (entry.Applied != null && entry.Label.text == entry.Applied) entry.Label.text = entry.Original;
                    entry.Label.font = entry.Font;
                    entry.Label.fontSharedMaterial = entry.Material;
                    entry.Label.richText = entry.RichText;
                    if (entry.Applied != null && entry.Label.isActiveAndEnabled) entry.Label.ForceMeshUpdate();
                }
            }
            catch { /* The native label may already be tearing down. Always release our material. */ }
            finally
            {
                if (entry.TranslatedMaterial != null)
                {
                    entry.NativeMaterials?.Remove(entry.TranslatedMaterial);
                    UnityEngine.Object.Destroy(entry.TranslatedMaterial);
                }
                entries.Remove(bubble);
            }
        }
        private static Material FontMaterial(Material original, TMP_FontAsset font)
        {
            // Chat bubbles are positioned and faced toward the viewer by the
            // game's BillboardFacing shader. TMP's font setter otherwise swaps
            // it for a regular SDF material, leaving text behind / back-to-front.
            // Preserve that shader and its properties; change only the font atlas
            // and its SDF metrics, as TMP does for a fallback font material.
            if (original == null || font == null || font.material == null) return null;
            var translated = new Material(original) { name = "ComfortUI-Chat-Font", hideFlags = HideFlags.DontSave };
            try
            {
                translated.SetTexture("_MainTex", font.material.GetTexture("_MainTex"));
                foreach (var property in new[] { "_TextureWidth", "_TextureHeight", "_GradientScale", "_WeightNormal", "_WeightBold" })
                    if (translated.HasProperty(property) && font.material.HasProperty(property)) translated.SetFloat(property, font.material.GetFloat(property));
                return translated;
            }
            catch { UnityEngine.Object.Destroy(translated); throw; }
        }
        private static void Reset()
        {
            generation++;
            cancellation?.Cancel();
            cancellation = null;
            foreach (var bubble in entries.Keys.ToArray()) Forget(bubble);
        }
        internal static void Tick(ComfortMod.Preferences preferences, ContentTranslator service, bool typing)
        {
            if (!ReferenceEquals(lastPreferences, preferences))
            {
                var next = string.Join("\n", preferences.language, preferences.translationProvider, preferences.proxyMode, preferences.proxyAddress, preferences.chatTranslation.ToString(),
                    preferences.localAcceleration, preferences.sourceLanguage, preferences.aiEndpoint, preferences.aiModel, preferences.microsoftRegion, preferences.libreEndpoint, preferences.TranslationRevision.ToString());
                if (next != settingsKey) { Reset(); settingsKey = next; }
                if (!preferences.chatTranslation) enabled = false;
                lastPreferences = preferences;
                lastKey = Time.realtimeSinceStartup;
            }
            // No scene-wide search or dictionary copy on every frame. Only the
            // visible batch and currently translated bubbles are inspected.
            var scan = Time.realtimeSinceStartup >= nextScan;
            if (scan)
            {
                nextScan = Time.realtimeSinceStartup + 0.35f;
                foreach (var pair in entries.ToArray())
                    if (pair.Key == null || !pair.Key.IsChatBubbleActive || pair.Value.Label == null || pair.Value.Label.text != (pair.Value.Applied ?? pair.Value.Original)) Forget(pair.Key);
            }
            Delivery result;
            string error = null;
            while (deliveries.TryDequeue(out result))
            {
                if (result.Completed != null)
                {
                    if (ReferenceEquals(cancellation, result.Completed)) cancellation = null;
                    if (result.Generation == generation && result.Completed.IsCancellationRequested) error = "timeout";
                    result.Completed.Dispose(); continue;
                }
                Entry current;
                if (result.Generation != generation || result.Entry.Bubble == null || !entries.TryGetValue(result.Entry.Bubble, out current) || !ReferenceEquals(current, result.Entry) || !current.Bubble.IsChatBubbleActive || current.Label == null || current.Label.text != current.Original) continue;
                if (result.Error != null) { error = result.Error; entries.Remove(current.Bubble); continue; }
                try
                {
                    var font = CameraUI.FontFor(preferences.language);
                    current.TranslatedMaterial = FontMaterial(current.Material, font);
                    if (current.TranslatedMaterial != null)
                    {
                        current.NativeMaterials?.Add(current.TranslatedMaterial);
                        current.Label.font = font;
                        current.Label.fontSharedMaterial = current.TranslatedMaterial;
                    }
                    current.Label.richText = false;
                    current.Applied = result.Text;
                    current.Label.text = result.Text;
                    // Rebuild the existing output, rather than leaving its old
                    // geometry visible for a frame or adding another text node.
                    current.Label.ForceMeshUpdate();
                }
                catch
                {
                    Forget(current.Bubble);
                    error = "response";
                }
            }
            if (error != null)
            {
                // A failed connection must not retry and show an error for every
                // new frame/message. The user can enable again after fixing it.
                enabled = false; Reset(); ComfortMod.Instance?.NotifyTranslation(error);
            }
            bool pressed = trigger; trigger = false;
            if (!typing && !keyboardOpen && Application.isFocused)
            {
                try
                {
                    var input = ViewManager.Instance == null ? null : InputField?.GetValue(ViewManager.Instance) as InputField;
                    if (input == null || !input.isFocused) pressed |= Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), preferences.translationKey));
                }
                catch { /* A missing legacy keyboard must not disable the in-menu action. */ }
            }
            if (pressed && Time.realtimeSinceStartup - lastKey >= 0.25f)
            {
                lastKey = Time.realtimeSinceStartup;
                if (enabled) { enabled = false; Reset(); ComfortMod.Instance?.NotifyTranslation("restored"); return; }
                if (!preferences.chatTranslation) { ComfortMod.Instance?.NotifyTranslation("disabled"); return; }
                enabled = true; nextScan = 0; scan = true;
                ComfortMod.Instance?.NotifyTranslation("chatEnabled");
            }
            if (enabled && scan && cancellation == null) Start(preferences, service);
        }
        private static void Start(ComfortMod.Preferences preferences, ContentTranslator service)
        {
            var dictionary = Bubbles.GetValue(null) as IDictionary;
            if (dictionary == null) return;
            var setup = ABI_RC.Core.Player.PlayerSetup.Instance;
            var camera = setup == null ? Camera.main : setup.activeCam;
            if (camera == null) return;
            var visible = new List<ChatBoxBubbleBehavior>();
            foreach (var value in dictionary.Values)
            {
                var bubble = value as ChatBoxBubbleBehavior;
                if (bubble == null || !bubble.isActiveAndEnabled || !bubble.IsChatBubbleActive || bubble.Player == null || bubble.Player.IsLocalPlayer || entries.ContainsKey(bubble)) continue;
                var label = Output.GetValue(bubble) as TMP_Text;
                if (label == null || !label.isActiveAndEnabled || string.IsNullOrWhiteSpace(label.text)) continue;
                if (camera != null)
                {
                    var point = camera.WorldToViewportPoint(label.transform.position);
                    if (point.z <= 0 || point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1) continue;
                }
                visible.Add(bubble);
            }
            // Snapshot Unity state on the main thread, only while the user has
            // explicitly enabled continuous translation for this session.
            var work = visible.Select(b => new Entry { Bubble = b, Label = Output.GetValue(b) as TMP_Text, NativeMaterials = Materials?.GetValue(b) as HashSet<Material> }).Where(e => e.Label != null).Select(e => { e.Original = e.Label.text; e.Font = e.Label.font; e.Material = e.Label.fontSharedMaterial; e.RichText = e.Label.richText; e.Generation = generation; return e; }).ToArray();
            if (work.Length == 0) return;
            foreach (var e in work) entries[e.Bubble] = e;
            var options = ComfortMod.TranslationOptions(preferences);
            var target = preferences.language;
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90)); cancellation = cts;
            Task.Run(async () =>
            {
                // Sequential requests avoid bursts and let the provider stop the entire batch on rate limits.
                string stopError = null;
                foreach (var e in work)
                {
                    if (cts.IsCancellationRequested) break;
                    try
                    {
                        if (stopError != null) { deliveries.Enqueue(new Delivery { Entry = e, Generation = e.Generation, Error = stopError }); continue; }
                        var output = await service.Translate(new[] { e.Original }, target, cts.Token, options).ConfigureAwait(false);
                        deliveries.Enqueue(new Delivery { Entry = e, Generation = e.Generation, Text = output[0] });
                    }
                    catch (OperationCanceledException) { break; }
                    catch (ContentTranslator.Failure ex) { stopError = ex.Code; deliveries.Enqueue(new Delivery { Entry = e, Generation = e.Generation, Error = ex.Code }); }
                    catch { stopError = "network"; deliveries.Enqueue(new Delivery { Entry = e, Generation = e.Generation, Error = "network" }); }
                }
                deliveries.Enqueue(new Delivery { Completed = cts, Generation = work[0].Generation });
                // Unity-facing state is cleaned on the main thread, not in this continuation.
            });
        }
    }
}
