using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Executes only hash-guarded impact/miss/sink movie selectors. Rendering, sound,
// messages and movie loading are recording/no-op stubs in this private x86 process.
class ClassicImpactOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr size,uint flags,uint protect);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr size,uint protect,out uint old);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr size,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Hit(IntPtr ship,int weapon,int viewer);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Select(IntPtr ship);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Movie(int id,IntPtr ship);
 byte[] source;int pe;uint imageBase;IntPtr memory;
 static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}
 int Offset(uint va){uint rva=va-imageBase;int table=pe+24+BitConverter.ToUInt16(source,pe+20);for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++){int at=table+i*40;uint first=BitConverter.ToUInt32(source,at+12),size=BitConverter.ToUInt32(source,at+16);if(rva>=first&&rva-first<size)return checked((int)(BitConverter.ToUInt32(source,at+20)+rva-first));}throw new InvalidDataException("VA outside PE");}
 uint Address(int offset){return unchecked((uint)memory.ToInt32()+(uint)offset);}
 void Write(int offset,byte[] bytes){Marshal.Copy(bytes,0,IntPtr.Add(memory,offset),bytes.Length);}
 void Copy(uint first,uint end,int expected){
  byte[] code=new byte[end-first];Array.Copy(source,Offset(first),code,0,code.Length);int relocations=0;
  var globals=new HashSet<uint>(new uint[]{0x4fb3d8,0x4aa7fc,0x55e810,0x4b47a0,0x4b47a6,0x4e3868});
  for(int at=0;at<=code.Length-4;at++){uint value=BitConverter.ToUInt32(code,at);if(!globals.Contains(value)&&!(value>=first&&value<end))continue;BitConverter.GetBytes(Address((int)(value-0x400000))).CopyTo(code,at);at+=3;relocations++;}
  if(relocations!=expected)throw new InvalidDataException("Selector relocation count differs");Write((int)(first-0x400000),code);
 }
 void Run(string exe,string destination){
  source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new InvalidDataException("Expected matching Bs.exe and x86 process");
  pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);
  memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try {
   Copy(0x419ce0,0x41a211,22);Copy(0x419270,0x4193ae,18);Copy(0x4177a0,0x417981,21);
   int requested=0,calls=0;Movie movie=delegate(int id,IntPtr context){requested=id;calls++;return 0;};
   var jump=new List<byte>(new byte[]{0xb8});jump.AddRange(BitConverter.GetBytes(unchecked((uint)Marshal.GetFunctionPointerForDelegate(movie).ToInt32())));jump.AddRange(new byte[]{0xff,0xe0});
   Write(0x2460,jump.ToArray());Write(0x2ad0,jump.ToArray());
   foreach(int offset in new[]{0x2980,0x2d20,0x20550})Write(offset,new byte[]{0x31,0xc0,0xc3});
   var rng=new List<byte>(new byte[]{0xff,0x05});rng.AddRange(BitConverter.GetBytes(Address(0x1ff004)));rng.Add(0xa1);rng.AddRange(BitConverter.GetBytes(Address(0x1ff000)));rng.Add(0xc3);Write(0x39790,rng.ToArray());
   foreach(int page in new[]{0x19000,0x1a000,0x17000,0x2000,0x20000,0x39000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");}
   var hit=(Hit)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x19ce0),typeof(Hit));var sink=(Select)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x19270),typeof(Select));var miss=(Select)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x177a0),typeof(Select));
   Marshal.WriteInt32(memory,0xfb3d8,7);Marshal.WriteInt32(memory,0xe3868,20);Marshal.WriteInt32(memory,0xb47a0,1);
   IntPtr ship=IntPtr.Add(memory,0x1f0000);var rows=new List<string>{"Kind,Type,Length,Remaining,Weapon,Roll,Movie,RandomCalls,Requests"};
   int[] types={7,1,2,12,4},lengths={5,5,3,4,2},weapons={14,15,16,17,18,20,24,25,26,29,30};
   for(int index=0;index<types.Length;index++)for(int remaining=0;remaining<lengths[index];remaining++)foreach(int weapon in weapons){
    Marshal.WriteInt32(ship,0,types[index]);Marshal.WriteInt16(ship,14,(short)lengths[index]);Marshal.WriteInt16(ship,16,(short)remaining);requested=calls=0;Marshal.WriteInt32(memory,0x1ff004,0);hit(ship,weapon,-1);
    rows.Add("Hit,"+types[index]+","+lengths[index]+","+remaining+","+weapon+",0,"+requested+","+Marshal.ReadInt32(memory,0x1ff004)+","+calls);
   }
   foreach(int type in types)for(int roll=0;roll<2;roll++){
    Marshal.WriteInt32(ship,0,type);requested=calls=0;Marshal.WriteInt32(memory,0x1ff000,roll);Marshal.WriteInt32(memory,0x1ff004,0);sink(ship);
    rows.Add("Sink,"+type+",0,0,0,"+roll+","+requested+","+Marshal.ReadInt32(memory,0x1ff004)+","+calls);
   }
   foreach(int weapon in weapons)for(int roll=0;roll<3;roll++){
    Marshal.WriteInt32(ship,0,weapon);Marshal.WriteInt16(ship,12,0);requested=calls=0;Marshal.WriteInt32(memory,0x1ff000,roll);Marshal.WriteInt32(memory,0x1ff004,0);miss(ship);
    rows.Add("Miss,0,0,0,"+weapon+","+roll+","+requested+","+Marshal.ReadInt32(memory,0x1ff004)+","+calls);
   }
   Directory.CreateDirectory(destination);File.WriteAllLines(Path.Combine(destination,"Classic-Impact-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured "+(rows.Count-1)+" bounded impact/miss/sink selectors with stubbed movie calls.");GC.KeepAlive(movie);
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){try{if(args.Length!=2)return 2;new ClassicImpactOracle().Run(args[0],args[1]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}


