// Own hidden test window only: no game, physical keys, speech, focus takeover or global hooks.
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using ComfortUI;
class WindowsKeyboardTests060
{
 static int count;
 static void Check(bool value,string label){if(!value)throw new Exception(label);count++;Console.WriteLine("PASS "+label);}
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr key,IntPtr value);
 [DllImport("user32.dll")]static extern IntPtr GetParent(IntPtr window);
 [DllImport("user32.dll")]static extern bool IsWindow(IntPtr window);
 [STAThread] static int Main(){try{Run();return 0;}catch(Exception ex){Console.WriteLine("FAIL "+ex.GetType().Name+": "+ex.Message);return 1;}}
 static void Run(){
  using(var form=new Form{ShowInTaskbar=false,ClientSize=new System.Drawing.Size(800,500)})
  using(var input=new WindowsKeyboardInput()){
   var window=form.Handle; // Never Show() this test window.
   var area=new WindowsKeyboardInput.Bounds{X=20,Y=20,Width=500,Height=100};
   Check(!input.Open(IntPtr.Zero,area,"",0,0,100,false,true),"Null parent rejected");
   Check(!Task.Run(()=>input.Open(window,area,"",0,0,100,false,true)).Result,"Other window thread rejected");
   Check(!input.Open(window,new WindowsKeyboardInput.Bounds{X=790,Y=0,Width=100,Height=30},"",0,0,100,false,true),"Out of client rectangle rejected");
   Check(input.Open(window,area,"hello\n世界",8,8,100,false,true),"Real Windows layered EDIT child created on its owner thread");
   var child=(IntPtr)typeof(WindowsKeyboardInput).GetField("child",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(input);
   Check(GetParent(child)==window,"System input stays a child of the supplied window");
   var state=input.Read().Value;Check(state.Text=="hello\n世界"&&state.Start==8&&state.End==8,"Native Unicode CRLF text and caret roundtrip");
   input.Write("日本語 한국어 😀",10,10);state=input.Read().Value;Check(state.Text=="日本語 한국어 😀"&&state.Start==10,"CJK and emoji native write/read");
   Check(input.Read()==null,"Unchanged state is not emitted every frame");
   input.Write("ab\ncd",3,5);state=input.Read().Value;Check(state.Start==3&&state.End==5,"Multiline selection offsets roundtrip");
   SendMessage(child,0x010d,IntPtr.Zero,IntPtr.Zero);Check(input.Read().Value.Composing,"IME start marks composition");
   input.Write("do not overwrite IME",0,0);Check(input.Read()==null,"Browser echoes cannot replace active composition");
   SendMessage(child,0x0100,new IntPtr(13),IntPtr.Zero);var confirmation=input.Read();Check(!confirmation.HasValue||confirmation.Value.Action==null,"IME Enter cannot submit");
   SendMessage(child,0x010e,IntPtr.Zero,IntPtr.Zero);Check(!input.Read().Value.Composing,"IME end clears composition");
   SendMessage(child,0x0101,new IntPtr(13),IntPtr.Zero);input.Read();SendMessage(child,0x0100,new IntPtr(13),IntPtr.Zero);Check(input.Read().Value.Action=="submit","Explicit subsequent Enter requests submit");
   SendMessage(child,0x0100,new IntPtr(27),IntPtr.Zero);Check(input.Read().Value.Action=="close","Escape requests close");
   Check(!input.UseLanguage("invalid"),"Unconfigured OS language does not change the input layout");
   Check(input.Move(new WindowsKeyboardInput.Bounds{X=30,Y=30,Width=450,Height=100}),"Native target moves within own client area");
   input.Dispose();Check(!input.Active&&!IsWindow(child),"Native child and callback are released on close");
   input.Dispose();Check(input.Read()==null,"Repeated cleanup is safe");
  }
  Console.WriteLine("TOTAL "+count);
 }
}
