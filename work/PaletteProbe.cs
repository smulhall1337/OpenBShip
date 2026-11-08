using System;
using System.IO;
using System.Runtime.InteropServices;

// Executes the PE32 DirectDraw palette API without opening a window.
class PaletteProbe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibrary(string path);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module,string name);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int CreateDD(IntPtr guid,out IntPtr dd,IntPtr outer);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int CreatePalette(IntPtr self,uint flags,IntPtr entries,out IntPtr palette,IntPtr outer);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int CreateSurface(IntPtr self,IntPtr desc,out IntPtr surface,IntPtr outer);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int Entries(IntPtr self,uint flags,uint start,uint count,IntPtr entries);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int BindPalette(IntPtr self,IntPtr palette);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate uint Release(IntPtr self);
 static Delegate Method(IntPtr obj,int slot,Type type){return Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(obj),slot*4),type);}
 static void Check(string stage,int result){Console.WriteLine(stage+": 0x"+result.ToString("X8"));if(result<0)throw new Exception(stage+" failed");}
 static int Main(string[] args){
  if(args.Length!=1)return 2;
  IntPtr dd=IntPtr.Zero,palette=IntPtr.Zero,surface=IntPtr.Zero,colors=Marshal.AllocHGlobal(1024),readback=Marshal.AllocHGlobal(1024),desc=Marshal.AllocHGlobal(108);
  try {
   string path=Path.GetFullPath(args[0]);Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
   IntPtr module=LoadLibrary(path);if(module==IntPtr.Zero)throw new Exception("LoadLibrary error "+Marshal.GetLastWin32Error());
   IntPtr entry=GetProcAddress(module,"DirectDrawCreate");if(entry==IntPtr.Zero)throw new Exception("DirectDrawCreate export missing");
   Check("DirectDrawCreate",((CreateDD)Marshal.GetDelegateForFunctionPointer(entry,typeof(CreateDD)))(IntPtr.Zero,out dd,IntPtr.Zero));
   for(int i=0;i<256;i++){Marshal.WriteByte(colors,i*4,(byte)i);Marshal.WriteByte(colors,i*4+1,(byte)(255-i));Marshal.WriteByte(colors,i*4+2,(byte)(i/2));Marshal.WriteByte(colors,i*4+3,0);}
   Check("CreatePalette (8-bit)",((CreatePalette)Method(dd,5,typeof(CreatePalette)))(dd,4,colors,out palette,IntPtr.Zero));
   for(int i=0;i<108;i++)Marshal.WriteByte(desc,i,0);
   Marshal.WriteInt32(desc,108);Marshal.WriteInt32(desc,4,0x1007);Marshal.WriteInt32(desc,8,200);Marshal.WriteInt32(desc,12,320);
   Marshal.WriteInt32(desc,72,32);Marshal.WriteInt32(desc,76,0x60);Marshal.WriteInt32(desc,84,8);Marshal.WriteInt32(desc,104,0x40);
   Check("CreateSurface (8-bit offscreen)",((CreateSurface)Method(dd,6,typeof(CreateSurface)))(dd,desc,out surface,IntPtr.Zero));
   Check("Surface.SetPalette",((BindPalette)Method(surface,31,typeof(BindPalette)))(surface,palette));
   Check("Palette.SetEntries (10..245)",((Entries)Method(palette,6,typeof(Entries)))(palette,0,10,236,IntPtr.Add(colors,40)));
   Check("Palette.GetEntries",((Entries)Method(palette,4,typeof(Entries)))(palette,0,10,236,readback));
   for(int i=0;i<236;i++)for(int component=0;component<3;component++)if(Marshal.ReadByte(colors,(i+10)*4+component)!=Marshal.ReadByte(readback,i*4+component))throw new Exception("Palette color round-trip mismatch");
   Console.WriteLine("Verified 236 RGB palette entries. Presentation and intro skipping remain separate desktop checks.");return 0;
  }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}
  finally{if(surface!=IntPtr.Zero)((Release)Method(surface,2,typeof(Release)))(surface);if(palette!=IntPtr.Zero)((Release)Method(palette,2,typeof(Release)))(palette);if(dd!=IntPtr.Zero)((Release)Method(dd,2,typeof(Release)))(dd);Marshal.FreeHGlobal(colors);Marshal.FreeHGlobal(readback);Marshal.FreeHGlobal(desc);}
 }
}
