using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Copies only original fixed-size progressive 2x/4x indexed-pixel expansion.
// Generated input patterns; no original graphics/audio/window/file callbacks.
class ClassicScaleOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr size,uint flags,uint protection);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr size,uint protection,out uint previous);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr size,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Scale(IntPtr input,IntPtr output);
 byte[] source;int pe;uint imageBase;IntPtr memory;const int Input=0x100000,Output=0x140000,OutputBytes=640*480;
 static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
 int Offset(uint va){uint rva=va-imageBase;int table=pe+24+BitConverter.ToUInt16(source,pe+20);for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++){int at=table+40*i;uint start=BitConverter.ToUInt32(source,at+12),size=BitConverter.ToUInt32(source,at+16);if(rva>=start&&rva-start<size)return checked((int)(BitConverter.ToUInt32(source,at+20)+rva-start));}throw new InvalidDataException("VA outside PE");}
 void Write(int offset,byte[] bytes){Marshal.Copy(bytes,0,IntPtr.Add(memory,offset),bytes.Length);}
 void Wrapper(int offset,int routine){var bytes=new List<byte>(new byte[]{0x56,0x57,0x8b,0x74,0x24,0x0c,0x8b,0x7c,0x24,0x10,0xe8});bytes.AddRange(BitConverter.GetBytes(routine-(offset+15)));bytes.AddRange(new byte[]{0x5f,0x5e,0xc3});Write(offset,bytes.ToArray());}
 void Run(string exe,string destination){
  source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new InvalidDataException("Matching Bs.exe and x86 process required");
  pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try{
   byte[] code=new byte[0x118];Array.Copy(source,Offset(0x437f9b),code,0,code.Length);if(code[0]!=0x60||code[0x8b]!=0x60||code[0x117]!=0xc3)throw new InvalidDataException("Scaler boundaries differ");Write(0x37f9b,code);Wrapper(0x1000,0x37f9b);Wrapper(0x1020,0x38026);
   foreach(int page in new[]{0x1000,0x37000,0x38000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");}
   Directory.CreateDirectory(destination);var rows=new List<string>{"Scale,InputWidth,InputHeight,OutputWidth,OutputHeight,RoutineVA,InputSHA256,OutputSHA256"};
   foreach(int factor in new[]{2,4}){
    int width=640/factor,height=480/factor;byte[] input=new byte[width*height];for(int y=0;y<height;y++)for(int x=0;x<width;x++)input[y*width+x]=(byte)((x*17+y*29+(x/8)*(y/8)*3)&255);
    Write(Input,input);byte[] empty=new byte[OutputBytes];for(int i=0;i<empty.Length;i++)empty[i]=0xa7;Write(Output,empty);Marshal.WriteInt32(memory,Output-4,unchecked((int)0x5a3cc35a));Marshal.WriteInt32(memory,Output+OutputBytes,unchecked((int)0x5a3cc35a));
    var scale=(Scale)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,factor==2?0x1000:0x1020),typeof(Scale));scale(IntPtr.Add(memory,Input),IntPtr.Add(memory,Output));
    if(Marshal.ReadInt32(memory,Output-4)!=unchecked((int)0x5a3cc35a)||Marshal.ReadInt32(memory,Output+OutputBytes)!=unchecked((int)0x5a3cc35a))throw new InvalidDataException("Scaler crossed 640x480 surface");
    byte[] output=new byte[OutputBytes];Marshal.Copy(IntPtr.Add(memory,Output),output,0,output.Length);for(int y=0;y<480;y++)for(int x=0;x<640;x++)if(output[y*640+x]!=input[(y/factor)*width+x/factor])throw new InvalidDataException("Native progressive replication differs");
    File.WriteAllBytes(Path.Combine(destination,"Scale"+factor+"-Input.idx"),input);File.WriteAllBytes(Path.Combine(destination,"Scale"+factor+"-Output.idx"),output);rows.Add(factor+","+width+","+height+",640,480,"+(factor==2?"00437F9B":"00438026")+","+Hash(input)+","+Hash(output));
   }
   File.WriteAllLines(Path.Combine(destination,"Classic-Scale-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured two original progressive scaler patterns: each writes exactly 640x480 indexed pixels; boundary canaries intact.");
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){try{if(args.Length!=2)return 2;new ClassicScaleOracle().Run(args[0],args[1]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
