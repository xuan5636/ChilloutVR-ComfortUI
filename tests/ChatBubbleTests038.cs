using System;
using System.Reflection;
using ComfortUI;
using ABI_RC.Systems.ChatBox;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
class ChatBubbleTests038 {
 static int count;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static bool Near(float a,float b){return Math.Abs(a-b)<.001f;}
 static ChatBoxBubbleBehavior Bubble(string message){
  var root=new GameObject("native-bubble",typeof(ChatBoxBubbleBehavior));var b=root.GetComponent<ChatBoxBubbleBehavior>();
  var label=new GameObject("native-output",typeof(TMP_Text));label.transform.SetParent(root.transform,false);b.textBubbleOutputTMP=label.GetComponent<TMP_Text>();b.textBubbleOutputTMP.text=message;
  b.textBubbleOutputTMP.margin=new Vector4(-242,-71,-240,-72);b.textBubbleOutputTMP.fontSize=60;b.textBubbleOutputTMP.rectTransform.sizeDelta=new Vector2(200,50);
  b.textBubbleHexagonImg=new GameObject("native-hexagon",typeof(Image)).GetComponent<Image>();b.textBubbleRoundImg=new GameObject("native-round",typeof(Image)).GetComponent<Image>();
  foreach(var im in new[]{b.textBubbleHexagonImg,b.textBubbleRoundImg}){im.transform.SetParent(root.transform,false);im.rectTransform.sizeDelta=new Vector2(810,213);im.color=new Color(0,1,0,.75f);}
  b.textBubbleRoundImg.gameObject.SetActive(false);ChatBoxBubbleBehavior.ChatBoxes[Guid.NewGuid().ToString()]=b;return b;
 }
 static void Main(){
  var pref=new ComfortMod.Preferences();var harmony=new HarmonyLib.Harmony();ChatBubbleUI.Install(harmony,()=>pref);Check(harmony.Patched.Count==7,"Native lifecycle/message/colour/typing/layout hooks installed");
  var b=Bubble("Hello");var t=b.textBubbleOutputTMP;var h=b.textBubbleHexagonImg;var r=b.textBubbleRoundImg;var mat=t.fontSharedMaterial;var originalFont=t.font;var bgmat=h.material;var sprite=h.sprite;var parent=t.transform.parent;
  ChatBubbleUI.Tick(pref);Check(h.sprite!=sprite&&h.type==Image.Type.Sliced,"Both native bubble shapes use a sliced rounded card");Check(h.sprite==r.sprite,"One shared card resource across both native shapes");
  Check(ReferenceEquals(mat,t.fontSharedMaterial)&&ReferenceEquals(bgmat,h.material),"Native facing/opacity materials are preserved");Check(ReferenceEquals(originalFont,t.font),"Original font atlas remains native");Check(t.text=="Hello"&&ReferenceEquals(parent,t.transform.parent),"Message and native output parent are unchanged");
  Check(!r.gameObject.activeSelf&&h.gameObject.activeSelf&&b.IsChatBubbleActive,"Native shape/visibility and lifetime remain in control");Check(Near(h.color.r,40/255f)&&t.color.r>.9f,"Quiet dark card with high contrast text");
  Check(t.alignment==TextAlignmentOptions.MidlineLeft&&t.margin.x>0&&t.enableWordWrapping,"Readable left aligned text and positive internal padding");Check(t.fontWeight==FontWeight.Bold,"Synthesized heavier text retains the native atlas");
  var shortWidth=h.rectTransform.sizeDelta.x;var shortHeight=h.rectTransform.sizeDelta.y;t.text=new string('W',220);ChatBubbleUI.Refresh(b);
  Check(h.rectTransform.sizeDelta.x>shortWidth&&h.rectTransform.sizeDelta.x<=740,"Long messages expand only to the maximum card width");Check(h.rectTransform.sizeDelta.y>shortHeight&&h.rectTransform.sizeDelta.y<=360,"Long messages wrap within bounded height");Check(Near(t.rectTransform.sizeDelta.x,h.rectTransform.sizeDelta.x)&&Near(t.rectTransform.sizeDelta.y,h.rectTransform.sizeDelta.y),"Text bounds match the card after wrapping");
  t.text="翻译后的聊天内容";ChatBubbleUI.Refresh(b);Check(t.text=="翻译后的聊天内容"&&h.rectTransform.sizeDelta.x<740,"Translated content reflows in the same native output");t.text="Hello";ChatBubbleUI.Refresh(b);Check(Near(h.rectTransform.sizeDelta.x,shortWidth),"Restoring original content restores compact size");
  var measurements=t.Measurements;for(var i=0;i<200;i++)ChatBubbleUI.Tick(pref);Check(t.Measurements==measurements,"Stable frames do not remeasure or rebuild the Canvas");Time.realtimeSinceStartup+=1;ChatBubbleUI.Tick(pref);Check(t.Measurements==measurements,"Unchanged periodic scans also skip layout writes");
  pref=new ComfortMod.Preferences{palette="custom",background="#f0f2f4",largeText=true};ChatBubbleUI.Tick(pref);Check(h.color.r>.9f&&t.color.r<.2f,"Custom light palette chooses dark text");Check(t.fontSizeMax==46,"Large text uses larger capped font size");
  ChatBubbleUI.PrepareFontChange(b);Check(t.fontWeight==FontWeight.Regular&&t.fontStyle==FontStyles.Normal,"Font changes clear synthetic bold before TMP can choose another font face");
  var fallback=new TMP_FontAsset();fallback.fontWeightTable[7].regularTypeface=new TMP_FontAsset();t.font=fallback;ChatBubbleUI.Refresh(b);Check(t.fontWeight==FontWeight.Regular&&t.fontStyle==FontStyles.Normal,"Alternate font faces are never forced by synthetic bold");Check(ReferenceEquals(mat,t.fontSharedMaterial),"Font fallback layout does not replace the billboard material");
  h.color=r.color=new Color(.1f,.6f,.7f,.75f);typeof(ChatBubbleUI).GetMethod("AfterColor",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{b});Check(h.color.r>.9f,"New message source colour cannot flash the old saturated background");
  pref=new ComfortMod.Preferences{theme=false};ChatBubbleUI.Tick(pref);Check(h.sprite==sprite&&h.type==Image.Type.Simple&&h.raycastTarget,"Original mode restores native sprite and input flags");Check(Near(h.color.g,.6f),"Original mode restores the latest native source colour");Check(Near(t.margin.x,-242)&&Near(t.fontSize,60)&&t.alignment==TextAlignmentOptions.Center,"Original text metrics restore exactly");Check(t.text=="Hello"&&ReferenceEquals(mat,t.fontSharedMaterial)&&!r.gameObject.activeSelf,"Restore preserves current content, material and visibility");
  pref=new ComfortMod.Preferences();ChatBubbleUI.Tick(pref);Check(h.sprite!=sprite,"Theme can be reenabled without duplicate UI objects");
  pref=new ComfortMod.Preferences{chatBubbleTheme=false};ChatBubbleUI.Tick(pref);Check(h.sprite==sprite&&pref.theme,"The independent bubble switch restores bubbles without disabling menu colours");
  pref=new ComfortMod.Preferences{chatBubbleTheme=true};ChatBubbleUI.Tick(pref);Check(h.sprite!=sprite,"The independent bubble switch reuses the owned card when enabled again");
  var missing=Bubble("Missing optional renderer");missing.textBubbleRoundImg=null;Time.realtimeSinceStartup+=1;ChatBubbleUI.Tick(pref);Check(missing.textBubbleHexagonImg.type==Image.Type.Simple,"Incomplete native prefabs safely remain native");
  var shared=h.sprite;typeof(ChatBubbleUI).GetMethod("Remove",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{b});Check(h.sprite==sprite&&!shared.Destroyed,"Destroy cleanup restores only this bubble and keeps shared resource alive");
  ChatBubbleUI.Reset();Check(shared.Destroyed,"Unload releases owned shared texture and sprite");Console.WriteLine("TOTAL "+count);
 }
}
