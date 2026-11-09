using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// Hash-guarded original numerical patch/fade code in this new x86 process only.
// The original graphics call is replaced with a local return; no original game
// process, file, registry setting, display palette or other process is modified.
class ClassicPaletteOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protection);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protection,out uint old);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void ApplyPatches();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Fade(IntPtr input);
 byte[] image;int pe;uint imageBase;
 int Offset(uint va) {
  uint rva=va-imageBase;int table=pe+24+BitConverter.ToUInt16(image,pe+20);
  for(int i=0;i<BitConverter.ToUInt16(image,pe+6);i++){int at=table+i*40;uint start=BitConverter.ToUInt32(image,at+12),size=BitConverter.ToUInt32(image,at+16);if(rva>=start&&rva-start<size)return checked((int)(BitConverter.ToUInt32(image,at+20)+rva-start));}
  throw new InvalidDataException("VA outside raw sections");
 }
 byte[] Read(uint va,int count){byte[] b=new byte[count];Array.Copy(image,Offset(va),b,0,count);return b;}
 static uint Address(IntPtr p,int offset){return unchecked((uint)p.ToInt32()+(uint)offset);}
 static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}
 static void Relocate(byte[] bytes,int at,uint expected,uint value){if(BitConverter.ToUInt32(bytes,at)!=expected)throw new InvalidDataException("Source relocation mismatch");BitConverter.GetBytes(value).CopyTo(bytes,at);}
 void Run(string source,string palette,string destination) {
  image=File.ReadAllBytes(source);
  if(IntPtr.Size!=4||Hash(image)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new InvalidDataException("Expected matching original image and x86 oracle");
  pe=BitConverter.ToInt32(image,60);imageBase=BitConverter.ToUInt32(image,pe+52);
  byte[] island=File.ReadAllBytes(palette);if(island.Length!=768)throw new InvalidDataException("Expected 768-byte palette");
  IntPtr code=VirtualAlloc(IntPtr.Zero,new UIntPtr(4096),0x3000,4),data=VirtualAlloc(IntPtr.Zero,new UIntPtr(4096),0x3000,4);
  if(code==IntPtr.Zero||data==IntPtr.Zero)throw new Exception("Allocation failed");
  try {
   byte[] stores=Read(0x42A7D6,0x6B),patch=new byte[0x6F];
   new byte[]{0x83,0xEC,0x64,0x56}.CopyTo(patch,0); // Recreate the source stack frame/ESI save.
   foreach(uint va in new uint[]{0x42A7D6,0x42A7DD,0x42A7E4,0x42A7EB,0x42A7F2,0x42A7F9,0x42A800,0x42A807,0x42A80E,0x42A815,0x42A822,0x42A827,0x42A82F,0x42A834,0x42A83B}) {
    int at=(int)(va-0x42A7D6);int operand=stores[at]==0xC6&&stores[at+1]==5?at+2:stores[at]==0xA2?at+1:-1;
    if(operand<0)throw new InvalidDataException("Expected original palette store");uint target=BitConverter.ToUInt32(stores,operand);
    if(target<0x4FB3E0||target>=0x4FB6E0)throw new InvalidDataException("Store outside palette span");
    Relocate(stores,operand,target,Address(data,(int)(target-0x4FB3E0)));
   }
   stores.CopyTo(patch,4);
   byte[] fade=Read(0x422C70,0x5E);
   Relocate(fade,4,0x5AE680,Address(data,1024));
   Relocate(fade,0x1A,0x5AE67E,Address(data,2048));
   Relocate(fade,0x31,0x5AE680,Address(data,1024));
   Relocate(fade,0x36,unchecked(0x401D40u-0x422CAAu),unchecked(Address(code,1024)-Address(code,512+0x3A)));
   Relocate(fade,0x40,0x5AE67E,Address(data,2048));
   Relocate(fade,0x56,0x5AE67E,Address(data,2048));
   Marshal.Copy(patch,0,code,patch.Length);Marshal.Copy(fade,0,IntPtr.Add(code,512),fade.Length);Marshal.WriteByte(code,1024,0xC3);
   Marshal.Copy(island,0,data,768);uint old;if(!VirtualProtect(code,new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");
   var apply=(ApplyPatches)Marshal.GetDelegateForFunctionPointer(code,typeof(ApplyPatches));apply();
   var patched=new byte[768];Marshal.Copy(data,patched,0,768);Directory.CreateDirectory(destination);File.WriteAllBytes(Path.Combine(destination,"Patched-Island.rgb"),patched);
   var scale=(Fade)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(code,512),typeof(Fade));var rows=new List<string>{"Factor,Completed,NextFactor,SHA256"};
   for(int factor=0;factor<=64;factor++) {
    Marshal.WriteInt16(data,2048,(short)factor);int completed=scale(data);var output=new byte[768];Marshal.Copy(IntPtr.Add(data,1024),output,0,768);
    File.WriteAllBytes(Path.Combine(destination,"Fade-"+factor.ToString("D2")+".rgb"),output);
    rows.Add(factor+","+completed+","+Marshal.ReadInt16(data,2048)+","+Hash(output));
   }
   File.WriteAllLines(Path.Combine(destination,"Palette-Oracle.csv"),rows.ToArray());
   Console.WriteLine("Original palette patches and 65 fade-factor outputs captured.");
  }finally{VirtualFree(code,UIntPtr.Zero,0x8000);VirtualFree(data,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){if(args.Length!=3){Console.Error.WriteLine("ClassicPaletteOracle <matching Bs.exe> <ISLAND.COL> <output directory>");return 2;}try{new ClassicPaletteOracle().Run(args[0],args[1],args[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
