using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// A controlled original phase-6 -> impact -> feedback/sink-cleanup slice.
// Seed/fleet/ships/target are fixture inputs, not original initialization.
class ClassicDamageOrderOracle : ClassicStageOrderOracle {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protect);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protect,out uint old);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Call();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Seed(uint value);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int RandomCall(int ret);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Select();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Movie(int id,IntPtr owner);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Message(int id,int owner,int x,int y);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Terminal(int winner);
 const int Fleet=0xb47a0,TargetShip=0x1f0000,GuardShip=0x1f0100,Viewer=0x1f1000;
 void CopyImpact(uint first,uint end,int expected,bool tables) {
  var globals=new HashSet<uint>(new uint[]{0x5ae67c,0x4e3818,0x4e42d0,0x4ae830,0x4fbdf4,0x4ae83c,0x4ae836,0x5ae678,0x4b47a0,0x55e800,0x4e3814,0x4fb3d8,0x4aa7fc,0x55e810,0x4b47a6,0x4e3868});
  byte[] code=Read(first,(int)(end-first));int count=0;
  for(int at=0;at<=code.Length-4;at++){
   uint value=BitConverter.ToUInt32(code,at);
   if(!globals.Contains(value)&&!(tables&&value>=first&&value<end))continue;
   BitConverter.GetBytes(Address((int)(value-0x400000))).CopyTo(code,at);at+=3;count++;
  }
  if(count!=expected)throw new InvalidDataException("Impact relocation count differs at "+first.ToString("X8")+": "+count);
  Write((int)(first-0x400000),code);
 }
 int I(int offset){return Marshal.ReadInt32(memory,offset);}
 int S(int offset){return Marshal.ReadInt16(memory,offset);}
 void I(int offset,int value){Marshal.WriteInt32(memory,offset,value);}
 void S(int offset,int value){Marshal.WriteInt16(memory,offset,(short)value);}
 void Pointer(int offset,int value){I(offset,unchecked((int)Address(value)));}
 int Relative(int offset){int pointer=I(offset);return pointer==0?-1:unchecked((int)((uint)pointer-Address(0)));}
 void RunImpact(string exe,string destination){
  source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!=Identity)throw new InvalidDataException("Matching Bs.exe and x86 required");
  pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);
  if(imageBase!=0x400000)throw new InvalidDataException("Image base differs");
  Directory.CreateDirectory(destination);
  using(var p=new StreamWriter(Path.Combine(destination,"Native-Damage-Order-Provenance.csv"))){
   p.WriteLine("SourceSHA256,VA,Bytes,SHA256,Role");
   uint[] starts={0x423540,0x423fb0,0x424130,0x419ce0,0x4177a0,0x419270,0x439790,0x439780};
   int[] lengths={0x384,0x17e,0x92,0x531,0x1e1,0x13e,0x2a,10};
   for(int i=0;i<starts.Length;i++)p.WriteLine(Identity+","+starts[i].ToString("X8")+","+lengths[i]+","+Hash(Read(starts[i],lengths[i]))+",Phase-impact-feedback-CRT slice");
  }
  memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try{
   CopyPhase();CopyImpact(0x423fb0,0x42412e,13,false);CopyImpact(0x424130,0x4241c2,3,false);
   CopyImpact(0x419ce0,0x41a211,22,true);CopyImpact(0x4177a0,0x417981,21,true);CopyImpact(0x419270,0x4193ae,18,true);
   CopyPrefix(0x439790,0x2a,2);CopyPrefix(0x439780,10,1);
   byte[] rand=new byte[0x2a],srand=new byte[10];Marshal.Copy(IntPtr.Add(memory,0x39790),rand,0,rand.Length);Marshal.Copy(IntPtr.Add(memory,0x39780),srand,0,srand.Length);Write(0x1000,rand);Write(0x1100,srand);
   int probe=0,order=0,draws=0,movieId=0,movies=0,messages=0,terminal=-1;
   Select nativeRandom=null;
   using(var trace=new StreamWriter(Path.Combine(destination,"Native-Damage-Order-Events.csv")))
   using(var cases=new StreamWriter(Path.Combine(destination,"Native-Damage-Order-Cases.csv"))){
    trace.WriteLine("Probe,Order,Kind,CallerVA,Value,StateAfter,Phase,HP,Cell,Outcome");
    cases.WriteLine("Probe,Case,Level,Seed,ShipType,Length,Weapon,HPBefore,HPAfter,CellBefore,CellAfter,Outcome,OwnHit,OpponentSentinel,Movie,Requests,RandomCalls,FinalState,TypeAfter,SlotAfter,ActiveSlot,ShooterAfter,Winner,Messages,PhaseAfter,SunkType");
    Action<string,uint,int> record=delegate(string kind,uint caller,int value){trace.WriteLine(probe+","+(order++)+","+kind+","+caller.ToString("X8")+","+value+","+unchecked((uint)I(0x54d50))+","+S(0xb4794)+","+S(TargetShip+16)+","+I(Grid+(6*10+3)*4)+","+S(0x1ae67c));};
    RandomCall random=delegate(int ret){int value=nativeRandom();draws++;uint caller=unchecked((uint)ret-Address(0)+0x400000-5);record("rand",caller,value);return value;};
    var wrapper=new List<byte>{0xff,0x34,0x24,0xb8};wrapper.AddRange(BitConverter.GetBytes(Marshal.GetFunctionPointerForDelegate(random).ToInt32()));wrapper.AddRange(new byte[]{0xff,0xd0,0x83,0xc4,4,0xc3});Write(0x39790,wrapper.ToArray());
    Movie movie=delegate(int id,IntPtr owner){movieId=id;movies++;record("MovieStub",0,id);return 0;};
    Message message=delegate(int id,int owner,int x,int y){messages++;record("MessageStub",0,id&65535);};
    Terminal end=delegate(int winner){terminal=winner&65535;record("TerminalStub",0,terminal);};
    Stub(0x2460,movie);Stub(0x2ad0,movie);Stub(0x20550,message);Stub(0x2e5d0,end);
    Write(0x2980,new byte[]{0x31,0xc0,0xc3});Write(0x2d20,new byte[]{0x31,0xc0,0xc3});
    foreach(int page in new[]{0x1000,0x23000,0x24000,0x19000,0x1a000,0x17000,0x2000,0x20000,0x2e000,0x39000}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");}
    nativeRandom=(Select)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1000),typeof(Select));
    var seed=(Seed)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1100),typeof(Seed));
    var advance=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x23540),typeof(Call));
    int[] types={7,1,2,12,4},lengths={5,5,3,4,2};
    foreach(uint inputSeed in new uint[]{0,1,731,uint.MaxValue})
    for(int level=0;level<4;level++)for(int index=0;index<types.Length;index++)foreach(int weapon in new[]{14,20,26,29})
    foreach(string name in new[]{"miss","repeat-miss","first-hit","later-hit","sink","sink-final","repeat-hit","repeat-sunk"}){
     int length=lengths[index],type=types[index];if(name=="later-hit"&&length==2)continue;
     Write(Fleet,new byte[492]);Write(TargetShip,new byte[64]);Write(GuardShip,new byte[64]);Write(Viewer,new byte[16]);
     I(Fleet,1);S(Fleet+4,0);S(Fleet+6,0);Pointer(Fleet+16,TargetShip);
     bool water=name=="miss"||name=="repeat-miss",removed=name=="repeat-sunk",last=name=="sink-final";
     int sx=water?5:6,hp=name=="sink"||last?1:name=="later-hit"||name=="repeat-hit"?length-1:removed?0:length;
     I(TargetShip,removed?0:type);S(TargetShip+6,0);S(TargetShip+8,sx);S(TargetShip+10,2);S(TargetShip+12,0);S(TargetShip+14,length);S(TargetShip+16,hp);
     for(int y=2;y<2+length;y++)I(Grid+(sx*10+y)*4,removed?8:2);
     if(name=="later-hit")I(Grid+(6*10+2)*4,8);
     if(name=="sink"||last)for(int y=2;y<2+length;y++)if(y!=3)I(Grid+(6*10+y)*4,8);
     if(name=="repeat-hit")I(Grid+(6*10+3)*4,8);
     if(name=="repeat-miss")I(Grid+(6*10+3)*4,1);
     if(!last){I(GuardShip,4);S(GuardShip+6,0);S(GuardShip+8,8);S(GuardShip+10,0);S(GuardShip+12,0);S(GuardShip+14,2);S(GuardShip+16,2);Pointer(Fleet+20,GuardShip);}
     if(removed){I(Fleet+16,0);Marshal.WriteByte(memory,Fleet+0x1d4,1);Pointer(0xe3814,GuardShip);}else Pointer(0xe3814,TargetShip);
     Pointer(0x15e800,Viewer);S(Viewer+4,0);I(0xfb3d8,7);I(0xe3868,20);I(0xfbdf4,level);I(0xfb6ec,0);I(0xfb6f4,weapon);
     Pointer(0xfbc4c,Fleet);Pointer(0xe375c,Ship);S(0xae81a,6);S(0xae818,3);S(0xb4794,6);S(0xe42d0,1);S(0xe41ec,0);S(0xe41ee,0);
     S(0xe3818,0x1234);S(0xe381a,0x1234);S(0x1ae67c,0x1234);I(0x1ae678,0);
     int before=I(Grid+(6*10+3)*4);seed(inputSeed);order=draws=movieId=movies=messages=0;terminal=-1;
     record("Begin",0,0);advance();record("Boundary",0,0);
     cases.WriteLine(probe+","+name+","+level+","+inputSeed+","+type+","+length+","+weapon+","+hp+","+S(TargetShip+16)+","+before+","+I(Grid+(6*10+3)*4)+","+S(0x1ae67c)+","+S(0xe3818)+","+S(0xe381a)+","+movieId+","+movies+","+draws+","+unchecked((uint)I(0x54d50))+","+I(TargetShip)+","+Relative(Fleet+16)+","+Marshal.ReadByte(memory,Fleet+0x1d4)+","+Relative(0xe3814)+","+terminal+","+messages+","+S(0xb4794)+","+I(0x1ae678));
     cases.Flush();trace.Flush();probe++;
    }
    GC.KeepAlive(random);GC.KeepAlive(movie);GC.KeepAlive(message);GC.KeepAlive(end);
   }
   GC.KeepAlive(nativeRandom);Console.WriteLine("Captured "+probe+" bounded original phase/impact/feedback/cleanup cases; media/messages/terminal endpoints are recording stubs.");
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){try{if(args.Length!=2)return 2;new ClassicDamageOrderOracle().RunImpact(args[0],args[1]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
