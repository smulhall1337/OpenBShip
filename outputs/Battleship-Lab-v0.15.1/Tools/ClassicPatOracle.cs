using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Hash-locked BATFIRE1 and constructed fixtures only. Executes copied decoder
// arithmetic in its own x86 allocation. No game, graphics, sound or registry calls.
class ClassicPatOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protect);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protect,out uint old);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Decode(IntPtr input,IntPtr output);
 byte[] source;int pe;uint imageBase;IntPtr memory;
 const int Input=0x180000,Output=0x1c0000,Consumed=0x1ff000,Pixels=19200;
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
  if(frame.Length>65536||frame[4]!=20||frame[5]!=15||frame[6]!=4||frame[7]!=4)throw new InvalidDataException("Oracle only accepts bounded 160x120 scale-four fixtures");
  Write(Input,frame);Marshal.WriteInt32(memory,Output-4,unchecked((int)0x5a3cc35a));Marshal.WriteInt32(memory,Output+Pixels,unchecked((int)0x5a3cc35a));
  decode(IntPtr.Add(memory,Input),IntPtr.Add(memory,Output));
  int consumed=unchecked((int)((uint)Marshal.ReadInt32(memory,Consumed)-Address(Input)));
  if(consumed!=frame.Length||Marshal.ReadInt32(memory,Output-4)!=unchecked((int)0x5a3cc35a)||Marshal.ReadInt32(memory,Output+Pixels)!=unchecked((int)0x5a3cc35a))throw new InvalidDataException("Original decoder length/canary differs: "+consumed+"/"+frame.Length);
  byte[] result=new byte[Pixels];Marshal.Copy(IntPtr.Add(memory,Output),result,0,result.Length);return result;
 }
 static byte[] PatternFixture(){
  var payload=new List<byte>(new byte[]{20,15,4,4});
  for(int block=0;block<300;block++){
   if(block>=256){payload.Add(0);payload.Add(21);continue;}
   payload.AddRange(new byte[]{4,11,22,33});
   for(int row=0;row<8;row++){
    payload.Add((byte)block);payload.Add((byte)block);
    for(int half=0;half<2;half++){int literal=101;for(int bit=6;bit>=0;bit-=2)if(((block>>bit)&3)==3)payload.Add((byte)literal++);}
   }
  }
  var bytes=new List<byte>();bytes.AddRange(BitConverter.GetBytes(payload.Count));bytes.AddRange(payload);return bytes.ToArray();
 }
 static byte[] AllOpsFixture(bool solid){
  var p=new List<byte>(new byte[]{20,15,4,4});
  for(int block=0;block<300;block++){
   int op=solid?0:(block%13)*4;p.Add((byte)op);
   if(op==0){p.Add(21);continue;}if(op==44)continue;
   if(op==28){p.AddRange(new byte[]{11,22,33,44,55,66,77});for(int y=0;y<8;y++){int codes=0;for(int x=0;x<8;x++)codes|=((x+y)%8)<<(3*x);p.Add((byte)codes);p.Add((byte)(codes>>8));p.Add((byte)(codes>>16));p.Add((byte)(100+y));}continue;}
   p.AddRange(new byte[]{11,22});
   if(op==36){for(int y=0;y<8;y++)p.AddRange(new byte[]{0xe4,0x1b,101,102});continue;}
   if(op==40){p.AddRange(new byte[]{0x5a,0xa5});for(int bit=15;bit>=0;bit--)if((0xa55a&(1<<bit))!=0)p.AddRange(new byte[]{0xe4,101});continue;}
   if(op==8){p.AddRange(new byte[]{33,0xa5});for(int y=0;y<8;y++)if((0xa5&(128>>y))!=0)p.AddRange(new byte[]{0xe4,0x1b,101,102});continue;}
   p.Add(33);
   if(op==4){for(int y=0;y<8;y++)p.AddRange(new byte[]{0xe4,0x1b,101,102});continue;}
   if(op==32){p.Add(0xa5);for(int y=0;y<8;y++)if((0xa5&(128>>y))!=0)p.AddRange(new byte[]{0xe4,101,0x1b,102});else p.Add(0xa5);continue;}
   p.AddRange(new byte[]{0x5a,0xa5});
   for(int bit=15;bit>=0;bit--){bool encoded=(0xa55a&(1<<bit))!=0;if(encoded)p.Add(0xe4);if(encoded||(op==24&&(bit%8!=7||encoded)))p.Add(101);else if(op==16)p.AddRange(new byte[]{70,71,72,73});}
  }
  var result=new List<byte>();result.AddRange(BitConverter.GetBytes(p.Count));result.AddRange(p);return result.ToArray();
 }
 void Run(string exe,string pat,string destination){
  source=File.ReadAllBytes(exe);byte[] movie=File.ReadAllBytes(pat);
  if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569"||Hash(movie)!="F1D9162231109BC56D5E483C47FFE1B6EA53589BB9178594D905542FEB1EAC1F")throw new InvalidDataException("Expected matching Bs.exe, original BATFIRE1.PAT and x86 oracle");
  pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);
  memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try{
   int a=Copy(0x433920,0x10),b=Copy(0x433aa5,0xd8),c=Copy(0x433d41,0xf4f),d=Copy(0x437ebd,0xde);
   if(a!=0||b!=22||c!=34||d!=4)throw new InvalidDataException("Relocation counts: "+a+","+b+","+c+","+d);
   Table(0x451875,13,0x433d41,0x437f03);Table(0x4540e1,256,0x434078,0x434c8e);
   // Capture ESI before the original popad restores it. No decoding instruction changes.
   byte[] expected=Read(0x433b1d,7);if(BitConverter.ToString(expected)!="61-A1-5D-18-45-00-C3")throw new InvalidDataException("Decoder exit differs");
   var jump=new List<byte>();jump.Add(0xe9);jump.AddRange(BitConverter.GetBytes(unchecked(0x433900-0x433b22)));Write(0x33b1d,jump.ToArray());
   var exit=new List<byte>(new byte[]{0x89,0x35});exit.AddRange(BitConverter.GetBytes(Address(Consumed)));exit.AddRange(new byte[]{0x61,0xc3});Write(0x33900,exit.ToArray());
   foreach(int page in new[]{0x33000,0x34000,0x37000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");}
   var decode=(Decode)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x33920),typeof(Decode));
   Directory.CreateDirectory(destination);var rows=new List<string>{"Frame,Offset,PayloadBytes,ConsumedBytes,SHA256"};int offset=0,index=0;
   while(offset<=movie.Length-4){uint n=BitConverter.ToUInt32(movie,offset);if(n==uint.MaxValue)break;if(n<4||n>65532||n>movie.Length-offset-4)throw new InvalidDataException("Trusted PAT framing differs");byte[] frame=new byte[n+4];Array.Copy(movie,offset,frame,0,frame.Length);byte[] result=Frame(decode,frame);
    rows.Add(index+","+offset+","+n+","+frame.Length+","+Hash(result));if(index==0||index==20||index==45)File.WriteAllBytes(Path.Combine(destination,"BATFIRE1-"+index.ToString("D2")+".idx"),result);
    if(index==0){byte[] dirty=new byte[Pixels];for(int i=0;i<dirty.Length;i++)dirty[i]=0xa7;Write(Output,dirty);if(Hash(Frame(decode,frame))!=Hash(result))throw new InvalidDataException("First frame depends on prior pixels");}
    offset+=frame.Length;index++;
   }
   if(index!=46||offset!=movie.Length-4)throw new InvalidDataException("Unexpected movie completion");File.WriteAllLines(Path.Combine(destination,"BATFIRE1-Frames-Oracle.csv"),rows.ToArray());
   byte[] fixture=PatternFixture();byte[] pixels=Frame(decode,fixture);File.WriteAllBytes(Path.Combine(destination,"PAT-Patterns-Input.bin"),fixture);var patterns=new List<string>{"Code,P0,P1,P2,P3"};for(int pattern=0;pattern<256;pattern++){int at=(pattern/20*8)*160+pattern%20*8;patterns.Add(pattern+","+pixels[at]+","+pixels[at+1]+","+pixels[at+2]+","+pixels[at+3]);}File.WriteAllLines(Path.Combine(destination,"PAT-Patterns-Oracle.csv"),patterns.ToArray());
   byte[] first=AllOpsFixture(true),second=AllOpsFixture(false);Frame(decode,first);byte[] allOps=Frame(decode,second);
   var full=new List<byte>();full.AddRange(first);full.AddRange(second);full.AddRange(BitConverter.GetBytes(uint.MaxValue));File.WriteAllBytes(Path.Combine(destination,"PAT-AllOps-Input.pat"),full.ToArray());File.WriteAllBytes(Path.Combine(destination,"PAT-AllOps-Oracle.idx"),allOps);
   Console.WriteLine("Original decoder: 46 frames, exact consumed lengths, output canaries, independent first-frame state, and 256 quartet patterns captured.");
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){if(args.Length!=3){Console.Error.WriteLine("ClassicPatOracle <matching Bs.exe> <original LWEBS/BATFIRE1.PAT> <new directory>");return 2;}try{new ClassicPatOracle().Run(args[0],args[1],args[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
