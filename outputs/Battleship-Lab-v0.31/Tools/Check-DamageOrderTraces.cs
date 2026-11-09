using System;using System.IO;using System.Drawing;using System.Collections.Generic;using FleetLab;
class CheckDamageOrderTraces {
 static void Need(bool ok,string why){if(!ok)throw new InvalidDataException(why);}
 static int N(string[] c,int at){return int.Parse(c[at]);}
 static int Main(string[] args){try{
  if(args.Length<3||args.Length>4)return 2;bool baseline=args.Length==4&&args[3]=="baseline";
  var cases=new Dictionary<int,string[]>();var expected=new Dictionary<int,List<string>>();
  var differences=new List<string>{"Probe,Case,Level,Seed,ShipType,Weapon,NativeMovie,ManagedMovie,NativeDraws,ManagedDraws"};
  int total=0,diff=0;var diffKinds=new HashSet<string>();
  foreach(string line in File.ReadAllLines(args[0])){if(line.StartsWith("Probe,"))continue;string[] c=line.Split(',');Need(c.Length==26,"Case columns");int id=N(c,0);Need(!cases.ContainsKey(id),"Duplicate case");cases.Add(id,c);total++;
   string kind=c[1];int level=N(c,2),type=N(c,4),length=N(c,5),weapon=N(c,6);uint seed=uint.Parse(c[3]);
   bool miss=kind=="miss"||kind=="repeat-miss"||kind=="repeat-sunk",sink=kind=="sink"||kind=="sink-final",hit=kind=="first-hit"||kind=="later-hit",removed=sink||kind=="repeat-sunk";
   var board=new Board();Need(board.Place(new Point(kind=="miss"||kind=="repeat-miss"?5:6,2),length,true,"Target",type),"Fixture target");
   if(kind!="sink-final")Need(board.Place(new Point(8,0),2,true,"Survivor",4),"Fixture survivor");
   Point target=new Point(6,3);Ship ship=board.Ships[0];
   if(kind=="repeat-sunk"){foreach(Point p in ship.Cells)board.Shoot(p);}
   else if(sink){foreach(Point p in ship.Cells)if(p!=target)board.Shoot(p);}
   else if(kind=="later-hit")board.Shoot(new Point(6,2));
   else if(kind=="repeat-hit"||kind=="repeat-miss")board.Shoot(target);
   Need(N(c,7)==length-ship.Hits,"HP fixture differs: "+id+" "+kind+" native="+N(c,7)+" managed="+(length-ship.Hits));
   Shot shot=board.Shoot(target);if(shot==null)shot=new Shot{Cell=target,Repeated=true};
   Ship impacted=board.ShipAt(target);int hp=length-ship.Hits,outcome=sink?2:hit?1:0;
   Need(N(c,8)==hp&&N(c,11)==outcome&&N(c,12)==(hit||sink?1:0),"Native HP/outcome differs: "+id);
   Need(N(c,10)==(kind=="miss"||kind=="repeat-miss"?1:8)&&N(c,13)==-1&&N(c,24)==7,"Native cell/sentinel/phase differs");
   Need(N(c,18)==(removed?0:type)&&N(c,19)==(removed?-1:0x1f0000),"Native removal differs");
   Need(N(c,20)==((kind=="sink"||kind=="repeat-sunk")?1:0),"Native active slot differs");
   Need(N(c,21)==((kind=="sink"||kind=="repeat-sunk")?0x1f0100:0x1f0000),"Native selected shooter differs");
   Need(N(c,22)==(kind=="sink-final"?1:-1)&&N(c,25)==(sink?type:0),"Native terminal/type capture differs");
   bool nativeDraw=miss&&level==0&&(weapon==14||weapon==26);var nativeRng=new NativeRandom(seed);int roll=nativeDraw?nativeRng.Next():0;
   int nativeMovie=kind=="repeat-hit"?0:sink?ClassicImpact.Sink(type):level>0?(hit?137:136):hit?ClassicImpact.Hit(type,length,hp,weapon):ClassicImpact.Miss(weapon,roll);
   Need(N(c,14)==nativeMovie&&N(c,15)==(nativeMovie==0?0:1)&&N(c,16)==nativeRng.Calls&&uint.Parse(c[17])==nativeRng.State,"Native feedback/random differs: "+id);
   Need(N(c,23)==(miss||sink?1:0),"Native message count differs");
   var managedRng=new NativeRandom(seed);bool needs;
   var method=typeof(ClassicImpact).GetMethod("NeedsRandom",new Type[]{typeof(int),typeof(Shot),typeof(Ship),typeof(int)});
   if(method!=null)needs=(bool)method.Invoke(null,new object[]{level,shot,impacted,weapon});
   else needs=level==0&&!shot.Hit&&(weapon>=14&&weapon<=18||weapon==25||weapon==26); // Delivered v0.30 Game.CompleteMovie.
   int managedMovie=ClassicImpact.Select(level,shot,impacted,weapon,needs?managedRng.Next():0);
   if(managedMovie!=nativeMovie||managedRng.Calls!=nativeRng.Calls||managedRng.State!=nativeRng.State){diff++;diffKinds.Add(kind);differences.Add(id+","+kind+","+level+","+seed+","+type+","+weapon+","+nativeMovie+","+managedMovie+","+nativeRng.Calls+","+managedRng.Calls);}
   var events=new List<string>{"Begin"};if(miss||sink)events.Add("MessageStub");if(nativeDraw)events.Add("rand");if(nativeMovie!=0)events.Add("MovieStub");if(kind=="sink-final")events.Add("TerminalStub");events.Add("Boundary");expected.Add(id,events);
  }
  Need(total==2496,"Coverage count differs");int previous=-1,index=0,draws=0;uint state=0;
  foreach(string line in File.ReadLines(args[1])){if(line.StartsWith("Probe,"))continue;string[] r=line.Split(',');Need(r.Length==10,"Event columns");int id=N(r,0);if(id!=previous){Need(previous<0||index==expected[previous].Count,"Truncated probe");Need(id==previous+1&&cases.ContainsKey(id),"Event order");previous=id;index=0;state=uint.Parse(cases[id][3]);}
   var c=cases[id];string kind=c[1],eventKind=r[2];Need(index<expected[id].Count&&N(r,1)==index&&eventKind==expected[id][index++],"Native callback ordering differs: "+id);
   bool begin=eventKind=="Begin",boundary=eventKind=="Boundary",miss=kind=="miss"||kind=="repeat-miss"||kind=="repeat-sunk",sink=kind=="sink"||kind=="sink-final";
   Need(N(r,6)==(begin?6:7)&&N(r,7)==(begin?N(c,7):N(c,8))&&N(r,9)==(begin?0x1234:N(c,11)),"Native callback HP/outcome timing differs");
   int cell=begin?N(c,9):boundary?N(c,10):miss?N(c,10):N(c,9);Need(N(r,8)==cell,"Native cell write ordering differs");
   if(eventKind=="rand"){Need(r[3]==(N(c,6)==14?"004177FA":"0041787A"),"Miss RNG site differs");state=unchecked(state*214013u+2531011u);Need(N(r,4)==(int)((state>>16)&32767),"CRT value differs");draws++;}
   else if(eventKind=="MovieStub")Need(N(r,4)==N(c,14),"Movie event differs");
   else if(eventKind=="MessageStub")Need(N(r,4)==(sink?N(c,4):27),"Message event differs");
   else if(eventKind=="TerminalStub")Need(N(r,4)==1,"Winner event differs");
   Need(uint.Parse(r[5])==state,"CRT event state differs");
  }
  Need(previous==2495&&index==expected[previous].Count&&draws==120,"Native history/coverage incomplete");
  if(baseline)Need(diff==360&&diffKinds.SetEquals(new string[]{"repeat-hit","repeat-sunk"}),"Expected v0.30 baseline differences changed");else Need(diff==0,"Delivered feedback differs in "+diff+" cases");
  File.WriteAllLines(Path.Combine(Path.GetDirectoryName(args[2]),"Managed-Differences.csv"),differences.ToArray());
  string report="PASS 2,496 bounded original phase/impact/feedback/cleanup cases; HP, cells, removal, active slot, terminal endpoint and callback write ordering agree.\r\nPASS 120 actual native miss-variant CRT draws and states.\r\n"+(baseline?"EXPECTED 360 v0.30 divergences: repeated-live-hit random consumption and repeated-sunk-cell feedback.\r\n":"PASS delivered feedback/random consumption agrees in all 2,496 cases.\r\n")+"Scope: synthetic valid-axis fixtures enter phase 6; media/message/terminal callbacks are stubs. Firing/target/travel initialization, complete battle lifecycle and audiovisual acceptance are outside this comparison.\r\n";
  File.WriteAllText(args[2],report);Console.WriteLine(report);return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}

