/* Comfort UI keyboard engine. Pure data/text operations; no DOM, network or code evaluation. */
(function(root,factory){var api=factory();if(typeof module==='object'&&module.exports)module.exports=api;else root.ComfortKeyboardCore=api;}(this,function(){
    'use strict';
    var languages=['zh','en','ja','ko','es','zh-Hant','fr','ru','pt-BR','it','id','pl','tr'];
    var names={zh:'简体中文',en:'English',ja:'日本語',ko:'한국어',es:'Español','zh-Hant':'繁體中文',fr:'Français',ru:'Русский','pt-BR':'Português (Brasil)',it:'Italiano',id:'Bahasa Indonesia',pl:'Polski',tr:'Türkçe'};
    function supported(code){return languages.indexOf(code)>=0;}
    function configuration(enabled,initial,uiLanguage){
        var list=[];if(Array.isArray(enabled))enabled.slice(0,13).forEach(function(code){if(supported(code)&&list.indexOf(code)<0)list.push(code);});
        if(!list.length){list=[supported(uiLanguage)?uiLanguage:'en'];if(list[0]!=='en')list.push('en');}
        initial=list.indexOf(initial)>=0?initial:list[0];return {languages:list,initial:initial};
    }
    function clean(value,max,multiline){
        value=String(value==null?'':value).replace(/\r\n?/g,'\n').replace(/[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]/g,'');
        if(!multiline)value=value.replace(/[\n\t]/g,' ');
        var out='',i,c,n;max=Math.min(16000,Math.max(0,max||16000));
        for(i=0;i<value.length&&out.length<max;i++){
            c=value.charCodeAt(i);
            if(c>=0xd800&&c<=0xdbff){n=value.charCodeAt(i+1);if(n>=0xdc00&&n<=0xdfff){if(out.length+2>max)break;out+=value.charAt(i)+value.charAt(++i);}continue;}
            if(c>=0xdc00&&c<=0xdfff)continue;out+=value.charAt(i);
        }return out;
    }
    function previousBoundary(text,pos){
        pos=Math.max(0,Math.min(pos,text.length));if(!pos)return 0;
        do{pos--;if(pos>0&&/[\udc00-\udfff]/.test(text.charAt(pos))&&/[\ud800-\udbff]/.test(text.charAt(pos-1)))pos--;
        }while(pos>0&&/[\u0300-\u036f\u3099\u309a\ufe0e\ufe0f]/.test(text.charAt(pos)));
        return pos;
    }
    function fold(s,language){var text=String(s).toLowerCase();if(language==='tr')text=text.replace(/ı/g,'i');return typeof text.normalize==='function'?text.normalize('NFD').replace(/[\u0300-\u036f]/g,''):text;}
    var kana={a:'あ',i:'い',u:'う',e:'え',o:'お',ka:'か',ki:'き',ku:'く',ke:'け',ko:'こ',sa:'さ',shi:'し',si:'し',su:'す',se:'せ',so:'そ',ta:'た',chi:'ち',ti:'ち',tsu:'つ',tu:'つ',te:'て',to:'と',na:'な',ni:'に',nu:'ぬ',ne:'ね',no:'の',ha:'は',hi:'ひ',fu:'ふ',hu:'ふ',he:'へ',ho:'ほ',ma:'ま',mi:'み',mu:'む',me:'め',mo:'も',ya:'や',yu:'ゆ',yo:'よ',ra:'ら',ri:'り',ru:'る',re:'れ',ro:'ろ',wa:'わ',wi:'うぃ',we:'うぇ',wo:'を',ga:'が',gi:'ぎ',gu:'ぐ',ge:'げ',go:'ご',za:'ざ',ji:'じ',zi:'じ',zu:'ず',ze:'ぜ',zo:'ぞ',da:'だ',di:'ぢ',du:'づ',de:'で',do:'ど',ba:'ば',bi:'び',bu:'ぶ',be:'べ',bo:'ぼ',pa:'ぱ',pi:'ぴ',pu:'ぷ',pe:'ぺ',po:'ぽ',va:'ゔぁ',vi:'ゔぃ',vu:'ゔ',ve:'ゔぇ',vo:'ゔぉ',fa:'ふぁ',fi:'ふぃ',fe:'ふぇ',fo:'ふぉ',she:'しぇ',je:'じぇ',che:'ちぇ',tsa:'つぁ',tsi:'つぃ',tse:'つぇ',tso:'つぉ',xa:'ぁ',xi:'ぃ',xu:'ぅ',xe:'ぇ',xo:'ぉ',xya:'ゃ',xyu:'ゅ',xyo:'ょ',xtsu:'っ',xtu:'っ',la:'ぁ',li:'ぃ',lu:'ぅ',le:'ぇ',lo:'ぉ',lya:'ゃ',lyu:'ゅ',lyo:'ょ',ltsu:'っ',ltu:'っ'};
    ['k','g','n','h','b','p','m','r'].forEach(function(c){['a','u','o'].forEach(function(v){kana[c+'y'+v]=kana[c+'i']+{a:'ゃ',u:'ゅ',o:'ょ'}[v];});});
    ['sh','ch','j','sy','ty','zy'].forEach(function(c){['a','u','o'].forEach(function(v){kana[c+v]=(c==='sh'||c==='sy'?'し':c==='ch'||c==='ty'?'ち':'じ')+{a:'ゃ',u:'ゅ',o:'ょ'}[v];});});
    function japanese(raw,final,katakana){
        raw=raw.toLowerCase();var out='',i=0,key,n;
        while(i<raw.length){
            if(raw[i]===raw[i+1]&&/[bcdfghjklmpqrstvwxyz]/.test(raw[i])){out+='っ';i++;continue;}
            if(raw[i]==='n'){
                if(raw[i+1]==="'"){out+='ん';i+=2;continue;}
                if(raw[i+1]==='n'){out+='ん';i+=i+2===raw.length?2:1;continue;}
                if(raw[i+1]&&/[^aiueoy]/.test(raw[i+1])||final&&i+1===raw.length){out+='ん';i++;continue;}
            }
            key='';for(n=Math.min(4,raw.length-i);n>0;n--)if(kana[raw.slice(i,i+n)]){key=raw.slice(i,i+n);break;}
            if(key){out+=kana[key];i+=key.length;}else{out+=raw[i]==='-'?'ー':raw[i];i++;}
        }
        return katakana?out.replace(/[ぁ-ゖ]/g,function(c){return String.fromCharCode(c.charCodeAt(0)+96);}):out;
    }
    var L='ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ',V='ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ',T=['','ㄱ','ㄲ','ㄳ','ㄴ','ㄵ','ㄶ','ㄷ','ㄹ','ㄺ','ㄻ','ㄼ','ㄽ','ㄾ','ㄿ','ㅀ','ㅁ','ㅂ','ㅄ','ㅅ','ㅆ','ㅇ','ㅈ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'];
    var koKeys={r:'ㄱ',R:'ㄲ',s:'ㄴ',e:'ㄷ',E:'ㄸ',f:'ㄹ',a:'ㅁ',q:'ㅂ',Q:'ㅃ',t:'ㅅ',T:'ㅆ',d:'ㅇ',w:'ㅈ',W:'ㅉ',c:'ㅊ',z:'ㅋ',x:'ㅌ',v:'ㅍ',g:'ㅎ',k:'ㅏ',o:'ㅐ',i:'ㅑ',O:'ㅒ',j:'ㅓ',p:'ㅔ',u:'ㅕ',P:'ㅖ',h:'ㅗ',y:'ㅛ',n:'ㅜ',b:'ㅠ',m:'ㅡ',l:'ㅣ'};
    var doubleV={'ㅗㅏ':'ㅘ','ㅗㅐ':'ㅙ','ㅗㅣ':'ㅚ','ㅜㅓ':'ㅝ','ㅜㅔ':'ㅞ','ㅜㅣ':'ㅟ','ㅡㅣ':'ㅢ'},doubleT={'ㄱㅅ':'ㄳ','ㄴㅈ':'ㄵ','ㄴㅎ':'ㄶ','ㄹㄱ':'ㄺ','ㄹㅁ':'ㄻ','ㄹㅂ':'ㄼ','ㄹㅅ':'ㄽ','ㄹㅌ':'ㄾ','ㄹㅍ':'ㄿ','ㄹㅎ':'ㅀ','ㅂㅅ':'ㅄ'},splitT={};
    Object.keys(doubleT).forEach(function(k){splitT[doubleT[k]]=k;});
    function korean(raw){
        var out='',on='',vow='',tail='';
        function glyph(){return on&&vow?String.fromCharCode(44032+(L.indexOf(on)*21+V.indexOf(vow))*28+Math.max(0,T.indexOf(tail))):on||vow||tail;}
        function flush(){out+=glyph();on=vow=tail='';}
        for(var i=0;i<raw.length;i++){
            var ch=koKeys[raw[i]]||raw[i];
            if(V.indexOf(ch)>=0){
                if(tail){var split=splitT[tail],next=split?split[1]:tail;tail=split?split[0]:'';flush();on=next;vow=ch;}
                else if(vow){if(doubleV[vow+ch])vow=doubleV[vow+ch];else{flush();vow=ch;}}
                else vow=ch;
            }else if(L.indexOf(ch)>=0){
                if(on&&vow){if(!tail&&T.indexOf(ch)>0)tail=ch;else if(doubleT[tail+ch])tail=doubleT[tail+ch];else{flush();on=ch;}}
                else{if(on||vow)flush();on=ch;}
            }else{flush();out+=ch;}
        }return out+glyph();
    }
    function lowerBound(array,value){var lo=0,hi=array.length;while(lo<hi){var mid=(lo+hi)>>1;if(array[mid]<value)lo=mid+1;else hi=mid;}return lo;}
    function Lexicon(data){this.data=data||{};this.wordIndexes={};this.pinyinKeys=Object.keys(this.data.pinyin||{}).sort();}
    Lexicon.prototype.words=function(language,prefix){
        if(!prefix)return [];var index=this.wordIndexes[language],data=(this.data.words||{})[language]||[],needle=fold(prefix,language);
        if(!index){index={};for(var i=0;i<data.length;i++){var word=data[i],key=fold(word,language).slice(0,2);if(!index[key])index[key]=[];index[key].push(word);}this.wordIndexes[language]=index;}
        var pool=needle.length>1?index[needle.slice(0,2)]||[]:data,result=[];
        for(var j=0;j<pool.length&&result.length<8;j++)if(fold(pool[j],language).indexOf(needle)===0&&pool[j]!==prefix)result.push(pool[j]);
        return result;
    };
    Lexicon.prototype.chinese=function(raw,traditional){
        var key=raw.toLowerCase().replace(/[ '\d]/g,''),dict=this.data.pinyin||{},values=(dict[key]||[]).slice(0,8),i,k;
        if(!values.length&&key.length>=2){i=lowerBound(this.pinyinKeys,key);for(var n=0;i<this.pinyinKeys.length&&n<5&&values.length<8;i++,n++){k=this.pinyinKeys[i];if(k.indexOf(key)!==0)break;values=values.concat(dict[k].slice(0,2));}}
        if(!values.length&&key.length>3){var at=0,parts=[],length;while(at<key.length){for(length=Math.min(20,key.length-at);length>0;length--)if(dict[key.slice(at,at+length)])break;if(!length)break;parts.push(dict[key.slice(at,at+length)][0]);at+=length;}if(at===key.length)values=[parts.join('')];}
        if(traditional){var map=this.data.traditional||{};values=values.map(function(s){return map[s]||s;});}return values.filter(function(v,index,a){return a.indexOf(v)===index;}).slice(0,8);
    };
    Lexicon.prototype.japanese=function(raw){var hira=japanese(raw,true,false),map=this.data.japanese||{},list=(map[hira]||[]).slice();if(list.indexOf(hira)<0)list.push(hira);var kata=japanese(raw,true,true);if(list.indexOf(kata)<0)list.push(kata);return list.slice(0,8);};
    function Model(language,lexicon){this.language=supported(language)?language:'en';this.lexicon=lexicon||new Lexicon();this.text='';this.start=this.end=0;this.raw='';this.anchor=0;this.max=16000;this.multiline=true;this.suggestions=true;this.hidden=false;}
    Model.prototype.open=function(text,max,multiline,hidden){this.max=max>0?Math.min(max,16000):16000;this.multiline=!!multiline;this.hidden=!!hidden;this.raw='';this.text=clean(text,this.max,this.multiline);this.start=0;this.end=this.text.length;};
    Model.prototype.setText=function(text,start,end){this.raw='';this.text=clean(text,this.max,this.multiline);this.start=Math.max(0,Math.min(start==null?this.text.length:start,this.text.length));this.end=Math.max(this.start,Math.min(end==null?this.start:end,this.text.length));};
    Model.prototype.previewRaw=function(){return this.language==='ja'?japanese(this.raw,false,false):this.language==='ko'?korean(this.raw):this.raw;};
    Model.prototype.value=function(){return this.raw?clean(this.text.slice(0,this.anchor)+this.previewRaw()+this.text.slice(this.anchor),this.max,this.multiline):this.text;};
    Model.prototype.insert=function(value){this.commit();value=clean(value,16000,this.multiline);var room=this.max-(this.text.length-(this.end-this.start)),insert=clean(value,room,this.multiline);if(room<=0)insert='';this.text=this.text.slice(0,this.start)+insert+this.text.slice(this.end);this.start+=insert.length;this.end=this.start;};
    Model.prototype.begin=function(){if(this.raw)return;this.text=this.text.slice(0,this.start)+this.text.slice(this.end);this.end=this.start;this.anchor=this.start;};
    Model.prototype.type=function(key){
        if(typeof key!=='string'||!key||key.length>8)return;
        var cjk=this.language==='zh'||this.language==='zh-Hant'||this.language==='ja'||this.language==='ko';
        if(!this.hidden&&cjk&&(/^[a-zA-Z']$/.test(key)||this.language==='ko'&&(L.indexOf(key)>=0||V.indexOf(key)>=0))){
            this.begin();var next=this.raw+key;if(next.length>64)return;var preview=this.language==='ko'?korean(next):this.language==='ja'?japanese(next,false,false):next;
            if(this.text.length+preview.length>this.max)return;this.raw=next;return;
        }
        if(key===' '&&this.raw){var choices=this.candidates(),preview=this.previewRaw();this.commit(this.language==='ko'||choices.indexOf(preview)>=0?preview:choices[0]);return;}
        this.insert(key);
    };
    Model.prototype.commit=function(value){
        if(!this.raw)return;var raw=this.raw,display=value==null?(this.language==='ja'?japanese(raw,true,false):this.previewRaw()):value;this.raw='';this.start=this.end=this.anchor;this.insert(display);
    };
    Model.prototype.backspace=function(){if(this.raw){this.raw=this.raw.slice(0,-1);return;}if(this.start!==this.end){this.insert('');return;}var before=previousBoundary(this.text,this.start);this.text=this.text.slice(0,before)+this.text.slice(this.end);this.start=this.end=before;};
    Model.prototype.candidates=function(){
        if(!this.suggestions||this.hidden)return [];
        if(this.raw){if(this.language==='zh'||this.language==='zh-Hant')return this.lexicon.chinese(this.raw,this.language==='zh-Hant');if(this.language==='ja')return this.lexicon.japanese(this.raw);return this.lexicon.words('ko',korean(this.raw));}
        if(this.start!==this.end)return [];var before=this.text.slice(0,this.start),match=before.match(/[^\s.,!?;:()[\]{}"，。！？、：；]+$/);return match?this.lexicon.words(this.language,match[0]):[];
    };
    Model.prototype.choose=function(word){if(this.hidden||typeof word!=='string'||this.candidates().indexOf(word)<0)return false;if(this.raw)this.commit(word);else{var m=this.text.slice(0,this.start).match(/[^\s.,!?;:()[\]{}"，。！？、：；]+$/);if(!m)return false;this.start-=m[0].length;this.insert(word);}return true;};
    Model.prototype.changeLanguage=function(language){if(supported(language)){this.commit();this.language=language;}};
    var qwerty=['qwertyuiop','asdfghjkl','zxcvbnm'];
    function layout(language,shift,symbols){
        if(symbols)return ['1234567890','!@#$%&*()-','_+=[]{}<>','.,:;?/\\|~'].map(function(row){return row.split('');});
        var rows=qwerty.map(function(row){return row.split('');}),extra={es:'ñáéíóúü',fr:'àâçéèêëîïôùûüœ', 'pt-BR':'áàâãçéêíóôõú',it:'àèéìòù',id:'é',pl:'ąćęłńóśźż',tr:'çğıİöşü'};
        if(language==='ru')rows=['йцукенгшщзхъ','фывапролджэ','ячсмитьбю'].map(function(row){return row.split('');});
        else if(language==='ko')rows=qwerty.map(function(row){return row.split('').map(function(key){return koKeys[shift?key.toUpperCase():key]||koKeys[key];});});
        else if(language==='fr')rows=['azertyuiop','qsdfghjklm','wxcvbn'].map(function(row){return row.split('');});
        if(extra[language])rows.push(extra[language].split(''));
        if(shift&&language!=='ko')rows=rows.map(function(row){return row.map(function(c){return language==='tr'&&c==='i'?'İ':c.toUpperCase();});});
        return rows;
    }
    return {languages:languages,names:names,supported:supported,configuration:configuration,clean:clean,previousBoundary:previousBoundary,japanese:japanese,korean:korean,layout:layout,Lexicon:Lexicon,Model:Model};
}));
