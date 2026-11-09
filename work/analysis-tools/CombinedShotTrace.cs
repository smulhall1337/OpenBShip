using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

// x86 debugger for an owned ClassicTargetOrderOracle helper only. It never attaches
// to an existing process; faults/timeouts terminate only the helper it started.
class FixtureTrace {
 [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] struct STARTUPINFO {
  public int cb; public string reserved, desktop, title;
  public int x,y,xSize,ySize,xChars,yChars,fill,flags; public short show,reserved2;
  public IntPtr reservedPtr,input,output,error;
 }
 [StructLayout(LayoutKind.Sequential)] struct PROCESS_INFORMATION { public IntPtr process,thread; public uint pid,tid; }
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool CreateProcess(string app,StringBuilder cmd,IntPtr pa,IntPtr ta,bool inherit,uint flags,IntPtr env,string cwd,ref STARTUPINFO si,out PROCESS_INFORMATION pi);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool WaitForDebugEvent(IntPtr ev,uint ms);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool ContinueDebugEvent(uint pid,uint tid,uint status);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool DebugActiveProcessStop(uint pid);
 [DllImport("kernel32.dll")] static extern bool DebugSetProcessKillOnExit(bool kill);
 [DllImport("kernel32.dll")] static extern bool TerminateProcess(IntPtr process,uint code);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
 [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenThread(uint access,bool inherit,uint tid);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetThreadContext(IntPtr thread,IntPtr ctx);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr process,IntPtr address,byte[] data,int count,out IntPtr read);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern uint GetFinalPathNameByHandle(IntPtr file,StringBuilder path,uint size,uint flags);
 [DllImport("dbghelp.dll",SetLastError=true)] static extern bool MiniDumpWriteDump(IntPtr process,uint pid,IntPtr file,uint type,IntPtr info,IntPtr user,IntPtr callbacks);
 static StreamWriter log; static IntPtr process; static List<KeyValuePair<uint,string>> modules=new List<KeyValuePair<uint,string>>();
 static Dictionary<uint,uint> sizes=new Dictionary<uint,uint>();
 static uint U(IntPtr b,int off){return unchecked((uint)Marshal.ReadInt32(b,off));}
 static IntPtr P(uint v){return new IntPtr(unchecked((int)v));}
 static void Say(string s){if(!s.StartsWith("Module ") && !s.StartsWith("stack+") && !s.StartsWith("frame ") && !s.StartsWith("Code @") && !s.StartsWith("EAX="))Console.WriteLine(s);log.WriteLine(s);log.Flush();}
 static string Name(IntPtr file){var b=new StringBuilder(2048);return GetFinalPathNameByHandle(file,b,2048,0)>0?b.ToString():"unknown";}
 static string Where(uint address){foreach(var m in modules){uint size;if(sizes.TryGetValue(m.Key,out size) && address>=m.Key && address-m.Key<size)return m.Value+"+0x"+(address-m.Key).ToString("X");}return "unmapped/unsymbolized";}
 static void AddModule(uint address,string name){modules.Add(new KeyValuePair<uint,string>(address,name));var h=Read(address,4096);if(h!=null){int pe=BitConverter.ToInt32(h,60);if(pe>=0 && pe+84<=h.Length)sizes[address]=BitConverter.ToUInt32(h,pe+80);}Say("Module "+address.ToString("X8")+" "+name);}
 static byte[] Read(uint address,int size){byte[] b=new byte[size];IntPtr got;if(!ReadProcessMemory(process,P(address),b,size,out got))return null;return b;}
 static void Capture(IntPtr ev,uint pid,uint tid,string dir){
  IntPtr thread=OpenThread(0x48,false,tid), ctx=Marshal.AllocHGlobal(716);
  try {
   for(int i=0;i<716;i++)Marshal.WriteByte(ctx,i,0);Marshal.WriteInt32(ctx,0x10007);
   if(!GetThreadContext(thread,ctx)){Say("GetThreadContext failed "+Marshal.GetLastWin32Error());return;}
   uint ip=U(ctx,184),sp=U(ctx,196),bp=U(ctx,180);
   Say("EIP="+ip.ToString("X8")+" ESP="+sp.ToString("X8")+" EBP="+bp.ToString("X8")+" "+Where(ip));
   Say("EAX="+U(ctx,176).ToString("X8")+" EBX="+U(ctx,164).ToString("X8")+" ECX="+U(ctx,172).ToString("X8")+" EDX="+U(ctx,168).ToString("X8")+" ESI="+U(ctx,160).ToString("X8")+" EDI="+U(ctx,156).ToString("X8"));
   var code=Read(ip-32,96);if(code!=null)Say("Code @"+(ip-32).ToString("X8")+": "+BitConverter.ToString(code));
   var stack=Read(sp,512);if(stack!=null){File.WriteAllBytes(Path.Combine(dir,"stack.bin"),stack);for(int i=0;i<stack.Length;i+=4){uint value=BitConverter.ToUInt32(stack,i);Say("stack+"+i.ToString("X3")+" "+value.ToString("X8")+" "+Where(value));}}
   for(int i=0;i<20 && bp!=0;i++){var frame=Read(bp,8);if(frame==null)break;uint next=BitConverter.ToUInt32(frame,0),ret=BitConverter.ToUInt32(frame,4);Say("frame "+i+" "+ret.ToString("X8")+" "+Where(ret));if(next<=bp || next-bp>0x100000)break;bp=next;}
   IntPtr pointers=Marshal.AllocHGlobal(8), info=Marshal.AllocHGlobal(12);
   try {Marshal.WriteIntPtr(pointers,ev+12);Marshal.WriteIntPtr(pointers,4,ctx);Marshal.WriteInt32(info,unchecked((int)tid));Marshal.WriteIntPtr(info,4,pointers);Marshal.WriteInt32(info,8,0);using(var f=File.Create(Path.Combine(dir,"crash.dmp"))){bool ok=MiniDumpWriteDump(process,pid,f.SafeFileHandle.DangerousGetHandle(),0x41,info,IntPtr.Zero,IntPtr.Zero);Say(ok?"Saved crash.dmp":"Minidump failed: "+Marshal.GetLastWin32Error());}}
   finally{Marshal.FreeHGlobal(info);Marshal.FreeHGlobal(pointers);}
  }finally{if(thread!=IntPtr.Zero)CloseHandle(thread);Marshal.FreeHGlobal(ctx);}
 }
 static string QuoteArgument(string value){
  var quoted=new StringBuilder("\"");int slashes=0;
  foreach(char c in value){
   if(c=='\\'){slashes++;continue;}
   if(c=='"'){quoted.Append('\\',slashes*2+1);quoted.Append(c);}
   else {quoted.Append('\\',slashes);quoted.Append(c);}
   slashes=0;
  }
  quoted.Append('\\',slashes*2);quoted.Append('"');return quoted.ToString();
 }
 static int Main(string[] args){try{return Run(args);}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 static int Run(string[] args){
  if(args.Length!=4){Console.Error.WriteLine("FixtureTrace <ClassicCombinedShotOracle.exe> <diagnostic-dir> <original> <evidence>");return 2;}
  string exe=Path.GetFullPath(args[0]),dir=Path.Combine(Path.GetFullPath(args[1]),DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
  if(!string.Equals(exe,Path.GetFullPath("work\\analysis-tools\\ClassicCombinedShotOracle.exe"),StringComparison.OrdinalIgnoreCase))throw new Exception("Only the newly owned combined-shot probe is accepted");
  Directory.CreateDirectory(dir);
  using(log=new StreamWriter(Path.Combine(dir,"trace.txt"))){
   STARTUPINFO si=new STARTUPINFO();si.cb=Marshal.SizeOf(si);PROCESS_INFORMATION pi;
   if(!CreateProcess(exe,new StringBuilder(QuoteArgument(exe)+" "+QuoteArgument(Path.GetFullPath(args[2]))+" "+QuoteArgument(Path.GetFullPath(args[3]))),IntPtr.Zero,IntPtr.Zero,false,2,IntPtr.Zero,Path.GetDirectoryName(exe),ref si,out pi)){Say("CreateProcess failed: "+Marshal.GetLastWin32Error());return 3;}
   process=pi.process;DebugSetProcessKillOnExit(true);Say("Started diagnostic PID "+pi.pid);Say("Diagnostic directory: "+dir);
   IntPtr ev=Marshal.AllocHGlobal(96);bool exited=false,captured=false;uint childExit=0;var timer=Stopwatch.StartNew();
   try{while(timer.Elapsed.TotalSeconds<45){if(!WaitForDebugEvent(ev,1000))continue;uint type=U(ev,0),pid=U(ev,4),tid=U(ev,8),status=0x10002;
    if(type==3){IntPtr file=Marshal.ReadIntPtr(ev,12);uint address=U(ev,24);AddModule(address,exe);if(file!=IntPtr.Zero)CloseHandle(file);}
    else if(type==6){IntPtr file=Marshal.ReadIntPtr(ev,12);uint address=U(ev,16);AddModule(address,Name(file));if(file!=IntPtr.Zero)CloseHandle(file);}
    else if(type==2){IntPtr t=Marshal.ReadIntPtr(ev,12);if(t!=IntPtr.Zero)CloseHandle(t);}
    else if(type==1){uint exception=U(ev,12),first=U(ev,92);Say("Exception 0x"+exception.ToString("X8")+" firstChance="+first+" address="+U(ev,24).ToString("X8"));
     if(exception!=0x80000003)status=0x80010001;
     if(exception==0xc0000005 && !captured){Say("Access kind="+U(ev,32)+" target="+U(ev,36).ToString("X8"));Capture(ev,pid,tid,dir);captured=true;TerminateProcess(process,0xc0000005);}
    }else if(type==5){childExit=U(ev,12);Say("Exit code 0x"+childExit.ToString("X8"));exited=true;}
    ContinueDebugEvent(pid,tid,status);if(exited)break;
   }}finally{if(!exited){Say("Fixture timeout: terminating only the helper started by this debugger.");TerminateProcess(process,0x102);DebugActiveProcessStop(pi.pid);}Marshal.FreeHGlobal(ev);CloseHandle(pi.thread);CloseHandle(pi.process);}
   return captured?1:(exited?(childExit==0?0:1):2);
  }
 }
}
