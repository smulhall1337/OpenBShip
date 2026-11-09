using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Executes ONLY hash-locked sequence polling/cleanup fragments in this x86 process.
// Sound requests/stops use managed recording stubs and successful dummy handles.
// No game process, original file, registry, graphics, sound or media is modified.
class ClassicPhaseOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protect);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protect,out uint old);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Advance();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Movie(int id,IntPtr ship);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Impact(int weapon,IntPtr shooter,IntPtr fleet,int x,int y); byte[] source;int pe;uint imageBase;IntPtr memory;
 const int Buffer=0x180000;
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
 void Run(string exe,string destination){
  source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new InvalidDataException("Expected matching Bs.exe and x86 oracle");
  pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try{
   byte[] code=Read(0x423540,0x384);int relocations=0;
   var globals=new HashSet<uint>(new uint[]{0x4fb6ec,0x4e41ec,0x4e41ee,0x4b4794,0x4fb6f4,0x4ae818,0x4ae81a,0x4fbc4c,0x4e375c,0x4e3818,0x4e381a,0x4e42d0,0x4238a4});
   for(int at=0;at<=code.Length-4;at++){uint value=BitConverter.ToUInt32(code,at);bool table=at>=0x364&&value>=0x423540&&value<0x423892;if(!globals.Contains(value)&&!table)continue;BitConverter.GetBytes(Address((int)(value-0x400000))).CopyTo(code,at);at+=3;relocations++;}
   if(relocations!=40)throw new InvalidDataException("Phase relocation count: "+relocations);Write(0x23540,code);
   int unavailable=0,impacts=0;var movies=new List<int>();Movie movie=delegate(int id,IntPtr ship){movies.Add(id);return unavailable;};Impact impact=delegate(int weapon,IntPtr shooter,IntPtr fleet,int x,int y){impacts++;};
   Action<int,Delegate> stub=delegate(int offset,Delegate callback){var bytes=new List<byte>(new byte[]{0xb8});bytes.AddRange(BitConverter.GetBytes(unchecked((uint)Marshal.GetFunctionPointerForDelegate(callback).ToInt32())));bytes.AddRange(new byte[]{0xff,0xe0});Write(offset,bytes.ToArray());};
   stub(0x2460,movie);stub(0x23fb0,impact);Write(0x12d0,new byte[]{0x31,0xc0,0xc3});
   var rng=new List<byte>(new byte[]{0xff,0x05});rng.AddRange(BitConverter.GetBytes(Address(0x1ff004)));rng.Add(0xa1);rng.AddRange(BitConverter.GetBytes(Address(0x1ff000)));rng.Add(0xc3);Write(0x39790,rng.ToArray());
   foreach(int page in new[]{0x23000,0x2000,0x1000,0x39000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Protection failed");}
   var advance=(Advance)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x23540),typeof(Advance));var rows=new List<string>{"Stage,Weapon,Roll,Unavailable,NextStage,Movie,Impacts,RandomCalls"};
   for(int stage=0;stage<8;stage++)foreach(int weapon in new[]{14,15,16,17,18,20,24,25,26,29,30})for(int roll=0;roll<3;roll++)for(unavailable=0;unavailable<2;unavailable++){
    Marshal.WriteInt16(memory,0xb4794,(short)stage);Marshal.WriteInt32(memory,0xfb6f4,weapon);Marshal.WriteInt32(memory,0x1ff000,roll);Marshal.WriteInt32(memory,0x1ff004,0);movies.Clear();impacts=0;advance();
    rows.Add(stage+","+weapon+","+roll+","+unavailable+","+Marshal.ReadInt16(memory,0xb4794)+","+(movies.Count==0?0:movies[0])+","+impacts+","+Marshal.ReadInt32(memory,0x1ff004));
   }
   Directory.CreateDirectory(destination);File.WriteAllLines(Path.Combine(destination,"Classic-Phase-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured "+(rows.Count-1)+" solo Classic phase/availability cases; original movie/impact/view calls are stubs.");GC.KeepAlive(movie);GC.KeepAlive(impact);
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){if(args.Length!=2){Console.Error.WriteLine("ClassicPhaseOracle <matching Bs.exe> <destination>");return 2;}try{new ClassicPhaseOracle().Run(args[0],args[1]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
