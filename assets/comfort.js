/* Comfort UI 0.5.2. System labels are local; optional content translation runs only on request. */
(function () {
    'use strict';
    var host = window.__comfortHost || {};
    var localePacks=window.__comfortLocales || {}, commonLanguageCodes=['zh','en','ja','ko','es'], extraLanguageCodes=['zh-Hant','fr','ru','pt-BR','it','id','pl','tr'];
    var languageCodes=commonLanguageCodes.concat(extraLanguageCodes), languageNames={zh:'chinese',en:'english',ja:'japanese',ko:'korean',es:'spanish','zh-Hant':'traditionalChinese',fr:'french',ru:'russian','pt-BR':'portuguese',it:'italian',id:'indonesian',pl:'polish',tr:'turkish'}, pickerEventsReady=false;
    if (window.ComfortUI) {
        if (host.preferences) window.ComfortUI.updatePreferences(host.preferences, true);
        return;
    }
    var KEY = 'CVR_COMFORT_UI_V1';
    var defaults = { language: 'zh', largeText: false, highContrast: false, reducedMotion: false, motionStyle:'fade', motionSpeed:'normal', theme: true, cameraLayout:true, chatTranslation:true, detailTranslation:true, translationProvider:'google', proxyMode:'system', proxyAddress:'http://127.0.0.1:7890', translationKey:'F8', palette:'graphite', background:'#282c33', panel:'#343a43', accent:'#c1cbdc' };
    Object.assign(defaults,{nameplateTheme:true,chatBubbleTheme:true,sourceLanguage:'auto',aiEndpoint:'https://api.openai.com/v1',aiModel:'',localAcceleration:'cpu',hasAiKey:false});
    var localModelStatus=window.__comfortLocalModelState || ['localIdle',-1];
    var prefs = copy(defaults), root, dock, context, view = 'home', shown = true;
    var mounted = false, observer, translationTimer, currentToast, lastRenderedView;
    var styleTemplate = window.__comfortStyles || '', colourValues, diagnosticTimer, reportCount = 0;
    var nativeHome, dashboard, homeMoves=[], homeGrids=[], homeActive=false, compatibilityReady = false, staticTexts=[], paletteOpen=false, refreshTimer, layoutFailures=0;
    var customPart = 'accent', colourStep = 8;
    var segmentStates=new WeakMap(), segmentMotions=[], segmentFrame=0;
    var hudTimers=[], started=false, microphone, microphoneMove, transitionPickers=[], settingEventsReady=false,quickLabelTimer;
    var hud, hudScale, hudTextSources=new WeakMap(), hudHooks=[], hudObserver;
    var palettes = {
        graphite:{background:'#282c33',panel:'#343a43',accent:'#c1cbdc'},
        warm:{background:'#302d29',panel:'#3c3832',accent:'#dfcdb5'},
        mist:{background:'#29323a',panel:'#35424d',accent:'#bfd4e2'},
        lavender:{background:'#302c37',panel:'#3d3746',accent:'#d7c4e2'}
    };
    var textSources = new WeakMap(), dictionary = {}, reverse = {}, valueTranslations=[];
    var labels = {
        zh: {
            brand: 'COMFORT UI', home: '常用', appearance: '界面设置', original: '原版首页',
            mainTitle: '常用空间', quickTitle: '快捷菜单',
            mainIntro: '世界、好友与游玩设置，一步找到。', quickIntro: '把常用操作放在手边。',
            section: '快速前往', appearanceIntro: '两个菜单，共用一套显示偏好。',
            worlds: '探索世界', worldsDesc: '浏览世界与房间',
            friends: '好友', friendsDesc: '好友列表与在线状态',
            avatars: '虚拟形象', avatarsDesc: '浏览与切换形象',
            audio: '音频与通讯', audioDesc: '音量、麦克风与文字通讯',
            controls: '操作与交互', controlsDesc: '打开游戏交互设置',
            settings: '游戏设置', settingsDesc: '查看全部游戏设置',
            camera: '便携相机', cameraDesc: '打开或收起相机',
            emotes: '表情与动作', emotesDesc: '当前形象的表情与切换',
            quickSettings: '快捷菜单设置', quickSettingsDesc: '打开游戏快捷菜单设置',
            more: '更多原有功能', moreDesc: '通知、重生与其他操作仍在原版首页。', moreAction: '打开原版首页',
            language: '界面语言', languageHelp: '常用系统菜单与本面板切换语言。',
            text: '文字大小', textHelp: '放大常用标签，并保持按钮可点击。', standard: '标准', large: '大字',
            contrast: '对比度', contrastHelp: '提高面板边界与文字的区分度。', normalContrast: '柔和', highContrast: '高对比',
            palette:'菜单配色',paletteHelp:'选择柔和预设，或按自己的喜好调整。',graphite:'柔和石墨',warm:'暖灰',mist:'雾蓝',lavender:'灰紫',custom:'自定义',
            background:'背景',panel:'面板',accent:'强调色',colourHelp:'调整 RGB，文字明暗自动适配。',fine:'细调',fast:'快调',up:'向上',down:'向下',
            motion: '界面动画', motionHelp: '轻微淡入与平滑高亮，也可减少动画。', reduced: '减少动画', full: '流畅动画',
            theme: '游戏菜单外观', themeHelp: '原有页面也使用你选定的配色。', comfortable: '应用配色', gameTheme: '游戏配色',
            reset: '恢复本模组默认设置', close: '返回常用', applied: '已应用', saved: '已保存到本机', saving: '正在保存…',
            saveFailed: '已应用，但未能保存；重启后可能恢复。',
            unavailable: '当前页面尚未就绪，请稍后再试。', nativeError: '游戏页面暂时无法打开，请使用原有菜单入口。',
            hint: 'PC · VR', edition: '简洁柔和 · 试用版 0.5.2',
            notifications: '通知与其他操作', previewAction: '预览中已打开：',
            previewNote: '界面预览中的示例操作', chinese: '简体中文', english: 'English'
        },
        en: {
            brand: 'COMFORT UI', home: 'Quick access', appearance: 'Appearance', original: 'Game home',
            mainTitle: 'Your space', quickTitle: 'Quick Menu',
            mainIntro: 'Worlds, friends and play settings. Within reach.', quickIntro: 'Everyday actions, close at hand.',
            section: 'GO TO', appearanceIntro: 'One set of preferences for both menus.',
            worlds: 'Worlds', worldsDesc: 'Explore worlds and instances',
            friends: 'Friends', friendsDesc: 'Your friends and their status',
            avatars: 'Avatars', avatarsDesc: 'Browse and change your avatar',
            audio: 'Audio & comms', audioDesc: 'Volume, microphone and text',
            controls: 'Interactions', controlsDesc: 'Open game interaction settings',
            settings: 'Game settings', settingsDesc: 'All game settings in one place',
            camera: 'Camera', cameraDesc: 'Open or close your camera',
            emotes: 'Emotes', emotesDesc: 'Actions for your current avatar',
            quickSettings: 'Menu settings', quickSettingsDesc: 'Open the game Quick Menu settings',
            more: 'More game features', moreDesc: 'Notifications, respawn and more are on game home.', moreAction: 'Open game home',
            language: 'Interface language', languageHelp: 'Applies to this panel and common system labels.',
            text: 'Text size', textHelp: 'Larger everyday labels with accessible buttons.', standard: 'Standard', large: 'Large',
            contrast: 'Contrast', contrastHelp: 'Make text and panel edges easier to distinguish.', normalContrast: 'Soft', highContrast: 'High contrast',
            palette:'Menu palette',paletteHelp:'Choose a soft preset, or make it your own.',graphite:'Graphite',warm:'Warm grey',mist:'Mist blue',lavender:'Lavender',custom:'Custom',
            background:'Background',panel:'Panels',accent:'Accent',colourHelp:'Adjust RGB. Text adapts automatically.',fine:'Fine',fast:'Fast',up:'Scroll up',down:'Scroll down',
            motion: 'Interface motion', motionHelp: 'Subtle fades and smooth highlights, with a reduced option.', reduced: 'Reduced', full: 'Smooth',
            theme: 'Game menu colours', themeHelp: 'Use your selected palette on existing pages.', comfortable: 'Apply palette', gameTheme: 'Game colours',
            reset: 'Reset mod preferences', close: 'Back to quick access', applied: 'Applied', saved: 'Saved on this PC', saving: 'Saving…',
            saveFailed: 'Applied, but could not save. Changes may reset after restart.',
            unavailable: 'This page is not ready yet. Please try again shortly.', nativeError: 'Unable to open the game page. Please use its original menu entry.',
            hint: 'PC · VR', edition: 'Soft minimal · Preview 0.5.2',
            notifications: 'Notifications and more', previewAction: 'Preview opened: ',
            previewNote: 'Sample action in the interface preview', chinese: '简体中文', english: 'English'
        }
    };
    // Only exact common system labels are translated; user lists, names and messages are excluded.
    var common = {
        'Home':'首页','Friends':'好友','Avatars':'虚拟形象','Worlds':'世界','Props':'道具','Groups':'群组',
        'Messages':'消息','Discover':'发现','Settings':'设置','Exit':'退出','Search':'搜索','General':'常规',
        'Graphics':'图形','User Interface':'用户界面','Audio & Comms':'音频与通讯','Controls':'操作',
        'Content Filters':'内容过滤','Cache Management':'缓存管理','Interactions':'交互','Implementation':'功能集成',
        'IK & Tracking':'IK 与追踪','Experimental':'实验性','Support':'支持','Game Info':'游戏信息',
        'Instance Info':'房间信息','Activity Feed':'动态','Switch':'切换模式','Seated':'坐姿模式',
        'Flight':'飞行','Respawn':'重生','Cam On':'打开相机','Cam Off':'关闭相机','Adv Avtr':'高级虚拟形象',
        'Txt Comms':'文字通讯','Cursorlock':'锁定光标','Mirror':'镜子','Prev':'上一首','Play/Pause':'播放／暂停',
        'Fwd':'下一首','Mute':'静音','Unmute':'取消静音','Camera':'相机','Recalibrate':'重新校准',
        'Seated Play':'坐姿游玩','Advanced Avatar Settings':'高级虚拟形象设置','Go Home':'返回家园',
        'Current Instance':'当前房间','Emotes & Toggles':'表情与切换','Emotes':'表情','Prop List':'道具列表',
        'Notifications':'通知','Show All':'显示全部','Back to Main':'返回主界面','Back':'返回',
        'Previous song (External media player)':'上一首（外部播放器）',
        'Play/pause song (External media player)':'播放／暂停（外部播放器）',
        'Next song (External media player)':'下一首（外部播放器）',
        'Open the Quick Menu settings':'打开快捷菜单设置','Opens the Player List':'打开玩家列表',
        'Open the main menu exit/logout page':'打开退出／注销页面','Send a text comms message':'发送文字通讯消息'
    };
    var icons = {
        home:'<path d="m3 10 9-7 9 7v10H5V10m4 10v-7h6v7"/>',
        worlds:'<circle cx="12" cy="12" r="9"/><path d="m16 8-2.5 5.5L8 16l2.5-5.5L16 8Z"/>',
        friends:'<circle cx="9" cy="8" r="3"/><path d="M3 20v-2a6 6 0 0 1 12 0v2m2-15a3 3 0 0 1 0 6m1 3a5 5 0 0 1 3 4v2"/>',
        avatars:'<circle cx="12" cy="8" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/>',
        audio:'<path d="M4 14v-3a8 8 0 0 1 16 0v3M4 13H2v7h5v-7H4Zm16 0h2v7h-5v-7h3Z"/>',
        controls:'<path d="M7 13V5a2 2 0 0 1 4 0v7-9a2 2 0 0 1 4 0v9-5a2 2 0 0 1 4 0v7c0 4-3 7-7 7h-1c-3 0-4-2-6-4l-3-4a2 2 0 0 1 3-2l2 2"/>',
        settings:'<path d="M4 6h16M4 12h16M4 18h16"/><circle cx="9" cy="6" r="2"/><circle cx="15" cy="12" r="2"/><circle cx="8" cy="18" r="2"/>',
        camera:'<path d="M3 7h4l2-3h6l2 3h4v13H3V7Z"/><circle cx="12" cy="13" r="4"/>',
        emotes:'<circle cx="12" cy="12" r="9"/><path d="M7 14s1 4 5 4 5-4 5-4M8 8v2m8-2v2"/>',
        arrow:'<path d="M5 12h14m-5-5 5 5-5 5"/>',
        palette:'<path d="M12 3a9 9 0 0 0 0 18h2a2 2 0 0 0 1-4c-2-1 0-3 2-3h2c4-5-1-11-7-11Z"/><path d="M7 9h.01M11 6h.01M16 7h.01M6 13h.01"/>',
        back:'<path d="m10 5-7 7 7 7M3 12h18"/>',
        language:'<path d="M3 5h12M9 3v2m3 0c0 6-5 10-9 12m2-9c1 4 4 7 8 9m1 4 4-11 4 11m-7-3h6"/>'
    };
    var safeSelectors = '.toolbar .toolbar-btn,.toolbar #search-btn,#settings .filter-option,#settings .list-filter h1,#settings .option-caption,#settings .subcategory-name,#settings .subcategory-description,' +
        '#home .content-caption,#home .action-btn,#quickmenu-wrapper .quick-button .label,' +
        '#quickmenu-wrapper .container-back .content,#quickmenu-wrapper .notification-container .header-button,' +
        '#quickmenu-wrapper .notification-container .header-text,#quickmenu-wrapper .toolbar-container .button';
    var saveStatus = 'applied';

    function copy(value) { var out = {}; Object.keys(value).forEach(function (key) { out[key] = value[key]; }); return out; }
    function validate(value) {
        var next = copy(defaults);
        if (value && languageCodes.indexOf(value.language)>=0) next.language = value.language;
        ['largeText','highContrast','reducedMotion','theme','cameraLayout','nameplateTheme','chatBubbleTheme','chatTranslation','detailTranslation'].forEach(function (key) { if (value && typeof value[key] === 'boolean') next[key] = value[key]; });
        if(value && ['fade','slide','zoom','orbit'].indexOf(value.motionStyle)>=0)next.motionStyle=value.motionStyle;
        if(value && ['fast','normal','gentle'].indexOf(value.motionSpeed)>=0)next.motionSpeed=value.motionSpeed;
        if (value && (palettes[value.palette] || value.palette === 'custom')) next.palette = value.palette;
        ['background','panel','accent'].forEach(function (key) { if (value && /^#[0-9a-fA-F]{6}$/.test(value[key])) next[key] = value[key].toLowerCase(); });
        if(value && ['google','mymemory','microsoft','ai','local'].indexOf(value.translationProvider)>=0)next.translationProvider=value.translationProvider;
        if(value && value.translationProvider==='libre')next.translationProvider='local';
        if(value && ['cpu','auto'].indexOf(value.localAcceleration)>=0)next.localAcceleration=value.localAcceleration;
        if(value && ['auto'].concat(languageCodes).indexOf(value.sourceLanguage)>=0)next.sourceLanguage=value.sourceLanguage;
        if(value && validServiceEndpoint(value.aiEndpoint))next.aiEndpoint=value.aiEndpoint;
        if(value && validServiceValue(value.aiModel,120))next.aiModel=value.aiModel;
        if(value && typeof value.hasAiKey==='boolean')next.hasAiKey=value.hasAiKey;
        if(value && ['system','direct','manual'].indexOf(value.proxyMode)>=0)next.proxyMode=value.proxyMode;
        if(value && typeof value.proxyAddress==='string' && /^https?:\/\/[^\s\/@?#]+(?::\d+)?\/?$/.test(value.proxyAddress) && value.proxyAddress.length<=256)next.proxyAddress=value.proxyAddress;
        if(value && /^(?:[A-Z]|Alpha[0-9]|F(?:[1-9]|1[0-2]))$/.test(value.translationKey))next.translationKey=value.translationKey;
        return next;
    }
    function validServiceValue(value,max){return typeof value==='string' && value.length<=max && !/[\r\n\x00]/.test(value);}
    function validServiceEndpoint(value){
        if(!validServiceValue(value,512))return false;
        var link=document.createElement('a');link.href=value;
        return !/[\s?#@]/.test(value) && /^https?:\/\//.test(value) && (link.protocol==='https:' || link.protocol==='http:' && /^(localhost|127\.0\.0\.1|\[::1\])$/i.test(link.hostname));
    }
    function t(key) { return (labels[prefs.language] || labels.en)[key] || labels.en[key] || key; }
    function pack(){return localePacks[prefs.language] || {};}
    function systemText(source){return prefs.language==='zh'?(dictionary[source] || source):prefs.language==='en'?source:((pack().system || {})[source] || source);}
    function escapeText(value) { return String(value).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;'); }
    function icon(name) { return '<svg class="cu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.65" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' + (icons[name] || icons.settings) + '</svg>'; }
    function button(action, text, className) { return '<button type="button" class="cu-button ' + (className || '') + '" data-action="' + action + '">' + text + '</button>'; }
    function report(kind, details) {
        if (!host.native || !window.engine || typeof engine.call !== 'function' || reportCount >= 40) return;
        reportCount++;
        try { engine.call('ComfortUI_Report',JSON.stringify({kind:kind,context:context,details:details})); } catch (ignored) {}
    }
    function compileStyle(values) {
        // Cohtml's variable/shorthand resolution differs from Chromium. Give it literal colours.
        if (!styleTemplate) return;
        var previous=document.getElementById('comfort-theme');
        var style=document.createElement('style');
        style.id='comfort-theme';style.type='text/css';
        style.textContent=styleTemplate.replace(/var\((--[a-z-]+)\)/g,function(match,key){return values[key] || match;});
        // Populate BEFORE inserting. Updating text on an already inserted empty style is ignored by this Cohtml build.
        (document.head||document.documentElement).appendChild(style);
        if(previous && previous.parentNode)previous.parentNode.removeChild(previous);
    }
    function setAttributes() {
        var html = document.documentElement;
        html.setAttribute('data-comfort-enabled', shown ? 'true' : 'false');
        html.setAttribute('data-comfort-context', context || detectContext());
        html.setAttribute('data-comfort-theme', 'dark');
        html.setAttribute('data-comfort-text', prefs.largeText ? 'large' : 'standard');
        html.setAttribute('data-comfort-contrast', prefs.highContrast ? 'high' : 'normal');
        html.setAttribute('data-comfort-motion', prefs.reducedMotion ? 'reduced' : 'full');
        html.setAttribute('data-comfort-animation',prefs.motionStyle);
        html.setAttribute('data-comfort-speed',prefs.motionSpeed);
        // Cohtml does not consistently reflect assignments to the HTML lang property.
        // Use real attributes, as with the other runtime stylesheet switches.
        html.setAttribute('lang',prefs.language === 'zh' ? 'zh-CN' : prefs.language);
        html.setAttribute('data-comfort-language',prefs.language);
        var face=prefs.language==='ko'?'"Noto Sans KR","Malgun Gothic","Noto Sans",sans-serif':prefs.language==='ja'?'"Noto Sans JP","Yu Gothic","Noto Sans",sans-serif':prefs.language==='zh-Hant'?'"Noto Sans TC","Microsoft JhengHei","Noto Sans SC",sans-serif':prefs.language==='zh'?'"Noto Sans SC","Microsoft YaHei","Noto Sans",sans-serif':'"Noto Sans","Segoe UI","Noto Sans SC","Noto Sans JP","Noto Sans KR",sans-serif';
        html.style.setProperty('--cu-font',face);html.style.setProperty('--standard-font-family',face);
        if(prefs.reducedMotion || !shown)finishSegmentMotions();
        applyPalette();
    }
    function rgb(hex) { return [parseInt(hex.substr(1,2),16),parseInt(hex.substr(3,2),16),parseInt(hex.substr(5,2),16)]; }
    function hex(rgbValue) { return '#' + rgbValue.map(function (v) { return ('0' + Math.max(0,Math.min(255,Math.round(v))).toString(16)).slice(-2); }).join(''); }
    function mix(a,b,amount) { var x=rgb(a),y=rgb(b); return hex(x.map(function(v,i){return v+(y[i]-v)*amount;})); }
    function luminance(colour) { var values=rgb(colour).map(function(v){v/=255;return v<=0.04045?v/12.92:Math.pow((v+0.055)/1.055,2.4);});return values[0]*0.2126+values[1]*0.7152+values[2]*0.0722; }
    function ratio(a,b) { var x=luminance(a),y=luminance(b);return (Math.max(x,y)+0.05)/(Math.min(x,y)+0.05); }
    function readable(colour) { return ratio(colour,'#fafbfc')>=ratio(colour,'#12161b')?'#fafbfc':'#12161b'; }
    function muted(colour,text) { var result=mix(colour,text,0.73);return ratio(result,colour)>=4.5?result:text; }
    function hudUnit(){
        var style=document.documentElement.style,scale=hudScale?hudScale.scale:Number(style.getPropertyValue('--ui-scale'))||1,fill=hudScale?hudScale.fill:Number(style.getPropertyValue('--fill-y'))||1;
        return window.innerHeight*0.01*Math.max(0.01,scale)/Math.max(0.01,fill);
    }
    function applyPalette() {
        var colours=palettes[prefs.palette] || prefs, bg=colours.background, panel=colours.panel, accent=colours.accent;
        var unit=context==='hud'?hudUnit():0;
        var inputKey=[bg,panel,accent,prefs.highContrast,unit,prefs.language].join(':');
        if(applyPalette.inputKey===inputKey && document.getElementById('comfort-theme'))return;
        applyPalette.inputKey=inputKey;
        var text=readable(bg), panelText=readable(panel), line=mix(bg,text,prefs.highContrast?0.6:0.28);
        var values={
            '--cu-bg':bg,'--cu-panel':panel,'--cu-hover':mix(panel,panelText,0.12),'--cu-line':line,'--cu-text':text,
            '--cu-muted':muted(bg,text),'--cu-panel-text':panelText,'--cu-panel-muted':muted(panel,panelText),
            '--cu-accent':accent,'--cu-heading-accent':ratio(accent,bg)>=3?accent:text,'--cu-ink':readable(accent),'--cu-icon-bg':mix(panel,accent,0.15),
            '--cu-icon-colour':ratio(accent,mix(panel,accent,0.15))>=3?accent:panelText,'--cu-native-accent':mix(panel,accent,0.35),
            '--cu-danger':mix(panel,'#d99292',0.32),'--cu-danger-text':readable(mix(panel,'#d99292',0.32)),
            '--cu-positive':mix(panel,'#a6c8b4',0.32),'--cu-positive-text':readable(mix(panel,'#a6c8b4',0.32))
        };
        values['--cu-hud-speaking']=ratio('#9bdfc6',bg)>=3?'#9bdfc6':'#136547';
        values['--cu-font']=document.documentElement.style.getPropertyValue('--cu-font');
        values['--cu-hud-speaking-bg']=mix(bg,values['--cu-hud-speaking'],0.15);
        if(unit){var metrics={max:22,bottom:1.2,padding:1.1,small:1.8,title:2.7,body:2.1,wrapper:18,label:1.45,glyph:5,stroke:0.24,meter:0.36};Object.keys(metrics).forEach(function(key){values['--cu-hud-'+key]=(metrics[key]*unit).toFixed(2)+'px';});}
        colourValues=values;
        Object.keys(values).forEach(function(key){document.documentElement.style.setProperty(key,values[key]);});
        var paletteKey=JSON.stringify(values);
        if(applyPalette.lastKey!==paletteKey || !document.getElementById('comfort-theme')){compileStyle(values);applyPalette.lastKey=paletteKey;}
        // These basic properties also protect the panel while a stylesheet is being resolved.
        if (root) { root.style.backgroundColor=bg;root.style.color=text;root.style.borderColor=line; }
        document.documentElement.setAttribute('data-comfort-tone',luminance(panel)>0.35?'light':'dark');
    }
    function localSave() {
        try {
            localStorage.setItem(KEY, JSON.stringify(prefs));
            localStorage.setItem('CVR_UI_LANG', prefs.language);
        } catch (e) { /* native persistence remains available if Cohtml localStorage is unavailable */ }
    }
    function updateSaveState(){
        if(!root)return;
        var state=root.querySelector('.cu-save-state'),value=t(saveStatus);
        if(state && state.textContent!==value)state.textContent=value;
    }
    function setPreferences(next, remote) {
        next=validate(next);
        var previous=prefs,changed=Object.keys(next).some(function(key){return next[key]!==previous[key];});
        if(!changed){if(remote){saveStatus='saved';updateSaveState();scheduleDiagnostics();}return;}
        if(previous.language!==next.language){var open=document.querySelectorAll('.cu-picker-open');for(var i=0;i<open.length;i++)closePicker(open[i],false);}
        prefs=next;localSave();setAttributes();
        if(remote)saveStatus='saved';
        else {
            saveStatus=host.native?'saving':'saved';
            if(host.native && window.engine && typeof engine.call==='function') {
                try {
                    var call=engine.call('ComfortUI_Save',JSON.stringify(prefs));
                    if(call && typeof call.catch==='function')call.catch(function(){saveStatus='saveFailed';updateSaveState();});
                }catch(e){saveStatus='saveFailed';}
            }
        }
        // Only a language change needs to revisit all native system text.
        if(previous.language!==prefs.language){translateCommon();decorateNativeSettings();}
        else if(window.ComfortContent)window.ComfortContent.refresh();
        render();
        if(previous.largeText!==prefs.largeText || previous.language!==prefs.language)scheduleDiagnostics();
    }
    function rebuildDictionary() {
        dictionary = {};
        var supplied = window.__comfortExistingTranslations || {};
        Object.keys(supplied).forEach(function (key) { if (typeof supplied[key] === 'string') dictionary[key] = supplied[key]; });
        Object.keys(common).forEach(function (key) { dictionary[key] = common[key]; });
        var ownSettings = window.__comfortSettingsTranslations || {};
        Object.keys(ownSettings).forEach(function (key) { if (typeof ownSettings[key] === 'string') dictionary[key] = ownSettings[key]; });
        Object.keys((localePacks.zh || {}).system || {}).forEach(function(key){dictionary[key]=localePacks.zh.system[key];});
        reverse = {};
        valueTranslations=[];
        function normalize(value){return value.replace(/\s+/g,' ').trim();}
        function alias(value,source){var key=normalize(value);if(!Object.prototype.hasOwnProperty.call(reverse,key))reverse[key]=source;}
        // English keys take priority over translated aliases. A live node retains its exact source.
        Object.keys(dictionary).forEach(function(key){reverse[normalize(key)]=key;});
        Object.keys(supplied).forEach(function(key){if(typeof supplied[key]==='string')alias(supplied[key],key);});
        Object.keys(dictionary).forEach(function(key){alias(dictionary[key],key);});
        Object.keys(localePacks).forEach(function(code){var words=localePacks[code].system || {};Object.keys(words).forEach(function(key){alias(words[key],key);});});
        function valuePattern(text){return new RegExp('^'+normalize(text).split('{value}').map(function(part){return part.replace(/[.*+?^${}()|[\]\\]/g,'\\$&');}).join('(.+?)')+'$');}
        Object.keys(dictionary).forEach(function(key){
            if(key.indexOf('{value}')<0)return;
            [key,dictionary[key]].forEach(function(value){valueTranslations.push({source:key,pattern:valuePattern(value)});});
            Object.keys(localePacks).forEach(function(code){var value=(localePacks[code].system || {})[key];if(value)valueTranslations.push({source:key,pattern:valuePattern(value)});});
        });
    }
    function sourceText(value){
        if(Object.prototype.hasOwnProperty.call(dictionary,value))return value;
        return reverse[value.replace(/\s+/g,' ').trim()];
    }
    function translateNode(node) {
        var raw=node.nodeValue,trimmed=raw && raw.trim();
        if(!trimmed)return;
        var record=textSources.get(node);
        if(!record || trimmed!==record.applied) {
            var source=sourceText(trimmed);
            record=source?{source:source}:null;
            if(!record)for(var i=0;i<valueTranslations.length;i++) {
                var pattern=valueTranslations[i],match=pattern.pattern.exec(trimmed.replace(/\s+/g,' '));
                if(match){record={source:pattern.source,value:match[1]};break;}
            }
            if(!record)return;
            textSources.set(node,record);
        }
        var desired=systemText(record.source).trim();
        if(record.value!==undefined)desired=desired.replace('{value}',record.value);
        record.applied=desired;
        var replaced=raw.replace(trimmed,desired);
        if(raw!==replaced)node.nodeValue=replaced;
    }
    function settingRow(key, help, pref, choices) {
        var options = choices.map(function (choice) {
            var selected = String(prefs[pref]) === String(choice[0]);
            return '<button type="button" class="cu-choice' + (selected ? ' cu-selected' : '') + '" data-pref="' + pref + '" data-value="' + choice[0] + '" aria-pressed="' + selected + '">' + t(choice[1]) + '</button>';
        }).join('');
        return '<div class="cu-setting" data-setting="'+pref+'"><div class="cu-setting-copy"><div class="cu-setting-title">' + t(key) + '</div><div class="cu-setting-help">' + t(help) + '</div></div><div class="cu-choices" role="group" aria-label="' + t(key) + '">' + options + '</div></div>';
    }
    function languageChoices(codes){return codes.map(function(code){return [code,languageNames[code]];});}
    function choicePicker(pref,choices,placeholder){
        var id='comfort-'+context+'-'+pref+'-options';
        // The game's capture-phase ScrollHandler prevents default wheel scrolling and
        // resolves only these two classes. The nearest list must own both wheel and drag.
        return '<div class="cu-picker" data-picker="'+pref+'" data-placeholder-key="'+(placeholder || '')+'"><button type="button" class="cu-picker-toggle" data-action="select:'+pref+'" aria-haspopup="listbox" aria-expanded="false" aria-controls="'+id+'"><span class="cu-picker-value"></span><span class="cu-picker-chevron" aria-hidden="true"></span></button><div class="cu-picker-list list-content scroll-content" nohorizontal="true" data-content-type="comfort-options" id="'+id+'" role="listbox" style="display:none">'+choices.map(function(choice){return '<button type="button" class="cu-picker-option" role="option" data-pref="'+pref+'" data-value="'+choice[0]+'" data-label-key="'+choice[1]+'" data-language-code="'+choice[0]+'" aria-selected="false" tabindex="-1">'+t(choice[1])+'</button>';}).join('')+'</div><div class="cu-picker-nav no-scroll" style="display:none"><button type="button" data-picker-step="-1"><span class="cu-picker-up" aria-hidden="true"></span><span class="cu-picker-nav-label"></span></button><button type="button" data-picker-step="1"><span class="cu-picker-down" aria-hidden="true"></span><span class="cu-picker-nav-label"></span></button></div></div>';
    }
    function pickerRow(key,help,pref,choices){return '<div class="cu-setting cu-picker-setting" data-setting="'+pref+'"><div class="cu-setting-copy"><div class="cu-setting-title">'+t(key)+'</div><div class="cu-setting-help">'+t(help)+'</div></div>'+choicePicker(pref,choices)+'</div>';}
    function languageSettingRow(){var row=settingRow('language','languageHelp','language',languageChoices(commonLanguageCodes));return row.slice(0,-6)+choicePicker('language',languageChoices(extraLanguageCodes),'moreLanguages')+'</div>';}
    function closePicker(picker,focus){
        picker.classList.remove('cu-picker-open');picker.querySelector('.cu-picker-list').style.display='none';picker.querySelector('.cu-picker-nav').style.display='none';
        var toggle=picker.querySelector('.cu-picker-toggle');toggle.setAttribute('aria-expanded','false');if(focus)toggle.focus();
    }
    function openPicker(picker){
        var open=document.querySelectorAll('.cu-picker-open');for(var i=0;i<open.length;i++)if(open[i]!==picker)closePicker(open[i],false);
        picker.classList.add('cu-picker-open');picker.querySelector('.cu-picker-list').style.display='block';picker.querySelector('.cu-picker-nav').style.display='flex';picker.querySelector('.cu-picker-toggle').setAttribute('aria-expanded','true');
        var selected=picker.querySelector('.cu-picker-option[aria-selected="true"]');
        if(selected)revealPickerOption(picker,selected);else picker.querySelector('.cu-picker-list').scrollTop=0;
        syncPickerScroll(picker);
    }
    function syncPickerScroll(picker){
        var list=picker.querySelector('.cu-picker-list'),buttons=picker.querySelectorAll('[data-picker-step]');
        if(!list.clientHeight)return;
        buttons[0].disabled=list.scrollTop<=1;buttons[1].disabled=list.scrollTop+list.clientHeight>=list.scrollHeight-1;
    }
    function revealPickerOption(picker,option){
        var list=picker.querySelector('.cu-picker-list'),bounds=list.getBoundingClientRect(),item=option.getBoundingClientRect();
        if(item.top<bounds.top+8)list.scrollTop-=bounds.top+8-item.top;
        else if(item.bottom>bounds.bottom-8)list.scrollTop+=item.bottom-bounds.bottom+8;
    }
    function syncPicker(picker){
        var pref=picker.getAttribute('data-picker'),options=picker.querySelectorAll('.cu-picker-option'),selected=null;
        for(var i=0;i<options.length;i++){
            var active=options[i].getAttribute('data-value')===prefs[pref];
            options[i].setAttribute('aria-selected',String(active));options[i].classList.toggle('cu-selected',active);options[i].textContent=t(options[i].getAttribute('data-label-key'));if(active)selected=options[i];
        }
        var value=picker.querySelector('.cu-picker-value'),toggle=picker.querySelector('.cu-picker-toggle');
        value.textContent=selected?selected.textContent:t(picker.getAttribute('data-placeholder-key'));
        value.setAttribute('data-language-code',selected?prefs[pref]:'');toggle.classList.toggle('cu-picker-selected',!!selected);
        picker.querySelector('.cu-picker-list').setAttribute('aria-label',t(pref==='language'?'moreLanguages':'sourceLanguage'));
        toggle.setAttribute('aria-label',t(pref==='language'?'moreLanguages':'sourceLanguage')+': '+value.textContent);
        var pageButtons=picker.querySelectorAll('[data-picker-step]');
        for(var b=0;b<pageButtons.length;b++){pageButtons[b].querySelector('.cu-picker-nav-label').textContent=t(b?'down':'up');pageButtons[b].setAttribute('aria-label',t(b?'down':'up'));}
        if(picker.__comfortPicker)return;picker.__comfortPicker=true;
        picker.querySelector('.cu-picker-list').addEventListener('scroll',function(){syncPickerScroll(picker);});
        for(var p=0;p<pageButtons.length;p++)pageButtons[p].addEventListener('click',function(event){
            event.preventDefault();event.stopPropagation();var list=picker.querySelector('.cu-picker-list');
            list.scrollTop+=Number(this.getAttribute('data-picker-step'))*Math.max(76,list.clientHeight*.8);syncPickerScroll(picker);
        });
        picker.addEventListener('keydown',function(event){
            var key=event.key || event.keyCode,isOpen=picker.classList.contains('cu-picker-open');
            if(key==='Escape'||key===27){if(isOpen){event.preventDefault();event.stopPropagation();closePicker(picker,true);}return;}
            if(key==='Tab'||key===9){closePicker(picker,false);return;}
            var down=key==='ArrowDown'||key===40,up=key==='ArrowUp'||key===38,home=key==='Home'||key===36,end=key==='End'||key===35;
            if(!down&&!up&&!home&&!end)return;
            event.preventDefault();event.stopPropagation();var index=Array.prototype.indexOf.call(options,document.activeElement);
            if(!isOpen){openPicker(picker);index=-1;for(var n=0;n<options.length;n++)if(options[n].getAttribute('aria-selected')==='true')index=n;if(index<0)index=up?options.length-1:0;}
            else index=home?0:end?options.length-1:Math.max(0,Math.min(options.length-1,index+(down?1:-1)));
            options[index].focus();revealPickerOption(picker,options[index]);syncPickerScroll(picker);
        });
        if(!pickerEventsReady){pickerEventsReady=true;document.addEventListener('mousedown',function(event){var open=document.querySelectorAll('.cu-picker-open');for(var n=0;n<open.length;n++)if(!open[n].contains(event.target))closePicker(open[n],false);});}
    }
    // Keep one thumb alive across renders. Only active controls use a shared frame;
    // no layout reads or polling are needed to slide, retarget or settle it.
    function segmentTime(){return window.performance && typeof performance.now==='function'?performance.now():Date.now();}
    function paintSegment(record,position){
        record.position=position;
        record.thumb.style.transform='translateX('+(position*100).toFixed(4)+'%)';
    }
    function sampleSegment(record,time){
        var progress=Math.max(0,Math.min(1,(time-record.start)/record.duration));
        paintSegment(record,record.from+(record.target-record.from)*(1-Math.pow(1-progress,3)));
        return progress===1;
    }
    function stopSegment(record){
        var index=segmentMotions.indexOf(record);
        if(index!==-1)segmentMotions.splice(index,1);
        record.running=false;
        if(!segmentMotions.length && segmentFrame){window.cancelAnimationFrame(segmentFrame);segmentFrame=0;}
    }
    function stepSegments(){
        segmentFrame=0;
        var time=segmentTime();
        for(var i=segmentMotions.length-1;i>=0;i--){
            var record=segmentMotions[i];
            if(!document.documentElement.contains(record.bar) || !record.bar.contains(record.thumb))stopSegment(record);
            else if(sampleSegment(record,time))stopSegment(record);
        }
        if(segmentMotions.length)segmentFrame=window.requestAnimationFrame(stepSegments);
    }
    function finishSegmentMotions(){
        if(segmentFrame)window.cancelAnimationFrame(segmentFrame);
        segmentFrame=0;
        for(var i=0;i<segmentMotions.length;i++){paintSegment(segmentMotions[i],segmentMotions[i].target);segmentMotions[i].running=false;}
        segmentMotions.length=0;
    }
    function syncSegmentedBar(bar,count,index){
        if(!bar)return;
        var record=segmentStates.get(bar);
        if(count<2 || index<0){
            if(record){stopSegment(record);if(record.thumb.parentNode)record.thumb.parentNode.removeChild(record.thumb);segmentStates.delete(bar);}
            bar.classList.remove('cu-segmented');return;
        }
        if(!record || !bar.contains(record.thumb) || record.count!==count){
            if(record){stopSegment(record);if(record.thumb.parentNode)record.thumb.parentNode.removeChild(record.thumb);}
            var thumb=document.createElement('span');thumb.className='cu-segment-thumb'+(bar.classList.contains('cu-language-slider')?' cu-language-thumb':'');
            thumb.setAttribute('aria-hidden','true');thumb.innerHTML='<span></span>';thumb.style.width=(100/count)+'%';
            bar.insertBefore(thumb,bar.firstChild);bar.classList.add('cu-segmented');
            record={bar:bar,thumb:thumb,count:count,target:index,position:index,running:false};segmentStates.set(bar,record);paintSegment(record,index);return;
        }
        if(record.target===index)return;
        record.target=index;
        if(prefs.reducedMotion || !shown || typeof window.requestAnimationFrame!=='function'){
            stopSegment(record);paintSegment(record,index);return;
        }
        record.from=record.position;record.start=segmentTime();record.duration=prefs.motionSpeed==='fast'?160:prefs.motionSpeed==='gentle'?320:220;
        if(!record.running){record.running=true;segmentMotions.push(record);}
        if(!segmentFrame)segmentFrame=window.requestAnimationFrame(stepSegments);
    }
    function syncOwnSegments(container){
        var rows=container.querySelectorAll('.cu-setting[data-setting]');
        for(var r=0;r<rows.length;r++){
            var pref=rows[r].getAttribute('data-setting');if(pref==='language')continue;
            var bar=rows[r].querySelector('.cu-choices');if(!bar)continue;
            var buttons=bar.querySelectorAll('[data-pref]'),index=-1;
            for(var i=0;i<buttons.length;i++){
                var selected=String(prefs[pref])===buttons[i].getAttribute('data-value');
                buttons[i].classList.toggle('cu-selected',selected);buttons[i].setAttribute('aria-pressed',selected?'true':'false');if(selected)index=i;
            }
            syncSegmentedBar(bar,buttons.length,index);
        }
        var pickers=container.querySelectorAll('.cu-picker');for(var p=0;p<pickers.length;p++)syncPicker(pickers[p]);
    }
    function updateLanguageSlider() {
        var row=root.querySelector('[data-setting="language"]'), bar=row.querySelector('.cu-choices');
        row.querySelector('.cu-setting-title').textContent=t('language');
        row.querySelector('.cu-setting-help').textContent=t('languageHelp');
        bar.classList.add('cu-language-slider');bar.setAttribute('aria-label',t('language'));
        var selected=commonLanguageCodes.indexOf(prefs.language);
        syncSegmentedBar(bar,commonLanguageCodes.length,selected);
        syncPicker(row.querySelector('.cu-picker'));
        var buttons=bar.querySelectorAll('[data-pref="language"]');
        for(var i=0;i<buttons.length;i++){
            buttons[i].classList.toggle('cu-selected',i===selected);buttons[i].setAttribute('aria-pressed',i===selected?'true':'false');buttons[i].setAttribute('tabindex',i===(selected<0?0:selected)?'0':'-1');
        }
        if(bar.__comfortLanguage)return;
        bar.__comfortLanguage=true;
        function select(index){var next=copy(prefs);next.language=commonLanguageCodes[index];if(next.language!==prefs.language)setPreferences(next,false);}
        bar.addEventListener('keydown',function(event){
            var key=event.key || event.keyCode,index=commonLanguageCodes.indexOf(prefs.language);
            if(key==='ArrowLeft'||key===37)index=Math.max(0,index-1);
            else if(key==='ArrowRight'||key===39)index=Math.min(4,index+1);
            else if(key==='Home'||key===36)index=0;
            else if(key==='End'||key===35)index=4;
            else return;
            event.preventDefault();event.stopPropagation();select(index);buttons[index].focus();
        });
        var drag;
        bar.addEventListener('mousedown',function(event){
            if(event.button!==0)return;
            var bounds=bar.getBoundingClientRect();drag={start:event.clientX,left:bounds.left,width:bounds.width,index:commonLanguageCodes.indexOf(prefs.language),moved:false};
        });
        window.addEventListener('mousemove',function(event){
            if(!drag)return;
            if(Math.abs(event.clientX-drag.start)>8)drag.moved=true;
            if(!drag.moved)return;
            var index=Math.max(0,Math.min(4,Math.floor((event.clientX-drag.left)*5/drag.width)));
            if(index!==drag.index){drag.index=index;syncSegmentedBar(bar,commonLanguageCodes.length,index);}
        });
        window.addEventListener('mouseup',function(){
            if(!drag)return;
            var finished=drag;drag=null;
            if(finished.moved){bar.__skipClick=true;select(finished.index);syncSegmentedBar(bar,commonLanguageCodes.length,commonLanguageCodes.indexOf(prefs.language));setTimeout(function(){bar.__skipClick=false;},0);}
        });
        window.addEventListener('blur',function(){drag=null;syncSegmentedBar(bar,commonLanguageCodes.length,commonLanguageCodes.indexOf(prefs.language));});
    }
    function colourEditor() {
        var current=palettes[prefs.palette] || prefs;
        var choices=['graphite','warm','mist','lavender','custom'].map(function(name){
            var selected=prefs.palette===name,colour=palettes[name] || prefs;
            return '<button type="button" class="cu-palette'+(selected?' cu-selected':'')+'" data-pref="palette" data-value="'+name+'" aria-pressed="'+selected+'"><span class="cu-palette-swatch" style="background-color:'+colour.background+';border-color:'+colour.accent+'"><span style="background-color:'+colour.panel+'"></span></span><span>'+t(name)+'</span></button>';
        }).join('');
        var parts=['background','panel','accent'].map(function(part){return button('colourpart:'+part,t(part),'cu-choice'+(customPart===part?' cu-selected':''));}).join('');
        var channels=rgb(current[customPart]);
        var rows=channels.map(function(value,index){return '<div class="cu-rgb-row"><span class="cu-rgb-name">'+['R','G','B'][index]+'</span>'+button('colour:'+index+':minus','−','cu-rgb-button')+'<span class="cu-rgb-value">'+value+'</span>'+button('colour:'+index+':plus','+','cu-rgb-button')+'<span class="cu-rgb-track"><span style="width:'+(value/255*100)+'%;background-color:'+['#d8abab','#b7cead','#b3c4de'][index]+'"></span></span></div>';}).join('');
        return '<div class="cu-palette-section"><div class="cu-setting-title">'+t('palette')+'</div><p class="cu-setting-help">'+t('paletteHelp')+'</p><div class="cu-palettes" role="group" aria-label="'+t('palette')+'">'+choices+'</div><div class="cu-custom-colours"><div class="cu-colour-parts">'+parts+'</div><div class="cu-colour-editor"><div class="cu-colour-preview" style="background-color:'+current[customPart]+';color:'+readable(current[customPart])+'"><strong>'+current[customPart].toUpperCase()+'</strong><span>'+t(customPart)+'</span></div><div class="cu-rgb">'+rows+'</div></div><div class="cu-colour-bottom"><span>'+t('colourHelp')+'</span><div class="cu-step">'+button('step:1',t('fine'),'cu-step-button'+(colourStep===1?' cu-selected':''))+button('step:8',t('fast'),'cu-step-button'+(colourStep===8?' cu-selected':''))+'</div></div></div></div>';
    }

    // System text only: login fields, typed text, content names and notifications are excluded.
    var excludedText='script,style,textarea,input,#comfort-ui,#comfort-translation,#comfort-keybinding,[data-comfort-translation-category],#comfort-dock,.cu-dashboard-heading,[data-comfort-system-label],#avatars .filter-content .filter-option:not([data-comfort-category-label]),#friends .filter-content .filter-option:not([data-comfort-category-label]),#worlds .filter-content .filter-option:not([data-comfort-category-label]),#props .filter-content .filter-option:not([data-comfort-category-label]),.activity-content,.profile-username,#profile-title,#group-name-home,.content-name,.game-greeting,.game-version,.hud-message,.prop-name,.input_mirror,.inputLineBlock,.textarea-title,.textarea-info,.notification .header,.notification .content,.notifier-root';
    safeSelectors='.toolbar .toolbar-btn,.toolbar #search-btn,#settings .filter-option,#settings .option-caption,#settings .subcategory-name,#settings .subcategory-description,.list-filter h1,.content-btn,.action-btn,.close-btn,.modal-button,.modal-base h2,.modal-base h3,.content-caption,.user-toolbar .button,#quickmenu-wrapper .quick-button .label,#quickmenu-wrapper .quick-button .inner-toggle,#quickmenu-wrapper .container-back .content,#quickmenu-wrapper .notification-container .header-button,#quickmenu-wrapper .notification-container .header-text,#quickmenu-wrapper .toolbar-container .button,#CVRUI-QMUI-SettingsPage .text-sm,#CVRUI-QMUI-SettingsPage .text-title,#CVRUI-QMUI-SettingsPage .category-header .header,.keyboard-func,.keyboard-mod,.keyboard .settings h2,.keyboard .settings h3,.login-block label,.login-block .button,.login-block .button-no-bg,.login-block .input-button,.login-block h1,.login-block h2,.login-block h3,.icon-block .exit-button';
    function addLabels(language, values) { if(!labels[language])labels[language]={};Object.keys(values).forEach(function(key){labels[language][key]=values[key];}); }
    addLabels('zh',{
        home:'首页',appearance:'界面与显示',appearanceIntro:'舒适的配色、清晰的文字，按你的习惯调整。',
        languageHelp:'主菜单、快捷菜单、登录与键盘共用此设置。',
        motionHelp:'短暂淡入和平滑高亮；关闭后立即切换，HUD 提示仍会正常收起。',
        reduced:'关闭动效',full:'舒适动效',original:'显示原版界面',resume:'返回新版界面',
        edition:'Comfort UI 0.5.2',paletteOpen:'调整菜单配色',paletteClose:'收起配色选项',
        mainTitle:'你的空间',quickTitle:'快捷操作',mainIntro:'个人信息、动态和游玩操作，清晰分区。',
        reset:'恢复界面默认设置',originalHelp:'临时切回原版；屏幕上会保留返回新版的按钮。',
        textHelp:'调整系统菜单文字；列表和设置可滚动。',saved:'已保存',
        appearanceEntry:'语言、动效与配色',interfaceSettings:'界面设置',playActions:'游玩操作',contentActions:'内容与形象',restoreHint:'设置 → 界面与显示',quickIntro:'常用操作与通知，一眼找到。'
    });
    addLabels('en',{
        home:'Home',appearance:'Interface & display',appearanceIntro:'A quieter palette and clear controls, tailored to you.',
        languageHelp:'Shared by menus, login and keyboard.',
        motionHelp:'Brief fades and smooth highlights. Off switches instantly; HUD notices still dismiss.',
        reduced:'Off',full:'Comfort motion',original:'Show original UI',resume:'Return to Comfort UI',
        edition:'Comfort UI 0.5.2',paletteOpen:'Adjust menu colours',paletteClose:'Hide colour options',
        mainTitle:'Your space',quickTitle:'Quick actions',mainIntro:'Your profile, activity and play controls, clearly arranged.',
        reset:'Reset interface preferences',originalHelp:'The return button stays available while the original UI is shown.',
        textHelp:'Adjust system labels. Lists and settings remain scrollable.',saved:'Saved',
        appearanceEntry:'Language, motion & colours',interfaceSettings:'Interface settings',playActions:'Play controls',contentActions:'Content & avatars',restoreHint:'Settings → Interface & display',quickIntro:'Everyday actions and notifications, at a glance.'
    });
    addLabels('zh',{motionStyle:'动效形式',motionStyleHelp:'上浮和缩放幅度轻微，淡出保持平稳。',motionFade:'柔和淡入',motionSlide:'轻柔上浮',motionZoom:'微缩显现',motionSpeed:'动效速度',motionSpeedHelp:'选择快速、标准或更舒缓的切换节奏；关闭动效时均不播放。',motionFast:'快速',motionNormal:'标准',motionGentle:'舒缓'});
    addLabels('en',{motionStyle:'Motion style',motionStyleHelp:'Small slide and scale effects, with a steady fade out.',motionFade:'Soft fade',motionSlide:'Gentle rise',motionZoom:'Subtle zoom',motionSpeed:'Motion speed',motionSpeedHelp:'Choose a quick, standard or relaxed pace. Off disables all styles.',motionFast:'Quick',motionNormal:'Standard',motionGentle:'Relaxed'});
    addLabels('zh',{cameraLayout:'相机界面',cameraLayoutHelp:'变焦、对焦与防抖放在外面，其余功能按用途分组；也可恢复原版相机排版。',cameraUnified:'统一布局',cameraOriginal:'原版布局'});
    addLabels('en',{cameraLayout:'Camera layout',cameraLayoutHelp:'Zoom, focus and stabilization stay outside. Other tools are grouped by use. The original layout remains available.',cameraUnified:'Unified',cameraOriginal:'Original'});
    addLabels('zh',{chatBubbleStyle:'聊天气泡外观',chatBubbleHelp:'圆角气泡与三点输入提示，向下箭头指向玩家；保留原版透明度与显示时长。',chatBubbleUnified:'新版气泡',chatBubbleOriginal:'原版气泡'});
    addLabels('en',{chatBubbleStyle:'Chat bubble style',chatBubbleHelp:'Rounded messages and three-dot typing hints point down towards the player; native opacity and lifetime are retained.',chatBubbleUnified:'Comfort bubbles',chatBubbleOriginal:'Original bubbles'});
    addLabels('zh',{moreLanguages:'更多语言'});addLabels('en',{moreLanguages:'More languages'});
    addLabels('ja',{moreLanguages:'その他の言語'});addLabels('ko',{moreLanguages:'다른 언어'});addLabels('es',{moreLanguages:'Más idiomas'});
    languageCodes.forEach(function(code){
        if(!labels[code])labels[code]=Object.assign({},labels.en);
        Object.assign(labels[code],(localePacks[code] || {}).labels || {});
        Object.assign(labels[code],{chinese:'简体中文',english:'English',japanese:'日本語',korean:'한국어',spanish:'Español',traditionalChinese:'繁體中文',french:'Français',russian:'Русский',portuguese:'Português (Brasil)',italian:'Italiano',indonesian:'Bahasa Indonesia',polish:'Polski',turkish:'Türkçe'});
    });
    var extra={
        'User Interface':'界面与显示','General Menu Settings':'其他界面设置','Quick Menu Settings':'快捷设置',
        'Welcome to ChilloutVR!':'欢迎来到 ChilloutVR','Email Address':'电子邮箱','Password':'密码','Paste':'粘贴',
        'Forgot Password?':'忘记密码？','Sign in':'登录','Create Account':'创建账号','Login with Steam':'通过 Steam 登录',
        'Login with Meta':'通过 Meta 登录','Logging in...':'正在登录…','Return to Login':'返回登录',
        'Create ChilloutVR Account':'创建 ChilloutVR 账号','Create Account using Steam':'通过 Steam 创建账号',
        'Create Account using Meta':'通过 Meta 创建账号','Privacy Policy':'隐私政策','Terms of Service':'服务条款',
        'Behaviour Guidelines':'行为准则','Open Terms of Service':'查看服务条款','Accept':'接受','Reject':'拒绝',
        'The Terms of Service have updated!':'服务条款已更新',
        "Before you're able to log in you must accept the latest Terms of Service!":'请先阅读并接受最新服务条款，再继续登录。',
        'No account was found, you can choose an option to create an account below!':'未找到账号，请选择下方方式创建账号。',
        'Keyboard Settings':'键盘设置','Default Collapsed on Desktop':'桌面模式默认收起键盘',
        'Keyboard Movable in VR':'VR 中允许移动键盘','Keyboard Transparency':'键盘透明度',
        'Backspace':'退格','Clear':'清空','Copy':'复制','Submit':'提交','New line':'换行',
        'Go Home':'返回家园','Current Instance':'当前房间','Adv Avtr':'形象参数','Cam On':'打开相机','Cam Off':'收起相机',
        'Activity Feed':'动态','Game Info':'运行信息','Instance Info':'房间信息','Refresh':'刷新','Back':'返回',
        'Add Category':'添加分类','Remove Category':'删除分类','Groups':'群组','Badges':'徽章',
        'Remove All Avatars':'隐藏全部形象','Remove All Props':'移除全部道具','Recalibrate':'重新校准',
        'Invites':'邀请','Invite Requests':'邀请请求','Friend Requests':'好友请求','Group Messages':'群组消息',
        'System Notifications':'系统通知','Other Messages':'其他消息','Silenced Users':'已屏蔽通讯的用户',
        'Create':'创建','Cancel':'取消','Apply':'应用','Save':'保存','Delete':'删除','Favorite':'收藏',
        'Join':'加入','Drop':'放置','Drop Prop':'放置道具','Drop Portal':'放置传送门','Request Invite':'请求邀请','Invite':'邀请','Block':'屏蔽',
        'Open Hub':'在 Hub 打开','Change':'使用此形象','Share':'分享','Unshare':'取消分享',
        'New':'新建','Load':'加载','Notifications':'通知','Dismiss':'忽略',
        'Name':'名称','Description':'说明','Author':'作者','Members':'成员','Settings':'设置',
        'Props':'道具','Worlds':'世界','Friends':'好友','Avatars':'虚拟形象','Search':'搜索',
        'Exit':'退出','Logout':'注销','Discover':'发现','Home':'首页','Unmute':'取消静音','Mute':'静音',
        'Advanced Avatars':'形象参数','Show all':'显示全部','No Clip':'穿越碰撞','Full Body':'全身追踪',
        'Voice Ping:':'语音延迟：','Game Version':'游戏版本','No Group':'未展示群组',
        'These options will adjust the Notification for Voice Activity while Mute':'这些选项会调整静音时语音活动的通知行为',
        'These options will adjust Player and Camera Nameplates to your specification':'这些选项用于调整玩家和相机姓名牌',
        'These options will adjust your Microphone to your specification':'这些选项用于调整麦克风行为',
        'These options will adjust Mouse and Keyboard input to your specification':'这些选项用于调整鼠标和键盘输入',
        'Sensitivity':'灵敏度','Graphics Settings':'画质设置','Game Anti-Aliasing':'游戏抗锯齿','Camera Anti-Aliasing':'相机抗锯齿',
        'Mirror Anti-Aliasing':'镜像抗锯齿','Portal Quality':'传送门质量','Maximum Mirror Resolution':'镜像最大分辨率',
        'World Transition':'世界过渡','Transition':'过渡','Primary':'主色','Secondary':'次色','Tertiary':'第三色',
        'Test Transition':'测试过渡','Changes the effect used when transitioning to a new instance':'调整进入新房间时使用的过渡效果',
        'Overall volume of the game audio':'游戏整体音量','Default Max Resolution':'默认最大分辨率',
        'Red: {value}':'红：{value}','Green: {value}':'绿：{value}','Blue: {value}':'蓝：{value}',
        'Fade to Black':'淡出至黑','Classic':'经典','Digitize':'数字化','None':'无'
    };
    Object.keys(extra).forEach(function(key){common[key]=extra[key];});
    var fixes={
        'My Avatars':'我的形象','Shared Avatars':'共享形象','Official Avatars':'官方形象',
        'New Avatars':'最新形象','Recently Updated':'最近更新','Favorited Avatars':'收藏的形象',
        'Online Friends':'在线好友','Offline Friends':'离线好友','Favorited Friends':'特别关注的好友',
        'My Worlds':'我的世界','Shared Worlds':'共享世界','Official Worlds':'官方世界',
        'New Worlds':'最新世界','Favorited Worlds':'收藏的世界','My Props':'我的道具',
        'Shared Props':'共享道具','Official Props':'官方道具','New Props':'最新道具','Favorited Props':'收藏的道具',
        'No badge featured':'未展示徽章','No group featured':'未展示群组',
        'Toolbox':'工具箱','Miscellaneous features and utilities':'实用功能与工具',
        'Personal Mirror':'个人镜子','Show Mirror':'显示镜子','Playspace Anchored':'固定到游玩空间',
        'Pickupable':'允许抓取','Configure':'配置','Personal Pen':'个人画笔','Undo Segment':'撤销上一笔',
        'Clear Pen':'清空笔迹','Utilities':'实用工具','Create Mini Me':'创建迷你分身',
        'Anchor Mode':'固定模式','Self Portrait':'自拍镜像','Developer Tools':'开发工具',
        'Mirror Settings':'镜子设置','Pen Settings':'画笔设置','Reset':'重置',
        'Enable':'启用','Disable':'停用','Enabled':'已启用','Disabled':'已停用',
        'Details':'详情','Change Avatar':'更换虚拟形象',
        'Scale':'大小','Opacity':'不透明度','Width':'宽度','Height':'高度','Distance':'距离',
        'Colour':'颜色','Color':'颜色','Quality':'质量','Resolution':'分辨率'
    };
    Object.keys(fixes).forEach(function(key){common[key]=fixes[key];});
    var detailWords={
        'User Profile':'玩家资料','My Profile':'我的资料','Activity':'动态','Bio':'个人简介','Notes':'备注',
        'Featured Badge':'展示的徽章','Featured Group':'展示的群组','Current Avatar':'当前形象',
        'Add Friend':'添加好友','Remove Friend':'移除好友','Unfavorite':'取消收藏','Kick':'移出房间','Vote Kick':'投票移出',
        'Set Home':'设为家园','Open In Hub':'在 Hub 打开','Preload':'预加载','Screenshots':'截图','Permissions':'权限',
        'World Information':'世界信息','The author':'作者','Profile':'查看资料','Other worlds by this author':'作者的其他世界',
        'Live instances':'正在开放的房间','Create new instance':'创建房间','Create Instance':'创建房间',
        'Normal Instance Types':'常规房间','Group Instance Types':'群组房间',
        'Public':'公开','Friends of Friends':'好友的好友','Friends Only':'仅限好友',
        'Everyone Can Invite':'所有人可邀请','Owner Must Invite':'仅房主可邀请',
        'Group Public':'群组公开','Friends of Members':'成员的好友','Members Only':'仅限群组成员',
        'Target Group:':'选择群组：','Select instance region':'服务器地区','EU':'欧洲','US':'美国','AS':'亚洲',
        'File size':'文件大小','Uploaded':'上传日期','Updated':'更新日期','Description:':'简介：','Tags:':'标签：','Tags':'标签',
        'Instance Owner':'房主','Instance Information':'房间信息','Players in Instance:':'房间内玩家：',
        'Copy Deeplink':'复制房间链接','Instance Details':'房间详情',
        'Upload Pic':'上传头像','Edit Pronouns':'编辑称谓','Edit Bio':'编辑简介','Player Colors':'玩家配色','Portal':'传送门',
        'This world has no description':'此世界暂无简介','This avatar has no description':'此形象暂无简介',
        'There are currently no open instances for this world':'此世界目前没有开放的房间','Private':'私有'
    };
    Object.keys(detailWords).forEach(function(key){common[key]=detailWords[key];});
    safeSelectors+=',.action-mute,[data-comfort-category-label]';
    safeSelectors+=',#settings .inp_slider .valueLabel,#settings .dropdown-value,#settings .valueList .listValue,.transition-row .option-caption,.detail .toolbar-btn,.detail .tab-btn,.detail .title,.detail .content-caption,.detail .instance-btn,'+
        '.detail .profile-badge .profile-extra-caption,.detail .profile-group .profile-extra-caption,'+
        '.detail .profile-avatar .profile-extra-caption,.detail .world-instances-empty-message,.detail .avatar-sidebar .privacy,#world-instance-create h2,#world-instance-create h3,#world-instance-create h4,#world-instance-create .region-select';
    excludedText+=',.detail > h1,.data-description,.data-safetyTags,.avatar-maininformation .description,.avatar-maininformation .tags,.prop-maininformation .description,.prop-maininformation .tags,.tab-content-bio,.tab-content-notes,.user-sidebar .pronouns,.user-sidebar .rank,.cu-content-translate,.cu-content-translated';

    var headingSources={'world-detail':'World: ','avatar-detail':'Avatar: ','prop-detail':'Prop: ','user-detail':'User: ','instance-detail':'Instance: '};
    var headingChinese={'world-detail':'世界：','avatar-detail':'形象：','prop-detail':'道具：','user-detail':'玩家：','instance-detail':'房间：'};
    function detailPrefix(id){return prefs.language==='zh'?headingChinese[id]:prefs.language==='en'?headingSources[id]:((pack().prefixes || {})[id] || headingSources[id]);}
    function splitDetailHeading(id,value){
        var candidates=[headingSources[id],headingChinese[id]];
        Object.keys(localePacks).forEach(function(code){candidates.push((localePacks[code].prefixes || {})[id]);});
        for(var i=0;i<candidates.length;i++)if(candidates[i] && value.indexOf(candidates[i])===0)return {text:value.substr(candidates[i].length),prefix:detailPrefix(id)};
        return {text:value,prefix:''};
    }
    function localizeDetailHeadings() {
        if(context!=='main')return;
        Object.keys(headingSources).forEach(function(id){
            var heading=document.querySelector('#'+id+' > h1');
            if(!heading || heading.querySelector('.cu-content-translated'))return;
            var part=splitDetailHeading(id,heading.textContent);
            if(part.prefix){var desired=part.prefix+part.text;if(heading.textContent!==desired)heading.textContent=desired;}
            else if(id==='user-detail')for(var i=0;i<heading.childNodes.length;i++)if(heading.childNodes[i].nodeType===3)translateNode(heading.childNodes[i]);
        });
        var own=document.querySelector('#our-user-detail > h1');
        if(own)for(var i=0;i<own.childNodes.length;i++)if(own.childNodes[i].nodeType===3)translateNode(own.childNodes[i]);
    }

    // Native refreshes use this lookup on every frame/update. Translate before the
    // text is assigned, so an observer never has to race the game for Seated Play.
    function installNativeLocalization() {
        installMessageLocalization();
        if(context==='main')installSettingsLocalization();
        if(context==='quick' && window.cvr && typeof cvr.t==='function' && !cvr.t.__comfortOriginal) {
            var original=cvr.t, words={
                'core-category-worlds':['世界','Worlds'],'core-category-friends':['好友','Friends'],
                'core-category-groups':['群组','Groups'],'core-category-avatars':['虚拟形象','Avatars'],
                'core-category-advavatars':['形象参数','Advanced Avatars'],'core-category-flight':['飞行','Flight'],
                'core-category-respawn':['重生','Respawn'],'core-category-camera':['相机','Camera'],
                'core-category-recalibrate':['重新校准','Recalibrate'],'core-category-seatedplay':['坐姿模式','Seated Play'],
                'core-category-props':['道具列表','Prop List'],'core-category-emotes':['表情与切换','Emotes & Toggles'],
                'core-category-events':['活动','Events'],'core-greeting':['欢迎，[playername]','Welcome, [playername]'],
                'core-datetime-format':['[year]年[monthname][day]日','[monthname] [day], [year]'],
                'core-flight-no-clip':['无碰撞飞行','No Clip'],'core-fbt-toggle':['全身追踪','Full Body'],
                'core-back-to-main':['返回主界面','Back to main screen'],
                'notification-header':['通知','Notifications'],'notification-button':['显示全部','Show all']
            };
            var months=['January','February','March','April','May','June','July','August','September','October','November','December'];
            var days=['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];
            months.forEach(function(name,i){words['core-month-'+i]=[(i+1)+'月',name];});
            days.forEach(function(name,i){words['core-weekday-'+i]=['星期'+['日','一','二','三','四','五','六'][i],name];});
            var translated=function(lang,key,translations) {
                // Resolve our own keys without replacing the native English registration.
                // Another language mod may register into that slot before or after us.
                var local=(pack().quick || {})[key];if(typeof local==='string')return local;
                if(words[key])return prefs.language==='zh'?words[key][0]:prefs.language==='en'?words[key][1]:((pack().quick || {})[key] || systemText(words[key][1]));
                var value=original.apply(this,arguments),source=typeof value==='string'?sourceText(value):null;return source?systemText(source):value;
            };
            translated.__comfortOriginal=original;cvr.t=translated;
        }
        if(context==='main' && typeof window.CreateRenderCategory==='function' && !window.CreateRenderCategory.__comfortOriginal) {
            var create=window.CreateRenderCategory;
            var localCreate=function(category,info) {
                var element=create.apply(this,arguments);
                if(info && info.IsSystemCategory && element) {
                    var text=info.CategoryClearTextName, source=common[text]?text:reverse[text];
                    if(source && common[source]) {
                        element.setAttribute('data-comfort-category-label',source);
                        element.textContent=systemText(source);
                    }
                }
                return element;
            };
            localCreate.__comfortOriginal=create;window.CreateRenderCategory=localCreate;
        }
    }
    function localizeToolbox() {
        if(context!=='quick')return;
        var selector='[id^="CVRUI-QMUI-CVRUILib-"] .text,[id^="CVRUI-QMUI-CVRUILib-"] .text-sm,'+
            '[id^="CVRUI-QMUI-CVRUILib-"] .text-title,[id^="CVRUI-QMUI-CVRUILib-"] .header,'+
            '[id^="CVRUI-QMUI-CVRUILib-"] .option-text,[id^="CVRUI-QMUI-CVRUILib-"] .desc-line,'+
            '#CVRUI-QMUI-MenuHeader,#CVRUI-QMUI-MenuSubtitle';
        var elements=document.querySelectorAll(selector);
        for(var i=0;i<elements.length;i++) {
            var element=elements[i];
            if(isExcluded(element))continue;
            for(var j=0;j<element.childNodes.length;j++)if(element.childNodes[j].nodeType===3)translateNode(element.childNodes[j]);
            translateAttributes(element);
        }
    }
    function nativeSetting(name) {
        if(typeof settings!=='undefined')for(var i=0;i<settings.length;i++)if(settings[i]&&settings[i].name===name)return settings[i];
        return null;
    }
    var settingLabels=new WeakMap();
    function localizeSettingControl(element) {
        if(!element)return;
        var nodes=settingLabels.get(element);
        if(!nodes){nodes=element.querySelectorAll('.valueLabel,.dropdown-value,.listValue');settingLabels.set(element,nodes);}
        for(var i=0;i<nodes.length;i++)for(var j=0;j<nodes[i].childNodes.length;j++)if(nodes[i].childNodes[j].nodeType===3)translateNode(nodes[i].childNodes[j]);
        var record=nativeControls.get(element);
        if(record)syncNativeControl(element,record);
        else if(!element.classList.contains('inp_slider') && !element.classList.contains('inp_number'))decorateNativeSettings(element);
    }
    var nativeControls=new WeakMap();
    function nativeControlDisabled(node){return !!(node.closest && node.closest('.disabled,.noaction,[aria-disabled="true"]'));}
    function nativePress(node){
        var event=document.createEvent('MouseEvent');event.initMouseEvent('mousedown',true,true,window,1,0,0,0,0,false,false,false,false,0,null);node.dispatchEvent(event);
    }
    function syncNativeControl(node,record){
        var name=node.id,label=node.closest('.row-wrapper'),caption=label && label.querySelector('.option-caption');
        if(caption)node.setAttribute('aria-label',caption.textContent.trim());
        if(record.kind==='toggle'){
            var checked=node.classList.contains('checked');
            for(var i=0;i<record.buttons.length;i++){
                var button=record.buttons[i],selected=(i===0)===checked,text=t(i===0?'on':'off');
                if(button.textContent!==text)button.textContent=text;
                button.setAttribute('aria-pressed',selected?'true':'false');button.disabled=nativeControlDisabled(node);
            }
            syncSegmentedBar(node,record.buttons.length,checked?0:1);
        }else if(record.kind==='dropdown'){
            var choices=node.querySelectorAll('.listValue'),count=choices.length,characters=0;
            for(var n=0;n<count;n++)characters+=choices[n].textContent.length;
            var segmented=count>=2 && count<=4 && characters<=76 && node.getAttribute('data-type')!=='subcategory';
            node.classList.toggle('cu-native-segments',segmented);
            var setting=nativeSetting(name),value=setting && setting.value();
            var selectedIndex=-1;
            for(var j=0;j<count;j++){
                var selected=String(choices[j].getAttribute('data-key'))===String(value);
                if(selected)selectedIndex=j;
                choices[j].classList.toggle('cu-native-selected',selected);choices[j].setAttribute('role','button');choices[j].setAttribute('tabindex','0');choices[j].setAttribute('aria-pressed',selected?'true':'false');
            }
            syncSegmentedBar(node.querySelector('.valueList'),segmented?count:0,selectedIndex);
        }
    }
    function decorateNativeSettings(scope){
        if(context!=='main')return;
        var base=document.getElementById('settings');if(!base)return;
        scope=scope && base.contains(scope)?scope:base;
        var elements=[];
        if(scope.matches && scope.matches('.inp_toggle,.inp_dropdown'))elements.push(scope);
        var descendants=scope.querySelectorAll('.inp_toggle,.inp_dropdown');
        for(var d=0;d<descendants.length;d++)elements.push(descendants[d]);
        for(var i=0;i<elements.length;i++){
            var node=elements[i],record=nativeControls.get(node);
            if(!record){
                record={kind:node.classList.contains('inp_toggle')?'toggle':'dropdown',buttons:[]};nativeControls.set(node,record);
                if(record.kind==='toggle'){
                    node.classList.add('cu-native-toggle');node.setAttribute('role','group');
                    for(var b=0;b<2;b++){var button=document.createElement('button');button.type='button';button.className='cu-native-toggle-option';button.setAttribute('data-cu-state',b===0?'True':'False');node.appendChild(button);record.buttons.push(button);}
                    node.addEventListener('mousedown',function(event){
                        if(!shown)return;
                        var button=event.target.closest && event.target.closest('[data-cu-state]'),wanted=button && button.getAttribute('data-cu-state')==='True';
                        if(nativeControlDisabled(this) || button && wanted===this.classList.contains('checked')){event.preventDefault();event.stopImmediatePropagation();}
                    },true);
                    node.addEventListener('keydown',function(event){
                        if(!shown || nativeControlDisabled(this))return;
                        var button=event.target.closest && event.target.closest('[data-cu-state]');if(!button)return;
                        var key=event.key || event.keyCode;
                        if(key==='ArrowLeft'||key===37)button=this.querySelector('[data-cu-state="True"]');
                        else if(key==='ArrowRight'||key===39)button=this.querySelector('[data-cu-state="False"]');
                        else if(key!=='Enter' && key!==' ' && key!==13 && key!==32)return;
                        event.preventDefault();event.stopPropagation();nativePress(button);button.focus();syncNativeControl(this,nativeControls.get(this));
                    });
                }else{
                    node.addEventListener('keydown',function(event){
                        if(!shown || nativeControlDisabled(this) || event.key!=='Enter' && event.key!==' ' && event.keyCode!==13 && event.keyCode!==32)return;
                        var choice=event.target.closest && event.target.closest('.listValue');if(!choice)return;
                        event.preventDefault();nativePress(choice);syncNativeControl(this,nativeControls.get(this));
                    });
                    node.addEventListener('mousedown',function(event){
                        if(shown && nativeControlDisabled(this)){event.preventDefault();event.stopImmediatePropagation();}
                    },true);
                }
                node.addEventListener('mousedown',function(){syncNativeControl(this,nativeControls.get(this));});
                node.addEventListener('dropdownSelect',function(){syncNativeControl(this,nativeControls.get(this));});
            }
            syncNativeControl(node,record);
        }
    }
    function installSettingsLocalization() {
        if(typeof settings!=='undefined')settings.forEach(function(entry){
            if(!entry||typeof entry.updateValue!=='function'||entry.updateValue.__comfortOriginal)return;
            var update=entry.updateValue;
            var wrapped=function(){
                var result=update.apply(this,arguments);
                localizeSettingControl(document.getElementById(entry.name));
                if(entry.name.indexOf('GraphicsTransitionColor')===0)syncTransitionPickers();
                return result;
            };
            wrapped.__comfortOriginal=update;entry.updateValue=wrapped;
        });
        ['updateDropDownOptions'].forEach(function(name){
            var original=window[name];
            if(typeof original!=='function'||original.__comfortOriginal)return;
            var wrapped=function(){
                var result=original.apply(this,arguments),element=document.getElementById(arguments[0]);
                if(element)settingLabels.delete(element);
                localizeSettingControl(element);
                return result;
            };
            wrapped.__comfortOriginal=original;window[name]=wrapped;
        });
        if(settingEventsReady)return;
        settingEventsReady=true;
        // Run after the native handler, preserving its keys, routing and save calls.
        document.addEventListener('mousedown',function(event){
            var control=event.target.closest&&event.target.closest('#settings .inp_dropdown');
            if(control)localizeSettingControl(control);
        });
        var draggingSetting;
        document.addEventListener('mousedown',function(event){draggingSetting=event.target.closest && event.target.closest('#settings .inp_slider');});
        document.addEventListener('mousemove',function(event){if(event.buttons===1 && draggingSetting)localizeSettingControl(draggingSetting);});
        document.addEventListener('mouseup',function(){draggingSetting=null;});
        window.addEventListener('blur',function(){draggingSetting=null;});
    }
    function hsvToRgb(h,s,v) {
        var c=v*s, x=c*(1-Math.abs((h/60)%2-1)), m=v-c;
        var colour=h<60?[c,x,0]:h<120?[x,c,0]:h<180?[0,c,x]:h<240?[0,x,c]:h<300?[x,0,c]:[c,0,x];
        return colour.map(function(value){return Math.round((value+m)*255);});
    }
    function rgbToHsv(colour,previousHue) {
        var r=colour[0]/255,g=colour[1]/255,b=colour[2]/255,max=Math.max(r,g,b),min=Math.min(r,g,b),delta=max-min,h=previousHue||0;
        if(delta){h=max===r?60*((g-b)/delta%6):max===g?60*((b-r)/delta+2):60*((r-g)/delta+4);if(h<0)h+=360;}
        return {h:h,s:max===0?0:delta/max,v:max};
    }
    function transitionColour(prefix) {
        var result=[];
        for(var i=0;i<3;i++) {
            var entry=nativeSetting('GraphicsTransitionColor'+prefix+['R','G','B'][i]);
            if(!entry||typeof entry.value!=='function'||typeof entry.updateValue!=='function')return null;
            var value=Number(entry.value());
            if(!isFinite(value)||value<0||value>255)return null;
            result.push(Math.round(value));
        }
        return result;
    }
    function setNativeTransitionColour(prefix,colour) {
        var previous=transitionColour(prefix);
        if(!previous||!window.engine||typeof engine.call!=='function'){toast(t('unavailable'));return false;}
        for(var i=0;i<3;i++) {
            if(previous[i]===colour[i])continue;
            var id='GraphicsTransitionColor'+prefix+['R','G','B'][i];
            try {
                nativeSetting(id).updateValue(colour[i]);
                if(typeof game_settings!=='undefined')game_settings[id]=colour[i];
                var call=engine.call('CVRAppCallSaveSetting',id,String(colour[i]));
                if(call&&typeof call.catch==='function')call.catch(function(){toast(t('saveFailed'));});
            } catch(ignored){toast(t('saveFailed'));return false;}
        }
        return true;
    }
    function pickerText(element,value) {if(element.textContent!==value)element.textContent=value;}
    function paintTransitionPicker(picker) {
        var hsv=picker.hsv,colour=hsvToRgb(hsv.h,hsv.s,hsv.v),ctx=picker.field.getContext('2d'),w=picker.field.width,h=picker.field.height;
        var drawKey=[hsv.h,hsv.s,hsv.v,prefs.language].join(':');
        if(picker.drawKey===drawKey)return;
        picker.drawKey=drawKey;
        if(picker.paintedHue!==hsv.h) {
            ctx.fillStyle='rgb('+hsvToRgb(hsv.h,1,1).join(',')+')';ctx.fillRect(0,0,w,h);
            var white=ctx.createLinearGradient(0,0,w,0);white.addColorStop(0,'rgba(255,255,255,1)');white.addColorStop(1,'rgba(255,255,255,0)');ctx.fillStyle=white;ctx.fillRect(0,0,w,h);
            var black=ctx.createLinearGradient(0,0,0,h);black.addColorStop(0,'rgba(0,0,0,0)');black.addColorStop(1,'rgba(0,0,0,1)');ctx.fillStyle=black;ctx.fillRect(0,0,w,h);
            picker.paintedHue=hsv.h;
        }
        picker.point.style.left=(hsv.s*100)+'%';picker.point.style.top=((1-hsv.v)*100)+'%';
        picker.huePoint.style.left=(hsv.h/360*100)+'%';
        picker.swatch.style.backgroundColor='rgb('+colour.join(',')+')';
        var hex='#'+colour.map(function(value){return ('0'+value.toString(16)).slice(-2);}).join('').toUpperCase();
        pickerText(picker.code,hex);
        var zh=prefs.language==='zh',colour=pack().colour || {},title=zh?(picker.prefix==='1'?'主色':'次色'):(colour[picker.prefix==='1'?'primary':'secondary'] || (picker.prefix==='1'?'Primary colour':'Secondary colour'));
        pickerText(picker.title,title);pickerText(picker.hint,zh?'点选色相，再在色板上选取明暗与浓淡。':(colour.hint || 'Choose a hue, then tap the colour field.'));
        picker.field.setAttribute('aria-label',title+(zh?'：选择明暗与浓淡':(colour.field || ' saturation and brightness')));
        picker.field.setAttribute('aria-valuetext',hex);
        picker.hue.setAttribute('aria-label',title+(zh?'：色相':(colour.hueSuffix || ' hue')));picker.hue.setAttribute('aria-valuenow',Math.round(hsv.h));
        pickerText(picker.hueTitle,zh?'色相':(colour.hue || 'Hue'));
        for(var i=0;i<picker.presets.length;i++)picker.presets[i].setAttribute('aria-label',title+' '+picker.presets[i].getAttribute('data-colour'));
    }
    function syncTransitionPickers() {
        transitionPickers.forEach(function(picker){
            var colour=transitionColour(picker.prefix);
            if(!colour)return;
            picker.hsv=rgbToHsv(colour,picker.hsv.h);paintTransitionPicker(picker);
        });
    }
    function installTransitionPalettes() {
        if(context!=='main')return;
        ['1','2'].forEach(function(prefix){
            var row=document.querySelector('.transition-row.'+(prefix==='1'?'primary':'secondary')+'-colour');
            if(!row||row.getAttribute('data-comfort-picker-fallback')==='true'||row.querySelector('[data-comfort-transition-palette]'))return;
            var colour=transitionColour(prefix);
            if(!colour)return;
            var panel=document.createElement('section');panel.className='cu-transition-picker';panel.setAttribute('data-comfort-transition-palette',prefix);
            panel.innerHTML='<div class="cu-colour-heading"><h3></h3><span class="cu-picked-swatch"></span><span class="cu-picked-code"></span></div>'+
                '<p class="cu-picker-hint"></p><div class="cu-colour-plane"><canvas class="cu-colour-field" width="512" height="256" tabindex="0" role="slider"></canvas><span class="cu-colour-point"></span></div>'+
                '<div class="cu-hue-title"></div><div class="cu-hue-plane"><canvas class="cu-hue-field" width="512" height="64" tabindex="0" role="slider" aria-valuemin="0" aria-valuemax="359"></canvas><span class="cu-hue-point"></span></div><div class="cu-picker-presets"></div>';
            var picker={prefix:prefix,panel:panel,hsv:rgbToHsv(colour),title:panel.querySelector('h3'),hint:panel.querySelector('.cu-picker-hint'),field:panel.querySelector('.cu-colour-field'),point:panel.querySelector('.cu-colour-point'),hue:panel.querySelector('.cu-hue-field'),huePoint:panel.querySelector('.cu-hue-point'),hueTitle:panel.querySelector('.cu-hue-title'),swatch:panel.querySelector('.cu-picked-swatch'),code:panel.querySelector('.cu-picked-code'),presets:[]};
            // Do not hide working native sliders if this Cohtml build lacks canvas support.
            var hueContext,fieldContext;
            try{hueContext=picker.hue.getContext('2d');fieldContext=picker.field.getContext('2d');}catch(ignored){row.setAttribute('data-comfort-picker-fallback','true');return;}
            if(!hueContext||!fieldContext){row.setAttribute('data-comfort-picker-fallback','true');return;}
            try {
            var gradient=hueContext.createLinearGradient(0,0,512,0);
            ['#ff0000','#ffff00','#00ff00','#00ffff','#0000ff','#ff00ff','#ff0000'].forEach(function(hex,index){gradient.addColorStop(index/6,hex);});
            hueContext.fillStyle=gradient;hueContext.fillRect(0,0,512,64);
            function commit() {
                var next={h:picker.hsv.h,s:picker.hsv.s,v:picker.hsv.v};
                if(setNativeTransitionColour(prefix,hsvToRgb(next.h,next.s,next.v)))picker.hsv=next;
                paintTransitionPicker(picker);
            }
            function fraction(event,canvas){var rect=canvas.getBoundingClientRect();return {x:Math.max(0,Math.min(1,(event.clientX-rect.left)/rect.width)),y:Math.max(0,Math.min(1,(event.clientY-rect.top)/rect.height))};}
            picker.field.addEventListener('click',function(event){var point=fraction(event,picker.field);picker.hsv.s=point.x;picker.hsv.v=1-point.y;commit();});
            picker.hue.addEventListener('click',function(event){picker.hsv.h=Math.min(359.99,fraction(event,picker.hue).x*360);commit();});
            picker.field.addEventListener('keydown',function(event){
                var step=event.shiftKey?0.1:0.02;
                if(event.key==='ArrowLeft')picker.hsv.s=Math.max(0,picker.hsv.s-step);
                else if(event.key==='ArrowRight')picker.hsv.s=Math.min(1,picker.hsv.s+step);
                else if(event.key==='ArrowUp')picker.hsv.v=Math.min(1,picker.hsv.v+step);
                else if(event.key==='ArrowDown')picker.hsv.v=Math.max(0,picker.hsv.v-step);
                else return;
                event.preventDefault();commit();
            });
            picker.hue.addEventListener('keydown',function(event){
                if(event.key!=='ArrowLeft'&&event.key!=='ArrowRight')return;
                picker.hsv.h=(picker.hsv.h+(event.key==='ArrowRight'?1:-1)*(event.shiftKey?15:3)+360)%360;
                event.preventDefault();commit();
            });
            ['#FFFFFF','#A8ADB7','#16181D','#C3D5E5','#C4D5C5','#DDC4D6'].forEach(function(hex){
                var preset=document.createElement('button');preset.type='button';preset.className='cu-picker-preset';preset.setAttribute('data-colour',hex);preset.style.backgroundColor=hex;
                preset.addEventListener('click',function(){picker.hsv=rgbToHsv(rgb(hex),picker.hsv.h);commit();});
                picker.presets.push(preset);panel.querySelector('.cu-picker-presets').appendChild(preset);
            });
            paintTransitionPicker(picker);row.appendChild(panel);row.classList.add('cu-picker-ready');transitionPickers.push(picker);
            } catch(ignored) {
                row.setAttribute('data-comfort-picker-fallback','true');
                report('colour-picker-fallback','Canvas unavailable; native colour controls retained.');
            }
        });
        transitionPickers.forEach(paintTransitionPicker);
    }
    function localizeMainCategories() {
        if(context!=='main')return;
        // These arrays retain the IsSystemCategory flag. User-created categories
        // and avatar names keep their exact text, even when named "My Avatars".
        var sets={
            friends:typeof friendsCategories!=='undefined'?friendsCategories:[],
            avatars:typeof avatarsCategories!=='undefined'?avatarsCategories:[],
            worlds:typeof worldsCategories!=='undefined'?worldsCategories:[],
            props:typeof propsCategories!=='undefined'?propsCategories:[]
        };
        Object.keys(sets).forEach(function(page){
            var filters=document.querySelectorAll('#'+page+' .filter-content .filter-option');
            sets[page].forEach(function(info){
                if(!info.IsSystemCategory)return;
                var source=common[info.CategoryClearTextName]?info.CategoryClearTextName:reverse[info.CategoryClearTextName];
                if(!source || !common[source])return;
                for(var i=0;i<filters.length;i++)if(filters[i].classList.contains('data-filter-'+info.CategoryKey)) {
                    filters[i].setAttribute('data-comfort-category-label',source);
                    var desired=systemText(source);
                    if(filters[i].textContent!==desired)filters[i].textContent=desired;
                }
            });
        });
        ['#profile-title','#group-name-home'].forEach(function(selector){
            var element=document.querySelector(selector);
            if(!element)return;
            // Only empty-profile placeholders; never a real badge/group name.
            for(var i=0;i<element.childNodes.length;i++) {
                var node=element.childNodes[i], value=node.nodeType===3?node.nodeValue.trim():'';
                var source=sourceText(value);if(source==='No badge featured'||source==='No group featured')translateNode(node);
            }
        });
    }

    function detectContext() {
        if(window.__comfortContext)return window.__comfortContext;
        var path=location.pathname.toLowerCase();
        if(path.indexOf('/login/')>=0 || document.getElementById('login-container'))return 'login';
        if(path.indexOf('/keyboardui/')>=0 || document.querySelector('.keyboard'))return 'keyboard';
        if(path.indexOf('/cvr_hud/')>=0 || document.querySelector('.hud-area-left-bottom'))return 'hud';
        return document.getElementById('quickmenu-wrapper') || path.indexOf('/gameui/')>=0 ? 'quick':'main';
    }
    function isExcluded(node) {return node && node.closest && !!node.closest(excludedText);}
    function rememberStatic(parent) {
        if(!parent || isExcluded(parent))return;
        for(var i=0;i<parent.childNodes.length;i++) {
            var child=parent.childNodes[i];
            if(child.nodeType===3 && child.nodeValue.trim())staticTexts.push(child);
            else if(child.nodeType===1)rememberStatic(child);
        }
    }
    function translateAttributes(item) {
        ['data-tooltip','data-placeholder','placeholder','data-input-title','data-normal-mode-label','data-shift-mode-label','data-format','data-zerotext'].forEach(function(attr){
            var value=item.getAttribute(attr);
            if(!value)return;
            var normalized=value.replace(/\s+/g,' ').trim();
            var source=Object.prototype.hasOwnProperty.call(dictionary,value)?value:Object.prototype.hasOwnProperty.call(dictionary,normalized)?normalized:reverse[value]||reverse[normalized];
            if(!source)return;
            var desired=systemText(source);
            if(value!==desired)item.setAttribute(attr,desired);
        });
        // data-options/data-value are native routing values, never translated.
    }
    function localizeQuickHome() {
        if(context!=='quick')return;
        // Built-in action classes are stable identifiers. Localize their label only,
        // independently of optional dictionaries and of the text a native refresh writes.
        var words={
            friends:['好友','Friends'],groups:['群组','Groups'],events:['活动','Events'],
            gohome:['返回家园','Go Home'],currinst:['当前房间','Current Instance'],worlds:['世界','Worlds'],
            props:['道具列表','Prop List'],avatars:['虚拟形象','Avatars'],advavatars:['形象参数','Advanced Avatars'],
            emotes:['表情与切换','Emotes & Toggles'],respawn:['重生','Respawn'],camera:['相机','Camera'],flight:['飞行','Flight']
        };
        function label(element,zh,en) {
            if(!element)return;
            element.setAttribute('data-comfort-system-label','true');
            var desired=prefs.language==='zh'?zh:systemText(en);
            if(element.textContent!==desired)element.textContent=desired;
        }
        Object.keys(words).forEach(function(action){
            label(document.querySelector('#quickmenu-wrapper .quick-button.'+action+' .label'),words[action][0],words[action][1]);
        });
        var calibration=document.querySelector('#quickmenu-wrapper .quick-button.recalibrateAndSeatedPlay');
        if(calibration) {
            var seated=calibration.classList.contains('seatedPlay');
            label(calibration.querySelector('.label'),seated?'坐姿模式':'重新校准',seated?'Seated Play':'Recalibrate');
        }
        label(document.querySelector('#quickmenu-wrapper .quick-button.flight .inner-toggle'),'无碰撞飞行','No Clip');
        label(document.querySelector('#quickmenu-wrapper .recalibrateAndSeatedPlay .inner-toggle'),'全身追踪','Full Body');
        var data=window.menu && menu.gameData && menu.gameData.core;
        if(data && typeof data.username==='string'){var greeting=prefs.language==='zh'?'欢迎，[playername]':prefs.language==='en'?'Welcome, [playername]':((pack().quick || {})['core-greeting'] || 'Welcome, [playername]');label(document.querySelector('#quickmenu-wrapper .game-greeting'),'欢迎，'+data.username,greeting.replace('[playername]',data.username));}
        var notifications=document.querySelector('#quickmenu-wrapper .notification-container .scroll-content');
        var count=notifications?notifications.childElementCount:0;
        label(document.querySelector('#quickmenu-wrapper .notification-container .header-text'),'通知 - '+count,systemText('Notifications')+' - '+count);
        label(document.querySelector('#quickmenu-wrapper .notification-container .header-button'),'显示全部','Show all');
    }
    function translateCommon() {
        if(!mounted)return;
        installNativeLocalization();
        localizeQuickHome();
        localizeMainCategories();
        localizeToolbox();
        installTransitionPalettes();
        localizeDetailHeadings();
        staticTexts=staticTexts.filter(function(node){
            if(!document.documentElement.contains(node))return false;
            if(!isExcluded(node.parentNode))translateNode(node);
            return true;
        });
        var controls=document.querySelectorAll(safeSelectors);
        for(var i=0;i<controls.length;i++) {
            var item=controls[i];
            if(isExcluded(item))continue;
            for(var j=0;j<item.childNodes.length;j++) {
                var child=item.childNodes[j];
                if(child.nodeType===3)translateNode(child);
                else if(child.nodeType===1 && (child.tagName==='SPAN'||child.tagName==='P') && !isExcluded(child)) {
                    for(var n=0;n<child.childNodes.length;n++)if(child.childNodes[n].nodeType===3)translateNode(child.childNodes[n]);
                }
            }
            translateAttributes(item);
        }
        var attributes=document.querySelectorAll('[data-tooltip],[data-placeholder],[data-input-title],[data-normal-mode-label],#settings [data-format],#settings [data-zerotext]');
        for(var a=0;a<attributes.length;a++)if(!isExcluded(attributes[a]) || attributes[a].tagName==='INPUT')translateAttributes(attributes[a]);
        if(window.ComfortContent)window.ComfortContent.refresh();
    }
    function renderDashboard() {
        if(!dashboard)return;
        // Only replace our own header. Native live content and its listeners stay intact.
        var heading=dashboard.querySelector('.cu-dashboard-heading');
        if(heading.__comfortLanguage!==prefs.language){heading.__comfortLanguage=prefs.language;heading.innerHTML='<div><span class="cu-kicker">COMFORT</span><h1>'+t(context==='main'?'mainTitle':'quickTitle')+'</h1><p>'+t(context==='main'?'mainIntro':'quickIntro')+'</p></div><span class="cu-dashboard-hint">'+t('hint')+'</span>';}
        dashboard.style.display=shown?'flex':'none';
        dashboard.setAttribute('aria-hidden',shown && homeActive?'false':'true');
        if(!shown)restoreHome();
    }
    function render() {
        if(!mounted)return;
        if(context==='hud'){refreshHud();return;}
        attachSettings();
        if(root) {
            var active=document.activeElement, pref=active && active.getAttribute('data-pref'), value=active && active.getAttribute('data-value');
            var action=active && active.getAttribute('data-action');
            if(!root.querySelector('.cu-appearance-options'))root.innerHTML='<div class="cu-panel-heading"><span class="cu-kicker">COMFORT</span><h2></h2><p></p></div>'+
                languageSettingRow()+'<div class="cu-appearance-options"></div>';
            root.querySelector('.cu-panel-heading h2').textContent=t('appearance');root.querySelector('.cu-panel-heading p').textContent=t('appearanceIntro');
            updateLanguageSlider();
            var appearanceKey=[prefs.language,paletteOpen,customPart,colourStep,paletteOpen?JSON.stringify([prefs.palette,prefs.background,prefs.panel,prefs.accent]):''].join('|');
            if(root.__comfortAppearanceKey!==appearanceKey){
            root.__comfortAppearanceKey=appearanceKey;
            root.querySelector('.cu-appearance-options').innerHTML=settingRow('motion','motionHelp','reducedMotion',[[false,'full'],[true,'reduced']])+
                settingRow('motionStyle','motionStyleHelp','motionStyle',[['fade','motionFade'],['slide','motionSlide'],['zoom','motionZoom'],['orbit','motionOrbit']])+
                settingRow('motionSpeed','motionSpeedHelp','motionSpeed',[['fast','motionFast'],['normal','motionNormal'],['gentle','motionGentle']])+
                settingRow('cameraLayout','cameraLayoutHelp','cameraLayout',[[true,'cameraUnified'],[false,'cameraOriginal']])+
                settingRow('nameplateStyle','nameplateHelp','nameplateTheme',[[true,'nameplateUnified'],[false,'nameplateOriginal']])+
                settingRow('chatBubbleStyle','chatBubbleHelp','chatBubbleTheme',[[true,'chatBubbleUnified'],[false,'chatBubbleOriginal']])+
                settingRow('text','textHelp','largeText',[[false,'standard'],[true,'large']])+
                settingRow('contrast','contrastHelp','highContrast',[[false,'normalContrast'],[true,'highContrast']])+
                '<div class="cu-palette-heading">'+button('palette-toggle',t(paletteOpen?'paletteClose':'paletteOpen'),'cu-palette-toggle')+'</div>'+
                (paletteOpen?colourEditor():'')+
                '<div class="cu-settings-bottom">'+button('reset',t('reset'),'cu-reset')+'<span class="cu-save-state" role="status">'+t(saveStatus)+'</span></div>'+
                '<div class="cu-restore-row"><p>'+t('originalHelp')+'</p>'+button('original',t('original'),'cu-original')+'</div>';
            bindOwn(root);
            }
            var choices=root.querySelectorAll('.cu-palettes [data-pref]');
            for(var c=0;c<choices.length;c++){
                var selected=String(prefs[choices[c].getAttribute('data-pref')])===choices[c].getAttribute('data-value');
                choices[c].classList.toggle('cu-selected',selected);choices[c].setAttribute('aria-pressed',selected?'true':'false');
            }
            syncOwnSegments(root);
            bindOwn(root.querySelector('[data-setting="language"]'));updateSaveState();
            var restore=pref?root.querySelector('[data-pref="'+pref+'"][data-value="'+value+'"]'):action?root.querySelector('[data-action="'+action+'"]'):null;
            // The language row and scroll containers survive. Refocusing the same live
            // button or rewriting scrollTop here forces layout before the first motion frame.
            if(restore && restore!==active && !document.documentElement.contains(active) && restore.focus)try{restore.focus();}catch(ignored){}
        }
        if(dock) {
            var dockKey=prefs.language+':'+shown;
            if(dock.__comfortKey!==dockKey){dock.__comfortKey=dockKey;
                dock.innerHTML=(!shown?button('resume',t('resume'),'cu-resume'):'')+'<div class="cu-toast" role="status" aria-live="polite"></div>';
                dock.setAttribute('data-paused',shown?'false':'true');bindOwn(dock);
            }
        }
        renderDashboard();renderTranslation();
    }

    var translationPanel, shortcutPanel, capturingKey=false, chatMode=window.__comfortChatEnabled===true, translationStatus='';
    function updateChatState(enabled){
        chatMode=!!enabled;
        if(!translationPanel)return;
        var action=translationPanel.querySelector('[data-action="translation-chat"]');
        if(action){action.textContent=t(chatMode?'chatStop':'chatStart');action.setAttribute('aria-pressed',chatMode?'true':'false');}
    }
    function translationTool(value){if(window.engine && typeof engine.call==='function')try{engine.call('ComfortUI_TranslationTools',value);}catch(ignored){}}
    function escapeAttribute(value){return String(value).replace(/&/g,'&amp;').replace(/"/g,'&quot;').replace(/</g,'&lt;');}
    function translationField(key,help,max){
        return '<div class="cu-service-field"><label for="comfort-'+key+'">'+t(key)+'</label><p class="cu-setting-help">'+t(help)+'</p><input type="text" id="comfort-'+key+'" value="'+escapeAttribute(prefs[key])+'" maxlength="'+max+'" data-max-length="'+max+'" data-input-title="'+escapeAttribute(t(key))+'" data-success-text="OK" /></div>';
    }
    function applyServiceConfiguration(){
        var next=copy(prefs),valid=true;
        ['aiEndpoint','aiModel'].forEach(function(key){
            var input=document.getElementById('comfort-'+key),value=input.value.trim();
            if(key==='aiEndpoint'?!validServiceEndpoint(value):!validServiceValue(value,120))valid=false;
            next[key]=value;
        });
        if(!valid){toast(t('serviceConfig'));return false;}
        setPreferences(next,false);return true;
    }
    function renderTranslation(){
        if(context!=='main' && context!=='quick')return;
        var parent;
        if(context==='main'){
            var content=document.querySelector('#settings .list-content'),filters=document.querySelector('#settings .filter-content');
            if(!content || !filters)return;
            parent=document.getElementById('settings-comfort-translation');
            if(!parent){
                parent=document.createElement('div');parent.id='settings-comfort-translation';parent.className='settings-categorie';content.appendChild(parent);
                var entry=document.createElement('div');entry.className='filter-option button';entry.setAttribute('data-comfort-translation-category','true');entry.setAttribute('onclick',"switchSettingCategorie('settings-comfort-translation',this)");filters.insertBefore(entry,filters.children[3] || null);
            }
            document.querySelector('[data-comfort-translation-category]').textContent=t('translationTitle');
        }else parent=document.getElementById('CVRUI-QMUI-SettingsPage-Content');
        if(!parent)return;
        if(!translationPanel){translationPanel=document.createElement('section');translationPanel.id='comfort-translation';translationPanel.className='cu-only';translationPanel.setAttribute('data-context',context);parent.appendChild(translationPanel);}
        var translationRenderKey=prefs.language;
        if(translationPanel.__comfortKey!==translationRenderKey){translationPanel.__comfortKey=translationRenderKey;
        translationPanel.innerHTML='<div class="cu-panel-heading"><h2>'+t('translationTitle')+'</h2><p>'+t('translationIntro')+'</p></div>'+
            settingRow('translationProvider','translationProviderHelp','translationProvider',[['google','google'],['mymemory','mymemory'],['microsoft','microsoft'],['ai','ai'],['local','local']])+
            pickerRow('sourceLanguage','sourceHelp','sourceLanguage',[['auto','autoDetect']].concat(languageChoices(languageCodes)))+
            '<div class="cu-service-config" data-service-config="ai">'+translationField('aiEndpoint','aiEndpointHelp',512)+translationField('aiModel','aiModelHelp',120)+'</div>'+
            '<div class="cu-service-config" data-service-config="microsoft"><p class="cu-setting-help">'+t('bingHelp')+'</p></div>'+
            '<div class="cu-service-config" data-service-config="local"><h3>'+t('localModelTitle')+'</h3><p class="cu-setting-help">'+t('localModelHelp')+'</p>'+settingRow('localAcceleration','localAccelerationHelp','localAcceleration',[['cpu','localCpu'],['auto','localAuto']])+'<div class="cu-translation-actions">'+button('translation-local-prepare',t('localPrepare'),'cu-original')+button('translation-local-cancel',t('localCancel'),'cu-original')+'</div><p class="cu-local-status" role="status"></p></div>'+
            '<div class="cu-service-config-action">'+button('translation-config',t('applyServices'),'cu-original')+'</div>'+
            '<div class="cu-credential"><label for="comfort-api-key">'+t('apiKey')+'</label><p class="cu-setting-help">'+t('apiKeyHelp')+'</p><input id="comfort-api-key" type="password" maxlength="1024" autocomplete="off" spellcheck="false" /><p class="cu-key-status"></p><div class="cu-credential-actions">'+button('translation-save-key',t('saveKey'),'cu-original')+button('translation-clear-key',t('clearKey'),'cu-original')+'</div></div>'+
            settingRow('detailTranslation','detailTranslationHelp','detailTranslation',[[true,'on'],[false,'off']])+
            settingRow('chatTranslation','chatTranslationHelp','chatTranslation',[[true,'on'],[false,'off']])+
            settingRow('proxyMode','proxyHelp','proxyMode',[['system','proxySystem'],['direct','proxyDirect'],['manual','proxyManual']])+
            '<div class="cu-translation-address"><label for="comfort-proxy-address">'+t('proxyAddress')+'</label><p>'+t('proxyAddressHelp')+'</p><input id="comfort-proxy-address" type="text" value="'+escapeAttribute(prefs.proxyAddress)+'" data-max-length="256" data-input-title="'+escapeAttribute(t('proxyAddress'))+'" data-placeholder="http://127.0.0.1:7890" data-success-text="OK" /><div>'+button('translation-proxy',t('applyProxy'),'cu-original')+'</div></div>'+
            '<div class="cu-translation-actions">'+button('translation-test',t('testTranslation'),'cu-original')+button('translation-cache',t('clearTranslationCache'),'cu-original')+button('translation-chat',t('chatNow'),'cu-original')+'</div><p class="cu-translation-status" role="status"></p><p class="cu-setting-help">'+t('testHelp')+'</p><p class="cu-setting-help">'+t('translationPrivacy')+'</p>';
        bindOwn(translationPanel);
        updateChatState(chatMode);
        var input=document.getElementById('comfort-proxy-address');
        if(input && context==='main')input.addEventListener('click',function(){if(typeof displayKeyboard==='function')displayKeyboard(input)});
        Array.prototype.forEach.call(translationPanel.querySelectorAll('.cu-service-field input'),function(field){if(context==='main')field.addEventListener('click',function(){if(typeof displayKeyboard==='function')displayKeyboard(field)});});
        }
        syncOwnSegments(translationPanel);
        Array.prototype.forEach.call(translationPanel.querySelectorAll('[data-service-config]'),function(group){group.style.display=group.getAttribute('data-service-config')===prefs.translationProvider?'':'none';});
        ['aiEndpoint','aiModel'].forEach(function(key){if(translationPanel['__value_'+key]!==prefs[key]){translationPanel['__value_'+key]=prefs[key];document.getElementById('comfort-'+key).value=prefs[key];}});
        var credential=prefs.translationProvider==='ai';
        translationPanel.querySelector('.cu-credential').style.display=credential?'':'none';
        translationPanel.querySelector('.cu-service-config-action').style.display=credential?'':'none';
        translationPanel.querySelector('.cu-key-status').textContent=t(prefs.hasAiKey?'keyPresent':'keyMissing');
        updateLocalModelState(localModelStatus[0],localModelStatus[1],localModelStatus[2]);
        if(translationPanel.__credentialProvider!==prefs.translationProvider){translationPanel.__credentialProvider=prefs.translationProvider;document.getElementById('comfort-api-key').value='';}
        translationPanel.querySelector('.cu-translation-status').textContent=translationStatus?t(translationStatus):'';
        var address=translationPanel.querySelector('.cu-translation-address'),proxyInput=document.getElementById('comfort-proxy-address');
        address.style.display=prefs.proxyMode==='manual'?'':'none';
        if(translationPanel.__comfortProxy!==prefs.proxyAddress){translationPanel.__comfortProxy=prefs.proxyAddress;proxyInput.value=prefs.proxyAddress;}
        var controls=context==='main'?document.getElementById('settings-input'):parent;
        if(controls){
            if(!shortcutPanel){shortcutPanel=document.createElement('section');shortcutPanel.id='comfort-keybinding';shortcutPanel.className='cu-only';shortcutPanel.setAttribute('data-context',context);controls.insertBefore(shortcutPanel,controls.firstChild);}
            var shortcutKey=[prefs.language,prefs.translationKey,capturingKey].join(':');
            if(shortcutPanel.__comfortKey!==shortcutKey){shortcutPanel.__comfortKey=shortcutKey;
            shortcutPanel.innerHTML='<div class="cu-setting"><div class="cu-setting-copy"><div class="cu-setting-title">'+t('translationKey')+'</div><p class="cu-setting-help">'+t('translationKeyHelp')+'</p></div><div class="cu-choices"><strong>'+prefs.translationKey.replace('Alpha','')+'</strong>'+button('translation-key',t(capturingKey?'pressKey':'changeKey'),'cu-choice')+'</div></div>';
            bindOwn(shortcutPanel);
            }
        }
    }
    function updateLocalModelState(code,percent,preparing){
        localModelStatus=[code,percent,preparing===true];if(!translationPanel)return;
        var status=translationPanel.querySelector('.cu-local-status');if(status)status.textContent=t(code)+(percent>=0?' '+Math.max(0,Math.min(100,percent))+'%':'');
        var busy=preparing===true && ['localRuntimeDownload','localModelDownload','localGpuDownload','localVerifying'].indexOf(code)>=0;
        var prepare=translationPanel.querySelector('[data-action="translation-local-prepare"]'),cancel=translationPanel.querySelector('[data-action="translation-local-cancel"]');
        if(prepare)prepare.disabled=busy;if(cancel)cancel.style.display=busy?'':'none';
    }
    function installTranslationInput(){
        document.addEventListener('focusin',function(event){if(event.target && /^(INPUT|TEXTAREA)$/.test(event.target.tagName))translationTool('typing');});
        document.addEventListener('focusout',function(){if(!capturingKey)translationTool('idle');});
        window.addEventListener('blur',function(){capturingKey=false;translationTool('idle');});
        document.addEventListener('keydown',function(event){
            if(!capturingKey)return;
            event.preventDefault();event.stopPropagation();
            var key=event.key;
            if(!key || key==='Unidentified'){
                var number=event.which || event.keyCode;
                key=number===27?'Escape':number>=112 && number<=123?'F'+(number-111):number>=65 && number<=90 || number>=48 && number<=57?String.fromCharCode(number):'';
            }
            if(key==='Escape'){capturingKey=false;translationTool('idle');renderTranslation();return;}
            var code=/^[a-z]$/i.test(key)?key.toUpperCase():/^\d$/.test(key)?'Alpha'+key:key;
            if(!/^(?:[A-Z]|Alpha[0-9]|F(?:[1-9]|1[0-2]))$/.test(code)){toast(t('keyRejected'));return;}
            capturingKey=false;var next=copy(prefs);next.translationKey=code;setPreferences(next,false);translationTool('idle');
        },true);
    }
    function installMessageLocalization(){
        ['uiAlertShow','uiAlertTimedShow','uiConfirmShow','uiLoadingShow','uiPushShow','createNotification'].forEach(function(name){
            var original=window[name];if(typeof original!=='function' || original.__comfortMessage)return;
            var wrapped=function(){var args=Array.prototype.slice.call(arguments);for(var i=0;i<Math.min(2,args.length);i++)if(typeof args[i]==='string')args[i]=messageText(args[i]);return original.apply(this,args);};
            wrapped.__comfortMessage=true;window[name]=wrapped;
        });
    }
    function messageText(value){
        var source=sourceText(value);if(source)return systemText(source);
        for(var i=0;i<valueTranslations.length;i++){var pattern=valueTranslations[i],match=pattern.pattern.exec(value.replace(/\s+/g,' ').trim());if(match)return systemText(pattern.source).replace('{value}',match[1]);}
        return value;
    }

    function bindOwn(container) {
        var controls=container.querySelectorAll('[data-action],[data-pref]');
        for(var i=0;i<controls.length;i++)controls[i].addEventListener('click',handleClick);
    }
    function toast(message) {
        if(!dock)return;
        var area=dock.querySelector('.cu-toast');if(!area)return;
        area.textContent=message;area.classList.add('cu-toast-visible');
        clearTimeout(currentToast);currentToast=setTimeout(function(){area.classList.remove('cu-toast-visible');},3500);
    }
    function originalClick(selector) {
        var target=document.querySelector(selector);
        if(!target || target.disabled || target.classList.contains('disabled') || target.classList.contains('noaction'))return false;
        var click=document.createEvent('MouseEvent');
        click.initMouseEvent('click',true,true,window,1,0,0,0,0,false,false,false,false,0,null);
        target.dispatchEvent(click);return true;
    }
    function openNativeHome() {
        if(context==='main') {
            if(nativeHome && nativeHome.classList.contains('in'))return true;
            return originalClick('.wrapper > .toolbar .tab_btn_home');
        }
        var menu=window.menu && window.menu.core?window.menu:window.quickmenu;
        if(!menu || !menu.core)return false;
        menu.core.switchCategorySelected('quickmenu-home');return true;
    }
    function openSettings() {
        if(context==='main') {
            if(!originalClick('.wrapper > .toolbar .tab_btn_settings'))return false;
            return originalClick('#settings .filter-option[onclick*="settings-ui"]');
        }
        return originalClick('#quickmenu-wrapper .toolbar-container .button-settings');
    }
    function setMode(enabled) {
        // Restore actual nodes before disabling our styles. The native page never loses its content.
        if(!enabled){restoreHome();restoreMicrophone();}
        shown=enabled;positionMicrophone();setAttributes();render();
        if(window.ComfortContent)window.ComfortContent.refresh();
        if(!openNativeHome())toast(t('unavailable'));
        report('mode',enabled?'comfort':'original');
        scheduleDiagnostics();
    }
    function handleClick(event) {
        var target=event.currentTarget;
        if(!target || target.disabled)return;
        if(event.stopPropagation)event.stopPropagation();
        var pref=target.getAttribute('data-pref'), action=target.getAttribute('data-action');
        report('click',pref || action);
        if(pref) {
            if(pref==='language' && target.parentNode.__skipClick)return;
            var picker=target.closest('.cu-picker');if(picker)closePicker(picker,false);
            var next=copy(prefs),value=target.getAttribute('data-value');
            next[pref]=typeof defaults[pref]==='string'?value:value==='true';
            if(pref==='palette' && palettes[value])['background','panel','accent'].forEach(function(key){next[key]=palettes[value][key];});
            setPreferences(next,false);if(picker && document.documentElement.contains(picker))picker.querySelector('.cu-picker-toggle').focus();return;
        }
        if(action && action.indexOf('select:')===0){var picker=target.closest('.cu-picker');if(picker.classList.contains('cu-picker-open'))closePicker(picker,false);else openPicker(picker);return;}
        if(action==='original'){setMode(false);return;}
        if(action==='resume'){setMode(true);return;}
        if(action==='palette-toggle'){paletteOpen=!paletteOpen;render();return;}
        if(action==='translation-proxy'){var field=document.getElementById('comfort-proxy-address'),value=field && field.value.trim();if(!/^https?:\/\/[^\s\/@?#]+(?::\d+)?\/?$/.test(value || '') || value.length>256){toast(t('proxy'));return;}var next=copy(prefs);next.proxyAddress=value;setPreferences(next,false);return;}
        if(action==='translation-config'){if(applyServiceConfiguration())toast(t('providerConfigApplied'));return;}
        if(action==='translation-save-key'||action==='translation-clear-key'){
            var keyField=document.getElementById('comfort-api-key'),key=action==='translation-clear-key'?'':keyField.value.trim();
            if(!validServiceValue(key,1024) || action==='translation-save-key'&&!key){toast(t('missingKey'));return;}
            if(!applyServiceConfiguration())return;
            keyField.value='';translationTool(JSON.stringify({action:'credential',provider:prefs.translationProvider,key:key}));return;
        }
        if(action==='translation-test'||action==='translation-cache'||action==='translation-chat'){translationTool(action==='translation-test'?'test':action==='translation-cache'?'clearCache':'chat');return;}
        if(action==='translation-local-prepare'||action==='translation-local-cancel'){if(action==='translation-local-prepare')updateLocalModelState('localVerifying',-1,true);translationTool(action==='translation-local-prepare'?'localPrepare':'localCancel');return;}
        if(action==='translation-key'){capturingKey=true;translationTool('typing');renderTranslation();return;}
        if(action==='reset'){setPreferences(defaults,false);return;}
        if(action.indexOf('colourpart:')===0){customPart=action.split(':')[1];render();return;}
        if(action.indexOf('step:')===0){colourStep=parseInt(action.split(':')[1],10);render();return;}
        if(action.indexOf('colour:')===0) {
            var parts=action.split(':'),base=palettes[prefs.palette]||prefs,nextColour=copy(prefs);
            ['background','panel','accent'].forEach(function(key){nextColour[key]=base[key];});
            var channels=rgb(base[customPart]),index=parseInt(parts[1],10);
            channels[index]=Math.max(0,Math.min(255,channels[index]+(parts[2]==='plus'?colourStep:-colourStep)));
            nextColour[customPart]=hex(channels);nextColour.palette='custom';setPreferences(nextColour,false);
        }
    }
    function attachSettings() {
        if(context!=='main' && context!=='quick')return;
        var parent=context==='main'?document.getElementById('settings-ui'):document.getElementById('CVRUI-QMUI-SettingsPage-Content');
        if(!parent)return;
        if(root && root.parentNode===parent)return;
        if(!root){root=document.createElement('section');root.id='comfort-ui';root.className='cu-only cu-settings-panel';root.setAttribute('data-context',context);}
        parent.insertBefore(root,parent.firstChild);
    }
    function moveHomeNode(node, parent) {
        var marker=document.createComment('Comfort UI original position');
        node.parentNode.insertBefore(marker,node);
        homeMoves.push({node:node,marker:marker});
        parent.appendChild(node);
    }
    function restoreMicrophone() {
        if(microphoneMove && microphoneMove.marker.parentNode)microphoneMove.marker.parentNode.replaceChild(microphoneMove.node,microphoneMove.marker);
        microphoneMove=null;
        if(microphone)microphone.style.display='none';
    }
    function positionMicrophone() {
        if(context!=='main')return;
        if(!shown){restoreMicrophone();return;}
        var wrapper=document.querySelector('.wrapper'), button=document.querySelector('.wrapper > .toolbar .action-mute');
        if(!wrapper || !button)return;
        if(!microphone){microphone=document.createElement('aside');microphone.id='comfort-microphone';wrapper.appendChild(microphone);}
        var marker=document.createComment('Comfort UI microphone original position');
        button.parentNode.insertBefore(marker,button);
        microphoneMove={node:button,marker:marker};
        microphone.appendChild(button);microphone.style.display='flex';
    }
    function restoreHome() {
        // Reverse order also restores buttons inside their original row wrappers.
        for(var i=homeMoves.length-1;i>=0;i--) {
            var move=homeMoves[i];
            if(move.marker.parentNode)move.marker.parentNode.replaceChild(move.node,move.marker);
        }
        homeMoves=[];
        homeGrids.forEach(function(grid){if(grid.parentNode)grid.parentNode.removeChild(grid);});
        homeGrids=[];homeActive=false;
        if(dashboard){dashboard.classList.remove('cu-home-active');dashboard.setAttribute('aria-hidden','true');}
    }
    function activateHome() {
        if(homeActive || !nativeHome || !dashboard)return homeActive;
        var body=dashboard.querySelector('.cu-dashboard-body');
        var selectors=context==='main'?['.home-content-root']:['.primary-buttons-container','.quick-buttons-container','.notification-container','.game-information-container'];
        var sections=selectors.map(function(selector){return nativeHome.querySelector(selector);});
        if(sections.some(function(section){return !section;}))return false;
        try {
            if(context==='quick') {
                // Move, never clone: the existing callbacks, toggles and disabled state are preserved.
                for(var s=0;s<2;s++) {
                    var section=sections[s], grid=document.createElement('div');
                    grid.className='cu-native-grid';
                    var buttons=section.querySelectorAll('.row > .quick-button');
                    if(buttons.length<4)throw new Error('Quick Menu actions not ready');
                    section.appendChild(grid);homeGrids.push(grid);
                    for(var b=0;b<buttons.length;b++)moveHomeNode(buttons[b],grid);
                }
            }
            sections.forEach(function(section){moveHomeNode(section,body);});
            homeActive=true;dashboard.classList.add('cu-home-active');dashboard.setAttribute('aria-hidden','false');
            return true;
        } catch(error) {
            restoreHome();report('home-restore',String(error));return false;
        }
    }
    function decorateMenus() {
        if(context!=='main' && context!=='quick')return;
        positionMicrophone();
        var home=document.querySelector(context==='main'?'#home':'.quickmenu-home');
        if(home && home!==nativeHome) {
            restoreHome();
            if(dashboard && dashboard.parentNode)dashboard.parentNode.removeChild(dashboard);
            dashboard=null;nativeHome=home;
        }
        if(nativeHome && !dashboard) {
            dashboard=document.createElement('section');dashboard.id='comfort-dashboard';dashboard.className='cu-only';
            dashboard.setAttribute('data-context',context);dashboard.setAttribute('aria-hidden','true');
            dashboard.innerHTML='<header class="cu-dashboard-heading"></header><div class="cu-dashboard-body"></div>';
            nativeHome.appendChild(dashboard);renderDashboard();scheduleDiagnostics();
        }
        attachSettings();
        enhanceInteraction();
    }
    function enhanceInteraction() {
        if(context==='keyboard' || context==='hud')return;
        var controls=document.querySelectorAll('.button,.quick-button,.filter-option,.container-back,.content-debug .content-caption,.detail .tab-btn,#world-instance-create .region-select');
        for(var i=0;i<controls.length;i++) {
            var control=controls[i];
            if(control.tagName==='BUTTON' || control.tagName==='INPUT' || control.tagName==='A' || control.getAttribute('data-comfort-keyboard')==='true')continue;
            // A native control remains the single action target for mouse, VR ray and keyboard.
            if(!control.hasAttribute('tabindex'))control.setAttribute('tabindex','0');
            if(!control.hasAttribute('role'))control.setAttribute('role','button');
            control.setAttribute('data-comfort-keyboard','true');
            control.addEventListener('keydown',function(event){
                if(event.target!==event.currentTarget || event.repeat || (event.key!=='Enter' && event.key!==' '))return;
                var target=event.currentTarget;
                if(target.disabled || target.classList.contains('disabled') || target.classList.contains('noaction') || target.getAttribute('aria-disabled')==='true')return;
                event.preventDefault();event.stopPropagation();
                var click=document.createEvent('MouseEvent');
                click.initMouseEvent('click',true,true,window,1,0,0,0,0,false,false,false,false,0,null);target.dispatchEvent(click);
            });
        }
    }
    function scheduleDiagnostics() {
        if(!mounted || diagnosticTimer)return;
        diagnosticTimer=setTimeout(function(){
            diagnosticTimer=null;
            if(!shown){report('layout-skipped','original-mode');return;}
            var menuPage=context==='main' || context==='quick';
            var target=menuPage?dashboard:context==='login'?document.getElementById('login-container'):context==='keyboard'?document.querySelector('.keyboard'):document.querySelector('.hud-wrapper-left-bottom');
            if(!target){report('layout-skipped','not-ready');return;}
            if(menuPage && (nativeHome.classList.contains('hidden') || nativeHome.classList.contains('hide') || nativeHome.classList.contains('out') || window.getComputedStyle(nativeHome).visibility==='hidden')){report('layout-skipped','inactive-page');return;}
            var pageBounds=(menuPage?nativeHome:target).getBoundingClientRect();
            // Hidden ancestors and inactive game surfaces are normal during preference broadcasts.
            if(pageBounds.width<1 || pageBounds.height<1){report('layout-skipped','inactive-surface');return;}
            var bounds=target.getBoundingClientRect(),style=window.getComputedStyle(target),valid=true,hit=null;
            if(menuPage) {
                var opaque=style.backgroundColor && style.backgroundColor!=='transparent' && !/rgba\([^)]*,\s*0(?:\.0+)?\s*\)/.test(style.backgroundColor);
                valid=style.position==='absolute' && style.display==='flex' && opaque && bounds.width>=pageBounds.width*0.85 && bounds.height>=pageBounds.height*0.85;
                // Validate the shell before moving live content into it. A failed shell leaves native UI intact.
                if(valid && !homeActive)valid=activateHome();
                if(valid) {
                    var candidates=dashboard.querySelectorAll(context==='main'?'.action-btn.button':'.quick-button');
                    var visibleTarget=false;
                    valid=candidates.length>0;
                    for(var i=0;i<candidates.length;i++) {
                        if(candidates[i].classList.contains('disabled'))continue;
                        var rect=candidates[i].getBoundingClientRect(),computed=window.getComputedStyle(candidates[i]);
                        if(rect.width<1 || rect.height<1 || computed.visibility==='hidden' || rect.top<bounds.top || rect.bottom>bounds.bottom)continue;
                        visibleTarget=true;valid=rect.width>=60 && rect.height>=60;
                        if(typeof document.elementFromPoint==='function') {
                            var element=document.elementFromPoint(rect.left+rect.width/2,rect.top+rect.height/2);
                            // A game confirmation or loading overlay can legitimately cover a button.
                            // Report occlusion; only structural failures may restore the original UI.
                            hit=!!element && (element===candidates[i] || candidates[i].contains(element));
                        }
                        break;
                    }
                    valid=valid && visibleTarget;
                }
            }
            compatibilityReady=valid;
            report('layout',{version:'0.5.2',width:Math.round(bounds.width),height:Math.round(bounds.height),background:style.backgroundColor,font:style.fontFamily,home:homeActive,hit:hit,valid:!!valid});
            if(!valid && ++layoutFailures>=2) {
                restoreHome();restoreMicrophone();shown=false;setAttributes();render();openNativeHome();
                report('compatibility-fallback','Original UI restored; return control retained.');
            } else if(valid)layoutFailures=0;
            else scheduleDiagnostics();
        },240);
    }
    function scheduleRefresh(records) {
        var needed=false;
        for(var i=0;i<records.length;i++) {
            var node=records[i].target.nodeType===3?records[i].target.parentNode:records[i].target;
            if(!node || (node.closest && node.closest('#comfort-ui,#comfort-translation,#comfort-keybinding,[data-comfort-translation-category],#comfort-dock,.cu-dashboard-heading,.cu-transition-picker')))continue;
            if(context==='hud')continue;
            if(records[i].type==='attributes') {
                if(node.classList && (node.classList.contains('content') || node.classList.contains('menu-category')))scheduleDiagnostics();
                if(context==='quick' && node.classList && node.classList.contains('recalibrateAndSeatedPlay'))needed=true;
                continue;
            }
            if(node.closest && node.closest('.game-debug-ping,.game-debug-fps,.game-debug-inVr,.game-debug-version,.game-debug-branch,.game-gs-uptime,.game-gs-timein,.game-max-prop'))continue;
            if(node.closest && node.closest('[data-comfort-system-label]')){
                if(!quickLabelTimer)quickLabelTimer=setTimeout(function(){quickLabelTimer=null;localizeQuickHome();},40);
                continue;
            }
            if(isExcluded(node))continue;
            // Native textContent refreshes do not require a document scan or layout decoration.
            if(records[i].type==='characterData'){translateNode(records[i].target);continue;}
            var added=records[i].addedNodes,removed=records[i].removedNodes,hasElements=false;
            for(var n=0;n<added.length;n++)if(added[n].nodeType===1){hasElements=true;break;}
            if(!hasElements)for(var r=0;r<removed.length;r++)if(removed[r].nodeType===1){hasElements=true;break;}
            if(!hasElements){
                for(var t=0;t<node.childNodes.length;t++)if(node.childNodes[t].nodeType===3)translateNode(node.childNodes[t]);
                translateAttributes(node);continue;
            }
            needed=true;break;
        }
        if(needed && !refreshTimer)refreshTimer=setTimeout(function(){
            refreshTimer=null;
            var oldParent=root && root.parentNode, oldDashboard=dashboard;decorateMenus();
            if((root && oldParent!==root.parentNode) || oldDashboard!==dashboard)render();
            translateCommon();
        },100);
    }
    function installHudMotion() {
        // Native status icons attach their animationend listener after 2s. A flat 2.4s animation
        // in the stylesheet preserves that lifecycle while visually disabling motion.
        if(context!=='hud')return;
        document.documentElement.setAttribute('data-comfort-hud','true');
    }
    function hudHook(name,after) {
        var original=window[name];if(typeof original!=='function')return;
        var wrapped=function(){var result=original.apply(this,arguments);try{after.apply(this,arguments);}catch(ignored){disableHudReplacement();}return result;};
        window[name]=wrapped;hudHooks.push({name:name,original:original,wrapped:wrapped});
    }
    function disableHudReplacement(){
        document.documentElement.removeAttribute('data-comfort-hud-ready');
        document.documentElement.removeAttribute('data-comfort-hud-stage');
        if(hud){clearTimeout(hud.verifyTimer);hud.verifyTimer=0;hud.ready=false;hud.mic.style.display='none';hud.mic.style.visibility='hidden';if(hud.rail)hud.rail.style.display='none';}
    }
    function queueHudVerification(reset){
        if(!hud || !shown)return;
        if(reset){hud.verifyAttempts=0;hud.verifyFailed=false;}
        if(hud.verifyFailed || hud.verifyTimer)return;
        hud.verifyTimer=setTimeout(function(){hud.verifyTimer=0;verifyHudReplacement();},80);
    }
    function verifyHudReplacement(){
        if(!hud)return;var html=document.documentElement;
        try{
            if(!shown){disableHudReplacement();return;}
            if(!html.contains(hud.mic) || !hud.nativeLive || !hud.nativeTts || !hud.icon.querySelector('.cu-mic-capsule') || !hud.icon.querySelector('.cu-mic-slash')){disableHudReplacement();return;}
            // Cohtml may defer the first layout after inserting a stylesheet. Stage
            // the complete card invisibly while the original microphone stays visible.
            // A bounded startup/resize retry replaces the former one-shot zero-size fail.
            html.setAttribute('data-comfort-hud-stage','probe');
            hud.mic.style.display='flex';if(!hud.ready)hud.mic.style.visibility='hidden';
            var glyph=hud.icon.querySelector('.cu-mic-capsule').getBoundingClientRect(),card=hud.mic.getBoundingClientRect();
            // A small render texture or the user's HUD scale is still a valid layout.
            // Fixed 2px/12px thresholds rejected working miniature HUDs as broken.
            if(glyph.width<=0 || glyph.height<=0 || card.width<=0 || card.height<=0 || glyph.width>card.width || glyph.height>card.height){
                html.removeAttribute('data-comfort-hud-ready');hud.ready=false;hud.mic.style.visibility='hidden';if(hud.rail)hud.rail.style.display='none';
                hud.verifyAttempts=(hud.verifyAttempts||0)+1;
                if(hud.verifyAttempts<12)queueHudVerification(false);
                else {disableHudReplacement();hud.verifyFailed=true;report('hud-replacement',{ready:false,reason:'layout-not-ready',attempts:12});}
                return;
            }
            html.setAttribute('data-comfort-hud-ready','true');html.removeAttribute('data-comfort-hud-stage');
            hud.mic.style.visibility='visible';if(hud.rail)hud.rail.style.display='flex';
            if(!hud.ready)report('hud-replacement',{ready:true,attempts:(hud.verifyAttempts||0)+1});
            hud.ready=true;hud.verifyAttempts=0;
        }catch(ignored){disableHudReplacement();}
    }
    function localizeHudMessage() {
        if(!hud || !hud.message)return;
        var card=hud.message,head=card.querySelector('.authorative'),title=card.querySelector('.headline');
        var sourceHead=sourceText(head?head.textContent:''),sourceTitle=sourceText(title?title.textContent:'');
        var trusted=['(Local) Client','(Global) Client','(Synced) Client','(Global) Server','(Local) Voice Server','Voice Server','(Local) Discord'].indexOf(sourceHead)>=0;
        var kind=trusted && (sourceTitle==='Connected to Instance' || sourceTitle==='Disconnected from Instance')?'connection':'notification';
        if(card.getAttribute('data-comfort-notice')!==kind)card.setAttribute('data-comfort-notice',kind);
        if(!trusted)return;
        ['.authorative','.headline','.message'].forEach(function(selector){
            var slot=card.querySelector(selector);if(!slot)return;
            // The client's Cohtml lacks createTreeWalker. Walk basic DOM links only,
            // keeping the native markup, event handlers and original text-node records.
            var pending=[slot];
            while(pending.length){
                var node=pending.pop();
                if(node.nodeType===3){
                    var raw=node.nodeValue,record=hudTextSources.get(node);
                    if(!record || raw!==record.applied){record={source:raw};hudTextSources.set(node,record);}
                    var next=messageText(record.source);record.applied=next;
                    if(raw!==next)node.nodeValue=next;
                }else if(node.nodeType===1 && !/^(script|style)$/i.test(node.tagName)){
                    for(var child=node.lastChild;child;child=child.previousSibling)pending.push(child);
                }
            }
        });
    }
    function tryLocalizeHudMessage(){
        // Notification decoration is optional. It must not abort native event delivery
        // or the actual microphone state/level refresh; retry on the next native update.
        try{localizeHudMessage();}catch(ignored){}
    }
    function refreshHud() {
        try{refreshHudContents();if(hud && !hud.ready)queueHudVerification(false);return true;}catch(ignored){disableHudReplacement();return false;}
    }
    function refreshHudContents() {
        if(!hud)return;
        if(prefs.reducedMotion || !shown || !prefs.theme || document.hidden)finishHudMotion();
        // The native voice manager snapshot wins over a stale HUD after joining/reloading.
        // DOM state is only a fallback before the manager is ready (and in browser preview).
        var live=hud.nativeLive.style.display!=='none',tts=/tts/i.test(hud.nativeTts.getAttribute('src')||'');
        var state=hudMicrophoneState || (live?(tts?'tts':'live'):'muted'),colour=colourValues?colourValues['--cu-text']:'#fafbfc';
        live=state!=='muted';tts=state==='tts';
        if(hud.mic.getAttribute('data-state')!==state)hud.mic.setAttribute('data-state',state);
        var label=t(live?(tts?'hudTts':'hudMicLive'):'hudMicMuted');
        if(hud.label.textContent!==label)hud.label.textContent=label;
        if(hud.iconKey!==state+colour){
            // Basic HTML boxes render without an image decoder, data URL or SVG repaint.
            changeHudIcon(state,!!hud.iconKey && hud.state!==state);hud.state=state;
            hud.iconKey=state+colour;
        }
        if(hud.mic.getAttribute('aria-label')!==label)hud.mic.setAttribute('aria-label',label);
        hud.badges.forEach(function(item){
            if(item.colour!==colour){item.icon.style.filter=colour==='#12161b'?'brightness(0)':'brightness(0) invert(1)';item.colour=colour;}
            var text=t(item.label);if(item.badge.getAttribute('title')!==text){item.badge.setAttribute('title',text);item.badge.setAttribute('aria-label',text);}
            var active=item.native.classList.contains('active') && !item.native.classList.contains('hidden');
            var display=active?'flex':'none';if(item.badge.style.display!==display)item.badge.style.display=display;
        });
        refreshHudMeter();
        tryLocalizeHudMessage();
    }
    var hudMicrophoneState=/^(muted|live|tts)$/.test(window.__comfortMicState||'')?window.__comfortMicState:null;
    var hudVoiceLevel=null,hudLevelTimer=0,hudLevelReceived=0;
    function stopHudLevelTimer(){clearTimeout(hudLevelTimer);hudLevelTimer=0;}
    function expireHudLevel(){
        var remaining=1500-(Date.now()-hudLevelReceived);
        if(remaining>0){hudLevelTimer=setTimeout(expireHudLevel,remaining);return;}
        hudLevelTimer=0;hudVoiceLevel=0;refreshHudMeter();
    }
    function updateHudMicrophoneLevel(percent){
        if(typeof percent!=='number' || !isFinite(percent))return;
        percent=Math.max(0,Math.min(100,Math.round(percent)));hudLevelReceived=Date.now();
        if(percent>0){if(!hudLevelTimer)hudLevelTimer=setTimeout(expireHudLevel,1500);}else stopHudLevelTimer();
        if(hudVoiceLevel===percent)return;
        hudVoiceLevel=percent;refreshHudMeter();
    }
    function updateHudMicrophone(state){
        if(state!=='muted'&&state!=='live'&&state!=='tts')return;
        if(hudMicrophoneState===state && hud && hud.state===state)return;
        if(hudVoiceLevel!==null){hudVoiceLevel=0;stopHudLevelTimer();}
        hudMicrophoneState=state;refreshHud();
    }
    function setHudGlyph(node,state){
        node.setAttribute('data-glyph',state);
        var slash=node.querySelector('.cu-mic-slash'),tts=node.querySelector('.cu-mic-tts');
        // Set the actual parts directly instead of relying on selector invalidation.
        slash.style.display=state==='muted'?'block':'none';slash.style.visibility=state==='muted'?'visible':'hidden';
        tts.style.display=state==='tts'?'block':'none';tts.style.visibility=state==='tts'?'visible':'hidden';
    }
    function finishHudMotion(){if(!hud)return;if(hud.frame)window.cancelAnimationFrame(hud.frame);clearTimeout(hud.motionTimer);hud.motionTimer=0;hud.frame=0;hud.previous.style.display='none';hud.previous.style.visibility='hidden';hud.previous.style.opacity='0';hud.icon.style.opacity='1';hud.icon.style.transform='scale(1)';}
    function changeHudIcon(state,animate){
        var previous=hud.icon.getAttribute('data-glyph');finishHudMotion();setHudGlyph(hud.icon,state);
        if(!animate || !previous || prefs.reducedMotion || !shown || !prefs.theme || document.hidden)return;
        setHudGlyph(hud.previous,previous);hud.previous.style.display='block';hud.previous.style.visibility='visible';hud.previous.style.opacity='1';hud.icon.style.opacity='0';hud.icon.style.transform='scale(0.92)';
        var start=segmentTime(),duration=prefs.motionSpeed==='fast'?150:prefs.motionSpeed==='gentle'?280:210;
        function step(){var progress=Math.min(1,(segmentTime()-start)/duration),ease=1-Math.pow(1-progress,3);hud.icon.style.opacity=String(ease);hud.icon.style.transform='scale('+(0.92+0.08*ease)+')';hud.previous.style.opacity=String(1-ease);if(progress<1)hud.frame=window.requestAnimationFrame(step);else finishHudMotion();}
        hud.frame=window.requestAnimationFrame(step);
        // A paused/throttled animation frame must never leave the old muted layer visible.
        hud.motionTimer=setTimeout(finishHudMotion,duration+80);
    }
    function hudMicGlyph(className){return '<span class="'+className+'" aria-hidden="true"><i class="cu-mic-capsule"></i><i class="cu-mic-cradle"></i><i class="cu-mic-stem"></i><i class="cu-mic-foot"></i><i class="cu-mic-slash"></i><i class="cu-mic-tts">T</i></span>';}
    function refreshHudMeter(){
        if(!hud)return;
        var level=hudVoiceLevel!==null?hudVoiceLevel:parseFloat(hud.nativeMeter?hud.nativeMeter.style.height:0);
        level=hud.state==='live' && isFinite(level)?Math.max(0,Math.min(100,level)):0;
        var value='scaleX('+(level/100)+')';
        if(hud.meter.style.transform!==value)hud.meter.style.transform=value;
        var opacity=hud.state==='live'?'1':'0';
        if(hud.meter.parentNode.style.opacity!==opacity)hud.meter.parentNode.style.opacity=opacity;
        // A green glyph + an actual level bar indicate voice input; never fake a pulse.
        var speaking=hud.state==='live' && (level>1 || (level>0 && hud.speaking)),colours=colourValues||{};
        hud.speaking=!!speaking;
        var key=[hud.speaking,colours['--cu-hud-speaking'],colours['--cu-hud-speaking-bg'],prefs.reducedMotion].join(':');
        if(hud.feedbackKey===key)return;
        hud.feedbackKey=key;
        var colour=speaking?colours['--cu-hud-speaking']:'',background=speaking?colours['--cu-hud-speaking-bg']:'';
        hud.mic.setAttribute('data-speaking',speaking?'true':'false');
        hud.mic.style.borderColor=colour;hud.mic.style.backgroundColor=background;
        hud.glyphBorders.forEach(function(node){node.style.borderColor=colour;node.style.color=colour;});
        hud.glyphFills.forEach(function(node){node.style.backgroundColor=colour;});
        hud.meter.style.backgroundColor=colours['--cu-hud-speaking']||'';
        hud.meter.style.transition=prefs.reducedMotion?'none':'transform 80ms linear';
    }
    function installHud() {
        if(context!=='hud' || hud)return;
        var wrapper=document.querySelector('.hex-hub-wrapper'),shards=document.querySelector('.shards');
        if(!wrapper || !shards || !wrapper.querySelector('.hub-unmuted') || !wrapper.querySelector('.hub-unmuted-image'))return;
        var mic=document.createElement('div');mic.className='cu-hud-mic';mic.setAttribute('aria-live','off');mic.style.display='none';mic.style.visibility='hidden';
        mic.innerHTML='<span class="cu-hud-mic-symbol">'+hudMicGlyph('cu-hud-mic-previous')+hudMicGlyph('cu-hud-mic-icon')+'</span><span class="cu-hud-mic-label"></span><div class="cu-hud-meter"><span></span></div>';
        wrapper.appendChild(mic);
        var rail=document.createElement('div');rail.className='cu-hud-badges';rail.style.display='none';shards.appendChild(rail);
        var nativeLive=wrapper.querySelector('.hub-unmuted'),nativeTts=wrapper.querySelector('.hub-unmuted-image');
        hud={mic:mic,rail:rail,icon:mic.querySelector('.cu-hud-mic-icon'),previous:mic.querySelector('.cu-hud-mic-previous'),label:mic.querySelector('.cu-hud-mic-label'),meter:mic.querySelector('.cu-hud-meter span'),message:document.querySelector('.hud-message'),nativeLive:nativeLive,nativeTts:nativeTts,nativeMeter:wrapper.querySelector('.hub-muted-bar .bar-content'),glyphBorders:Array.prototype.slice.call(mic.querySelectorAll('.cu-mic-capsule,.cu-mic-cradle,.cu-mic-tts')),glyphFills:Array.prototype.slice.call(mic.querySelectorAll('.cu-mic-stem,.cu-mic-foot,.cu-mic-slash')),badges:[]};
        ['infos','friends','notifications','comms','votes','alerts'].forEach(function(name){
            var native=shards.querySelector('.shard-'+name);if(!native)return;
            var badge=document.createElement('span');badge.className='cu-hud-badge';
            badge.innerHTML='<img alt=""/>';
            var badgeIcon=badge.querySelector('img');badgeIcon.src=native.getAttribute('src');
            rail.appendChild(badge);hud.badges.push({native:native,badge:badge,icon:badgeIcon,label:'hud'+name});
        });
        hudHook('renderHudMessage',tryLocalizeHudMessage);
        hudHook('changeHudStatus',refreshHud);
        hudHook('TimeoutShard',refreshHud);
        hudHook('setMicLevel',refreshHudMeter);
        // Observe only native state attributes; no document scans or per-frame style/layout reads.
        hudObserver=new MutationObserver(function(records){var changed=false,meter=false,visibility=false;records.forEach(function(r){if(r.target===hud.nativeMeter)meter=true;else changed=true;if(r.target===wrapper)visibility=true;});if(changed)refreshHud();if(meter)refreshHudMeter();if(visibility && !hud.ready)queueHudVerification(true);});
        [wrapper,nativeLive,nativeTts,wrapper.querySelector('.hub-muted'),hud.nativeMeter].concat(hud.badges.map(function(item){return item.native;})).forEach(function(node){if(node)hudObserver.observe(node,{attributes:true,attributeFilter:['style','src','class']});});
        if(hud.message)hudObserver.observe(hud.message,{childList:true,subtree:true,characterData:true});
        if(window.engine && typeof engine.on==='function')engine.on('SetHudScale',function(scale,fillX,fillY){scale=Number(scale);fillY=Number(fillY);if(isFinite(scale)&&scale>0&&isFinite(fillY)&&fillY>0){hudScale={scale:scale,fill:fillY};applyPalette();queueHudVerification(true);}});
        window.addEventListener('resize',function(){applyPalette();queueHudVerification(true);});
        if(refreshHud())queueHudVerification(true);
    }
    function mount(attempt) {
        if(mounted)return;
        context=detectContext();
        var anchor=context==='main'?document.querySelector('.wrapper > .toolbar'):context==='quick'?document.getElementById('quickmenu-wrapper'):context==='login'?document.getElementById('login-container'):context==='keyboard'?document.querySelector('.keyboard'):document.querySelector('.hud-area-left-bottom');
        if(!anchor){if(attempt<80)setTimeout(function(){mount(attempt+1);},250);return;}
        mounted=true;localSave();setAttributes();rebuildDictionary();rememberStatic(document.body);
        if(context==='main'||context==='quick') {
            dock=document.createElement('aside');dock.id='comfort-dock';dock.setAttribute('data-context',context);
            (context==='main'?document.querySelector('.wrapper'):anchor).appendChild(dock);
            decorateMenus();
        }
        installTranslationInput();installHudMotion();installHud();enhanceInteraction();translateCommon();decorateNativeSettings();render();scheduleDiagnostics();
        if(context==='quick' && window.engine && typeof engine.on==='function') {
            ['LoadMessages','ClearMessages','ClearAllMessages'].forEach(function(name){engine.on(name,localizeQuickHome);});
        }
        observer=new MutationObserver(scheduleRefresh);
        if(context!=='hud')observer.observe(document.body,{childList:true,subtree:true,characterData:true,attributes:true,attributeFilter:['class']});
        window.addEventListener('storage',function(e){if(e.key===KEY&&e.newValue)try{setPreferences(JSON.parse(e.newValue),true);}catch(ignored){}});
        window.addEventListener('beforeunload',function(){observer.disconnect();if(hudObserver)hudObserver.disconnect();finishHudMotion();stopHudLevelTimer();clearTimeout(refreshTimer);clearTimeout(quickLabelTimer);clearTimeout(diagnosticTimer);clearTimeout(currentToast);hudHooks.forEach(function(hook){if(window[hook.name]===hook.wrapped)window[hook.name]=hook.original;});});
        document.addEventListener('visibilitychange',function(){if(document.hidden){finishSegmentMotions();finishHudMotion();}});
        report('ready',{version:'0.5.2',settings:!!root,dashboard:!!dashboard});
    }
    try{var stored=localStorage.getItem(KEY);if(stored)prefs=validate(JSON.parse(stored));}catch(ignored){}
    if(host.preferences)prefs=validate(host.preferences);
    window.ComfortUI={
        version:'0.5.2',hudMicrophone:updateHudMicrophone,hudMicrophoneLevel:updateHudMicrophoneLevel,localModelState:updateLocalModelState,translationNotice:function(code,enabled){if(typeof enabled==='boolean')updateChatState(enabled);translationStatus=code;if(translationPanel){var status=translationPanel.querySelector('.cu-translation-status');if(status)status.textContent=t(code);}toast(t(code));},chatState:updateChatState,updatePreferences:setPreferences,getPreferences:function(){return copy(prefs);},
        translationErrorText:function(code){return labels.en[code]?t(code):t('network');},
        preferenceFailed:function(){saveStatus='saveFailed';render();},
        showPreviewNotice:toast,setMode:setMode,
        openSettings:openSettings,systemText:systemText,detailPrefix:detailPrefix,splitDetailHeading:splitDetailHeading
    };
    if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',function(){mount(0);});else mount(0);
})();
