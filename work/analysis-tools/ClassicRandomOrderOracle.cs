using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Executes ONLY hash-locked selector/dispatcher fragments in this new x86 process.
// Executes the original shared CRT RNG; recording callbacks replace media/target/stage work.
// No game process, original file, registry, graphics, sound or media is modified.
class ClassicRandomOrderOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protect);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protect,out uint old);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Select();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Dispatch(int weapon,IntPtr ship);
 byte[] source;int pe;uint imageBase;IntPtr memory;
 const int Input=0x1ff000,Rolls=0x1ff004,Movie=0x1ff008,Movies=0x1ff00c,Ship=0x1ff020;
 int Offset(uint va) {
  uint rva=va-imageBase;int table=pe+24+BitConverter.ToUInt16(source,pe+20);
  for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++) {
   int at=table+i*40;uint start=BitConverter.ToUInt32(source,at+12),size=BitConverter.ToUInt32(source,at+16);
   if(rva>=start&&rva-start<size)return checked((int)(BitConverter.ToUInt32(source,at+20)+rva-start));
  }
  throw new InvalidDataException("Address outside raw PE sections");
 }
 byte[] Read(uint va,int length){byte[] b=new byte[length];Array.Copy(source,Offset(va),b,0,length);return b;}
 uint Address(int offset){return unchecked((uint)memory.ToInt32()+(uint)offset);}
 void Write(int offset,byte[] bytes){Marshal.Copy(bytes,0,IntPtr.Add(memory,offset),bytes.Length);}
 static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
 void CopyRelocated(uint va,int count) {
  byte[] bytes=Read(va,count);int changes=0;
  var globals=new HashSet<uint>(new uint[]{0x4fb3d8,0x4aa7fc,0x55e810,0x4b47a0,0x4169d0,0x41698c,0x4e3814,0x44d730,0x44d750,0x44d760,0x4b4b60,0x4e42d0,0x4b499c,0x4fb6f4,0x4ae818,0x4ae81a,0x4e375c,0x4fbc4c,0x454d50});
  for(int at=0;at<=bytes.Length-4;at++) {
   uint operand=BitConverter.ToUInt32(bytes,at);
   bool table=va==0x416640&&at>=0x34c&&at<0x390&&operand>=0x416640&&operand<=0x416988;
   if(!globals.Contains(operand)&&!table)continue;
   BitConverter.GetBytes(Address((int)(operand-0x400000))).CopyTo(bytes,at);changes++;at+=3;
  }
  int expected=va==0x416640?24:va==0x423ede?4:va==0x41c8c0?11:va==0x439780?1:2;
  if(changes!=expected)throw new InvalidDataException("Relocation count differs at "+va.ToString("X8")+": "+changes);
  Write((int)(va-0x400000),bytes);
 }
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int RandomCall(int returnAddress);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Target(IntPtr x,IntPtr y);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int LoadMovie(int id,IntPtr owner);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Call();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Seed(uint value);
 void Stub(int at,Delegate callback){var b=new List<byte>{0xb8};b.AddRange(BitConverter.GetBytes(Marshal.GetFunctionPointerForDelegate(callback).ToInt32()));b.AddRange(new byte[]{0xff,0xe0});Write(at,b.ToArray());}
 void Run(string exe,string destination){source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new Exception("Matching image/x86 required");pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation");try{
  CopyRelocated(0x416640,0x3b2);CopyRelocated(0x423ede,0x42);CopyRelocated(0x41c8c0,0x7b);CopyRelocated(0x439790,0x2a);CopyRelocated(0x439780,10);
  // Move the position-independent leaf rand/srand copies to owned helper slots.
  byte[] rand=new byte[0x2a],srand=new byte[10];Marshal.Copy(IntPtr.Add(memory,0x39790),rand,0,rand.Length);Marshal.Copy(IntPtr.Add(memory,0x39780),srand,0,srand.Length);Write(0x1000,rand);Write(0x1100,srand);
  Write(0x23edd,new byte[]{0x53});Write(0x23f20,new byte[]{0x8b,0xc3,0x5b,0xc3});Write(0x4d730,Read(0x44d730,0x48));Marshal.WriteInt32(memory,0xfb3d8,7);Marshal.WriteInt32(memory,0xe3814,unchecked((int)Address(Ship)));Marshal.WriteInt32(memory,0xb499c,unchecked((int)Address(Ship)));Marshal.WriteByte(memory,0xb4b60,0);
  Select nativeRandom=null;var rows=new List<string>{"Seed,ShipType,Side,Order,Kind,CallerVA,Value,StateAfter"};uint inputSeed=0;int type=7,order=0;string side="";uint state=0;
  Action<string,uint,int> record=delegate(string kind,uint caller,int value){state=unchecked((uint)Marshal.ReadInt32(memory,0x54d50));rows.Add(inputSeed+","+type+","+side+","+(order++)+","+kind+","+caller.ToString("X8")+","+value+","+state);};
  RandomCall random=delegate(int ret){int value=nativeRandom();uint caller=unchecked((uint)ret)-Address(0)+0x400000-5;record("rand",caller,value);return value;};var wrapper=new List<byte>{0xff,0x34,0x24,0xb8};wrapper.AddRange(BitConverter.GetBytes(Marshal.GetFunctionPointerForDelegate(random).ToInt32()));wrapper.AddRange(new byte[]{0xff,0xd0,0x83,0xc4,4,0xc3});Write(0x39790,wrapper.ToArray());
  LoadMovie movie=delegate(int id,IntPtr owner){record("MovieRequest",0,id);return 0;};Target target=delegate(IntPtr x,IntPtr y){record("TargetRequestStub",0,Marshal.ReadInt32(memory,0xfb6f4));Marshal.WriteInt16(x,4);Marshal.WriteInt16(y,6);};Call stage=delegate{record("NextStageStub",0,0);};Stub(0x2460,movie);Stub(0x1c940,target);Stub(0x23540,stage);
  foreach(int page in new[]{0x1000,0x16000,0x23000,0x1c000,0x39000,0x2000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Protection");}nativeRandom=(Select)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1000),typeof(Select));var seed=(Seed)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1100),typeof(Seed));var player=(Select)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x23edd),typeof(Select));var opponent=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1c8c0),typeof(Call));var dispatch=(Dispatch)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x16640),typeof(Dispatch));
  foreach(uint s in new[]{0u,1u,731u,uint.MaxValue})foreach(int t in new[]{7,1,2,12,4})foreach(string who in new[]{"PlayerPairHarness","OpponentNativePrefix"}){inputSeed=s;type=t;side=who;order=0;seed(s);Marshal.WriteInt32(memory,Ship,t);if(who=="PlayerPairHarness"){int weapon=player();record("SelectedWeapon",0,weapon);dispatch(weapon,IntPtr.Add(memory,Ship));}else opponent();record("Boundary",0,0);}
  Directory.CreateDirectory(destination);File.WriteAllLines(Path.Combine(destination,"Native-Weapon-Random-Traces.csv"),rows.ToArray());Console.WriteLine("Captured40bounded shared-native-RNG probes. Player selector+dispatch order is a harness; opponent prefix order is original code. Target/next-stage/media remain stubs.");GC.KeepAlive(random);GC.KeepAlive(nativeRandom);GC.KeepAlive(movie);GC.KeepAlive(target);GC.KeepAlive(stage);
 }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}}
 static int Main(string[] args){try{if(args.Length!=2&&args.Length!=3)return 2;new ClassicRandomOrderOracle().Run(args[0],args[args.Length-1]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}


