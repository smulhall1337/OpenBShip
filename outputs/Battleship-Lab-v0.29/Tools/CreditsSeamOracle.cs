using System;using System.IO;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Security.Cryptography;
// Copies guarded Credits parser, scroll/media dispatcher and front font raster.
// Original file/device/render/player/network callbacks are stubs; no full game.
class CreditsSeamOracle {
 [DllImport("kernel32.dll")]static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint f,uint protection);
 [DllImport("kernel32.dll")]static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protection,out uint old);
 [DllImport("kernel32.dll")]static extern bool VirtualFree(IntPtr p,UIntPtr n,uint f);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Step();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Measure(IntPtr text,int style);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Position(int x,int y);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void StringArg(IntPtr text);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Fade(IntPtr input);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Load(IntPtr output,IntPtr name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Call();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Wait(int ms);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Movie(int id);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate uint Clock();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Palette(IntPtr rgb);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int CreditParse(IntPtr text,int bytes);
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
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Parse(IntPtr source,IntPtr table,int count);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Format(IntPtr dest,IntPtr format,IntPtr first,IntPtr second);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Text(int x,int y,IntPtr value);
 string B64(string s){return Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(s));}
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate uint ReadFile(IntPtr data,uint size,uint count,IntPtr file);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Request(int movie,IntPtr owner);
 void Run(string exe,string graphics,string dest){
  source=File.ReadAllBytes(exe);byte[] island=File.ReadAllBytes(Path.Combine(graphics,"ISLAND.COL"));if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569"||Hash(island)!="DEBB5F1835FFB4D23169086BFE7ED505165ADD48918E28876FA4FD4F9E13D6A7")throw new Exception("Matching executable/island/x86 required");pe=BitConverter.ToInt32(source,60);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x400000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation");basis=unchecked((uint)memory.ToInt32());Console.WriteLine("Fixture image base="+basis.ToString("X8"));try{
   var globals=new HashSet<uint>(new uint[]{0x457074,0x4e42e0,0x457078,0x496f90,0x457434,0x4f73c8,0x456a1c,0x4e388c,0x4484a8,0x4ae81c,0x4e3770,0x4b42a0,0x5ae590,0x563434,0x4fbdd8,0x55ea4c,0x55e7f0,0x4fbc30,0x55e818,0x4aa7f4,0x4fbdd4,0x4fb3fe,0x4fb41c});
   var specs=new[]{new uint[]{0x42f4c7,0x14b},new uint[]{0x42fd01,0x61},new uint[]{0x42fe70,0x72},new uint[]{0x402830,0x7e},new uint[]{0x402732,0x68},new uint[]{0x426ec0,0x12}};int[] expected={23,6,5,11,8,1};var counts=new List<string>();bool bad=false;for(int i=0;i<specs.Length;i++){int count=Copy(specs[i][0],(int)specs[i][1],globals);counts.Add(specs[i][0].ToString("X8")+":"+count);bad|=count!=expected[i];}if(bad)throw new Exception("Reviewed seam relocations: "+string.Join(",",counts.ToArray()));
   var code=new List<byte>{0xe9};code.AddRange(BitConverter.GetBytes(0x2fd58-(0x2f60e+5)));Write(0x2f60e,code.ToArray());code=new List<byte>{0xe9};code.AddRange(BitConverter.GetBytes(0x2fd58-(0x2fd4d+5)));Write(0x2fd4d,code.ToArray());
   foreach(var pair in new[]{new[]{0x1100,0x2f4c7},new[]{0x1200,0x2fd01}}){code=new List<byte>{0x83,0xec,8,0x53,0x56,0x57,0x55,0x33,0xdb,0xe9};code.AddRange(BitConverter.GetBytes(pair[1]-(pair[0]+14)));Write(pair[0],code.ToArray());}
   code=new List<byte>{0x56,0x57,0x53,0xe9};code.AddRange(BitConverter.GetBytes(0x2732-(0x1300+8)));Write(0x1300,code.ToArray());Write(0x279a,new byte[]{0x5b,0x5f,0x5e,0xc3});Copy(0x4484a8,11,new HashSet<uint>());
   W(0x4aa7f4,P(0x610000));W(0x4ae81c,P(0x650000));W(0x4e388c,P(0x620000));W(0x4f73c8,210);var text=new byte[210];Write(0x220000,text);W(0x4fbdd8,1);
   uint now=0;int id=0,loads=0,stops=0,clears=0,randCalls=0,draws=0;int[] lengths={3,4};int failLoad=0;var events=new List<string>();var palettes=new Dictionary<int,byte[]>();for(int k=1;k<=3;k++){byte[] p=new byte[768];for(int i=0;i<768;i++)p[i]=(byte)((i*13+k*17)%64);palettes[k]=p;}
   byte[] basePalette=(byte[])island.Clone();int[] offsets={51,52,53,54,55,56,57,58,59,39,30,31,32,40,41};byte[] values={166,178,198,206,215,227,138,150,174,13,255,255,255,114,255};for(int i=0;i<offsets.Length;i++)basePalette[offsets[i]]=values[i];
   Clock clock=delegate{return now;};Step rand=delegate{int choice=randCalls++%2;events.Add("Rand:"+choice);return choice;};StringArg loadIsland=delegate(IntPtr path){events.Add("Island");Write(0xfb3e0,basePalette);};
   Step nextLength=delegate{return R(0x55ea4c)>=lengths[id-1]?-1:8;};ReadFile read=delegate(IntPtr data,uint size,uint count,IntPtr file){for(int i=0;i<count;i++)Marshal.WriteByte(data,i,0);return count;};
   Call decode=delegate{int frame=R(0x55ea4c)-1;events.Add("Decode:"+id+":"+frame);byte[] image=new byte[640*480];for(int i=0;i<image.Length;i++)image[i]=(byte)(20+(id*19+frame)%118);Write(0x250000,image);};
   Wait cue=delegate(int frame){events.Add("Cue:"+id+":"+frame);};Call stop=delegate{stops++;events.Add("Stop:"+id);Marshal.WriteByte(memory,0xb42a0,0);};
   var subset=(Call)null;Request request=delegate(int movie,IntPtr owner){events.Add("Request:"+movie);if(movie<1||movie>2||movie==failLoad)return 1;id=movie;loads++;Write(0x210000,palettes[id]);subset();W(0x55ea4c,0);W(0x55e818,8);W(0x4e42e0,(int)now);Marshal.WriteByte(memory,0xb42a0,1);return 0;};
   Measure measure=delegate(IntPtr value,int style){return 0;};Text draw=delegate(int x,int y,IntPtr value){if(draws++==0)events.Add("Text");Marshal.WriteByte(memory,0x250000,209);};
   Stub(0x222a0,clock);Stub(0x39790,rand);Stub(0x2a780,loadIsland);Stub(0x33930,nextLength);Stub(0x39470,read);Stub(0x28b0,decode);Stub(0x29f0,cue);Stub(0x2980,stop);Stub(0x2460,request);Stub(0x271a0,measure);Stub(0x27070,draw);
   // Record clear while preserving execution of its real 640x480 zero fill.
   Call beforeClear=delegate{clears++;events.Add("Clear");};Stub(0x1400,beforeClear);code=new List<byte>{0xe8};code.AddRange(BitConverter.GetBytes(0x1400-(0x1500+5)));code.Add(0xe9);code.AddRange(BitConverter.GetBytes(0x26ec0-(0x1500+10)));Write(0x1500,code.ToArray());uint rw; // redirect only the two source calls in dispatcher to wrapper
   byte[] tickBytes=new byte[0x14b];Marshal.Copy(IntPtr.Add(memory,0x2f4c7),tickBytes,0,tickBytes.Length);int callAt=0x2f51a-0x2f4c7;BitConverter.GetBytes(0x1500-(0x2f51a+5)).CopyTo(tickBytes,callAt+1);Write(0x2f4c7,tickBytes);
   foreach(int page in new[]{0x1000,0x2000,0x2f000,0x22000,0x39000,0x2a000,0x33000,0x26000,0x27000})if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out rw))throw new Exception("Protection");
   subset=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1300),typeof(Call));var init=(Step)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1200),typeof(Step));var step=(Step)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1100),typeof(Step));
   var paletteRows=new List<string>{"Quality,State,SHA256,InputSHA256"};foreach(int quality in new[]{1,2})foreach(int state in new[]{0,14,41}){Write(0xfb3e0,basePalette);Write(0x210000,palettes[1]);W(0x4fbdd4,quality);W(0x457074,state);subset();byte[] output=new byte[768],input=new byte[768];Marshal.Copy(IntPtr.Add(memory,0xfb3e0),output,0,768);Marshal.Copy(IntPtr.Add(memory,0x210000),input,0,768);paletteRows.Add(quality+","+state+","+Hash(output)+","+Hash(input));}
   var rows=new List<string>{"Case,Tick,Result,ID,Counter,Active,State,Line,Offset,Loads,Stops,Clears,RandCalls,CanvasSHA256,PaletteSHA256,EventsBase64"};byte[] guard=new byte[32];for(int i=0;i<32;i++)guard[i]=0xa7;
   foreach(string name in new[]{"normal-low","normal-high","two-frame","single-frame","load-fails-initial","load-fails-next","space-tick1","space-completion","space-init","mouse-left","mouse-right","disabled"}){
    now=0;id=loads=stops=clears=randCalls=0;failLoad=name=="load-fails-initial"?1:name=="load-fails-next"?2:0;lengths=name=="two-frame"?new[]{2,2}:name=="single-frame"?new[]{1,3}:new[]{3,4};W(0x4fbdd4,name=="normal-high"?2:1);W(0x4e42e0,0);W(0x55ea4c,123);W(0x457434,123);W(0x456a1c,123);W(0x457078,123);Marshal.WriteByte(memory,0xe3770,(byte)(name=="disabled"?0:'L'));Marshal.WriteByte(memory,0xb42a0,0);Marshal.WriteByte(memory,0x1ae590,(byte)(name=="space-init"?1:0));W(0x563434,0);Write(0x250000,new byte[640*480]);Write(0x250000-32,guard);Write(0x250000+640*480,guard);events.Clear();draws=0;int result=init();
    for(int tick=0;tick<=8;tick++){
     if(tick>0){if(result!=0||R(0x457074)==0)break;now+=50;Marshal.WriteByte(memory,0x1ae590,(byte)((name=="space-tick1"&&tick==1||name=="space-completion"&&tick==2)?1:0));W(0x563434,name=="mouse-left"&&tick==1?1:name=="mouse-right"?2:0);events.Clear();draws=0;result=step();}
     byte[] canvas=new byte[640*480],palette=new byte[768];Marshal.Copy(IntPtr.Add(memory,0x250000),canvas,0,canvas.Length);Marshal.Copy(IntPtr.Add(memory,0xfb3e0),palette,0,768);for(int i=0;i<32;i++)if(Marshal.ReadByte(memory,0x250000-32+i)!=0xa7||Marshal.ReadByte(memory,0x250000+canvas.Length+i)!=0xa7)throw new Exception("Clear/render canary");rows.Add(name+","+tick+","+result+","+id+","+R(0x55ea4c)+","+Marshal.ReadByte(memory,0xb42a0)+","+R(0x457074)+","+Marshal.ReadInt16(memory,0x57434)+","+Marshal.ReadInt16(memory,0x56a1c)+","+loads+","+stops+","+clears+","+randCalls+","+Hash(canvas)+","+Hash(palette)+","+Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(string.Join("|",events.ToArray()))));
    }
   }
   Directory.CreateDirectory(dest);File.WriteAllLines(Path.Combine(dest,"Credits-Seam-Oracle.csv"),rows.ToArray());File.WriteAllLines(Path.Combine(dest,"Credits-PaletteSubset-Oracle.csv"),paletteRows.ToArray());Console.WriteLine("Captured source Credits init/wait/wrapper/frame/clear/palette seams with synthetic recording media, no original IO/devices.");GC.KeepAlive(clock);GC.KeepAlive(rand);GC.KeepAlive(loadIsland);GC.KeepAlive(nextLength);GC.KeepAlive(read);GC.KeepAlive(decode);GC.KeepAlive(cue);GC.KeepAlive(stop);GC.KeepAlive(request);GC.KeepAlive(measure);GC.KeepAlive(draw);GC.KeepAlive(beforeClear);
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){try{if(args.Length!=3)return 2;new CreditsSeamOracle().Run(args[0],args[1],args[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
