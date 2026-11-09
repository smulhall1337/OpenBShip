using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// Prepared, coherent five-ship fleets BEFORE original target selection.
// Executes original opponent/target/phase/impact/selectors/cleanup on one CRT.
// Playback, messages, views and terminal lifecycle remain recording endpoints.
class ClassicCombinedShotOracle : ClassicDamageOrderOracle {
 [DllImport("kernel32.dll")]static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint flags,uint protection);
 [DllImport("kernel32.dll")]static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint protection,out uint old);
 [DllImport("kernel32.dll")]static extern bool VirtualFree(IntPtr p,UIntPtr n,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Call();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Seed(uint value);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Select();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int RandomCall(int ret);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Target(IntPtr x,IntPtr y);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int Movie(int id,IntPtr owner);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int View(int id);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Message(int id,int owner,int x,int y);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void Terminal(int winner);
 const int PlayerFleet=0xb47a0,EnemyFleet=PlayerFleet+492,PlayerShips=0x1f0000,EnemyShips=0x1e0000,Viewer=0x1f1000;
 static readonly int[] Types={7,1,2,12,4},Lengths={5,5,3,4,2};
 int I(int p){return Marshal.ReadInt32(memory,p);}void I(int p,int v){Marshal.WriteInt32(memory,p,v);}
 int S(int p){return Marshal.ReadInt16(memory,p);}void S(int p,int v){Marshal.WriteInt16(memory,p,(short)v);}
 void Pointer(int p,int target){I(p,unchecked((int)Address(target)));}
 uint State{get{return unchecked((uint)I(0x54d50));}}
 int Cell(int x,int y){return I(Grid+(x*10+y)*4);}
 string Cells(){var v=new string[100];for(int i=0;i<100;i++)v[i]=I(Grid+i*4).ToString();return string.Join(";",v);}
 string HP(){var v=new string[5];for(int i=0;i<5;i++)v[i]=S(PlayerShips+i*256+16).ToString();return string.Join(";",v);}
 int Alive(){int bits=0;for(int i=0;i<5;i++)if(I(PlayerFleet+16+i*4)!=0)bits|=1<<i;return bits;}
 void Hit(int slot,int y){int ship=PlayerShips+slot*256,x=slot*2;I(Grid+(x*10+y)*4,8);S(ship+16,S(ship+16)-1);}
 void Sink(int slot){for(int y=2;y<2+Lengths[slot];y++)Hit(slot,y);I(PlayerShips+slot*256,0);I(PlayerFleet+16+slot*4,0);}
 void Prepare(string name,int shooter){
  Write(PlayerFleet,new byte[984]);Write(PlayerShips,new byte[5*256]);Write(EnemyShips,new byte[5*256]);Write(Viewer,new byte[16]);
  foreach(int fleet in new[]{PlayerFleet,EnemyFleet}){int side=fleet==PlayerFleet?0:1;I(fleet,1);S(fleet+4,side);S(fleet+6,side);
   for(int slot=0;slot<5;slot++){int ship=(side==0?PlayerShips:EnemyShips)+slot*256;Pointer(fleet+16+slot*4,ship);I(ship,Types[slot]);S(ship+6,side);S(ship+8,slot*2+side);S(ship+10,2);S(ship+12,side);S(ship+14,Lengths[slot]);S(ship+16,Lengths[slot]);
    for(int y=2;y<2+Lengths[slot];y++)I(fleet+64+((slot*2+side)*10+y)*4,2);
   }
  }
  if(name=="wounded")Hit(1,3);
  else if(name=="line"){Hit(1,3);Hit(1,4);}
  else if(name=="finish"){Hit(4,2);I(Grid+(8*10+1)*4,1);}
  else if(name=="finish-active"){for(int y=2;y<6;y++)Hit(0,y);I(Grid+4,1);}
  else if(name=="sunk-first"){Sink(0);I(Grid+4,1);I(Grid+7*4,1);}
  else if(name=="crowded"||name=="last-hit"){
   for(int slot=0;slot<4;slot++)Sink(slot);
   for(int i=0;i<100;i++)if(I(Grid+i*4)==0)I(Grid+i*4,1);
   if(name=="last-hit")Hit(4,2);
  }else if(name!="fresh")throw new InvalidDataException("Unknown board fixture");
  int active=0;while(I(PlayerFleet+16+active*4)==0)active++;
  Marshal.WriteByte(memory,PlayerFleet+0x1d4,(byte)active);Marshal.WriteByte(memory,EnemyFleet+0x1d4,(byte)shooter);
  Pointer(0xe3814,PlayerShips+active*256);Pointer(0x15e800,Viewer);S(Viewer+4,0);
 }
 void RunCombined(string exe,string destination){
  source=File.ReadAllBytes(exe);if(IntPtr.Size!=4||Hash(source)!=Identity)throw new InvalidDataException("Matching original and x86 required");pe=BitConverter.ToInt32(source,60);imageBase=BitConverter.ToUInt32(source,pe+52);if(imageBase!=0x400000)throw new InvalidDataException("Image base differs");
  Directory.CreateDirectory(destination);
  using(var p=new StreamWriter(Path.Combine(destination,"Native-Combined-Shot-Provenance.csv"))){
   p.WriteLine("SourceSHA256,VA,Bytes,SHA256");uint[] starts={0x41c8c0,0x416640,0x41c940,0x423540,0x423fb0,0x424130,0x419ce0,0x4177a0,0x419270,0x439790,0x439780,0x44d730};int[] sizes={0x7b,0x3b2,0x348,0x384,0x17e,0x92,0x531,0x1e1,0x13e,0x2a,10,0x48};
   for(int i=0;i<starts.Length;i++)p.WriteLine(Identity+","+starts[i].ToString("X8")+","+sizes[i]+","+Hash(Read(starts[i],sizes[i])));
  }
  memory=VirtualAlloc(IntPtr.Zero,new UIntPtr(0x200000),0x3000,4);if(memory==IntPtr.Zero)throw new Exception("Allocation failed");
  try{
   CopyPrefix(0x41c8c0,0x7b,11);CopyPrefix(0x416640,0x3b2,24);CopyTarget();CopyPhase();
   CopyImpact(0x423fb0,0x42412e,13,false);CopyImpact(0x424130,0x4241c2,3,false);CopyImpact(0x419ce0,0x41a211,22,true);CopyImpact(0x4177a0,0x417981,21,true);CopyImpact(0x419270,0x4193ae,18,true);
   CopyPrefix(0x439790,0x2a,2);CopyPrefix(0x439780,10,1);Write(0x4d730,Read(0x44d730,0x48));
   byte[] rand=new byte[0x2a],srand=new byte[10];Marshal.Copy(IntPtr.Add(memory,0x39790),rand,0,rand.Length);Marshal.Copy(IntPtr.Add(memory,0x39780),srand,0,srand.Length);Write(0x1000,rand);Write(0x1100,srand);
   Select nativeRandom=null;Target nativeTarget=null;
   int probe=0,order=0,draws=0,prefixCalls=0,firing=0,travel=0,feedback=0,winner=-1,requests=0,unavailable=0,x=-1,y=-1;
   using(var events=new StreamWriter(Path.Combine(destination,"Native-Combined-Shot-Events.csv")))
   using(var cases=new StreamWriter(Path.Combine(destination,"Native-Combined-Shot-Cases.csv"))){
    events.WriteLine("Probe,Order,Kind,CallerVA,Value,StateAfter,Phase,Side,X,Y,HP,Cell,Outcome,Alive");
    cases.WriteLine("Probe,Case,Seed,Level,ShooterSlot,Unavailable,Weapon,FiringMovie,TravelMovie,FeedbackMovie,X,Y,CallsAtTarget,FinalCalls,FinalState,Outcome,Winner,PhaseAfter,SideAfter,AliveBefore,AliveAfter,ActiveBefore,ActiveAfter,MovieRequests,HPBefore,HPAfter,GridBefore,GridAfter");
    Action<string,uint,int> record=delegate(string kind,uint caller,int value){events.WriteLine(probe+","+(order++)+","+kind+","+caller.ToString("X8")+","+value+","+State+","+S(0xb4794)+","+S(0xe42d0)+","+x+","+y+","+HP()+","+(x<0?-1:Cell(x,y))+","+S(0x1ae67c)+","+Alive());};
    RandomCall random=delegate(int ret){int value=nativeRandom();draws++;uint offset=unchecked((uint)ret-Address(0)-5);uint caller=offset>=TargetCode&&offset<TargetCode+0x348?0x41c940+offset-TargetCode:0x400000+offset;record("rand",caller,value);return value;};
    var wrapper=new List<byte>{0xff,0x34,0x24,0xb8};wrapper.AddRange(BitConverter.GetBytes(Marshal.GetFunctionPointerForDelegate(random).ToInt32()));wrapper.AddRange(new byte[]{0xff,0xd0,0x83,0xc4,4,0xc3});Write(0x39790,wrapper.ToArray());
    Target target=delegate(IntPtr px,IntPtr py){record("TargetEnter",0x41c91b,I(0xfb6f4));nativeTarget(px,py);x=Marshal.ReadInt16(px);y=Marshal.ReadInt16(py);prefixCalls=draws;record("TargetReturn",0x41c91b,0);};
    Movie movie=delegate(int id,IntPtr owner){int phase=S(0xb4794);string kind=phase==4?"FiringMovie":phase==6?"TravelMovie":"FeedbackMovie";if(phase==4)firing=id;else if(phase==6)travel=id;else feedback=id;requests++;record(kind,0,id);int result=phase==6?unavailable:0;record("AdmissionReturn",0,result);return result;};
    View view=delegate(int id){record("ViewStub",0,id);return 0;};
    Message message=delegate(int id,int owner,int px,int py){record("MessageStub",0,id&65535);};
    Terminal terminal=delegate(int id){winner=id&65535;record("TerminalStub",0,winner);};
    Stub(0x1c940,target);Stub(0x2460,movie);Stub(0x2ad0,movie);Stub(0x12d0,view);Stub(0x20550,message);Stub(0x2e5d0,terminal);Write(0x2980,new byte[]{0x31,0xc0,0xc3});Write(0x2d20,new byte[]{0x31,0xc0,0xc3});
    foreach(int page in new[]{0x1000,0x16000,0x17000,0x19000,0x1a000,0x1c000,0x23000,0x24000,0x39000,0x2000,0x20000,0x2e000,TargetCode}){uint old;if(!VirtualProtect(IntPtr.Add(memory,page),new UIntPtr(4096),0x20,out old))throw new Exception("Code protection failed");}
    nativeRandom=(Select)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1000),typeof(Select));nativeTarget=(Target)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,TargetCode),typeof(Target));
    var seed=(Seed)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1100),typeof(Seed));var opponent=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x1c8c0),typeof(Call));var advance=(Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory,0x23540),typeof(Call));
    foreach(uint input in new uint[]{0,1,731,uint.MaxValue})for(int level=0;level<4;level++)for(int shooter=0;shooter<5;shooter++)foreach(string name in new[]{"fresh","wounded","line","finish","finish-active","sunk-first","crowded","last-hit"})for(unavailable=0;unavailable<2;unavailable++){
     Prepare(name,shooter);seed(input);order=draws=prefixCalls=firing=travel=feedback=requests=0;winner=x=y=-1;
     I(0xfb3d8,7);I(0xe3868,20);I(0xfbdf4,level);I(0xfb6ec,0);I(0xfb6f4,0);S(0xb4794,4);S(0xe42d0,0);S(0xe41ec,0);S(0xe41ee,0);S(0xe3818,0x1234);S(0xe381a,0x1234);S(0x1ae67c,0x1234);I(0x1ae678,0);
     string hpBefore=HP(),gridBefore=Cells();int aliveBefore=Alive(),activeBefore=Marshal.ReadByte(memory,PlayerFleet+0x1d4);
     record("Begin",0,0);opponent();record("PrefixReturn",0,0);
     record("FiringCompletionHarness",0,0);advance();record("FiringCompletionReturn",0,0);
     if(unavailable==0){record("TravelCompletionHarness",0,0);advance();record("TravelCompletionReturn",0,0);}
     if(winner<0){record("PostImpactHarness",0,0);advance();}
     record("Boundary",0,0);
     cases.WriteLine(probe+","+name+","+input+","+level+","+shooter+","+unavailable+","+I(0xfb6f4)+","+firing+","+travel+","+feedback+","+x+","+y+","+prefixCalls+","+draws+","+State+","+S(0x1ae67c)+","+winner+","+S(0xb4794)+","+S(0xe42d0)+","+aliveBefore+","+Alive()+","+activeBefore+","+Marshal.ReadByte(memory,PlayerFleet+0x1d4)+","+requests+","+hpBefore+","+HP()+","+gridBefore+","+Cells());
     cases.Flush();events.Flush();probe++;
    }
    GC.KeepAlive(random);GC.KeepAlive(target);GC.KeepAlive(movie);GC.KeepAlive(view);GC.KeepAlive(message);GC.KeepAlive(terminal);
   }
   GC.KeepAlive(nativeRandom);GC.KeepAlive(nativeTarget);Console.WriteLine("Captured "+probe+" coherent-fleet native combined shots. Completion boundaries and audiovisual/terminal endpoints remain controlled.");
  }finally{VirtualFree(memory,UIntPtr.Zero,0x8000);}
 }
 static int Main(string[] args){try{if(args.Length!=2)return 2;new ClassicCombinedShotOracle().RunCombined(args[0],args[1]);return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
