using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Executes ONLY hash-locked selector/dispatcher fragments in this new x86 process.
// RNG returns a controlled fixture value. Movie calls record an ID and return.
// No game process, original file, registry, graphics, sound or media is modified.
class ClassicWeaponsOracle {
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
  var globals=new HashSet<uint>(new uint[]{0x4fb3d8,0x4aa7fc,0x55e810,0x4b47a0,0x4169d0,0x41698c,0x4e3814,0x44d730,0x44d750,0x44d760});
  for(int at=0;at<=bytes.Length-4;at++) {
   uint operand=BitConverter.ToUInt32(bytes,at);
   bool table=va==0x416640&&at>=0x34c&&at<0x390&&operand>=0x416640&&operand<=0x416988;
   if(!globals.Contains(operand)&&!table)continue;
   BitConverter.GetBytes(Address((int)(operand-0x400000))).CopyTo(bytes,at);changes++;at+=3;
  }
  int expected=va==0x416640?24:va==0x423ede?4:2;
  if(changes!=expected)throw new InvalidDataException("Relocation count differs at "+va.ToString("X8")+": "+changes);
  Write((int)(va-0x400000),bytes);
 }
 void Run(string executable,string directory) {
  source=File.ReadAllBytes(executable);
  if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new InvalidDataException("Expected matching original PE and x86 oracle");
  pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);
  memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try {
   CopyRelocated(0x416640,0x3b2);
   CopyRelocated(0x423ede,0x42);
   CopyRelocated(0x41c8d9,0x28);
   Write(0x23edd,new byte[]{0x53});Write(0x23f20,new byte[]{0x8b,0xc3,0x5b,0xc3}); // save EBX; return selected EBX.
   Write(0x1c8d8,new byte[]{0x56});Write(0x1c901,new byte[]{0x8b,0xc6,0x5e,0xc3}); // save ESI; return selected ESI.
   Write(0x4d730,Read(0x44d730,0x48));
   Marshal.WriteInt32(memory,0xfb3d8,7);
   Marshal.WriteInt32(memory,0xe3814,unchecked((int)Address(Ship)));
   var rng=new List<byte>();rng.AddRange(new byte[]{0xff,0x05});rng.AddRange(BitConverter.GetBytes(Address(Rolls)));rng.Add(0xa1);rng.AddRange(BitConverter.GetBytes(Address(Input)));rng.Add(0xc3);Write(0x39790,rng.ToArray());
   var movie=new List<byte>(new byte[]{0x8b,0x44,0x24,0x04,0xa3});movie.AddRange(BitConverter.GetBytes(Address(Movie)));movie.AddRange(new byte[]{0xff,0x05});movie.AddRange(BitConverter.GetBytes(Address(Movies)));movie.AddRange(new byte[]{0x31,0xc0,0xc3});Write(0x2460,movie.ToArray());
   foreach(int page in new[]{0x16000,0x23000,0x1c000,0x39000,0x2000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");}
   var player=(Select)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x23edd),typeof(Select));
   var opponent=(Select)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1c8d8),typeof(Select));
   var dispatch=(Dispatch)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x16640),typeof(Dispatch));
   int[] types={7,1,2,12,4};var selections=new List<string>{"ShipType,Opponent,Input,WeaponID,RandomCalls"};
   foreach(int type in types)foreach(bool ai in new[]{false,true})for(int input=0;input<50;input++) {
    Marshal.WriteInt32(memory,Ship,type);Marshal.WriteInt32(memory,Input,input);Marshal.WriteInt32(memory,Rolls,0);
    int result=ai?opponent():player();selections.Add(type+","+(ai?1:0)+","+input+","+result+","+Marshal.ReadInt32(memory,Rolls));
   }
   var movies=new List<string>{"ShipType,WeaponID,Input,MovieID,RandomCalls,MovieCalls"};
   foreach(int type in types)foreach(int weapon in new[]{14,15,16,17,18,20,24,25,26,29,30})for(int input=0;input<4;input++) {
    Marshal.WriteInt32(memory,Ship,type);Marshal.WriteInt32(memory,Input,input);Marshal.WriteInt32(memory,Rolls,0);Marshal.WriteInt32(memory,Movie,-1);Marshal.WriteInt32(memory,Movies,0);
    dispatch(weapon,IntPtr.Add(memory,Ship));
    movies.Add(type+","+weapon+","+input+","+Marshal.ReadInt32(memory,Movie)+","+Marshal.ReadInt32(memory,Rolls)+","+Marshal.ReadInt32(memory,Movies));
   }
   Directory.CreateDirectory(directory);File.WriteAllLines(Path.Combine(directory,"Weapons-Oracle.csv"),selections.ToArray());File.WriteAllLines(Path.Combine(directory,"Firing-Movies-Oracle.csv"),movies.ToArray());
   Console.WriteLine("Captured 500 list selections and 220 firing-movie dispatch outputs; two grouped numerical fixtures.");
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){if(args.Length!=2){Console.Error.WriteLine("ClassicWeaponsOracle <matching Bs.exe> <output directory>");return 2;}try{new ClassicWeaponsOracle().Run(args[0],args[1]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
