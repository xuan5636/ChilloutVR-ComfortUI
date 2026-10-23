/* Comfort UI multilingual keyboard: local candidates and fixed typed native input events. */
(function(){
    'use strict';
    var core=window.ComfortKeyboardCore;if(!core||window.ComfortKeyboard)return;
    var model=new core.Model('en',new core.Lexicon(window.__comfortKeyboardData||{}));
    var root,field,password,keys,candidates,languages,status,prefs={},session='',revision=0,desktop=false,closed=false,composing=false,shift=false,symbols=false,lastLayout='',repeatTimer,repeatDelay,renderTimer,selectionTimer,nativeRows=[],capsLock=false,collapsed=false,titleText='';
    var words={
        en:{title:'Keyboard',system:'System input · Win+H',next:'Next keyboard',settings:'Languages',done:'Done',suggest:'Word suggestions',systemHelp:'Click the text to use your installed IME or press Win+H. Shift+Enter: new line.',unavailable:'System input is unavailable here. Use the on-screen keyboard or paste.',focused:'System input active · Win+H for voice',submit:'Submit',close:'Close',space:'Space',back:'Backspace',clear:'Clear',paste:'Paste',choose:'Choose keyboards',defaultLabel:'Default keyboard',enabled:'Comfort keyboard',normal:'Game keyboard',syson:'System IME and voice',settingsHelp:'Enable languages, then switch with the bottom language button. Suggestions stay on this device.',compose:'Composing',voice:'Voice typing follows your Windows language and speech settings.'},
        zh:{title:'键盘',system:'系统输入 · Win+H',next:'切换键盘',settings:'语言设置',done:'完成',suggest:'文字联想',systemHelp:'点击输入区使用系统输入法，按 Win+H 语音输入；Shift+Enter 换行。',unavailable:'此处暂时无法接入系统输入，请使用屏幕键盘或粘贴。',focused:'系统输入已接入 · Win+H 语音输入',submit:'确认',close:'关闭',space:'空格',back:'退格',clear:'清空',paste:'粘贴',choose:'启用的键盘',defaultLabel:'默认键盘',enabled:'舒适键盘',normal:'原版键盘',syson:'系统输入法与语音',settingsHelp:'勾选需要的语言，再用底部按钮切换。联想在本机完成。',compose:'正在组词',voice:'语音语言和可用性由 Windows 语音设置决定。'},
        'zh-Hant':{title:'鍵盤',system:'系統輸入 · Win+H',next:'切換鍵盤',settings:'語言設定',done:'完成',suggest:'文字聯想',systemHelp:'點選輸入區使用系統輸入法，按 Win+H 語音輸入；Shift+Enter 換行。',unavailable:'此處暫時無法使用系統輸入，請使用螢幕鍵盤或貼上。',focused:'系統輸入已啟用 · Win+H 語音輸入',submit:'確認',close:'關閉',space:'空白',back:'退格',clear:'清除',paste:'貼上',choose:'啟用的鍵盤',defaultLabel:'預設鍵盤',enabled:'舒適鍵盤',normal:'原版鍵盤',syson:'系統輸入法與語音',settingsHelp:'勾選需要的語言，再以底部按鈕切換。聯想在本機完成。',compose:'組字中',voice:'語音語言與可用性取決於 Windows 語音設定。'},
        ja:{title:'キーボード',system:'システム入力 · Win+H',next:'言語切替',settings:'言語設定',done:'完了',suggest:'入力候補',systemHelp:'入力欄からIME、Win+Hで音声入力。Shift+Enterで改行。',unavailable:'システム入力を使用できません。画面キーボードか貼り付けを使用してください。',focused:'システム入力中 · Win+Hで音声入力',submit:'確定',close:'閉じる',space:'空白',back:'削除',clear:'クリア',paste:'貼り付け',choose:'使用するキーボード',defaultLabel:'既定のキーボード',enabled:'Comfort キーボード',normal:'ゲームのキーボード',syson:'システムIMEと音声',settingsHelp:'使う言語を選び、下のボタンで切り替えます。候補は端末内で処理します。',compose:'変換中',voice:'音声の言語と利用可否はWindowsの設定に従います。'},
        ko:{title:'키보드',system:'시스템 입력 · Win+H',next:'키보드 전환',settings:'언어 설정',done:'완료',suggest:'단어 제안',systemHelp:'입력란을 눌러 IME를 사용하거나 Win+H로 음성 입력. Shift+Enter: 줄 바꿈.',unavailable:'시스템 입력을 사용할 수 없습니다. 화면 키보드나 붙여넣기를 사용하세요.',focused:'시스템 입력 중 · Win+H 음성 입력',submit:'확인',close:'닫기',space:'공백',back:'지우기',clear:'모두 지우기',paste:'붙여넣기',choose:'사용할 키보드',defaultLabel:'기본 키보드',enabled:'Comfort 키보드',normal:'게임 키보드',syson:'시스템 IME 및 음성',settingsHelp:'언어를 선택하고 아래 버튼으로 전환하세요. 제안은 기기에서 처리됩니다.',compose:'조합 중',voice:'음성 언어와 사용 가능 여부는 Windows 설정을 따릅니다.'}
    };
    Object.assign(words,{"es":{"title":"Teclado","system":"Entrada del sistema · Win+H","next":"Siguiente teclado","settings":"Idiomas","done":"Listo","suggest":"Sugerencias","systemHelp":"Pulsa el texto para usar el IME o Win+H para dictar. Mayús+Intro: nueva línea.","unavailable":"Entrada del sistema no disponible. Usa el teclado en pantalla o pega el texto.","focused":"Entrada del sistema activa · Win+H para dictar","submit":"Confirmar","close":"Cerrar","space":"Espacio","back":"Borrar","clear":"Vaciar","paste":"Pegar","choose":"Teclados habilitados","defaultLabel":"Teclado predeterminado","enabled":"Teclado Comfort","normal":"Teclado del juego","syson":"IME y dictado del sistema","settingsHelp":"Elige idiomas y cambia con el botón inferior. Las sugerencias se procesan en tu dispositivo.","compose":"Composición","voice":"El dictado utiliza el idioma y los ajustes de voz de Windows."},"fr":{"title":"Clavier","system":"Saisie système · Win+H","next":"Clavier suivant","settings":"Langues","done":"Terminé","suggest":"Suggestions","systemHelp":"Cliquez sur le texte pour utiliser l’IME ou Win+H pour dicter. Maj+Entrée : nouvelle ligne.","unavailable":"Saisie système indisponible. Utilisez le clavier à l’écran ou collez le texte.","focused":"Saisie système active · Win+H pour dicter","submit":"Valider","close":"Fermer","space":"Espace","back":"Effacer","clear":"Tout effacer","paste":"Coller","choose":"Claviers activés","defaultLabel":"Clavier par défaut","enabled":"Clavier Comfort","normal":"Clavier du jeu","syson":"IME et dictée système","settingsHelp":"Choisissez les langues, puis utilisez le bouton du bas. Les suggestions restent sur cet appareil.","compose":"Composition","voice":"La dictée utilise la langue et les paramètres vocaux de Windows."},"ru":{"title":"Клавиатура","system":"Системный ввод · Win+H","next":"Следующая раскладка","settings":"Языки","done":"Готово","suggest":"Подсказки слов","systemHelp":"Нажмите на текст для IME или Win+H для диктовки. Shift+Enter: новая строка.","unavailable":"Системный ввод недоступен. Используйте экранную клавиатуру или вставку.","focused":"Системный ввод включён · Win+H для диктовки","submit":"Подтвердить","close":"Закрыть","space":"Пробел","back":"Удалить","clear":"Очистить","paste":"Вставить","choose":"Включённые клавиатуры","defaultLabel":"Клавиатура по умолчанию","enabled":"Клавиатура Comfort","normal":"Клавиатура игры","syson":"Системный IME и диктовка","settingsHelp":"Выберите языки и переключайте их кнопкой внизу. Подсказки обрабатываются на устройстве.","compose":"Набор","voice":"Язык и доступность диктовки зависят от настроек Windows."},"pt-BR":{"title":"Teclado","system":"Entrada do sistema · Win+H","next":"Próximo teclado","settings":"Idiomas","done":"Concluído","suggest":"Sugestões de palavras","systemHelp":"Clique no texto para usar o IME ou Win+H para ditar. Shift+Enter: nova linha.","unavailable":"Entrada do sistema indisponível. Use o teclado na tela ou cole o texto.","focused":"Entrada do sistema ativa · Win+H para ditar","submit":"Confirmar","close":"Fechar","space":"Espaço","back":"Apagar","clear":"Limpar","paste":"Colar","choose":"Teclados habilitados","defaultLabel":"Teclado padrão","enabled":"Teclado Comfort","normal":"Teclado do jogo","syson":"IME e ditado do sistema","settingsHelp":"Escolha idiomas e alterne pelo botão inferior. As sugestões ficam neste dispositivo.","compose":"Composição","voice":"O ditado segue o idioma e as configurações de voz do Windows."},"it":{"title":"Tastiera","system":"Input di sistema · Win+H","next":"Tastiera successiva","settings":"Lingue","done":"Fine","suggest":"Suggerimenti","systemHelp":"Tocca il testo per usare l’IME o Win+H per dettare. Maiusc+Invio: nuova riga.","unavailable":"Input di sistema non disponibile. Usa la tastiera su schermo o incolla.","focused":"Input di sistema attivo · Win+H per dettare","submit":"Conferma","close":"Chiudi","space":"Spazio","back":"Cancella","clear":"Svuota","paste":"Incolla","choose":"Tastiere abilitate","defaultLabel":"Tastiera predefinita","enabled":"Tastiera Comfort","normal":"Tastiera del gioco","syson":"IME e dettatura di sistema","settingsHelp":"Scegli le lingue e passa da una all’altra con il pulsante in basso. I suggerimenti restano sul dispositivo.","compose":"Composizione","voice":"La dettatura segue la lingua e le impostazioni vocali di Windows."},"id":{"title":"Papan ketik","system":"Input sistem · Win+H","next":"Papan ketik berikutnya","settings":"Bahasa","done":"Selesai","suggest":"Saran kata","systemHelp":"Klik teks untuk memakai IME atau Win+H untuk dikte. Shift+Enter: baris baru.","unavailable":"Input sistem tidak tersedia. Gunakan papan ketik layar atau tempel.","focused":"Input sistem aktif · Win+H untuk dikte","submit":"Konfirmasi","close":"Tutup","space":"Spasi","back":"Hapus","clear":"Bersihkan","paste":"Tempel","choose":"Papan ketik aktif","defaultLabel":"Papan ketik bawaan","enabled":"Papan ketik Comfort","normal":"Papan ketik game","syson":"IME dan dikte sistem","settingsHelp":"Pilih bahasa, lalu ganti dengan tombol di bawah. Saran diproses di perangkat ini.","compose":"Menyusun","voice":"Bahasa dan ketersediaan dikte mengikuti pengaturan suara Windows."},"pl":{"title":"Klawiatura","system":"Wprowadzanie systemowe · Win+H","next":"Następna klawiatura","settings":"Języki","done":"Gotowe","suggest":"Podpowiedzi słów","systemHelp":"Kliknij tekst, aby użyć IME, lub Win+H, aby dyktować. Shift+Enter: nowy wiersz.","unavailable":"Wprowadzanie systemowe niedostępne. Użyj klawiatury ekranowej lub wklej tekst.","focused":"Wprowadzanie systemowe aktywne · Win+H do dyktowania","submit":"Zatwierdź","close":"Zamknij","space":"Spacja","back":"Usuń","clear":"Wyczyść","paste":"Wklej","choose":"Włączone klawiatury","defaultLabel":"Domyślna klawiatura","enabled":"Klawiatura Comfort","normal":"Klawiatura gry","syson":"Systemowy IME i dyktowanie","settingsHelp":"Wybierz języki i przełączaj je przyciskiem na dole. Podpowiedzi pozostają na urządzeniu.","compose":"Wpisywanie","voice":"Dyktowanie korzysta z języka i ustawień mowy Windows."},"tr":{"title":"Klavye","system":"Sistem girişi · Win+H","next":"Sonraki klavye","settings":"Diller","done":"Bitti","suggest":"Sözcük önerileri","systemHelp":"IME için metne tıklayın veya dikte için Win+H kullanın. Shift+Enter: yeni satır.","unavailable":"Sistem girişi kullanılamıyor. Ekran klavyesini veya yapıştırmayı kullanın.","focused":"Sistem girişi etkin · Dikte için Win+H","submit":"Onayla","close":"Kapat","space":"Boşluk","back":"Sil","clear":"Temizle","paste":"Yapıştır","choose":"Etkin klavyeler","defaultLabel":"Varsayılan klavye","enabled":"Comfort klavyesi","normal":"Oyun klavyesi","syson":"Sistem IME ve dikte","settingsHelp":"Dilleri seçin ve alttaki düğmeyle değiştirin. Öneriler bu cihazda işlenir.","compose":"Birleştirme","voice":"Dikte dili ve kullanılabilirliği Windows konuşma ayarlarına bağlıdır."}});
    function t(key){var dictionary=words[prefs.language]||{};return dictionary[key]||words.en[key]||key;}
    function node(tag,className,text){var el=document.createElement(tag);if(className)el.className=className;if(text!=null)el.textContent=text;return el;}
    function button(text,action,className){var b=node('button',className||'',text);b.type='button';b.addEventListener('mousedown',function(e){e.preventDefault();});b.addEventListener('click',function(e){e.stopPropagation();action();});return b;}
    function getField(){return model.hidden?password:field;}
    function call(value){if(!session||!window.engine||typeof engine.call!=='function')return;value.session=session;try{var promise=engine.call('ComfortUI_Keyboard',JSON.stringify(value));if(promise&&promise.catch)promise.catch(function(){notice('unavailable');});}catch(ignored){notice('unavailable');}}
    function rectangle(){var r=getField().getBoundingClientRect();return [r.left,r.top,r.width,r.height];}
    function syncNative(action){if(!session)return;if(action==='focus')model.commit();if(model.raw){call({action:'closed'});return;}revision++;call({action:action,text:model.text,start:model.start,end:model.end,revision:revision,language:model.language,rectangle:rectangle()});}
    function notice(code){if(status)status.textContent=t(code);}
    function updateNativeModel(){
        // The native input is retained solely for its show/close/history semantics.
        // Never pass user text to its innerHTML-based multiline preview renderer.
        try{if(typeof keyboardInputText!=='undefined')keyboardInputText=model.value().split('\n');if(typeof keyboardInputLineIndex!=='undefined')keyboardInputLineIndex=keyboardInputText.length-1;if(typeof keyboardInputElement!=='undefined')keyboardInputElement.value=model.value().split('\n').slice(-1)[0];}catch(ignored){}
    }
    function cancelRepeat(){clearInterval(repeatTimer);clearTimeout(repeatDelay);repeatTimer=repeatDelay=null;}
    function press(key){if(closed||composing)return;model.type(key);renderText();syncNative('edit');}
    function back(){if(composing)return;model.backspace();renderText();syncNative('edit');}
    function renderText(){
        if(!root)return;var input=getField(),value=model.value();if(input.value!==value)input.value=value;
        if(!composing){var caret=model.raw?model.anchor+model.previewRaw().length:model.start;try{input.setSelectionRange(caret,model.raw?caret:model.end);}catch(ignored){}}
        field.style.display=model.hidden?'none':'';password.style.display=model.hidden?'':'none';
        root.querySelector('.cu-kb-count').textContent=value.length+' / '+model.max;
        root.querySelector('.cu-kb-compose').textContent=!model.hidden&&model.raw?t('compose')+' · '+model.previewRaw():'';
        candidates.textContent='';
        (composing?[]:model.candidates()).forEach(function(word,index){var choice=button((index+1)+'  '+word,function(){if(!composing&&model.choose(word)){renderText();syncNative('edit');}},'cu-kb-candidate');choice.setAttribute('aria-label',word);candidates.appendChild(choice);});
        if(!composing&&!model.hidden){var extra=core.layout(model.language,false,false)[3]||[];extra.forEach(function(char){candidates.appendChild(button(char,function(){press(char);},'cu-kb-accent'));});}
        candidates.style.display=model.hidden?'none':'';updateNativeModel();
    }
    function submit(){
        if(composing)return;model.commit(model.raw&&model.language!=='ko'?model.candidates()[0]:undefined);renderText();
        // Submission remains an explicit user action; IME Enter commits only.
        if(!model.multiline&&!model.hidden&&typeof keyboardHistory!=='undefined')keyboardHistory.push(model.text);if(window.engine)engine.call('CVRUIKeyboardSubmit',model.text.replace(/\n$/,''));close(false);
    }
    function close(nativeClose){closed=true;composing=false;cancelRepeat();call({action:'closed'});session='';model.open('',0,true,true);updateNativeModel();if(root){field.value=password.value='';candidates.textContent='';root.querySelector('.cu-kb-compose').textContent='';root.setAttribute('data-open','false');}if(nativeClose&&window.engine)engine.call('CVRUIKeyboardClose');}
    function nextLanguage(){if(composing)return;var enabled=core.configuration(prefs.keyboardLanguages,prefs.keyboardDefault,prefs.language).languages;var index=enabled.indexOf(model.language);model.changeLanguage(enabled[(index+1)%enabled.length]);shift=false;renderKeys();renderLanguages();renderText();syncNative('edit');}
    function chooseLanguage(code){if(composing)return;model.changeLanguage(code);shift=false;renderKeys();renderLanguages();renderText();syncNative('edit');}
    function renderLanguages(){
        if(!languages)return;languages.textContent='';var enabled=core.configuration(prefs.keyboardLanguages,prefs.keyboardDefault,prefs.language).languages;
        var index=enabled.indexOf(model.language),next=button('🌐 '+core.names[model.language]+' ›',nextLanguage,'cu-kb-language-next');next.title=t('next');languages.appendChild(next);
        var add=button('+',function(){var panel=root.querySelector('.cu-kb-preferences'),expanded=!panel.classList.contains('cu-kb-expanded');panel.classList.toggle('cu-kb-expanded',expanded);root.setAttribute('data-settings-open',String(expanded));renderSettings(panel);},'cu-kb-add');add.setAttribute('aria-label',t('choose'));languages.appendChild(add);
        enabled.forEach(function(code){var b=button(core.names[code],function(){chooseLanguage(code);},'cu-kb-language');b.setAttribute('aria-pressed',String(model.language===code));languages.appendChild(b);});
    }
    function nativeText(value){return window.ComfortUI?ComfortUI.systemText(value):value;}
    function moveCaret(direction){
        if(composing)return;model.commit();var text=model.text,pos=model.start;
        if((direction==='left'||direction==='right')&&model.start!==model.end)pos=direction==='left'?model.start:model.end;
        else if(direction==='left')pos=core.previousBoundary(text,pos);
        else if(direction==='right'){pos=Math.min(text.length,pos+1);if(pos<text.length&&/[\udc00-\udfff]/.test(text.charAt(pos)))pos++;}
        else if(!model.multiline&&!model.hidden&&typeof getKeyboardHistory==='function'){
            updateNativeModel();getKeyboardHistory(direction==='down');model.setText(keyboardInputText.join('\n'));renderText();syncNative('edit');return;
        }else{
            var beginning=text.lastIndexOf('\n',pos-1)+1,column=pos-beginning;
            if(direction==='up'&&beginning>0){var previous=text.lastIndexOf('\n',beginning-2)+1;pos=Math.min(beginning-1,previous+column);}
            if(direction==='down'){var next=text.indexOf('\n',pos);if(next>=0){var finish=text.indexOf('\n',next+1);pos=Math.min(finish<0?text.length:finish,next+1+column);}}
        }
        model.start=model.end=pos;renderText();syncNative('edit');
    }
    function nativeFunction(name){
        if(composing&&name!=='CANCEL')return;
        if(name==='BACKSPACE')back();
        else if(name==='TAB')press('\t');
        else if(name==='SUBMIT'){if((shift||capsLock)&&model.multiline)press('\n');else submit();}
        else if(name==='COPY'){if(!model.hidden){model.commit();call({action:'copy',text:model.start!==model.end?model.text.slice(model.start,model.end):model.text});}}
        else if(name==='PASTE'&&typeof keyboardPasteFromClipboard==='function')keyboardPasteFromClipboard();
        else if(['LEFT','RIGHT','UP','DOWN'].indexOf(name)>=0)moveCaret(name.toLowerCase());
    }
    function renderKeys(){
        if(!keys)return;root.setAttribute('data-keyboard-language',model.language);
        var shifted=shift||capsLock,layout=core.layout(model.language,shifted,false),signature=JSON.stringify([layout,shifted,model.multiline,prefs.language]);if(signature===lastLayout)return;lastLayout=signature;keys.textContent='';
        nativeRows.forEach(function(row,rowIndex){
            var group=node('div','cu-kb-row'),keyIndex=0;
            row.forEach(function(item){
                if(item.spacer){var space=node('span','cu-kb-spacer');space.style.flex='1.4';group.appendChild(space);return;}
                var label,action;
                if(item.key!==null){
                    var char=shifted?item.shift:item.key;
                    if(rowIndex>=1&&rowIndex<=3){
                        var mapped=layout[rowIndex-1]||[],fallback=model.language==='fr'?[['^','$','*'],['ù'],[',',';',':','!']][rowIndex-1]:model.language==='ru'?[[],[],['.']][rowIndex-1]:null;
                        char=keyIndex<mapped.length?mapped[keyIndex]:fallback&&fallback[keyIndex-mapped.length]!=null?fallback[keyIndex-mapped.length]:char;
                    }
                    keyIndex++;label=char===' '?t('space'):char;
                    action=function(){press(char);if(shift&&!capsLock){shift=false;renderKeys();}};
                }else if(item.mod){
                    label=item.lock?'Caps':'Shift';action=function(){if(item.lock){capsLock=!capsLock;shift=false;}else shift=!shift;renderKeys();};
                }else{
                    label={BACKSPACE:t('back'),TAB:'Tab',SUBMIT:shifted&&model.multiline?nativeText('New line'):t('submit'),COPY:nativeText('Copy'),PASTE:t('paste'),LEFT:'←',RIGHT:'→',UP:'↑',DOWN:'↓'}[item.func]||item.func;
                    action=function(){nativeFunction(item.func);};
                }
                var b=button(label,action,'cu-kb-key'+(item.func==='SUBMIT'?' cu-kb-submit':''));b.style.flex=String(item.weight);b.setAttribute('data-native-function',item.func||item.mod||'key');if(item.key!==null)b.setAttribute('data-native-key',item.key);
                if(item.mod)b.setAttribute('aria-pressed',String(item.lock?capsLock:shift));
                if(item.func==='COPY')b.disabled=model.hidden;
                if(item.func==='BACKSPACE'){b.addEventListener('mousedown',function(){cancelRepeat();repeatDelay=setTimeout(function(){repeatTimer=setInterval(back,70);},450);});['mouseup','mouseleave'].forEach(function(event){b.addEventListener(event,cancelRepeat);});}
                group.appendChild(b);
            });keys.appendChild(group);
        });
    }
    function save(next){if(window.ComfortUI)ComfortUI.updatePreferences(next,false);else{prefs=next;configure(prefs);}}
    function settingsBlock(container){
        container.textContent='';container.appendChild(node('h3','',t('choose')));container.appendChild(node('p','',t('settingsHelp')));
        var enabled=core.configuration(prefs.keyboardLanguages,prefs.keyboardDefault,prefs.language),list=node('div','cu-kb-checklist');
        core.languages.forEach(function(code){var label=node('label','cu-kb-check'),box=node('input');box.type='checkbox';box.checked=enabled.languages.indexOf(code)>=0;box.setAttribute('aria-label',core.names[code]);box.addEventListener('change',function(){var next=Object.assign({},prefs),values=enabled.languages.slice(),i=values.indexOf(code);if(box.checked&&i<0)values.push(code);if(!box.checked&&i>=0)values.splice(i,1);if(!values.length){box.checked=true;return;}next.keyboardLanguages=values;next.keyboardDefault=values.indexOf(prefs.keyboardDefault)>=0?prefs.keyboardDefault:values[0];save(next);});label.appendChild(box);label.appendChild(node('span','',core.names[code]));list.appendChild(label);});container.appendChild(list);
        container.appendChild(node('p','cu-kb-settings-label',t('defaultLabel')));var defaults=node('div','cu-kb-defaults');enabled.languages.forEach(function(code){var b=button(core.names[code],function(){var next=Object.assign({},prefs);next.keyboardDefault=code;save(next);});b.setAttribute('aria-pressed',String(enabled.initial===code));defaults.appendChild(b);});container.appendChild(defaults);
        [['keyboardEnabled','enabled'],['keyboardSuggestions','suggest'],['keyboardSystemInput','syson']].forEach(function(item){var label=node('label','cu-kb-check'),box=node('input');box.type='checkbox';box.checked=prefs[item[0]]!==false;box.addEventListener('change',function(){var next=Object.assign({},prefs);next[item[0]]=box.checked;save(next);});label.appendChild(box);label.appendChild(node('span','',t(item[1])));container.appendChild(label);});
        container.appendChild(node('p','cu-kb-note',t('voice')));
    }
    function renderSettings(container,configuration){if(configuration)prefs=configuration;if(container)settingsBlock(container);}
    function focusSystem(){
        if(!desktop||prefs.keyboardSystemInput===false){notice('unavailable');return;}
        syncNative('focus');
    }
    function inputChange(input){
        if(composing)return;model.setText(input.value,input.selectionStart,input.selectionEnd);renderText();syncNative('edit');
        if(window.engine)engine.trigger('CVRUIKeyboardOnInput');
    }
    function physical(event){
        if(composing||event.isComposing||event.keyCode===229)return;
        event.stopImmediatePropagation();
        if(event.type==='keypress')return;
        if(event.ctrlKey||event.metaKey||event.altKey)return;
        if(event.key==='Enter'){event.preventDefault();if(model.raw){model.commit(model.language==='ko'?undefined:model.candidates()[0]);renderText();syncNative('edit');}else if(event.shiftKey&&model.multiline)press('\n');else submit();return;}
        if(event.key==='Escape'){event.preventDefault();if(model.raw){model.raw='';renderText();}else close(true);return;}
        if(event.key==='Backspace'&&model.raw){event.preventDefault();back();return;}
        if(event.key===' '&&model.raw){event.preventDefault();press(' ');return;}
        if(model.raw&&/^[1-8]$/.test(event.key)&&model.candidates()[Number(event.key)-1]){event.preventDefault();model.choose(model.candidates()[Number(event.key)-1]);renderText();syncNative('edit');return;}
        if(!model.hidden&&(model.language==='zh'||model.language==='zh-Hant'||model.language==='ja'||model.language==='ko')&&(/^[a-zA-Z']$/.test(event.key)||event.key===' '&&model.raw)){
            event.preventDefault();model.start=getField().selectionStart;model.end=getField().selectionEnd;press(event.key);
        }
    }
    function mount(){
        if(root||!document.querySelector('.keyboard'))return;
        var parent=document.querySelector('.keyboard');
        nativeRows=Array.prototype.slice.call(parent.children).filter(function(el){return el.classList.contains('keyboard-row');}).slice(1).map(function(row){return Array.prototype.map.call(row.children,function(el){return {key:el.hasAttribute('data-key')?el.getAttribute('data-key'):null,shift:el.getAttribute('data-key-shift')||el.getAttribute('data-key'),func:el.getAttribute('data-key-func'),mod:el.getAttribute('data-key-mod')||el.getAttribute('data-key-mod-lock'),lock:el.hasAttribute('data-key-mod-lock'),spacer:el.classList.contains('spacer'),weight:el.classList.contains('space')?10:el.classList.contains('widest')?2.5:el.classList.contains('wider')?2:el.classList.contains('wide')?1.5:1};});});
        if(nativeRows.length!==5)return; // Preserve native UI if a future game changes its structure.
        root=node('section','cu-multilingual-keyboard cu-only');root.id='comfort-keyboard';root.setAttribute('data-open','false');
        var heading=node('div','cu-kb-heading'),clear=button(t('clear'),function(){if(composing)return;model.open('',model.max,model.multiline,model.hidden);renderText();syncNative('edit');},'cu-kb-clear');heading.appendChild(clear);
        var inputBlock=node('div','cu-kb-input-block'),meta=node('div','cu-kb-meta');meta.appendChild(node('span','cu-kb-title',t('title')));meta.appendChild(node('span','cu-kb-count'));inputBlock.appendChild(meta);heading.appendChild(inputBlock);
        function iconButton(func,action,extra){var b=button('',action,'cu-kb-header-button '+extra),source=parent.querySelector('[data-key-func="'+func+'"] img');if(source){var img=node('img');img.src=source.getAttribute('src');img.alt='';b.appendChild(img);}else b.textContent=func==='SETTINGS'?'⚙':func==='COLLAPSE'?'⌃':'×';b.setAttribute('aria-label',nativeText(func==='SETTINGS'?'Keyboard Settings':func==='COLLAPSE'?'Collapse/expand keyboard to and from entry bar only mode':'Close'));heading.appendChild(b);return b;}
        iconButton('SETTINGS',function(){var panel=parent.querySelector('.settings');if(panel){var show=panel.classList.contains('hide');panel.classList.toggle('hide',!show);panel.classList.toggle('show',show);}},'cu-kb-native-settings');
        iconButton('COLLAPSE',function(){collapsed=!collapsed;root.setAttribute('data-collapsed',String(collapsed));if(typeof setCollapsed==='function')setCollapsed(collapsed);},'cu-kb-collapse');
        iconButton('CANCEL',function(){close(true);},'cu-kb-close');root.appendChild(heading);
        field=node('textarea','cu-kb-text');field.setAttribute('aria-label',t('title'));field.spellcheck=false;field.rows=2;field.maxLength=16000;
        password=node('input','cu-kb-text');password.type='password';password.setAttribute('aria-label',t('title'));password.autocomplete='off';
        [field,password].forEach(function(input){
            input.addEventListener('keydown',physical,true);input.addEventListener('keypress',physical,true);
            input.addEventListener('input',function(){inputChange(input);});
            input.addEventListener('compositionstart',function(){composing=true;});
            input.addEventListener('compositionend',function(){composing=false;inputChange(input);});
            input.addEventListener('click',function(){model.start=input.selectionStart;model.end=input.selectionEnd;if(desktop&&prefs.keyboardSystemInput!==false)focusSystem();});
            input.addEventListener('keyup',function(){if(!composing&&!model.raw){model.start=input.selectionStart;model.end=input.selectionEnd;renderText();}});
            inputBlock.appendChild(input);
        });
        var hint=node('div','cu-kb-hint');hint.appendChild(node('span','cu-kb-compose'));hint.appendChild(button(t('system'),focusSystem,'cu-kb-system'));root.appendChild(hint);
        status=node('p','cu-kb-status',t('systemHelp'));root.appendChild(status);
        candidates=node('div','cu-kb-candidates');candidates.setAttribute('aria-label',t('suggest'));root.appendChild(candidates);
        keys=node('div','cu-kb-keys');root.appendChild(keys);languages=node('div','cu-kb-languages');root.appendChild(languages);
        var preferencesPanel=node('div','cu-kb-preferences list-content scroll-content');preferencesPanel.setAttribute('nohorizontal','true');root.appendChild(preferencesPanel);document.querySelector('.keyboard').appendChild(root);
        if(typeof updateTextArea==='function'){
            var nativeUpdate=updateTextArea;
            window.updateTextArea=function(){if(prefs.keyboardEnabled!==false&&document.documentElement.getAttribute('data-comfort-enabled')==='true'){return;}return nativeUpdate.apply(this,arguments);};
        }
        document.addEventListener('mouseup',cancelRepeat);window.addEventListener('blur',cancelRepeat);
        if(window.engine){
            engine.on('showKeyboard',function(value,title,placeholder,submitText,max,hidden,multiline,collapse){titleText=typeof title==='string'?title:'';open({text:value,maximum:max,hidden:hidden,multiline:multiline,collapse:collapse},false);});
            engine.on('pasteFromClipboard',function(text){if(!closed&&typeof text==='string'){model.insert(text);renderText();syncNative('edit');}});
        }
        selectionTimer=setInterval(function(){if(!closed&&session&&desktop&&prefs.keyboardEnabled!==false&&prefs.keyboardSystemInput!==false&&document.documentElement.getAttribute('data-comfort-enabled')==='true')call({action:'layout',rectangle:rectangle()});},250);
    }
    function open(data,native){
        mount();if(!root)return;closed=false;composing=false;shift=symbols=capsLock=false;lastLayout='';if(!native)collapsed=data.collapse===true;root.setAttribute('data-collapsed',String(collapsed));root.setAttribute('data-settings-open','false');root.querySelector('.cu-kb-preferences').classList.remove('cu-kb-expanded');
        var config=core.configuration(prefs.keyboardLanguages,prefs.keyboardDefault,prefs.language);
        model.changeLanguage(config.initial);model.open(data.text,data.maximum,data.multiline,data.hidden);model.suggestions=prefs.keyboardSuggestions!==false;
        root.querySelector('.cu-kb-title').textContent=titleText||t('title');
        root.setAttribute('data-open','true');notice('systemHelp');renderKeys();renderLanguages();renderText();
        getField().focus();
    }
    function configure(value){
        prefs=value||{};mount();if(!root)return;
        root.closest('.keyboard').setAttribute('data-cu-keyboard',String(prefs.keyboardEnabled!==false));
        model.suggestions=prefs.keyboardSuggestions!==false;
        var enabled=core.configuration(prefs.keyboardLanguages,prefs.keyboardDefault,prefs.language);
        if(enabled.languages.indexOf(model.language)<0)model.changeLanguage(enabled.initial);
        root.querySelector('.cu-kb-title').textContent=titleText||t('title');root.querySelector('.cu-kb-clear').textContent=t('clear');root.querySelector('.cu-kb-system').textContent=t('system');
        field.setAttribute('lang',model.language==='zh'?'zh-CN':model.language==='zh-Hant'?'zh-TW':model.language);
        lastLayout='';renderKeys();renderLanguages();renderText();
        if(root.querySelector('.cu-kb-preferences').classList.contains('cu-kb-expanded'))renderSettings(root.querySelector('.cu-kb-preferences'));
        if(prefs.keyboardEnabled===false||prefs.keyboardSystemInput===false)call({action:'closed'});
    }
    function receive(json){
        if(typeof json!=='string'||json.length>200000)return;var data;try{data=JSON.parse(json);}catch(ignored){return;}
        if(!data||typeof data!=='object'||Array.isArray(data)||!core.supported(model.language)||typeof data.session!=='string'||!/^[a-f0-9]{32}$/.test(data.session))return;
        if(data.kind==='open'){
            if(typeof data.text!=='string'||data.text.length>16000||typeof data.multiline!=='boolean'||typeof data.hidden!=='boolean'||typeof data.desktop!=='boolean'||!Number.isInteger(data.maximum)||data.maximum<1||data.maximum>16000)return;
            if(session===data.session)return;session=data.session;revision=0;desktop=data.desktop;open(data,true);return;
        }
        if(data.session!==session||closed)return;
        if(data.kind==='status'){if(data.code==='focused'||data.code==='unavailable')notice(data.code);return;}
        if(data.kind!=='text'||typeof data.text!=='string'||data.text.length>16000||!Number.isInteger(data.start)||!Number.isInteger(data.end)||data.start<0||data.end<data.start||data.end>data.text.length||typeof data.composing!=='boolean'||typeof data.focused!=='boolean'||['','submit','close'].indexOf(data.action)<0||!Number.isInteger(data.revision)||data.revision<revision)return;
        composing=data.composing;model.setText(data.text,data.start,data.end);renderText();if(window.engine)engine.trigger('CVRUIKeyboardOnInput');
        if(data.action==='submit'&&!composing)submit();else if(data.action==='close'&&!composing)close(true);
    }
    window.ComfortKeyboard={configure:configure,receive:receive,settings:renderSettings};
    window.addEventListener('beforeunload',function(){close(false);clearInterval(selectionTimer);clearTimeout(renderTimer);});
}());
