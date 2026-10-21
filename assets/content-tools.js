(function () {
    'use strict';
    if(window.ComfortContent)return;
    var initialized=false,media=[],states={},sequence=0,timer,observer,language,mediaDirty=true,layoutDirty=true,mediaEnabled,lastLayoutKey;
    var mediaSelector='#avatars .content-cell .image-holder > img.content-image,#worlds .content-cell .image-holder > img.content-image,#props .content-cell .image-holder > img.content-image,.detail .avatar-image,.detail .prop-image,#world-detail .world-image.data-worldImage,#instance-detail .content-instance-world .world-image';
    var specifications={
        'world-detail':{description:'.data-description',name:'WorldName'},
        'avatar-detail':{description:'.description',name:'AvatarName'},
        'prop-detail':{description:'.description',name:'SpawnableName'},
        'user-detail':{description:'.bio-block',bio:true},
        'our-user-detail':{description:'.bio-block',bio:true}
    };
    var words={zh:{translate:'翻译名称与简介',bio:'翻译简介',original:'显示原文',pending:'翻译中 · 点击取消',failed:'翻译失败 · 点击重试',empty:'暂无可翻译内容',hint:'按需联网翻译成简体中文。仅发送本页名称和简介；MyMemory 免费服务有额度限制。',network:'翻译服务暂不可用，已保留原文。',quota:'翻译额度暂时用完，已保留原文。',busy:'已有翻译正在处理，请稍后重试。'},en:{translate:'Translate name & info',bio:'Translate bio',original:'Show original',pending:'Translating · cancel',failed:'Failed · retry',empty:'No text to translate',hint:'Translate into English on demand. Only this name and description are sent to MyMemory; free limits apply.',network:'Translation is unavailable. The original text is preserved.',quota:'The translation allowance is exhausted. Original text is preserved.',busy:'Other translations are in progress. Try again shortly.'}};
    function prefs(){return window.ComfortUI?window.ComfortUI.getPreferences():{language:'zh'}}
    function enabled(){return document.documentElement.getAttribute('data-comfort-enabled')==='true'}
    function text(key){var code=prefs().language,pack=(window.__comfortLocales || {})[code];if(key==='hint' && pack && pack.labels)return pack.labels.translationHint;return ((words[code] || (pack && pack.content) || words.en)[key]) || words.en[key] || key;}
    function showMessage(message){if(window.ComfortUI)window.ComfortUI.showPreviewNotice(message)}
    function connected(node){return document.documentElement.contains(node)}
    function schedule(){if(!timer)timer=setTimeout(function(){timer=null;refresh()},40)}
    function markLayout(){layoutDirty=true;schedule()}
    function mediaMutated(records){
        for(var i=0;i<records.length;i++){
            var record=records[i],node=record.target;
            if(record.type==='attributes'){
                if(record.attributeName==='src' && node.tagName==='IMG'){
                    // Update the changed item before paint, so its old texture cannot linger
                    // beside a newly loaded name while the batched refresh is pending.
                    if(node.__comfortMedia)fit(node.__comfortMedia);else{mediaDirty=true;layoutDirty=true}
                    continue;
                }
                if(record.attributeName==='class' && node.classList && node.classList.contains('content'))layoutDirty=true;
                continue;
            }
            var changed=Array.prototype.slice.call(record.addedNodes).concat(Array.prototype.slice.call(record.removedNodes));
            for(var n=0;n<changed.length;n++)if(changed[n].nodeType===1 && (changed[n].tagName==='IMG'||changed[n].querySelector('img.content-image,.avatar-image,.prop-image,.data-worldImage'))){mediaDirty=true;layoutDirty=true;break}
        }
        if(mediaDirty||layoutDirty)schedule();
    }

    // Cohtml does not expose naturalWidth/naturalHeight. Its background renderer can
    // contain the same cached texture without those DOM properties or object-fit.
    function restoreBackground(entry,force){
        if(!entry.background && !force)return;
        var style=entry.frame.style;
        style.backgroundImage=entry.originalBackground.image;style.backgroundSize=entry.originalBackground.size;
        style.backgroundPosition=entry.originalBackground.position;style.backgroundRepeat=entry.originalBackground.repeat;
        entry.background=false;entry.source=null;
    }
    function textureBackground(entry){
        var source=entry.image.getAttribute('src')||'',style=entry.frame.style;
        if(!source || /[\r\n\f\x00]/.test(source)){restoreBackground(entry);return false}
        if(!entry.background || entry.source!==source){
            style.backgroundImage='none';
            style.backgroundImage='url("'+source.replace(/\\/g,'\\\\').replace(/"/g,'\\"')+'")';
            style.backgroundSize='contain';style.backgroundPosition='center';style.backgroundRepeat='no-repeat';
            entry.background=true;entry.source=source;
        }
        if(!style.backgroundImage || style.backgroundImage==='none' || style.backgroundSize!=='contain'){
            restoreBackground(entry);return false;
        }
        return true;
    }
    function fit(entry){
        if(!enabled()||!connected(entry.image))return;
        var frame=entry.frame,image=entry.image,w=frame.clientWidth||frame.offsetWidth;
        if(!isFinite(w)||w<8)return;
        if(!entry.detail)frame.style.height=Math.round(w)+'px';
        var h=frame.clientHeight,nw=image.naturalWidth,nh=image.naturalHeight;
        if(!isFinite(h)||h<8)return;
        image.style.bottom='auto';image.style.right='auto';image.style.position='absolute';
        if(!isFinite(nw)||!isFinite(nh)||nw<=0||nh<=0){
            // Always give the original element a concrete box, even before loading
            // or if the background property is rejected. Its native events stay on it.
            image.style.width=Math.floor(w)+'px';image.style.height=Math.floor(h)+'px';
            image.style.left='0px';image.style.top='0px';
            image.style.opacity=textureBackground(entry)?'0':entry.originalOpacity;
            frame.setAttribute('data-comfort-media-mode',entry.background?'texture':'native');
            return;
        }
        restoreBackground(entry);image.style.opacity=entry.originalOpacity;
        var scale=Math.min(w/nw,h/nh),iw=Math.max(1,Math.min(Math.floor(w),Math.round(nw*scale))),ih=Math.max(1,Math.min(Math.floor(h),Math.round(nh*scale)));
        image.style.width=iw+'px';image.style.height=ih+'px';
        image.style.left=Math.round((w-iw)/2)+'px';image.style.top=Math.round((h-ih)/2)+'px';
        frame.setAttribute('data-comfort-media-mode','pixels');
    }
    function restoreMedia(){
        media.forEach(function(entry){
            restoreBackground(entry,true);entry.frame.removeAttribute('data-comfort-media-mode');
            entry.image.style.cssText=entry.originalImage;
            if(entry.detail){if(entry.frame.parentNode){entry.frame.parentNode.insertBefore(entry.image,entry.frame);entry.frame.parentNode.removeChild(entry.frame)}}
            else entry.frame.style.height=entry.originalHeight;
            delete entry.image.__comfortMedia;
        });media=[];
    }
    function scanMedia(){
        if(!enabled()){restoreMedia();return}
        if(!mediaDirty && !layoutDirty)return;
        media=media.filter(function(entry){return connected(entry.image)});
        var images=mediaDirty?document.querySelectorAll(mediaSelector):[];
        for(var i=0;i<images.length;i++){
            var image=images[i],entry=image.__comfortMedia;
            if(entry && media.indexOf(entry)<0)media.push(entry);
            if(!entry){
                var detail=!image.classList.contains('content-image'),frame=image.parentNode;
                if(detail){frame=document.createElement('div');frame.className='cu-media-frame';image.parentNode.insertBefore(frame,image);frame.appendChild(image)}
                entry={image:image,frame:frame,detail:detail,originalImage:image.style.cssText,originalOpacity:image.style.opacity,originalHeight:frame.style.height,
                    originalBackground:{image:frame.style.backgroundImage,size:frame.style.backgroundSize,position:frame.style.backgroundPosition,repeat:frame.style.backgroundRepeat}};
                image.__comfortMedia=entry;media.push(entry);
                if(!image.__comfortMediaLoad){image.__comfortMediaLoad=true;image.addEventListener('load',function(){if(this.__comfortMedia)fit(this.__comfortMedia)})}
            }
        }
        mediaDirty=false;layoutDirty=false;
        var visiblePages=[],inactivePages=[];
        media.forEach(function(entry){
            var page=entry.image.closest('.content');
            if(page){
                if(inactivePages.indexOf(page)>=0)return;
                if(visiblePages.indexOf(page)<0){
                    if(page.classList.contains('hidden')||page.classList.contains('out')||page.classList.contains('hide')){inactivePages.push(page);return}
                    visiblePages.push(page);
                }
            }
            fit(entry);
        });
    }
    function restoreFields(state){
        (state.fields||[]).forEach(function(field){
            if(field.rendered && field.rendered.parentNode===field.node){field.node.removeChild(field.rendered);field.node.appendChild(field.original)}
            field.rendered=null;field.original=null;
        });
    }
    function cancel(state){
        if(state.pending && window.engine)try{engine.call('ComfortUI_Translate',JSON.stringify({id:state.request,cancel:true}))}catch(ignored){}
        state.pending=false;state.request=null;
    }
    function reset(state){cancel(state);restoreFields(state);state.translated=false;state.failed=false;state.fields=[]}
    function label(state){
        if(!state.button)return;
        var caption=state.pending?'pending':state.translated?'original':state.failed?'failed':state.spec.bio?'bio':'translate';
        if(state.button.textContent!==text(caption))state.button.textContent=text(caption);
        state.button.title=text('hint');state.button.setAttribute('data-tooltip',text('hint'));state.button.setAttribute('aria-label',text(caption));
        state.button.setAttribute('aria-pressed',state.translated?'true':'false');
        state.button.disabled=!state.loaded;state.button.style.display=enabled() && prefs().detailTranslation!==false?'block':'none';
    }
    function capture(state){
        var fields=[],heading=state.root.querySelector('h1'),description=state.root.querySelector(state.spec.description);
        if(!state.spec.bio && heading){
            var part=window.ComfortUI.splitDetailHeading(state.root.id,heading.textContent);
            fields.push({node:heading,text:state.name || part.text,prefix:part.prefix});
        }
        if(description && description.textContent.trim() && !state.emptyBio)fields.push({node:description,text:description.innerText||description.textContent,prefix:''});
        return fields.filter(function(field){return field.text.trim().length>0});
    }
    function toggle(state){
        if(!enabled()||!state.loaded||prefs().detailTranslation===false)return;
        if(state.translated||state.pending){reset(state);label(state);return}
        Object.keys(states).forEach(function(id){var other=states[id];if(other!==state && other.pending){reset(other);label(other)}});
        state.fields=capture(state);
        if(!state.fields.length){showMessage(text('empty'));return}
        if(state.fields.some(function(field){return field.text.length>6000})){showMessage(prefs().language==='zh'?'名称或简介过长，单段最多翻译 6000 字。':(prefs().language==='en'?'A name or description exceeds the 6,000 character limit.':text('length')));return}
        state.pending=true;state.failed=false;state.target=prefs().language;
        state.request='cu-'+Date.now()+'-'+(++sequence);label(state);
        var request=state.request;
        try{
            var result=engine.call('ComfortUI_Translate',JSON.stringify({id:state.request,language:state.target,texts:state.fields.map(function(field){return field.text})}));
            if(result && typeof result.catch==='function')result.catch(function(){receive(JSON.stringify({id:request,language:state.target,error:'network'}))});
        }catch(ignored){receive(JSON.stringify({id:state.request,language:state.target,error:'network'}))}
    }
    function receive(json){
        // This receives a typed event argument, never executable source or HTML.
        if(typeof json!=='string' || json.length>1048576)return;
        var result;try{result=JSON.parse(json)}catch(ignored){return}
        if(!result || typeof result!=='object' || Array.isArray(result) || typeof result.id!=='string' || result.id.length>64 || !/^cu-[0-9]+-[0-9]+$/.test(result.id) || /[\r\n]/.test(result.id) || typeof result.language!=='string')return;
        if(Object.keys(result).some(function(key){return key!=='id' && key!=='language' && key!=='texts' && key!=='error'}))return;
        var hasError=Object.prototype.hasOwnProperty.call(result,'error');
        if(hasError){if(typeof result.error!=='string' || !/^[A-Za-z]{1,40}$/.test(result.error) || Object.prototype.hasOwnProperty.call(result,'texts'))return;}
        else if(!Array.isArray(result.texts) || result.texts.length<1 || result.texts.length>2 || result.texts.some(function(value){return typeof value!=='string' || value.length>48000}))return;
        Object.keys(states).forEach(function(id){
            var state=states[id];
            if(!state.pending || state.request!==result.id || !enabled() || !connected(state.root) || state.target!==prefs().language || result.language!==state.target)return;
            if(!hasError && result.texts.length!==state.fields.length)return;
            state.pending=false;
            if(hasError){state.failed=true;label(state);if(window.ComfortUI)window.ComfortUI.translationNotice(result.error);return}
            state.fields.forEach(function(field,i){
                var original=document.createDocumentFragment();while(field.node.firstChild)original.appendChild(field.node.firstChild);
                var rendered=document.createElement('span');rendered.className='cu-content-translated';rendered.textContent=field.prefix+result.texts[i];
                field.original=original;field.rendered=rendered;field.node.appendChild(rendered);
            });state.translated=true;state.failed=false;label(state);
        });
    }
    function loaded(id,data){
        var state=states[id];if(!state)return;reset(state);
        state.loaded=true;state.name=state.spec.name&&data?data[state.spec.name]:null;state.emptyBio=!!(state.spec.bio && (!data || !data.Bio));
        if(state.emptyBio)state.loaded=false;label(state);schedule();
    }
    function initialize(){
        initialized=true;language=translationKey();
        Object.keys(specifications).forEach(function(id){
            var root=document.getElementById(id);if(!root)return;
            var state=states[id]={root:root,spec:specifications[id],loaded:false,fields:[]};
            var button=document.createElement('button');button.type='button';button.className='cu-content-translate';button.addEventListener('click',function(){toggle(state)});
            root.appendChild(button);state.button=button;
        });
        if(window.engine && typeof engine.on==='function'){
            engine.on('LoadWorldDetails',function(data){loaded('world-detail',data)});
            engine.on('LoadAvatarDetails',function(data){loaded('avatar-detail',data)});
            engine.on('LoadPropDetails',function(data){loaded('prop-detail',data)});
            engine.on('LoadUserDetails',function(data,profile,local){loaded(local?'our-user-detail':'user-detail',data)});
        }
        observer=new MutationObserver(mediaMutated);observer.observe(document.body,{subtree:true,childList:true,attributes:true,attributeFilter:['src','class']});
        window.addEventListener('resize',markLayout);
        window.addEventListener('beforeunload',function(){observer.disconnect();clearTimeout(timer);Object.keys(states).forEach(function(id){cancel(states[id])})});
    }
    function refresh(){
        if(!window.ComfortUI || document.documentElement.getAttribute('data-comfort-context')!=='main')return;
        if(!initialized)initialize();
        var nextEnabled=enabled(),nextLayoutKey=prefs().largeText+':'+prefs().highContrast;
        if(mediaEnabled!==nextEnabled){mediaEnabled=nextEnabled;mediaDirty=true;layoutDirty=true}
        if(lastLayoutKey!==nextLayoutKey){lastLayoutKey=nextLayoutKey;layoutDirty=true}
        if(language!==translationKey() || !enabled()){
            Object.keys(states).forEach(function(id){reset(states[id])});language=translationKey();
        }
        Object.keys(states).forEach(function(id){label(states[id])});scanMedia();
    }
    function translationKey(){var p=prefs();return [p.language,p.translationProvider,p.proxyMode,p.proxyAddress,p.detailTranslation].join(':');}
    window.ComfortContent={refresh:refresh,receive:receive};
})();
