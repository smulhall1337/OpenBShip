using System;
using System.Text;
using System.Runtime.InteropServices;
public static class OwnedWindowCheck {
 public delegate bool Callback(IntPtr window,IntPtr arg);
 [DllImport("user32.dll")] static extern bool EnumWindows(Callback callback,IntPtr arg);
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window,StringBuilder text,int count);
 [DllImport("user32.dll")] static extern bool PostMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
 [DllImport("kernel32.dll")] static extern bool GetExitCodeProcess(IntPtr process,out uint code);
 public static uint ExitCode(IntPtr handle) {uint code;if(!GetExitCodeProcess(handle,out code))throw new Exception("Exit query failed");return code;}
 public static string Close(int process) {return Close(process,"Battleship Fleet Lab ");}
 public static string Close(int process,string titlePrefix) {
  string result="No matching main form";
  EnumWindows(delegate(IntPtr window,IntPtr arg){uint pid;GetWindowThreadProcessId(window,out pid);if(pid!=(uint)process)return true;var text=new StringBuilder(256);GetWindowText(window,text,256);if(!text.ToString().StartsWith(titlePrefix))return true;result="Owned PID "+process+" main form found: "+text+"; WM_CLOSE posted="+PostMessage(window,0x10,IntPtr.Zero,IntPtr.Zero);return true;},IntPtr.Zero);
  return result;
 }
}

