using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ComfortUI;
using Newtonsoft.Json;
class LocalModelTests031
{
 static int passed;
 sealed class Handler:HttpMessageHandler { internal int Calls;internal Func<CancellationToken,Task<HttpResponseMessage>> Send;protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){Calls++;return Send(token);} }
 static void Check(bool condition,string name){if(!condition)throw new Exception(name);passed++;Console.WriteLine("PASS "+name);}
 static async Task Run(string[] args)
 {
  var directory=Path.GetFullPath(args.Length>1?args[1]:"artifacts/tests/local-model-missing-031");
  Check(LocalModelService.SafeEntry(directory,"runtime/llama-server.exe").StartsWith(directory+Path.DirectorySeparatorChar),"Runtime entries remain inside the dedicated local model folder");
  foreach(var entry in new[]{"../outside.dll","/absolute.dll","C:/outside.dll","runtime/../../escape.dll"}){var failed=false;try{LocalModelService.SafeEntry(directory,entry);}catch(InvalidDataException){failed=true;}Check(failed,"Archive rejects escaping entry: "+entry);}
  Check(!TranslationProviders.ValidProvider("libre")&&TranslationProviders.ValidProvider("local"),"LibreTranslate is replaced by the local model service");
  Directory.CreateDirectory(directory);var probe=Path.Combine(directory,"download-probe-"+Guid.NewGuid().ToString("N"));
  using(var local=new LocalModelService(directory,null))
  {
   var fetch=typeof(LocalModelService).GetMethod("Fetch",BindingFlags.Instance|BindingFlags.NonPublic);
   var handler=new Handler{Send=t=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("corrupted")})};
   using(var client=new HttpClient(handler)){
    try{await (Task)fetch.Invoke(local,new object[]{client,"https://example.invalid/fixed",probe,new string('0',64),"localModelDownload",100L,CancellationToken.None});throw new Exception("Bad digest accepted");}catch(ContentTranslator.Failure e){Check(e.Code=="localIntegrity"&&!File.Exists(probe)&&Directory.GetFiles(directory,Path.GetFileName(probe)+"*.part").Length==0,"A corrupted download is rejected and its partial file is removed");}
   }
   handler=new Handler{Send=async t=>{await Task.Delay(30000,t);return new HttpResponseMessage(HttpStatusCode.OK);}};
   using(var client=new HttpClient(handler))using(var cancellation=new CancellationTokenSource(30)){
    try{await (Task)fetch.Invoke(local,new object[]{client,"https://example.invalid/fixed",probe,new string('0',64),"localModelDownload",100L,cancellation.Token});throw new Exception("Cancelled download completed");}catch(OperationCanceledException){Check(!File.Exists(probe)&&Directory.GetFiles(directory,Path.GetFileName(probe)+"*.part").Length==0,"Cancelling preparation interrupts network work and leaves no runnable or partial file");}
   }
   File.WriteAllText(probe,"verified cache");var hash=LocalModelService.Hash(probe);handler=new Handler{Send=t=>throw new Exception("Verified cache accessed the network")};
   try{using(var client=new HttpClient(handler))await (Task)fetch.Invoke(local,new object[]{client,"https://example.invalid/fixed",probe,hash,"localModelDownload",100L,CancellationToken.None});Check(handler.Calls==0&&LocalModelService.Hash(probe)==hash,"A valid pinned cache is reused without downloading or modifying it");}finally{File.Delete(probe);}
  }
  var live=args.Length>0&&args[0]=="--live";
  if(args.Length>0&&args[0]=="--bing"){
   using(var translator=new ContentTranslator()){
    translator.Diagnostic=value=>Console.WriteLine(value);
    var options=new ContentTranslator.Options{Provider="microsoft",ProxyMode=args.Length>2?"manual":"system",ProxyAddress=args.Length>2?args[2]:"",BypassCache=true};
    foreach(var language in new[]{"zh","en","ja","ko","es"}){var output=await translator.Translate(new[]{"Hello, welcome home."},language,CancellationToken.None,options);Check(!string.IsNullOrWhiteSpace(output[0]),"Real free Bing translation returns text for "+language);}
   }return;
  }
  if(args.Length>0&&args[0]=="--hold"){
   using(var local=new LocalModelService(directory,null))using(var connection=await local.Connect("cpu",CancellationToken.None)){Console.WriteLine("READY "+connection.Endpoint);await Task.Delay(120000);}return;
  }
  if(!live){using(var local=new LocalModelService(directory,null)){try{await local.Connect("cpu",CancellationToken.None);throw new Exception("Missing files started a process");}catch(ContentTranslator.Failure e){Check(e.Code=="localMissing","Missing model fails before launching a server or downloading files");}}return;}
  var statuses=new List<string>();int lastProgress=-1;
  using(var local=new LocalModelService(directory,(code,percent)=>{lock(statuses)statuses.Add(code);if(percent!=lastProgress||percent<0){Console.WriteLine(code+" "+percent);lastProgress=percent;}}))
  {
   var options=new ContentTranslator.Options{Provider="local",ProxyMode=args.Length>2?"manual":"system",ProxyAddress=args.Length>2?args[2]:"",LocalAcceleration="auto"};
   await local.Prepare(options,CancellationToken.None);
   Check(LocalModelService.Hash(Path.Combine(directory,"model.gguf"))==LocalModelService.ModelHash,"Downloaded model matches the pinned official model SHA256");
   foreach(var mode in new[]{"cpu","auto"})
   {
    options.LocalAcceleration=mode;options.LocalConnect=t=>local.Connect(mode,t);options.BypassCache=true;
    using(var translator=new ContentTranslator())
    {
     translator.Diagnostic=value=>{};
     var measurements=new List<object>();
     foreach(var language in new[]{"zh","en","ja","ko","es"}){
      var watch=Stopwatch.StartNew();var input=language=="en"?"Hola, bienvenido a casa.":"Hello, welcome home.";
      var output=await translator.Translate(new[]{input},language,CancellationToken.None,options);
      Check(!string.IsNullOrWhiteSpace(output[0])&&!output[0].Contains("<think>"),mode+" real local translation returns bounded text for "+language);
      measurements.Add(new{language,elapsedMs=watch.ElapsedMilliseconds,input,output=output[0]});
     }
     File.WriteAllText(Path.Combine(directory,"synthetic-live-"+mode+".json"),JsonConvert.SerializeObject(measurements,Formatting.Indented));
    }
   }
   Check(statuses.Contains("localRunningCpu"),"CPU inference actually started successfully");Check(statuses.Contains("localRunningGpu")||statuses.Contains("localCpuFallback"),"GPU preference either starts Vulkan or falls back explicitly");
  }
  Console.WriteLine("Local service disposed; owned server stopped.");
 }
 static int Main(string[] args){try{Run(args).GetAwaiter().GetResult();Console.WriteLine("TOTAL "+passed+" local model checks");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
