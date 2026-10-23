/* Fixed native event bridge. The initializer below contains embedded code only. */
(function () {
    'use strict';
    if (window.ComfortNativeBridge) return;
    var installed=false,started=false,closed=false;
    function record(value){return value!==null && typeof value==='object' && !Array.isArray(value);}
    function parse(json,limit){
        if(typeof json!=='string' || json.length>limit)return null;
        try{var value=JSON.parse(json);return record(value)?value:null;}catch(ignored){return null;}
    }
    function localState(value){return Array.isArray(value) && value.length>=2 && value.length<=3 && typeof value[0]==='string' && /^[A-Za-z]{1,40}$/.test(value[0]) && typeof value[1]==='number' && isFinite(value[1]) && value[1]>=-1 && value[1]<=100 && (value.length===2 || typeof value[2]==='boolean');}
    function ready(){try{window.engine.call('ComfortUI_BridgeReady','1');}catch(ignored){}}
    function install(initialize){
        if(installed){ready();return;}
        if(!window.engine || typeof engine.on!=='function' || typeof engine.call!=='function' || typeof initialize!=='function')return;
        installed=true;
        engine.on('ComfortUI_Bootstrap',function(json){
            if(closed)return;
            var data=parse(json,8388608);
            if(!data || data.protocol!==1 || typeof data.context!=='string' || !/^(main|quick|login|keyboard|hud)$/.test(data.context) || !record(data.preferences) || typeof data.chatEnabled!=='boolean' || !localState(data.localModelState) || !(data.microphone===null || typeof data.microphone==='string' && /^(muted|live|tts)$/.test(data.microphone)) || !record(data.existingTranslations) || !record(data.settingsTranslations) || !record(data.locales) || typeof data.styles!=='string' || data.styles.length>400000)return;
            if(started){if(window.ComfortUI){ComfortUI.updatePreferences(data.preferences,true);ComfortUI.chatState(data.chatEnabled);}return;}
            window.__comfortContext=data.context;
            window.__comfortHost={native:true,preferences:data.preferences};
            window.__comfortChatEnabled=data.chatEnabled;
            window.__comfortLocalModelState=data.localModelState;
            window.__comfortMicState=data.microphone;
            window.__comfortExistingTranslations=data.existingTranslations;
            window.__comfortSettingsTranslations=data.settingsTranslations;
            window.__comfortLocales=data.locales;
            window.__comfortStyles=data.styles;
            started=true;
            initialize();
        });
        engine.on('ComfortUI_Preferences',function(json){
            if(closed || !started || !window.ComfortUI)return;
            var data=parse(json,16384);
            if(!data || !record(data.preferences) || typeof data.chatEnabled!=='boolean')return;
            ComfortUI.updatePreferences(data.preferences,true);ComfortUI.chatState(data.chatEnabled);
        });
        engine.on('ComfortUI_TranslationResult',function(json){
            if(!closed && started && window.ComfortContent)ComfortContent.receive(json);
        });
        engine.on('ComfortUI_KeyboardState',function(json){
            if(!closed && started && window.__comfortContext==='keyboard' && window.ComfortKeyboard)ComfortKeyboard.receive(json);
        });
        engine.on('ComfortUI_TranslationNotice',function(json){
            if(closed || !started || !window.ComfortUI)return;
            var data=parse(json,256);
            if(!data || typeof data.code!=='string' || !/^[A-Za-z]{1,40}$/.test(data.code) || typeof data.chatEnabled!=='boolean')return;
            ComfortUI.translationNotice(data.code,data.chatEnabled);
        });
        engine.on('ComfortUI_LocalModelState',function(json){
            if(closed || !started || !window.ComfortUI || typeof json!=='string' || json.length>256)return;
            var data;try{data=JSON.parse(json);}catch(ignored){return;}
            if(localState(data))ComfortUI.localModelState.apply(null,data);
        });
        window.addEventListener('beforeunload',function(){closed=true;});
        // Acknowledge registration; the host sends data on a subsequent update,
        // outside this native callback. No eval, Function or dynamic script text.
        ready();
    }
    window.ComfortNativeBridge={install:install};
})();
