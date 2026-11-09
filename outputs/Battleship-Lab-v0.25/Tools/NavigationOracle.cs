using System;using System.IO;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Security.Cryptography;
// Bounded paused-Escape -> Classic cleanup -> front-end initialization chain.
// All resource/audio/network/device calls are recording stubs, never native APIs.
class NavigationOracle {
 [DllImport("kernel32.dll")]static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protection);
 [DllImport("kernel32.dll")]static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protection,out uint old);
 [DllImport("kernel32.dll")]static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Step();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Call();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void One(int value);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Three(int a,int b,int c);
 byte[] source;int pe;IntPtr memory;uint basis;
 static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
 int Offset(uint va){int table=pe+24+BitConverter.ToUInt16(source,pe+20);uint rva=va-0x400000;for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++){int p=table+i*40;uint start=BitConverter.ToUInt32(source,p+12),size=BitConverter.ToUInt32(source,p+16);if(rva>=start&&rva-start<size)return (int)(BitConverter.ToUInt32(source,p+20)+rva-start);}throw new Exception("VA outside PE");}
 void Write(int at,byte[] b){Marshal.Copy(b,0,IntPtr.Add(memory,at),b.Length);}
 int P(uint va){return unchecked((int)(basis+va-0x400000));}int R(uint va){return Marshal.ReadInt32(memory,(int)(va-0x400000));}void W(uint va,int value){Marshal.WriteInt32(memory,(int)(va-0x400000),value);}
 int Copy(uint va,int count,HashSet<uint> globals){byte[] b=new byte[count];Array.Copy(source,Offset(va),b,0,count);int n=0;for(int i=0;i<=count-4;i++){uint value=BitConverter.ToUInt32(b,i);if(!globals.Contains(value))continue;BitConverter.GetBytes(P(value)).CopyTo(b,i);i+=3;n++;}Write((int)(va-0x400000),b);return n;}
 void Stub(uint va,Delegate callback){var b=new List<byte>{0xb8};b.AddRange(BitConverter.GetBytes(Marshal.GetFunctionPointerForDelegate(callback).ToInt32()));b.AddRange(new byte[]{0xff,0xe0});Write((int)(va-0x400000),b.ToArray());}
 void Run(string exe,string dest){source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new Exception("Matching executable/x86 required");pe=BitConverter.ToInt32(source,60);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation");basis=unchecked((uint)memory.ToInt32());try{
  var globals=new HashSet<uint>(new uint[]{0x5ae58b,0x4b46f8,0x4fbc40,0x4fbdf8,0x4ae82c,0x4b40b4,0x4fb6ec,0x5ae5c0,0x4231fc,0x4231e4,0x5c3348,0x5c3340,0x4fbe60,0x4ae824,0x4fb3d0,0x4e3888,0x4b4300,0x4fbdd4,0x4d297a,0x4b42a0,0x4ae81c,0x4e42cc,0x4e3880,0x4b46f0,0x4d2980,0x4b498c,0x4b4974,0x4b47b4,0x4e3814,0x4b47a0,0x4b4b60,0x4b49a0,0x5ae674,0x4d2820,0x563554,0x563550,0x457b58,0x457074});
  int input=Copy(0x42277f,0x6c,globals),controller=Copy(0x422f20,0x2c4,globals),front=Copy(0x42ece5,0x5f,globals);Write(0x227eb,new byte[]{0xc3});
  if(input!=8||controller!=68||front!=7)throw new Exception("Set reviewed relocation counts: "+input+"/"+controller+"/"+front);
  byte[] table=new byte[24];Array.Copy(source,Offset(0x4231e4),table,0,24);for(int i=0;i<table.Length;i+=4)BitConverter.GetBytes(P(BitConverter.ToUInt32(table,i))).CopyTo(table,i);Write(0x231e4,table);Copy(0x4231fc,20,new HashSet<uint>());
  // The original front-end function has already pushed ESI/EDI at this entry.
  var wrap=new List<byte>{0x56,0x57,0xe9};wrap.AddRange(BitConverter.GetBytes(0x2ece5-0x1007));Write(0x1000,wrap.ToArray());
  int failure=0;var calls=new List<string>();var keep=new List<Delegate>();
  Action<uint,string> stub=delegate(uint va,string name){Call callback=delegate{calls.Add(name);};keep.Add(callback);Stub(va,callback);};
  foreach(var pair in new[]{new object[]{0x426c20u,"NetworkFlush"},new object[]{0x4101e0u,"NetworkCleanup"},new object[]{0x41b6b0u,"Callback41B6B0"},new object[]{0x402980u,"Callback402980"},new object[]{0x402d20u,"Callback402D20"},new object[]{0x42b2f0u,"BattleResourceRelease"},new object[]{0x422bc0u,"FadeOut"}})stub((uint)pair[0],(string)pair[1]);
  One stop=delegate(int value){calls.Add("StopAudio:"+value);},bank=delegate(int value){calls.Add("Bank:"+value);},scan=delegate(int value){calls.Add("Script:"+value.ToString("X8"));},error=delegate(int ignored){calls.Add("ResourceError");};Three command=delegate(int a,int b,int c){calls.Add("NetworkCommand:"+a+":"+b+":"+c);};Step load=delegate{calls.Add("FrontResources");return failure;};Stub(0x41a720,stop);Stub(0x4012d0,bank);Stub(0x42f200,scan);Stub(0x41fd40,error);Stub(0x426ba0,command);Stub(0x42ee00,load);
  foreach(int page in new[]{0x1000,0x1a000,0x10000,0x1b000,0x02000,0x2b000,0x22000,0x23000,0x26000,0x2e000,0x2f000,0x1f000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection");}
  var keys=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x2277f),typeof(Call));var classic=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x22f20),typeof(Call));var init=(Step)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1000),typeof(Step));
  var rows=new List<string>{"Network,Flag4D2820,Failure,Stage,State,Mode,NextState,NextMode,Script,Return,Calls"};
  for(int network=0;network<=2;network++)for(int flag=0;flag<=1;flag++)for(int fail=0;fail<=1;fail++){
   W(0x4fb6ec,network);Marshal.WriteInt16(memory,0xb46f8,1);Marshal.WriteByte(memory,0x1ae58b,1);Marshal.WriteByte(memory,0x1ae5c0,0);W(0x4fbc40,0);W(0x4fbdf8,0);W(0x4ae82c,0);W(0x4b40b4,0);W(0x4fbdd4,14);Marshal.WriteByte(memory,0xb42a0,0);Marshal.WriteByte(memory,0xd2820,(byte)flag);Marshal.WriteInt16(memory,0xd297a,7);failure=fail;
   for(int stage=0;stage<4;stage++){calls.Clear();int result=0;if(stage==0)keys();else if(stage<3)classic();else result=init();rows.Add(network+","+flag+","+fail+","+stage+","+R(0x4fbc40)+","+R(0x4fbdd4)+","+R(0x4fbdf8)+","+R(0x4ae82c)+","+R(0x4b40b4).ToString("X8")+","+result+","+string.Join("|",calls.ToArray()));}
  }
  Directory.CreateDirectory(dest);File.WriteAllLines(Path.Combine(dest,"Navigation-Chain-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured12bounded navigation chains; original resources/audio/network/file APIs substituted.");GC.KeepAlive(keep);GC.KeepAlive(stop);GC.KeepAlive(bank);GC.KeepAlive(scan);GC.KeepAlive(error);GC.KeepAlive(command);GC.KeepAlive(load);
 }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}}
 static int Main(string[] args){try{if(args.Length!=2)return 2;new NavigationOracle().Run(args[0],args[1]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
