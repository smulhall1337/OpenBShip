using System;using System.IO;using System.Drawing;using System.Collections.Generic;using FleetLab;
// Independent prepared-board construction and pure-selector replay. The primary
// Game still uses separate streams; this is an explicitly shared-stream probe.
class CheckCombinedShotTraces {
 class Event {public string Kind,Caller,HP;public int Value,Phase,Side,X,Y,Cell,Outcome,Alive;}
 static void Need(bool ok,string why){if(!ok)throw new InvalidDataException(why);}
 static int N(string[] c,int i){return int.Parse(c[i]);}
 static Board Prepare(string name){
  var board=new Board();int[] types={7,1,2,12,4},lengths={5,5,3,4,2};
  for(int i=0;i<5;i++)Need(board.Place(new Point(i*2,2),lengths[i],true,"Ship"+i,types[i]),"Independent legal-axis placement");
  Action<int> sink=delegate(int slot){foreach(Point p in board.Ships[slot].Cells)board.Shoot(p);};
  if(name=="wounded")board.Shoot(new Point(2,3));
  else if(name=="line"){board.Shoot(new Point(2,3));board.Shoot(new Point(2,4));}
  else if(name=="finish"){board.Shoot(new Point(8,2));board.Shoot(new Point(8,1));}
  else if(name=="finish-active"){for(int y=2;y<6;y++)board.Shoot(new Point(0,y));board.Shoot(new Point(0,1));}
  else if(name=="sunk-first"){sink(0);board.Shoot(new Point(0,1));board.Shoot(new Point(0,7));}
  else if(name=="crowded"||name=="last-hit"){
   for(int i=0;i<4;i++)sink(i);
   for(int x=0;x<10;x++)for(int y=0;y<10;y++)if(board.ShipAt(new Point(x,y))==null)board.Shoot(new Point(x,y));
   if(name=="last-hit")board.Shoot(new Point(8,2));
  }else Need(name=="fresh","Unknown board fixture");
  return board;
 }
 static string Raw(Board b){var values=new List<string>();for(int x=0;x<10;x++)for(int y=0;y<10;y++){bool ship=b.ShipAt(new Point(x,y))!=null;values.Add((b.Shots[x,y]?(ship?8:1):(ship?2:0)).ToString());}return string.Join(";",values.ToArray());}
 static string HP(Board b){var values=new List<string>();foreach(Ship ship in b.Ships)values.Add((ship.Cells.Count-ship.Hits).ToString());return string.Join(";",values.ToArray());}
 static int Alive(Board b){int mask=0;for(int i=0;i<b.Ships.Count;i++)if(!b.Ships[i].Sunk)mask|=1<<i;return mask;}
 static int FirstAlive(Board b){for(int i=0;i<b.Ships.Count;i++)if(!b.Ships[i].Sunk)return i;return -1;}
 static List<Event> Expected(string[] c){
  Board board=Prepare(c[1]);int aliveBefore=Alive(board),activeBefore=FirstAlive(board);string hpBefore=HP(board),gridBefore=Raw(board);
  Need(c[24]==hpBefore&&c[26]==gridBefore&&N(c,19)==aliveBefore&&N(c,21)==activeBefore,"Native starting fleet differs at "+c[0]);
  int[] types={7,1,2,12,4};int shooter=N(c,4),level=N(c,3),unavailable=N(c,5);uint seed=uint.Parse(c[2]);Need(shooter>=0&&shooter<5&&unavailable>=0&&unavailable<=1,"Input bounds");
  var enemy=new Board();int[] lengths={5,5,3,4,2};Need(enemy.Place(new Point(0,2),lengths[shooter],true,"Shooter",types[shooter]),"Shooter fixture");
  var rng=new NativeRandom(seed);FiringPresentation fire=ClassicWeapons.Choose(enemy.Ships[0],true,rng);
  var raw=new int[10,10];string[] values=gridBefore.Split(';');for(int i=0;i<100;i++)raw[i/10,i%10]=int.Parse(values[i]);
  TargetDecision target=OriginalOpponent.Choose(raw,level,rng);Need(!target.Stalled,"Invalid isolated-hit trap fixture");int prefixCalls=rng.Calls;
  int travel=ClassicSequence.Travel(fire.WeaponId,fire.WeaponId>=14&&fire.WeaponId<=18?rng.Next():0);
  int travelCalls=rng.Calls-prefixCalls;Point cell=target.Cell;int cellBefore=raw[cell.X,cell.Y];
  Shot shot=board.Shoot(cell);if(shot==null)shot=new Shot{Cell=cell,Repeated=true};Ship impacted=board.ShipAt(cell);
  bool needs=ClassicImpact.NeedsRandom(level,shot,impacted,fire.WeaponId);int feedback=ClassicImpact.Select(level,shot,impacted,fire.WeaponId,needs?rng.Next():0);
  int outcome=shot.Sunk?2:shot.Hit?1:0,winner=board.AllSunk?1:-1,aliveAfter=Alive(board),activeAfter=FirstAlive(board);if(activeAfter<0)activeAfter=activeBefore;
  Need(N(c,6)==fire.WeaponId&&N(c,7)==fire.MovieId&&N(c,8)==travel&&N(c,9)==feedback,"Combined presentation differs at "+c[0]);
  Need(N(c,10)==cell.X&&N(c,11)==cell.Y&&N(c,12)==prefixCalls&&N(c,13)==rng.Calls&&uint.Parse(c[14])==rng.State,"Combined targeting/RNG differs at "+c[0]);
  Need(N(c,15)==outcome&&N(c,16)==winner&&N(c,17)==(winner<0?0:7)&&N(c,18)==(winner<0?0:1),"Combined outcome/terminal boundary differs");
  Need(N(c,20)==aliveAfter&&N(c,22)==activeAfter&&N(c,23)==2+(feedback!=0?1:0),"Combined roster/request result differs");
  Need(c[25]==HP(board)&&c[27]==Raw(board),"Native complete HP/grid delta differs");
  var events=new List<Event>();int x=-1,y=-1,phase=4,side=0,observedCell=-1,flag=0x1234,alive=aliveBefore;string hp=hpBefore;var replay=new NativeRandom(seed);
  Action<string,string,int> add=delegate(string kind,string caller,int value){events.Add(new Event{Kind=kind,Caller=caller,Value=value,Phase=phase,Side=side,X=x,Y=y,HP=hp,Cell=observedCell,Outcome=flag,Alive=alive});};
  add("Begin","00000000",0);side=1;
  add("rand","0041C8D9",replay.Next());add("rand","dispatcher",replay.Next());
  add("FiringMovie","00000000",fire.MovieId);add("AdmissionReturn","00000000",0);add("TargetEnter","0041C91B",fire.WeaponId);
  var targetSites=new List<string>();
  if(target.Reason=="pursue single hit")for(int i=2;i<prefixCalls;i++)targetSites.Add("0041CA94");
  else if(prefixCalls>2){
   var sampled=new NativeRandom(seed);sampled.Next();sampled.Next();int[,] masked=OriginalOpponent.Mask(raw);
   while(targetSites.Count<prefixCalls-2){int sx=sampled.Next()%10,sy=sampled.Next()%8+1;targetSites.Add("0041CB4B");targetSites.Add("0041CB5B");if(masked[sx,sy]==0){sampled.Next();targetSites.Add("0041CB8D");}}
  }
  Need(targetSites.Count==prefixCalls-2,"Target call-site history length");
  foreach(string site in targetSites)add("rand",site,replay.Next());
  x=cell.X;y=cell.Y;observedCell=cellBefore;add("TargetReturn","0041C91B",0);phase=5;add("ViewStub","00000000",2);add("PrefixReturn","00000000",0);add("FiringCompletionHarness","00000000",0);
  phase=6;if(travelCalls!=0)add("rand","0042378D",replay.Next());add("TravelMovie","00000000",travel);add("AdmissionReturn","00000000",unavailable);
  Action impact=delegate{
   phase=7;hp=HP(board);flag=outcome;
   bool miss=!shot.Hit;bool repeatedLive=shot.Repeated&&impacted!=null&&!impacted.Sunk;
   if(miss&&!repeatedLive){observedCell=int.Parse(Raw(board).Split(';')[x*10+y]);add("MessageStub","00000000",27);}
   if(shot.Sunk)add("MessageStub","00000000",impacted.SourceType);
   if(needs)add("rand",fire.WeaponId>=14&&fire.WeaponId<=18?"004177FA":"0041787A",replay.Next());
   if(feedback!=0){add("FeedbackMovie","00000000",feedback);add("AdmissionReturn","00000000",0);}
   alive=aliveAfter;if(winner>=0)add("TerminalStub","00000000",winner);
   observedCell=int.Parse(Raw(board).Split(';')[x*10+y]);
  };
  if(unavailable!=0)impact();add("FiringCompletionReturn","00000000",0);
  if(unavailable==0){add("TravelCompletionHarness","00000000",0);impact();add("TravelCompletionReturn","00000000",0);}
  if(winner<0){add("PostImpactHarness","00000000",0);add("ViewStub","00000000",1);phase=0;side=0;}
  add("Boundary","00000000",0);Need(replay.State==rng.State&&replay.Calls==rng.Calls,"Expected replay mismatch");return events;
 }
 static int Main(string[] args){try{
  if(args.Length!=3)return 2;var cases=new Dictionary<int,string[]>();var inputs=new HashSet<string>();var shapes=new HashSet<string>();var seeds=new HashSet<uint>();var levels=new HashSet<int>();var shooters=new HashSet<int>();var admissions=new HashSet<int>();
  foreach(string line in File.ReadAllLines(args[0])){if(line.StartsWith("Probe,"))continue;string[] c=line.Split(',');Need(c.Length==28,"Case columns");int id=N(c,0);Need(!cases.ContainsKey(id),"Duplicate case");cases.Add(id,c);Need(inputs.Add(string.Join(",",c,1,5)),"Duplicate input combination");shapes.Add(c[1]);seeds.Add(uint.Parse(c[2]));levels.Add(N(c,3));shooters.Add(N(c,4));admissions.Add(N(c,5));}
  int previous=-1,index=0,draws=0,targets=0,travels=0,feedbackDraws=0,hits=0,sinks=0,misses=0,repeats=0,terminals=0;List<Event> expected=null;uint state=0;
  foreach(string line in File.ReadLines(args[1])){if(line.StartsWith("Probe,"))continue;string[] r=line.Split(',');Need(r.Length==14,"Event columns");int id=N(r,0);
   if(id!=previous){Need(expected==null||index==expected.Count,"Previous trace truncated");Need(id==previous+1&&cases.ContainsKey(id),"Missing/out-of-order probe");previous=id;index=0;expected=Expected(cases[id]);state=uint.Parse(cases[id][2]);int outcome=N(cases[id],15);if(outcome==0)misses++;else if(outcome==1)hits++;else if(outcome==2)sinks++;else throw new Exception("Invalid outcome");if(N(cases[id],16)>=0)terminals++;
    var c=cases[id];int before=int.Parse(c[26].Split(';')[N(c,10)*10+N(c,11)]);if(before==1||before==8)repeats++;
   }
   Need(index<expected.Count&&N(r,1)==index,"Event ordinal mismatch");Event e=expected[index++];string at="probe "+id+",event "+r[1];
   Need(r[2]==e.Kind&&N(r,4)==e.Value,"Event/value order differs: "+at);
   Need(N(r,6)==e.Phase&&N(r,7)==e.Side&&N(r,8)==e.X&&N(r,9)==e.Y,"Phase/side/coordinates differ: "+at);
   Need(r[10]==e.HP&&N(r,11)==e.Cell&&N(r,12)==e.Outcome&&N(r,13)==e.Alive,"HP/grid/outcome/roster timing differs: "+at);
   if(e.Caller=="dispatcher"){uint va=Convert.ToUInt32(r[3],16);Need(va>=0x416640&&va<0x41698c,"Dispatcher caller leaves span");}
   else Need(r[3]==e.Caller,"Native caller differs: "+at);
   if(e.Caller=="0041CA94"||e.Caller=="0041CB4B"||e.Caller=="0041CB5B"||e.Caller=="0041CB8D")targets++;
   if(e.Kind=="rand"){state=unchecked(state*214013u+2531011u);Need(e.Value==(int)((state>>16)&32767),"CRT draw differs");draws++;if(e.Caller=="0042378D")travels++;if(e.Caller=="004177FA"||e.Caller=="0041787A")feedbackDraws++;}
   Need(uint.Parse(r[5])==state,"Shared CRT state differs: "+at);
  }
  Need(previous==1279&&cases.Count==1280&&index==expected.Count&&shapes.Count==8,"Coverage incomplete");
  Need(seeds.SetEquals(new uint[]{0,1,731,uint.MaxValue})&&levels.SetEquals(new int[]{0,1,2,3})&&shooters.SetEquals(new int[]{0,1,2,3,4})&&admissions.SetEquals(new int[]{0,1}),"Input matrix differs");
  Need(hits>0&&sinks>0&&misses>0&&repeats>0&&terminals==160&&feedbackDraws>0,"Outcome/branch coverage incomplete");
  string report="PASS 1,280 prepared coherent-fleet native opponent shots: 8 board states x4 seeds x4 difficulty levels x5 shooter slots x2 travel-admission paths.\r\n"+
   "PASS shared CRT history: "+draws+" actual draws (target="+targets+",travel="+travels+",feedback="+feedbackDraws+"); every value/state and native caller position agrees.\r\n"+
   "PASS firing, target, travel, damage, feedback, roster cleanup and callback-time HP/grid changes match the delivered pure selectors and independent board model.\r\n"+
   "Outcomes: "+misses+" miss/no-new-hit,"+hits+" surviving hit,"+sinks+" sink; "+repeats+" already-resolved targets; "+terminals+" last-ship terminal endpoint calls.\r\n"+
   "Scope: prepared fleets and seed, manually supplied completion boundaries, stubbed media/view/message/terminal lifecycle. Terminal cases stop at phase7. No original initialization, player-side entry, natural playback/animation or complete game/session is established. Production streams remain independent.\r\n";
  File.WriteAllText(args[2],report);Console.WriteLine(report);return 0;
 }catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
