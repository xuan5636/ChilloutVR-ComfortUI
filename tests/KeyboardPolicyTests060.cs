using System;
using System.Linq;
using ComfortUI;
class KeyboardPolicyTests060
{
 static int count;
 static void Check(bool value,string label){if(!value)throw new Exception(label);count++;Console.WriteLine("PASS "+label);}
 static void Main(){
  foreach(var language in LanguageSupport.Codes){var defaults=KeyboardPolicy.Languages(null,language);Check(defaults[0]==language&&defaults.Contains("en"),language+" keyboard defaults follow UI and retain English");}
  Check(KeyboardPolicy.Languages(new[]{"ko","xx","ko","ja"},"zh").SequenceEqual(new[]{"ko","ja"}),"Unsupported and duplicate keyboards removed");
  Check(KeyboardPolicy.Languages(new[]{"de"},"unknown").SequenceEqual(new[]{"en"}),"Invalid empty lists use safe English default");
  Check(KeyboardPolicy.Initial("ja",new[]{"zh","en"})=="zh","Removed default falls back to first enabled language");
  Check(KeyboardPolicy.Initial("en",new[]{"zh","en"})=="en","Enabled default retained");
  Check(KeyboardPolicy.Clean("a\r\nb\rc",50,true)=="a\nb\nc","Native CRLF normalization");
  Check(KeyboardPolicy.Clean("a\r\nb\tc",50,false)=="a b c","Single line normalizes line breaks and tabs");
  Check(KeyboardPolicy.Clean("\ud800a\udc00\0b\x1b",50,true)=="ab","Unpaired surrogates and controls removed");
  Check(KeyboardPolicy.Clean("a😀b",2,true)=="a"&&KeyboardPolicy.Clean("a😀b",3,true)=="a😀","UTF16 limit preserves complete emoji");
  Check(KeyboardPolicy.Clean(new string('a',17000),0,true).Length==16000,"Unlimited native inputs have a bounded maximum");
  Check(KeyboardPolicy.Clean("<img onerror=x>\"</script>",100,true)=="<img onerror=x>\"</script>","Markup remains literal text for the typed event channel");
  Check(KeyboardPolicy.ValidSession(Guid.NewGuid().ToString("N")),"Fresh session accepted");
  Check(!KeyboardPolicy.ValidSession("stale")&&!KeyboardPolicy.ValidSession(null),"Malformed sessions rejected");
  Check(KeyboardPolicy.ValidRectangle(0,0,100,50),"Visible bounded field accepted");
  foreach(var rectangle in new[]{new[]{double.NaN,0,100,50},new[]{0,double.PositiveInfinity,100,50},new[]{-1d,0,100,50},new[]{0d,0,29,16},new[]{0d,0,100,15},new[]{16380d,0,100,50}})Check(!KeyboardPolicy.ValidRectangle(rectangle[0],rectangle[1],rectangle[2],rectangle[3]),"Invalid field coordinates rejected");
  Console.WriteLine("TOTAL "+count);
 }
}
