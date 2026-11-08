using System;
using System.IO;
using System.Runtime.InteropServices;
class SystemPaletteProbe {
 [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr window);
 [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr window,IntPtr dc);
 [DllImport("gdi32.dll")] static extern uint GetSystemPaletteEntries(IntPtr dc,uint start,uint count,[Out] byte[] entries);
 static int Main(string[] args) {
  byte[] b=new byte[1024];for(int i=0;i<b.Length;i++)b[i]=0xcc;
  IntPtr dc=GetDC(IntPtr.Zero);
  try {uint n=GetSystemPaletteEntries(dc,0,256,b);Console.WriteLine("Returned entries: "+n);
   for(int i=0;i<256;i++)if(i<10||i>=246)Console.WriteLine(i+": "+b[i*4]+","+b[i*4+1]+","+b[i*4+2]+" flags="+b[i*4+3]);
   if(args.Length==1){byte[] reserved=new byte[60];int at=0;for(int i=0;i<256;i++)if(i<10||i>=246)for(int c=0;c<3;c++)reserved[at++]=b[i*4+c];File.WriteAllBytes(args[0],reserved);}
   return n==256?0:2; // A zero count is recorded as an observation, not API success.
  }finally{ReleaseDC(IntPtr.Zero,dc);}
 }
}
