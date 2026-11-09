using System;using System.IO;using System.Runtime.InteropServices;using System.Collections.Generic;using System.Security.Cryptography;
// Executes only the identity-locked palette branch and ISLAND color patches.
// No game process, file/device callback, window or audio API executes.
class ResultPaletteOracle {
 [DllImport("kernel32.dll")]static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protection);
 [DllImport("kernel32.dll")]static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protection,out uint previous);
 [DllImport("kernel32.dll")]static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Call();
 byte[] source;int pe;IntPtr memory;uint basis;
 static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
 int Offset(uint va){int table=pe+24+BitConverter.ToUInt16(source,pe+20);uint rva=va-0x400000;for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++){int at=table+i*40;uint first=BitConverter.ToUInt32(source,at+12),size=BitConverter.ToUInt32(source,at+16);if(rva>=first&&rva-first<size)return (int)(BitConverter.ToUInt32(source,at+20)+rva-first);}throw new Exception("VA outside image");}
 void Write(int offset,byte[] bytes){Marshal.Copy(bytes,0,IntPtr.Add(memory,offset),bytes.Length);}int Pointer(int offset){return unchecked((int)(basis+(uint)offset));}
 void Run(string exe,string assets,string destination){source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new Exception("Matching image/x86 required");pe=BitConverter.ToInt32(source,60);byte[] island=File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL"));if(Hash(island)!="DEBB5F1835FFB4D23169086BFE7ED505165ADD48918E28876FA4FD4F9E13D6A7")throw new Exception("ISLAND identity");memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation");basis=unchecked((uint)memory.ToInt32());try{
  byte[] code=new byte[0x68];Array.Copy(source,Offset(0x402732),code,0,code.Length);var globals=new HashSet<uint>{0x4fbdd4,0x457074,0x4aa7f4,0x4fb3fe,0x4fb41c};int relocated=0;for(int i=0;i<=code.Length-4;i++){uint v=BitConverter.ToUInt32(code,i);if(!globals.Contains(v))continue;BitConverter.GetBytes(Pointer((int)(v-0x400000))).CopyTo(code,i);i+=3;relocated++;}if(relocated!=8)throw new Exception("Palette relocations="+relocated);Write(0x2732,code);Write(0x279a,new byte[]{0xc3});var wrapper=new List<byte>{0x60,0xe8};wrapper.AddRange(BitConverter.GetBytes(0x2732-0x1006));wrapper.AddRange(new byte[]{0x61,0xc3});Write(0x1000,wrapper.ToArray());
  // The exact patch constants are independently executed in ClassicPaletteOracle.
  int[] offsets={51,52,53,54,55,56,57,58,59,39,30,31,32,40,41};byte[] values={166,178,198,206,215,227,138,150,174,13,255,255,255,114,255};for(int i=0;i<offsets.Length;i++)island[offsets[i]]=values[i];
  Marshal.WriteInt32(memory,0xaa7f4,Pointer(0x180000));foreach(int page in new[]{0x1000,0x2000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection");}var call=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1000),typeof(Call));
  var trusted=new Dictionary<string,string>{{"VICTORY","E963E7B457068B3B40ED9CD306AB292FB6D942C3DB8B73871ECE1B215D6B3335"},{"LOSE","37D635813B45BF6C9054E603B70FC8D4940C128B7E3DD001E087820DD9AEBC35"}};var rows=new List<string>{"Clip,EngineMode,ScriptState,SHA256"};Directory.CreateDirectory(destination);
  foreach(var item in trusted){byte[] palette=File.ReadAllBytes(Path.Combine(assets,"HWEBS",item.Key+".PAL"));if(Hash(palette)!=item.Value)throw new Exception("Palette identity "+item.Key);foreach(int state in new[]{0,6,41}){Write(0xfb3e0,island);Write(0x180000,palette);Marshal.WriteInt32(memory,0xfbdd4,2);Marshal.WriteInt32(memory,0x57074,state);call();byte[] result=new byte[768];Marshal.Copy(IntPtr.Add(memory,0xfb3e0),result,0,768);string name="Result-Palette-"+item.Key+"-"+state+".bin";File.WriteAllBytes(Path.Combine(destination,name),result);rows.Add(item.Key+",2,"+state+","+Hash(result));}}
  File.WriteAllLines(Path.Combine(destination,"Result-Palette-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured6native palette outputs: actual front-end mode2, states0/6/full and41/Credits subset.");
 }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}}
 static int Main(string[] args){try{if(args.Length!=3)return 2;new ResultPaletteOracle().Run(args[0],args[1],args[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}

