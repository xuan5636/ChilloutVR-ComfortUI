using System;
using ABI_RC.Core.Player;
using ComfortUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
class NameplateTests030
{
 static int passed;
 static void Check(bool condition,string text){if(!condition)throw new Exception(text);passed++;Console.WriteLine("PASS "+text);}
 static T Node<T>(string name)where T:Component {return new GameObject(name,typeof(RectTransform),typeof(T)).GetComponent<T>();}
 static PlayerNameplate Plate(bool local=false){var p=new PlayerNameplate{usrNameText=Node<TMP_Text>("Name"),rankText=Node<TMP_Text>("Rank"),staffText=Node<TMP_Text>("Staff"),nameplateBackground=Node<Image>("Background"),staffplateBackground=Node<Image>("Role"),friendsIndicator=Node<Image>("Friend"),playerImage=Node<RawImage>("Avatar")};p.Player.IsLocalPlayer=local;p.staffText.text="GM";p.rankText.text="Original role";p.staffplateBackground.color=p.NativeColour;return p;}
 static bool Near(float x,float y){return Math.Abs(x-y)<.0001f;}
 static int Main(){try{
  foreach(var locale in new[]{"zh-CN","zh-TW","zh-Hant-HK","ZH_hans","en-GB","en-US","ja-JP","ko-KR","es-MX","es-ES"}){var expected=locale.Replace('_','-').Substring(0,2).ToLowerInvariant();Check(StartupLanguage.Choose(false,"zh",()=>locale)==expected,locale+" chooses a supported first-run language");}
  Check(StartupLanguage.FromLocale("de-DE")=="en"&&StartupLanguage.FromLocale("")=="en"&&StartupLanguage.FromLocale(null)=="en","Unsupported or missing system language falls back to English");
  var calls=0;Check(StartupLanguage.Choose(true,"ja",()=>{calls++;return "zh-CN";})=="ja"&&calls==0,"An existing user choice is retained without consulting the system locale");
  Check(StartupLanguage.Choose(false,"zh",()=>{throw new Exception();})=="en","Detection failure uses English without preventing startup");
  Check(LanguageSupport.Supported(StartupLanguage.FromLocale(StartupLanguage.SystemLocale())),"Real Windows UI language query returns a usable supported fallback");
  var pref=new ComfortMod.Preferences();var harmony=new HarmonyLib.Harmony();NameplateUI.Install(harmony,()=>pref);Check(harmony.Patched.Count==3,"Settings, state and destruction hooks resolve");
  var p=Plate();var root=new GameObject("Native Content",typeof(RectTransform));p.usrNameText.transform.SetParent(root.transform,false);var font=p.usrNameText.font;var textMaterial=p.usrNameText.fontSharedMaterial;var bgMaterial=p.nameplateBackground.material;var oldSprite=p.nameplateBackground.sprite;var oldRect=p.usrNameText.rectTransform.sizeDelta;var oldFriend=p.friendsIndicator.sprite;
  PlayerNameplate.PlayerNameplates.Add(p);NameplateUI.Tick(pref);
  Check(p.nameplateBackground.type==Image.Type.Sliced&&p.nameplateBackground.sprite!=oldSprite,"Rounded card replaces the native panel surface");
  var trim=p.nameplateBackground.GetComponent<Outline>();p.PlayerDescriptor.Colour=new Color(.62f,.28f,.84f,.3f);p.UpdateNamePlateSettings();
  Check(trim!=null&&Near(trim.effectColor.r,.62f)&&Near(trim.effectColor.b,.84f)&&Near(trim.effectColor.a,.65f),"Soft trim follows this player's effective purple colour instead of the shared theme accent");
  var sprite=p.nameplateBackground.sprite;var corner=sprite.border.x/(sprite.pixelsPerUnit/100*p.nameplateBackground.pixelsPerUnitMultiplier);
  Check(corner< p.nameplateBackground.rectTransform.sizeDelta.y/2&&corner>.05f,"World Canvas pixel density leaves straight edges instead of collapsing every corner into an ellipse");
  Check(ReferenceEquals(p.usrNameText.font,font)&&ReferenceEquals(p.usrNameText.fontSharedMaterial,textMaterial)&&ReferenceEquals(p.nameplateBackground.material,bgMaterial),"Original font atlas and all billboard material references stay intact");
  Check(ReferenceEquals(p.usrNameText.transform.parent,root.transform)&&Near(p.usrNameText.transform.localScale.x,1),"Native hierarchy and facing/scale transforms are preserved");
  Check(p.usrNameText.text=="Original name"&&p.rankText.text=="Original role"&&p.staffText.text=="GM","Names and role content remain unchanged");
  Check(p.usrNameText.fontStyle.HasFlag(FontStyles.Bold)&&p.usrNameText.enableAutoSizing&&p.usrNameText.overflowMode==TextOverflowModes.Ellipsis,"Name text gains safe synthesized weight, bounded autosizing and ellipsis");
  var nr=p.usrNameText.rectTransform;var ar=p.playerImage.rectTransform;Check(nr.anchoredPosition.x-nr.sizeDelta.x/2>ar.anchoredPosition.x+ar.sizeDelta.x/2,"Name begins after the portrait rather than overlapping it");
  Check(ReferenceEquals(p.friendsIndicator.sprite,oldFriend)&&p.friendsIndicator.enabled&&p.playerImage.gameObject.activeSelf,"Native friend marker and portrait visibility remain in charge");
  var shortWidth=p.nameplateBackground.rectTransform.sizeDelta.x;p.usrNameText.text=new string('W',80);p.UpdateNamePlateSettings();Check(p.nameplateBackground.rectTransform.sizeDelta.x>shortWidth&&p.nameplateBackground.rectTransform.sizeDelta.x<=3.2f,"Short names use compact cards and long names stay inside a bounded card");
  p.usrNameText.text="Original name";p.UpdateNamePlateSettings();
  p.rankText.gameObject.SetActive(false);p.UpdateNamePlateSettings();Check(Near(p.usrNameText.rectTransform.anchoredPosition.y,.06f)&&Near(p.nameplateBackground.rectTransform.sizeDelta.y,.48f),"Hidden rank rows center the name and remove empty vertical space");p.rankText.gameObject.SetActive(true);p.UpdateNamePlateSettings();
  var writes=p.nameplateBackground.Writes;for(var i=0;i<120;i++)NameplateUI.AfterState(p,false,false);Check(p.nameplateBackground.Writes==writes,"Stable per-frame native updates do not dirty the Canvas");
  var trimWrites=trim.Writes;for(var i=0;i<120;i++)NameplateUI.AfterState(p,false,true);Check(trim.Writes==trimWrites,"Repeated native repaint callbacks with unchanged colour do not dirty the trim");writes=p.nameplateBackground.Writes;
  NameplateUI.AfterState(p,true,true);Check(p.nameplateBackground.Writes==writes+1&&Near(p.nameplateBackground.color.r,40/255f),"Speaking updates the quiet surface without rank-colour flashes");
  Check(Near(trim.effectColor.r,.62f)&&Near(trim.effectColor.b,.84f),"Speaking highlight preserves the player's identifying border hue");
  p.playerImage.gameObject.SetActive(false);p.friendsIndicator.enabled=false;p.UpdateNamePlateSettings();Check(nr.sizeDelta.x>1&& !p.playerImage.gameObject.activeSelf&&!p.friendsIndicator.enabled,"Native settings refresh widens names when portraits are hidden without overriding visibility");
  var local=Plate(true);PlayerNameplate.PlayerNameplates.Add(local);var localSprite=local.nameplateBackground.sprite;Time.realtimeSinceStartup+=2;NameplateUI.Tick(pref);Check(local.nameplateBackground.sprite==localSprite,"Local-player nameplates are left untouched");
  Check(local.nameplateBackground.GetComponent<Outline>()==null,"Remote-player trim does not add components to the local nameplate");
  var alternate=Plate();alternate.usrNameText.font.fontWeightTable[7].regularTypeface=new TMP_FontAsset();PlayerNameplate.PlayerNameplates.Add(alternate);Time.realtimeSinceStartup+=2;NameplateUI.Tick(pref);Check(alternate.usrNameText.fontWeight==FontWeight.Regular&&alternate.usrNameText.fontStyle==FontStyles.Normal,"An alternate bold font face is not selected over the billboard material");
  alternate.PlayerDescriptor.Colour=new Color(.2f,.7f,.65f);alternate.UpdateNamePlateSettings();Check(Near(alternate.nameplateBackground.GetComponent<Outline>().effectColor.g,.7f)&&Near(trim.effectColor.g,.28f),"Players with different native colours keep independent borders");
  p.PlayerDescriptor.Colour=new Color(.95f,.64f,.2f);p.UpdateNamePlateSettings();Check(ReferenceEquals(trim,p.nameplateBackground.GetComponent<Outline>())&&Near(trim.effectColor.r,.95f)&&Near(trim.effectColor.g,.64f),"Native profile colour refresh updates the existing trim without adding duplicate effects");
  var originalEffectPlate=Plate();var originalEffect=originalEffectPlate.nameplateBackground.gameObject.AddComponent<Outline>();originalEffect.enabled=false;originalEffect.useGraphicAlpha=true;originalEffect.effectColor=new Color(.1f,.2f,.3f,.4f);originalEffect.effectDistance=new Vector2(.2f,-.3f);PlayerNameplate.PlayerNameplates.Add(originalEffectPlate);Time.realtimeSinceStartup+=2;NameplateUI.Tick(pref);
  Check(ReferenceEquals(originalEffect,originalEffectPlate.nameplateBackground.GetComponent<Outline>())&&originalEffect.enabled,"Existing native outline is reused instead of stacking a second outline");
  pref=new ComfortMod.Preferences{palette="custom",background="#ffffff",accent="#445566"};NameplateUI.Tick(pref);Check(p.usrNameText.color.r<.2f&&p.nameplateBackground.color.r>.9f,"A light custom palette receives dark readable name text");
  Check(Near(trim.effectColor.r,.95f)&&Near(trim.effectDistance.x,.015f)&&!trim.useGraphicAlpha,"A light theme retains player colour and the thin trim independently of panel opacity");
  pref=new ComfortMod.Preferences{palette="custom",background="#00ff00",accent="#445566",largeText=true};NameplateUI.Tick(pref);Check(p.usrNameText.color.r<.2f&&Near(p.usrNameText.fontSizeMax,.27f),"Bright saturated palettes retain readable text and large-text sizing applies");
  pref=new ComfortMod.Preferences{nameplateTheme=false};NameplateUI.Tick(pref);Check(p.nameplateBackground.sprite==p.NativeSprite&&Near(p.nameplateBackground.color.r,p.NativeColour.r)&&p.NativeRefreshes>=2,"Disabling the theme reapplies current native settings without repainting the custom surface");
  Check(trim==null&&!originalEffect.enabled&&originalEffect.useGraphicAlpha&&Near(originalEffect.effectColor.a,.4f)&&Near(originalEffect.effectDistance.y,-.3f),"Disabling removes only owned trim and exactly restores a pre-existing native outline");
  Check(p.usrNameText.fontStyle==FontStyles.Normal&&Near(nr.sizeDelta.x,oldRect.x)&&ReferenceEquals(p.usrNameText.fontSharedMaterial,textMaterial),"Disabling restores original typography, geometry and material references");
  pref=new ComfortMod.Preferences();NameplateUI.Tick(pref);Check(p.nameplateBackground.type==Image.Type.Sliced,"Nameplate styling can be enabled again");
  NameplateUI.BeforeDestroy(p);PlayerNameplate.PlayerNameplates.Remove(p);UnityEngine.Object.Destroy(p);Time.realtimeSinceStartup+=2;NameplateUI.Tick(pref);Check(true,"Native destruction releases owned UI and permits later discovery");
  NameplateUI.Reset();Check(alternate.nameplateBackground.sprite==alternate.NativeSprite,"Unloading restores remaining remote nameplates");
  Console.WriteLine("TOTAL "+passed+" nameplate/startup checks passed with native stand-ins; actual Unity/VR rendering remains unverified.");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
