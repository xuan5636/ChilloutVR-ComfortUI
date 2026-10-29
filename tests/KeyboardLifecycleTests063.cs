// Execute the actual KeyboardInput source with isolated game/Windows doubles.
// The separate WindowsKeyboardTests060 covers a real owned EDIT child.
using System;
using System.Collections.Generic;
using System.Reflection;
using ComfortUI;
using Newtonsoft.Json.Linq;
using ABI_RC.Core.UI;
using ABI_RC.Core.UI.UIRework;
using ABI_RC.Core.UI.UIRework.Managers;

class KeyboardLifecycleTests063
{
 static int count;
 static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Static;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static void Call(string name,params object[] args){typeof(KeyboardInput).GetMethod(name,Flags).Invoke(null,args);}
 static object Get(string name){return typeof(KeyboardInput).GetField(name,Flags).GetValue(null);}
 static KeyboardManager Start(out CohtmlViewTarget view,bool enabled=true){
  KeyboardInput.Reset();ABI_RC.Core.Savior.MetaPort.Instance.isUsingVr=false;
  var manager=new KeyboardManager();manager.Closing=()=>Call("Closed");
  Call("Shown",manager,new object[]{"initial",null,"", "",100,false,true,"",0});
  view=new CohtmlViewTarget();KeyboardInput.Tick(manager.Owner,view,new ComfortMod.Preferences{keyboardEnabled=enabled});return manager;
 }
 static void Command(CohtmlViewTarget target,string action,string session=null){KeyboardInput.Queue(target,UiDataJson.Serialize(new JObject{["session"]=session??(string)Get("session"),["action"]=action}));}
 static void Main(){
  KeyboardInput.Install(new HarmonyLib.Harmony());Check(HarmonyLib.Harmony.Methods.Count==4,"Show, close, submit and menu toggle hooks install");
  CohtmlViewTarget view;var manager=Start(out view);var session=(string)Get("session");
  var desktop=(WindowsKeyboardInput)Get("desktop");desktop.Active=true;Command(view,"cancel");KeyboardInput.Tick(manager.Owner,view,new ComfortMod.Preferences());
  Check(manager.CloseCount==1&&!manager.IsViewShown&&!manager.InputLocked&&!manager.CursorUnlocked,"Validated cancellation executes the native close/unlock exactly once");
  Check(!desktop.Active&&(string)Get("session")==null&&Get("manager")==null,"Cancellation disposes Windows input and clears session owner");
  Check(view.Events.Exists(e=>(string)e["kind"]=="closed"&&(string)e["session"]==session),"Native closure emits a typed closed notification for the matching session");
  manager=Start(out view);Command(view,"cancel","0".PadLeft(32,'0'));KeyboardInput.Tick(manager.Owner,view,new ComfortMod.Preferences());Check(manager.CloseCount==0&&manager.InputLocked,"Stale cancellation cannot close a newer input");
  var foreign=new CohtmlViewTarget();Command(foreign,"cancel");KeyboardInput.Tick(manager.Owner,view,new ComfortMod.Preferences());Check(manager.CloseCount==0,"A different view cannot cancel the active keyboard");
  Call("MenuToggled",new ABI_RC.Core.InteractionSystem.ViewManager(),false);Check(manager.CloseCount==0,"A menu hide does not cancel an unrelated keyboard");
  Call("MenuToggled",manager,true);Check(manager.CloseCount==0,"Keyboard's own show does not cancel itself");
  Call("MenuToggled",new ABI_RC.Core.InteractionSystem.ViewManager(),true);Check(manager.CloseCount==1&&!manager.InputLocked,"PC main menu opening cancels and unlocks the enhanced keyboard");
  manager=Start(out view);Call("MenuToggled",new ABI_RC.Core.InteractionSystem.CVR_MenuManager(),true);Check(manager.CloseCount==1,"Actual quick-menu type also cancels on PC");
  manager=Start(out view);ABI_RC.Core.Savior.MetaPort.Instance.isUsingVr=true;Call("MenuToggled",new ABI_RC.Core.InteractionSystem.ViewManager(),true);Check(manager.CloseCount==0,"VR menu and keyboard coexist without desktop cancellation");
  manager=Start(out view,false);Call("MenuToggled",new ABI_RC.Core.InteractionSystem.ViewManager(),true);Check(manager.CloseCount==0,"Disabling the enhanced keyboard preserves native menu behavior");
  manager=Start(out view);view.ThrowOnSend=true;manager.Close();Check(manager.CloseCount==1&&!manager.InputLocked&&Get("session")==null,"A failing UI notification cannot interrupt native unlock or cleanup");
  manager=Start(out view);view.IsCurrent=false;manager.Close();Check(manager.CloseCount==1&&Get("session")==null,"A stale view is skipped safely during native closure");
  for(int i=0;i<20;i++){manager=Start(out view);Command(view,"cancel");KeyboardInput.Tick(manager.Owner,view,new ComfortMod.Preferences());if(manager.InputLocked||manager.IsViewShown||Get("session")!=null)throw new Exception("repeat close");}
  Check(true,"Twenty isolated sessions cancel without leaving input locks");KeyboardInput.Reset();Console.WriteLine("TOTAL "+count);
 }
}
namespace HarmonyLib {
 public class HarmonyMethod:Attribute{public HarmonyMethod(Type t,string name){}}
 public class Harmony{public static List<string> Methods=new List<string>();public void Patch(MethodInfo original,HarmonyMethod prefix=null,HarmonyMethod postfix=null){if(original==null)throw new Exception("missing API");Methods.Add(original.Name);}}
 public static class AccessTools{public static FieldInfo Field(Type t,string name){return t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);}public static MethodInfo Method(Type t,string name){return t.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);}}
}
namespace UnityEngine {
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
 public class Transform{public Vector3 TransformPoint(Vector3 p){return p;}}
 public class Renderer{public Transform transform=new Transform();}
 public class Camera{public static Camera main;public Vector3 WorldToScreenPoint(Vector3 p){return p;}}
 public static class Screen{public static int width=1920,height=1080;}
 public static class GUIUtility{public static string systemCopyBuffer;}
}
namespace ABI_RC.Core.Savior{public class MetaPort{public static MetaPort Instance=new MetaPort();public bool isUsingVr;}}
namespace ABI_RC.Core.Player{public class PlayerSetup{public static PlayerSetup Instance;public UnityEngine.Camera activeCam;}}
namespace ABI_RC.Core.UI{public class CohtmlControlledView{public int Width=1520,Height=420;}}
namespace ABI_RC.Core.UI.UIRework{
 public class CVRUIManagerBase{protected CohtmlControlledView cohtmlView=new CohtmlControlledView();protected UnityEngine.Renderer _uiRenderer=new UnityEngine.Renderer();public CohtmlControlledView Owner=>cohtmlView;}
 public class CVRUIManagerBaseInput:CVRUIManagerBase{public bool IsViewShown=true;public void ToggleView(bool show){IsViewShown=show;}}
}
namespace ABI_RC.Core.InteractionSystem{public class ViewManager:CVRUIManagerBaseInput{} public class CVR_MenuManager:CVRUIManagerBaseInput{}}
namespace ABI_RC.Core.UI.UIRework.Managers{
 public class KeyboardManager:CVRUIManagerBaseInput{
  public Action Closing;public bool InputLocked=true,CursorUnlocked=true;public int CloseCount;
  public void ShowKeyboard(){}private void OnKeyboardSubmit(){}
  private void OnKeyboardClose(){Closing?.Invoke();CloseCount++;ToggleView(false);InputLocked=false;CursorUnlocked=false;}
  public void Close(){OnKeyboardClose();}
 }
}
namespace ComfortUI{
 internal class ComfortMod{internal class Preferences{public bool keyboardEnabled=true,keyboardSystemInput=true;}}
 internal class CohtmlViewTarget{internal bool IsCurrent=true,ThrowOnSend;internal List<JObject> Events=new List<JObject>();internal bool Keyboard(string json){if(ThrowOnSend)throw new Exception("simulated stale UI");Events.Add(JObject.Parse(json));return true;}}
 internal class WindowsKeyboardInput:IDisposable{
  internal bool Active;internal struct Bounds{internal int X,Y,Width,Height;}
  internal struct State{internal string Text,Action;internal int Start,End;internal bool Composing,Focused;}
  internal State? Read(){return null;}internal void Blur(){Active=false;}public void Dispose(){Active=false;}internal bool Move(Bounds b){return true;}
  internal void Write(string s,int start,int end){}internal void UseLanguage(string s){}
  internal bool Open(IntPtr w,Bounds b,string s,int start,int end,int max,bool hidden,bool multiline){Active=true;return true;}
  internal static IntPtr FindGameWindow(){return new IntPtr(1);}internal static bool GameIsForeground(IntPtr w){return false;}
  internal static bool ClientSize(IntPtr w,out int width,out int height){width=1920;height=1080;return true;}internal static bool ValidBounds(IntPtr w,Bounds b){return true;}
 }
}
