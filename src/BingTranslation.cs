using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ComfortUI
{
    // Public Bing web translation. Session tokens stay in memory, per proxy transport.
    // A challenge, quota or unavailable page is an error; no challenge bypass or Azure key.
    internal static class BingTranslation
    {
        private sealed class Session { internal string Ig, Key, Token; internal long Expires; }
        private static readonly ConditionalWeakTable<HttpClient, Session> Sessions = new ConditionalWeakTable<HttpClient, Session>();
        internal static async Task<string> Translate(HttpClient client, string text, string target, ContentTranslator.Options options, CancellationToken token, Action<string> diagnostic)
        {
            var state = Sessions.GetValue(client, _ => new Session());
            if (state.Expires < Stopwatch.GetTimestamp())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, "https://www.bing.com/translator"))
                {
                    request.Headers.UserAgent.ParseAdd("Mozilla/5.0 ComfortUI/0.3.2");
                    var html = await TranslationProviders.Send(client, request, options, token, diagnostic).ConfigureAwait(false);
                    var ig = Regex.Match(html, "\\bIG:\\s*\"([A-Za-z0-9]+)\"");
                    var helper = Regex.Match(html, @"params_AbusePreventionHelper\s*=\s*(\[[^;\r\n]+\])");
                    if (!ig.Success || !helper.Success) throw new ContentTranslator.Failure("blocked");
                    var data = JArray.Parse(helper.Groups[1].Value);
                    if (data.Count < 2 || data[0].Type != JTokenType.Integer || data[1].Type != JTokenType.String) throw new ContentTranslator.Failure("response");
                    state.Ig = ig.Groups[1].Value; state.Key = data[0].ToString(); state.Token = (string)data[1];
                    state.Expires = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 180;
                }
            }
            using (var request = new HttpRequestMessage(HttpMethod.Post, "https://www.bing.com/ttranslatev3?isVertical=1&IG=" + Uri.EscapeDataString(state.Ig) + "&IID=translator.5028.1"))
            {
                request.Headers.Referrer = new Uri("https://www.bing.com/translator");
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                    ["fromLang"] = options.SourceLanguage == "auto" ? "auto-detect" : options.SourceLanguage == "zh" ? "zh-Hans" : options.SourceLanguage,
                    ["text"] = text, ["to"] = target == "zh-CN" ? "zh-Hans" : target,
                    ["key"] = state.Key, ["token"] = state.Token });
                try
                {
                    var data = JArray.Parse(await TranslationProviders.Send(client, request, options, token, diagnostic).ConfigureAwait(false));
                    var result = (string)data[0]?["translations"]?[0]?["text"];
                    if (string.IsNullOrWhiteSpace(result) || result.Length > 24000) throw new ContentTranslator.Failure("response");
                    return result;
                }
                catch { state.Expires = 0; throw; }
            }
        }
    }
}
