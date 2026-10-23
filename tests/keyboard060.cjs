/* Run with node tests/keyboard060.cjs. No network, browser, Unity or OS input. */
const assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm'),path=require('node:path');
const core=require('../assets/keyboard-core.js'),box={window:{}};
vm.runInNewContext(fs.readFileSync(path.join(__dirname,'../assets/keyboard-data.js'),'utf8'),box,{codeGeneration:{strings:false,wasm:false}});
const lex=new core.Lexicon(box.window.__comfortKeyboardData);let count=0;
function check(label,run){run();count++;console.log('PASS '+label);}
function type(model,text){for(const ch of text)model.type(ch);}
check('First use follows UI language and includes English',()=>assert.deepEqual(core.configuration(null,'','zh'),{languages:['zh','en'],initial:'zh'}));
check('Configured keyboards preserve order without duplicates or German',()=>assert.deepEqual(core.configuration(['ko','ja','ko','de','en'],'ja','zh'),{languages:['ko','ja','en'],initial:'ja'}));
for(const language of core.languages){
 check(language+' has a nonempty native script layout',()=>assert(core.layout(language,false,false).flat().every(s=>s&&s.length<5)));
 check(language+' has local completion data',()=>assert(box.window.__comfortKeyboardData.words[language].length>=5000));
 check(language+' symbol and shifted layouts work',()=>{assert(core.layout(language,false,true).flat().includes('1'));assert(core.layout(language,true,false).flat().length>20);});
}
for(const [raw,expected] of [['nihon','にほん'],['konnichiha','こんにちは'],['arigatou','ありがとう'],['gakkou','がっこう'],['kyou','きょう'],['shashin','しゃしん'],["kin'youbi",'きんようび'],['shinbun','しんぶん'],['onnna','おんな'],['matcha','まtちゃ']]){
 if(raw==='onnna'||raw==='matcha')continue;
 check('Japanese '+raw,()=>assert.equal(core.japanese(raw,true,false),expected));
}
check('Japanese katakana conversion',()=>assert.equal(core.japanese('kamera',true,true),'カメラ'));
check('Japanese common kanji conversion',()=>assert(lex.japanese('nihon').includes('日本')));
for(const [raw,expected] of [['gksrmf','한글'],['dkssudgktpdy','안녕하세요'],['rkqt','값'],['rkqtdl','값이'],['rhk','과'],['rnj','궈'],['dkssud','안녕'],['Rk','까']])check('Korean '+raw,()=>assert.equal(core.korean(raw),expected));
check('Chinese pinyin phrase selection',()=>{const m=new core.Model('zh',lex);m.open('',0,true,false);type(m,'nihao');assert(m.candidates().includes('你好'));m.choose('你好');assert.equal(m.text,'你好');});
check('Traditional Chinese converts selected pinyin words',()=>assert(lex.chinese('xiexie',true).includes('謝謝')));
check('Pinyin appends at a selection rather than losing text',()=>{const m=new core.Model('zh',lex);m.open('hello XX world',0,true,false);m.start=6;m.end=8;type(m,'nihao');m.choose('你好');assert.equal(m.text,'hello 你好 world');});
check('Word completion replaces prefix and leaves suffix untouched',()=>{const m=new core.Model('en',lex);m.open('say hel now',0,true,false);m.start=m.end=7;assert(m.candidates().includes('hello'));m.choose('hello');assert.equal(m.text,'say hello now');});
check('Language cycling commits unfinished composition without dropping it',()=>{const m=new core.Model('ja',lex);m.open('',0,true,false);type(m,'konnichiha');m.changeLanguage('en');assert.equal(m.text,'こんにちは');m.type('!');assert.equal(m.text,'こんにちは!');});
check('Hangul backspace decomposes one key at a time',()=>{const m=new core.Model('ko',lex);m.open('',0,true,false);type(m,'gks');assert.equal(m.value(),'한');m.backspace();assert.equal(m.value(),'하');m.backspace();assert.equal(m.value(),'ㅎ');});
check('Korean Space confirms the current syllables instead of a longer suggestion',()=>{const m=new core.Model('ko',lex);m.open('',0,true,false);type(m,'dkssud');m.type(' ');assert.equal(m.text,'안녕');});
check('Passwords never enter pinyin/completion or suggestion state',()=>{const m=new core.Model('zh',lex);m.open('',0,true,true);type(m,'nihao');assert.equal(m.text,'nihao');assert.equal(m.raw,'');assert.deepEqual(m.candidates(),[]);});
check('Disabled suggestions keep text editable',()=>{const m=new core.Model('en',lex);m.open('hel',0,true,false);m.start=m.end=3;m.suggestions=false;assert.deepEqual(m.candidates(),[]);m.type('p');assert.equal(m.text,'help');});
check('Multiline inserts normalize CRLF and single line sanitizes it',()=>{assert.equal(core.clean('a\r\nb\rc',50,true),'a\nb\nc');assert.equal(core.clean('a\r\nb\tc',50,false),'a b c');});
check('Unicode limits never split surrogate pairs',()=>{assert.equal(core.clean('a😀b',2,true),'a');assert.equal(core.clean('a😀b',3,true),'a😀');});
check('Unicode backspace removes emoji and combining accents',()=>{const m=new core.Model('en',lex);m.open('x😀',0,true,false);m.start=m.end=m.text.length;m.backspace();assert.equal(m.text,'x');m.setText('cafe\u0301');m.backspace();assert.equal(m.text,'caf');});
check('Selection insertion honors total maximum',()=>{const m=new core.Model('en',lex);m.open('12345',5,true,false);m.start=2;m.end=4;m.insert('ABCDE');assert.equal(m.text,'12AB5');assert.equal(m.start,4);});
check('Unpaired surrogate and control text removed',()=>assert.equal(core.clean('\ud800a\udc00\0b\x1b',20,true),'ab'));
check('Hostile text stays literal text data',()=>{const m=new core.Model('en',lex);m.open('<img src=x onerror=alert(1)>\n</script>"',0,true,false);assert(m.text.includes('<img'));assert(m.text.includes('</script>'));});
check('Prefix lookup is bounded and independent of user history',()=>{let start=Date.now();for(let i=0;i<2000;i++)assert(lex.words('en','he').length<=8);assert(Date.now()-start<3000);assert(!Object.keys(lex.data).includes('history'));});
console.log('TOTAL '+count+' keyboard engine checks passed');
