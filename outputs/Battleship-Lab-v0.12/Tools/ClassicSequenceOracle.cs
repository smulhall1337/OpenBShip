using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Executes ONLY hash-locked sequence polling/cleanup fragments in this x86 process.
// Sound requests/stops use managed recording stubs and successful dummy handles.
// No game process, original file, registry, graphics, sound or media is modified.
class ClassicSequenceOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protect);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protect,out uint old);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Tick(int frame);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Close();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int StartSound(IntPtr name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void StopSound(int handle);
 byte[] source;int pe;uint imageBase;IntPtr memory;
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
 void Run(string exe,string assets,string destination){
  source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new InvalidDataException("Expected original executable identity and x86 oracle");
  pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try{
   byte[] code=Read(0x402980,0x14d);int relocations=0;
   var globals=new HashSet<uint>(new uint[]{0x4b42a0,0x4fc270,0x4fbc30,0x4e3764,0x55e7f4});
   for(int at=0;at<=code.Length-4;at++){uint value=BitConverter.ToUInt32(code,at);if(!globals.Contains(value))continue;BitConverter.GetBytes(Address((int)(value-0x400000))).CopyTo(code,at);at+=3;relocations++;}
   if(relocations!=15)throw new InvalidDataException("Sequence relocation count: "+relocations);Write(0x2980,code);
   var rows=new List<string>{"Clip,Kind,Cue,Frame,Handle"};string current="";int frame=0;
   StartSound start=delegate(IntPtr name){int cue=unchecked((int)((uint)name.ToInt32()-Address(Buffer+0x182)))/12;if(cue<0||cue>=63)throw new InvalidDataException("Native cue pointer outside constructed data");rows.Add(current+",Start,"+cue+","+frame+","+(cue+1));return cue+1;};
   StopSound stop=delegate(int handle){rows.Add(current+",Stop,"+(handle-1)+","+frame+","+handle);};
   Action<int,Delegate> stub=delegate(int offset,Delegate callback){var bytes=new List<byte>(new byte[]{0xb8});bytes.AddRange(BitConverter.GetBytes(unchecked((uint)Marshal.GetFunctionPointerForDelegate(callback).ToInt32())));bytes.AddRange(new byte[]{0xff,0xe0});Write(offset,bytes.ToArray());};
   stub(0x22320,start);stub(0x1a720,stop);Write(0x395c0,new byte[]{0x31,0xc0,0xc3});Write(0x222a0,new byte[]{0x31,0xc0,0xc3});
   foreach(int page in new[]{0x2000,0x22000,0x1a000,0x39000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");}
   var tick=(Tick)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x29f0),typeof(Tick));var close=(Close)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x2980),typeof(Close));
   int total=0;
   foreach(string line in File.ReadAllLines(Path.Combine(assets,"Evidence","Firing-Clip-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] fields=line.Replace("\"","").Split(',');current=fields[0];int count=int.Parse(fields[2]);
    Write(Buffer,new byte[4096]);for(int i=0;i<64;i++){Marshal.WriteInt16(memory,Buffer+2+2*i,-1);Marshal.WriteInt16(memory,Buffer+0x102+2*i,-1);}
    int cues=0;foreach(string record in File.ReadAllLines(Path.Combine(assets,"Assets","SFX",current,current+".SEQ"))){if(record.StartsWith("*"))break;string[] p=record.Split(',');if(p.Length!=3||p[0].Length>8||cues>=63)throw new InvalidDataException("Invalid constructed SEQ input");short onset=short.Parse(p[1]),end=short.Parse(p[2]);if(onset<0||end<0)throw new InvalidDataException("Negative cue input");Marshal.WriteInt16(memory,Buffer+2+cues*2,onset);Marshal.WriteInt16(memory,Buffer+0x82+cues*2,end);Write(Buffer+0x182+cues*12,System.Text.Encoding.ASCII.GetBytes(p[0]+"\0"));cues++;}
    Marshal.WriteInt32(memory,0x15e7f4,unchecked((int)Address(Buffer)));Marshal.WriteByte(memory,0xb42a0,1);
    for(frame=0;frame<count;frame++)tick(frame);close();total+=cues;
   }
   Directory.CreateDirectory(destination);File.WriteAllLines(Path.Combine(destination,"Classic-Sequence-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured original sequence timing/cleanup for "+total+" cues across 20 clips, with successful dummy sound handles.");GC.KeepAlive(start);GC.KeepAlive(stop);
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){if(args.Length!=3){Console.Error.WriteLine("ClassicSequenceOracle <matching Bs.exe> <package root> <destination>");return 2;}try{new ClassicSequenceOracle().Run(args[0],args[1],args[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
