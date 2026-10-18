using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ABI_RC.Core.UI;
using HarmonyLib;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

[assembly: MelonInfo(typeof(ComfortUI.ComfortMod), "Comfort UI", "0.3.2", "Local UI Workshop")]
[assembly: MelonGame(null, "ChilloutVR")]

namespace ComfortUI
{
    // An independent local UI extension. No original game implementation is included.
    // This modification is unofficial and not supported by ChilloutVR or its team.
    public sealed class ComfortMod : MelonMod
    {
        internal static ComfortMod Instance;
        private readonly Dictionary<uint, AttachedView> views = new Dictionary<uint, AttachedView>();
        private static readonly FieldInfo HudViewField = typeof(CohtmlHud).GetField("hudView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private string javascript, stylesheet, settingsPath, pending, localTranslations = "{}", settingsTranslations, languagePacks;
        private Preferences preferences = new Preferences();
        private float nextScan;
        private bool initialized;
        private readonly ContentTranslator contentTranslator = new ContentTranslator();
        private volatile bool uiTyping;
        private readonly ConcurrentQueue<string> translationCommands = new ConcurrentQueue<string>();
        private int commandCount, testRunning, credentialRevision;
        private TranslationSecrets translationSecrets;
        private LocalModelService localModels;
        private CancellationTokenSource localPreparation;
        private int preparingLocal;
        private string pendingLocalState, localState = "[\"localIdle\",-1]";
        private readonly ConcurrentQueue<string> translationNotices = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<TranslationDelivery> translations = new ConcurrentQueue<TranslationDelivery>();

        public sealed class Preferences
        {
            public string language = "zh";
            public bool largeText = false;
            public bool highContrast = false;
            public bool reducedMotion = false;
            public string motionStyle = "fade";
            public string motionSpeed = "normal";
            public bool theme = true;
            public bool cameraLayout = true;
            public bool nameplateTheme = true;
            public bool chatTranslation = true, detailTranslation = true;
            public string translationProvider = "google", proxyMode = "system", proxyAddress = "http://127.0.0.1:7890", translationKey = "F8";
            public string sourceLanguage = "auto", aiEndpoint = "https://api.openai.com/v1", aiModel = "", microsoftRegion = "", libreEndpoint = "http://127.0.0.1:5000";
            public string localAcceleration = "cpu";
            [JsonIgnore] internal int TranslationRevision;
            public string palette = "graphite";
            public string background = "#282c33";
            public string panel = "#343a43";
            public string accent = "#c1cbdc";
        }

        private sealed class AttachedView
        {
            public CohtmlControlledView Owner;
            public CohtmlViewTarget Target;
            public cohtml.Net.BoundEventHandle SaveHandle;
            public cohtml.Net.BoundEventHandle ReportHandle;
            public cohtml.Net.BoundEventHandle TranslateHandle;
            public cohtml.Net.BoundEventHandle ToolsHandle;
            public CancellationTokenSource TranslationCancellation;
            public string TranslationRequest;
        }
        private sealed class TranslationDelivery
        {
            public AttachedView Entry;
            public string Request, Json;
        }

        public override void OnInitializeMelon()
        {
            Instance = this;
            javascript = ReadResource("ComfortUI.content-tools.js") + "\n" + ReadResource("ComfortUI.comfort.js");
            stylesheet = ReadResource("ComfortUI.comfort.css");
            settingsTranslations = JObject.Parse(ReadResource("ComfortUI.settings-zh.json")).ToString(Formatting.None);
            languagePacks = JObject.Parse(ReadResource("ComfortUI.locales.json")).ToString(Formatting.None);
            settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "ComfortUI", "preferences.json");
            var hasSavedPreferences = File.Exists(settingsPath);
            if (hasSavedPreferences)
            {
                try { preferences = ParsePreferences(File.ReadAllText(settingsPath)); }
                catch (Exception ex) { LoggerInstance.Warning("Preferences could not be read; using defaults: " + ex.GetType().Name); }
            }
            preferences.language = StartupLanguage.Choose(hasSavedPreferences, preferences.language, StartupLanguage.SystemLocale);
            if (!hasSavedPreferences)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                    // CreateNew cannot replace a configuration created concurrently.
                    using (var stream = new FileStream(settingsPath, FileMode.CreateNew, FileAccess.Write))
                    using (var writer = new StreamWriter(stream)) writer.Write(JsonConvert.SerializeObject(preferences, Formatting.Indented));
                }
                catch (Exception ex) { LoggerInstance.Warning("Initial preferences could not be saved: " + ex.GetType().Name); }
            }
            try { translationSecrets = new TranslationSecrets(Path.Combine(Path.GetDirectoryName(settingsPath), "translation-credentials.dat")); }
            catch (Exception ex) { LoggerInstance.Warning("Translation credentials unavailable: " + ex.GetType().Name); }
            contentTranslator.Diagnostic = value => LoggerInstance.Msg("Translation connection: " + value);
            localModels = new LocalModelService(Path.Combine(Path.GetDirectoryName(settingsPath), "LocalTranslation"), (code, percent) => Interlocked.Exchange(ref pendingLocalState, JsonConvert.SerializeObject(new object[] { code, percent, Volatile.Read(ref preparingLocal) != 0 })));
            var loaded = typeof(CohtmlControlledView).GetMethod("OnFinishLoad", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (loaded == null) throw new MissingMethodException("CohtmlControlledView.OnFinishLoad");
            HarmonyInstance.Patch(loaded, postfix: new HarmonyMethod(typeof(ComfortMod), nameof(AfterPageLoaded)));
            try { CameraUI.Install(HarmonyInstance, ReadResource("ComfortUI.camera-zh.json"), languagePacks); }
            catch (Exception ex) { LoggerInstance.Warning("Camera hooks unavailable: " + ex.GetType().Name + ". Native camera retained."); }
            try { ChatTranslation.Install(HarmonyInstance); }
            catch (Exception ex) { LoggerInstance.Warning("Chat translation hooks unavailable: " + ex.GetType().Name); }
            try { NativeLocalization.Install(HarmonyInstance, languagePacks, () => preferences.language); }
            catch (Exception ex) { LoggerInstance.Warning("Native message localization unavailable: " + ex.GetType().Name); }
            try { NameplateUI.Install(HarmonyInstance, () => preferences); }
            catch (Exception ex) { LoggerInstance.Warning("Nameplate theme unavailable: " + ex.GetType().Name); }
            try { localTranslations = ReadLocalTranslations(); }
            catch (Exception ex) { LoggerInstance.Warning("Optional translations skipped: " + ex.GetType().Name); }
            initialized = true;
            LoggerInstance.Msg("Comfort UI 0.3.2 ready. Unified menus, login, keyboard and HUD; preferences in game Settings.");
        }

        private static string ReadResource(string name)
        {
            using (var stream = typeof(ComfortMod).Assembly.GetManifestResourceStream(name))
            using (var reader = new StreamReader(stream ?? throw new FileNotFoundException(name)))
                return reader.ReadToEnd();
        }

        private static void AfterPageLoaded(CohtmlControlledView __instance)
        {
            Instance?.Attach(__instance, true);
        }

        public override void OnUpdate()
        {
            if (!initialized) return;
            TranslationDelivery delivery;
            while (translations.TryDequeue(out delivery))
            {
                AttachedView current;
                // IDs are captured while the page is alive. Never query a retained
                // native handle merely to decide whether a late reply is stale.
                if (views.TryGetValue(delivery.Entry.Target.Id, out current) && ReferenceEquals(current, delivery.Entry) && current.TranslationRequest == delivery.Request)
                    current.Target.Execute("if(window.ComfortContent)window.ComfortContent.receive(" + delivery.Json + ");");
            }
            // The native callback only queues changes; execute JS after the callback has returned.
            var incoming = Interlocked.Exchange(ref pending, null);
            if (incoming != null)
            {
                try
                {
                    preferences = ParsePreferences(incoming);
                    Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                    var temporary = settingsPath + ".tmp";
                    File.WriteAllText(temporary, JsonConvert.SerializeObject(preferences, Formatting.Indented));
                    if (File.Exists(settingsPath)) File.Replace(temporary, settingsPath, null);
                    else File.Move(temporary, settingsPath);
                    BroadcastPreferences();
                }
                catch (Exception ex)
                {
                    LoggerInstance.Warning("Preference update failed: " + ex.GetType().Name);
                    BroadcastPreferences();
                    Broadcast("if(window.ComfortUI)window.ComfortUI.preferenceFailed();");
                }
            }
            CameraUI.Tick(preferences);
            NameplateUI.Tick(preferences);
            ChatTranslation.Tick(preferences, contentTranslator, uiTyping);
            string command;
            while (translationCommands.TryDequeue(out command))
            {
                Interlocked.Decrement(ref commandCount);
                if (command.StartsWith("{", StringComparison.Ordinal)) SaveTranslationCredential(command);
                else if (command == "chat") ChatTranslation.Trigger();
                else if (command == "clearCache") { contentTranslator.ClearCache(); translationNotices.Enqueue("cacheCleared"); }
                else if (command == "localCancel") localPreparation?.Cancel();
                else if (command == "localPrepare")
                {
                    if (Interlocked.Exchange(ref preparingLocal, 1) != 0) continue;
                    var options = TranslationOptions(preferences);
                    var cancellation = new CancellationTokenSource(); localPreparation = cancellation;
                    Task.Run(async () => {
                        try { await localModels.Prepare(options, cancellation.Token).ConfigureAwait(false); }
                        catch (OperationCanceledException) { Interlocked.Exchange(ref pendingLocalState, "[\"localCancelled\",-1]"); }
                        catch (ContentTranslator.Failure ex) { Interlocked.Exchange(ref pendingLocalState, JsonConvert.SerializeObject(new object[] { ex.Code, -1 })); }
                        catch { Interlocked.Exchange(ref pendingLocalState, "[\"localDownloadFailed\",-1]"); }
                        finally { Interlocked.Exchange(ref preparingLocal, 0); }
                    });
                }
                else if (command == "test")
                {
                    if (Interlocked.Exchange(ref testRunning, 1) != 0) continue;
                    translationNotices.Enqueue("testing");
                    var current = preferences;
                    var options = TranslationOptions(current);
                    options.BypassCache = true;
                    Task.Run(async () =>
                    {
                        try { await contentTranslator.Translate(new[] { "Hello." }, current.language, CancellationToken.None, options).ConfigureAwait(false); translationNotices.Enqueue("testSuccess"); }
                        catch (ContentTranslator.Failure ex) { translationNotices.Enqueue(ex.Code); }
                        catch { translationNotices.Enqueue("network"); }
                        finally { Interlocked.Exchange(ref testRunning, 0); }
                    });
                }
            }
            string notice;
            while (translationNotices.TryDequeue(out notice)) NotifyTranslation(notice);
            var localUpdate = Interlocked.Exchange(ref pendingLocalState, null);
            if (localUpdate != null) { localState = localUpdate; Broadcast("if(window.ComfortUI)window.ComfortUI.localModelState.apply(null," + localUpdate + ");"); }
            if (Time.realtimeSinceStartup < nextScan) return;
            nextScan = Time.realtimeSinceStartup + 10f;
            // A slow discovery pass also covers views which existed before the hook was installed.
            foreach (var view in Resources.FindObjectsOfTypeAll<CohtmlControlledView>()) Attach(view, false);
        }

        public override void OnSceneWasLoaded(int index, string name) { nextScan = 0; }
        public override void OnDeinitializeMelon() { initialized = false; localPreparation?.Cancel(); localModels?.Dispose(); NameplateUI.Reset(); }

        private void Attach(CohtmlControlledView owner, bool reloaded)
        {
            try
            {
                if (!initialized || owner == null || string.IsNullOrEmpty(owner.Page)) return;
                var page = owner.Page.Replace('\\', '/').ToLowerInvariant();
                var context = PageContext(page);
                if (context == null) return;
                CohtmlViewTarget target;
                if (!CohtmlViewTarget.TryCreate(owner, out target)) return;
                var id = target.Id;
                AttachedView previous;
                if (views.TryGetValue(id, out previous) && !reloaded && previous.Target.IsCurrent && ReferenceEquals(previous.Owner, owner)) return;
                if (previous != null)
                {
                    previous.Target.TryUse(view => view.UnbindCall(previous.SaveHandle));
                    previous.Target.TryUse(view => view.UnbindCall(previous.ReportHandle));
                    previous.Target.TryUse(view => view.UnbindCall(previous.TranslateHandle));
                    previous.Target.TryUse(view => view.UnbindCall(previous.ToolsHandle));
                    lock (previous) { previous.TranslationCancellation?.Cancel(); previous.TranslationRequest = null; }
                }
                var entry = new AttachedView { Owner = owner, Target = target };
                if (!target.TryUse(view =>
                {
                    entry.SaveHandle = view.BindCall("ComfortUI_Save", new Action<string>(QueuePreferences));
                    entry.ReportHandle = view.BindCall("ComfortUI_Report", new Action<string>(ReportUI));
                    entry.ToolsHandle = view.BindCall("ComfortUI_TranslationTools", new Action<string>(value =>
                    {
                        if (value == "typing") uiTyping = true;
                        else if (value == "idle") uiTyping = false;
                        else if (value != null && value.Length <= 4096 && (value == "chat" || value == "test" || value == "clearCache" || value == "localPrepare" || value == "localCancel" || value.StartsWith("{", StringComparison.Ordinal)))
                        {
                            if (Interlocked.Increment(ref commandCount) <= 16) translationCommands.Enqueue(value);
                            else Interlocked.Decrement(ref commandCount);
                        }
                    }));
                    if (context == "main") entry.TranslateHandle = view.BindCall("ComfortUI_Translate", new Action<string>(json => QueueTranslation(entry, json)));
                })) return;
                views[id] = entry;
                var bootstrap = "window.__comfortContext=" + JsonConvert.SerializeObject(context) + ";" +
                    "window.__comfortHost={native:true,preferences:" + PublicPreferencesJson() + "};" +
                    "window.__comfortChatEnabled=" + (ChatTranslation.IsEnabled ? "true" : "false") + ";" +
                    "window.__comfortLocalModelState=" + localState + ";" +
                    "window.__comfortExistingTranslations=" + localTranslations + ";" +
                    "window.__comfortSettingsTranslations=" + settingsTranslations + ";" +
                    "window.__comfortLocales=" + languagePacks + ";" +
                    "window.__comfortStyles=" + JsonConvert.SerializeObject(stylesheet) + ";\n" + javascript;
                // OnFinishLoad handles reloads. Never clear another mod's initial scripts.
                if (!target.Execute(bootstrap)) { ForgetView(id); return; }
                LoggerInstance.Msg("Attached " + context + " (view " + id + ").");
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning("UI attach deferred: " + ex.GetType().Name);
            }
        }

        internal static string PageContext(string page)
        {
            // An explicit allowlist avoids injecting into content or arbitrary embedded web views.
            var path = page.Replace('\\', '/').Split('?', '#')[0].ToLowerInvariant();
            if (path.EndsWith("/cvrtest/index.html")) return "main";
            if (path.EndsWith("/gameui/quickmenu.html") || path.EndsWith("/gameui/menu.html")) return "quick";
            if (path.EndsWith("/login/login.html")) return "login";
            if (path.EndsWith("/keyboardui/keyboard.html")) return "keyboard";
            if (path.EndsWith("/cvr_hud/default/index.html") || path.EndsWith("/cvr_hud/relative/index.html")) return "hud";
            return null;
        }

        private void QueuePreferences(string json)
        {
            if (json == null || json.Length > 4096) return;
            try { ParsePreferences(json); Interlocked.Exchange(ref pending, json); }
            catch { /* malformed browser requests cannot write arbitrary files */ }
        }

        private void ReportUI(string json)
        {
            if (json == null || json.Length > 4096) return;
            // Reports contain only this mod's layout and action IDs, never game/user content.
            LoggerInstance.Msg("UI " + json.Replace('\r', ' ').Replace('\n', ' '));
        }

        private void QueueTranslation(AttachedView entry, string json)
        {
            if (json == null || json.Length > 32000) return;
            try
            {
                var request = JObject.Parse(json);
                var id = (string)request["id"];
                if (id == null || !Regex.IsMatch(id, "^cu-[0-9]+-[0-9]+$") || id.Length > 64) return;
                if ((bool?)request["cancel"] == true)
                {
                    lock (entry) if (entry.TranslationRequest == id) { entry.TranslationCancellation?.Cancel(); entry.TranslationRequest = null; }
                    return;
                }
                var language = (string)request["language"];
                var fields = request["texts"] as JArray;
                if (!LanguageSupport.Supported(language) || fields == null || fields.Count < 1 || fields.Count > 2) return;
                var texts = new string[fields.Count];
                for (int i = 0; i < texts.Length; i++)
                {
                    if (fields[i].Type != JTokenType.String || ((string)fields[i]).Length > 6000) return;
                    texts[i] = (string)fields[i];
                }
                var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(75));
                lock (entry)
                {
                    entry.TranslationCancellation?.Cancel();
                    entry.TranslationCancellation = cancellation;
                    entry.TranslationRequest = id;
                }
                var currentPreferences = preferences;
                var options = TranslationOptions(currentPreferences);
                Task.Run(async () =>
                {
                    var result = new JObject { ["id"] = id, ["language"] = language };
                    try
                    {
                        if (!currentPreferences.detailTranslation) throw new ContentTranslator.Failure("disabled");
                        result["texts"] = new JArray(await contentTranslator.Translate(texts, language, cancellation.Token, options).ConfigureAwait(false));
                    }
                    catch (ContentTranslator.Failure ex) { result["error"] = ex.Code; }
                    catch { result["error"] = "network"; }
                    finally
                    {
                        lock (entry) if (ReferenceEquals(entry.TranslationCancellation, cancellation)) entry.TranslationCancellation = null;
                        cancellation.Dispose();
                    }
                    translations.Enqueue(new TranslationDelivery { Entry = entry, Request = id, Json = result.ToString(Formatting.None) });
                });
            }
            catch { /* Invalid requests cannot select arbitrary network targets or expose game data. */ }
        }

        private string ReadLocalTranslations()
        {
            // Reuse the user's existing dictionaries locally; do not ship another mod's resources.
            var result = new JObject();
            var ui = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChilloutVR_Data", "StreamingAssets", "Cohtml", "UIResources");
            try
            {
                var main = ReadRegistration(Path.Combine(ui, "CVRTest", "_CVR_CN", "dist", "translations", "chinese.translation.js"));
                foreach (var prop in main.Properties()) if (prop.Value.Type == JTokenType.String) result[prop.Name] = prop.Value.DeepClone();
                var english = ReadRegistration(Path.Combine(ui, "GameUI", "translations", "english.translation.js"));
                var chinese = ReadRegistration(Path.Combine(ui, "GameUI", "_CVR_CN", "dist", "translations", "chinese.translation.js"));
                foreach (var prop in english.Properties())
                    if (prop.Value.Type == JTokenType.String && chinese[prop.Name]?.Type == JTokenType.String)
                        result[(string)prop.Value] = chinese[prop.Name].DeepClone();
            }
            catch (Exception ex) { LoggerInstance.Warning("Optional existing translations unavailable: " + ex.GetType().Name); }
            return JsonConvert.SerializeObject(result);
        }

        private static JObject ReadRegistration(string path)
        {
            if (!File.Exists(path)) return new JObject();
            var text = File.ReadAllText(path);
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end <= start) return new JObject();
            return JObject.Parse(text.Substring(start, end - start + 1));
        }

        internal static Preferences ParsePreferences(string json)
        {
            var obj = JObject.Parse(json);
            var p = new Preferences();
            if (obj["language"] != null)
            {
                var language = (string)obj["language"];
                if (!LanguageSupport.Supported(language)) throw new FormatException("Unsupported language");
                p.language = language;
            }
            p.largeText = ReadBool(obj, "largeText", false);
            p.highContrast = ReadBool(obj, "highContrast", false);
            p.reducedMotion = ReadBool(obj, "reducedMotion", false);
            var motionStyle = (string)obj["motionStyle"];
            if (motionStyle != null && motionStyle != "fade" && motionStyle != "slide" && motionStyle != "zoom") throw new FormatException("Unknown motion style");
            if (motionStyle != null) p.motionStyle = motionStyle;
            var motionSpeed = (string)obj["motionSpeed"];
            if (motionSpeed != null && motionSpeed != "fast" && motionSpeed != "normal" && motionSpeed != "gentle") throw new FormatException("Unknown motion speed");
            if (motionSpeed != null) p.motionSpeed = motionSpeed;
            p.theme = ReadBool(obj, "theme", true);
            p.cameraLayout = ReadBool(obj, "cameraLayout", true);
            p.nameplateTheme = ReadBool(obj, "nameplateTheme", true);
            p.chatTranslation = ReadBool(obj, "chatTranslation", true);
            p.detailTranslation = ReadBool(obj, "detailTranslation", true);
            var provider = (string)obj["translationProvider"];
            if (provider == "libre") provider = "local";
            if (provider != null && !TranslationProviders.ValidProvider(provider)) throw new FormatException("Unknown translation service");
            if (provider != null) p.translationProvider = provider;
            p.localAcceleration = (string)obj["localAcceleration"] ?? "cpu";
            if (p.localAcceleration != "cpu" && p.localAcceleration != "auto") throw new FormatException("Unknown local acceleration");
            p.sourceLanguage = (string)obj["sourceLanguage"] ?? p.sourceLanguage;
            if (p.sourceLanguage != "auto" && !LanguageSupport.Supported(p.sourceLanguage)) throw new FormatException("Unknown source language");
            p.aiEndpoint = (string)obj["aiEndpoint"] ?? p.aiEndpoint;
            p.libreEndpoint = (string)obj["libreEndpoint"] ?? p.libreEndpoint;
            p.aiModel = (string)obj["aiModel"] ?? p.aiModel;
            p.microsoftRegion = (string)obj["microsoftRegion"] ?? p.microsoftRegion;
            if (!TranslationProviders.ValidEndpoint(p.aiEndpoint) || !TranslationProviders.ValidEndpoint(p.libreEndpoint) || !TranslationProviders.ValidValue(p.aiModel, 120) || !TranslationProviders.ValidValue(p.microsoftRegion, 64)) throw new FormatException("Invalid service configuration");
            p.TranslationRevision = Instance?.credentialRevision ?? 0;
            var mode = (string)obj["proxyMode"];
            if (mode != null && mode != "system" && mode != "direct" && mode != "manual") throw new FormatException("Unknown proxy mode");
            if (mode != null) p.proxyMode = mode;
            var address = (string)obj["proxyAddress"];
            if (address != null && !ContentTranslator.ValidProxy(address)) throw new FormatException("Invalid proxy address");
            if (address != null) p.proxyAddress = address;
            var binding = (string)obj["translationKey"];
            if (binding != null && !ChatTranslation.ValidKey(binding)) throw new FormatException("Invalid translation key");
            if (binding != null) p.translationKey = binding;
            var palette = (string)obj["palette"];
            if (palette != null && palette != "graphite" && palette != "warm" && palette != "mist" && palette != "lavender" && palette != "custom") throw new FormatException("Unknown palette");
            if (palette != null) p.palette = palette;
            p.background = ReadColour(obj, "background", p.background);
            p.panel = ReadColour(obj, "panel", p.panel);
            p.accent = ReadColour(obj, "accent", p.accent);
            return p;
        }

        private static string ReadColour(JObject obj, string key, string fallback)
        {
            var value = (string)obj[key];
            if (value == null) return fallback;
            if (!Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$")) throw new FormatException(key);
            return value.ToLowerInvariant();
        }

        private static bool ReadBool(JObject obj, string key, bool fallback)
        {
            var token = obj[key];
            if (token == null) return fallback;
            if (token.Type != JTokenType.Boolean) throw new FormatException(key);
            return (bool)token;
        }

        private void BroadcastPreferences()
        {
            var script = "if(window.ComfortUI)window.ComfortUI.updatePreferences(" + PublicPreferencesJson() + ",true);";
            script += "if(window.ComfortUI&&window.ComfortUI.chatState)window.ComfortUI.chatState(" + (ChatTranslation.IsEnabled && preferences.chatTranslation ? "true" : "false") + ");";
            Broadcast(script);
        }
        private void ForgetView(uint id)
        {
            AttachedView entry;
            if (!views.TryGetValue(id, out entry)) return;
            lock (entry) { entry.TranslationCancellation?.Cancel(); entry.TranslationRequest = null; }
            views.Remove(id);
        }
        private void Broadcast(string script)
        {
            var stale = new List<uint>();
            foreach (var item in views)
                if (!item.Value.Target.Execute(script)) stale.Add(item.Key);
            foreach (var id in stale) ForgetView(id);
        }
        internal static ContentTranslator.Options TranslationOptions(Preferences value)
        {
            return new ContentTranslator.Options {
                Provider = value.translationProvider, ProxyMode = value.proxyMode, ProxyAddress = value.proxyAddress,
                SourceLanguage = value.sourceLanguage, AiEndpoint = value.aiEndpoint, AiModel = value.aiModel,
                LocalAcceleration = value.localAcceleration, LocalConnect = token => Instance.localModels.Connect(value.localAcceleration, token),
                AiKey = Instance?.translationSecrets?.Get("ai", value.aiEndpoint) ?? "",
            };
        }
        private string PublicPreferencesJson()
        {
            var data = JObject.FromObject(preferences);
            data["hasAiKey"] = !string.IsNullOrEmpty(translationSecrets?.Get("ai", preferences.aiEndpoint));
            return data.ToString(Formatting.None);
        }
        private void SaveTranslationCredential(string json)
        {
            try
            {
                var data = JObject.Parse(json);
                string provider = (string)data["provider"], key = (string)data["key"];
                if ((string)data["action"] != "credential" || provider != "ai" || !TranslationProviders.ValidValue(key, 1024)) throw new FormatException();
                // If an existing protected file cannot be read, do not overwrite it silently.
                if (translationSecrets == null) throw new InvalidOperationException();
                var endpoint = provider == "ai" ? preferences.aiEndpoint : provider == "libre" ? preferences.libreEndpoint : "";
                translationSecrets.Save(provider, endpoint, key);
                credentialRevision++;
                preferences = ParsePreferences(JsonConvert.SerializeObject(preferences));
                BroadcastPreferences();
                translationNotices.Enqueue(key.Length == 0 ? "keyCleared" : "keySaved");
            }
            catch { translationNotices.Enqueue("keySaveFailed"); }
        }
        internal void NotifyTranslation(string code)
        {
            // Diagnostic codes only: never log chat text, translations or identities.
            LoggerInstance.Msg("Translation status: " + code + " (chat mode: " + (ChatTranslation.IsEnabled ? "on" : "off") + ").");
            Broadcast("if(window.ComfortUI)window.ComfortUI.translationNotice(" + JsonConvert.SerializeObject(code) + "," + (ChatTranslation.IsEnabled ? "true" : "false") + ");");
            try
            {
                CohtmlViewTarget hud;
                if (CohtmlHud.Instance != null && CohtmlHud.IsReady && CohtmlViewTarget.TryCreate(HudViewField?.GetValue(CohtmlHud.Instance) as CohtmlControlledView, out hud) && (ABI_RC.Core.InteractionSystem.ViewManager.Instance == null || !ABI_RC.Core.InteractionSystem.ViewManager.Instance.IsViewShown))
                    CohtmlHud.Instance.ViewDropTextImmediate("Comfort UI", NativeLocalization.Label(code, preferences.language), "", "", false);
            }
            catch { }
        }
    }
}
