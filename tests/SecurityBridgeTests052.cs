using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ComfortUI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class SecurityBridgeTests052
{
    private static int passed;
    private static void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; }
    private static void Invalid(Action action, string name) { try { action(); } catch (FormatException) { passed++; return; } throw new Exception(name); }
    static int Main(string[] args)
    {
        try
        {
            // Deliberately hostile process-wide defaults must not affect this bridge.
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, StringEscapeHandling = StringEscapeHandling.Default };
            var texts = new List<string> { "Hello / 你好 / 안녕하세요", "single ' double \" backtick ` slash \\", "[]{}():;,/* text */ // text", "<b>plain</b> & <img src=\"about:blank\">", "</script><script>window.__comfortAuditMarker=1;</script>", "\");window.__comfortAuditMarker=1;//", "');window.__comfortAuditMarker=1;//", "${window.__comfortAuditMarker=1}", "a\u2028b\u2029c\u0085d", "a\r\nb\nc\rd\tend", "&lt;script&gt; &#39; &#x5c; \\u0022", "繁體 Français Русский Português 日本語 한국어 😀", "{\"__proto__\":{\"polluted\":true}}" };
            for (int i=0;i<32;i++) texts.Add("before"+(char)i+"after");
            const string alphabet = "ab012\"'`\\/<>;:=(){}[]$&*+-_ \t\r\n\u2028\u2029";
            var random = new Random(52021);
            for (int i=0;i<256;i++) { var b=new StringBuilder(); for(int n=0,length=16+random.Next(300);n<length;n++) b.Append(alphabet[random.Next(alphabet.Length)]); texts.Add(b.ToString()); }
            var fixtures = new JArray();
            foreach (var text in texts)
            {
                var json=UiDataJson.Translation("cu-123456-1","en",new[]{text,text});
                var value=JObject.Parse(json);
                Check((string)value["texts"][0]==text && (string)value["texts"][1]==text && (string)value["language"]=="en" && value.Count==3,"Exact two-field round trip");
                Check(!json.Contains("<") && !json.Contains(">") && !json.Contains("&") && !json.Contains("'") && !json.Contains("\u2028") && !json.Contains("\u2029") && !json.Contains("$type"),"Explicit HTML and separator escaping");
                fixtures.Add(new JObject { ["text"]=text,["json"]=json,["singleJson"]=UiDataJson.Translation("cu-123456-1","en",new[]{text}) });
            }
            Check(!UiDataJson.RequestId("cu-1-1\n") && !UiDataJson.RequestId(null) && !UiDataJson.RequestId(new string('1',65)) && UiDataJson.RequestId("cu-1-1"),"Strict bounded identity");
            Invalid(()=>UiDataJson.Translation("cu-1-1\n","en",new[]{"x"}),"Invalid identity accepted");
            Invalid(()=>UiDataJson.Translation("cu-1-1","unknown",new[]{"x"}),"Unknown target accepted");
            foreach(var input in new string[][] {null,new string[0],new[]{"a","b","c"},new string[]{null},new[]{new string('x',48001)}})
            {
                var error=JObject.Parse(UiDataJson.Translation("cu-1-1","en",input));
                Check((string)error["error"]=="response" && error["texts"]==null,"Invalid translation output fails closed");
            }
            Check((string)JObject.Parse(UiDataJson.Translation("cu-1-1","en",null,"<script>"))["error"]=="network","Unrecognized errors become a fixed code");
            Check((string)JObject.Parse(UiDataJson.Translation("cu-1-1","en",new[]{new string('x',48000)}))["texts"][0]==new string('x',48000),"Maximum translated field is retained");
            var settings=UiDataJson.FromObject(new {name="<test>",values=new[]{1,2}});
            Check(settings["$type"]==null && settings["values"] is JArray && UiDataJson.Serialize(settings).Contains("\\u003c"),"Global serializer defaults cannot add type metadata or disable HTML escaping");
            JsonConvert.DefaultSettings=null;
            if(args.Length>0) File.WriteAllText(args[0],new JObject { ["version"]="0.5.2",["assembly"]=typeof(JsonConvert).Assembly.GetName().FullName,["productVersion"]=System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(JsonConvert).Assembly.Location).ProductVersion,["checks"]=passed,["fixtures"]=fixtures }.ToString(Formatting.None),new UTF8Encoding(false));
            Console.WriteLine("TOTAL "+passed+" security bridge checks; "+texts.Count+" text fixtures; no network or game session");
            return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { JsonConvert.DefaultSettings=null; }
    }
}
