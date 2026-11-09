using System;using System.IO;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Security.Cryptography;
// Runs only native ending prefix dispatcher/fade fragments. File, movie, clock
// and graphics calls are recording stubs; stops BEFORE the menu handoff opcode.
class EndingSeamOracle {
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Request(int id,IntPtr owner);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate uint ReadFile(IntPtr data,uint size,uint count,IntPtr file);
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
 void Run(string exe,string dest){source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!="B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569")throw new Exception("Matching image/x86 required");pe=BitConverter.ToInt32(source,60);memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x400000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation");basis=unchecked((uint)memory.ToInt32());try{
  var globals=new HashSet<uint>(new uint[]{0x4e3880,0x4b40b4,0x457074,0x4e42e0,0x4e42cc,0x4ae81c,0x4e3888,0x4b4300,0x4fbdd4,0x4e4300,0x5ae67e,0x4fb3e0,0x5ae680,0x42fda4,0x42fd88,0x42fdc8,0x44eca4,0x4e3770,0x4b42a0,0x5ae590,0x563434,0x4fbdd8,0x55ea4c,0x55e7f0,0x4fbc30,0x55e818,0x4aa7f4});
  int dispatch=Copy(0x42f300,0xa86,globals),fade=Copy(0x422ba0,0x12e,globals);if(dispatch!=139||fade!=22)throw new Exception("Relocation count differs "+dispatch+"/"+fade);if(Copy(0x42fe70,0x72,globals)!=5||Copy(0x402830,0x7e,globals)!=11)throw new Exception("Media seam relocations");
  foreach(var span in new[]{new uint[]{0x42fd88,7},new uint[]{0x42fdc8,41}}){byte[] b=new byte[span[1]*4];Array.Copy(source,Offset(span[0]),b,0,b.Length);for(int i=0;i<b.Length;i+=4){uint va=BitConverter.ToUInt32(b,i);BitConverter.GetBytes(basis+va-0x400000).CopyTo(b,i);}Write((int)(span[0]-0x400000),b);}Copy(0x42fda4,0x24,new HashSet<uint>());
  foreach(uint va in new[]{0x44ee64u,0x44ee5cu,0x44eca4u})Copy(va,16,new HashSet<uint>());
  foreach(uint va in new[]{0x4512d8u,0x451318u})Copy(va,64,new HashSet<uint>{0x44ee64,0x44ee5c});
  Marshal.WriteInt32(memory,0x4ae81c-0x400000,unchecked((int)(basis+0x200000)));Marshal.WriteInt32(memory,0x4e42cc-0x400000,unchecked((int)(basis+0x250000)));
  Marshal.WriteInt32(memory,0xaa7f4,unchecked((int)(basis+0x180000)));Marshal.WriteInt32(memory,0xfbdd8,1);
  uint time=100;int id=0,stops=0,requests=0,length=3;string name="";var events=new List<string>();var rows=new List<string>{"Case,Tick,Return,State,PC,Counter,Active,Stops,Requests,Factor,CanvasSHA256,EventsBase64"};
  Clock clock=delegate{return ++time;};Palette palette=delegate(IntPtr rgb){events.Add("Fade:"+Marshal.ReadInt16(memory,0x1ae67e));};Call clear=delegate{events.Add("Clear");Write(0x200000,new byte[640*480]);};
  Request request=delegate(int movie,IntPtr owner){requests++;events.Add("Request:"+movie);if(name.EndsWith("load-fails")||movie==0)return 1;id=movie;Marshal.WriteInt32(memory,0x15ea4c,0);Marshal.WriteInt32(memory,0x15e818,8);Marshal.WriteInt32(memory,0xe42e0,(int)time);Marshal.WriteByte(memory,0xb42a0,1);return 0;};
  Step next=delegate{return Marshal.ReadInt32(memory,0x15ea4c)>=length?-1:8;};ReadFile read=delegate(IntPtr data,uint size,uint count,IntPtr file){for(int i=0;i<count;i++)Marshal.WriteByte(data,i,0);return count;};Wait cue=delegate(int frame){events.Add("Cue:"+frame);};Call decode=delegate{int frame=Marshal.ReadInt32(memory,0x15ea4c)-1;events.Add("Decode:"+frame);byte[] pixels=new byte[640*480];for(int i=0;i<pixels.Length;i++)pixels[i]=(byte)(20+(id==140?40:0)+frame);Write(0x200000,pixels);};Call stop=delegate{stops++;events.Add("Stop");Marshal.WriteByte(memory,0xb42a0,0);};
  Stub(0x222a0,clock);Stub(0x1d40,palette);Stub(0x26ec0,clear);Stub(0x2460,request);Stub(0x33930,next);Stub(0x39470,read);Stub(0x29f0,cue);Stub(0x28b0,decode);Stub(0x2980,stop);
  foreach(int page in new[]{0x2f000,0x22000,0x26000,0x2000,0x33000,0x39000,0x1000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Protection");}
  var step=(Step)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x2f300),typeof(Step));var decrease=(Step)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x22c10),typeof(Step));byte[] guard=new byte[32];for(int i=0;i<guard.Length;i++)guard[i]=0xa7;
  foreach(uint script in new[]{0x4512d8u,0x451318u})foreach(string kind in new[]{"normal","space-tick1","space-eof","space-init","single-frame","load-fails"}){
   name=(script==0x4512d8?"VICTORY-":"LOSE-")+kind;id=stops=requests=0;time=100;length=kind=="single-frame"?1:script==0x4512d8?3:4;Write(0x200000,new byte[640*480]);Write(0x200000-32,guard);Write(0x200000+640*480,guard);Write(0xe3770,new byte[]{(byte)'H',0});Marshal.WriteByte(memory,0xb42a0,0);Marshal.WriteInt32(memory,0x15ea4c,0);Marshal.WriteInt32(memory,0xb40b4,unchecked((int)(basis+script-0x400000+32)));Marshal.WriteInt32(memory,0x57074,0);Marshal.WriteInt32(memory,0xfbdd4,2);Marshal.WriteInt32(memory,0x163434,0);Marshal.WriteInt16(memory,0x1ae67e,64);
   for(int tick=0;tick<100;tick++){
    events.Clear();int state=Marshal.ReadInt32(memory,0x57074),mode=Marshal.ReadInt32(memory,0xfbdd4),counter=Marshal.ReadInt32(memory,0x15ea4c);bool skip=kind=="space-init"&&tick==0||kind=="space-tick1"&&tick==1||kind=="space-eof"&&state==6&&counter==length-1;Marshal.WriteByte(memory,0x1ae590,(byte)(skip?1:0));int result=0;
    if(mode==8){if(decrease()!=0)Marshal.WriteInt32(memory,0xfbdd4,Marshal.ReadInt32(memory,0xe4300));}else result=step();
    byte[] pixels=new byte[640*480];Marshal.Copy(IntPtr.Add(memory,0x200000),pixels,0,pixels.Length);for(int i=0;i<32;i++)if(Marshal.ReadByte(memory,0x200000-32+i)!=0xa7||Marshal.ReadByte(memory,0x200000+pixels.Length+i)!=0xa7)throw new Exception("Canvas canary");uint pc=unchecked((uint)Marshal.ReadInt32(memory,0xb40b4));rows.Add(name+","+tick+","+result+","+Marshal.ReadInt32(memory,0x57074)+","+(pc-basis+0x400000).ToString("X8")+","+Marshal.ReadInt32(memory,0x15ea4c)+","+Marshal.ReadByte(memory,0xb42a0)+","+stops+","+requests+","+Marshal.ReadInt16(memory,0x1ae67e)+","+Hash(pixels)+","+Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(string.Join("|",events.ToArray()))));
    if(kind=="single-frame"&&tick==0||kind=="load-fails"&&tick==3||Marshal.ReadInt32(memory,0xfbdd4)==2&&pc==basis+script-0x400000+48)break;if(tick==99)throw new Exception("Ending seam did not reach bounded exit");
   }
  }
  Directory.CreateDirectory(dest);File.WriteAllLines(Path.Combine(dest,"Ending-Seam-Oracle.csv"),rows.ToArray());Console.WriteLine("Captured12native ending opcode/wait/wrapper/EOF/skip/fade cases with recording synthetic media; no original IO/devices.");GC.KeepAlive(clock);GC.KeepAlive(palette);GC.KeepAlive(clear);GC.KeepAlive(request);GC.KeepAlive(next);GC.KeepAlive(read);GC.KeepAlive(cue);GC.KeepAlive(decode);GC.KeepAlive(stop);
 }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}}
 static int Main(string[] args){try{if(args.Length!=2&&args.Length!=3)return 2;new EndingSeamOracle().Run(args[0],args[args.Length-1]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}








