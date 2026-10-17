using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Win32;

namespace ComfortUI
{
    // Only on-demand retrieval. Providers and proxy modes are explicit local preferences.
    internal sealed class ContentTranslator : IDisposable
    {
        private readonly HttpClient client;
        private readonly SemaphoreSlim requests = new SemaphoreSlim(2, 2);
        private readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        private readonly object cacheLock = new object();
        internal sealed class Failure : Exception
        {
            internal readonly string Code;
            internal Failure(string code) : base(code) { Code = code; }
        }
        internal ContentTranslator(HttpMessageHandler handler = null)
        {
            if (handler == null) return;
            client = Configure(new HttpClient(handler));
        }
        private static HttpClient Configure(HttpClient client)
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.MaxResponseContentBufferSize = 128 * 1024;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ComfortUI/0.2.11");
            return client;
        }
        internal sealed class Options
        {
            internal string Provider = "mymemory", ProxyMode = "system", ProxyAddress = "";
            internal bool BypassCache;
        }
        internal static bool ValidProxy(string address)
        {
            Uri uri;
            return address != null && address.Length <= 256 && Uri.TryCreate(address, UriKind.Absolute, out uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https") && string.IsNullOrEmpty(uri.UserInfo) && uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
        }
        private static HttpClient CreateClient(Options options)
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false, UseProxy = options.ProxyMode != "direct" };
            if (options.ProxyMode == "manual") handler.Proxy = new WebProxy(options.ProxyAddress);
            else if (options.ProxyMode == "system")
            {
                handler.Proxy = WebRequest.GetSystemWebProxy();
                // Mono's system proxy does not consistently read the Windows Internet Settings.
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
                    {
                        if (Convert.ToInt32(key?.GetValue("ProxyEnable", 0)) == 1)
                        {
                            var server = key.GetValue("ProxyServer", "") as string;
                            if (!string.IsNullOrWhiteSpace(server))
                            {
                                if (server.Contains("="))
                                {
                                    foreach (var part in server.Split(';')) if (part.StartsWith("https=", StringComparison.OrdinalIgnoreCase)) server = part.Substring(6);
                                    if (server.Contains("=")) foreach (var part in server.Split(';')) if (part.StartsWith("http=", StringComparison.OrdinalIgnoreCase)) server = part.Substring(5);
                                }
                                if (!server.Contains("://")) server = "http://" + server;
                                if (ValidProxy(server)) handler.Proxy = new WebProxy(server);
                            }
                        }
                    }
                }
                catch { /* Default system proxy remains available. */ }
            }
            return Configure(new HttpClient(handler));
        }
        internal void ClearCache() { lock (cacheLock) cache.Clear(); }
        internal static List<string> Split(string text)
        {
            // Stay under the provider's 500 UTF-8 byte limit; never split a surrogate pair.
            var result = new List<string>();
            int start = 0;
            while (start < text.Length)
            {
                int end = start, bytes = 0, boundary = -1;
                while (end < text.Length)
                {
                    int count = char.IsHighSurrogate(text[end]) && end + 1 < text.Length && char.IsLowSurrogate(text[end + 1]) ? 2 : 1;
                    int next = Encoding.UTF8.GetByteCount(text.Substring(end, count));
                    if (bytes + next > 450) break;
                    bytes += next;
                    end += count;
                    if (char.IsWhiteSpace(text[end - 1]) || ".!?。！？".IndexOf(text[end - 1]) >= 0) boundary = end;
                }
                if (end < text.Length && boundary > start + (end - start) / 2) end = boundary;
                result.Add(text.Substring(start, end - start));
                start = end;
            }
            return result;
        }
        internal async Task<string[]> Translate(string[] texts, string language, CancellationToken token, Options options = null)
        {
            options = options ?? new Options();
            if (options.Provider != "google" && options.Provider != "mymemory") throw new Failure("invalid");
            if (options.ProxyMode != "system" && options.ProxyMode != "direct" && options.ProxyMode != "manual") throw new Failure("invalid");
            if (options.ProxyMode == "manual" && !ValidProxy(options.ProxyAddress)) throw new Failure("proxy");
            if (!LanguageSupport.Supported(language)) throw new Failure("invalid");
            if (texts == null || texts.Length < 1 || texts.Length > 2) throw new Failure("invalid");
            foreach (var text in texts) if (text == null || text.Length > 6000) throw new Failure("length");
            if (!await requests.WaitAsync(0, token).ConfigureAwait(false)) throw new Failure("busy");
            HttpClient transport = null;
            try
            {
                transport = client ?? CreateClient(options);
                var result = new string[texts.Length];
                for (int i = 0; i < texts.Length; i++)
                {
                    var translated = new StringBuilder();
                    foreach (var part in Split(texts[i]))
                    {
                        token.ThrowIfCancellationRequested();
                        string key = options.Provider + "\n" + language + "\n" + part, value;
                        value = null;
                        if (!options.BypassCache) lock (cacheLock) cache.TryGetValue(key, out value);
                        if (value == null)
                        {
                            string core = part.Trim();
                            if (core.Length == 0) value = part;
                            else
                            {
                                string target = LanguageSupport.TranslationTarget(language);
                                var url = options.Provider == "google"
                                    ? "https://translate.google.com/translate_a/single?client=gtx&sl=auto&tl=" + Uri.EscapeDataString(target) + "&dt=t&q=" + Uri.EscapeDataString(core)
                                    : "https://api.mymemory.translated.net/get?q=" + Uri.EscapeDataString(core) + "&langpair=" + Uri.EscapeDataString("autodetect|" + target);
                                using (var response = await transport.GetAsync(url, token).ConfigureAwait(false))
                                {
                                    if ((int)response.StatusCode == 429) throw new Failure("quota");
                                    if ((int)response.StatusCode == 403) throw new Failure("blocked");
                                    if (!response.IsSuccessStatusCode) throw new Failure("network");
                                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                    string output;
                                    if (options.Provider == "google")
                                    {
                                        var segments = JArray.Parse(body)[0] as JArray;
                                        if (segments == null) throw new Failure("network");
                                        var combined = new StringBuilder();
                                        foreach (var segment in segments) if (segment is JArray && segment[0]?.Type == JTokenType.String) combined.Append((string)segment[0]);
                                        output = combined.ToString();
                                    }
                                    else
                                    {
                                        var data = JObject.Parse(body);
                                        if ((bool?)data["quotaFinished"] == true || (int?)data["responseStatus"] == 429) throw new Failure("quota");
                                        if ((int?)data["responseStatus"] != 200 || data["responseData"]?["translatedText"]?.Type != JTokenType.String) throw new Failure("network");
                                        output = (string)data["responseData"]["translatedText"];
                                    }
                                    if (string.IsNullOrWhiteSpace(output)) throw new Failure("network");
                                    int left = 0, right = part.Length;
                                    while (left < part.Length && char.IsWhiteSpace(part[left])) left++;
                                    while (right > left && char.IsWhiteSpace(part[right - 1])) right--;
                                    value = part.Substring(0, left) + (options.Provider == "google" ? output : WebUtility.HtmlDecode(output)).Trim() + part.Substring(right);
                                }
                            }
                            lock (cacheLock)
                            {
                                if (cache.Count >= 256) cache.Clear();
                                cache[key] = value;
                            }
                        }
                        translated.Append(value);
                    }
                    result[i] = translated.ToString();
                }
                return result;
            }
            finally { if (transport != null && transport != client) transport.Dispose(); requests.Release(); }
        }
        public void Dispose() { client?.Dispose(); }
    }
}
