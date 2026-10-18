using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ComfortUI;
namespace HarmonyLib { public class Harmony { public void Patch(MethodInfo m,HarmonyMethod prefix=null,HarmonyMethod postfix=null){} } public class HarmonyMethod { public HarmonyMethod(Type t,string n){} } public static class AccessTools { public static FieldInfo Field(Type t,string n){return t.GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);} public static MethodInfo Method(Type t,string n){return t.GetMethod(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);} public static Type TypeByName(string n){return null;} } }
namespace UnityEngine {
 public class Object { public string name; public bool Destroyed; public HideFlags hideFlags; public static void Destroy(Object value){value.Destroyed=true;} } public class MonoBehaviour { public bool isActiveAndEnabled=true; }
 public enum HideFlags { DontSave }
 public class Texture:Object {}
 public class Shader:Object { public Shader(string value){name=value;} }
 public class Material:Object {
  public Shader shader; public int renderQueue=3010;
  readonly Dictionary<string,float> floats=new Dictionary<string,float>(); readonly Dictionary<string,Texture> textures=new Dictionary<string,Texture>();
  public Material(string shaderName){shader=new Shader(shaderName);SetTexture("_MainTex",new Texture());SetFloat("_TextureWidth",512);SetFloat("_TextureHeight",512);SetFloat("_GradientScale",5);SetFloat("_WeightNormal",0);SetFloat("_WeightBold",.5f);SetFloat("_MaxOpacity",.4f);SetFloat("_FirstPersonLocalNameplateScaleVr",1);SetFloat("_FirstPersonLocalNameplateScaleDesktop",1);SetFloat("_IsLocalPlayer",0);}
  public Material(Material other){shader=other.shader;renderQueue=other.renderQueue;foreach(var pair in other.floats)floats[pair.Key]=pair.Value;foreach(var pair in other.textures)textures[pair.Key]=pair.Value;}
  public bool HasProperty(string property){return floats.ContainsKey(property)||textures.ContainsKey(property);} public float GetFloat(string property){return floats[property];} public void SetFloat(string property,float value){floats[property]=value;} public Texture GetTexture(string property){return textures[property];} public void SetTexture(string property,Texture value){textures[property]=value;}
 }
 public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;} }
 public class Transform { public Vector3 position=new Vector3(.5f,.5f,1); public Vector3 localScale=new Vector3(1,1,1); }
 public class Camera { public static Camera main=new Camera(); public int Calls; public Vector3 WorldToViewportPoint(Vector3 v){Calls++;return v;} }
 public static class Time { public static float realtimeSinceStartup=10; }
 public static class Application { public static bool isFocused=true; }
 public enum KeyCode { F7,F8,F9,A,Alpha1 }
 public static class Input { public static bool Pressed; public static bool GetKeyDown(KeyCode key){var value=Pressed;Pressed=false;return value;} }
}
namespace UnityEngine.UI { public class InputField { public bool isFocused; } }
namespace TMPro {
 public class TMP_FontAsset { public UnityEngine.Material material=new UnityEngine.Material("TextMeshPro/Distance Field"); }
 public class TMP_Text {
  public static int Created; public int MeshUpdates; public string RenderedText; public bool ThrowMeshOnce;
  public bool isActiveAndEnabled=true,richText=true; public string text; TMP_FontAsset currentFont=new TMP_FontAsset(); public UnityEngine.Material fontSharedMaterial;
  public TMP_FontAsset font { get{return currentFont;} set{if(currentFont==value)return;currentFont=value;fontSharedMaterial=value.material;} }
  public UnityEngine.Transform transform=new UnityEngine.Transform();
  public TMP_Text(){Created++;fontSharedMaterial=new UnityEngine.Material("Alpha Blend Interactive/TextMeshPro/Mobile/Distance Field-BillboardFacing");fontSharedMaterial.SetFloat("_TextureWidth",2048);fontSharedMaterial.SetFloat("_TextureHeight",2048);fontSharedMaterial.SetFloat("_GradientScale",10);}
  public void ForceMeshUpdate(){if(ThrowMeshOnce){ThrowMeshOnce=false;throw new InvalidOperationException("Renderer unavailable");}MeshUpdates++;RenderedText=text;}
 }
}
namespace ABI_RC.Core.Player { public class PlayerBase { public bool IsLocalPlayer; } public class PlayerSetup { public static PlayerSetup Instance; public UnityEngine.Camera activeCam; } }
namespace ABI_RC.Core.UI { public class CohtmlHud { public void ViewDropText(string a,string b){} } }
namespace ABI_RC.Core.InteractionSystem { public class ViewManager { public static ViewManager Instance; public UnityEngine.UI.InputField _inputField; public void TriggerAlert(string x,string y,int a,bool b){} } }
namespace ABI_RC.Systems.ChatBox {
 public class ChatBoxBubbleBehavior:UnityEngine.MonoBehaviour {
  public static Dictionary<string,ChatBoxBubbleBehavior> ChatBoxes=new Dictionary<string,ChatBoxBubbleBehavior>();
  public bool IsChatBubbleActive=true; public ABI_RC.Core.Player.PlayerBase Player=new ABI_RC.Core.Player.PlayerBase(); public TMPro.TMP_Text textBubbleOutputTMP=new TMPro.TMP_Text(); public readonly HashSet<UnityEngine.Material> _materials=new HashSet<UnityEngine.Material>(); public void OnMessage(){} public void OnDestroy(){}
  public ChatBoxBubbleBehavior(){_materials.Add(textBubbleOutputTMP.fontSharedMaterial);}
 }
}
namespace ComfortUI {
 internal class ComfortMod {
  internal static ComfortMod Instance=new ComfortMod(); internal List<string> Notices=new List<string>(); internal void NotifyTranslation(string value){Notices.Add(value);}
  internal class Preferences { public string language="zh",translationProvider="google",proxyMode="system",proxyAddress="http://127.0.0.1:7890",translationKey="F8",sourceLanguage="auto",aiEndpoint="https://api.openai.com/v1",aiModel="",microsoftRegion="",libreEndpoint="http://127.0.0.1:5000"; public int TranslationRevision; public bool chatTranslation=true; }
  internal static ContentTranslator.Options TranslationOptions(Preferences p){return new ContentTranslator.Options();}
 }
 internal static class CameraUI { internal static TMPro.TMP_FontAsset FontFor(string c){return new TMPro.TMP_FontAsset();} }
 internal class ContentTranslator {
  internal class Options {} internal class Failure:Exception { internal string Code; internal Failure(string c){Code=c;} }
  internal readonly List<string> Texts=new List<string>(); internal readonly List<TaskCompletionSource<string[]>> Pending=new List<TaskCompletionSource<string[]>>();
  internal Task<string[]> Translate(string[] values,string language,CancellationToken token,Options options){lock(this){Texts.Add(values[0]);var task=new TaskCompletionSource<string[]>();Pending.Add(task);token.Register(()=>task.TrySetCanceled());return task.Task;}}
 }
}
