using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Hash-locked request admission only. File/media/context work uses recording stubs.
class CreditsCatalogRequestOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr size,uint flags,uint protection);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr size,uint protection,out uint previous);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr size,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Request(int movie,IntPtr owner);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Close();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Palette(IntPtr path);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Format(IntPtr output,IntPtr format,IntPtr directory,IntPtr name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Context(int movie,IntPtr owner);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Sequence(int movie);
 byte[] source;int pe;uint imageBase;IntPtr memory;
 static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
 int Offset(uint va){uint rva=va-imageBase;int table=pe+24+BitConverter.ToUInt16(source,pe+20);for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++){int at=table+40*i;uint first=BitConverter.ToUInt32(source,at+12),size=BitConverter.ToUInt32(source,at+16);if(rva>=first&&rva-first<size)return checked((int)(BitConverter.ToUInt32(source,at+20)+rva-first));}throw new InvalidDataException("VA outside raw PE");}
 uint Address(int offset){return unchecked((uint)memory.ToInt32()+(uint)offset);}
 void Write(int offset,byte[] bytes){Marshal.Copy(bytes,0,IntPtr.Add(memory,offset),bytes.Length);}
 void Data(uint va,int bytes){byte[] b=new byte[bytes];Array.Copy(source,Offset(va),b,0,bytes);Write((int)(va-0x400000),b);}
 void Stub(int offset,Delegate callback){var b=new List<byte>(new byte[]{0xb8});b.AddRange(BitConverter.GetBytes(unchecked((uint)Marshal.GetFunctionPointerForDelegate(callback).ToInt32())));b.AddRange(new byte[]{0xff,0xe0});Write(offset,b.ToArray());}
 void Run(string exe,string destination){
  source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new InvalidDataException("Matching Bs.exe and x86 oracle required");
  pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try{
   byte[] code=new byte[0xf2];Array.Copy(source,Offset(0x402460),code,0,code.Length);int relocated=0;var globals=new HashSet<uint>(new uint[]{0x4fb904,0x4e3770,0x55e810,0x4b3bf0,0x55ea4c,0x4b42a0,0x4fbdd4,0x44e288,0x4e41f0,0x4474b4,0x55e7f8,0x55e7f4});
   for(int at=0;at<=code.Length-4;at++){uint value=BitConverter.ToUInt32(code,at);if(!globals.Contains(value))continue;BitConverter.GetBytes(Address((int)(value-0x400000))).CopyTo(code,at);at+=3;relocated++;}if(relocated!=19)throw new InvalidDataException("Admission relocation count differs");Write(0x2460,code);Data(0x44e288,138*9);
   int closed=0,palettes=0,contexts=0,sequences=0,loadFailure=0;string directory="",name="";
   Close close=delegate{closed++;Marshal.WriteByte(memory,0xb42a0,0);};Palette palette=delegate(IntPtr path){palettes++;return loadFailure;};Context context=delegate(int id,IntPtr owner){contexts++;};Sequence sequence=delegate(int id){sequences++;return 1;};
   Format format=delegate(IntPtr output,IntPtr pattern,IntPtr dir,IntPtr clip){directory=Marshal.PtrToStringAnsi(dir);name=Marshal.PtrToStringAnsi(clip);Marshal.WriteByte(output,0,(byte)'X');return 1;};
   Stub(0x2980,close);Stub(0x26e0,palette);Stub(0x201e0,context);Stub(0x2560,sequence);Stub(0x390d0,format);
   foreach(int page in new[]{0x2000,0x20000,0x39000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");}
   Request request=(Request)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x2460),typeof(Request));var rows=new List<string>{"Kind,CurrentPriority,RequestedPriority,DecodedFrames,Active,Enabled,HasPath,Movie,LoadFailure,Mode,Return,CloseCalls,PaletteCalls,ContextCalls,SequenceCalls,FinalPriority,FinalActive,FinalMovie,FinalContext,Directory,Name"};
   Action<string,int,int,int,int,int,int,int,int,int> run=delegate(string kind,int current,int priority,int frames,int active,int enabled,int path,int id,int failure,int mode){
    Marshal.WriteInt32(memory,0xb3bf0,current);Marshal.WriteInt16(memory,0x15e810,(short)priority);Marshal.WriteInt32(memory,0x15ea4c,frames);Marshal.WriteByte(memory,0xb42a0,(byte)active);Marshal.WriteInt32(memory,0xfb904,enabled);Write(0xe3770,new byte[]{(byte)(path==0?0:'L'),0});Write(0xe41f0,new byte[]{(byte)'H',0});Marshal.WriteInt32(memory,0xfbdd4,mode);Marshal.WriteInt32(memory,0x15e7f8,77);Marshal.WriteInt32(memory,0x15e7f4,88);loadFailure=failure;closed=palettes=contexts=sequences=0;directory=name="";
    int result=request(id,IntPtr.Zero);rows.Add(kind+","+current+","+priority+","+frames+","+active+","+enabled+","+path+","+id+","+failure+","+mode+","+result+","+closed+","+palettes+","+contexts+","+sequences+","+Marshal.ReadInt16(memory,0xb3bf0)+","+(Marshal.ReadByte(memory,0xb42a0)==0?0:1)+","+Marshal.ReadInt32(memory,0x15e7f8)+","+Marshal.ReadInt32(memory,0x15e7f4)+","+directory+","+name);
   };
   for(int id=1;id<=137;id++)foreach(int mode in new[]{1,2})run("Catalog",0,4,0,0,1,1,id,0,mode);
   Directory.CreateDirectory(destination);File.WriteAllLines(Path.Combine(destination,"Credits-Catalog-Requests-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured "+(rows.Count-1)+" admission cases; media/file/context calls stubbed.");GC.KeepAlive(close);GC.KeepAlive(palette);GC.KeepAlive(context);GC.KeepAlive(sequence);GC.KeepAlive(format);
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){try{if(args.Length!=2)return 2;new CreditsCatalogRequestOracle().Run(args[0],args[1]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}

