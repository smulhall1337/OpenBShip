using System;using System.IO;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Security.Cryptography;
// Copies guarded menu entry/reset/label/button/fade/input-branch/draw/raster code.
// Original device, file-loading and menu polling calls are stubs; no full game.
class QuitOracle {
 [DllImport("kernel32.dll")]static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint f,uint protection);
 [DllImport("kernel32.dll")]static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protection,out uint old);
 [DllImport("kernel32.dll")]static extern bool VirtualFree(IntPtr p,UIntPtr n,uint f);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Step();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Fade(IntPtr input);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Load(IntPtr output,IntPtr name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Call();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Wait(int ms);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Movie(int id);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate uint Clock();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Palette(IntPtr rgb);
 byte[] source;int pe;IntPtr memory;uint basis;
 static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
 int Offset(uint va){int table=pe+24+BitConverter.ToUInt16(source,pe+20);uint rva=va-0x400000;for(int i=0;i<BitConverter.ToUInt16(source,pe+6);i++){int p=table+i*40;uint start=BitConverter.ToUInt32(source,p+12),size=BitConverter.ToUInt32(source,p+16);if(rva>=start&&rva-start<size)return (int)(BitConverter.ToUInt32(source,p+20)+rva-start);}throw new Exception("Bad VA");}
 void Write(int at,byte[] b){Marshal.Copy(b,0,IntPtr.Add(memory,at),b.Length);}
 int Copy(uint va,int bytes,HashSet<uint> globals){byte[] b=new byte[bytes];Array.Copy(source,Offset(va),b,0,bytes);int count=0;for(int i=0;i<=bytes-4;i++){uint value=BitConverter.ToUInt32(b,i);if(!globals.Contains(value))continue;BitConverter.GetBytes(basis+value-0x400000).CopyTo(b,i);i+=3;count++;}Write((int)(va-0x400000),b);return count;}
 void Stub(int at,Delegate callback){var b=new List<byte>{0xb8};b.AddRange(BitConverter.GetBytes(Marshal.GetFunctionPointerForDelegate(callback).ToInt32()));b.AddRange(new byte[]{0xff,0xe0});Write(at,b.ToArray());}
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Raster(IntPtr command);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Scan(IntPtr script);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Draw(int x,int y,int frame,IntPtr asset,int mode,int percent,int flags);
 int R(int va){return Marshal.ReadInt32(memory,va-0x400000);}void W(int va,int value){Marshal.WriteInt32(memory,va-0x400000,value);}int P(int va){return unchecked((int)(basis+(uint)(va-0x400000)));}
 void Run(string exe,string graphics,string dest){
  source=File.ReadAllBytes(exe);byte[] sph=File.ReadAllBytes(Path.Combine(graphics,"EO_QUIT.SPH"));if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569"||Hash(sph)!="EAAE794A448680A6DAA0F4BD5E1B764D1EB361EFDC01A2215C7DF9AFF016955A")throw new Exception("Matching image/sprite/x86 required");pe=BitConverter.ToInt32(source,60);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x400000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation");basis=unchecked((uint)memory.ToInt32());try{
   var globals=new HashSet<uint>(new uint[]{0x4e3880,0x4b40b4,0x457074,0x4e42e0,0x4e42cc,0x4ae81c,0x4e3888,0x4b4300,0x4fbdd4,0x4e4300,0x5ae67e,0x4fb3e0,0x5ae680,0x42fda4,0x42fd88,0x42fdc8,0x44eca4,0x42f278,0x42f2cc,0x456870,0x457b50,0x457088,0x457430,0x496f90,0x456cb0,0x456ccc,0x456cbc,0x456cc4,0x456cc8,0x456cb4,0x44ef40,0x563090,0x55ea48,0x55e820,0x4e384c,0x4e42d0,0x4fb6ec,0x4aa7fc,0x456a18,0x563434,0x4b46ec,0x4e42dc,0x4e42ec});
   int[] expectedRelocations={163,22,3,6,3,4,13,10,10,0,11,1};int relocationIndex=0;foreach(var span in new[]{new uint[]{0x42f300,0xa86},new uint[]{0x422ba0,0x12e},new uint[]{0x42f200,0x75},new uint[]{0x4321f0,0x32},new uint[]{0x430820,0x5c},new uint[]{0x433140,0x95},new uint[]{0x4323e0,0xa1},new uint[]{0x4302d6,0x83},new uint[]{0x407950,0x41d},new uint[]{0x4339f3,0x40},new uint[]{0x432360,0x7b},new uint[]{0x426ec0,0x12}}){int count=Copy(span[0],(int)span[1],globals);if(count!=expectedRelocations[relocationIndex++])throw new Exception("Relocation count differs: "+span[0].ToString("X8")+" actual="+count);}
   foreach(var span in new[]{new uint[]{0x42fd88,7},new uint[]{0x42fdc8,41},new uint[]{0x42f278,21}}){byte[] data=new byte[span[1]*4];Array.Copy(source,Offset(span[0]),data,0,data.Length);for(int i=0;i<data.Length;i+=4)BitConverter.GetBytes(P((int)BitConverter.ToUInt32(data,i))).CopyTo(data,i);Write((int)(span[0]-0x400000),data);}Copy(0x42fda4,36,new HashSet<uint>());Copy(0x42f2cc,39,new HashSet<uint>());
   var refs=new HashSet<uint>{0x44ef88,0x44efc0,0x44ef34,0x457b58,0x456ca0,0x4321f0,0x456a18,0x44ef2c,0x432360};foreach(var span in new[]{new uint[]{0x44ef88,56},new uint[]{0x44efc0,392},new uint[]{0x4512d8,64},new uint[]{0x451318,64},new uint[]{0x44f148,192}})Copy(span[0],(int)span[1],refs);Copy(0x44ef34,8,new HashSet<uint>());Copy(0x44ef2c,8,new HashSet<uint>());Copy(0x44eca4,8,new HashSet<uint>());
   Write(0x210000,sph);byte[] titleSph=File.ReadAllBytes(Path.Combine(graphics,"EO_TITLE.SPH"));if(Hash(titleSph)!="D5928B8FDF76EC36D40B31E6DFAF7C0F9688735DB1B76B0FF05BF6A9CDCD511F")throw new Exception("Title identity");Write(0x220000,titleSph);W(0x563090+27*4,P(0x620000));W(0x563090+33*4,P(0x610000));W(0x4ae81c,P(0x650000));W(0x4e42cc,P(0x6a0000));
   var traces=new List<string>{"Case,Order,Kind,Value"};string name="";int order=0,waits=0,requested=-2;Load load=delegate(IntPtr output,IntPtr card){traces.Add(name+","+(order++)+",Card,"+Marshal.PtrToStringAnsi(card));return 0;};Palette pal=delegate(IntPtr rgb){traces.Add(name+","+(order++)+",Fade,"+Marshal.ReadInt16(memory,0x1ae67e));};Movie poll=delegate(int count){waits++;return requested;};Stub(0x22d90,load);Stub(0x1d40,pal);Stub(0x300b0,poll);
   var draws=new List<string>{"Selected,Order,X,Y,Frame,Mode,Percent,Flags"};int selected=0,drawOrder=0;Raster raster=null;bool rasterMode=false;Draw draw=delegate(int x,int y,int frame,IntPtr asset,int mode,int percent,int flags){if(rasterMode){W(0x710004,x);W(0x710008,y);W(0x71000c,frame);W(0x710010,asset.ToInt32());W(0x710014,mode);W(0x710018,percent);raster(new IntPtr(P(0x710000)));return;}draws.Add(selected+","+(drawOrder++)+","+x+","+y+","+frame+","+mode+","+percent+","+flags);};Stub(0x7800,draw);
   // Wrapper provides the registers/stack expected by the exact click branch.
   var wrapper=new List<byte>{0x53,0x56,0x57,0x55,0xbe};wrapper.AddRange(new byte[]{0,0,0,0});wrapper.Add(0x8b);wrapper.Add(0x35);wrapper.AddRange(BitConverter.GetBytes(P(0x563544)));wrapper.Add(0x8b);wrapper.Add(0x1d);wrapper.AddRange(BitConverter.GetBytes(P(0x563548)));wrapper.Add(0xe9);wrapper.AddRange(BitConverter.GetBytes(0x302d6-(0x1000+wrapper.Count+4)));Write(0x1000,wrapper.ToArray());
   foreach(int page in new[]{0x2f000,0x22000,0x23000,0x30000,0x32000,0x33000,0x7000,0x26000,0x1000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Protection");}
   var step=(Step)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x2f300),typeof(Step));var fade=(Fade)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x22c70),typeof(Fade));var fadeOut=(Fade)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x22c10),typeof(Fade));var scan=(Scan)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x2f200),typeof(Scan));var drawMenu=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x323e0),typeof(Call));var hit=(Step)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1000),typeof(Step));
   raster=(Raster)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x7950),typeof(Raster));
   var states=new List<string>{"Case,Step,Result,PC,State,Mode,CallbackResult,MenuKind,ButtonCount,Selected,Polls,Factor"};
   foreach(int action in new[]{-2,1,2,-1}){
    name=action==-2?"entry":action==1?"back":action==2?"quit":"escape";order=waits=0;requested=-2;W(0x457074,0);W(0x4fbdd4,4);W(0x457b58,123);scan(new IntPtr(P(0x44f148)));W(0x4b40b4,P(0x44f148));int loops=0,result=0;
    do{if(++loops>200)throw new Exception("Quit initialization wait");result=step();}while(waits==0&&result==0);
    if(result!=0||R(0x457074)!=39||R(0x457430)!=2)throw new Exception("Quit entry boundary");
    states.Add(name+",0,"+result+","+(R(0x4b40b4)-P(0x400000)+0x400000).ToString("X8")+","+R(0x457074)+","+R(0x4fbdd4)+","+R(0x457b58)+","+R(0x456a18)+","+R(0x457430)+","+R(0x44ef40)+","+waits+","+Marshal.ReadInt16(memory,0x1ae67e));
    if(action!=-2){requested=action;int previousWait=waits;for(int t=1;t<200;t++){if(R(0x4fbdd4)==7){if(fade(IntPtr.Add(memory,0xfb3e0))!=0)W(0x4fbdd4,R(0x4e4300));}else if(R(0x4fbdd4)==8){if(fadeOut(IntPtr.Zero)!=0)W(0x4fbdd4,R(0x4e4300));}else result=step();if(waits>previousWait){requested=-2;previousWait=waits;}states.Add(name+","+t+","+result+","+(R(0x4b40b4)-P(0x400000)+0x400000).ToString("X8")+","+R(0x457074)+","+R(0x4fbdd4)+","+R(0x457b58)+","+R(0x456a18)+","+R(0x457430)+","+R(0x44ef40)+","+waits+","+Marshal.ReadInt16(memory,0x1ae67e));if(result!=0||waits>2)break;if(t==199)throw new Exception("Quit route did not finish");}}
   }
   W(0x457074,0);W(0x4fbdd4,4);requested=-2;waits=0;scan(new IntPtr(P(0x44f148)));W(0x4b40b4,P(0x44f148));while(waits==0)step();
   var buttons=new List<string>{"Index,X,Y,Action,Frame,Group,Width,Height"};var hits=new List<string>{"X,Y,Action"};for(int i=0;i<2;i++){int at=0x456cb0+i*32,x=R(at),y=R(at+4),w=Marshal.ReadInt16(memory,at-0x400000+28),h=Marshal.ReadInt16(memory,at-0x400000+30);buttons.Add(i+","+x+","+y+","+R(at+12)+","+R(at+20)+","+R(at+24)+","+w+","+h);for(int py=y-1;py<=y+h;py++)foreach(int px in new[]{x-1,x,x+w/2,x+w-1,x+w}){W(0x563544,px);W(0x563548,py);hits.Add(px+","+py+","+hit());}}
   var render=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x32360),typeof(Call));var composites=new List<string>{"Selected,SHA256"};var pixels=new byte[640*480];for(int i=0;i<pixels.Length;i++)pixels[i]=(byte)(1+(i*7)%255);W(0x4e42dc,640);W(0x4e42ec,480);for(int i=0;i<2;i++){selected=i;drawOrder=0;W(0x44ef40,i);render();rasterMode=true;Write(0x250000,pixels);var guard=new byte[32];for(int j=0;j<32;j++)guard[j]=0xa7;Write(0x250000-32,guard);Write(0x250000+pixels.Length,guard);render();rasterMode=false;var output=new byte[pixels.Length];Marshal.Copy(IntPtr.Add(memory,0x250000),output,0,output.Length);for(int j=0;j<32;j++)if(Marshal.ReadByte(memory,0x250000-32+j)!=0xa7||Marshal.ReadByte(memory,0x250000+pixels.Length+j)!=0xa7)throw new Exception("Quit raster canary");composites.Add(i+","+Hash(output));}
   Directory.CreateDirectory(dest);File.WriteAllLines(Path.Combine(dest,"Quit-Flow-Oracle.csv"),states.ToArray());File.WriteAllLines(Path.Combine(dest,"Quit-Trace-Oracle.csv"),traces.ToArray());File.WriteAllLines(Path.Combine(dest,"Quit-Buttons-Oracle.csv"),buttons.ToArray());File.WriteAllLines(Path.Combine(dest,"Quit-Hit-Oracle.csv"),hits.ToArray());File.WriteAllLines(Path.Combine(dest,"Quit-Draw-Oracle.csv"),draws.ToArray());File.WriteAllLines(Path.Combine(dest,"Quit-Composite-Oracle.csv"),composites.ToArray());Console.WriteLine("Captured native Quit entry, Back/Quit/Escape flow, two button rasters and boundary samples; source IO/devices/input polling substituted.");
   GC.KeepAlive(load);GC.KeepAlive(pal);GC.KeepAlive(poll);GC.KeepAlive(draw);
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] a){try{if(a.Length!=3)return 2;new QuitOracle().Run(a[0],a[1],a[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}





