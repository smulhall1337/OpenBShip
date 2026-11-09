using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Hash-locked three startup movies in HWEBS. Executes copied decoder
// arithmetic in its own x86 allocation. No game, graphics, sound or registry calls.
class StartupPatOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protect);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protect,out uint old);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Decode(IntPtr input,IntPtr output);
 byte[] source;int pe;uint imageBase;IntPtr memory;
 const int Input=0x180000,Output=0x1c0000,Consumed=0x1ff000;int Pixels,Columns,Rows,Scale;
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
  if(frame.Length>65536||frame[4]!=Columns||frame[5]!=Rows||frame[6]!=Scale||frame[7]!=Scale)throw new InvalidDataException("Oracle accepts only bounded original low/high Credits fixtures");
  Write(Input,frame);Marshal.WriteInt32(memory,Output-4,unchecked((int)0x5a3cc35a));Marshal.WriteInt32(memory,Output+Pixels,unchecked((int)0x5a3cc35a));
  decode(IntPtr.Add(memory,Input),IntPtr.Add(memory,Output));
  int consumed=unchecked((int)((uint)Marshal.ReadInt32(memory,Consumed)-Address(Input)));
  if(consumed!=frame.Length||Marshal.ReadInt32(memory,Output-4)!=unchecked((int)0x5a3cc35a)||Marshal.ReadInt32(memory,Output+Pixels)!=unchecked((int)0x5a3cc35a))throw new InvalidDataException("Original decoder length/canary differs: "+consumed+"/"+frame.Length);
  byte[] result=new byte[Pixels];Marshal.Copy(IntPtr.Add(memory,Output),result,0,result.Length);return result;
 }
 static readonly Dictionary<string,string> Trusted=new Dictionary<string,string>{
 {"HWEBS/HASBRO","63E0A172437496EF3C5C87ADCB930225516DED2C1428823A0AEECDF40F008BA4"},
 {"HWEBS/NMSLOGO1","C4D7CB091BD8F70CD9E1F35D33CAF590DC0C7F40AFE3CE9A07D7860AF5815030"},
 {"HWEBS/UK_INTRO","2FC444E2735A906502A80565D51065382A8DA919FF2802E6611BF73D31B5DABE"}
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
   var rows=new List<string>{"Directory,Clip,Frame,Offset,PayloadBytes,SHA256"};var firstRows=new List<string>{"Directory,Clip,Independent,ZeroSHA256,DirtySHA256"};int clips=0,total=0;
   foreach(var item in Trusted){
    string[] key=item.Key.Split('/');bool high=key[0]=="HWEBS";Columns=high?40:20;Rows=high?30:15;Scale=high?2:4;Pixels=Columns*Rows*64;byte[] movie=File.ReadAllBytes(Path.Combine(directory,key[0],key[1]+".PAT"));if(Hash(movie)!=item.Value)throw new InvalidDataException("Untrusted PAT: "+item.Key);
    Write(Output,new byte[Pixels]);int offset=0,index=0;
    while(offset<=movie.Length-4){uint n=BitConverter.ToUInt32(movie,offset);if(n==uint.MaxValue)break;if(n<4||n>65532||n>movie.Length-offset-4||index>=1024)throw new InvalidDataException("Trusted framing differs");byte[] frame=new byte[n+4];Array.Copy(movie,offset,frame,0,frame.Length);byte[] result=Frame(decode,frame);
     if(index==0){byte[] dirty=new byte[Pixels];for(int i=0;i<dirty.Length;i++)dirty[i]=0xa7;Write(Output,dirty);string dirtyHash=Hash(Frame(decode,frame));firstRows.Add(key[0]+","+key[1]+","+(dirtyHash==Hash(result)?1:0)+","+Hash(result)+","+dirtyHash);Write(Output,new byte[Pixels]);result=Frame(decode,frame);}
     rows.Add(key[0]+","+key[1]+","+index+","+offset+","+n+","+Hash(result));index++;offset+=frame.Length;
    }
    if(index==0||offset!=movie.Length-4)throw new InvalidDataException("Trusted terminal differs");clips++;total+=index;
   }
   Directory.CreateDirectory(destination);File.WriteAllLines(Path.Combine(destination,"Startup-Frames-Oracle.csv"),rows.ToArray());File.WriteAllLines(Path.Combine(destination,"Startup-FirstFrames-Oracle.csv"),firstRows.ToArray());Console.WriteLine("Captured "+total+" original-decoder frame hashes for "+clips+" trusted HWEBS startup resources, with exact consumed lengths and independent first frames.");
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 } static int Main(string[] args){if(args.Length!=3){Console.Error.WriteLine("StartupPatOracle <matching Bs.exe> <HWEBS directory> <destination>");return 2;}try{new StartupPatOracle().Run(args[0],args[1],args[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}



