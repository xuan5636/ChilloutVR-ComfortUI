'use strict';
// Offline Node regression for the exact event receiver. No browser/network needed.
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),assert=require('node:assert/strict');
const repo=path.resolve(__dirname,'..'),source=fs.readFileSync(path.join(repo,'assets/native-bridge.js'),'utf8');
const sourceMod=fs.readFileSync(path.join(repo,'src/ComfortMod.cs'),'utf8');
assert(sourceMod.includes('current.Target.Translation(delivery.Json)'));
assert(!sourceMod.includes('ComfortContent.receive('));
assert(!/\beval\s*\(|\bFunction\s*\(|\.innerHTML\s*=/.test(source));
const tools=fs.readFileSync(path.join(repo,'assets/content-tools.js'),'utf8');
assert(!/\beval\s*\(|\bFunction\s*\(|\.innerHTML\s*=/.test(tools));
const received=[],handlers={},listeners={},calls=[];
const context=vm.createContext({window:null,engine:{on:(name,fn)=>{assert(!handlers[name],name);handlers[name]=fn;},call:(...args)=>calls.push(args)},Array,JSON,Object,console},{codeGeneration:{strings:false,wasm:false}});
context.window=context;context.addEventListener=(name,fn)=>listeners[name]=fn;
vm.runInContext(source,context);
let initialized=0,passed=0;
const check=(ok,name)=>{assert(ok,name);passed++;};
context.ComfortNativeBridge.install(()=>{initialized++;context.ComfortUI={updatePreferences:(...args)=>received.push(['preferences',...args]),chatState:v=>received.push(['chat',v]),translationNotice:(...v)=>received.push(['notice',...v]),localModelState:(...v)=>received.push(['local',...v])};context.ComfortContent={receive:json=>received.push(['translation',JSON.parse(json)])};});
check(calls.length===1 && calls[0][0]==='ComfortUI_BridgeReady' && calls[0][1]==='1','The ready handshake follows handler registration');
check(Object.keys(handlers).sort().join(',')===['ComfortUI_Bootstrap','ComfortUI_Preferences','ComfortUI_TranslationResult','ComfortUI_KeyboardState','ComfortUI_TranslationNotice','ComfortUI_LocalModelState'].sort().join(','),'Exactly six fixed event names, including keyboard data');
const payload={protocol:1,context:'main',preferences:{language:'en'},chatEnabled:false,localModelState:['localIdle',-1],microphone:null,existingTranslations:{},settingsTranslations:{},locales:{},styles:'body{}'};
for(const invalid of [null,[],true,42,'bad',{}, {...payload,protocol:2},{...payload,context:{toString:0}},{...payload,context:'main\n'},{...payload,preferences:null},{...payload,preferences:[]},{...payload,chatEnabled:1},{...payload,localModelState:['idle',NaN]},{...payload,localModelState:['idle',101]},{...payload,localModelState:['idle',1,'true']},{...payload,microphone:{toString:0}},{...payload,microphone:'muted\n'},{...payload,styles:[]},{...payload,styles:'x'.repeat(400001)}]){
 handlers.ComfortUI_Bootstrap(JSON.stringify(invalid));check(initialized===0,'Malformed bootstrap is ignored');
}
handlers.ComfortUI_Bootstrap('x'.repeat(8388609));check(initialized===0,'Oversized bootstrap is ignored');
handlers.ComfortUI_TranslationResult('{"id":"cu-1-1","language":"en","texts":["early"]}');check(received.length===0,'Early results wait for initialization');
handlers.ComfortUI_Bootstrap(JSON.stringify(payload));check(initialized===1 && context.__comfortContext==='main' && context.__comfortHost.native===true,'Typed bootstrap initializes the embedded bundle once');
handlers.ComfortUI_Bootstrap(JSON.stringify(payload));context.ComfortNativeBridge.install(()=>initialized++);check(initialized===1 && calls.length===2,'Duplicate installation and bootstrap do not multiply handlers');
let fixtures=[];
if(process.argv[2])fixtures=JSON.parse(fs.readFileSync(process.argv[2],'utf8')).fixtures;
else fixtures=[{text:'</script><b>ordinary text</b> " \\ \u2028',json:JSON.stringify({id:'cu-1-1',language:'en',texts:['</script><b>ordinary text</b> " \\ \u2028']})}];
context.__comfortAuditMarker=0;
for(const fixture of fixtures){
 for(const key of ['json','singleJson']){
  if(!fixture[key])continue;
  handlers.ComfortUI_TranslationResult(fixture[key]);const result=received.pop();
  check(result[0]==='translation' && result[1].texts[0]===fixture.text && context.__comfortAuditMarker===0,'Response remains typed data without compiling text');
 }
}
received.length=0;
handlers.ComfortUI_Preferences(JSON.stringify({preferences:{language:'ja',aiModel:'</script>'},chatEnabled:true}));
check(received.length===2 && received[0][1].aiModel==='</script>' && received[1][1]===true,'Preferences stay data and retain the chat mode');
for(const json of ['null','[]','{}','{"preferences":[],"chatEnabled":true}','{"preferences":{},"chatEnabled":"true"}','x'.repeat(16385)]){const n=received.length;handlers.ComfortUI_Preferences(json);check(received.length===n,'Malformed preference event ignored');}
handlers.ComfortUI_LocalModelState('["localReady",100,false]');check(received.pop()[1]==='localReady','Local model progress uses an event');
handlers.ComfortUI_TranslationNotice('{"code":"chatEnabled","chatEnabled":true}');check(received.pop()[1]==='chatEnabled','Notices use an event');
const n=received.length;listeners.beforeunload();
for(const [name,fn]of Object.entries(handlers))fn(name==='ComfortUI_Bootstrap'?JSON.stringify(payload):'{"code":"testing","chatEnabled":true,"preferences":{}}');
check(received.length===n && initialized===1,'All event handlers stop using a closed document');
console.log('TOTAL '+passed+' offline event bridge checks; no eval, network or actual Cohtml session');
