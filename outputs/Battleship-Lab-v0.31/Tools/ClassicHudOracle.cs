using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Copies only Classic HUD command dispatch. Sprite draw is a recording stub.
class ClassicHudOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr size,uint flags,uint protection);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr size,uint protection,out uint previous);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr size,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Render();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Draw(int x,int y,int frame,IntPtr asset,int mode,int percent,int flags);
 byte[] source;int pe;uint imageBase;IntPtr memory;
 static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
 int Offset(uint va){uint rva=va-imageBase;int table=pe+24+BitConverter.ToUInt16(source,pe+20);for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++){int at=table+40*i;uint first=BitConverter.ToUInt32(source,at+12),size=BitConverter.ToUInt32(source,at+16);if(rva>=first&&rva-first<size)return checked((int)(BitConverter.ToUInt32(source,at+20)+rva-first));}throw new InvalidDataException("VA outside raw PE");}
 uint Address(int offset){return unchecked((uint)memory.ToInt32()+(uint)offset);}
 void Write(int offset,byte[] b){Marshal.Copy(b,0,IntPtr.Add(memory,offset),b.Length);}
 void Run(string exe,string destination){
  source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new InvalidDataException("Matching Bs.exe and x86 process required");
  pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try{
   byte[] code=new byte[0x1e3];Array.Copy(source,Offset(0x40938c),code,0,code.Length);int relocated=0;var globals=new HashSet<uint>(new uint[]{0x4b42a0,0x563134,0x563130,0x4b4794,0x4b47b0,0x4483c0,0x4483b0,0x4b499c,0x4483e0,0x4483d0,0x4e381a,0x4e3818});
   for(int at=0;at<=code.Length-4;at++){uint value=BitConverter.ToUInt32(code,at);if(!globals.Contains(value))continue;BitConverter.GetBytes(Address((int)(value-0x400000))).CopyTo(code,at);at+=3;relocated++;}if(relocated!=26)throw new InvalidDataException("HUD relocation count differs");Write(0x938c,code);Write(0x956f,new byte[]{0xc3});
   byte[] positions=new byte[0x40];Array.Copy(source,Offset(0x4483b0),positions,0,positions.Length);Write(0x483b0,positions);
   var wrapper=new List<byte>(new byte[]{0x56,0x57,0xe8});wrapper.AddRange(BitConverter.GetBytes(0x938c-0x1007));wrapper.AddRange(new byte[]{0x5f,0x5e,0xc3});Write(0x1000,wrapper.ToArray());
   var rows=new List<string>{"Case,Phase,MovieActive,OwnResult,EnemyResult,AliveMask,Order,Group,Frame,X,Y,Percent,Mode,Flags"};int id=0,phase=0,active=0,own=0,enemy=0,mask=0,order=0;
   Draw draw=delegate(int x,int y,int frame,IntPtr asset,int mode,int percent,int flags){string group=asset.ToInt32()==1?"Panels":asset.ToInt32()==2?"Status":"Unknown";rows.Add(id+","+phase+","+active+","+own+","+enemy+","+mask+","+order+","+group+","+frame+","+x+","+y+","+percent+","+mode+","+flags);order++;};
   var stub=new List<byte>(new byte[]{0xb8});stub.AddRange(BitConverter.GetBytes(unchecked((uint)Marshal.GetFunctionPointerForDelegate(draw).ToInt32())));stub.AddRange(new byte[]{0xff,0xe0});Write(0x7800,stub.ToArray());
   foreach(int page in new[]{0x1000,0x7000,0x9000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");}
   Marshal.WriteInt32(memory,0x163134,1);Marshal.WriteInt32(memory,0x163130,2);var render=(Render)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1000),typeof(Render));
   for(phase=0;phase<=8;phase++)for(active=0;active<2;active++)foreach(int ownValue in new[]{-1,0,1})foreach(int enemyValue in new[]{-1,0,1})foreach(int alive in new[]{0,21,31}){
    own=ownValue;enemy=enemyValue;mask=alive;order=0;Marshal.WriteInt16(memory,0xb4794,(short)phase);Marshal.WriteByte(memory,0xb42a0,(byte)active);Marshal.WriteInt16(memory,0xe3818,(short)own);Marshal.WriteInt16(memory,0xe381a,(short)enemy);for(int i=0;i<5;i++){Marshal.WriteInt32(memory,0xb47b0+i*4,(mask>>i)&1);Marshal.WriteInt32(memory,0xb499c+i*4,(mask>>i)&1);}render();id++;
   }
   Directory.CreateDirectory(destination);File.WriteAllLines(Path.Combine(destination,"Classic-Hud-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured "+id+" Classic HUD cases / "+(rows.Count-1)+" sprite draw commands; raster/window calls stubbed.");GC.KeepAlive(draw);
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){try{if(args.Length!=2)return 2;new ClassicHudOracle().Run(args[0],args[1]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}

