using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ComfortUI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class TranslationTests029
{
    sealed class Handler : HttpMessageHandler
    {
        internal int Calls;
        internal Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> Send;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) { Calls++; return Send(request,token); }
    }
    static int passed;
    static readonly List<string> results=new List<string>();
    static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Pass(string text){passed++;results.Add(text);Console.WriteLine("PASS "+text);}
    static HttpResponseMessage Response(string text){return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(new JObject{["responseStatus"]=200,["quotaFinished"]=false,["responseData"]=new JObject{["translatedText"]=text}}.ToString())};}
    static async Task Failure(Func<Task> operation,string expected){try{await operation();}catch(ContentTranslator.Failure e){Assert(e.Code==expected,e.Code);return;}throw new Exception("Expected "+expected);}
    static async Task Run()
    {
        var requests = new List<string>();
        var multi = new Handler { Send=(request,token)=>{
            var url=request.RequestUri.ToString(); requests.Add(url);
            return Task.FromResult(url.Contains("translate.google.com") ? new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("[[[\"Hola\",\"Hello\"],[\" mundo\",\" world\"]],null,\"en\"]")} : Response("test"));
        }};
        using(var service=new ContentTranslator(multi)){
            foreach(var code in new[]{"zh","en","ja","ko","es"}){
                var output=await service.Translate(new[]{"Hello world"},code,CancellationToken.None,new ContentTranslator.Options{Provider="google",ProxyMode="manual",ProxyAddress="http://127.0.0.1:7890"});
                Assert(output[0]=="Hola mundo","Google segments");
            }
            Assert(requests.Count==5&&requests.Any(x=>x.Contains("tl=ja"))&&requests.Any(x=>x.Contains("tl=ko"))&&requests.Any(x=>x.Contains("tl=es"))&&requests.Any(x=>x.Contains("tl=zh-CN")),"target mapping");
            await service.Translate(new[]{"Hello world"},"ja",CancellationToken.None,new ContentTranslator.Options{Provider="google"});Assert(requests.Count==5,"Google cache");
            await service.Translate(new[]{"Hello world"},"ja",CancellationToken.None);Assert(requests.Count==6,"provider cache collision");
            service.ClearCache();await service.Translate(new[]{"Hello world"},"ja",CancellationToken.None);Assert(requests.Count==7,"clear cache");
            await service.Translate(new[]{"Hello world"},"ja",CancellationToken.None,new ContentTranslator.Options{BypassCache=true});Assert(requests.Count==8,"Connection test must bypass an existing translation cache");
            await Failure(()=>service.Translate(new[]{"test"},"zh",CancellationToken.None,new ContentTranslator.Options{ProxyMode="manual",ProxyAddress="file:///C:/x"}),"proxy");
            Assert(!ContentTranslator.ValidProxy("http://user:pass@localhost:7890")&&!ContentTranslator.ValidProxy("http://localhost:7890/path")&&ContentTranslator.ValidProxy("http://127.0.0.1:7890"),"proxy validation");
        }
        Pass("Google segments, five targets, provider/language cache separation, cache clearing and proxy validation pass without network");
        var input=string.Concat(Enumerable.Repeat("Hello 😀 你好🌷。\n",160));
        var chunks=ContentTranslator.Split(input);
        Assert(string.Concat(chunks)==input,"split changed text");
        Assert(chunks.All(x=>Encoding.UTF8.GetByteCount(x)<=450),"byte cap");
        Assert(chunks.All(x=>!char.IsHighSurrogate(x[x.Length-1])&&!char.IsLowSurrogate(x[0])),"broken surrogate");
        Pass("UTF-8 chunks respect the provider limit and preserve Unicode, emoji, spacing and newlines");
        var handler=new Handler();handler.Send=(r,t)=>{Assert(r.RequestUri.Scheme=="https"&&r.RequestUri.Host=="api.mymemory.translated.net"&&r.Method==HttpMethod.Get,"network destination");Assert(!r.Headers.Contains("Authorization"),"unexpected credentials");return Task.FromResult(Response("Translated &amp; safe"));};
        using(var service=new ContentTranslator(handler))
        {
            var output=await service.Translate(new[]{"  Hello\n"},"zh",CancellationToken.None);
            Assert(output[0]=="  Translated & safe\n","HTML decode/spacing");
            Pass("Translation uses only the fixed retrieval endpoint and returns plain text with boundary whitespace preserved");
            await service.Translate(new[]{"  Hello\n"},"zh",CancellationToken.None);Assert(handler.Calls==1,"cache miss");
            await service.Translate(new[]{"  Hello\n"},"en",CancellationToken.None);Assert(handler.Calls==2,"language cache collision");
            Pass("Session cache avoids repeat requests and separates Chinese and English targets");
            var before=handler.Calls;await service.Translate(new[]{" \n\t"},"zh",CancellationToken.None);Assert(handler.Calls==before,"whitespace caused request");
            Pass("Whitespace-only fields never trigger a network request");
            await Failure(()=>service.Translate(new[]{"x"},"invalid",CancellationToken.None),"invalid");
            await Failure(()=>service.Translate(new[]{"x","y","z"},"en",CancellationToken.None),"invalid");
            await Failure(()=>service.Translate(new[]{new string('x',6001)},"en",CancellationToken.None),"length");Assert(handler.Calls==before,"invalid input went online");
            Pass("Unsupported target, excess fields and overlong text are rejected before network access");
        }
        handler=new Handler{Send=(r,t)=>Task.FromResult(new HttpResponseMessage((HttpStatusCode)429))};
        using(var service=new ContentTranslator(handler))await Failure(()=>service.Translate(new[]{"test"},"zh",CancellationToken.None),"quota");
        Pass("HTTP rate limiting produces a quota error without replacing content");
        handler=new Handler{Send=(r,t)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"responseStatus\":429,\"quotaFinished\":true}")})};
        using(var service=new ContentTranslator(handler))await Failure(()=>service.Translate(new[]{"test"},"zh",CancellationToken.None),"quota");
        Pass("Provider JSON quota responses also produce a quota error");
        handler=new Handler{Send=(r,t)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway))};
        using(var service=new ContentTranslator(handler))
        {
            await Failure(()=>service.Translate(new[]{"test"},"zh",CancellationToken.None),"network");
            handler.Send=(r,t)=>Task.FromResult(Response("恢复"));
            Assert((await service.Translate(new[]{"test"},"zh",CancellationToken.None))[0]=="恢复","retry");Assert(handler.Calls==2,"failure cached");
        }
        Pass("Network failures are not cached and a subsequent retry can succeed");
        handler=new Handler{Send=async(r,t)=>{await Task.Delay(30000,t);return Response("late");}};
        using(var service=new ContentTranslator(handler))using(var cts=new CancellationTokenSource())
        {
            var first=service.Translate(new[]{"first"},"zh",cts.Token);var second=service.Translate(new[]{"second"},"zh",cts.Token);
            Assert(!first.IsCompleted&&!second.IsCompleted,"blocked caller");await Failure(()=>service.Translate(new[]{"third"},"zh",CancellationToken.None),"busy");cts.Cancel();
            foreach(var task in new[]{first,second})try{await task;}catch(OperationCanceledException){}
            handler.Send=(r,t)=>Task.FromResult(Response("fresh"));Assert((await service.Translate(new[]{"fresh"},"zh",CancellationToken.None))[0]=="fresh","semaphore not released");
        }
        Pass("Requests are asynchronous, bounded to two, cancellable, and release capacity after cancellation");
    }
    static int Main(){try{Run().GetAwaiter().GetResult();Console.WriteLine("TOTAL "+passed+" passed");System.IO.File.WriteAllText("artifacts/tests/translation-report.json",JsonConvert.SerializeObject(new{version="0.2.9",passed=results,live="Mock HTTP only; no game or personal content"},Formatting.Indented));return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
