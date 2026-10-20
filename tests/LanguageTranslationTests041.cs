using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ComfortUI;
using Newtonsoft.Json.Linq;

partial class TranslationTests029
{
    static Dictionary<string,string> Parameters(string value)
    {
        return value.TrimStart('?').Split('&').Where(s=>s.Contains("=")).Select(s=>s.Split(new[]{'='},2)).ToDictionary(p=>Uri.UnescapeDataString(p[0]),p=>Uri.UnescapeDataString(p[1].Replace("+"," ")));
    }
    static async Task LanguageChecks()
    {
        var cases=new[]{
            new[]{"zh","zh-CN","zh-Hans","Simplified Chinese"},new[]{"en","en","en","English"},
            new[]{"ja","ja","ja","Japanese"},new[]{"ko","ko","ko","Korean"},new[]{"es","es","es","Spanish"},
            new[]{"zh-Hant","zh-TW","zh-Hant","Traditional Chinese (Taiwan)"},new[]{"fr","fr","fr","French"},
            new[]{"ru","ru","ru","Russian"},new[]{"pt-BR","pt","pt","Portuguese (Brazil)"},
            new[]{"it","it","it","Italian"},new[]{"id","id","id","Indonesian"},new[]{"pl","pl","pl","Polish"},new[]{"tr","tr","tr","Turkish"}
        };
        Assert(LanguageSupport.Codes.SequenceEqual(cases.Select(p=>p[0]))&&!LanguageSupport.Supported("de")&&!LanguageSupport.Supported("zh-TW"),"Language registry must contain exactly the 13 persisted codes, with no German or duplicate regional code");
        Pass("Thirteen distinct saved language codes are supported; German remains excluded");
        foreach(var item in cases)
        {
            var code=item[0];
            foreach(var provider in new[]{"google","mymemory","microsoft","ai","local"})
            {
                var h=new Handler{Send=async(r,t)=>{
                    if(provider=="google"){
                        var q=Parameters(r.RequestUri.Query);Assert(q["tl"]==item[1]&&q["sl"]==item[1]&&q["q"]=="Hello, Test Player!","Google language/text routing for "+code);
                        return JsonResponse("[[[\"translated\",\"Hello\"]]]");
                    }
                    if(provider=="mymemory"){
                        Assert(Parameters(r.RequestUri.Query)["langpair"]==item[1]+"|"+item[1],"MyMemory regional routing for "+code);return Response("translated");
                    }
                    if(provider=="microsoft"){
                        if(r.Method==HttpMethod.Get)return JsonResponse("IG:\"LANG041\";var params_AbusePreventionHelper=[12345,\"memory-only\",3600000];");
                        var p=Parameters(await r.Content.ReadAsStringAsync());Assert(p["fromLang"]==item[2]&&p["to"]==item[2],"Bing regional language routing for "+code);
                        return JsonResponse("[{\"translations\":[{\"text\":\"translated\"}]}]");
                    }
                    var body=JObject.Parse(await r.Content.ReadAsStringAsync());
                    Assert(((string)body["messages"][0]["content"]).Contains("into "+item[3]+"."),provider+" silently fell back to English for "+code);
                    Assert((string)body["messages"][1]["content"]=="Hello, Test Player!","Translation content must remain separate from its instructions");
                    if(provider=="local")Assert(r.RequestUri.IsLoopback,"Local translation left loopback");
                    return JsonResponse("{\"choices\":[{\"message\":{\"content\":\"translated\"},\"finish_reason\":\"stop\"}]}");
                }};
                using(var translator=new ContentTranslator(h))
                {
                    var o=new ContentTranslator.Options{Provider=provider,SourceLanguage=code,AiEndpoint="http://127.0.0.1:55123/v1",AiModel="test-model",LocalConnect=t=>Task.FromResult(new LocalModelService.Connection{Endpoint="http://127.0.0.1:55123",Key="ephemeral"})};
                    Assert((await translator.Translate(new[]{"Hello, Test Player!"},code,CancellationToken.None,o))[0]=="translated","Translated output for "+code);
                }
            }
            Pass(code+": Google, MyMemory, free Bing, AI and local requests use the intended target/source and preserve message boundaries");
        }
        var blocked=new Handler{Send=(r,t)=>Task.FromResult(Response("unexpected"))};
        using(var translator=new ContentTranslator(blocked))await Failure(()=>translator.Translate(new[]{"Hello"},"de",CancellationToken.None),"invalid");
        Assert(blocked.Calls==0,"Unsupported language entered network");
        Pass("An unsupported target is rejected before any provider request");
    }
}
