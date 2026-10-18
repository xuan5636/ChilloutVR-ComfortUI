using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
namespace HarmonyLib {
 public class Harmony { public readonly List<MethodInfo> Patched=new List<MethodInfo>(); public void Patch(MethodInfo method,HarmonyMethod prefix=null,HarmonyMethod postfix=null){if(method==null)throw new Exception("Missing native method");Patched.Add(method);} }
 public class HarmonyMethod { public HarmonyMethod(Type type,string method){if(AccessTools.Method(type,method)==null)throw new Exception("Missing hook");} }
 public static class AccessTools { public static MethodInfo Method(Type t,string n){return t.GetMethod(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);} }
}
namespace UnityEngine {
 public class Object { public string name; public bool Destroyed; public static void Destroy(Object value){if(ReferenceEquals(value,null))return;value.Destroyed=true;var go=value as GameObject;if(go!=null)foreach(var c in go.Components)c.Destroyed=true;} public static bool operator==(Object a,Object b){var an=ReferenceEquals(a,null)||a.Destroyed;var bn=ReferenceEquals(b,null)||b.Destroyed;return an&&bn||!an&&!bn&&ReferenceEquals(a,b);} public static bool operator!=(Object a,Object b){return !(a==b);} public override bool Equals(object other){return ReferenceEquals(this,other);} public override int GetHashCode(){return RuntimeHelpers.GetHashCode(this);} }
 public class Component:Object { public GameObject gameObject; public Transform transform{get{return gameObject.transform;}} }
 public class Transform:Component { public Transform parent; public Vector3 localScale=new Vector3(1,1,1); public Quaternion localRotation=Quaternion.identity; public void SetParent(Transform p,bool worldPositionStays){parent=p;} }
 public class RectTransform:Transform { public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta; }
 public class GameObject:Object { public int layer; public bool activeSelf=true; public Transform transform; public readonly List<Component> Components=new List<Component>(); public GameObject(string n,params Type[] types){name=n;transform=new RectTransform{gameObject=this};Components.Add(transform);foreach(var t in types)if(t!=typeof(RectTransform)){var c=(Component)Activator.CreateInstance(t);c.gameObject=this;Components.Add(c);}} public T GetComponent<T>()where T:Component {foreach(var c in Components)if(c is T)return (T)c;return null;} public void SetActive(bool value){activeSelf=value;} }
 public struct Vector2 { public float x,y;public Vector2(float a,float b){x=a;y=b;} }
 public struct Vector3 { public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;} }
 public struct Vector4 { public float x,y,z,w;public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}public static Vector4 zero{get{return new Vector4();}} }
 public struct Quaternion { public float x,y,z,w;public static Quaternion identity{get{return new Quaternion{w=1};}} }
 public struct Color { public float r,g,b,a;public Color(float x,float y,float z,float alpha=1){r=x;g=y;b=z;a=alpha;}public float grayscale{get{return .299f*r+.587f*g+.114f*b;}}public static Color white{get{return new Color(1,1,1);}}public static Color Lerp(Color x,Color y,float t){return new Color(x.r+(y.r-x.r)*t,x.g+(y.g-x.g)*t,x.b+(y.b-x.b)*t,x.a+(y.a-x.a)*t);} }
 public struct Color32 { public byte r,g,b,a;public Color32(byte x,byte y,byte z,byte alpha){r=x;g=y;b=z;a=alpha;} }
 public struct Rect { public Rect(float x,float y,float w,float h){} }
 public enum TextureFormat{RGBA32} public enum TextureWrapMode{Clamp} public enum SpriteMeshType{FullRect}
 public class Texture2D:Object { public TextureWrapMode wrapMode;public Texture2D(int w,int h,TextureFormat format,bool mipmap){}public void SetPixels32(Color32[] values){}public void Apply(bool update,bool immutable){} }
 public class Sprite:Object { public static Sprite Create(Texture2D texture,Rect rect,Vector2 pivot,float ppu,uint extrude,SpriteMeshType type,Vector4 border){return new Sprite();} }
 public class Material:Object { public string Shader="Native BillboardFacing"; }
 public static class Mathf {public static float Max(float a,float b,float c){return Math.Max(a,Math.Max(b,c));}public static float Sqrt(float n){return (float)Math.Sqrt(n);}public static float Clamp01(float n){return Math.Max(0,Math.Min(1,n));} }
 public static class ColorUtility {public static bool TryParseHtmlString(string s,out Color c){c=new Color(Convert.ToInt32(s.Substring(1,2),16)/255f,Convert.ToInt32(s.Substring(3,2),16)/255f,Convert.ToInt32(s.Substring(5,2),16)/255f);return true;} }
 public static class Time {public static float realtimeSinceStartup=10;}
}
namespace UnityEngine.UI {
 public class Image:UnityEngine.Component {public enum Type{Simple,Sliced} public UnityEngine.Sprite sprite=new UnityEngine.Sprite();public Type type;public bool enabled=true,raycastTarget=true;public UnityEngine.Material material=new UnityEngine.Material();public UnityEngine.RectTransform rectTransform{get{return (UnityEngine.RectTransform)transform;}}UnityEngine.Color value=UnityEngine.Color.white;public int Writes;public UnityEngine.Color color{get{return value;}set{this.value=value;Writes++;}} }
 public class RawImage:UnityEngine.Component {public UnityEngine.Material material=new UnityEngine.Material();public UnityEngine.RectTransform rectTransform{get{return (UnityEngine.RectTransform)transform;}} }
}
namespace TMPro {
 public enum FontWeight{Regular=400,Bold=700} [Flags]public enum FontStyles{Normal=0,Bold=1}public enum TextAlignmentOptions{Left,Center}public enum TextOverflowModes{Overflow,Ellipsis}
 public struct TMP_FontWeightPair{public TMP_FontAsset regularTypeface,italicTypeface;}
 public class TMP_FontAsset{public TMP_FontWeightPair[] fontWeightTable=new TMP_FontWeightPair[10];}
 public class TMP_Text:UnityEngine.Component {public string text="Original name";public TMP_FontAsset font=new TMP_FontAsset();public UnityEngine.Material fontSharedMaterial=new UnityEngine.Material();public UnityEngine.Vector4 margin;public FontStyles fontStyle;public FontWeight fontWeight=FontWeight.Regular;public float fontSizeMin=.1f,fontSizeMax=.3f,fontSize=.3f;public bool enableAutoSizing;public TextAlignmentOptions alignment=TextAlignmentOptions.Center;public TextOverflowModes overflowMode;public UnityEngine.Color color=UnityEngine.Color.white;public UnityEngine.RectTransform rectTransform{get{return (UnityEngine.RectTransform)transform;}} }
}
namespace ABI_RC.Core.Player {
 public class PlayerBase {public bool IsLocalPlayer,Mic;public bool IsMicActive(){return Mic;}}
 public class PlayerNameplate:UnityEngine.Object {
  public static readonly HashSet<PlayerNameplate> PlayerNameplates=new HashSet<PlayerNameplate>();public PlayerBase Player=new PlayerBase();public TMPro.TMP_Text usrNameText,rankText,staffText;public UnityEngine.UI.Image nameplateBackground,staffplateBackground,friendsIndicator;public UnityEngine.UI.RawImage playerImage;public int NativeRefreshes;public UnityEngine.Sprite NativeSprite=new UnityEngine.Sprite();public UnityEngine.Color NativeColour=new UnityEngine.Color(.8f,.3f,.2f,.7f);
  public void UpdateNamePlateSettings(){NativeRefreshes++;nameplateBackground.sprite=NativeSprite;nameplateBackground.color=NativeColour;staffplateBackground.color=NativeColour;bool dirty;ComfortUI.NameplateUI.BeforeState(true,out dirty);ComfortUI.NameplateUI.AfterState(this,Player.Mic,dirty);ComfortUI.NameplateUI.AfterSettings(this);}public void UpdateNamePlateState(){}public void OnDestroy(){}
 }
}
namespace ComfortUI {
 internal class ComfortMod {public static ComfortMod Instance=new ComfortMod();public Log LoggerInstance=new Log();public class Log{public void Warning(string message){}}public class Preferences{public bool theme=true,nameplateTheme=true,largeText;public string palette="graphite",background="#282c33",accent="#c1cbdc";}}
}
