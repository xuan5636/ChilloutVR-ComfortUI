using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ComfortUI
{
    // Explicit providers; bounded asynchronous work, cached transports, no chat logging.
    internal sealed class ContentTranslator : IDisposable
    {
        private readonly HttpClient client;
        private readonly Func<Options, HttpClient> factory;
        private readonly SemaphoreSlim requests = new SemaphoreSlim(2, 2), wire = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        private readonly Dictionary<string, long> cooldowns = new Dictionary<string, long>();
        private readonly Dictionary<string, Transport> clients = new Dictionary<string, Transport>();
        private readonly object cacheLock = new object(), clientsLock = new object();
        private readonly int interval;
        private long nextPublicRequest;
        private bool disposed;
        internal Action<string> Diagnostic;
        private sealed class Transport { internal HttpClient Client; internal int Users; }
        internal sealed class Failure : Exception
        {
            internal readonly string Code;
            internal Failure(string code) : base(code) { Code = code; }
        }
        internal sealed class Options
        {
            internal string Provider = "mymemory", ProxyMode = "system", ProxyAddress = "", SourceLanguage = "auto";
            internal string AiEndpoint = "https://api.openai.com/v1", AiModel = "", AiKey = "";
            internal string MicrosoftKey = "", MicrosoftRegion = "", LibreEndpoint = "http://127.0.0.1:5000", LibreKey = "";
            internal string LocalAcceleration = "cpu";
            internal Func<CancellationToken, Task<LocalModelService.Connection>> LocalConnect;
            internal bool BypassCache;
            internal Options Snapshot() { return (Options)MemberwiseClone(); }
            internal string Identity()
            {
                using (var hash = SHA256.Create())
                    return Provider + "\n" + SourceLanguage + "\n" + AiEndpoint + "\n" + AiModel + "\n" + LocalAcceleration + "\n" + Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(AiKey)));
            }
        }
        internal ContentTranslator(HttpMessageHandler handler = null, int minimumIntervalMs = -1, Func<Options, HttpClient> clientFactory = null)
        {
            interval = minimumIntervalMs >= 0 ? minimumIntervalMs : handler == null && clientFactory == null ? 900 : 0;
            factory = clientFactory == null ? (Func<Options, HttpClient>)CreateClient : o => Configure(clientFactory(o));
            if (handler != null) client = Configure(new HttpClient(handler));
        }
        private static HttpClient Configure(HttpClient value)
        {
            value.Timeout = Timeout.InfiniteTimeSpan;
            value.MaxResponseContentBufferSize = 2 * 1024 * 1024;
            value.DefaultRequestHeaders.UserAgent.ParseAdd("ComfortUI/0.3.2");
            return value;
        }
        internal static bool ValidProxy(string address)
        {
            Uri uri;
            return address != null && address.Length <= 256 && Uri.TryCreate(address, UriKind.Absolute, out uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https") && string.IsNullOrEmpty(uri.UserInfo) && uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
        }
        internal static string SystemProxyAddress()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
                {
                    if (Convert.ToInt32(key?.GetValue("ProxyEnable", 0)) != 1) return "";
                    var server = key.GetValue("ProxyServer", "") as string;
                    if (string.IsNullOrWhiteSpace(server)) return "";
                    if (server.Contains("="))
                    {
                        var selected = server.Split(';').FirstOrDefault(p => p.StartsWith("https=", StringComparison.OrdinalIgnoreCase)) ?? server.Split(';').FirstOrDefault(p => p.StartsWith("http=", StringComparison.OrdinalIgnoreCase));
                        server = selected == null ? "" : selected.Substring(selected.IndexOf('=') + 1);
                    }
                    if (!server.Contains("://")) server = "http://" + server;
                    return ValidProxy(server) ? server : "";
                }
            }
            catch { return ""; }
        }
        private static HttpClient CreateClient(Options options)
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false, UseProxy = options.ProxyMode != "direct" };
            if (options.ProxyMode == "manual") handler.Proxy = new WebProxy(options.ProxyAddress);
            else if (options.ProxyMode == "system") handler.Proxy = WebRequest.GetSystemWebProxy();
            return Configure(new HttpClient(handler));
        }
        private Transport Acquire(Options options, string route)
        {
            if (client != null) return new Transport { Client = client };
            lock (clientsLock)
            {
                if (disposed) throw new OperationCanceledException();
                Transport value;
                if (!clients.TryGetValue(route, out value))
                {
                    foreach (var key in clients.Where(p => p.Value.Users == 0).Select(p => p.Key).ToArray())
                    {
                        if (clients.Count < 4) break;
                        clients[key].Client.Dispose(); clients.Remove(key);
                    }
                    value = new Transport { Client = factory(options) }; clients.Add(route, value);
                }
                value.Users++; return value;
            }
        }
        private void Release(Transport value) { if (client == null && value != null) lock (clientsLock) value.Users--; }
        internal void ClearCache() { lock (cacheLock) cache.Clear(); }
        internal static List<string> Split(string text, int maxBytes = 450)
        {
            var result = new List<string>();
            int start = 0;
            while (start < text.Length)
            {
                int end = start, bytes = 0, boundary = -1;
                while (end < text.Length)
                {
                    int count = char.IsHighSurrogate(text[end]) && end + 1 < text.Length && char.IsLowSurrogate(text[end + 1]) ? 2 : 1;
                    int next = Encoding.UTF8.GetByteCount(text.Substring(end, count));
                    if (bytes + next > maxBytes) break;
                    bytes += next; end += count;
                    if (char.IsWhiteSpace(text[end - 1]) || ".!?。！？".IndexOf(text[end - 1]) >= 0) boundary = end;
                }
                if (end < text.Length && boundary > start + (end - start) / 2) end = boundary;
                result.Add(text.Substring(start, end - start)); start = end;
            }
            return result;
        }
        internal async Task<string[]> Translate(string[] texts, string language, CancellationToken token, Options options = null)
        {
            options = options ?? new Options();
            TranslationProviders.Validate(options);
            if (options.ProxyMode != "system" && options.ProxyMode != "direct" && options.ProxyMode != "manual") throw new Failure("invalid");
            if (options.ProxyMode == "manual" && !ValidProxy(options.ProxyAddress)) throw new Failure("proxy");
            if (!LanguageSupport.Supported(language)) throw new Failure("invalid");
            if (texts == null || texts.Length < 1 || texts.Length > 2) throw new Failure("invalid");
            foreach (var text in texts) if (text == null || text.Length > 6000) throw new Failure("length");
            if (!await requests.WaitAsync(0, token).ConfigureAwait(false)) throw new Failure("busy");
            Transport transport = null;
            try
            {
                var settings = options.Snapshot();
                if (settings.Provider == "local") { settings.ProxyMode = "direct"; settings.ProxyAddress = ""; }
                else if (settings.ProxyMode == "system")
                {
                    var proxy = SystemProxyAddress();
                    if (proxy.Length > 0) { settings.ProxyMode = "manual"; settings.ProxyAddress = proxy; }
                }
                var route = settings.ProxyMode + "\n" + (settings.ProxyMode == "manual" ? settings.ProxyAddress : "");
                var identity = options.Identity();
                var cooldownKey = identity + "\n" + route;
                var result = new string[texts.Length];
                for (int i = 0; i < texts.Length; i++)
                {
                    var translated = new StringBuilder();
                    var parts = options.Provider == "mymemory" ? Split(texts[i]) : options.Provider == "google" ? Split(texts[i], 1800) : new List<string> { texts[i] };
                    foreach (var part in parts)
                    {
                        token.ThrowIfCancellationRequested();
                        string key = identity + "\n" + language + "\n" + part, value = null;
                        if (!options.BypassCache) lock (cacheLock) cache.TryGetValue(key, out value);
                        if (value == null && part.Trim().Length == 0) value = part;
                        if (value == null)
                        {
                            await wire.WaitAsync(token).ConfigureAwait(false);
                            try
                            {
                                if (!options.BypassCache) lock (cacheLock) cache.TryGetValue(key, out value);
                                if (value == null)
                                {
                                    long until;
                                    lock (cacheLock) if (cooldowns.TryGetValue(cooldownKey, out until) && Stopwatch.GetTimestamp() < until) throw new Failure("quota");
                                    bool paced = options.Provider == "google" || options.Provider == "mymemory" || options.Provider == "microsoft";
                                    var wait = (nextPublicRequest - Stopwatch.GetTimestamp()) * 1000 / Stopwatch.Frequency;
                                    if (paced && wait > 0) await Task.Delay((int)Math.Min(wait, interval), token).ConfigureAwait(false);
                                    if (transport == null) transport = Acquire(settings, route);
                                    string output;
                                    try { output = await TranslationProviders.Translate(transport.Client, part.Trim(), language, settings, token, Diagnostic).ConfigureAwait(false); }
                                    catch (Failure ex)
                                    {
                                        if (ex.Code == "quota") lock (cacheLock)
                                        {
                                            if (cooldowns.Count >= 16) cooldowns.Clear();
                                            cooldowns[cooldownKey] = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 60;
                                        }
                                        throw;
                                    }
                                    finally { if (paced) nextPublicRequest = Stopwatch.GetTimestamp() + Stopwatch.Frequency * interval / 1000; }
                                    int left = 0, right = part.Length;
                                    while (left < part.Length && char.IsWhiteSpace(part[left])) left++;
                                    while (right > left && char.IsWhiteSpace(part[right - 1])) right--;
                                    value = part.Substring(0, left) + output.Trim() + part.Substring(right);
                                    lock (cacheLock) { if (cache.Count >= 256) cache.Clear(); cache[key] = value; }
                                }
                            }
                            finally { wire.Release(); }
                        }
                        translated.Append(value);
                    }
                    result[i] = translated.ToString();
                }
                return result;
            }
            finally { Release(transport); requests.Release(); }
        }
        public void Dispose()
        {
            lock (clientsLock) { disposed = true; foreach (var value in clients.Values) value.Client.Dispose(); clients.Clear(); }
            client?.Dispose();
        }
    }
}
