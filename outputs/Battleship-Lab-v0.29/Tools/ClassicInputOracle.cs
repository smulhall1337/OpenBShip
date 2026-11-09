using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Bounded keyboard-state writers, decay loop and Escape/P dispatcher only.
// No window procedure, physical input, audio or networking is executed.
class ClassicInputOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr size,uint flags,uint protection);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr size,uint protection,out uint previous);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr size,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Key(int window,int key,int down,int repeat,int flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Action();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Command(int command,int first,int second);
 byte[] source;int pe;uint imageBase;IntPtr memory;
 static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
 int Offset(uint va){uint rva=va-imageBase;int table=pe+24+BitConverter.ToUInt16(source,pe+20);for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++){int at=table+40*i;uint first=BitConverter.ToUInt32(source,at+12),size=BitConverter.ToUInt32(source,at+16);if(rva>=first&&rva-first<size)return checked((int)(BitConverter.ToUInt32(source,at+20)+rva-first));}throw new InvalidDataException("VA outside raw PE");}
 uint Address(int offset){return unchecked((uint)memory.ToInt32()+(uint)offset);}
 void Write(int offset,byte[] bytes){Marshal.Copy(bytes,0,IntPtr.Add(memory,offset),bytes.Length);}
 int Copy(uint first,uint end,uint[] globals,bool terminate){byte[] code=new byte[end-first];Array.Copy(source,Offset(first),code,0,code.Length);var allowed=new HashSet<uint>(globals);int count=0;for(int at=0;at<=code.Length-4;at++){uint value=BitConverter.ToUInt32(code,at);if(!allowed.Contains(value))continue;BitConverter.GetBytes(Address((int)(value-0x400000))).CopyTo(code,at);at+=3;count++;}Write((int)(first-0x400000),code);if(terminate)Write((int)(end-0x400000),new byte[]{0xc3});return count;}
 void Stub(int offset,Delegate callback){var code=new List<byte>(new byte[]{0xb8});code.AddRange(BitConverter.GetBytes(unchecked((uint)Marshal.GetFunctionPointerForDelegate(callback).ToInt32())));code.AddRange(new byte[]{0xff,0xe0});Write(offset,code.ToArray());}
 void Run(string exe,string destination){
  source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new InvalidDataException("Matching Bs.exe and x86 process required");pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try{
   int downRel=Copy(0x4245e0,0x424601,new uint[]{0x5ae570,0x563440},true),upRel=Copy(0x424640,0x42464f,new uint[]{0x563440},true),decayRel=Copy(0x401443,0x40145f,new uint[]{0x5ae570},true);
   int handlerRel=Copy(0x42277f,0x4227eb,new uint[]{0x5ae58b,0x4b46f8,0x4fbc40,0x4fbdf8,0x4ae82c,0x4b40b4,0x4fb6ec,0x5ae5c0},true),pauseRel=Copy(0x422cd0,0x422d14,new uint[]{0x4fb6ec,0x4b46f8},false);
   if(downRel!=2||upRel!=1||decayRel!=2||handlerRel!=8||pauseRel!=5)throw new InvalidDataException("Input relocation counts differ: "+downRel+","+upRel+","+decayRel+","+handlerRel+","+pauseRel);
   var calls=new List<string>();Action audioPause=delegate{calls.Add("AudioPause");},audioResume=delegate{calls.Add("AudioResume");},networkPause=delegate{calls.Add("NetworkPause");},networkNotify=delegate{calls.Add("NetworkNotify");},flush=delegate{calls.Add("NetworkFlush");};Command command=delegate(int c,int a,int b){calls.Add("NetworkCommand:"+c+":"+a+":"+b);};Stub(0x1a840,audioPause);Stub(0x1a880,audioResume);Stub(0x25780,networkPause);Stub(0x257b0,networkNotify);Stub(0x26ba0,command);Stub(0x26c20,flush);
   foreach(int page in new[]{0x1000,0x1a000,0x22000,0x24000,0x25000,0x26000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");}
   var keyDown=(Key)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x245e0),typeof(Key));var keyUp=(Key)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x24640),typeof(Key));var decay=(Action)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1443),typeof(Action));var handler=(Action)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x2277f),typeof(Action));
   Directory.CreateDirectory(destination);var rows=new List<string>{"Key,RepeatFlag,EventAfterDown,HeldAfterDown,FirstDecay,SecondDecay,HeldAfterUp,EventAfterUp"};
   foreach(int key in new[]{0,13,27,32,80,255})foreach(int repeat in new[]{0,0x4000}){Write(0x1ae570,new byte[256]);Write(0x163440,new byte[256]);keyDown(0,key,1,1,repeat);int pulse=Marshal.ReadByte(memory,0x1ae570+key),held=Marshal.ReadByte(memory,0x163440+key);decay();int first=Marshal.ReadByte(memory,0x1ae570+key);decay();int second=Marshal.ReadByte(memory,0x1ae570+key);keyUp(0,key,0,1,0);rows.Add(key+","+repeat+","+pulse+","+held+","+first+","+second+","+Marshal.ReadByte(memory,0x163440+key)+","+Marshal.ReadByte(memory,0x1ae570+key));}
   File.WriteAllLines(Path.Combine(destination,"Classic-Key-Pulse-Oracle.csv"),rows.ToArray());rows=new List<string>{"Paused,Escape,P,Network,FinalPaused,MenuState,ReturnState,ReturnMode,MenuPointer,Calls"};
   for(int paused=0;paused<2;paused++)for(int escape=0;escape<2;escape++)for(int p=0;p<2;p++)for(int network=0;network<3;network++){Marshal.WriteInt16(memory,0xb46f8,(short)paused);Marshal.WriteByte(memory,0x1ae58b,(byte)escape);Marshal.WriteByte(memory,0x1ae5c0,(byte)p);Marshal.WriteInt32(memory,0xfb6ec,network);foreach(int at in new[]{0xfbc40,0xfbdf8,0xae82c,0xb40b4})Marshal.WriteInt32(memory,at,0);calls.Clear();handler();rows.Add(paused+","+escape+","+p+","+network+","+Marshal.ReadInt16(memory,0xb46f8)+","+Marshal.ReadInt32(memory,0xfbc40)+","+Marshal.ReadInt32(memory,0xfbdf8)+","+Marshal.ReadInt32(memory,0xae82c)+","+Marshal.ReadInt32(memory,0xb40b4).ToString("X8")+","+string.Join("|",calls.ToArray()));}
   File.WriteAllLines(Path.Combine(destination,"Classic-Pause-Key-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured12key-state cases and24Escape/P/network-mode cases; audio/network callbacks are recording stubs.");GC.KeepAlive(audioPause);GC.KeepAlive(audioResume);GC.KeepAlive(networkPause);GC.KeepAlive(networkNotify);GC.KeepAlive(flush);GC.KeepAlive(command);
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){try{if(args.Length!=2)return 2;new ClassicInputOracle().Run(args[0],args[1]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
