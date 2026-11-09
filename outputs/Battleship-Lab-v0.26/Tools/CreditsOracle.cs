using System;using System.IO;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Security.Cryptography;
// Copies guarded Credits parser, scroll/media dispatcher and front font raster.
// Original file/device/render/player/network callbacks are stubs; no full game.
class CreditsOracle {
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
 void Run(string exe,string graphics,string dest){
  source=File.ReadAllBytes(exe);byte[] font=File.ReadAllBytes(Path.Combine(graphics,"FEFONT.SPH")),raw=File.ReadAllBytes(Path.Combine(graphics,"CREDITS.TXT"));
  if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569"||Hash(font)!="8AC9A62A61EB8C50256491CD40E08129D90A8044F8FF1AE5E9A992CC450B5313"||Hash(raw)!="E6A8140E616CE97E96DF427D3FFE4AA00F5B8D75CEB8802468867755E26C33A0")throw new Exception("Matching executable/front font/credits/x86 required");
  pe=BitConverter.ToInt32(source,60);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x400000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation");basis=unchecked((uint)memory.ToInt32());Console.WriteLine("Fixture image base="+basis.ToString("X8"));try{
   var globals=new HashSet<uint>(new uint[]{0x4e3880,0x4b40b4,0x457074,0x4e42e0,0x457078,0x496f90,0x457434,0x4f73c8,0x456a1c,0x4e388c,0x4484a8,0x5630b4,0x457b6a,0x457daa,0x45795a,0x55ea44,0x55ea3c,0x4ae81c,0x4e42dc,0x4e42ec,0x4fb3e0,0x4e3868,0x4fb3d8,0x4e42e4,0x4e42e8,0x4b4220,0x4e42cc,0x4e3884,0x4b42a0});
   int[] expected={0,23,4,2,14,2,0,6,10,0};int index=0;var counts=new List<string>();bool bad=false;
   foreach(var span in new[]{new uint[]{0x4305cb,0x23},new uint[]{0x42f4c7,0x14b},new uint[]{0x42b8e0,0x52},new uint[]{0x4271a0,0x90},new uint[]{0x427070,0x121},new uint[]{0x427230,0x1c},new uint[]{0x427660,5},new uint[]{0x42fd01,0x61},new uint[]{0x407950,0x41d},new uint[]{0x4339f3,0x40}}){int n=Copy(span[0],(int)span[1],globals);counts.Add(span[0].ToString("X8")+":"+n);if(n!=expected[index++])bad=true;}if(bad)throw new Exception("Reviewed Credits relocations: "+string.Join(",",counts.ToArray()));
   // Exit at the reviewed dispatcher boundaries, preserving its own epilogue.
   var jump=new List<byte>{0xe9};jump.AddRange(BitConverter.GetBytes(0x2fd58-(0x2f60e+5)));Write(0x2f60e,jump.ToArray());jump=new List<byte>{0xe9};jump.AddRange(BitConverter.GetBytes(0x2fd58-(0x2fd4d+5)));Write(0x2fd4d,jump.ToArray());
   byte[] parser={0x53,0x56,0x57,0x55,0x8b,0x6c,0x24,0x14,0x8b,0x74,0x24,0x18,0x33,0xff,0xe9};var code=new List<byte>(parser);code.AddRange(BitConverter.GetBytes(0x305cb-(0x1000+19)));Write(0x1000,code.ToArray());
   foreach(var pair in new[]{new[]{0x1100,0x2f4c7},new[]{0x1200,0x2fd01}}){code=new List<byte>{0x83,0xec,8,0x53,0x56,0x57,0x55,0x33,0xdb,0xe9};code.AddRange(BitConverter.GetBytes(pair[1]-(pair[0]+14)));Write(pair[0],code.ToArray());}
   Write(0x200000,font);W(0x5630b4,P(0x600000));W(0x4ae81c,P(0x650000));W(0x4e42dc,640);W(0x4e42ec,480);Copy(0x4484a8,11,new HashSet<uint>());
   string normalized=System.Text.Encoding.ASCII.GetString(raw).Replace("\r\n","\n");byte[] textBytes=System.Text.Encoding.ASCII.GetBytes(normalized);Write(0x210000,textBytes);var guard=new byte[32];for(int j=0;j<guard.Length;j++)guard[j]=0xa7;Write(0x210000-32,guard);Write(0x210000+textBytes.Length,guard);
   int tick=0,random=0,status=0,startStatus=0;uint now=0;var calls=new List<string>();var commandRows=new List<string>{"Tick,Line,X,Y,TextBase64"};var localCommands=new List<string>();int lineOrder=0;bool recordCommands=true;
   Text draw=delegate(int x,int y,IntPtr text){string value=Marshal.PtrToStringAnsi(text);string row=(lineOrder++)+","+x+","+y+","+B64(value);localCommands.Add(row);if(recordCommands&&new HashSet<int>{0,1,6,7,14,21,252,700,1469,1470}.Contains(tick))commandRows.Add(tick+","+row);};
   Clock clock=delegate{return now;};Step rand=delegate{calls.Add("Rand:"+random);return random;};Movie movie=delegate(int id){calls.Add("Movie:"+id);return id==0?status:startStatus;};StringArg palette=delegate(IntPtr name){calls.Add("Palette:"+Marshal.PtrToStringAnsi(name));};Call clear=delegate{calls.Add("Clear");};Stub(0x222a0,clock);Stub(0x39790,rand);Stub(0x2fe70,movie);Stub(0x2a780,palette);Stub(0x26ec0,clear);Stub(0x27070,draw);
   foreach(int page in new[]{0x30000,0x2f000,0x2b000,0x27000,0x1000,0x22000,0x39000,0x2a000,0x26000,0x7000,0x33000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Protection");}
   var parse=(CreditParse)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1000),typeof(CreditParse));int count=parse(new IntPtr(P(0x610000)),textBytes.Length);W(0x4e388c,P(0x610000));W(0x4f73c8,count);for(int j=0;j<32;j++)if(Marshal.ReadByte(memory,0x210000-32+j)!=0xa7||Marshal.ReadByte(memory,0x210000+textBytes.Length+j)!=0xa7)throw new Exception("Credits parser canary");
   var texts=new List<string>{"Index,Bytes,SHA256"};int ptr=P(0x610000);for(int i=0;i<count;i++){byte[] value=System.Text.Encoding.ASCII.GetBytes(Marshal.PtrToStringAnsi(new IntPtr(ptr)));texts.Add(i+","+value.Length+","+Hash(value));ptr+=value.Length+1;}
   var metrics=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x2b8e0),typeof(Call));metrics();var metricRows=new List<string>{"Frame,Width,Height"};for(int i=0;i<564;i++)metricRows.Add(i+","+Marshal.ReadInt16(memory,0x57b6a+i*16)+","+Marshal.ReadByte(memory,0x57b6c+i*16));
   var init=(Step)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1200),typeof(Step));var step=(Step)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1100),typeof(Step));var initRows=new List<string>{"Rand,MediaReturn,Return,InputState,Line,Offset,Pointer,Calls"};
   foreach(int value in new[]{0,136,32767})foreach(int fail in new[]{0,1}){random=value;startStatus=fail;W(0x457434,123);W(0x456a1c,123);W(0x457078,123);calls.Clear();int result=init();initRows.Add(value+","+fail+","+result+","+R(0x457074)+","+Marshal.ReadInt16(memory,0x57434)+","+Marshal.ReadInt16(memory,0x56a1c)+","+(R(0x457078)==P(0x610000)?0:R(0x457078))+","+string.Join("|",calls.ToArray()));}
   random=0;startStatus=status=0;init();now=0;W(0x4e42e0,0);var states=new List<string>{"Tick,BeforeLine,BeforeOffset,AfterLine,AfterOffset,Pointer,CommandsSHA256"};
   for(tick=0;tick<=count*7;tick++){int beforeLine=Marshal.ReadInt16(memory,0x57434),beforeOffset=Marshal.ReadInt16(memory,0x56a1c);now+=50;lineOrder=0;localCommands.Clear();step();states.Add(tick+","+beforeLine+","+beforeOffset+","+Marshal.ReadInt16(memory,0x57434)+","+Marshal.ReadInt16(memory,0x56a1c)+","+(R(0x457078)-P(0x610000))+","+Hash(System.Text.Encoding.ASCII.GetBytes(string.Join("\n",localCommands.ToArray()))));}
   var endings=new List<string>{"Status,Return,InputState,Line,Offset,Calls"};foreach(int value in new[]{0,6,9,1}){status=value;random=136;startStatus=0;W(0x457074,41);W(0x457434,36);W(0x456a1c,0);int at=P(0x610000);for(int i=0;i<36;i++)at+=Marshal.PtrToStringAnsi(new IntPtr(at)).Length+1;W(0x457078,at);now+=50;calls.Clear();int result=step();endings.Add(value+","+result+","+R(0x457074)+","+Marshal.ReadInt16(memory,0x57434)+","+Marshal.ReadInt16(memory,0x56a1c)+","+string.Join("|",calls.ToArray()));}
   // Native direct text/raster, rather than a synthetic draw callback, for pixels.
   uint oldProtection;if(!VirtualProtect(IntPtr.Add(memory,0x27000),new UIntPtr(4096),4,out oldProtection))throw new Exception("Text RW");int rel=Copy(0x427070,0x121,globals);if(rel!=14)throw new Exception("Direct text relocations="+rel);if(!VirtualProtect(IntPtr.Add(memory,0x27000),new UIntPtr(4096),0x20,out oldProtection))throw new Exception("Text RX");
   var pattern=new byte[640*480];for(int i=0;i<pattern.Length;i++)pattern[i]=(byte)(1+(i*7)%255);var images=new List<string>{"Line,Offset,SHA256"};status=startStatus=0;recordCommands=false;
   foreach(int first in new[]{0,2,36,40,100,200,209})foreach(int offset in new[]{0,-12}){W(0x457074,41);W(0x457434,first);Marshal.WriteInt16(memory,0x56a1c,(short)offset);int at=P(0x610000);for(int i=0;i<first;i++)at+=Marshal.PtrToStringAnsi(new IntPtr(at)).Length+1;W(0x457078,at);Write(0x250000,pattern);Write(0x250000-32,guard);Write(0x250000+pattern.Length,guard);now+=50;step();var output=new byte[pattern.Length];Marshal.Copy(IntPtr.Add(memory,0x250000),output,0,output.Length);for(int j=0;j<32;j++)if(Marshal.ReadByte(memory,0x250000-32+j)!=0xa7||Marshal.ReadByte(memory,0x250000+pattern.Length+j)!=0xa7)throw new Exception("Credits raster canary");images.Add(first+","+offset+","+Hash(output));}
   Directory.CreateDirectory(dest);File.WriteAllLines(Path.Combine(dest,"Credits-Text-Oracle.csv"),texts.ToArray());File.WriteAllLines(Path.Combine(dest,"Credits-Metrics-Oracle.csv"),metricRows.ToArray());File.WriteAllLines(Path.Combine(dest,"Credits-Init-Oracle.csv"),initRows.ToArray());File.WriteAllLines(Path.Combine(dest,"Credits-Roll-Oracle.csv"),states.ToArray());File.WriteAllLines(Path.Combine(dest,"Credits-Commands-Oracle.csv"),commandRows.ToArray());File.WriteAllLines(Path.Combine(dest,"Credits-Media-Oracle.csv"),endings.ToArray());File.WriteAllLines(Path.Combine(dest,"Credits-Pixels-Oracle.csv"),images.ToArray());Console.WriteLine("Captured native Credits count="+count+", loop="+(count*7)+"ticks, front font/direct raster and media status boundaries; all original media/IO/device calls substituted.");GC.KeepAlive(draw);GC.KeepAlive(clock);GC.KeepAlive(rand);GC.KeepAlive(movie);GC.KeepAlive(palette);GC.KeepAlive(clear);
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){try{if(args.Length!=3)return 2;new CreditsOracle().Run(args[0],args[1],args[2]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}