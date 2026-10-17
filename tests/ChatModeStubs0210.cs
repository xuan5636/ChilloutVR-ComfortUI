using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ComfortUI;
namespace HarmonyLib { public class Harmony { public void Patch(MethodInfo m,HarmonyMethod prefix=null,HarmonyMethod postfix=null){} } public class HarmonyMethod { public HarmonyMethod(Type t,string n){} } public static class AccessTools { public static FieldInfo Field(Type t,string n){return t.GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);} public static MethodInfo Method(Type t,string n){return t.GetMethod(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);} public static Type TypeByName(string n){return null;} } }
namespace UnityEngine {
 public class Object {} public class MonoBehaviour { public bool isActiveAndEnabled=true; }
 public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;} }
 public class Transform { public Vector3 position=new Vector3(.5f,.5f,1); }
 public class Camera { public static Camera main=new Camera(); public int Calls; public Vector3 WorldToViewportPoint(Vector3 v){Calls++;return v;} }
 public static class Time { public static float realtimeSinceStartup=10; }
 public static class Application { public static bool isFocused=true; }
 public enum KeyCode { F7,F8,F9,A,Alpha1 }
 public static class Input { public static bool Pressed; public static bool GetKeyDown(KeyCode key){var value=Pressed;Pressed=false;return value;} }
}
namespace UnityEngine.UI { public class InputField { public bool isFocused; } }
namespace TMPro { public class TMP_FontAsset {} public class TMP_Text { public bool isActiveAndEnabled=true,richText=true; public string text; public TMP_FontAsset font=new TMP_FontAsset(); public UnityEngine.Transform transform=new UnityEngine.Transform(); } }
namespace ABI_RC.Core.Player { public class PlayerBase { public bool IsLocalPlayer; } public class PlayerSetup { public static PlayerSetup Instance; public UnityEngine.Camera activeCam; } }
namespace ABI_RC.Core.UI { public class CohtmlHud { public void ViewDropText(string a,string b){} } }
namespace ABI_RC.Core.InteractionSystem { public class ViewManager { public static ViewManager Instance; public UnityEngine.UI.InputField _inputField; public void TriggerAlert(string x,string y,int a,bool b){} } }
namespace ABI_RC.Systems.ChatBox {
 public class ChatBoxBubbleBehavior:UnityEngine.MonoBehaviour {
  public static Dictionary<string,ChatBoxBubbleBehavior> ChatBoxes=new Dictionary<string,ChatBoxBubbleBehavior>();
  public bool IsChatBubbleActive=true; public ABI_RC.Core.Player.PlayerBase Player=new ABI_RC.Core.Player.PlayerBase(); public TMPro.TMP_Text textBubbleOutputTMP=new TMPro.TMP_Text(); public void OnMessage(){}
 }
}
namespace ComfortUI {
 internal class ComfortMod {
  internal static ComfortMod Instance=new ComfortMod(); internal List<string> Notices=new List<string>(); internal void NotifyTranslation(string value){Notices.Add(value);}
  internal class Preferences { public string language="zh",translationProvider="google",proxyMode="system",proxyAddress="http://127.0.0.1:7890",translationKey="F8"; public bool chatTranslation=true; }
  internal static ContentTranslator.Options TranslationOptions(Preferences p){return new ContentTranslator.Options();}
 }
 internal static class CameraUI { internal static TMPro.TMP_FontAsset FontFor(string c){return new TMPro.TMP_FontAsset();} }
 internal class ContentTranslator {
  internal class Options {} internal class Failure:Exception { internal string Code; internal Failure(string c){Code=c;} }
  internal readonly List<string> Texts=new List<string>(); internal readonly List<TaskCompletionSource<string[]>> Pending=new List<TaskCompletionSource<string[]>>();
  internal Task<string[]> Translate(string[] values,string language,CancellationToken token,Options options){lock(this){Texts.Add(values[0]);var task=new TaskCompletionSource<string[]>();Pending.Add(task);token.Register(()=>task.TrySetCanceled());return task.Task;}}
 }
}
