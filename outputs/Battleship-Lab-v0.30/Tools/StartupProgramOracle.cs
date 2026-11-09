using System;using System.IO;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Security.Cryptography;
// Runs only native ending prefix dispatcher/fade fragments. File, movie, clock
// and graphics calls are recording stubs; stops BEFORE the menu handoff opcode.
class StartupProgramOracle {
 [DllImport("kernel32.dll")]static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint f,uint protection);
 [DllImport("kernel32.dll")]static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protection,out uint old);
 [DllImport("kernel32.dll")]static extern bool VirtualFree(IntPtr p,UIntPtr n,uint f);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Step();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Fade(IntPtr input);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Load(IntPtr output,IntPtr name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Call();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Wait(int ms);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Movie(int id);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate uint Clock();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Palette(IntPtr rgb);
 byte[] source;int pe;IntPtr memory;uint basis;
 static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
 int Offset(uint va){int table=pe+24+BitConverter.ToUInt16(source,pe+20);uint rva=va-0x400000;for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++){int p=table+i*40;uint start=BitConverter.ToUInt32(source,p+12),size=BitConverter.ToUInt32(source,p+16);if(rva>=start&&rva-start<size)return (int)(BitConverter.ToUInt32(source,p+20)+rva-start);}throw new Exception("Bad VA");}
 void Write(int at,byte[] b){Marshal.Copy(b,0,IntPtr.Add(memory,at),b.Length);}
 int Copy(uint va,int bytes,HashSet<uint> globals){byte[] b=new byte[bytes];Array.Copy(source,Offset(va),b,0,bytes);int count=0;for(int i=0;i<=bytes-4;i++){uint value=BitConverter.ToUInt32(b,i);if(!globals.Contains(value))continue;BitConverter.GetBytes(basis+value-0x400000).CopyTo(b,i);i+=3;count++;}Write((int)(va-0x400000),b);return count;}
 void Stub(int at,Delegate callback){var b=new List<byte>{0xb8};b.AddRange(BitConverter.GetBytes(Marshal.GetFunctionPointerForDelegate(callback).ToInt32()));b.AddRange(new byte[]{0xff,0xe0});Write(at,b.ToArray());}
 void Run(string exe,string dest){source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new Exception("Matching image/x86 required");pe=BitConverter.ToInt32(source,60);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x400000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation");basis=unchecked((uint)memory.ToInt32());try{
  var globals=new HashSet<uint>(new uint[]{0x4e3880,0x4b40b4,0x457074,0x4e42e0,0x4e42cc,0x4ae81c,0x4e3888,0x4b4300,0x4fbdd4,0x4e4300,0x5ae67e,0x4fb3e0,0x5ae680,0x42fda4,0x42fd88,0x42fdc8,0x44eca4});
  int dispatch=Copy(0x42f300,0xa86,globals),fade=Copy(0x422ba0,0x12e,globals);if(dispatch!=136||fade!=22)throw new Exception("Relocation count differs");
  foreach(var span in new[]{new uint[]{0x42fd88,7},new uint[]{0x42fdc8,41}}){byte[] b=new byte[span[1]*4];Array.Copy(source,Offset(span[0]),b,0,b.Length);for(int i=0;i<b.Length;i+=4){uint va=BitConverter.ToUInt32(b,i);BitConverter.GetBytes(basis+va-0x400000).CopyTo(b,i);}Write((int)(span[0]-0x400000),b);}Copy(0x42fda4,0x24,new HashSet<uint>());
  foreach(uint va in new[]{0x44ee64u,0x44ee5cu,0x44eca4u})Copy(va,16,new HashSet<uint>());
  Copy(0x44ef48,64,new HashSet<uint>{0x44ef88});
  Marshal.WriteInt32(memory,0x4ae81c-0x400000,unchecked((int)(basis+0x200000)));Marshal.WriteInt32(memory,0x4e42cc-0x400000,unchecked((int)(basis+0x250000)));
  var rows=new List<string>{"Outcome,FinishCode,Order,Kind,Value"};string outcome="";int finish=6,order=0,polls=0;uint time=0;Action<string,string> record=delegate(string kind,string value){rows.Add(outcome+","+finish+","+(order++)+","+kind+","+value);};
  Load load=delegate(IntPtr target,IntPtr name){record("Card",Marshal.PtrToStringAnsi(name));return 0;};Wait wait=delegate(int ms){record("Wait",ms.ToString());time+=(uint)ms;};Movie movie=delegate(int id){if(id!=0){polls=1;record("Movie",id.ToString());return 0;}return ++polls<4?0:finish;};Call clear=delegate{record("Clear","0");};Clock clock=delegate{return ++time;};Palette palette=delegate(IntPtr rgb){record("Fade",Marshal.ReadInt16(memory,0x1ae67e).ToString());};
  Stub(0x22d90,load);Stub(0x222b0,wait);Stub(0x2fe70,movie);Stub(0x26ec0,clear);Stub(0x222a0,clock);Stub(0x1d40,palette);
  foreach(int page in new[]{0x2f000,0x22000,0x26000,0x1000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Protection");}
  var step=(Step)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x2f300),typeof(Step));var increase=(Fade)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x22c70),typeof(Fade));var decrease=(Step)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x22c10),typeof(Step));
  foreach(int result in new[]{6,9})foreach(uint script in new[]{0x44ef48u}){outcome="Startup";finish=result;order=0;polls=0;time=100;Marshal.WriteInt32(memory,0xb40b4,unchecked((int)(basis+script-0x400000)));Marshal.WriteInt32(memory,0x57074,0);Marshal.WriteInt32(memory,0xfbdd4,2);int loops=0;while(true){uint pointer=unchecked((uint)Marshal.ReadInt32(memory,0xb40b4));int mode=Marshal.ReadInt32(memory,0xfbdd4);if(mode==2&&pointer==basis+script-0x400000+48){record("Boundary","18");break;}if(++loops>200)throw new Exception("Prefix did not finish");if(mode==7||mode==8){int done=mode==7?increase(IntPtr.Add(memory,0xfb3e0)):decrease();if(done!=0)Marshal.WriteInt32(memory,0xfbdd4,Marshal.ReadInt32(memory,0xe4300));}else {int returned=step();if(returned!=0)throw new Exception("Unexpected native return"+returned);}}
  }
  Directory.CreateDirectory(dest);File.WriteAllLines(Path.Combine(dest,"Startup-Program-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured2native startup sequence traces through menu boundary; movie/file/time/palette calls stubbed.");GC.KeepAlive(load);GC.KeepAlive(wait);GC.KeepAlive(movie);GC.KeepAlive(clear);GC.KeepAlive(clock);GC.KeepAlive(palette);
 }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}}
 static int Main(string[] args){try{if(args.Length!=2)return 2;new StartupProgramOracle().Run(args[0],args[1]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}



