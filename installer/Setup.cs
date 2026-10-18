using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Comfort UI 安装助手")]
[assembly: AssemblyVersion("0.3.0.0")]
namespace ComfortSetup {
 public sealed class State {
  public string Version, GamePath, Target, OriginalBackup, InstalledHash, InstalledAt;
 }
 public static class Install {
  public const string Version="0.3.0";
  public const string PayloadHash="76436ACCAEACE99E6EC7C3B37B77269D468FC6C220903272EB67B7C5CA42A321";
  public const string LoaderUrl="https://github.com/LavaGang/MelonLoader/releases/download/v0.7.3/MelonLoader.Installer.exe";
  public const string LoaderHash="27B9EC8D7E937E75333F32A4C8B38CB448B389CBA9BD4EEC32ADFE67C96D0232";
  static readonly JavaScriptSerializer Json=new JavaScriptSerializer();
  public static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}
  public static string HashFile(string path){return Hash(File.ReadAllBytes(path));}
  public static string Normalize(string p){return Path.GetFullPath(p.Trim().Trim('"')).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);}
  public static bool Game(string p){try{return File.Exists(Path.Combine(Normalize(p),"ChilloutVR.exe"))&&Directory.Exists(Path.Combine(Normalize(p),"ChilloutVR_Data"));}catch{return false;}}
  public static bool Loader(string p){return File.Exists(Path.Combine(p,"MelonLoader","net35","MelonLoader.dll"))&&File.Exists(Path.Combine(p,"version.dll"));}
  public static bool Running(){return Process.GetProcessesByName("ChilloutVR").Length!=0;}
  static void Guard(string p,bool loader){if(!Game(p))throw new InvalidOperationException("请选择包含 ChilloutVR.exe 和 ChilloutVR_Data 的游戏文件夹。");if(Running())throw new InvalidOperationException("请先正常退出 ChilloutVR，再继续安装或移除。程序不会关闭游戏。");if(loader&&!Loader(p))throw new InvalidOperationException("尚未检测到 Mono 版 MelonLoader，请先使用下方加载器安装引导。");}
  public static byte[] Payload(){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("ComfortUI.Payload")){if(s==null)throw new InvalidDataException("安装包缺少模组文件。");using(var m=new MemoryStream()){s.CopyTo(m);var b=m.ToArray();if(Hash(b)!=PayloadHash)throw new InvalidDataException("模组文件校验失败，请重新获取安装程序。");return b;}}}
  static State ReadState(string file){if(!File.Exists(file))return null;var s=Json.Deserialize<State>(File.ReadAllText(file));if(s==null)throw new InvalidDataException("安装记录无法读取，已保留现有文件。");return s;}
  static void CheckState(State s,string target){if(s!=null&&(!String.Equals(Normalize(s.Target??""),target,StringComparison.OrdinalIgnoreCase)||String.IsNullOrEmpty(s.InstalledHash)))throw new InvalidDataException("安装记录中的路径或校验值不匹配，已停止修改。");}
  static string Stamp(){return DateTime.Now.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+Guid.NewGuid().ToString("N").Substring(0,6);}
  static void Atomic(string path,byte[] bytes){var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllBytes(temp,bytes);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
  public static string Apply(string path){
   var p=Normalize(path);Guard(p,true);var bytes=Payload();
   var target=Path.Combine(p,"Mods","CVRComfortUI.dll");var data=Path.Combine(p,"UserData","ComfortUI");var stateFile=Path.Combine(data,"install-state.json");var backup=Path.Combine(data,"backups");
   var state=ReadState(stateFile);CheckState(state,target);bool had=File.Exists(target);byte[] old=had?File.ReadAllBytes(target):null;byte[] oldState=File.Exists(stateFile)?File.ReadAllBytes(stateFile):null;
   if(had){if(state!=null&&Hash(old)!=state.InstalledHash)throw new InvalidOperationException("现有模组在安装后被修改过，已保留。请先检查文件或备份后移走再安装。");if(AssemblyName.GetAssemblyName(target).Name!="CVRComfortUI")throw new InvalidOperationException("同名文件不属于 Comfort UI，已保留。");}
   Directory.CreateDirectory(Path.GetDirectoryName(target));Directory.CreateDirectory(backup);
   string saved=null;if(had){saved=Path.Combine(backup,"before-update-"+Stamp()+".dll");File.WriteAllBytes(saved,old);}
   if(state==null)state=new State{OriginalBackup=saved};
   state.Version=Version;state.GamePath=p;state.Target=target;state.InstalledHash=PayloadHash;state.InstalledAt=DateTimeOffset.Now.ToString("o");
   try{Guard(p,true);Atomic(target,bytes);if(HashFile(target)!=PayloadHash)throw new IOException("安装后文件校验失败。");Atomic(stateFile,Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(Json.Serialize(state))).ToArray());}
   catch{if(had)Atomic(target,old);else if(File.Exists(target))File.Delete(target);if(oldState!=null)Atomic(stateFile,oldState);else if(File.Exists(stateFile))File.Delete(stateFile);throw;}
   return "Comfort UI 0.3.0 已安装。\r\n下次启动游戏时生效；配色和语言设置已保留。";
  }
  public static string Remove(string path){
   var p=Normalize(path);Guard(p,false);var target=Path.Combine(p,"Mods","CVRComfortUI.dll");var data=Path.Combine(p,"UserData","ComfortUI");var stateFile=Path.Combine(data,"install-state.json");var backup=Path.Combine(data,"backups");var state=ReadState(stateFile);
   if(state==null)throw new InvalidOperationException("没有找到本安装器的安装记录，未修改任何文件。");CheckState(state,target);
   if(File.Exists(target)&&HashFile(target)!=state.InstalledHash)throw new InvalidOperationException("模组安装后已被修改，已保留，请先检查文件。");
   byte[] original=null;if(!String.IsNullOrEmpty(state.OriginalBackup)){var b=Path.GetFullPath(state.OriginalBackup);if(!b.StartsWith(Normalize(backup)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!File.Exists(b))throw new InvalidDataException("原文件备份缺失或路径异常，已停止移除。");original=File.ReadAllBytes(b);}
   Directory.CreateDirectory(backup);string tag=Stamp();var previous=File.Exists(target)?File.ReadAllBytes(target):null;
   if(previous!=null)File.WriteAllBytes(Path.Combine(backup,"disabled-"+tag+".dll"),previous);
   try{Guard(p,false);if(original!=null)Atomic(target,original);else if(File.Exists(target))File.Delete(target);File.Move(stateFile,Path.Combine(backup,"uninstalled-"+tag+".json"));}
   catch{if(previous!=null)Atomic(target,previous);else if(File.Exists(target))File.Delete(target);throw;}
   return "Comfort UI 已移除，个人设置与备份已保留。\r\nMelonLoader 和其他模组没有变动。";
  }
  public static IEnumerable<string> LibraryPaths(string text){foreach(Match m in Regex.Matches(text,"\"path\"\\s*\"([^\"]+)\"",RegexOptions.IgnoreCase))yield return m.Groups[1].Value.Replace("\\\\","\\");foreach(Match m in Regex.Matches(text,"\"\\d+\"\\s*\"([^\"]+)\""))yield return m.Groups[1].Value.Replace("\\\\","\\");}
  public static List<string> Find(){
   var libs=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var games=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")){if(key!=null){var s=key.GetValue("SteamPath") as string;if(!String.IsNullOrEmpty(s))libs.Add(s);}}
   foreach(var view in new[]{RegistryView.Registry32,RegistryView.Registry64})try{using(var hive=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,view))using(var k=hive.OpenSubKey(@"SOFTWARE\Valve\Steam")){if(k!=null){var s=k.GetValue("InstallPath") as string;if(!String.IsNullOrEmpty(s))libs.Add(s);}}}catch{}
   libs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam"));
   foreach(var d in DriveInfo.GetDrives())try{if(d.IsReady){libs.Add(Path.Combine(d.RootDirectory.FullName,"SteamLibrary"));libs.Add(Path.Combine(d.RootDirectory.FullName,"Steam"));}}catch{}
   foreach(var l in libs.ToArray())try{var vdf=Path.Combine(l,"steamapps","libraryfolders.vdf");if(File.Exists(vdf))foreach(var other in LibraryPaths(File.ReadAllText(vdf)))libs.Add(other);}catch{}
   foreach(var l in libs)try{var manifest=Path.Combine(l,"steamapps","appmanifest_661130.acf");if(File.Exists(manifest)){var m=Regex.Match(File.ReadAllText(manifest),"\"installdir\"\\s*\"([^\"]+)\"");if(m.Success){var p=Path.Combine(l,"steamapps","common",m.Groups[1].Value);if(Game(p))games.Add(Normalize(p));}}var standard=Path.Combine(l,"steamapps","common","ChilloutVR");if(Game(standard))games.Add(Normalize(standard));}catch{}
   if(Game(AppDomain.CurrentDomain.BaseDirectory))games.Add(Normalize(AppDomain.CurrentDomain.BaseDirectory));
   return games.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();
  }
 }
 public sealed class MainForm:Form {
  readonly ComboBox path=new ComboBox();readonly Label status=new Label();readonly Label help=new Label();readonly Button install=new Button(),loader=new Button(),browse=new Button(),scan=new Button(),remove=new Button();bool busy;
  public MainForm(){
   Text="Comfort UI · 安装助手 0.3.0";ClientSize=new Size(720,450);MinimumSize=new Size(680,480);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(40,44,51);ForeColor=Color.FromArgb(245,246,248);AutoScaleMode=AutoScaleMode.Dpi;
   var grid=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(28),ColumnCount=3,RowCount=8};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));
   var title=new Label{Text="让 ChilloutVR 的菜单更舒服",AutoSize=true,Font=new Font(Font.FontFamily,19),Margin=new Padding(0,0,0,12)};grid.Controls.Add(title,0,0);grid.SetColumnSpan(title,3);
   var intro=new Label{Text="选择游戏文件夹，安装即可。模组已包含在程序中。",AutoSize=true,Margin=new Padding(0,0,0,18)};grid.Controls.Add(intro,0,1);grid.SetColumnSpan(intro,3);
   path.Dock=DockStyle.Fill;path.DropDownStyle=ComboBoxStyle.DropDown;path.Margin=new Padding(0,0,8,0);grid.Controls.Add(path,0,2);SetButton(browse,"选择文件夹");grid.Controls.Add(browse,1,2);SetButton(scan,"重新查找");grid.Controls.Add(scan,2,2);
   status.AutoSize=true;status.MaximumSize=new Size(640,0);status.Margin=new Padding(0,18,0,12);grid.Controls.Add(status,0,3);grid.SetColumnSpan(status,3);
   SetButton(install,"安装 / 更新模组");install.Height=52;install.Dock=DockStyle.Fill;install.BackColor=Color.FromArgb(193,203,220);install.ForeColor=Color.FromArgb(18,22,27);grid.Controls.Add(install,0,4);grid.SetColumnSpan(install,3);
   SetButton(loader,"安装加载器引导");loader.Dock=DockStyle.Fill;grid.Controls.Add(loader,0,5);grid.SetColumnSpan(loader,3);
   help.Text="首次使用：先完成官方 MelonLoader 安装，再回来安装模组。\r\n不会自动启动或关闭游戏；个人设置、其他模组保持不变。";help.AutoSize=true;help.MaximumSize=new Size(640,0);help.ForeColor=Color.FromArgb(193,198,204);help.Margin=new Padding(0,16,0,14);grid.Controls.Add(help,0,6);grid.SetColumnSpan(help,3);
   SetButton(remove,"移除本模组");remove.Dock=DockStyle.Left;remove.Width=150;grid.Controls.Add(remove,0,7);grid.SetColumnSpan(remove,3);Controls.Add(grid);
   browse.Click+=delegate{using(var d=new FolderBrowserDialog{Description="选择包含 ChilloutVR.exe 的游戏文件夹",ShowNewFolderButton=false}){if(Install.Game(path.Text))d.SelectedPath=path.Text;if(d.ShowDialog(this)==DialogResult.OK)path.Text=d.SelectedPath;}};
   scan.Click+=delegate{Scan();};path.TextChanged+=delegate{RefreshStatus();};install.Click+=delegate{Run(()=>Install.Apply(path.Text));};remove.Click+=delegate{if(MessageBox.Show(this,"移除 Comfort UI？个人设置会保留。","移除模组",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes)Run(()=>Install.Remove(path.Text));};loader.Click+=async delegate{await LoaderGuide();};
   Activated+=delegate{if(!busy)RefreshStatus();};FormClosing+=(s,e)=>{if(busy)e.Cancel=true;};Shown+=delegate{Scan();};
  }
  static void SetButton(Button b,string text){b.Text=text;b.AutoSize=false;b.Height=40;b.Dock=DockStyle.Fill;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=Color.FromArgb(97,104,116);b.BackColor=Color.FromArgb(52,58,67);b.ForeColor=Color.White;b.Margin=new Padding(0,3,8,3);b.Cursor=Cursors.Hand;}
  void Scan(){var found=Install.Find();path.Items.Clear();path.Items.AddRange(found.ToArray());if(found.Count==1)path.Text=found[0];else if(found.Count>1){path.SelectedIndex=-1;path.Text="";}RefreshStatus();}
  void RefreshStatus(){if(busy)return;bool game=Install.Game(path.Text);bool ready=game&&Install.Loader(path.Text);status.Text=!game?(path.Items.Count>1?"找到多个游戏目录，请选择需要安装的那一个。":"未找到游戏，请手动选择 ChilloutVR 文件夹。"):ready?"已找到游戏和加载器，可以安装模组。":"已找到游戏，下一步：安装 MelonLoader 加载器。";install.Enabled=ready;remove.Enabled=game;loader.Enabled=game;}
  void Enable(bool value){busy=!value;foreach(var b in new[]{install,loader,browse,scan,remove})b.Enabled=value;path.Enabled=value;if(value)RefreshStatus();}
  void Run(Func<string> action){try{var message=action();status.Text=message;MessageBox.Show(this,message,"Comfort UI",MessageBoxButtons.OK,MessageBoxIcon.Information);}catch(UnauthorizedAccessException){MessageBox.Show(this,"没有写入权限。请关闭程序，右键选择「以管理员身份运行」再试。","权限不足",MessageBoxButtons.OK,MessageBoxIcon.Information);}catch(Exception ex){MessageBox.Show(this,ex.Message,"未完成",MessageBoxButtons.OK,MessageBoxIcon.Information);}finally{RefreshStatus();}}
  async Task LoaderGuide(){
   if(Install.Running()){MessageBox.Show(this,"请先正常退出 ChilloutVR。");return;}
   var selected=path.Text;if(!Install.Game(selected))return;
   var message="将下载并打开 MelonLoader 官方安装器。\r\n\r\n在官方安装器中选择：\r\n"+Path.Combine(selected,"ChilloutVR.exe")+"\r\n\r\n完成后关闭官方安装器，回到这里点击「安装 / 更新模组」。\r\n不要在安装器中启动游戏。\r\n\r\n本模组已在 0.8.0-ci.2548 Mono 使用；其他版本尚需实机验证。\r\n是否继续下载？";
   if(MessageBox.Show(this,message,"加载器安装引导",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)!=DialogResult.OK)return;
   Enable(false);try{
    var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ComfortUI","Downloads");Directory.CreateDirectory(dir);var file=Path.Combine(dir,"MelonLoader.Installer-verified.exe");
    if(!File.Exists(file)||Install.HashFile(file)!=Install.LoaderHash){var temp=file+"."+Guid.NewGuid().ToString("N")+".part";try{ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;using(var web=new WebClient()){web.Headers[HttpRequestHeader.UserAgent]="ComfortUI-Setup/0.3.0";web.DownloadProgressChanged+=(s,e)=>status.Text="正在从 GitHub 下载官方安装器… "+e.ProgressPercentage+"%";await web.DownloadFileTaskAsync(new Uri(Install.LoaderUrl),temp);}if(Install.HashFile(temp)!=Install.LoaderHash)throw new InvalidDataException("官方安装器校验失败，未运行下载的文件。");if(File.Exists(file))File.Delete(file);File.Move(temp,file);}finally{if(File.Exists(temp))File.Delete(temp);}}
    if(Install.Running())throw new InvalidOperationException("游戏已启动，请先退出再安装加载器。");status.Text="请在官方安装器中完成加载器安装，然后回到这里。";
    using(var process=Process.Start(new ProcessStartInfo(file){UseShellExecute=true})){if(process!=null)await Task.Run(()=>process.WaitForExit());}
   }catch(Exception ex){MessageBox.Show(this,"加载器安装引导未完成："+ex.Message+"\r\n\r\n可从 github.com/LavaGang/MelonLoader/releases 获取官方安装器。","下载或运行未完成",MessageBoxButtons.OK,MessageBoxIcon.Information);}finally{Enable(true);}
  }
 }
 static class Program {
  [STAThread] static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);try{Install.Payload();Application.Run(new MainForm());}catch(Exception e){MessageBox.Show(e.Message,"Comfort UI 安装助手");}}
 }
}
