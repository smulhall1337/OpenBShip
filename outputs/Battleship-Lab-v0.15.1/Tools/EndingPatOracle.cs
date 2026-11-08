using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Hash-locked 61-clip whitelist. Executes copied decoder
// arithmetic in its own x86 allocation. No game, graphics, sound or registry calls.
class EndingPatOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protect);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protect,out uint old);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Decode(IntPtr input,IntPtr output);
 byte[] source;int pe;uint imageBase;IntPtr memory;
 const int Input=0x180000,Output=0x1c0000,Consumed=0x1ff000,Pixels=76800;
 static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
 int Offset(uint va){uint rva=va-imageBase;int table=pe+24+BitConverter.ToUInt16(source,pe+20);for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++){int at=table+i*40;uint start=BitConverter.ToUInt32(source,at+12),size=BitConverter.ToUInt32(source,at+16);if(rva>=start&&rva-start<size)return checked((int)(BitConverter.ToUInt32(source,at+20)+rva-start));}throw new InvalidDataException("VA outside raw sections");}
 byte[] Read(uint va,int count){byte[] b=new byte[count];Array.Copy(source,Offset(va),b,0,count);return b;}
 uint Address(int offset){return unchecked((uint)memory.ToInt32()+(uint)offset);}
 void Write(int offset,byte[] b){Marshal.Copy(b,0,IntPtr.Add(memory,offset),b.Length);}
 int Copy(uint va,int size){
  byte[] b=Read(va,size);int count=0;
  var globals=new HashSet<uint>(new uint[]{0x4517f0,0x4517f4,0x4517f8,0x4517fc,0x451804,0x451808,0x45185d,0x451865,0x451867,0x451869,0x45186d,0x451875,0x4540e1});
  for(int i=0;i<=b.Length-4;i++){uint value=BitConverter.ToUInt32(b,i);if(!globals.Contains(value))continue;BitConverter.GetBytes(Address((int)(value-0x400000))).CopyTo(b,i);i+=3;count++;}
  Write((int)(va-0x400000),b);return count;
 }
 void Table(uint va,int count,uint first,uint last){byte[] b=Read(va,count*4);for(int i=0;i<count;i++){uint target=BitConverter.ToUInt32(b,i*4);if(target<first||target>last)throw new InvalidDataException("Source dispatch pointer out of range");BitConverter.GetBytes(Address((int)(target-0x400000))).CopyTo(b,4*i);}Write((int)(va-0x400000),b);}
 byte[] Frame(Decode decode,byte[] frame){
  if(frame.Length>65536||frame[4]!=40||frame[5]!=30||frame[6]!=2||frame[7]!=2)throw new InvalidDataException("Oracle only accepts bounded 320x240 scale-two fixtures");
  Write(Input,frame);Marshal.WriteInt32(memory,Output-4,unchecked((int)0x5a3cc35a));Marshal.WriteInt32(memory,Output+Pixels,unchecked((int)0x5a3cc35a));
  decode(IntPtr.Add(memory,Input),IntPtr.Add(memory,Output));
  int consumed=unchecked((int)((uint)Marshal.ReadInt32(memory,Consumed)-Address(Input)));
  if(consumed!=frame.Length||Marshal.ReadInt32(memory,Output-4)!=unchecked((int)0x5a3cc35a)||Marshal.ReadInt32(memory,Output+Pixels)!=unchecked((int)0x5a3cc35a))throw new InvalidDataException("Original decoder length/canary differs: "+consumed+"/"+frame.Length);
  byte[] result=new byte[Pixels];Marshal.Copy(IntPtr.Add(memory,Output),result,0,result.Length);return result;
 }
 static readonly Dictionary<string,string> Trusted=new Dictionary<string,string>{
 {"VICTORY","3C9F8B88484EFEE0DA50AA620F6FCBDCC4CEBE5E7C8F93575E3D87225B65316D"},
 {"LOSE","D1E47B2F2F7F2792116EE3C3BFE77EA8609EE9BAE1BBDDB13C3BB016C8448401"}
 };
 void Run(string exe,string directory,string destination){
  source=File.ReadAllBytes(exe);
  if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new InvalidDataException("Expected matching Bs.exe and x86 oracle");
  pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);
  memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try {
   if(Copy(0x433920,0x10)!=0||Copy(0x433aa5,0xd8)!=22||Copy(0x433d41,0xf4f)!=34||Copy(0x437ebd,0xde)!=4)throw new InvalidDataException("Decoder relocations differ");
   Table(0x451875,13,0x433d41,0x437f03);Table(0x4540e1,256,0x434078,0x434c8e);
   if(BitConverter.ToString(Read(0x433b1d,7))!="61-A1-5D-18-45-00-C3")throw new InvalidDataException("Decoder exit differs");
   var jump=new List<byte>();jump.Add(0xe9);jump.AddRange(BitConverter.GetBytes(unchecked(0x433900-0x433b22)));Write(0x33b1d,jump.ToArray());
   var exit=new List<byte>(new byte[]{0x89,0x35});exit.AddRange(BitConverter.GetBytes(Address(Consumed)));exit.AddRange(new byte[]{0x61,0xc3});Write(0x33900,exit.ToArray());
   foreach(int page in new[]{0x33000,0x34000,0x37000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");}
   var decode=(Decode)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x33920),typeof(Decode));
   var rows=new List<string>{"Clip,Frame,Offset,PayloadBytes,SHA256"};int clips=0,total=0;
   foreach(var item in Trusted){
    byte[] movie=File.ReadAllBytes(Path.Combine(directory,item.Key+".PAT"));if(Hash(movie)!=item.Value)throw new InvalidDataException("Untrusted PAT: "+item.Key);
    Write(Output,new byte[Pixels]);int offset=0,index=0;
    while(offset<=movie.Length-4){uint n=BitConverter.ToUInt32(movie,offset);if(n==uint.MaxValue)break;if(n<4||n>65532||n>movie.Length-offset-4||index>=1024)throw new InvalidDataException("Trusted framing differs");byte[] frame=new byte[n+4];Array.Copy(movie,offset,frame,0,frame.Length);byte[] result=Frame(decode,frame);
     if(index==0){byte[] dirty=new byte[Pixels];for(int i=0;i<dirty.Length;i++)dirty[i]=0xa7;Write(Output,dirty);if(Hash(Frame(decode,frame))!=Hash(result))throw new InvalidDataException("First frame depends on previous surface: "+item.Key);}
     rows.Add(item.Key+","+index+","+offset+","+n+","+Hash(result));index++;offset+=frame.Length;
    }
    if(index==0||offset!=movie.Length-4)throw new InvalidDataException("Trusted terminal differs");clips++;total+=index;
   }
   Directory.CreateDirectory(destination);File.WriteAllLines(Path.Combine(destination,"Ending-Frames-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured "+total+" original-decoder frame hashes for "+clips+" trusted firing clips, with exact consumed lengths and independent first frames.");
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 } static int Main(string[] args){if(args.Length!=3){Console.Error.WriteLine("EndingPatOracle <matching Bs.exe> <HWEBS directory> <destination>");return 2;}try{new EndingPatOracle().Run(args[0],args[1],args[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}


