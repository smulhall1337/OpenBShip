using System;using System.IO;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Security.Cryptography;
// Copies guarded menu entry/reset/label/button/fade/input-branch/draw/raster code.
// Original device, file-loading and menu polling calls are stubs; no full game.
class PreferencesOracle {
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
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int SetVolume(IntPtr self,int decibels);
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
  source=File.ReadAllBytes(exe);byte[] sph=File.ReadAllBytes(Path.Combine(graphics,"EO_PREFS.SPH")),titleSph=File.ReadAllBytes(Path.Combine(graphics,"EO_TITLE.SPH"));
  if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569"||Hash(sph)!="F381AD219D2136674E8B88784C5D4099484C870B388C9615F53B3D0AD561E114"||Hash(titleSph)!="D5928B8FDF76EC36D40B31E6DFAF7C0F9688735DB1B76B0FF05BF6A9CDCD511F")throw new Exception("Matching executable/sprites/x86 required");
  pe=BitConverter.ToInt32(source,60);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x400000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation");basis=unchecked((uint)memory.ToInt32());Console.WriteLine("Fixture image base="+basis.ToString("X8"));try{
   var globals=new HashSet<uint>(new uint[]{0x4e3880,0x4b40b4,0x457074,0x4e42e0,0x4e42cc,0x4ae81c,0x4e3888,0x4b4300,0x4fbdd4,0x4e4300,0x5ae67e,0x4fb3e0,0x5ae680,0x42fda4,0x42fd88,0x42fdc8,0x44eca4,0x42f278,0x42f2cc,0x456870,0x457b50,0x457088,0x457430,0x496f90,0x456cb0,0x456ccc,0x456cbc,0x456cc4,0x456cc8,0x456cb4,0x44ef40,0x563090,0x55ea48,0x55e820,0x4e384c,0x4e42d0,0x4fb6ec,0x4aa7fc,0x456a18,0x563434,0x4b46ec,0x4e42dc,0x4e42ec,0x4fb904,0x4fbc34,0x4fbdf4,0x4e3890,0x456dc8,0x456dc4,0x456db0,0x456db4,0x563544,0x563548,0x563554,0x563550,0x5ae598,0x5ae596,0x5ae58b,0x5c1ed0,0x4484bc,0x5c1ef4,0x5c1f18});
   int[] expected={165,22,3,6,3,4,13,51,35,10,0,2,2,3,1,4};int index=0;bool badRelocations=false;var observedRelocations=new List<string>();
   foreach(var span in new[]{new uint[]{0x42f300,0xa86},new uint[]{0x422ba0,0x12e},new uint[]{0x42f200,0x75},new uint[]{0x4321f0,0x32},new uint[]{0x430820,0x5c},new uint[]{0x433140,0x95},new uint[]{0x4323e0,0xa1},new uint[]{0x4300b0,0x2a9},new uint[]{0x432e80,0x244},new uint[]{0x407950,0x41d},new uint[]{0x4339f3,0x40},new uint[]{0x420a20,0x78},new uint[]{0x420b50,0x41},new uint[]{0x4206a0,0x3a},new uint[]{0x4223b6,0x20},new uint[]{0x41b0e0,0x48}}){int count=Copy(span[0],(int)span[1],globals);observedRelocations.Add(span[0].ToString("X8")+":"+count);if(count!=expected[index])badRelocations=true;index++;}if(badRelocations)throw new Exception("Reviewed relocations: "+string.Join(",",observedRelocations.ToArray()));Write(0x223d6,new byte[]{0xc3});
   foreach(var span in new[]{new uint[]{0x42fd88,7},new uint[]{0x42fdc8,41},new uint[]{0x42f278,21}}){byte[] data=new byte[span[1]*4];Array.Copy(source,Offset(span[0]),data,0,data.Length);for(int i=0;i<data.Length;i+=4)BitConverter.GetBytes(P((int)BitConverter.ToUInt32(data,i))).CopyTo(data,i);Write((int)(span[0]-0x400000),data);}Copy(0x42fda4,36,new HashSet<uint>());Copy(0x42f2cc,39,new HashSet<uint>());
   var refs=new HashSet<uint>{0x44ef88,0x44efc0,0x44ef34,0x44ee9c,0x457b58,0x456ca0,0x4321f0,0x456a18,0x432e80,0x4fb904,0x4fbc34,0x4fbdf4,0x4e3890,0x42e8d0,0x4224b0,0x42e600};Copy(0x450ec8,904,refs);Copy(0x44ef88,56,refs);Copy(0x44efc0,392,refs);Copy(0x44ef34,8,new HashSet<uint>());Copy(0x44ee9c,8,new HashSet<uint>());Copy(0x44eca4,8,new HashSet<uint>());
   Write(0x200000,sph);Write(0x220000,titleSph);W(0x563090+30*4,P(0x600000));W(0x563090+27*4,P(0x620000));W(0x4ae81c,P(0x660000));W(0x4e42cc,P(0x6b0000));W(0x4e42dc,640);W(0x4e42ec,480);
   var pattern=new byte[640*480];for(int i=0;i<pattern.Length;i++)pattern[i]=(byte)(1+(i*7)%255);int waits=0,action=-2,saves=0,hover=-1;var calls=new List<string>();
   Load load=delegate(IntPtr output,IntPtr resource){Marshal.Copy(pattern,0,output,pattern.Length);return 0;};Palette palette=delegate(IntPtr ignored){};Movie poll=delegate(int count){int next=action;action=-2;if(next==-2)waits++;return next;};Call noop=delegate{};Step vol=delegate{calls.Add("Volume");return 0;},preview=delegate{calls.Add("Preview");return 0;},save=delegate{saves++;return 0;},select=delegate{return hover;};
   Stub(0x22d90,load);Stub(0x1d40,palette);Stub(0x300b0,poll);Stub(0x7890,noop);Stub(0x27300,noop);Stub(0x2e8d0,vol);Stub(0x224b0,preview);Stub(0x2e600,save);Stub(0x30ab0,select);
   bool rasterMode=false,recordDraw=false;int drawCase=0,drawOrder=0;var draws=new List<string>{"Case,Order,X,Y,Frame,Mode,Percent,Flags"};Raster raster=(Raster)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x7950),typeof(Raster));
   Draw draw=delegate(int x,int y,int frame,IntPtr asset,int mode,int percent,int flags){if(recordDraw)draws.Add(drawCase+","+(drawOrder++)+","+x+","+y+","+frame+","+mode+","+percent+","+flags);if(rasterMode){W(0x700004,x);W(0x700008,y);W(0x70000c,frame);W(0x700010,asset.ToInt32());W(0x700014,mode);W(0x700018,percent);raster(new IntPtr(P(0x700000)));}};Stub(0x7800,draw);
   var wrapper=new List<byte>{0xe9};wrapper.AddRange(BitConverter.GetBytes(0x300b0-(0x1000+5)));Write(0x1000,wrapper.ToArray());
   var gainWrap=new List<byte>{0x56,0xbe,7,0,0,0,0xe8};gainWrap.AddRange(BitConverter.GetBytes(0x223b6-(0x1100+11)));gainWrap.AddRange(new byte[]{0x5e,0xc3});Write(0x1100,gainWrap.ToArray());
   foreach(int page in new[]{0x2f000,0x22000,0x23000,0x27000,0x30000,0x32000,0x33000,0x7000,0x1000,0x20000,0x1b000,0x2e000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Protection");}
   var step=(Step)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x2f300),typeof(Step));var fade=(Fade)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x22c70),typeof(Fade));var scan=(Scan)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x2f200),typeof(Scan));var drawPrefs=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x32e80),typeof(Call));
   Action run=delegate{int old=waits,loops=0;do{if(++loops>200)throw new Exception("Preferences boundary timeout");if(R(0x4fbdd4)==7){if(fade(IntPtr.Add(memory,0xfb3e0))!=0)W(0x4fbdd4,R(0x4e4300));}else if(step()!=0)throw new Exception("Unexpected script return");}while(waits==old);};
   Action init=delegate{W(0x457074,0);W(0x4fbdd4,2);W(0x4b40b4,P(0x450ec8));action=-2;scan(new IntPtr(P(0x450ec8)));run();if(R(0x4b40b4)!=P(0x451044)||R(0x457074)!=39||R(0x457430)!=13||R(0x456a18)!=4)throw new Exception("Wrong Preferences wait boundary");};
   Console.WriteLine("Stage: initialize");W(0x4fb904,1);W(0x4fbc34,0);W(0x4fbdf4,0);W(0x4e3890,80);init();var buttons=new List<string>{"Index,X,Y,Action,Frame,Group,Width,Height"};for(int i=0;i<13;i++){int at=0x456cb0+i*32;buttons.Add(i+","+R(at)+","+R(at+4)+","+R(at+12)+","+R(at+20)+","+R(at+24)+","+Marshal.ReadInt16(memory,at-0x400000+28)+","+Marshal.ReadInt16(memory,at-0x400000+30));}
   var changes=new List<string>{"Quality,Radar,Difficulty,Volume,Action,FinalQuality,FinalRadar,FinalDifficulty,FinalVolume,Title,SaveCalls,Calls"};
   foreach(int q in new[]{1,2})foreach(int r in new[]{0,1})for(int d=0;d<4;d++)foreach(int v in new[]{0,5,80,95,100})foreach(int a in new[]{1,2,3,4,5,6,7,8,9,55,98,97,99,-1}){
    W(0x4fb904,q);W(0x4fbc34,r);W(0x4fbdf4,d);W(0x4e3890,v);init();calls.Clear();saves=0;action=a;run();bool title=R(0x4b40b4)==P(0x44f068);changes.Add(q+","+r+","+d+","+v+","+a+","+R(0x4fb904)+","+R(0x4fbc34)+","+R(0x4fbdf4)+","+R(0x4e3890)+","+(title?1:0)+","+saves+","+string.Join("|",calls.ToArray()));
   }
   Console.WriteLine("Stage: input");
   // Restore actual input handler after scripted input polling is complete.
   uint oldProtection;if(!VirtualProtect(IntPtr.Add(memory,0x30000),new UIntPtr(4096),4,out oldProtection))throw new Exception("Input RW");int inputRel=Copy(0x4300b0,0x2a9,globals);if(inputRel!=51)throw new Exception("Input relocations="+inputRel);if(!VirtualProtect(IntPtr.Add(memory,0x30000),new UIntPtr(4096),0x20,out oldProtection))throw new Exception("Input RX");
   var hit=(Movie)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x300b0),typeof(Movie));var hits=new List<string>{"X,Y,Buttons,Previous,InitialVolume,Action,Volume"};W(0x4fb904,1);W(0x4fbc34,0);W(0x4fbdf4,0); // init must be restored using scan/step before polling was replaced; records reset below
   // Rebuild the exact thirteen records without dispatching another wait39.
   var fill=(Scan)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x30820),typeof(Scan));fill(new IntPtr(P(0x450eec)));W(0x456a18,4);
   foreach(int y in new[]{110,111,140,141,191,192,221,222,272,273,302,303,353,354,383,384,430,431,460,461,466,467})foreach(int x in new[]{14,15,141,142,153,225,226,304,305,310,311,325,326,394,395,404,405,409,410,473,474,488,489,497,498,622,623})foreach(int b in new[]{1,2}){
    W(0x563544,x);W(0x563548,y);W(0x563434,b);W(0x4b46ec,0);W(0x4e3890,80);Marshal.WriteByte(memory,0x1ae58b,0);int a=hit(13);hits.Add(x+","+y+","+b+",0,80,"+a+","+R(0x4e3890));
   }
   var bars=new List<string>{"X,Volume"};for(int x=142;x<405;x++){W(0x563544,x);W(0x563548,354);W(0x563434,1);W(0x4b46ec,0);W(0x4e3890,80);hit(13);bars.Add(x+","+R(0x4e3890));}
   Console.WriteLine("Stage: raster");
   var images=new List<string>{"Case,Quality,Radar,Difficulty,Volume,Hover,SHA256"};rasterMode=recordDraw=true;drawCase=0;
   foreach(int q in new[]{1,2})foreach(int r in new[]{0,1})for(int d=0;d<4;d++)foreach(int v in new[]{0,5,50,80,100})foreach(int h in new[]{-1,10,11,12}){
    W(0x4fb904,q);W(0x4fbc34,r);W(0x4fbdf4,d);W(0x4e3890,v);hover=h;drawOrder=0;Write(0x260000,pattern);var guard=new byte[32];for(int j=0;j<guard.Length;j++)guard[j]=0xa7;Write(0x260000-32,guard);Write(0x260000+pattern.Length,guard);drawPrefs();for(int j=0;j<guard.Length;j++)if(Marshal.ReadByte(memory,0x260000-32+j)!=0xa7||Marshal.ReadByte(memory,0x260000+pattern.Length+j)!=0xa7)throw new Exception("Preferences raster canary");var output=new byte[pattern.Length];Marshal.Copy(IntPtr.Add(memory,0x260000),output,0,output.Length);images.Add(drawCase+","+q+","+r+","+d+","+v+","+h+","+Hash(output));drawCase++;
   }
   Console.WriteLine("Stage: gain");
   rasterMode=recordDraw=false;int observed=0;SetVolume set=delegate(IntPtr self,int db){observed=db;return 0;};W(0x5c1ed0,1);W(0x4484bc,0);W(0x5c1f18+7*324,P(0x7d0000));W(0x7d0000,P(0x7d0100));W(0x7d0100+0x3c,Marshal.GetFunctionPointerForDelegate(set).ToInt32());
   var gain=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1100),typeof(Call));var gains=new List<string>{"Percent,NativeVolume,HundredthsDb"};for(int v=0;v<=100;v++){W(0x4e3890,v);observed=int.MinValue;gain();gains.Add(v+","+R(0x5c1ef4+7*324)+","+observed);}
   Directory.CreateDirectory(dest);File.WriteAllLines(Path.Combine(dest,"Preferences-Buttons-Oracle.csv"),buttons.ToArray());File.WriteAllLines(Path.Combine(dest,"Preferences-Changes-Oracle.csv"),changes.ToArray());File.WriteAllLines(Path.Combine(dest,"Preferences-Hits-Oracle.csv"),hits.ToArray());File.WriteAllLines(Path.Combine(dest,"Preferences-Bar-Oracle.csv"),bars.ToArray());File.WriteAllLines(Path.Combine(dest,"Preferences-Draw-Oracle.csv"),draws.ToArray());File.WriteAllLines(Path.Combine(dest,"Preferences-Composite-Oracle.csv"),images.ToArray());File.WriteAllLines(Path.Combine(dest,"Preferences-Volume-Oracle.csv"),gains.ToArray());Console.WriteLine("Captured bounded original Preferences script/button/action/input/draw/gain fixtures; all device/file/network/save calls substituted.");GC.KeepAlive(load);GC.KeepAlive(palette);GC.KeepAlive(poll);GC.KeepAlive(noop);GC.KeepAlive(vol);GC.KeepAlive(preview);GC.KeepAlive(save);GC.KeepAlive(select);GC.KeepAlive(draw);GC.KeepAlive(set);
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){try{if(args.Length!=3)return 2;new PreferencesOracle().Run(args[0],args[1],args[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}