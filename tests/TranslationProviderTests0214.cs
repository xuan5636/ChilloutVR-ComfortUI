using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ComfortUI;
using Newtonsoft.Json.Linq;

partial class TranslationTests029
{
    static HttpResponseMessage JsonResponse(string value) { return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(value) }; }
    static async Task ProviderChecks()
    {
        var hosts = new List<string>();
        var h = new Handler { Send = (r,t) => { hosts.Add(r.RequestUri.Host); return Task.FromResult(hosts.Count == 1 ? new HttpResponseMessage((HttpStatusCode)429) : JsonResponse("[[[\"你好\",\"Hello\"]]]")); } };
        using (var s = new ContentTranslator(h)) {
            Assert((await s.Translate(new[]{"Hello"},"zh",CancellationToken.None,new ContentTranslator.Options{Provider="google"}))[0]=="你好", "Google fallback");
            Assert(hosts.SequenceEqual(new[]{"translate.googleapis.com","translate.google.com"}), "Unexpected fallback host");
        }
        Pass("Google tries a primary and one same-service fallback without sending content to a different provider");
        h = new Handler { Send = (r,t) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)) };
        using (var s = new ContentTranslator(h)) {
            var o = new ContentTranslator.Options{Provider="google"};
            await Failure(()=>s.Translate(new[]{"Hello"},"zh",CancellationToken.None,o),"quota");
            Assert(h.Calls==2,"Unbounded Google retries");
            s.ClearCache();o.BypassCache=true;
            await Failure(()=>s.Translate(new[]{"Different"},"en",CancellationToken.None,o),"quota");
            Assert(h.Calls==2,"Quota cooldown bypassed");
        }
        Pass("Quota responses start a 60-second route-specific cooldown; clearing cache or testing cannot hammer the service");
        h = new Handler { Send = async (r,t) => {
            Assert(r.Method==HttpMethod.Post && r.RequestUri.Host=="api.cognitive.microsofttranslator.com","Microsoft endpoint");
            Assert(r.RequestUri.Query.Contains("to=zh-Hans") && r.RequestUri.Query.Contains("from=en"),"Microsoft language mapping");
            Assert(r.Headers.GetValues("Ocp-Apim-Subscription-Key").Single()=="test-ms-key" && r.Headers.GetValues("Ocp-Apim-Subscription-Region").Single()=="eastasia","Microsoft headers");
            Assert((string)JArray.Parse(await r.Content.ReadAsStringAsync())[0]["Text"]=="Hello","Microsoft JSON body");
            return JsonResponse("[{\"translations\":[{\"text\":\"你好\",\"to\":\"zh-Hans\"}]}]");
        }};
        using(var s=new ContentTranslator(h)) Assert((await s.Translate(new[]{"Hello"},"zh",CancellationToken.None,new ContentTranslator.Options{Provider="microsoft",MicrosoftKey="test-ms-key",MicrosoftRegion="eastasia",SourceLanguage="en"}))[0]=="你好","Microsoft result");
        Pass("Microsoft uses the official HTTPS JSON API with subscription/region headers and separate source/target languages");
        string longText = new string('a',1800)+"\n你好😀";
        h = new Handler { Send = async(r,t)=>{
            Assert(r.RequestUri.ToString()=="https://example.invalid/v1/chat/completions" && r.Method==HttpMethod.Post,"AI endpoint");
            Assert(r.Headers.Authorization.Scheme=="Bearer"&&r.Headers.Authorization.Parameter=="test-ai-key","AI authorization");
            var body=JObject.Parse(await r.Content.ReadAsStringAsync());
            Assert((string)body["model"]=="my-model" && (bool)body["stream"]==false,"AI options");
            Assert((string)body["messages"][1]["content"]==longText && ((string)body["messages"][0]["content"]).Contains("Japanese"),"AI message boundaries");
            Assert(body["tools"]==null && body["temperature"]==null,"Unexpected AI features");
            return JsonResponse("{\"choices\":[{\"message\":{\"content\":\"こんにちは\"},\"finish_reason\":\"stop\"}]}");
        }};
        using(var s=new ContentTranslator(h)) Assert((await s.Translate(new[]{longText},"ja",CancellationToken.None,new ContentTranslator.Options{Provider="ai",AiEndpoint="https://example.invalid/v1",AiModel="my-model",AiKey="test-ai-key"}))[0]=="こんにちは"&&h.Calls==1,"Long AI request split");
        Pass("AI preserves long input in one translation-only Chat Completions request with explicit model and authorization");
        h = new Handler { Send = async(r,t)=>{
            Assert(r.RequestUri.ToString()=="http://127.0.0.1:5000/translate","Libre endpoint duplicated");
            var body=JObject.Parse(await r.Content.ReadAsStringAsync());
            Assert((string)body["source"]=="auto"&&(string)body["target"]=="ko"&&(string)body["format"]=="text"&&(string)body["api_key"]=="test-libre-key","Libre body");
            return JsonResponse("{\"translatedText\":\"안녕\"}");
        }};
        using(var s=new ContentTranslator(h)) Assert((await s.Translate(new[]{"Hello"},"ko",CancellationToken.None,new ContentTranslator.Options{Provider="libre",LibreEndpoint="http://127.0.0.1:5000/translate",LibreKey="test-libre-key"}))[0]=="안녕","Libre result");
        Pass("LibreTranslate supports a local endpoint, automatic source detection and an optional key without duplicating /translate");
        h = new Handler{Send=(r,t)=>Task.FromResult(JsonResponse("{}"))};
        using(var s=new ContentTranslator(h)) {
            await Failure(()=>s.Translate(new[]{"Hello"},"zh",CancellationToken.None,new ContentTranslator.Options{Provider="ai"}),"serviceConfig");
            await Failure(()=>s.Translate(new[]{"Hello"},"zh",CancellationToken.None,new ContentTranslator.Options{Provider="ai",AiModel="test"}),"missingKey");
            await Failure(()=>s.Translate(new[]{"Hello"},"zh",CancellationToken.None,new ContentTranslator.Options{Provider="microsoft"}),"missingKey");
            await Failure(()=>s.Translate(new[]{"Hello"},"zh",CancellationToken.None,new ContentTranslator.Options{Provider="libre",LibreEndpoint="http://example.invalid"}),"serviceConfig");
            Assert(h.Calls==0,"Invalid configuration accessed network");
        }
        Assert(!TranslationProviders.ValidEndpoint("https://user:key@example.invalid") && !TranslationProviders.ValidEndpoint("https://example.invalid/?key=x") && !TranslationProviders.ValidValue("key\nInjected",1024),"Credential validation");
        Pass("Missing keys/models, unsafe remote HTTP and credential-bearing URLs are rejected before network access");
        foreach(var error in new[]{Tuple.Create(401,"auth"),Tuple.Create(403,"auth"),Tuple.Create(407,"proxy"),Tuple.Create(404,"serviceConfig"),Tuple.Create(302,"redirect")}) {
            h=new Handler{Send=(r,t)=>Task.FromResult(new HttpResponseMessage((HttpStatusCode)error.Item1))};
            using(var s=new ContentTranslator(h)) await Failure(()=>s.Translate(new[]{"Hello"},"zh",CancellationToken.None,new ContentTranslator.Options{Provider="microsoft",MicrosoftKey="test"}),error.Item2);
        }
        Pass("Authentication, proxy authentication, bad endpoint and redirects report distinct actionable failure codes");
        h=new Handler{Send=(r,t)=>Task.FromResult(JsonResponse("{\"choices\":[{\"message\":{\"content\":\"partial\"},\"finish_reason\":\"length\"}]}"))};
        using(var s=new ContentTranslator(h)) await Failure(()=>s.Translate(new[]{"Hello"},"zh",CancellationToken.None,new ContentTranslator.Options{Provider="ai",AiEndpoint="http://localhost:8000/v1",AiModel="test"}),"response");
        h=new Handler{Send=(r,t)=>Task.FromResult(JsonResponse("[]"))};
        using(var s=new ContentTranslator(h)) await Failure(()=>s.Translate(new[]{"Hello"},"zh",CancellationToken.None,new ContentTranslator.Options{Provider="microsoft",MicrosoftKey="test"}),"response");
        Pass("Truncated AI output and malformed successful responses fail safely instead of replacing text with partial data");
        h=new Handler{Send=(r,t)=>Task.FromResult(JsonResponse("{\"choices\":[{\"message\":{\"content\":\"ok\"}}]}"))};
        using(var s=new ContentTranslator(h)) {
            var o=new ContentTranslator.Options{Provider="ai",AiEndpoint="http://localhost:8000/v1",AiModel="one"};
            await s.Translate(new[]{"Hello"},"zh",CancellationToken.None,o);
            await s.Translate(new[]{"Hello"},"zh",CancellationToken.None,o);Assert(h.Calls==1,"Cache missed");
            o.AiModel="two";await s.Translate(new[]{"Hello"},"zh",CancellationToken.None,o);
            o.AiEndpoint="http://localhost:8001/v1";await s.Translate(new[]{"Hello"},"zh",CancellationToken.None,o);
            o.AiKey="new-key";await s.Translate(new[]{"Hello"},"zh",CancellationToken.None,o);
            o.SourceLanguage="en";await s.Translate(new[]{"Hello"},"zh",CancellationToken.None,o);
            Assert(h.Calls==5,"Different service configurations shared cache");
        }
        Pass("Translation cache separates source languages, models, endpoints and key revisions while identical requests are reused");
        var ready=new TaskCompletionSource<HttpResponseMessage>();
        h=new Handler{Send=(r,t)=>ready.Task};
        using(var s=new ContentTranslator(h)) {
            var a=s.Translate(new[]{"Hello"},"zh",CancellationToken.None);
            var b=s.Translate(new[]{"Hello"},"zh",CancellationToken.None);
            Assert(h.Calls==1 && !a.IsCompleted && !b.IsCompleted,"Duplicate entered network");
            ready.SetResult(Response("你好"));await Task.WhenAll(a,b);Assert(h.Calls==1,"Duplicate not coalesced");
        }
        Pass("Concurrent identical translations coalesce behind the shared network gate without blocking the calling thread");
        var created=0;
        using(var s=new ContentTranslator(clientFactory:o=>{created++;return new HttpClient(new Handler{Send=(r,t)=>Task.FromResult(Response("ok"))});})) {
            var o=new ContentTranslator.Options{ProxyMode="manual",ProxyAddress="http://127.0.0.1:7000"};
            for(int n=0;n<3;n++)await s.Translate(new[]{"message "+n},"zh",CancellationToken.None,o);
            Assert(created==1,"Client created per message");
            for(int n=1;n<=4;n++){o.ProxyAddress="http://127.0.0.1:"+(7000+n);await s.Translate(new[]{"new "+n},"zh",CancellationToken.None,o);}
            o.ProxyAddress="http://127.0.0.1:7000";await s.Translate(new[]{"back"},"zh",CancellationToken.None,o);
            Assert(created==6,"Idle transport pool is not bounded to four");
        }
        Pass("Network connections are reused across messages and idle proxy transports are evicted from a bounded pool");
        h=new Handler{Send=(r,t)=>Task.FromResult(Response("ok"))};
        using(var s=new ContentTranslator(h,180)) {
            await s.Translate(new[]{"one"},"zh",CancellationToken.None);
            var watch=Stopwatch.StartNew();await s.Translate(new[]{"two"},"zh",CancellationToken.None);
            Assert(watch.ElapsedMilliseconds>=130,"Public calls were not paced");
            using(var cts=new CancellationTokenSource(20)) {try{await s.Translate(new[]{"three"},"zh",cts.Token);throw new Exception("Pacing ignored cancellation");}catch(OperationCanceledException){}}
            Assert(h.Calls==2,"Cancelled wait entered network");
        }
        Pass("Public-service pacing is asynchronous and cancellation stops requests before they enter the network");
        var diagnostics=new List<string>();
        h=new Handler{Send=(r,t)=>Task.FromResult(JsonResponse("{\"choices\":[{\"message\":{\"content\":\"ok\"}}]}"))};
        using(var s=new ContentTranslator(h)) {s.Diagnostic=diagnostics.Add;await s.Translate(new[]{"private test text"},"zh",CancellationToken.None,new ContentTranslator.Options{Provider="ai",AiEndpoint="https://example.invalid/v1",AiKey="private-test-key",AiModel="test"});}
        Assert(diagnostics.Count==1 && diagnostics[0].Contains("example.invalid HTTP 200") && !diagnostics[0].Contains("private")&&!diagnostics[0].Contains("/v1"),"Sensitive diagnostics");
        Pass("Connection diagnostics include only service, route, host and HTTP status; no chat, path or credentials");
        var file=Path.GetFullPath("artifacts/tests/credential-"+Guid.NewGuid().ToString("N")+".dat");
        try {
            var store=new TranslationSecrets(file);store.Save("ai","https://example.invalid/v1","test-local-secret");
            Assert(!Encoding.UTF8.GetString(File.ReadAllBytes(file)).Contains("test-local-secret"),"Key stored in plaintext");
            var loaded=new TranslationSecrets(file);
            Assert(loaded.Get("ai","https://example.invalid/v1")=="test-local-secret"&&loaded.Get("ai","https://other.invalid/v1")=="","Endpoint binding");
            loaded.Save("microsoft","","test-ms-secret");loaded.Save("ai","https://example.invalid/v1","");
            loaded=new TranslationSecrets(file);Assert(loaded.Get("ai","https://example.invalid/v1")==""&&loaded.Get("microsoft","")=="test-ms-secret","Independent clear");
        } finally {if(File.Exists(file))File.Delete(file);}
        Pass("Real Windows encryption round-trips keys, binds them to the configured endpoint and deletes one key without affecting others");
    }
}
