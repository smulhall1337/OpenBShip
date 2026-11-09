using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab {
    public static class Validation {
        static string Hash(byte[] value){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(value)).Replace("-","");}
        static void Require(bool condition,string message) {
            if(!condition)throw new Exception(message);
        }
        static void Reject(Action action) {
            bool rejected=false;
            try {
                action();
            }
            catch(InvalidDataException) {
                rejected=true;
            }
            Require(rejected,"Malformed input was accepted");
        }
        static void W16(byte[] b,int p,int value) {
            b[p]=(byte)value;
            b[p+1]=(byte)(value>>8);
        }
        static byte[] PcxFixture() {
            byte[] b=new byte[901];
            b[0]=10;
            b[1]=5;
            b[2]=1;
            b[3]=8;
            W16(b,8,1);
            W16(b,10,1);
            b[65]=1;
            W16(b,66,2);
            for(int i=0; i<4; i++)b[128+i]=(byte)(i+1);
            b[132]=12;
            b[133+3]=255;
            b[133+7]=255;
            return b;
        }
        static byte[] CueWaveFixture(){
            byte[] b=new byte[48];Encoding.ASCII.GetBytes("RIFF").CopyTo(b,0);BitConverter.GetBytes(40).CopyTo(b,4);Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(b,8);BitConverter.GetBytes(16).CopyTo(b,16);W16(b,20,1);W16(b,22,1);BitConverter.GetBytes(11025).CopyTo(b,24);BitConverter.GetBytes(11025).CopyTo(b,28);W16(b,32,1);W16(b,34,8);Encoding.ASCII.GetBytes("data").CopyTo(b,36);BitConverter.GetBytes(3).CopyTo(b,40);b[44]=0;b[45]=128;b[46]=255;return b;
        }
        public static int Run(string assets,string report) {
            var output=new List<string>();
            int passed=0,failed=0;
            Action<string,Action> check=delegate(string name,Action action) {
                try {
                    action();
                    passed++;
                    output.Add("PASS "+name);
                }
                catch(Exception e) {
                    failed++;
                    output.Add("FAIL "+name+": "+e.Message);
                }
            };
            check("Independent PCX RLE/color fixture",delegate {
                var p=Assets.Pcx(PcxFixture());
                Require(p.Width==2&&p.Height==2&&p.Pixels[0]==1&&p.Pixels[3]==4,"Pixel geometry");
                Require(p.Palette[1].R==255&&p.Palette[2].G==255,"Palette channels");
            });
            check("PCX truncation, palette and RLE bounds",delegate {
                var p=PcxFixture();
                p[128]=197;
                p[129]=1;
                Reject(delegate {
                    Assets.Pcx(p);
                });
                p=PcxFixture();
                p[132]=0;
                Reject(delegate {
                    Assets.Pcx(p);
                });
                Reject(delegate {
                    Assets.Pcx(new byte[10]);
                });
            });
            check("Independent SPH skip/run and missing-frame fixture",delegate {
                byte[] b=new byte[25];
                W16(b,0,2);
                BitConverter.GetBytes(10).CopyTo(b,2);
                BitConverter.GetBytes(uint.MaxValue).CopyTo(b,6);
                W16(b,10,65535);
                W16(b,12,2);
                W16(b,14,3);
                W16(b,16,2);
                new byte[] {
                    1,2,5,6,0,1,7
                }
                .CopyTo(b,18);
                var f=Assets.Sph(b,new Color[256]);
                Require(f[1]==null&&f[0].OriginX==-1&&!f[0].Visible[0]&&f[0].Pixels[1]==5&&f[0].Pixels[3]==7,"Sprite geometry");
                b[18]=3;
                Reject(delegate {
                    Assets.Sph(b,new Color[256]);
                });
            });
            check("Independent SCN x-major grid and tail bounds",delegate {
                var b=new List<byte>();
                for(int x=0; x<64; x++)for(int y=0; y<64; y++) {
                    if(x==2&&y==3) {
                        b.Add(2);
                        b.Add(29);
                    }
                    else b.Add(1);
                }
                b.Add(2);
                b.AddRange(new byte[16]);
                var s=Scenario.Load(b.ToArray(),"fixture");
                Require(s.At(2,3).Kind==2&&s.At(2,3).Value==29&&s.At(3,2).Kind==1,"Grid orientation");
                b.RemoveAt(b.Count-1);
                Reject(delegate {
                    Scenario.Load(b.ToArray(),"truncated");
                });
            });
            check("Packaged original resource fixtures",delegate {
                var p=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));
                Require(p.Width==640&&p.Height==512,"Classic background dimensions");
                var f=Assets.Sph(Path.Combine(assets,"Graphics","BATTSHIP.SPH"),Assets.Palette(Path.Combine(assets,"Graphics","ISLAND.COL")));
                Require(f.Count==5&&f[0].Width==134&&f[0].Height==95&&f[4]==null,"Battleship sprite frame table");
                int[] expected= {
                    4342,4519,11052
                };
                string[] names= {
                    "BS201.SCN","BS202.SCN","BS204.SCN"
                };
                for(int i=0; i<3; i++) {
                    var s=Scenario.Load(Path.Combine(assets,"Graphics",names[i]));
                    Require(s.TerrainBytes==expected[i]&&s.Players==2,"Scenario fixture offset "+names[i]);
                }
            });
            string evidence=Path.Combine(Path.GetDirectoryName(assets),"Evidence");
            check("Credits catalog matches all274native resource requests and rejects altered tables",delegate{
                var catalog=CreditsCatalog.Load(assets);Require(catalog.Names.Length==137,"Credits catalog size");var rows=File.ReadAllLines(Path.Combine(evidence,"Credits-Catalog-Requests-Oracle.csv"));Require(rows.Length==275,"Catalog request fixture shape");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');string name=catalog.Name(int.Parse(f[7]));Require(name==f[20]&&catalog.Directory(name,f[9]=="2")== (f[19]=="H"?"HWEBS":"LWEBS")&&f[10]=="0","Native catalog request "+i);}var names=catalog.Names;names[0]="changed";Require(catalog.Name(1)=="BATFIRE1","Catalog name exposure");byte[] source=(byte[])catalog.OriginalBytes.Clone();source[9]^=1;Reject(delegate{CreditsCatalog.Read(source);});Reject(delegate{CreditsCatalog.Read(new byte[1241]);});Reject(delegate{catalog.Directory("../bad",false);});foreach(int id in new[]{0,138}){bool rejected=false;try{catalog.Name(id);}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Catalog ID boundary");}
            });
            check("Full Credits catalog matches30358native decoded frame hashes and source framing",delegate{
                var groups=new Dictionary<string,List<string[]>>();foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Credits-Catalog-Frames-Oracle.csv"))){if(row.StartsWith("Directory,"))continue;var f=row.Split(',');string key=f[0]+"/"+f[1];if(!groups.ContainsKey(key))groups[key]=new List<string[]>();groups[key].Add(f);}Require(groups.Count==274,"Catalog decoder fixture groups");int total=0;foreach(var group in groups){var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,group.Key.Replace('/',Path.DirectorySeparatorChar)+".PAT")));Require(movie.Frames.Count==group.Value.Count,"Catalog native frame count");foreach(var f in group.Value){int index=int.Parse(f[2]);var frame=movie.Frames[index];Require(frame.Offset==int.Parse(f[3])&&frame.PayloadBytes==int.Parse(f[4])&&Hash(frame.Pixels)==f[5],"Catalog decoder "+group.Key+" frame"+index);total++;}}Require(total==30358,"Catalog frame sweep count");
                var first=File.ReadAllLines(Path.Combine(evidence,"Credits-Catalog-FirstFrames-Oracle.csv"));Require(first.Length==275,"Catalog independent-first fixtures");for(int i=1;i<first.Length;i++){var f=first[i].Split(',');Require(f[2]=="1"&&f[3]==f[4]&&groups[f[0]+"/"+f[1]][0][5]==f[3],"Native first-frame independence");}
            });
            check("Full catalog cue onsets retain native sequence order including failed handles",delegate{
                var starts=new Dictionary<string,List<string[]>>();foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Credits-Catalog-Sequence-Oracle.csv"))){var f=row.Split(',');if(f.Length!=6||f[2]!="Start")continue;string key=f[0]+"/"+f[1];if(!starts.ContainsKey(key))starts[key]=new List<string[]>();starts[key].Add(f);}int cuesTotal=0,failures=0,cases=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Credits-Catalog-Sequence-Inputs.csv"))){if(row.StartsWith("Directory,"))continue;var f=row.Split(',');int frames=int.Parse(f[2]);byte[] seq=File.ReadAllBytes(Path.Combine(assets,"SFX",f[1],f[1]+".SEQ"));Require(Hash(seq)==f[3]&&Hash(File.ReadAllBytes(Path.Combine(assets,f[0],f[1]+".PAT")))==f[4],"Native sequence input identity");var cues=ClipSound.Sequence(Encoding.ASCII.GetString(seq));var expected=starts[f[0]+"/"+f[1]];int count=0;for(int i=0;i<cues.Count;i++){if(cues[i].ActualStart>=frames)continue;var native=expected[count++];Require(int.Parse(native[3])==i&&int.Parse(native[4])==cues[i].ActualStart,"Native catalog cue time "+f[1]+"/"+i);bool present=File.Exists(Path.Combine(assets,"SFX",f[1],cues[i].Name+".WAV"));Require(int.Parse(native[5])==(present?i+1:-1),"Native failed cue handle");if(!present)failures++;}Require(count==expected.Count,"Extra native cue starts");cuesTotal+=cues.Count;cases++;}Require(cases==274&&cuesTotal==2050&&failures==12,"Catalog sequence fixture totals");
            });
            check("Missing original cue files preserve remaining samples and queued onset offsets",delegate{
                List<SoundCue> cues;List<string> missing;var mix=ClipSound.LoadMixWithGaps(assets,"ISL_DSHS",86,50,out cues,out missing);Require(cues.Count==10&&missing.Count==4&&new HashSet<string>(missing).SetEquals(new[]{"SHELIN_A.WAV","FIRE45F.WAV","EXP01.WAV","EXP02.WAV"}),"Missing source files");var pcm=mix.Pcm(100);Require(pcm.Length==47407,"20fps fractional sample endpoint");short[] wave=ClipSound.Pcm(File.ReadAllBytes(Path.Combine(assets,"SFX","ISL_DSHS","SMLEXP3.WAV")));var sum=new int[pcm.Length];foreach(int frame in new[]{32,36,44,66}){int at=(int)((long)frame*11025/20);for(int i=0;i<wave.Length&&at+i<sum.Length;i++)sum[at+i]+=wave[i];}bool nonzero=false;for(int i=0;i<sum.Length;i++){short expected=(short)Math.Max(short.MinValue,Math.Min(short.MaxValue,sum[i]));Require(pcm[i]==expected,"Missing-cue mix shifted or lost surviving audio");nonzero|=pcm[i]!=0;}Require(nonzero&&cues[4].ActualStart==32&&cues[4].Slot>=0&&cues[0].Slot==-1&&cues[1].Slot==-1,"Missing cue queue/voice boundary");
                List<SoundCue> normal,partial;List<string> none;var a=ClipSound.LoadMix(assets,"BATFIRE1",46,50,out normal).Pcm(100);var b=ClipSound.LoadMixWithGaps(assets,"BATFIRE1",46,50,out partial,out none).Pcm(100);Require(none.Count==0&&a.Length==b.Length,"No-gap sample count");for(int i=0;i<a.Length;i++)Require(a[i]==b[i],"No-gap mixer changed samples");
            });
            check("Original fleet generator matches native layouts, RNG consumption and rebuilt occupancy",delegate{
                var rows=File.ReadAllLines(Path.Combine(evidence,"Placement-Layouts-Oracle.csv"));Require(rows.Length==298,"Placement fixture case count");foreach(string row in new List<string>(rows).GetRange(1,rows.Length-1)){var f=row.Split(',');uint seed=uint.Parse(f[1]);int zero=int.Parse(f[2]),calls=0;int[] types=Array.ConvertAll(f[3].Split(';'),int.Parse),sizes=Array.ConvertAll(f[4].Split(';'),int.Parse),pointers=Array.ConvertAll(f[5].Split(';'),int.Parse);var initial=new Point[6];for(int i=0;i<6;i++){initial[i]=new Point(123,124);if(types[i]==0||pointers[i]==0)sizes[i]=0;}var rng=new NativeRandom(seed);var result=OriginalPlacement.Generate(sizes,delegate{calls++;return calls<=zero?0:rng.Next();},initial);Require(result.Calls==int.Parse(f[7])&&result.Restarts==int.Parse(f[8])&&rng.State==uint.Parse(f[9]),"Native placement RNG/restart "+f[0]);var xy=f[10].Split(';');var grid=new byte[400];for(int i=0;i<100;i++)BitConverter.GetBytes(int.Parse(f[6])==2?3:0).CopyTo(grid,i*4);for(int i=0;i<6;i++){var point=xy[i].Split(':');Require(result.Positions[i]==new Point(int.Parse(point[0]),int.Parse(point[1])),"Native placement coordinate "+f[0]+"/"+i);if(sizes[i]!=0){Require(Board.Inside(result.Positions[i])&&result.Positions[i].Y+sizes[i]<=10,"Native position bounds");for(int y=0;y<sizes[i];y++)BitConverter.GetBytes(2).CopyTo(grid,(result.Positions[i].X*10+result.Positions[i].Y+y)*4);}}Require(Hash(grid)==f[11],"Native occupancy rebuild "+f[0]);foreach(Point p in initial)Require(p==new Point(123,124),"Placement mutated prior input");}
            });
            check("Original placement random-call traces retain tie order and full100failure restart",delegate{
                var expected=new Dictionary<string,List<string>>();foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Placement-Trace-Oracle.csv"))){if(row.StartsWith("Case,"))continue;string key=row.Substring(0,row.IndexOf(','));if(!expected.ContainsKey(key))expected[key]=new List<string>();expected[key].Add(row);}foreach(string name in new[]{"default-1","restart-202"}){var rng=new NativeRandom(1);int calls=0,zeros=name=="restart-202"?202:0;Point[] before=null;var rows=new List<string>();int[] sizes={5,5,3,4,2,0};var initial=new Point[6];for(int i=0;i<6;i++)initial[i]=new Point(123,124);var result=OriginalPlacement.Generate(sizes,delegate{int roll=++calls<=zeros?0:rng.Next();var coordinates=new List<string>();for(int i=0;i<6;i++)if(sizes[i]!=0)coordinates.Add(i+":"+before[i].X+":"+before[i].Y);rows.Add(name+","+calls+","+roll+","+rng.State+","+string.Join(";",coordinates.ToArray()));return roll;},initial,256,delegate(Point[] points){before=points;});Require(rows.Count==expected[name].Count,"Placement trace count");for(int i=0;i<rows.Count;i++)Require(rows[i]==expected[name][i],"Native placement trace "+name+"/"+i);Require(result.Restarts==(zeros==0?0:1),"Forced full restart count");}
            });
            check("Placement rejects invalid data and bounds the original unbounded restart",delegate{
                Reject(delegate{OriginalPlacement.Generate(new[]{5},delegate{return -1;});});Reject(delegate{OriginalPlacement.Generate(new[]{5},delegate{return 32768;});});int calls=0;bool stopped=false;var prior=new[]{new Point(3,4),new Point(5,0)};try{OriginalPlacement.Generate(new[]{5,5},delegate{calls++;return 0;},prior,0);}catch(InvalidOperationException e){stopped=e.Message.Contains("native loop is unbounded");}Require(stopped&&calls==202&&prior[0]==new Point(3,4)&&prior[1]==new Point(5,0),"Bounded restart/transaction boundary");foreach(int[] input in new[]{new int[0],new int[7],new[]{11},new[]{-1}}){bool rejected=false;try{OriginalPlacement.Generate(input,delegate{return 0;});}catch(ArgumentException){rejected=true;}Require(rejected,"Invalid placement shape accepted");}
            });
            check("Recovered player re-roll and enemy start use consecutive native placement draws",delegate{
                foreach(int seed in new[]{0,1,731,-1,int.MinValue}){var game=new Game(seed,RuleProfile.Recovered);var rng=new NativeRandom(unchecked((uint)seed));int[] sizes={5,5,3,4,2};for(int pass=0;pass<3;pass++){var expected=OriginalPlacement.Generate(sizes,rng.Next);Board board;if(pass<2){game.AutoPlace();board=game.Player;}else{Require(game.Start(),"Native layout start rejected");board=game.Enemy;}for(int i=0;i<5;i++)Require(board.Ships[i].Cells[0]==expected.Positions[i]&&board.Ships[i].SourceType==RuleProfile.Recovered.Fleet[i].SourceType,"Game native layout stream/roster");Require(game.PlacementState==rng.State&&game.PlacementCalls==rng.Calls,"Game placement stream state");}}
            });
            check("Credits bounded script and native text parser preserve 210 original lines",delegate{
                byte[] bytes=File.ReadAllBytes(Path.Combine(assets,"Graphics","CREDITS.TXT"));var text=new CreditsText(bytes);Require(text.Lines.Length==210&&text.Lines[36]=="NMS Battleship Credits"&&text.Lines[209]=="Nancy Trolley ","Credits text boundaries");var rows=File.ReadAllLines(Path.Combine(evidence,"Credits-Text-Oracle.csv"));Require(rows.Length==211,"Credits text fixture shape");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');byte[] line=Encoding.ASCII.GetBytes(text.Lines[i-1]);Require(line.Length==int.Parse(f[1])&&Hash(line)==f[2],"Native Credits parsed line "+i);}Require(Hash(text.OriginalBytes)==Hash(bytes),"Credits input not preserved");Reject(delegate{new CreditsText(Encoding.ASCII.GetBytes("one\n"));});Reject(delegate{new CreditsText(new byte[]{65,128,10,10});});Reject(delegate{new CreditsText(Encoding.ASCII.GetBytes("one\rtwo\n\n"));});byte[] script=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","Credits.seqbin"));CreditsProgram.Read(script);script[16]=42;Reject(delegate{CreditsProgram.Read(script);});
            });
            check("Front-end Credits font metrics and direct indexed raster match native fixtures",delegate{
                var palette=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")));using(var font=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),palette,true)){var metrics=File.ReadAllLines(Path.Combine(evidence,"Credits-Metrics-Oracle.csv"));Require(metrics.Length==565,"FE font fixture shape");for(int i=1;i<metrics.Length;i++){var f=metrics[i].Split(',');Require(font.Metric(int.Parse(f[0]))==new Size(int.Parse(f[1]),int.Parse(f[2])),"Native front font metric "+i);}var roll=new CreditsRoll(new CreditsText(File.ReadAllBytes(Path.Combine(assets,"Graphics","CREDITS.TXT"))));var rows=File.ReadAllLines(Path.Combine(evidence,"Credits-Pixels-Oracle.csv"));Require(rows.Length==15,"Credits pixel cases");foreach(string row in new List<string>(rows).GetRange(1,rows.Length-1)){var f=row.Split(',');byte[] pixels=new byte[640*480];for(int i=0;i<pixels.Length;i++)pixels[i]=(byte)(1+(i*7)%255);foreach(var line in roll.Commands(font,int.Parse(f[0]),int.Parse(f[1])))pixels=font.Raster(pixels,640,480,line.At.X,line.At.Y,line.Text,4);Require(Hash(pixels)==f[2],"Native Credits pixels "+f[0]+"/"+f[1]);}}
            });
            check("Credits roll matches a complete native loop including pre-update drawing",delegate{
                var text=new CreditsText(File.ReadAllBytes(Path.Combine(assets,"Graphics","CREDITS.TXT")));var roll=new CreditsRoll(text);var palette=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")));using(var font=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),palette,true)){var rows=File.ReadAllLines(Path.Combine(evidence,"Credits-Roll-Oracle.csv"));Require(rows.Length==1472,"Credits native loop length");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');Require(roll.StateLine==int.Parse(f[1])&&roll.StateOffset==int.Parse(f[2]),"Credits before state "+i);roll.Advance(50);Require(roll.StateLine==int.Parse(f[3])&&roll.StateOffset==int.Parse(f[4]),"Credits after state "+i);var commands=roll.Commands(font);var joined=new List<string>();for(int j=0;j<commands.Count;j++){var line=commands[j];joined.Add(j+","+line.At.X+","+line.At.Y+","+Convert.ToBase64String(Encoding.ASCII.GetBytes(line.Text)));}Require(Hash(Encoding.ASCII.GetBytes(string.Join("\n",joined.ToArray())))==f[6],"Credits draw state "+i);}}
            });
            check("Credits scroll retains elapsed remainder and loops without frame partition drift",delegate{
                var text=new CreditsText(File.ReadAllBytes(Path.Combine(assets,"Graphics","CREDITS.TXT")));var a=new CreditsRoll(text);var b=new CreditsRoll(text);a.Advance(147049);for(int i=0;i<147;i++){b.Advance(17);b.Advance(983);}b.Advance(49);Require(a.Ticks==b.Ticks&&a.StateLine==b.StateLine&&a.DisplayOffset==b.DisplayOffset&&a.Ticks==2940,"Credits partition/wrap");Require(!a.Advance(0)&&a.Advance(1)&&a.StateLine==0&&a.StateOffset==-2,"Credits exact boundary");a.Reset();Require(a.Ticks==0&&a.StateOffset==0,"Credits reset");bool rejected=false;try{a.Advance(-1);}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Negative Credits elapsed accepted");
            });
            Func<List<IndexedImage>> prefFrames=delegate{return Assets.Sph(Path.Combine(assets,"Graphics","EO_PREFS.SPH"),Assets.Pcx(Path.Combine(assets,"Graphics","E_PREFS.PCX")).Palette);};
            Func<PreferencesProgram> prefProgram=delegate{return PreferencesProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","Preferences.seqbin")),prefFrames());};
            check("Original Preferences script boundary and native thirteen-button records",delegate{
                var frames=prefFrames();byte[] data=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","Preferences.seqbin"));var program=PreferencesProgram.Read(data,frames);var rows=File.ReadAllLines(Path.Combine(evidence,"Preferences-Buttons-Oracle.csv"));Require(rows.Length==14,"Preferences button fixture shape");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');var b=program.Buttons[int.Parse(f[0])];Require(b.At==new Point(int.Parse(f[1]),int.Parse(f[2]))&&b.Action==int.Parse(f[3])&&b.Frame==int.Parse(f[4])&&b.Group==int.Parse(f[5])&&b.Size==new Size(int.Parse(f[6]),int.Parse(f[7])),"Native preference record "+i);}
                for(int i=0;i<data.Length;i+=4){var bad=(byte[])data.Clone();bad[i]^=0x40;Reject(delegate{PreferencesProgram.Read(bad,frames);});}Reject(delegate{PreferencesProgram.Read(new byte[900],frames);});var fewer=new List<IndexedImage>(frames);fewer.RemoveAt(33);Reject(delegate{PreferencesProgram.Read(data,fewer);});Reject(delegate{PreferencesProgram.Change(new PreferenceValues(),10);});Reject(delegate{PreferencesProgram.Change(new PreferenceValues{Volume=3},9);});
            });
            check("Preferences changes and save/back/Escape boundaries match original dispatcher",delegate{
                var rows=File.ReadAllLines(Path.Combine(evidence,"Preferences-Changes-Oracle.csv"));Require(rows.Length==1121,"Preference action case count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');var state=new PreferenceValues{Quality=int.Parse(f[0]),Radar=int.Parse(f[1]),Difficulty=int.Parse(f[2]),Volume=int.Parse(f[3])};int action=int.Parse(f[4]);bool external=action==97||action==99||action==-1;var next=external?state:PreferencesProgram.Change(state,action);Require(next.Quality==int.Parse(f[5])&&next.Radar==int.Parse(f[6])&&next.Difficulty==int.Parse(f[7])&&next.Volume==int.Parse(f[8])&&f[9]==(external?"1":"0")&&f[10]==(action==97?"1":"0"),"Native preference action "+i);string calls=action==9||action==98||(action==55&&state.Volume>0)?"Volume|Preview":"";Require(f[11]==calls,"Native preference volume callback order");}
            });
            check("Preferences half-open hits and all263volume bar positions match native input",delegate{
                var program=prefProgram();var rows=File.ReadAllLines(Path.Combine(evidence,"Preferences-Hits-Oracle.csv"));Require(rows.Length==1189,"Preference pointer matrix count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');int x=int.Parse(f[0]),y=int.Parse(f[1]),buttons=int.Parse(f[2]);int index=program.Hit(x,y,buttons==2),action=index<0?-2:program.Buttons[index].Action;int volume=buttons==1&&PreferencesProgram.VolumeBar.Contains(x,y)?PreferencesProgram.BarValue(x):int.Parse(f[4]);Require(action==int.Parse(f[5])&&volume==int.Parse(f[6]),"Native preference hit "+i);}
                rows=File.ReadAllLines(Path.Combine(evidence,"Preferences-Bar-Oracle.csv"));Require(rows.Length==264,"Volume bar extent");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');Require(PreferencesProgram.BarValue(int.Parse(f[0]))==int.Parse(f[1]),"Native bar quantization");}Require(program.Hit(-1,-1,false)==-1&&program.Hit(640,480,true)==-1,"Outside preference surface");
            });
            check("Preferences indexed overlays, volume bar and footer match320native composites",delegate{
                var frames=prefFrames();var program=PreferencesProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","Preferences.seqbin")),frames);var card=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=new Color[256]};for(int i=0;i<card.Pixels.Length;i++)card.Pixels[i]=(byte)(1+(i*7)%255);var rows=File.ReadAllLines(Path.Combine(evidence,"Preferences-Composite-Oracle.csv"));Require(rows.Length==321,"Preference composition count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');var state=new PreferenceValues{Quality=int.Parse(f[1]),Radar=int.Parse(f[2]),Difficulty=int.Parse(f[3]),Volume=int.Parse(f[4])};Require(Hash(program.Compose(card,frames,state,int.Parse(f[5])).Pixels)==f[6],"Native preference pixels "+f[0]);}Require(card.Pixels[0]==1&&card.Pixels[142+354*640]==(byte)(1+((142+354*640)*7)%255),"Composition mutated input");Reject(delegate{program.Compose(card,frames,new PreferenceValues{Quality=3},-1);});
            });
            check("Cinematic gain curve matches original DirectSound requests and retains headroom",delegate{
                var rows=File.ReadAllLines(Path.Combine(evidence,"Preferences-Volume-Oracle.csv"));Require(rows.Length==102,"Native gain fixture count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');int percent=int.Parse(f[0]);Require(SoundLevel.NativeBufferVolume(percent)==int.Parse(f[1])&&SoundLevel.NativeHundredthsDb(percent)==int.Parse(f[2]),"Native gain transfer");}
                Require(SoundLevel.Gain(0)==0&&SoundLevel.Gain(100)==1&&Math.Abs(SoundLevel.Gain(50)-0.1)<1e-12&&Math.Abs(SoundLevel.Gain(80)-0.3981071705534972)<1e-12,"Software gain/modern Off");var raw=new[]{80000,-80000,32767,-32768,0};var full=SoundLevel.Mix(raw,100);var lower=SoundLevel.Mix(raw,50);Require(full[0]==32767&&full[1]==-32768&&lower[0]==8000&&lower[1]==-8000&&lower[2]==3277&&lower[3]==-3277&&raw[0]==80000,"Gain must precede final saturation without mutating source");foreach(short sample in SoundLevel.Mix(raw,0))Require(sample==0,"Off is not silent");foreach(int bad in new[]{-1,101}){bool rejected=false;try{SoundLevel.Gain(bad);}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Gain bound");}
            });
            check("Original22050/16bit and11025/8bit effect WAV gain preserves metadata and rejects malformed RIFF",delegate{
                foreach(string name in new[]{"KEYCLIK2.WAV","CANNON1.WAV"}){byte[] original=File.ReadAllBytes(Path.Combine(assets,"SFX",name));string hash=Hash(original);Require(Hash(SoundLevel.Wave(original,100))==hash,"Unity effect changed");byte[] muted=SoundLevel.Wave(original,0);int at=12,data=-1,length=0,bits=0;while(at<original.Length){string tag=System.Text.Encoding.ASCII.GetString(original,at,4);int count=BitConverter.ToInt32(original,at+4);if(tag=="fmt ")bits=BitConverter.ToUInt16(original,at+8+14);if(tag=="data"){data=at+8;length=count;}at+=8+count+(count&1);}Require(data>=0&&length>0,"Independent effect chunk fixture");for(int i=0;i<original.Length;i++)Require(muted[i]==(i>=data&&i<data+length?(byte)(bits==8?128:0):original[i]),"Muted effect samples/metadata");Require(Hash(original)==hash,"Gain modified original WAV");var truncated=new byte[original.Length-1];Array.Copy(original,truncated,truncated.Length);Reject(delegate{SoundLevel.Wave(truncated,100);});var bad=(byte[])original.Clone();bad[0]=0;Reject(delegate{SoundLevel.Wave(bad,100);});}
                Reject(delegate{SoundLevel.Wave(new byte[44],100);});
            });
            check("Preferences migration, numeric bounds and unknown bindings persist",delegate{
                var old=DisplaySettings.Parse("sound=false\ncustomConfirm=Enter\n");Require(old.FxVolume==100&&old.Difficulty==0&&old.Radar==0&&!old.Sound,"Legacy preference migration changed output");var prefs=DisplaySettings.Parse("fxvolume=80\ndifficulty=3\nradar=1\ncustomConfirm=Enter\n");var again=DisplaySettings.Parse(prefs.Serialize());Require(again.FxVolume==80&&again.Difficulty==3&&again.Radar==1&&again.Serialize().Contains("customConfirm=Enter"),"Preference round trip/binding retention");foreach(string bad in new[]{"fxvolume=-5","fxvolume=101","fxvolume=3","fxvolume=NaN","difficulty=4","radar=2"}){var value=DisplaySettings.Parse(bad);Require(value.Problem!=""&&value.FxVolume==100&&value.Difficulty==0&&value.Radar==0,"Invalid preference accepted");}Reject(delegate{new DisplaySettings{FxVolume=3}.Serialize();});
            });
            Func<ClassicMessages> loadMessages=delegate{return new ClassicMessages(File.ReadAllBytes(Path.Combine(assets,"Graphics","ENGLISH.TXT")),File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","MessageShipNames.seqbin")));};
            check("English text-mode lines and native ship-name records",delegate {
                var messages=loadMessages();var rows=File.ReadAllLines(Path.Combine(evidence,"Message-Language-Oracle.csv"));Require(rows.Length==132,"Language oracle count");for(int i=1;i<rows.Length;i++){var fields=rows[i].Split(',');byte[] line=System.Text.Encoding.ASCII.GetBytes(messages.Line(int.Parse(fields[0])));Require(line.Length==int.Parse(fields[1])&&Hash(line)==fields[2],"Native English line "+i);}
                byte[] english=File.ReadAllBytes(Path.Combine(assets,"Graphics","ENGLISH.TXT")),names=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","MessageShipNames.seqbin"));Require(messages.Line(13).EndsWith(" ",StringComparison.Ordinal),"Trailing space lost");Reject(delegate{new ClassicMessages(new byte[]{65,0,10},names);});Reject(delegate{new ClassicMessages(new byte[]{65,13,66,10},names);});Reject(delegate{new ClassicMessages(english,new byte[337]);});var bad=(byte[])names.Clone();for(int i=0;i<26;i++)bad[i]=65;Reject(delegate{new ClassicMessages(english,bad);});
            });
            check("Classic messages match1200native dispatch/wrap cases",delegate {
                var messages=loadMessages();int cases=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Message-Commands-Oracle.csv"))){if(row.StartsWith("Case"))continue;var f=row.Split(',');int local=int.Parse(f[1]),phase=int.Parse(f[2]),type=int.Parse(f[3]),sunk=int.Parse(f[4]),result=int.Parse(f[5]),line=int.Parse(f[6]);var commands=messages.Commands(phase,local,type,sunk,result,local==0?"The Computer":"Human");if(line<0)Require(commands.Count==0,"Unexpected message");else{Require(line<commands.Count,"Missing message line");var command=commands[line];Require(command.At==new Point(int.Parse(f[7]),int.Parse(f[8]))&&f[9]=="3"&&command.Text==System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[10])),"Native message command "+f[0]);}if(line<=0)cases++;}Require(cases==1200,"Native dispatch count");
                Require(messages.Commands(3,0,1,1,0,"The Computer").Count==0&&messages.Commands(7,0,1,1,1,"The Computer").Count==0,"Hit/miss should use native icons");Reject(delegate{messages.Commands(1,0,1,1,0,"bad\nname");});
            });
            check("Original style3font metrics/glyph positions and native raster pixels",delegate {
                using(var font=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette)){
                    var rows=File.ReadAllLines(Path.Combine(evidence,"Message-Metrics-Oracle.csv"));Require(rows.Length==565,"Metric count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');Require(font.Metric(int.Parse(f[0]))==new Size(int.Parse(f[1]),int.Parse(f[2]))&&int.Parse(f[3])==i-1,"Native font metric "+i);}
                    foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Message-Glyphs-Oracle.csv"))){if(row.StartsWith("Sample"))continue;var f=row.Split(',');string sample=System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[0]));var glyph=font.Layout(12,11,sample)[int.Parse(f[1])];Require(glyph.At==new Point(int.Parse(f[2]),int.Parse(f[3]))&&glyph.Frame==int.Parse(f[4])&&f[5]=="100"&&f[6]=="0","Native glyph placement");}
                    byte[] background=new byte[640*480];for(int i=0;i<background.Length;i++)background[i]=(byte)(1+(i*7)%255);rows=File.ReadAllLines(Path.Combine(evidence,"Message-Pixels-Oracle.csv"));Require(rows.Length==6,"Font pixel fixture count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');string sample=System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[0]));Require(Hash(font.Raster(background,640,480,12,11,sample))==f[1],"Native font pixels "+i);}Reject(delegate{font.Layout(0,0,"bad\ttext");});
                }
            });
            check("Native pause centering and style4measure/glyph/pixel fixtures",delegate {
                var messages=loadMessages();using(var font=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette)){
                    var rows=File.ReadAllLines(Path.Combine(evidence,"Pause-Commands-Oracle.csv"));Require(rows.Length==13,"Pause case shape");foreach(string row in rows){if(row.StartsWith("Width"))continue;var f=row.Split(',');if(f[1]=="0"){Require(f[2]=="-1","Unpaused commands");continue;}var line=font.PauseLines(messages,int.Parse(f[0]),true)[int.Parse(f[2])];Require(line.At==new Point(int.Parse(f[3]),int.Parse(f[4]))&&line.Text==System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[6]))&&f[5]=="4","Native pause layout");}
                    foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Pause-Measure-Oracle.csv"))){if(row.StartsWith("Sample"))continue;var f=row.Split(',');Require(font.Measure(System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[0])),4)==int.Parse(f[1]),"Native style4measure");}
                    foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Pause-Glyphs-Oracle.csv"))){if(row.StartsWith("Sample"))continue;var f=row.Split(',');string sample=System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[0]));var glyph=font.Layout(12,11,sample,4)[int.Parse(f[1])];Require(glyph.At==new Point(int.Parse(f[2]),int.Parse(f[3]))&&glyph.Frame==int.Parse(f[4])&&f[5]=="100"&&f[6]=="0","Native style4glyph");}
                    byte[] background=new byte[640*480];for(int i=0;i<background.Length;i++)background[i]=(byte)(1+(i*7)%255);rows=File.ReadAllLines(Path.Combine(evidence,"Pause-Pixels-Oracle.csv"));Require(rows.Length==6,"Pause pixel sample count");for(int i=1;i<rows.Length;i++){var f=rows[i].Split(',');Require(Hash(font.Raster(background,640,480,12,11,System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(f[0])),4))==f[1],"Native style4pixels");}
                }
            });
            check("Adapted pause prompts preserve binding and reject unsupported typography",delegate {
                using(var font=new ClassicSpriteFont(Path.Combine(assets,"Graphics"),Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette)){
                    var lines=font.PauseLines(loadMessages(),640,false);Require(lines[0].Text=="Game Is Paused"&&lines[1].Text=="Escape Also Resumes"&&lines[2].Text=="Press P To Resume","Runtime prompt contract");Require(font.Measure(" ",4)==font.Metric(412).Width&&font.Measure("E",4)==font.Metric(412).Width+3,"Style4space must omit padding");Require(font.Measure("",4)==0,"Empty width");Reject(delegate{font.Measure("a\t",4);});Reject(delegate{font.Layout(0,0,"abc",2);});foreach(int width in new[]{0,32768}){bool rejected=false;try{font.PauseLines(loadMessages(),width,false);}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Pause viewport bound");}
                }
            });
            check("Original paused-Escape teardown and title-entry chain",delegate {
                var rows=File.ReadAllLines(Path.Combine(evidence,"Navigation-Chain-Oracle.csv"));Require(rows.Length==49,"Navigation fixture shape");for(int i=1;i<rows.Length;i++){
                    var f=rows[i].Split(',');int network=int.Parse(f[0]),flag=int.Parse(f[1]),failure=int.Parse(f[2]),stage=int.Parse(f[3]);Require(network>=0&&network<=2&&flag>=0&&flag<=1&&failure>=0&&failure<=1&&stage>=0&&stage<=3,"Navigation fixture domain");
                    int state=stage==0?4:stage==1?20:stage==2?9:failure==0?0:9,mode=stage<2?14:2;string expected="";
                    if(stage==0){if(network>0)expected="NetworkCommand:9:0:0|NetworkFlush";}
                    else if(stage==1){expected="StopAudio:7";if(network>0)expected+="|NetworkFlush|NetworkCleanup";if(network<=1)expected+="|Callback41B6B0";}
                    else if(stage==2)expected=(flag==1?"Callback402D20|":"")+"BattleResourceRelease|FadeOut";
                    else expected="Bank:0|FrontResources|"+(failure==0?"Script:0044EF88":"ResourceError");
                    Require(int.Parse(f[4])==state&&int.Parse(f[5])==mode&&f[6]=="9"&&f[7]=="2"&&f[8]=="0044EF88"&&int.Parse(f[9])==(stage==3?failure:0)&&f[10]==expected,"Native navigation stage "+i);
                }
            });
            check("Local battle entry prefix and factor order match original dispatcher",delegate {
                byte[] bytes=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","BattleEntry.seqbin"));var program=EntryProgram.Read(bytes,-1);
                for(int i=0;i<bytes.Length;i+=4){var bad=(byte[])bytes.Clone();bad[i]^=0x40;Reject(delegate{EntryProgram.Read(bad,-1);});}Reject(delegate{EntryProgram.Read(new byte[172],-1);});Reject(delegate{EntryProgram.Read(bytes,0);});Reject(delegate{EntryProgram.Read(bytes,1);});
                var timeline=new EntryTimeline(program);timeline.Advance(560);var expected=new List<string>();foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Entry-Trace-Oracle.csv"))){if(!row.StartsWith("-1,"))continue;var f=row.Split(',');if(f[2]=="Card"||f[2]=="Fade")expected.Add(f[2]+","+f[3]);}
                Require(expected.Count==15&&timeline.Trace.Count==16&&timeline.Complete,"Entry trace shape");for(int i=0;i<expected.Count;i++)Require(timeline.Trace[i]==expected[i],"Native entry order "+i);Require(timeline.Trace[15]=="Boundary,4","Entry boundary");
                var states=File.ReadAllLines(Path.Combine(evidence,"Entry-State-Oracle.csv"));Require(states.Length==4&&states[1]=="-1,6,0,4,14,1,4,00450EC8","Native local outer dispatch");
                var native=File.ReadAllLines(Path.Combine(evidence,"Entry-Trace-Oracle.csv"));Require(Array.IndexOf(native,"-1,15,Init,Human")>=0&&Array.IndexOf(native,"-1,16,Init,Opponents")>=0&&Array.IndexOf(native,"-1,17,Init,EmptyFleets")>=0,"Local callback order");
            });
            check("Entry clock partitions, final-frame hold, pause and idempotent completion",delegate {
                var program=EntryProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","BattleEntry.seqbin")),-1);var one=new EntryTimeline(program);var split=new EntryTimeline(program);one.Advance(560);for(int i=0;i<80;i++)split.Advance(7);Require(string.Join(";",one.Trace.ToArray())==string.Join(";",split.Trace.ToArray()),"Entry partitions");
                var paused=new EntryTimeline(program);paused.Advance(520);Require(paused.Factor==64&&!paused.Complete,"Full-bright frame hold");paused.Pause();paused.Advance(100000);Require(!paused.Complete,"Pause advanced entry");paused.Resume();paused.Advance(39);Require(!paused.Complete,"Final frame shortened");paused.Advance(1);int count=paused.Trace.Count;paused.Resume();paused.Advance(long.MaxValue);Require(paused.Complete&&paused.Trace.Count==count,"Repeated entry completion");
            });
            check("Fleet PCX storage is cropped to the native480line viewport",delegate {
                var card=Assets.Pcx(Path.Combine(assets,"Graphics","EC_FLEET.PCX"));Require(card.Width==640&&card.Height==512,"Original Fleet storage dimensions");
                var view=FleetProgram.Viewport(card);Require(view.Width==640&&view.Height==480&&view.Pixels.Length==307200&&view.Palette==card.Palette,"Fleet viewport shape/palette");
                for(int i=0;i<view.Pixels.Length;i++)Require(view.Pixels[i]==card.Pixels[i],"Fleet crop changed source pixel "+i);
                Reject(delegate{FleetProgram.Viewport(new IndexedImage{Width=640,Height=479,Pixels=new byte[640*479],Palette=card.Palette});});
                Reject(delegate{FleetProgram.Viewport(new IndexedImage{Width=640,Height=512,Pixels=new byte[640*480],Palette=card.Palette});});
            });
            check("Fleet camera/projection and cell picks match native fixtures",delegate {
                var camera=File.ReadAllLines(Path.Combine(evidence,"Fleet-Camera-Oracle.csv"));Require(camera.Length==2&&camera[1]=="5,5,147,98,147","Fleet camera");
                var rows=File.ReadAllLines(Path.Combine(evidence,"Fleet-Projection-Oracle.csv"));Require(rows.Length==122,"Projection count");for(int i=1;i<rows.Length;i++){var c=rows[i].Split(',');Require(FleetGeometry.Project(int.Parse(c[0]),int.Parse(c[1]))==new Point(int.Parse(c[2]),int.Parse(c[3])),"Fleet projection "+i);}
                rows=File.ReadAllLines(Path.Combine(evidence,"Fleet-Pick-Oracle.csv"));Require(rows.Length==101,"Pick count");for(int i=1;i<rows.Length;i++){var c=rows[i].Split(',');Require(FleetGeometry.Pick(new Point(int.Parse(c[2]),int.Parse(c[3])))==new Point(int.Parse(c[4]),int.Parse(c[5])),"Fleet pick "+i);}
            });
            check("Fleet centered moves match780native validator cases and commit atomically",delegate {
                var board=new Board();var specs=RuleProfile.Recovered.Fleet;for(int i=0;i<5;i++)Require(board.Place(new Point(i*2,0),specs[i].Length,true,specs[i].Name,specs[i].SourceType),"Fixture layout");var rows=File.ReadAllLines(Path.Combine(evidence,"Fleet-Placement-Oracle.csv"));Require(rows.Length==781,"Placement count");for(int i=1;i<rows.Length;i++){var c=rows[i].Split(',');Require(board.CanMoveClassic(int.Parse(c[0]),new Point(int.Parse(c[2]),int.Parse(c[3])))==(c[4]=="1"),"Native placement "+i);}
                var ship=board.Ships[0];Require(!board.MoveClassic(0,new Point(2,2))&&ship.Cells[0]==Point.Empty&&board.ShipAt(Point.Empty)==ship,"Failed move changed occupancy");Require(board.MoveClassic(0,new Point(1,2))&&board.ShipAt(Point.Empty)==null&&board.ShipAt(new Point(1,0))==ship,"Successful move occupancy");
                var game=new Game(731,RuleProfile.Recovered,OpponentKind.Original,0,false);game.AutoPlace();game.Start();Require(!game.MovePlacedShip(0,new Point(1,2)),"Battle admitted fleet editing");
            });
            check("Fleet strict script/button reader and native dimensions",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var flow=File.ReadAllBytes(Path.Combine(p,"FleetFlow.seqbin"));var buttons=File.ReadAllBytes(Path.Combine(p,"FleetButtons.seqbin"));var frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_FLEET.SPH"),Assets.Pcx(Path.Combine(assets,"Graphics","EC_FLEET.PCX")).Palette);var program=FleetProgram.Read(flow,buttons,frames);var rows=File.ReadAllLines(Path.Combine(evidence,"Fleet-Buttons-Oracle.csv"));Require(rows.Length==12,"Fleet buttons count");for(int i=1;i<rows.Length;i++){var c=rows[i].Split(',');var b=program.Buttons[i-1];Require(b.X==int.Parse(c[1])&&b.Y==int.Parse(c[2])&&b.Frame==int.Parse(c[3])&&b.Group==int.Parse(c[4])&&b.Action==int.Parse(c[5])&&b.Bounds.Width==int.Parse(c[6])&&b.Bounds.Height==int.Parse(c[7]),"Fleet button "+i);}
                Reject(delegate{FleetProgram.Read(new byte[180],buttons,frames);});var bad=(byte[])flow.Clone();bad[0]=255;Reject(delegate{FleetProgram.Read(bad,buttons,frames);});bad=(byte[])buttons.Clone();bad[12]=255;Reject(delegate{FleetProgram.Read(flow,bad,frames);});Reject(delegate{FleetProgram.Read(flow,new byte[220],frames);});
                Require(program.Hit(15,431)==2&&program.Hit(140,431)==-1,"Fleet half-open button edge");
                rows=File.ReadAllLines(Path.Combine(evidence,"Fleet-Init-Oracle.csv"));Require(rows.Length==11,"Native initializer count");for(int i=1;i<rows.Length;i++){var c=rows[i].Split(',');var spec=RuleProfile.Recovered.Fleet[(i-1)%5];Require(int.Parse(c[1])==spec.SourceType&&int.Parse(c[2])==(i-1)/5&&int.Parse(c[5])==spec.Length,"Native initial roster");}Require(File.ReadAllLines(Path.Combine(evidence,"Fleet-Autoplace-Calls.csv")).Length==3,"Native randomizes both fleets");
            });
            Func<SoloProgram> loadSolo=delegate {string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var palette=Assets.Pcx(Path.Combine(assets,"Graphics","E2_SOLO.PCX")).Palette;var resources=new Dictionary<int,List<IndexedImage>>{{28,Assets.Sph(Path.Combine(assets,"Graphics","EO1_SOLO.SPH"),palette)},{29,Assets.Sph(Path.Combine(assets,"Graphics","EO2_SOLO.SPH"),palette)},{38,Assets.Sph(Path.Combine(assets,"Graphics","EO4_SOLO.SPH"),palette)}};return SoloProgram.Read(File.ReadAllBytes(Path.Combine(p,"SoloEntry.seqbin")),File.ReadAllBytes(Path.Combine(p,"PracticeSetup.seqbin")),File.ReadAllBytes(Path.Combine(p,"PlayerLimits.seqbin")),resources,File.ReadAllLines(Path.Combine(assets,"Graphics","SCNTEXT.TXT")));};
            check("Solo exact prefixes, text bounds and corrected native title label routes",delegate {
                string p=Path.Combine(Path.GetDirectoryName(assets),"Programs");var palette=Assets.Pcx(Path.Combine(assets,"Graphics","E2_SOLO.PCX")).Palette;var resources=new Dictionary<int,List<IndexedImage>>{{28,Assets.Sph(Path.Combine(assets,"Graphics","EO1_SOLO.SPH"),palette)},{29,Assets.Sph(Path.Combine(assets,"Graphics","EO2_SOLO.SPH"),palette)},{38,Assets.Sph(Path.Combine(assets,"Graphics","EO4_SOLO.SPH"),palette)}};byte[][] bytes={File.ReadAllBytes(Path.Combine(p,"SoloEntry.seqbin")),File.ReadAllBytes(Path.Combine(p,"PracticeSetup.seqbin")),File.ReadAllBytes(Path.Combine(p,"PlayerLimits.seqbin"))};var text=File.ReadAllLines(Path.Combine(assets,"Graphics","SCNTEXT.TXT"));
                for(int k=0;k<3;k++){byte[] good=bytes[k];for(int at=0;at<good.Length;at+=4){byte[] bad=(byte[])good.Clone();bad[at]^=0x40;bytes[k]=bad;Reject(delegate{SoloProgram.Read(bytes[0],bytes[1],bytes[2],resources,text);});}bytes[k]=new byte[good.Length-1];Reject(delegate{SoloProgram.Read(bytes[0],bytes[1],bytes[2],resources,text);});bytes[k]=good;}Reject(delegate{SoloProgram.Read(bytes[0],bytes[1],bytes[2],resources,new string[20]);});
                string[] routes=File.ReadAllLines(Path.Combine(evidence,"Title-Routes-Oracle.csv"));Require(routes.Length==5&&routes[1]=="1,0044F09C,0044F0D4,0044F3F8"&&routes[2]=="2,0044F078,0044F090,0044F208"&&routes[3].EndsWith(",00450EC8")&&routes[4].EndsWith(",0044F148"),"Native label routing; Solo and Multiplayer must not be swapped");Require(text.Length==21&&loadSolo().Description(20).Contains("turn-based"),"Original Classic description");
            });
            check("Practice player clamp, draw commands and indexed pixels match12native states",delegate {
                var program=loadSolo();var card=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=new Color[256]};for(int i=0;i<card.Pixels.Length;i++)card.Pixels[i]=(byte)(1+(i*7)%255);var states=new Dictionary<int,SoloState>();int count=0;
                foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Solo-State-Raster-Oracle.csv"))){if(row.StartsWith("Case"))continue;string[] p=row.Split(',');var state=new SoloState(program);state.SelectPlayers(int.Parse(p[2]));state.SelectType(int.Parse(p[1]));Require(state.Players==int.Parse(p[3])&&state.Playable==(state.Type==20),"Native player clamp/playability");states.Add(int.Parse(p[0]),state);using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(program.Compose(card,state).Pixels)).Replace("-","")==p[4],"Native Practice raster");count++;}Require(count==12,"Grouped setup-state coverage");
                count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Solo-Draw-Oracle.csv"))){if(row.StartsWith("Case"))continue;string[] p=row.Split(',');var commands=program.Commands(states[int.Parse(p[0])],-1);var command=commands[int.Parse(p[4])];var button=program.Buttons[command.Index];Require(command.Index==int.Parse(p[5])&&button.X==int.Parse(p[6])&&button.Y==int.Parse(p[7])&&command.Frame==int.Parse(p[8])&&button.Group==int.Parse(p[9]),"Native Practice sprite command");count++;}Require(count==42,"Draw command coverage");
            });
            check("Practice button geometry and half-open hits match native edge sweep",delegate {
                var program=loadSolo();int count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Solo-Buttons-Oracle.csv"))){if(row.StartsWith("Index"))continue;string[] p=row.Split(',');var b=program.Buttons[int.Parse(p[0])];Require(b.X==int.Parse(p[1])&&b.Y==int.Parse(p[2])&&b.Action==int.Parse(p[3])&&b.Frame==int.Parse(p[4])&&b.Group==int.Parse(p[5])&&b.Bounds.Width==int.Parse(p[6])&&b.Bounds.Height==int.Parse(p[7]),"Setup button geometry");count++;}Require(count==14,"Setup button coverage");count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Solo-Hit-Oracle.csv"))){if(row.StartsWith("X"))continue;string[] p=row.Split(',');int hit=program.Hit(int.Parse(p[0]),int.Parse(p[1]));Require((hit<0?-2:program.Buttons[hit].Action)==int.Parse(p[2]),"Native setup hit");count++;}Require(count==3360,"Grouped setup boundary sweep");
            });
            check("Exact title prefixes and native menu entry states",delegate {
                var frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_TITLE.SPH"),Assets.Pcx(Path.Combine(assets,"Graphics","E_TITLE.PCX")).Palette);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");byte[] entry=File.ReadAllBytes(Path.Combine(programs,"MenuEntry.seqbin")),title=File.ReadAllBytes(Path.Combine(programs,"MenuTitle.seqbin"));TitleProgram.Read(entry,title,frames);
                for(int at=0;at<entry.Length;at+=4){byte[] bad=(byte[])entry.Clone();bad[at]^=0x40;Reject(delegate{TitleProgram.Read(bad,title,frames);});}for(int at=0;at<title.Length;at+=4){byte[] bad=(byte[])title.Clone();bad[at]^=0x40;Reject(delegate{TitleProgram.Read(entry,bad,frames);});}Reject(delegate{TitleProgram.Read(new byte[55],title,frames);});Reject(delegate{TitleProgram.Read(entry,new byte[167],frames);});
                string[] states=File.ReadAllLines(Path.Combine(evidence,"Menu-State-Oracle.csv"));Require(states.Length==4&&states[1]=="VICTORY,-1,1,2,0,0,0,0,0,0,39,4"&&states[2]=="LOSE,-1,1,2,0,0,0,0,0,0,39,4"&&states[3]=="DirectNoFade,123,123,123,123,123,123,123,0,0,39,4","Native reset/boundary cases");
                string[] trace=File.ReadAllLines(Path.Combine(evidence,"Menu-Entry-Oracle.csv"));Require(trace.Length==32,"Native entry trace coverage");for(int k=0;k<2;k++){string name=k==0?"VICTORY":"LOSE";Require(trace[1+k*15]==name+",0,Card,e_title","Title loader");for(int i=0;i<14;i++)Require(trace[2+k*15+i]==name+","+(i+1)+",Fade,"+(12+4*i),"Title fade order");}Require(trace[31]=="DirectNoFade,0,Card,e_title","Direct no-fade branch");
            });
            check("Title button geometry, split draw and half-open hits match original code",delegate {
                string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_TITLE.SPH"),Assets.Pcx(Path.Combine(assets,"Graphics","E_TITLE.PCX")).Palette);var menu=TitleProgram.Read(File.ReadAllBytes(Path.Combine(programs,"MenuEntry.seqbin")),File.ReadAllBytes(Path.Combine(programs,"MenuTitle.seqbin")),frames);int count=0;
                foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Menu-Buttons-Oracle.csv"))){if(row.StartsWith("Index"))continue;string[] p=row.Split(',');var b=menu.Buttons[int.Parse(p[0])];Require(b.X==int.Parse(p[1])&&b.Y==int.Parse(p[2])&&b.Action==int.Parse(p[3])&&b.Frame==int.Parse(p[4])&&b.Group==int.Parse(p[5])&&b.Bounds.Width==int.Parse(p[8])&&b.Bounds.Height==int.Parse(p[7]),"Native button geometry");count++;}Require(count==4,"Button coverage");
                count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Menu-Hit-Oracle.csv"))){if(row.StartsWith("X"))continue;string[] p=row.Split(',');int hit=menu.Hit(int.Parse(p[0]),int.Parse(p[1])),action=hit<0?-2:menu.Buttons[hit].Action;Require(action==int.Parse(p[2]),"Native half-open overlap/edge priority");count++;}Require(count==1104,"Grouped native boundary sweep");
                count=0;foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Menu-Draw-Oracle.csv"))){if(row.StartsWith("Selected"))continue;string[] p=row.Split(',');var b=menu.Buttons[int.Parse(p[0])];int half=int.Parse(p[1]);Require(int.Parse(p[2])==b.X+half*200&&int.Parse(p[3])==b.Y&&int.Parse(p[4])==b.Frame+half*4&&p[5]=="0"&&p[6]=="100"&&p[7]=="0","Native split sprite command");count++;}Require(count==8,"Menu draw coverage");
            });
            check("Title composition matches all four native unscaled zero-key raster outputs",delegate {
                var card=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=new Color[256]};for(int i=0;i<card.Pixels.Length;i++)card.Pixels[i]=(byte)(1+(i*7)%255);var frames=Assets.Sph(Path.Combine(assets,"Graphics","EO_TITLE.SPH"),card.Palette);string programs=Path.Combine(Path.GetDirectoryName(assets),"Programs");var menu=TitleProgram.Read(File.ReadAllBytes(Path.Combine(programs,"MenuEntry.seqbin")),File.ReadAllBytes(Path.Combine(programs,"MenuTitle.seqbin")),frames);int count=0;
                foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Menu-Composite-Oracle.csv"))){if(row.StartsWith("Selected"))continue;string[] p=row.Split(',');var actual=TitleProgram.Compose(card,frames,menu,int.Parse(p[0]));using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(actual.Pixels)).Replace("-","")==p[1],"Native unscaled indexed composition");count++;}Require(count==4,"Composed button coverage");
            });
            check("Bounded ending programs reject unsupported instructions/references",delegate {
                foreach(string name in MovieCatalog.Endings){byte[] bytes=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs",name+".seqbin"));var program=EndingProgram.Read(bytes);Require(program.Movie==name&&program.HoldMilliseconds==5000&&program.MenuPointer==0x44ef88,"Ending program decode");foreach(int at in new[]{0,4,8,12,16,20,24,28,32,36,40,44,48,52,56,60}){byte[] corrupt=(byte[])bytes.Clone();corrupt[at]^=0x40;Reject(delegate{EndingProgram.Read(corrupt);});}Reject(delegate{EndingProgram.Read(new byte[63]);});}
            });
            check("Managed ending prefix matches four native completion/skip traces",delegate {
                var all=new Dictionary<string,List<string>>();foreach(string row in File.ReadAllLines(Path.Combine(evidence,"Ending-Program-Oracle.csv"))){if(row.StartsWith("Outcome"))continue;string[] p=row.Split(',');string key=p[0]+p[1];if(!all.ContainsKey(key))all[key]=new List<string>();Require(int.Parse(p[2])==all[key].Count,"Native prefix order");all[key].Add(p[3]+","+p[4]);}
                foreach(string name in MovieCatalog.Endings)foreach(int status in new[]{6,9}){var timeline=new EndingTimeline(EndingProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs",name+".seqbin"))));timeline.Start();timeline.Advance(6240);Require(timeline.Phase==EndingPhase.Movie,"Ending must block on movie");timeline.Advance(60000);Require(timeline.Phase==EndingPhase.Movie,"Ending crossed pending movie");Require(timeline.CompleteMovie(),"Ending movie completion");timeline.Advance(680);Require(timeline.Phase==EndingPhase.Complete&&!timeline.Playing&&!timeline.CompleteMovie(),"Ending handoff/completion");var native=all[name+status];Require(native.Count==54&&timeline.Trace.Count==native.Count,"Ending trace coverage");for(int i=0;i<native.Count;i++)Require(native[i]==timeline.Trace[i],"Ending native step "+name+"/"+i);}Require(all.Count==4,"Native ending case count");
            });
            check("Ending wait/transition clock partitions and pause without progression",delegate {
                var program=EndingProgram.Read(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(assets),"Programs","VICTORY.seqbin")));var a=new EndingTimeline(program);var b=new EndingTimeline(program);a.Start();b.Start();for(int i=0;i<624;i++)a.Advance(10);b.Advance(6240);Require(a.Phase==b.Phase&&string.Join(";",a.Trace.ToArray())==string.Join(";",b.Trace.ToArray()),"Ending partition history");a.Pause();a.Advance(10000);Require(!a.CompleteMovie()&&a.Phase==EndingPhase.Movie,"Paused ending advanced");a.Resume();Require(a.CompleteMovie(),"Ending resume gate");a.Advance(679);Require(a.Phase==EndingPhase.MovieFade&&a.Factor==0,"Last fade factor must hold");a.Advance(1);Require(a.Phase==EndingPhase.Complete,"Fade final duration");a.Reset();Require(a.Phase==EndingPhase.Idle&&!a.Playing&&a.Trace.Count==0,"Ending reset");
            });
            check("Palette fades retain protected20entries and use native scale arithmetic",delegate {
                var colors=new Color[256];byte[] rgb=new byte[768];for(int i=0;i<256;i++){colors[i]=Color.FromArgb(i,255-i,i/2);rgb[i*3]=(byte)i;rgb[i*3+1]=(byte)(255-i);rgb[i*3+2]=(byte)(i/2);}foreach(int factor in new[]{0,12,16,60,64}){Color[] faded=ClassicPalette.FadeDisplay(colors,factor);byte[] nativeArithmetic=ClassicPalette.Fade(rgb,factor);for(int i=0;i<256;i++){if(i<10||i>245)Require(faded[i]==colors[i],"Protected palette index faded");else Require(faded[i].R==nativeArithmetic[3*i]&&faded[i].G==nativeArithmetic[3*i+1]&&faded[i].B==nativeArithmetic[3*i+2],"Display fade arithmetic");}}
            });
            check("Ending catalog and20fps route against original script/name evidence",delegate {
                int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Ending-Catalog.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');Require(MovieCatalog.IsEnding(p[0])&&MovieCatalog.FrameMilliseconds(p[0])==int.Parse(p[4])&&MovieCatalog.Directory(p[0],false)=="HWEBS"&&MovieCatalog.PaletteDirectory(p[0])=="HWEBS","Ending context metadata");byte[] script=File.ReadAllBytes(Path.Combine(evidence,p[0]=="VICTORY"?"Ending-VictoryScript.bin":"Ending-LossScript.bin"));Require(BitConverter.ToInt32(script,32)==6&&BitConverter.ToInt32(script,36)==int.Parse(p[1]),"Native ending operand");Require(!File.Exists(Path.Combine(assets,"LWEBS",p[0]+".PAT")),"Unexpected synthetic low ending");rows++;}byte[] clock=File.ReadAllBytes(Path.Combine(evidence,"Ending-MovieWait.bin"));Require(clock[11]==0x83&&clock[12]==0xf8&&clock[13]==50&&MovieCatalog.Names.Length==63&&rows==2,"Original50ms limiter/catalog");
            });
            check("Both ending movies against1341original decoder frames",delegate {
                var fixtures=new Dictionary<string,List<string[]>>();foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Ending-Frames-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');if(!fixtures.ContainsKey(p[0]))fixtures.Add(p[0],new List<string[]>());fixtures[p[0]].Add(p);}int total=0;
                foreach(string name in MovieCatalog.Endings){var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"HWEBS",name+".PAT")));Require(movie.DecodedWidth==320&&movie.DecodedHeight==240&&movie.Frames.Count==(name=="VICTORY"?661:680),"Native long ending count");for(int i=0;i<movie.Frames.Count;i++){var frame=movie.Frames[i];string[] row=fixtures[name][i];Require(int.Parse(row[1])==i&&frame.Offset==int.Parse(row[2])&&frame.PayloadBytes==int.Parse(row[3]),"Ending native frame bounds");using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(frame.Pixels)).Replace("-","")==row[4],"Ending native pixels "+name+"/"+i);total++;}}Require(total==1341,"Ending decoded coverage");
            });
            check("Ending60cue native polling/cleanup and26original PCM paths",delegate {
                int total=0,rows=0;foreach(string name in MovieCatalog.Endings){int count=name=="VICTORY"?661:680;List<SoundCue> cues;short[] mixed=ClipSound.Load(assets,name,count,50,out cues);Require(mixed.Length==(name=="VICTORY"?364376:374850),"Ending finite sample count");total+=cues.Count;var starts=new List<int>();foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Ending-Sequence-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');if(p[0]!=name)continue;if(p[1]=="Start"){int cue=int.Parse(p[2]);Require(cues[cue].ActualStart==int.Parse(p[3]),"Ending native onset");starts.Add(cue);}else Require(int.Parse(p[3])==count,"Ending native close cleanup");rows++;}Require(starts.Count==cues.Count,"Ending requested cue total");}
                int files=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Ending-Wave-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');Require(ClipSound.Pcm(File.ReadAllBytes(Path.Combine(assets,"SFX",p[0],p[1]))).Length==int.Parse(p[7]),"Ending PCM samples");files++;}Require(total==60&&rows==120&&files==26,"Ending sound fixture coverage");
            });
            check("20fps partitions, fractional sample placement and complete last-frame hold",delegate {
                var a=new ClipTimeline(661,50);var b=new ClipTimeline(661,50);a.Play();b.Play();for(int i=0;i<100;i++)a.Advance(10);for(int i=0;i<20;i++)b.Advance(50);Require(a.Frame==20&&b.Frame==20&&a.PositionMilliseconds==1000,"20fps time partitions");a.Synchronize(33049);Require(a.Frame==660&&!a.Completed,"20fps last-frame hold");a.Synchronize(33050);Require(a.Completed&&a.DurationMilliseconds==33050,"20fps finite end");int[] expected={0,551,1102,1653,2205};for(int i=0;i<5;i++)Require(ClipSound.FrameSample(i,50)==expected[i],"Fractional sample floor");Require(ClipSound.FrameSample(661,50)==364376,"No cumulative sample rounding");var cues=ClipSound.Sequence("A,1,2\nB,3,4\n*END*");short[] mixed=ClipSound.Mix(5,50,cues,delegate(string name){return new short[]{1234,-1234};});Require(mixed.Length==2756&&mixed[550]==0&&mixed[551]==1234&&mixed[552]==-1234&&mixed[1653]==1234,"20fps cue placement");Reject(delegate{ClipSound.Sequence("A,1024,1024\n*END*");});
            });
            check("Long ending bitmap cache keeps one current and one retiring frame",delegate {
                var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"HWEBS","LOSE.PAT")));var colors=PatMovie.ClipColors(File.ReadAllBytes(Path.Combine(assets,"HWEBS","LOSE.PAL")),File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL")),false);Bitmap previous=null;
                using(var cache=new ClipFrames(movie,colors)){for(int i=0;i<movie.Frames.Count;i++){Bitmap image=cache.Frame(i);Require(cache.CachedBitmaps==(i==0?1:2)&&object.ReferenceEquals(image,cache.Frame(i)),"Unbounded/repeated bitmap realization");Require(image.GetPixel(319,239).ToArgb()==colors[movie.Frames[i].Pixels[76799]].ToArgb(),"Cached ending image pixel");if(previous!=null)Require(previous.Width==320,"Borrowed image retired early");cache.ReleaseRetired();Require(cache.CachedBitmaps==1,"Retired bitmap retained");if(previous!=null){bool disposed=false;try{int width=previous.Width;}catch(ArgumentException){disposed=true;}Require(disposed,"Previous bitmap lifetime");}previous=image;}}bool finalDisposed=false;try{int width=previous.Width;}catch(ArgumentException){finalDisposed=true;}Require(finalDisposed,"Cache dispose retained final image");
            });
            check("Classic HUD dispatch against 486 native cases and 4032 ordered commands",delegate {
                int current=-1,order=0,cases=0,commands=0;List<ClassicHudCommand> plan=null;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Hud-Oracle.csv"))){if(line.StartsWith("Case"))continue;string[] p=line.Split(',');int id=int.Parse(p[0]);if(id!=current){if(plan!=null)Require(order==plan.Count,"HUD missing native draw command");current=id;order=0;cases++;plan=ClassicHud.Commands(int.Parse(p[1]),int.Parse(p[2])!=0,int.Parse(p[3]),int.Parse(p[4]),int.Parse(p[5]));}Require(order==int.Parse(p[6])&&order<plan.Count,"HUD command index");ClassicHudCommand command=plan[order];Require(command.Panel==(p[7]=="Panels")&&command.Frame==int.Parse(p[8])&&command.At==new Point(int.Parse(p[9]),int.Parse(p[10]))&&int.Parse(p[11])==100&&int.Parse(p[12])==0&&int.Parse(p[13])==0,"Native HUD command "+id+"/"+order);order++;commands++;}Require(order==plan.Count&&cases==486&&commands==4032,"HUD fixture coverage");
            });
            check("Original cinematic HUD shape and deliberately opaque stored backing",delegate {
                var source=Assets.Sph(Path.Combine(assets,"Graphics","E_CLAS2.SPH"),Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette);int stored=0,black=0;using(var hud=new ClassicHud(Path.Combine(assets,"Graphics"),source[0].Palette)){for(int i=0;i<4;i++){var f=source[i];Bitmap bitmap=hud.Panel(i);for(int y=0;y<f.Height;y++)for(int x=0;x<f.Width;x++){int at=y*f.Width+x;Color color=bitmap.GetPixel(x,y);Require(color.A==(f.Visible[at]?255:0),"HUD stored-run opacity");if(f.Visible[at]){Require(color.ToArgb()==f.Palette[f.Pixels[at]].ToArgb(),"HUD source color");stored++;if(f.Pixels[at]==0)black++;}}}}Require(stored==109467&&black>0,"HUD mask/backing fixture");
            });
            check("Progressive nearest-neighbor output matches original 640x480 scalers",delegate {
                var colors=new Color[256];for(int i=0;i<256;i++)colors[i]=Color.FromArgb(i,i,i);
                foreach(int factor in new[]{2,4}){int width=640/factor,height=480/factor;byte[] input=File.ReadAllBytes(Path.Combine(evidence,"Scale"+factor+"-Input.idx")),expected=File.ReadAllBytes(Path.Combine(evidence,"Scale"+factor+"-Output.idx"));Require(input.Length==width*height&&expected.Length==640*480,"Native scaler dimensions");using(var image=new IndexedImage{Width=width,Height=height,Pixels=input,Palette=colors}.Bitmap())using(var result=new Bitmap(640,480)){using(Graphics g=Graphics.FromImage(result)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;g.DrawImage(image,new Rectangle(0,0,640,480));}var data=result.LockBits(new Rectangle(0,0,640,480),System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);try{byte[] row=new byte[640*4];for(int y=0;y<480;y++){System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);for(int x=0;x<640;x++)Require(row[x*4]==expected[y*640+x]&&row[x*4+1]==row[x*4]&&row[x*4+2]==row[x*4],"Original scaler pixel "+factor+"/"+x+","+y);}}finally{result.UnlockBits(data);}}}
                Require(ClassicGeometry.ViewWidth==640&&ClassicGeometry.ViewHeight==480,"Viewport differs from source scaler");
            });
            check("Classic HUD eight-phase mapping follows pending shot and turn handoff",delegate {
                var game=new Game(950,RuleProfile.Recovered,OpponentKind.Original,1,true);game.AutoPlace();game.Start();Require(ClassicHud.Phase(game)==0,"Waiting player phase");game.Fire(game.Enemy.Ships[0].Cells[0]);Require(ClassicHud.Phase(game)==1,"Player firing phase");var pending=game.Pending;game.CompleteMovie(pending,ShotStage.Firing);Require(ClassicHud.Phase(game)==2,"Player travel phase");game.CompleteMovie(pending,ShotStage.Travel);Require(ClassicHud.Phase(game)==3,"Player feedback phase");game.CompleteMovie(pending,ShotStage.Feedback);Require(ClassicHud.Phase(game)==4,"Opponent wait phase");game.AdvanceOpponent();Require(ClassicHud.Phase(game)==5,"Opponent firing phase");pending=game.Pending;game.CompleteMovie(pending,ShotStage.Firing);Require(ClassicHud.Phase(game)==6,"Opponent travel phase");game.CompleteMovie(pending,ShotStage.Travel);Require(ClassicHud.Phase(game)==7,"Opponent feedback phase");game.CompleteMovie(pending,ShotStage.Feedback);Require(ClassicHud.Phase(game)==0,"Player return phase");
            });
            check("All 61 high-detail clips against 5641 original 320x240 decoder frames",delegate {
                var fixtures=new Dictionary<string,List<string[]>>();foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-High-Frames-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');if(!fixtures.ContainsKey(p[0]))fixtures.Add(p[0],new List<string[]>());fixtures[p[0]].Add(p);}
                int rows=0;foreach(string name in FiringClips.Names){var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"HWEBS",name+".PAT")));Require(movie.DecodedWidth==320&&movie.DecodedHeight==240&&movie.DisplayScale==2&&movie.Variant==2&&movie.Frames.Count==fixtures[name].Count,"High detail dimensions/count "+name);for(int i=0;i<movie.Frames.Count;i++){var frame=movie.Frames[i];string[] p=fixtures[name][i];Require(int.Parse(p[1])==i&&frame.Offset==int.Parse(p[2])&&frame.PayloadBytes==int.Parse(p[3])&&frame.Pixels.Length==76800,"High native frame span "+name);using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(frame.Pixels)).Replace("-","")==p[4],"High native pixels "+name+"/"+i);rows++;}}
                Require(fixtures.Count==61&&rows==5641,"High decoder fixture coverage");
            });
            check("High detail preserves source palettes and documented per-clip durations",delegate {
                var lowCounts=new Dictionary<string,int>();foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Impact-Clip-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');lowCounts.Add(p[0],int.Parse(p[2]));}
                int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"HWEBS-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');Require(int.Parse(p[1])==lowCounts[p[0]]+(p[0]=="BATT_DSH"?1:p[0]=="FRG_DSH"?-1:0)&&p[4]=="28-1E-02-02"&&p[5]=="True"&&p[6]=="True","High framing/timing/palette source "+p[0]);using(var sha=System.Security.Cryptography.SHA256.Create()){Require(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(assets,"HWEBS",p[0]+".PAT")))).Replace("-","")==p[7],"High source identity");Require(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(assets,"LWEBS",p[0]+".PAL")))).Replace("-","")==p[8],"Shared low/high palette identity");}rows++;}Require(rows==61,"High inventory coverage");
            });
            check("Independent high block stride and malformed/mixed variant rejection",delegate {
                byte[] b=new byte[2412];BitConverter.GetBytes(2404).CopyTo(b,0);b[4]=40;b[5]=30;b[6]=2;b[7]=2;for(int i=0;i<1200;i++){b[8+i*2]=0;b[9+i*2]=(byte)((i%40)+(i/40)*3);}BitConverter.GetBytes(uint.MaxValue).CopyTo(b,2408);var movie=PatMovie.Load(b);byte[] pixels=movie.Frames[0].Pixels;Require(pixels[0]==0&&pixels[319]==39&&pixels[239*320]==87&&pixels[239*320+319]==126&&pixels[8]==1&&pixels[8*320]==3&&movie.Frames[0].Opcodes[0]==1200,"High block rows/stride");
                b[8]=1;Reject(delegate{PatMovie.Load(b);});b[8]=0;b[7]=4;Reject(delegate{PatMovie.Load(b);});b[7]=2;byte[] mixed=new byte[2420];Array.Copy(b,mixed,2408);BitConverter.GetBytes(4).CopyTo(mixed,2408);mixed[2412]=20;mixed[2413]=15;mixed[2414]=4;mixed[2415]=4;BitConverter.GetBytes(uint.MaxValue).CopyTo(mixed,2416);Reject(delegate{PatMovie.Load(mixed);});
            });
            check("High-detail preference preserves legacy settings and unknown keys",delegate {
                Require(!DisplaySettings.Parse("movies=true\nsound=false\n").HighMovies,"Legacy detail changed");var prefs=DisplaySettings.Parse("highmovies=true\nmovies=true\nsound=false\ncustom=kept\n");var again=DisplaySettings.Parse(prefs.Serialize());Require(again.HighMovies&&again.MovieTurns&&!again.Sound&&again.Serialize().Contains("custom=kept"),"High preference round trip");Require(!DisplaySettings.Parse("highmovies=invalid\n").HighMovies,"Invalid high setting accepted");
            });
            check("Backdrop mask keeps grid lines opaque, surfaces at 70% and separate HUD holes open",delegate {
                var source=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));byte[] before=(byte[])source.Pixels.Clone();
                using(var foreground=ClassicBackdrop.Foreground(source)){
                    foreach(Point p in new[]{new Point(223,305),new Point(437,162),new Point(28,305),new Point(437,32)})Require(foreground.GetPixel(p.X,p.Y).ToArgb()==source.Palette[source.Pixels[p.Y*640+p.X]].ToArgb(),"Protected board/HUD pixel "+p);
                    foreach(Point p in new[]{new Point(300,80),new Point(10,240),new Point(630,240),new Point(10,20),new Point(100,100),new Point(500,350),new Point(400,450)})Require(foreground.GetPixel(p.X,p.Y).A==0,"Background was not opened "+p);
                    foreach(Point p in new[]{new Point(145,318),new Point(359,175)}){Color color=foreground.GetPixel(p.X,p.Y),original=source.Palette[source.Pixels[p.Y*640+p.X]];Require(color.A==179&&color.R==original.R&&color.G==original.G&&color.B==original.B,"Surface opacity/RGB differs "+p);}
                }
                for(int i=0;i<before.Length;i++)Require(before[i]==source.Pixels[i],"Original indexed data modified");Require(source.Visible==null,"Original visibility data modified");Reject(delegate{ClassicBackdrop.Foreground(new IndexedImage{Width=1,Height=1,Pixels=new byte[1]});});
            });
            check("Backdrop cover keeps movie aspect ratio and centered bounded cropping",delegate {
                foreach(RectangleF area in new[]{new RectangleF(0,0,640,512),new RectangleF(0,0,1920,1080),new RectangleF(12,9,747,430)}){RectangleF fit=ClassicBackdrop.Cover(new Size(160,120),area);Require(fit.Left<=area.Left+.01f&&fit.Top<=area.Top+.01f&&fit.Right>=area.Right-.01f&&fit.Bottom>=area.Bottom-.01f,"Backdrop leaves gaps");Require(Math.Abs(fit.Width/fit.Height-4f/3)<.00001f&&Math.Abs(fit.Left+fit.Width/2-area.Left-area.Width/2)<.01f&&Math.Abs(fit.Top+fit.Height/2-area.Top-area.Height/2)<.01f,"Backdrop stretched/offcenter");}
                bool rejected=false;try{ClassicBackdrop.Cover(Size.Empty,new RectangleF(0,0,1,1));}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Invalid movie dimensions accepted");
            });
            check("Independent signed 16-bit cue conversion and alignment rejection",delegate {
                byte[] b=new byte[50];Array.Copy(CueWaveFixture(),b,44);BitConverter.GetBytes(42).CopyTo(b,4);BitConverter.GetBytes(22050).CopyTo(b,28);W16(b,32,2);W16(b,34,16);BitConverter.GetBytes(6).CopyTo(b,40);W16(b,44,32768);W16(b,46,12345);W16(b,48,32767);short[] decoded=ClipSound.Pcm(b);Require(decoded.Length==3&&decoded[0]==short.MinValue&&decoded[1]==12345&&decoded[2]==short.MaxValue,"Signed PCM values/byte order");b[40]=5;Reject(delegate{ClipSound.Pcm(b);});b[40]=6;b[32]=1;Reject(delegate{ClipSound.Pcm(b);});
            });
            check("Impact selectors against 252 original hit/miss/sink cases",delegate {
                int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Impact-Oracle.csv"))){if(line.StartsWith("Kind"))continue;string[] p=line.Split(',');int type=int.Parse(p[1]),length=int.Parse(p[2]),remaining=int.Parse(p[3]),weapon=int.Parse(p[4]),roll=int.Parse(p[5]);int actual=p[0]=="Hit"?ClassicImpact.Hit(type,length,remaining,weapon):p[0]=="Sink"?ClassicImpact.Sink(type):ClassicImpact.Miss(weapon,roll);int draws=p[0]=="Miss"&&(weapon>=14&&weapon<=18||weapon==25||weapon==26)?1:0;Require(actual==int.Parse(p[6])&&draws==int.Parse(p[7])&&int.Parse(p[8])==1,"Native selector "+line);rows++;}Require(rows==252,"Impact fixture total");
            });
            check("Native movie names identify every impact and corrected travel clip",delegate {
                var names=new Dictionary<int,string>();foreach(string file in new[]{"Movie-Names.csv","Classic-LateMovieNames.csv"})foreach(string line in File.ReadAllLines(Path.Combine(evidence,file))){if(line.StartsWith("\"MovieID\""))continue;string[] p=line.Replace("\"","").Split(',');names.Add(int.Parse(p[0]),p[1]);}
                var reachable=new HashSet<string>();foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Impact-Oracle.csv"))){if(line.StartsWith("Kind"))continue;int id=int.Parse(line.Split(',')[6]);if(id==0)continue;Require(ClassicSequence.MovieName(id)==names[id],"Native impact movie name "+id);reachable.Add(names[id]);}
                Require(ClassicSequence.MovieName(12)==names[12]&&names[12]=="MIS_FLY2"&&ClassicSequence.MovieName(13)=="MIS_FLY3","Travel/miss movie IDs were conflated");Require(reachable.Count==33&&new HashSet<string>(FiringClips.Names).Count==61,"Impact/catalog names");
            });
            check("61 original combat clips: 5641 native frames, 404 cues, 232 PCM paths",delegate {
                var movies=new Dictionary<string,PatMovie>();var cues=new Dictionary<string,List<SoundCue>>();int frames=0,totalCues=0;
                foreach(string name in FiringClips.Names){var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAT")));movies.Add(name,movie);frames+=movie.Frames.Count;List<SoundCue> sequence;ClipSound.Load(assets,name,movie.Frames.Count,out sequence);cues.Add(name,sequence);totalCues+=sequence.Count;}
                Require(movies.Count==61&&frames==5641&&totalCues==404,"Impact library totals");int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Impact-Frames-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');var f=movies[p[0]].Frames[int.Parse(p[1])];Require(f.Offset==int.Parse(p[2])&&f.PayloadBytes==int.Parse(p[3]),"Impact native frame span");using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(f.Pixels)).Replace("-","")==p[4],"Impact native pixels "+p[0]+"/"+p[1]);rows++;}Require(rows==5641,"Impact frame coverage");
                int starts=0,stops=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Impact-Sequence-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');if(p[1]=="Start"){Require(cues[p[0]][int.Parse(p[2])].ActualStart==int.Parse(p[3]),"Impact cue onset");starts++;}else {Require(int.Parse(p[3])==movies[p[0]].Frames.Count,"Impact cue cleanup");stops++;}}Require(starts==404&&stops==404,"Impact cue coverage");
                int files=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Impact-Wave-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');Require(ClipSound.Pcm(File.ReadAllBytes(Path.Combine(assets,"SFX",p[0],p[1]))).Length==int.Parse(p[7]),"Impact PCM samples");files++;}Require(files==232,"Impact PCM coverage");
            });
            check("Default-setting impact and terminal sink wait, pause and once-only completion",delegate {
                var g=new Game(940,RuleProfile.Recovered,OpponentKind.Original,0,true);g.AutoPlace();g.Start();Point target=g.Enemy.Ships[0].Cells[0];var ship=g.Enemy.Ships[0];Require(g.Fire(target),"Default reservation");var pending=g.Pending;g.CompleteMovie(pending,ShotStage.Firing);g.CompleteMovie(pending,ShotStage.Travel);Require(pending.Stage==ShotStage.Feedback&&pending.FeedbackId!=0&&pending.FeedbackId!=137&&ship.Hits==1&&g.Phase==Phase.PlayerTurn,"Default impact movie omitted");g.Paused=true;Require(!g.CompleteMovie(pending,ShotStage.Feedback),"Paused impact completed");g.Paused=false;Require(g.CompleteMovie(pending,ShotStage.Feedback)&&g.Phase==Phase.OpponentTurn&&!g.CompleteMovie(pending,ShotStage.Feedback),"Impact handoff/stale guard");
                for(int level=0;level<4;level++){g=new Game(941+level,RuleProfile.Recovered,OpponentKind.Original,level,true);g.AutoPlace();g.Start();g.Enemy=new Board();g.Enemy.Place(new Point(0,0),1,true,"Last",1);g.Fire(new Point(0,0));pending=g.Pending;g.CompleteMovie(pending,ShotStage.Firing);g.CompleteMovie(pending,ShotStage.Travel);Require(pending.Result.Sunk&&pending.MovieName=="BATTSINK"&&g.Phase==Phase.PlayerTurn&&g.Enemy.AllSunk,"Sink ended game before playback");g.CompleteMovie(pending,ShotStage.Feedback);Require(g.Phase==Phase.Finished&&g.Pending==null,"Sink movie did not finish victory");}
            });
            check("Original solo phase and unavailable-movie transitions (528 controlled cases)",delegate {
                int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Phase-Oracle.csv"))){if(line.StartsWith("Stage"))continue;string[] p=line.Split(',');int stage=int.Parse(p[0]),weapon=int.Parse(p[1]),roll=int.Parse(p[2]),unavailable=int.Parse(p[3]);bool travel=stage==1||stage==5;int next=stage==7?0:stage+1,impacts=stage==2||stage==6?1:0;if(travel&&unavailable==1){next++;impacts++;}Require(int.Parse(p[4])==next&&int.Parse(p[5])==(travel?ClassicSequence.Travel(weapon,roll):0)&&int.Parse(p[6])==impacts&&int.Parse(p[7])==(travel&&weapon>=14&&weapon<=18?1:0),"Native phase/availability output");rows++;}Require(rows==528,"Phase fixture count");
            });
            check("Complete 28-clip shot library: 2179 native frames, 117 cues and 84 PCM files",delegate {
                var movies=new Dictionary<string,PatMovie>();var cues=new Dictionary<string,List<SoundCue>>();int frames=0,totalCues=0;
                foreach(string name in FiringClips.SequenceNames){var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAT")));movies.Add(name,movie);frames+=movie.Frames.Count;List<SoundCue> sequence;ClipSound.Load(assets,name,movie.Frames.Count,out sequence);cues.Add(name,sequence);totalCues+=sequence.Count;}
                Require(movies.Count==28&&frames==2179&&totalCues==117,"Shot library totals");int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Shot-Frames-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');var f=movies[p[0]].Frames[int.Parse(p[1])];using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(f.Pixels)).Replace("-","")==p[4],"Shot frame "+p[0]+"/"+p[1]);rows++;}Require(rows==2179,"Shot decoder coverage");
                int starts=0,stops=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Combat-Sequence-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');if(p[1]=="Start"){Require(cues[p[0]][int.Parse(p[2])].ActualStart==int.Parse(p[3]),"Shot cue onset");starts++;}else{Require(int.Parse(p[3])==movies[p[0]].Frames.Count,"Shot cue cleanup");stops++;}}Require(starts==117&&stops==117,"Shot cue fixture coverage");
                int files=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Combat-Wave-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');Require(ClipSound.Pcm(File.ReadAllBytes(Path.Combine(assets,"SFX",p[0],p[1]))).Length==int.Parse(p[7]),"Shot PCM count");files++;}Require(files==84,"Shot PCM paths");
            });
            check("Movie shot reserves without damage, impacts once after travel and completes feedback",delegate {
                var g=new Game(920,RuleProfile.Recovered,OpponentKind.Original,1,true);g.AutoPlace();g.Start();Point target=g.Enemy.Ships[0].Cells[0];var ship=g.Enemy.Ships[0];int events=g.Events.Count;
                Require(g.Fire(target)&&g.Pending!=null&&ship.Hits==0&&!g.Enemy.Shots[target.X,target.Y],"Reservation applied premature damage");var pending=g.Pending;
                Require(!g.Fire(new Point(9,9))&&!g.AdvanceOpponent()&&!g.SelectShooter(0)&&g.Events.Count==events,"Busy input gate");
                Require(g.CompleteMovie(pending,ShotStage.Firing)&&pending.Stage==ShotStage.Travel&&ship.Hits==0,"Firing completion");Require(!g.CompleteMovie(pending,ShotStage.Firing),"Stale firing callback advanced travel");
                g.TogglePause();Require(!g.CompleteMovie(pending,ShotStage.Travel)&&ship.Hits==0,"Paused movie applied impact");g.TogglePause();
                Require(g.CompleteMovie(pending,ShotStage.Travel)&&ship.Hits==1&&pending.Stage==ShotStage.Feedback&&pending.MovieName=="CL_HIT"&&g.Phase==Phase.PlayerTurn,"Travel impact/feedback order");Require(!g.CompleteMovie(pending,ShotStage.Travel)&&ship.Hits==1,"Duplicate impact");
                Require(g.CompleteMovie(pending,ShotStage.Feedback)&&g.Pending==null&&g.Phase==Phase.OpponentTurn&&g.Events.Count==events+1,"Feedback turn handoff");Require(!g.CompleteMovie(pending,ShotStage.Feedback),"Old shot callback reused");
            });
            check("Feedback subset, default opt-in and movie preference round trip",delegate {
                var board=new Board();board.Place(new Point(0,0),5,true,"Battleship",1);Ship ship=board.Ships[0];ship.Hits=1;Require(ClassicImpact.Select(0,new Shot{Hit=true},ship,14,0)==45&&ClassicImpact.Select(1,new Shot{Hit=true},ship,14,0)==137&&ClassicImpact.Select(2,new Shot(),null,14,0)==136,"Hit/miss level policy");Require(ClassicImpact.Select(3,new Shot{Hit=true,Sunk=true},ship,14,0)==47&&ClassicImpact.Select(3,new Shot{Hit=true,Repeated=true},ship,14,0)==0,"Sink/repeated occupied gate");
                var legacy=new Game(921,RuleProfile.Legacy,OpponentKind.Prototype,0,true);Require(!legacy.MovieTurns,"Prototype acquired movies");Require(!new Game(921).MovieTurns&&!DisplaySettings.Parse("sound=false\n").MovieTurns,"Existing default changed");
                var config=DisplaySettings.Parse("movies=true\nsound=false\ncustom=kept\n");Require(config.MovieTurns&&!config.Sound&&DisplaySettings.Parse(config.Serialize()).MovieTurns&&config.Serialize().Contains("custom=kept"),"Movie preference compatibility");
            });
            check("Cinematic and instant histories preserve targets, weapons, damage and turns",delegate {
                for(int level=0;level<4;level++){var instant=new Game(925+level,RuleProfile.Recovered,OpponentKind.Original,level);var movie=new Game(925+level,RuleProfile.Recovered,OpponentKind.Original,level,true);instant.AutoPlace();movie.AutoPlace();instant.Start();movie.Start();
                    for(int turn=0;turn<15;turn++){Point target=new Point(turn/10,turn%10);Require(instant.Fire(target)&&movie.Fire(target),"Parallel player action");DrainMovies(movie);Require(instant.LastPlayerShot.Description==movie.LastPlayerShot.Description&&instant.LastPlayerFire.WeaponId==movie.LastPlayerFire.WeaponId&&instant.LastPlayerFire.MovieId==movie.LastPlayerFire.MovieId,"Player stream/damage changed");Require(instant.AdvanceOpponent()&&movie.AdvanceOpponent(),"Parallel opponent action");DrainMovies(movie);Require(instant.LastEnemyShot.Description==movie.LastEnemyShot.Description&&instant.LastEnemyFire.WeaponId==movie.LastEnemyFire.WeaponId&&instant.Phase==movie.Phase&&instant.Turn==movie.Turn,"Opponent history changed");}
                }
            });
            check("Cinematic terminal outcomes and stale cross-game callbacks",delegate {
                var g=new Game(930,RuleProfile.Recovered,OpponentKind.Original,3,true);g.AutoPlace();g.Start();g.Enemy=new Board();g.Enemy.Place(new Point(0,0),1,true,"Last",1);Require(g.Fire(new Point(0,0)),"Terminal reservation");var stale=g.Pending;Require(g.Phase!=Phase.Finished,"Victory before impact");DrainMovies(g);Require(g.Phase==Phase.Finished&&g.Enemy.AllSunk&&g.Pending==null&&!g.AdvanceOpponent(),"Terminal impact");
                var next=new Game(931,RuleProfile.Recovered,OpponentKind.Original,1,true);next.AutoPlace();next.Start();next.Fire(new Point(0,0));Require(!next.CompleteMovie(stale,ShotStage.Firing)&&next.Pending.Stage==ShotStage.Firing,"Cross-game callback accepted");
                g=new Game(932,RuleProfile.Recovered,OpponentKind.Prototype,1,true);g.AutoPlace();g.Start();g.Player=new Board();g.Player.Place(new Point(0,1),1,true,"Last",1);for(int x=0;x<10;x++)for(int y=0;y<10;y++)g.Player.Shots[x,y]=!(x==0&&y==1);g.Phase=Phase.OpponentTurn;Require(g.AdvanceOpponent()&&g.Player.Ships[0].Hits==0,"Opponent reservation");DrainMovies(g);Require(g.Phase==Phase.Finished&&g.Player.AllSunk,"Cinematic defeat");
            });
            check("All 20 reachable firing clips and 1460 frames against original decoder",delegate {
                var reachable=new HashSet<string>();foreach(int type in new[]{7,1,2,12,4})foreach(bool ai in new[]{false,true})for(int roll=0;roll<50;roll++)for(int variant=0;variant<4;variant++)reachable.Add(ClassicWeapons.Clip(ClassicWeapons.Movie(type,ClassicWeapons.Weapon(type,ai,roll),variant)));
                Require(reachable.SetEquals(FiringClips.FiringNames)&&reachable.Count==20,"Reachable catalog differs");
                var movies=new Dictionary<string,PatMovie>();foreach(string name in FiringClips.FiringNames)movies.Add(name,PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAT"))));
                int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Firing-Frames-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');var f=movies[p[0]].Frames[int.Parse(p[1])];Require(f.Offset==int.Parse(p[2])&&f.PayloadBytes==int.Parse(p[3]),"Native firing span");using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(f.Pixels)).Replace("-","")==p[4],"Native firing pixels "+p[0]+"/"+p[1]);rows++;}
                int total=0;foreach(var movie in movies.Values)total+=movie.Frames.Count;Require(rows==1460&&rows==total,"Native firing frame total");
            });
            check("Original SEQ one-cue-per-frame timing and close cleanup fixtures",delegate {
                var cues=new Dictionary<string,List<SoundCue>>();var counts=new Dictionary<string,int>();int total=0;
                foreach(string name in FiringClips.FiringNames){var list=ClipSound.Sequence(File.ReadAllText(Path.Combine(assets,"SFX",name,name+".SEQ")));cues.Add(name,list);counts.Add(name,PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAT"))).Frames.Count);total+=list.Count;}
                int starts=0,stops=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Sequence-Oracle.csv"))){if(line.StartsWith("Clip"))continue;string[] p=line.Split(',');int index=int.Parse(p[2]),frame=int.Parse(p[3]);Require(int.Parse(p[4])==index+1,"Dummy handle identity");if(p[1]=="Start"){Require(cues[p[0]][index].ActualStart==frame,"Original cue onset");starts++;}else {Require(p[1]=="Stop"&&frame==counts[p[0]],"Valid handle stopped during frame polling");stops++;}}
                Require(total==104&&starts==total&&stops==total,"Sequence fixture coverage");
                var tied=ClipSound.Sequence("ONE,0,1\nTWO,0,1\nTHREE,0,1\n*END*\n");Require(tied[0].ActualStart==0&&tied[1].ActualStart==1&&tied[2].ActualStart==2&&tied[2].End==1,"Tie spacing/end preservation");
            });
            check("Independent cue PCM sample conversion and malformed RIFF/SEQ bounds",delegate {
                byte[] b=CueWaveFixture();short[] samples=ClipSound.Pcm(b);Require(samples.Length==3&&samples[0]==-32768&&samples[1]==0&&samples[2]==32512,"Unsigned PCM conversion");
                foreach(int at in new[]{4,20,22,24,32,34,40}){b=CueWaveFixture();b[at]=99;Reject(delegate{ClipSound.Pcm(b);});}Reject(delegate{ClipSound.Pcm(new byte[1]);});
                foreach(string text in new[]{"A,0,1", "../A,0,1\n*END*", "A,1,0,2\n*END*", "A,-1,0\n*END*", "A,5,2\n*END*", "A,0,1\n*END*\nB,0,1", "*WAIT*"})Reject(delegate{ClipSound.Sequence(text);});
                var longList=new StringBuilder();for(int i=0;i<64;i++)longList.AppendLine("A,0,1");longList.AppendLine("*END*");Reject(delegate{ClipSound.Sequence(longList.ToString());});
            });
            check("Finite overlapping cue mixing, preserved end fields, saturation and eight-voice pool",delegate {
                byte[] request=File.ReadAllBytes(Path.Combine(evidence,"Sound-CueRequest.bin"));Require(request[0x82]==0x6a&&request[0x83]==8&&request[0x84]==0x6a&&request[0x85]==0&&request[0xbb]==0x6a&&request[0xbc]==0,"Native pool range/finite Play argument");
                var cues=ClipSound.Sequence("A,0,1\nB,0,1\nA,0,1\n*END*");short[] a=new short[3*441],b=new short[2*441];for(int i=0;i<a.Length;i++)a[i]=1000;for(int i=0;i<b.Length;i++)b[i]=2000;
                var mixed=ClipSound.Mix(5,cues,delegate(string name){return name=="A"?a:b;});Require(mixed.Length==5*441&&mixed[0]==1000&&mixed[441]==3000&&mixed[882]==4000&&mixed[1323]==1000&&mixed[2204]==1000,"Finite sample overlap/end semantics");
                var records=new StringBuilder();for(int i=0;i<9;i++)records.AppendLine("S"+i+",0,0");records.AppendLine("NEW,12,0");records.AppendLine("*END*");var pool=ClipSound.Sequence(records.ToString());short[] longWave=new short[10*441];for(int i=0;i<longWave.Length;i++)longWave[i]=30000;
                var saturated=ClipSound.Mix(15,pool,delegate(string name){return longWave;});Require(pool[8].Slot==-1&&pool[9].Slot==0&&saturated[441]==32767,"Pool drop/reuse or saturation");
            });
            check("All 71 original cue files decode and 20 finite soundtracks compose",delegate {
                int files=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Firing-Wave-Inventory.csv"))){if(line.StartsWith("\"Clip\""))continue;string[] p=line.Replace("\"","").Split(',');var samples=ClipSound.Pcm(File.ReadAllBytes(Path.Combine(assets,"SFX",p[0],p[1])));Require(samples.Length==int.Parse(p[7]),"Original sample count "+p[0]+"/"+p[1]);files++;}Require(files==71,"Cue asset count");
                int drops=0;foreach(string name in FiringClips.FiringNames){int frames=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAT"))).Frames.Count;List<SoundCue> cues;short[] pcm=ClipSound.Load(assets,name,frames,out cues);Require(pcm.Length==frames*441,"Soundtrack finite duration");bool nonzero=false;foreach(short value in pcm)if(value!=0){nonzero=true;break;}Require(nonzero,"Empty soundtrack");foreach(var cue in cues)if(cue.Slot<0)drops++;}Require(drops==1,"Expected one eight-voice overflow in supplied library");
            });
            check("Device-clock timeline synchronization is monotonic and respects pause/completion",delegate {
                var timeline=new ClipTimeline(46);timeline.Play();timeline.Synchronize(480);Require(timeline.Frame==12,"Device-frame conversion");timeline.Synchronize(400);Require(timeline.Frame==12,"Device clock regressed video");timeline.Pause();timeline.Synchronize(800);Require(timeline.Frame==12,"Paused sync advanced");timeline.Play();timeline.Synchronize(1840);Require(timeline.Completed&&!timeline.Playing&&timeline.Frame==45,"Device finite completion");
            });
            check("Original PAT movie against 46 original-decoder frame hashes and lengths",delegate {
                byte[] input=File.ReadAllBytes(Path.Combine(assets,"LWEBS","BATFIRE1.PAT"));
                var movie=PatMovie.Load(input);string[] rows=File.ReadAllLines(Path.Combine(evidence,"BATFIRE1-Frames-Oracle.csv"));
                Require(movie.Frames.Count==46&&rows.Length==47,"Original frame count");
                int[] counts=new int[13];
                for(int i=0;i<46;i++){
                    PatFrame f=movie.Frames[i];string[] p=rows[i+1].Split(',');
                    Require(int.Parse(p[0])==i&&int.Parse(p[1])==f.Offset&&int.Parse(p[2])==f.PayloadBytes&&int.Parse(p[3])==f.PayloadBytes+4,"Original frame spans");
                    using(var sha=System.Security.Cryptography.SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(f.Pixels)).Replace("-","")==p[4],"Original pixels at frame "+i);
                    for(int op=0;op<13;op++)counts[op]+=f.Opcodes[op];
                }
                int[] expected={1626,11,682,958,0,6190,150,836,387,540,1129,1197,94};
                for(int i=0;i<13;i++)Require(counts[i]==expected[i],"Movie opcode distribution");
                foreach(int i in new[]{0,20,45})Require(Convert.ToBase64String(movie.Frames[i].Pixels)==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(evidence,"BATFIRE1-"+i.ToString("D2")+".idx"))),"Independent pixel fixture");
            });
            check("All 13 PAT block operations and 256 quartet patterns against original code",delegate {
                var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(evidence,"PAT-AllOps-Input.pat")));
                Require(movie.Frames.Count==2&&Convert.ToBase64String(movie.Frames[1].Pixels)==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(evidence,"PAT-AllOps-Oracle.idx"))),"All-opcode pixel fixture");
                foreach(int count in movie.Frames[1].Opcodes)Require(count>0,"Unexercised opcode");
                byte[] fixture=File.ReadAllBytes(Path.Combine(evidence,"PAT-Patterns-Input.bin"));byte[] complete=new byte[fixture.Length+4];fixture.CopyTo(complete,0);BitConverter.GetBytes(uint.MaxValue).CopyTo(complete,fixture.Length);
                var patterns=PatMovie.Load(complete);int total=0;
                foreach(string line in File.ReadAllLines(Path.Combine(evidence,"PAT-Patterns-Oracle.csv"))){if(line.StartsWith("Code"))continue;string[] p=line.Split(',');int code=int.Parse(p[0]),at=(code/20*8)*160+(code%20)*8;for(int x=0;x<4;x++)Require(patterns.Frames[0].Pixels[at+x]==int.Parse(p[x+1]),"Quartet order/literal escape "+code);total++;}
                Require(total==256,"Pattern fixture size");
            });
            check("PAT length, truncation, terminal, opcode, variant and frame-count bounds",delegate {
                byte[] good=File.ReadAllBytes(Path.Combine(assets,"LWEBS","BATFIRE1.PAT"));
                Reject(delegate{PatMovie.Load(new byte[0]);});
                var b=(byte[])good.Clone();BitConverter.GetBytes(uint.MaxValue-1).CopyTo(b,0);Reject(delegate{PatMovie.Load(b);});
                foreach(int at in new[]{4,5,6,7,8}){b=(byte[])good.Clone();b[at]=at==8?(byte)52:(byte)0;Reject(delegate{PatMovie.Load(b);});}
                b=new byte[good.Length-4];Array.Copy(good,b,b.Length);Reject(delegate{PatMovie.Load(b);});
                b=(byte[])good.Clone();BitConverter.GetBytes(0).CopyTo(b,b.Length-4);Reject(delegate{PatMovie.Load(b);});
                b=new byte[good.Length+1];good.CopyTo(b,0);Reject(delegate{PatMovie.Load(b);});
                var many=new List<byte>();byte[] empty=new byte[308];BitConverter.GetBytes(304).CopyTo(empty,0);new byte[]{20,15,4,4}.CopyTo(empty,4);for(int i=8;i<308;i++)empty[i]=44;
                for(int i=0;i<1024;i++)many.AddRange(empty);many.AddRange(BitConverter.GetBytes(uint.MaxValue));Require(PatMovie.Load(many.ToArray()).Frames.Count==1024,"Frame-count upper boundary");many.InsertRange(0,empty);Reject(delegate{PatMovie.Load(many.ToArray());});
            });
            check("Both original movie palette branches preserve untouched entries",delegate {
                byte[] pal=File.ReadAllBytes(Path.Combine(assets,"LWEBS","BATFIRE1.PAL")),island=File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL"));
                var baseline=ClassicPalette.Gameplay(island);var original=(byte[])pal.Clone();
                foreach(bool shared in new[]{false,true}){
                    Color[] colors=PatMovie.ClipColors(pal,island,shared);int first=shared?20:10,last=shared?137:245;
                    for(int i=0;i<256;i++){
                        Color expected=i<first||i>last?baseline[i]:Color.FromArgb(pal[i*3]*4,pal[i*3+1]*4,pal[i*3+2]*4);
                        Require(colors[i].ToArgb()==expected.ToArgb(),"Clip palette boundary/RGB conversion "+i);
                    }
                }
                Require(Convert.ToBase64String(pal)==Convert.ToBase64String(original),"Source palette mutated");
                Reject(delegate{PatMovie.ClipColors(new byte[1],island,false);});pal[60]=64;Reject(delegate{PatMovie.ClipColors(pal,island,false);});
            });
            check("Clip timeline frame holds, completion, partitions, pause/step/reset and bounded catch-up",delegate {
                var a=new ClipTimeline(46);a.Play();Require(!a.Advance(39)&&a.Frame==0,"First frame hold");Require(a.Advance(1)&&a.Frame==1,"40 ms transition");
                var b=new ClipTimeline(46);b.Play();for(int i=0;i<46;i++){b.Advance(16);b.Advance(24);}Require(b.Frame==45&&b.Completed&&!b.Playing,"Finite completion/final-frame hold");
                a=new ClipTimeline(46);a.Play();for(int i=0;i<46;i++)a.Advance(40);Require(a.Frame==b.Frame&&a.Completed==b.Completed,"Frame partitions diverged");
                a.Play();Require(a.Frame==0&&!a.Completed,"Replay reset");a.Advance(10000);Require(a.Frame==5,"Catch-up bound");a.Pause();a.Advance(200);Require(a.Frame==5,"Pause advanced");a.Step(-1);Require(a.Frame==4&&!a.Playing,"Step");a.End();Require(a.Frame==45&&a.Completed,"Skip end");a.Reset();Require(a.Frame==0&&!a.Playing&&!a.Completed,"Reset");
                var single=new ClipTimeline(1);single.Play();single.Advance(40);Require(single.Completed&&single.Frame==0,"Single-frame completion");
            });
            check("Palette patches and 65 fade outputs against original x86 code",delegate {
                string directory=Path.Combine(evidence,"Palette");
                byte[] input=File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL"));
                byte[] expected=File.ReadAllBytes(Path.Combine(directory,"Patched-Island.rgb"));
                byte[] patched=ClassicPalette.Patched(input);
                Require(Convert.ToBase64String(patched)==Convert.ToBase64String(expected),"Native palette patch bytes");
                for(int factor=0;factor<=64;factor++) {
                    byte[] faded=ClassicPalette.Fade(patched,factor);
                    byte[] native=File.ReadAllBytes(Path.Combine(directory,"Fade-"+factor.ToString("D2")+".rgb"));
                    Require(Convert.ToBase64String(faded)==Convert.ToBase64String(native),"Native fade arithmetic at "+factor);
                }
                Require(input[30]!=patched[30],"Source palette mutated or fixture patch absent");
                Reject(delegate{ClassicPalette.Patched(new byte[767]);});
                Reject(delegate{ClassicPalette.Fade(input,65);});
            });
            check("Recovered gameplay RGB entries and deterministic protected endpoints",delegate {
                byte[] raw=File.ReadAllBytes(Path.Combine(assets,"Graphics","ISLAND.COL"));
                byte[] expected=File.ReadAllBytes(Path.Combine(evidence,"Palette","Patched-Island.rgb"));
                var colors=ClassicPalette.Gameplay(raw);
                for(int i=10;i<=245;i++)Require(colors[i]==Color.FromArgb(expected[i*3],expected[i*3+1],expected[i*3+2]),"Mutable native RGB entry "+i);
                Require(colors[0].ToArgb()==Color.Black.ToArgb()&&colors[255].ToArgb()==Color.White.ToArgb(),"Protected black/white");
                Require(colors[1]==Color.FromArgb(128,0,0)&&colors[249]==Color.FromArgb(255,0,0),"Captured deterministic endpoints");
                byte[] endpoints=File.ReadAllBytes(Path.Combine(evidence,"Palette","Observed-Reserved-Endpoints.rgb"));
                Require(endpoints.Length==60,"Endpoint observation fixture");int offset=0;
                for(int i=0;i<256;i++)if(i<10||i>=246) {
                    Require(colors[i]==Color.FromArgb(endpoints[offset],endpoints[offset+1],endpoints[offset+2]),"Deterministic endpoint "+i);offset+=3;
                }
                Require(colors[10].ToArgb()==Color.White.ToArgb(),"Native patched white");
            });
            check("Classic damage resource, phase and bottom-aligned anchors",delegate {
                var pal=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette;
                var frames=Assets.Sph(Path.Combine(assets,"Graphics","SMOKE.SPH"),pal);
                Require(frames.Count==34&&frames[33]==null&&frames[0].Width==16&&frames[0].Height==47,"Original damage frame fixture");
                for(int i=0;i<33;i++)Require(frames[i]!=null&&frames[i].UnresolvedTail.Length==0,"Damage frame boundary "+i);
                Require(ClassicDamage.Frame(0,0)==0&&ClassicDamage.Frame(17,2)==0&&ClassicDamage.Frame(32,1)==7&&ClassicDamage.Frame(33,0)==0,"Native frame phase/modulus");
                Require(ClassicDamage.Anchor(new Point(2,4))==new Point(164,331),"Native damage anchor");
                Require(ClassicDamage.Bounds(new Point(164,331),new Size(12,37))==new Rectangle(158,294,12,37),"Native bottom-centered draw");
                byte[] draw=File.ReadAllBytes(Path.Combine(evidence,"Classic-DamageDraw.bin"));
                Require(BitConverter.ToInt32(draw,0x40DC90-0x40DB39+4)==33&&draw[0x40DCC1-0x40DB39+4]==8,"Native modulo and segment increment");
                byte[] binding=File.ReadAllBytes(Path.Combine(evidence,"Classic-DamageResource.bin"));
                Require(binding[0]==0x6a&&binding[1]==15,"Native SMOKE sprite bank");
            });
            check("Damage rendering responds to known hits and disappears on sinking",delegate {
                var palette=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette;
                using(var effects=new ClassicDamage(Path.Combine(assets,"Graphics"),palette))using(var bitmap=new Bitmap(640,512))using(var graphics=Graphics.FromImage(bitmap)) {
                    var board=new Board();board.Place(new Point(2,4),2,true,"Fixture",4);Ship ship=board.Ships[0];
                    effects.Draw(graphics,board,ship,0);
                    Require(AlphaCount(bitmap,new Rectangle(150,285,30,50))==0,"Effect before any hit");
                    board.Shoot(new Point(2,4));effects.Draw(graphics,board,ship,0);
                    Require(AlphaCount(bitmap,new Rectangle(158,294,12,37))>0,"Known hit effect absent");
                    board.Shoot(new Point(2,5));graphics.Clear(Color.Transparent);effects.Draw(graphics,board,ship,0);
                    Require(AlphaCount(bitmap,new Rectangle(150,285,60,65))==0,"Sunk ship still burns");
                }
            });
            check("Effect clock partitions, pause/still, reset and bounded catch-up",delegate {
                var a=new EffectClock();var b=new EffectClock();
                for(int i=0;i<10;i++)a.Advance(40,true);
                foreach(int ms in new[]{17,23,80,11,29,120,120})b.Advance(ms,true);
                Require(a.Tick==10&&b.Tick==10,"Frame partition dependence");
                a.Advance(9999,false);Require(a.Tick==10,"Paused/still animation advanced");
                a.Reset();Require(a.Tick==0,"Reset phase");
                a.Advance(1000,true);Require(a.Tick==5,"Catch-up was not capped at five steps");
                a.Reset();a.Advance(39,true);a.Advance(1000,false);a.Advance(1,true);Require(a.Tick==1,"Pause lost partial frame");
            });
            check("Legacy sound settings and atomic display preference round-trip",delegate {
                var old=DisplaySettings.Parse("sound=false");
                Require(!old.Sound&&!old.RecoveredColors&&old.Motion,"Legacy mute/default display");
                var settings=DisplaySettings.Parse("sound=false\r\ncustomConfirm=Enter\r\ncolors=recovered\r\nmotion=false\r\n");
                string path=Path.GetTempFileName();
                try {
                    settings.Save(path);var loaded=DisplaySettings.Load(path);
                    Require(!loaded.Sound&&loaded.RecoveredColors&&!loaded.Motion&&loaded.Serialize().Contains("customConfirm=Enter"),"Preference round trip or unknown key lost");
                    loaded.Motion=true;loaded.Save(path);Require(DisplaySettings.Load(path).Motion,"Atomic replacement");
                }finally{File.Delete(path);if(File.Exists(path+".new"))File.Delete(path+".new");}
                var malformed=DisplaySettings.Parse("sound=false\ncolors=unknown\nmotion=maybe\ninvalid line\n");
                Require(!malformed.Sound&&!malformed.RecoveredColors&&malformed.Motion&&malformed.Problem!="","Malformed preferences changed accepted mute/defaults");
            });
            check("Managed targeting against 113 executions of original x86 selector/RNG",delegate {
                string[] rows=File.ReadAllLines(Path.Combine(evidence,"Original-Target-Cases.csv"));
                Require(rows.Length==114,"Original oracle fixture count");
                for(int i=1;i<rows.Length;i++) {
                    string[] f=rows[i].Replace("\"","").Split(',');
                    var raw=new int[10,10];string[] cells=f[6].Split(';');
                    Require(cells.Length==100,"Oracle grid dimensions");
                    for(int x=0;x<10;x++)for(int y=0;y<10;y++)raw[x,y]=int.Parse(cells[x*10+y]);
                    var random=new NativeRandom(uint.Parse(f[2]));
                    TargetDecision decision=OriginalOpponent.Choose(raw,int.Parse(f[1]),random);
                    string identity=f[0]+" level="+f[1]+" seed="+f[2];
                    Require(!decision.Stalled&&decision.Cell==new Point(int.Parse(f[3]),int.Parse(f[4])),"Target mismatch "+identity);
                    Require(random.State==uint.Parse(f[5]),"Random consumption mismatch "+identity);
                }
            });
            check("Original targeting budgets, masking and non-mutating observations",delegate {
                int[] expected={10,50,100,200};
                byte[] code=File.ReadAllBytes(Path.Combine(evidence,"Original-TargetSelector.bin"));
                int[] entries={0x41CB11,0x41CB1B,0x41CB25,0x41CB2F};
                for(int i=0;i<4;i++)Require(OriginalOpponent.Budget(i)==expected[i]&&BitConverter.ToInt32(code,entries[i]-0x41C940+4)==expected[i],"Native search budget");
                var raw=new int[10,10];raw[1,1]=2;raw[2,2]=4;raw[3,3]=1;raw[4,4]=8;
                var masked=OriginalOpponent.Mask(raw);
                Require(masked[1,1]==0&&masked[2,2]==0&&masked[3,3]==1&&masked[4,4]==8,"Native observation codes");
                Require(raw[1,1]==2&&raw[2,2]==4,"Source state mutated");
                Reject(delegate {OriginalOpponent.Mask(new int[2,2]);});
                bool rejected=false;try{OriginalOpponent.Budget(4);}catch(ArgumentOutOfRangeException){rejected=true;}
                Require(rejected,"Unsupported original setting accepted");
            });
            check("Opponent observation hides unshot ships and preserves visible sunk hits",delegate {
                var a=new Board();var b=new Board();
                a.Place(new Point(2,2),2,true,"A");b.Place(new Point(7,7),2,true,"B");
                a.Shoot(new Point(0,0));b.Shoot(new Point(0,0));
                int[,] seenA=a.ObservedShots(),seenB=b.ObservedShots();
                for(int x=0;x<10;x++)for(int y=0;y<10;y++)Require(seenA[x,y]==seenB[x,y],"Hidden layout leaked into observation");
                var r1=new NativeRandom(91);var r2=new NativeRandom(91);
                Require(OriginalOpponent.Choose(seenA,2,r1).Cell==OriginalOpponent.Choose(seenB,2,r2).Cell&&r1.State==r2.State,"Hidden layout affected targeting");
                a.Shoot(new Point(2,2));a.Shoot(new Point(2,3));seenA=a.ObservedShots();
                Require(seenA[2,2]==8&&seenA[2,3]==8&&seenA[0,0]==1,"Visible hit/miss state");
            });
            check("Original exhausted search can repeat without damage and still ends its turn",delegate {
                bool covered=false;
                for(int seed=0;seed<64&&!covered;seed++) {
                    var g=new Game(seed);g.AutoPlace();g.Start();
                    for(int x=0;x<10;x++)for(int y=0;y<10;y++)if(g.Player.ShipAt(new Point(x,y))==null)g.Player.Shoot(new Point(x,y));
                    var probe=new NativeRandom(unchecked((uint)(seed^0x17b5)));
                    var decision=OriginalOpponent.Choose(g.Player.ObservedShots(),0,probe);
                    if(!g.Player.Shots[decision.Cell.X,decision.Cell.Y])continue;
                    g.Phase=Phase.OpponentTurn;
                    Require(g.AdvanceOpponent()&&g.LastEnemyShot.Repeated&&!g.LastEnemyShot.Hit&&g.Phase==Phase.PlayerTurn&&g.Turn==2,"Repeat turn handoff");
                    foreach(var ship in g.Player.Ships)Require(ship.Hits==0,"Repeat dealt damage");
                    Require(g.Message.Contains("repeat / no new damage"),"Repeat was hidden from the player");
                    covered=true;
                }
                Require(covered,"No native repeat case exercised");
            });
            check("Unsupported isolated-hit trap fails visibly instead of hanging",delegate {
                var raw=new int[10,10];raw[5,5]=8;raw[5,4]=1;raw[5,6]=1;
                var random=new NativeRandom(1);var decision=OriginalOpponent.Choose(raw,0,random);
                Require(decision.Stalled&&random.Calls==0,"Native trap was silently replaced");
                var g=new Game(90);g.AutoPlace();g.Start();g.Player=new Board();
                g.Player.Place(new Point(5,5),1,true,"Incompatible fixture");g.Player.Place(new Point(9,9),1,true,"Survivor");
                g.Player.Shoot(new Point(5,5));g.Player.Shoot(new Point(5,4));g.Player.Shoot(new Point(5,6));g.Phase=Phase.OpponentTurn;
                Require(!g.AdvanceOpponent()&&g.Paused&&g.Message.Contains("F2")&&g.LastEnemyShot==null,"Stall did not pause/report clearly");
            });
            check("Native camera constants and fleet origins from original code bytes",delegate {
                byte[] camera=File.ReadAllBytes(Path.Combine(evidence,"Classic-Camera.bin"));
                byte[] origins=File.ReadAllBytes(Path.Combine(evidence,"Classic-BoardOrigins.bin"));
                Require(camera[0]==0xc7&&camera[30]==0xb8,"Original camera instruction fixture");
                Require(ClassicGeometry.CameraX==BitConverter.ToInt32(camera,6)&&ClassicGeometry.CameraY==BitConverter.ToInt32(camera,16),"Camera origin constants");
                Require(ClassicGeometry.ScaleY==BitConverter.ToInt32(camera,26)&&ClassicGeometry.ScaleX==BitConverter.ToInt32(camera,31),"Projection scale constants");
                Require(origins[0x36]==0xbf&&BitConverter.ToInt32(origins,0x37)==11,"Opponent board X origin");
                byte[] entry=File.ReadAllBytes(Path.Combine(evidence,"Classic-ViewDispatch.bin"));
                byte[] track=File.ReadAllBytes(Path.Combine(evidence,"Classic-CameraDispatch.bin"));
                Require(BitConverter.ToUInt32(entry,7*4)==0x408A30&&BitConverter.ToUInt32(track,7*4)==0x408D11,"Classic mode 7 dispatch");
            });
            check("Independent native board vertices, picking and boundary truncation",delegate {
                Point[] own={new Point(28,305),new Point(223,175),new Point(417,305),new Point(223,435)};
                Point[] other={new Point(242,162),new Point(437,32),new Point(632,162),new Point(437,292)};
                Point[] grid={new Point(0,0),new Point(10,0),new Point(10,10),new Point(0,10)};
                for(int i=0;i<4;i++) {
                    Require(ClassicGeometry.Project(false,grid[i].X,grid[i].Y)==own[i],"Native own vertex "+i);
                    Require(ClassicGeometry.Project(true,grid[i].X,grid[i].Y)==other[i],"Native enemy vertex "+i);
                }
                Require(ClassicGeometry.Pick(false,new PointF(164,331))==new Point(2,4),"Original X/Y axis orientation");
                Require(ClassicGeometry.Pick(false,new PointF(27,305))==new Point(0,0),"Native truncation toward zero near origin");
                Require(ClassicGeometry.Pick(false,new PointF(5,5))==null&&ClassicGeometry.Pick(true,new PointF(164,331))==null,"Outside/wrong board");
                Require(ClassicGeometry.Pick(false,new PointF(float.NaN,0))==null&&ClassicGeometry.Pick(false,new PointF(float.PositiveInfinity,0))==null,"Invalid pixel coordinates");
            });
            check("Original odd/even ship anchor and percent-centering cases",delegate {
                Require(ClassicGeometry.ShipPercent==79,"Native scale truncation");
                Require(ClassicGeometry.ShipAnchor(false,new Point(0,0),5)==new Point(85,331),"Carrier anchor");
                Require(ClassicGeometry.ShipAnchor(true,new Point(0,0),5)==new Point(299,188),"Opponent origin");
                Require(ClassicGeometry.ShipAnchor(false,new Point(4,3),5)==new Point(221,318),"Battleship anchor");
                Require(ClassicGeometry.ShipAnchor(false,new Point(2,1),4)==new Point(134,312),"Even submarine anchor");
                Require(ClassicGeometry.ShipAnchor(false,new Point(9,8),2)==new Point(387,299),"Even destroyer anchor");
                Require(ClassicGeometry.ShipAnchor(false,new Point(1,7),3)==new Point(221,396),"Odd frigate anchor");
                Require(ClassicShips.Bounds(new Point(85,331),new Size(106,75))==new Rectangle(32,294,106,75),"Native centered scaled bounds");
            });
            check("Independent scaled SPH scanline, transparency and 16.16 fixture",delegate {
                var fixture=new IndexedImage {Width=8,Height=4,Pixels=new byte[32],Visible=new bool[32],Palette=new Color[256]};
                byte[] row={0,22,23,24,25};
                for(int i=0;i<5;i++){fixture.Pixels[3+i]=row[i];fixture.Visible[3+i]=true;}
                for(int i=0;i<8;i++){fixture.Pixels[8+i]=(byte)(i+1);fixture.Visible[8+i]=true;}
                for(int i=0;i<3;i++){fixture.Pixels[17+i]=(byte)(11+i);fixture.Visible[17+i]=true;}
                var scaled=ClassicShips.Scale(fixture,50);
                Require(scaled.Width==4&&scaled.Height==2&&!scaled.Visible[1]&&scaled.Visible[2]&&scaled.Pixels[2]==23,"Scaled literal start/zero transparency");
                Require(scaled.Visible[4]&&scaled.Pixels[4]==11&&!scaled.Visible[5]&&!scaled.Visible[6],"Scaled source row/run length");
                scaled=ClassicShips.Scale(fixture,79);
                Require(scaled.Width==6&&scaled.Height==3&&scaled.Pixels[9]==4&&scaled.Pixels[10]==6,"16.16 step truncation versus exact-ratio sampling");
                fixture.Visible[1]=true;
                Reject(delegate {ClassicShips.Scale(fixture,50);});
                Reject(delegate {ClassicShips.Scale(fixture,0);});
                Reject(delegate {ClassicShips.Scale(fixture,1);});
            });
            check("Native ship dispatch fixtures and five packaged scaled ship frames",delegate {
                var background=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));
                var lines=File.ReadAllLines(Path.Combine(evidence,"Native-Ship-Dispatch.csv"));
                Require(lines.Length==6,"Original dispatch fixture count");
                using(var ships=new ClassicShips(Path.Combine(assets,"Graphics"),background.Palette)) {
                    for(int i=1;i<lines.Length;i++) {
                        // Extractor emits simple quoted fields, without embedded CSV delimiters.
                        string[] fields=lines[i].Replace("\"","").Split(',');
                        int type=int.Parse(fields[0]),frame=int.Parse(fields[5]);
                        string resource=fields[4];
                        Require(ClassicShips.Resource(type)==resource&&ClassicShips.Frame(type)==frame,"Resource/frame dispatch "+type);
                        var image=ships.Image(type);
                        Require(image.Width==int.Parse(fields[9])&&image.Height==int.Parse(fields[10]),"Native scaled dimensions "+type);
                        byte[] handler=File.ReadAllBytes(Path.Combine(evidence,"Ship-Handler-"+type+".bin"));
                        Require(handler[4]==0x8b&&handler[5]==0x15&&BitConverter.ToUInt32(handler,6)==Convert.ToUInt32(fields[2],16),"Original bank pointer fixture");
                        bool visible=false,transparent=false;
                        for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A==0)transparent=true;else visible=true;
                        Require(visible&&transparent,"Ship transparency "+type);
                    }
                }
            });
            check("Ship visibility hides opponent layout and removes sunk art",delegate {
                var b=new Board();
                b.Place(new Point(2,2),2,true,"Fixture",4);
                var ship=b.Ships[0];
                Require(BoardView.RevealsShip(false,Phase.PlayerTurn,ship)&&!BoardView.RevealsShip(true,Phase.PlayerTurn,ship),"Hidden opponent ship leak");
                b.Shoot(new Point(2,2));
                Require(BoardView.RevealsShip(false,Phase.PlayerTurn,ship),"Partial hit hid live ship");
                Require(BoardView.RevealsShip(true,Phase.Finished,ship),"Post-battle surviving opponent reveal");
                b.Shoot(new Point(2,3));
                Require(!BoardView.RevealsShip(false,Phase.PlayerTurn,ship)&&!BoardView.RevealsShip(true,Phase.Finished,ship),"Sunk sprite retained");
            });
            check("Recovered fleet against original initializer/template/name bytes",delegate {
                byte[] types=File.ReadAllBytes(Path.Combine(evidence,"Classic-TypeIds.bin"));
                byte[] templates=File.ReadAllBytes(Path.Combine(evidence,"Classic-Templates.bin"));
                byte[] names=File.ReadAllBytes(Path.Combine(evidence,"Classic-Names.bin"));
                byte[] init=File.ReadAllBytes(Path.Combine(evidence,"Classic-Initializer.bin"));
                int[] pushes={0x42393B,0x42399F,0x4239C2,0x4239E5,0x423A09};
                int[] calls={0x423988,0x4239AB,0x4239CE,0x4239F1,0x423A15};
                Require(types.Length==5&&templates.Length==1340&&names.Length==130,"Fixture sizes");
                int total=0;
                for(int i=0;i<5;i++) {
                    ShipSpec spec=RuleProfile.Recovered.Fleet[i];
                    string original=Encoding.ASCII.GetString(names,i*26,26).Split('\0')[0];
                    int at=pushes[i]-0x4238D0,call=calls[i]-0x4238D0;
                    Require(init[at]==0x6a&&init[at+1]==types[i],"Initializer type argument");
                    Require(init[call]==0xe8&&calls[i]+5+BitConverter.ToInt32(init,call+1)==0x42CEF0,"CreateShip target");
                    Require(spec.SourceType==types[i]&&spec.Name==original,"Ship identity "+i);
                    Require(spec.Length==BitConverter.ToUInt16(templates,i*268+14)&&spec.Length==BitConverter.ToUInt16(templates,i*268+16),"Length and initial hits "+i);
                    total+=spec.Length;
                }
                Require(total==19,"Recovered occupied cell count");
            });
            check("Recovered weapon names, weighted lists and dispatch against native data",delegate {
                byte[] lists=File.ReadAllBytes(Path.Combine(evidence,"Classic-WeaponLists.bin"));
                byte[] names=File.ReadAllBytes(Path.Combine(evidence,"Weapon-Names.bin"));
                byte[] movieNames=File.ReadAllBytes(Path.Combine(evidence,"Movie-Names.bin"));
                foreach(int type in new[]{7,1,2,12,4})foreach(bool ai in new[]{false,true}) {
                    int start=ai?0:type==12?32:type==7?48:0,length=0;
                    while(BitConverter.ToInt32(lists,start+length*4)!=0)length++;
                    Require(length>0&&length<=7,"Original list sentinel");
                    for(int input=0;input<=32767;input++)
                        Require(ClassicWeapons.Weapon(type,ai,input)==BitConverter.ToInt32(lists,start+((input%50)%length)*4),"List choice/bias");
                }
                foreach(int weapon in new[]{14,15,16,17,18,20,24,25,26,29,30}) {
                    Require(ClassicWeapons.Name(weapon)==Encoding.ASCII.GetString(names,(weapon-14)*48,48).Split('\0')[0],"Original weapon name");
                    foreach(int type in new[]{7,1,2,12,4})for(int input=0;input<4;input++) {
                        int id=ClassicWeapons.Movie(type,weapon,input);
                        Require(ClassicWeapons.Clip(id)==Encoding.ASCII.GetString(movieNames,id*9,9).Split('\0')[0],"Original movie lookup");
                    }
                }
                bool rejected=false;try{ClassicWeapons.Movie(1,19,0);}catch(NotSupportedException){rejected=true;}
                Require(rejected,"Unsupported dispatch was silently accepted");
            });
            check("Original-code weapon selection and movie dispatch fixtures (two numerical grids)",delegate {
                int rows=0;
                foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Weapons-Oracle.csv"))) {
                    if(line.StartsWith("ShipType"))continue;string[] p=line.Split(',');
                    Require(p.Length==5&&ClassicWeapons.Weapon(int.Parse(p[0]),p[1]=="1",int.Parse(p[2]))==int.Parse(p[3])&&p[4]=="1","Native list output/roll count");rows++;
                }
                Require(rows==500,"Selection fixture size");rows=0;
                foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Firing-Movies-Oracle.csv"))) {
                    if(line.StartsWith("ShipType"))continue;string[] p=line.Split(',');
                    Require(p.Length==6&&ClassicWeapons.Movie(int.Parse(p[0]),int.Parse(p[1]),int.Parse(p[2]))==int.Parse(p[3])&&p[4]=="1"&&p[5]=="1","Native movie output/roll/movie count");rows++;
                }
                Require(rows==220,"Movie fixture size");
            });
            check("Firing ship selection gates and initial slot",delegate {
                var g=new Game(910);
                Require(!g.SelectShooter(0)&&!g.SelectShooter(new Point(0,0)),"Selection before battle");
                g.AutoPlace();g.Start();
                Require(g.PlayerShooter==1&&g.EnemyShooter==1,"Native initial firing slot");
                Require(g.SelectShooter(g.Player.Ships[3].Cells[2])&&g.PlayerShooter==3,"Own ship segment selection");
                Require(g.Turn==1&&g.Events.Count==2&&g.LastPlayerShot==null,"Selection consumed a turn");
                Require(!g.SelectShooter(-1)&&!g.SelectShooter(5)&&!g.SelectShooter(new Point(-1,-1)),"Invalid selection");
                g.TogglePause();Require(!g.SelectShooter(0)&&g.PlayerShooter==3,"Paused selection");g.TogglePause();
                g.Fire(new Point(0,0));Require(!g.SelectShooter(0)&&g.PlayerShooter==3,"Opponent-phase selection");
                g.Phase=Phase.Finished;Require(!g.SelectShooter(0),"Terminal selection");
                var legacy=new Game(911,RuleProfile.Legacy);legacy.AutoPlace();legacy.Start();
                Require(legacy.PlayerShooter==-1&&!legacy.SelectShooter(0),"Legacy selection changed");
            });
            check("Selected ship sinking switches to the first surviving roster slot",delegate {
                var g=new Game(912,RuleProfile.Recovered,OpponentKind.Prototype,0);g.AutoPlace();g.Start();
                g.Player=new Board();g.Player.Place(new Point(9,9),1,true,"Survivor",7);g.Player.Place(new Point(0,1),1,true,"Selected",1);
                for(int x=0;x<10;x++)for(int y=0;y<10;y++)g.Player.Shots[x,y]=!(x==0&&y==1);
                g.Phase=Phase.OpponentTurn;
                Require(g.AdvanceOpponent()&&g.LastEnemyShot.Sunk&&g.PlayerShooter==0,"Actual opponent sink did not replace selected ship");
                Require(!g.SelectShooter(1)&&g.SelectShooter(0),"Sunk ship selected");
                var active=g.Enemy.Ships[1];
                foreach(Point cell in active.Cells) {g.Phase=Phase.PlayerTurn;Require(g.Fire(cell),"Sink scenario player shot");}
                Require(active.Sunk&&g.EnemyShooter==0,"Enemy selected ship fallback");
                var survivor=g.Enemy.Ships[2];
                foreach(Point cell in survivor.Cells) {g.Phase=Phase.PlayerTurn;Require(g.Fire(cell),"Other sink scenario");}
                Require(g.EnemyShooter==0,"Unselected ship sinking changed selection");
            });
            check("Weapon presentation consumes two rolls only for valid actions",delegate {
                var random=new NativeRandom(731);var expected=new NativeRandom(731);
                var ship=new Ship {Name="Fixture",SourceType=12};ship.Cells.Add(new Point(0,0));
                int weapon=ClassicWeapons.Weapon(12,false,expected.Next()),movie=ClassicWeapons.Movie(12,weapon,expected.Next());
                var presentation=ClassicWeapons.Choose(ship,false,random);
                Require(presentation.WeaponId==weapon&&presentation.MovieId==movie&&presentation.RandomCalls==2&&random.State==expected.State,"Presentation RNG consumption");
                var g=new Game(913);g.AutoPlace();g.Start();g.SelectShooter(0);
                g.TogglePause();Require(!g.Fire(new Point(0,0))&&g.LastPlayerFire==null,"Paused presentation");g.TogglePause();
                Require(!g.Fire(new Point(-1,0))&&g.LastPlayerFire==null,"Invalid presentation");
                g.Fire(new Point(0,0));var previous=g.LastPlayerFire;
                Require(previous.Shooter=="Super Carrier"&&(previous.WeaponId==29||previous.WeaponId==30),"Selected carrier did not supply its list");
                g.AdvanceOpponent();Require(!g.Fire(new Point(0,0))&&g.LastPlayerFire==previous,"Repeat changed firing presentation");
            });
            check("Different ship/weapon choices retain the accepted target and damage history",delegate {
                var a=new Game(914);var b=new Game(914);a.AutoPlace();b.AutoPlace();a.Start();b.Start();
                int different=0;
                for(int turn=0;turn<15;turn++) {
                    for(int i=0;i<5;i++)if(!a.Player.Ships[i].Sunk){a.SelectShooter(i);break;}
                    for(int i=4;i>=0;i--)if(!b.Player.Ships[i].Sunk){b.SelectShooter(i);break;}
                    var target=new Point(turn/10,turn%10);Require(a.Fire(target)&&b.Fire(target),"Parallel shot");
                    Require(a.LastPlayerShot.Description==b.LastPlayerShot.Description,"Weapon altered cell damage");
                    if(a.LastPlayerFire.WeaponId!=b.LastPlayerFire.WeaponId)different++;
                    Require(a.AdvanceOpponent()&&b.AdvanceOpponent(),"Parallel opponent turn");
                    Require(a.LastEnemyShot.Description==b.LastEnemyShot.Description&&a.LastOpponentDecision.RandomCalls==b.LastOpponentDecision.RandomCalls,"Presentation changed target history");
                }
                Require(different>0,"Comparison did not exercise different weapons");
            });
            check("Recovered manual fixed-axis placement, touching and edge cases",delegate {
                var g=new Game(801);
                Require(!g.Start()&&!g.Rotate(),"Incomplete start or rotation accepted");
                g.Vertical=false; // Even stale modern orientation cannot override the recovered rule.
                Require(!g.Place(new Point(9,9))&&!g.Place(new Point(0,0))&&g.Player.Ships.Count==0,"Centered ship crossed edge");
                Require(g.Place(new Point(9,7)),"Boundary placement");
                Require(g.Player.Ships[0].Cells[4]==new Point(9,9),"Fixed Y axis");
                Require(!g.Place(new Point(9,7)),"Overlap accepted");
                Require(g.Place(new Point(8,7)),"Adjacent parallel ship rejected");
                Require(g.Place(new Point(9,1))&&g.Place(new Point(8,2))&&g.Place(new Point(7,9)),"Remaining fleet");
                Require(g.NextLength==0&&g.Start(),"Completed deployment");
            });
            check("v0.1 preset retains rotating 17-cell fleet",delegate {
                var g=new Game(802,RuleProfile.Legacy);
                int total=0;
                foreach(var spec in g.Rules.Fleet)total+=spec.Length;
                Require(total==17&&!g.PlacementVertical,"Legacy fleet/default axis");
                Require(g.Place(new Point(0,0))&&g.Player.Ships[0].Cells[4]==new Point(4,0),"Horizontal placement");
                Require(g.Rotate()&&g.PlacementVertical&&g.Place(new Point(0,1)),"Rotation/vertical placement");
                g.ClearPlacement();
                Require(g.Player.Ships.Count==0,"Clear placement");
                g.AutoPlace();
                Require(g.Start()&&!g.Rotate(),"Battle rotation gate");
            });
            check("Fleet generation and roster order across both presets (128 seeds each)",delegate {
                foreach(var profile in new[]{RuleProfile.Recovered,RuleProfile.Legacy})for(int seed=0;seed<128;seed++) {
                    var g=new Game(seed,profile);
                    g.AutoPlace();
                    Require(g.Start(),"Generated deployment could not start");
                    foreach(var b in new[]{g.Player,g.Enemy}) {
                        Require(b.Ships.Count==5,"Fleet count");
                        int occupied=0;
                        for(int i=0;i<5;i++) {
                            Ship s=b.Ships[i]; ShipSpec spec=profile.Fleet[i];
                            Require(s.Name==spec.Name&&s.SourceType==spec.SourceType&&s.Cells.Count==spec.Length&&s.Hits==0,"Roster identity/order");
                            for(int cell=0;cell<s.Cells.Count;cell++) {
                                Point p=s.Cells[cell];
                                Require(Board.Inside(p)&&b.ShipAt(p)==s&&!b.Shots[p.X,p.Y],"Cell indexing");
                                if(profile.FixedY)Require(p==new Point(s.Cells[0].X,s.Cells[0].Y+cell),"Recovered axis");
                            }
                        }
                        for(int x=0;x<10;x++)for(int y=0;y<10;y++)if(b.ShipAt(new Point(x,y))!=null)occupied++;
                        Require(occupied==(profile.FixedY?19:17),"Overlapping or missing cells");
                    }
                }
            });
            check("Original EO_CLASS icons and native roster coordinate fixture",delegate {
                var pal=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX")).Palette;
                var icons=Assets.Sph(Path.Combine(assets,"Graphics","EO_CLASS.SPH"),pal);
                Require(icons.Count==15&&icons[14]==null,"Original frame count");
                byte[] xy=File.ReadAllBytes(Path.Combine(evidence,"Classic-RosterXY.bin"));
                Require(xy.Length==64,"Coordinate table size");
                foreach(bool enemy in new[]{false,true})for(int i=0;i<5;i++) {
                    int offset=enemy?32:0;
                    Point p=BoardView.RosterPosition(enemy,i);
                    Require(p==new Point(BitConverter.ToInt16(xy,offset+i*2),BitConverter.ToInt16(xy,offset+16+i*2)),"Native roster coordinate");
                    var f=icons[(enemy?9:4)+i];
                    Require(f.Width==88&&f.Height==30&&f.OriginX==1&&f.OriginY==1,"Status frame geometry");
                }
            });
            check("Hit/miss/sink handoff, repeated-shot gate and destroyed roster state",delegate {
                var g=new Game(803);
                g.AutoPlace(); g.Start();
                g.Enemy=new Board();
                g.Enemy.Place(new Point(0,0),2,true,"Fixture");
                g.Enemy.Place(new Point(9,0),1,true,"Survivor");
                Require(BoardView.RosterFrame(g.Enemy,0)==4,"Live icon");
                Require(g.Fire(new Point(0,0))&&g.LastPlayerShot.Hit&&g.Phase==Phase.OpponentTurn,"Hit retains turn");
                Require(g.AdvanceOpponent(),"Opponent return");
                int events=g.Events.Count;
                Require(!g.Fire(new Point(0,0))&&g.Phase==Phase.PlayerTurn&&g.Enemy.Ships[0].Hits==1&&g.Events.Count==events,"Repeat consumed action");
                Require(g.Fire(new Point(9,9))&&!g.LastPlayerShot.Hit&&g.Phase==Phase.OpponentTurn&&g.AdvanceOpponent(),"Miss handoff");
                Require(g.Fire(new Point(0,1))&&g.LastPlayerShot.Sunk&&g.Phase==Phase.OpponentTurn,"Nonfinal sink handoff");
                Require(BoardView.RosterFrame(g.Enemy,0)==9&&BoardView.RosterFrame(g.Enemy,1)==5,"Destroyed/live icon selection");
            });
            check("Placement overlap, bounds and adjacency",delegate {
                var b=new Board();
                Require(b.Place(new Point(0,0),5,false,"A"),"First ship");
                Require(!b.Place(new Point(0,0),3,true,"B"),"Overlap");
                Require(!b.Place(new Point(9,9),2,false,"B"),"Edge");
                Require(b.Place(new Point(0,1),4,false,"B"),"Adjacency");
            });
            check("Hit, miss, repeat rejection and sink",delegate {
                var b=new Board();
                b.Place(new Point(1,1),2,true,"Fixture");
                Require(!b.Shoot(new Point(9,9)).Hit,"Miss");
                Require(b.Shoot(new Point(1,1)).Hit&&!b.AllSunk,"Hit");
                Require(b.Shoot(new Point(1,1))==null&&b.Ships[0].Hits==1,"Repeat");
                Require(b.Shoot(new Point(1,2)).Sunk&&b.AllSunk,"Sink");
            });
            check("Turn handoff and pause gates",delegate {
                var g=new Game(123);
                g.AutoPlace();
                Require(g.Start(),"Start");
                g.TogglePause();
                Require(!g.Fire(new Point(0,0)),"Paused firing");
                g.TogglePause();
                Require(g.Fire(new Point(0,0))&&!g.Fire(new Point(1,0)),"One player action");
                g.TogglePause();
                Require(!g.AdvanceOpponent(),"Paused opponent");
                g.TogglePause();
                Require(g.AdvanceOpponent()&&g.Phase==Phase.PlayerTurn&&g.Turn==2,"Turn return");
            });
            check("Victory, defeat and terminal input gates",delegate {
                var g=new Game(124);
                g.AutoPlace();
                g.Start();
                g.Enemy=new Board();
                g.Enemy.Place(new Point(0,0),1,false,"Fixture");
                Require(g.Fire(new Point(0,0))&&g.Phase==Phase.Finished&&!g.AdvanceOpponent()&&!g.Fire(new Point(1,0)),"Victory gate");
                g=new Game(125);
                g.AutoPlace();
                g.Start();
                g.Player=new Board();
                g.Player.Place(new Point(0,1),2,true,"Fixture");
                g.Player.Shoot(new Point(0,2));
                for(int x=0; x<10; x++)for(int y=0; y<10; y++)g.Player.Shots[x,y]=!(x==0&&y==1);
                g.Phase=Phase.OpponentTurn;
                Require(g.AdvanceOpponent()&&g.Phase==Phase.Finished&&!g.Fire(new Point(0,0)),"Defeat gate");
            });
            check("Complete deterministic battles and reset in both presets",delegate {
                foreach(var profile in new[]{RuleProfile.Recovered,RuleProfile.Legacy}) {
                var g=new Game(126,profile);
                g.AutoPlace();
                g.Start();
                int shots=0;
                while(g.Phase!=Phase.Finished&&shots<100) {
                    bool fired=false;
                    for(int y=0; y<10&&!fired; y++)for(int x=0; x<10&&!fired; x++)if(!g.Enemy.Shots[x,y]) {
                        Require(g.Fire(new Point(x,y)),"Player action");
                        shots++;
                        fired=true;
                    }
                    if(g.Phase==Phase.OpponentTurn)Require(g.AdvanceOpponent(),"Opponent action");
                }
                Require(g.Phase==Phase.Finished&&shots<=100,"Battle did not terminate");
                g=new Game(126,profile);
                Require(g.Phase==Phase.Placement&&g.Player.Ships.Count==0&&!g.Paused&&g.Turn==1,"Reset state");
                }
            });
            check("Complete battles across four original settings and prototype pursuit",delegate {
                for(int mode=0;mode<5;mode++)for(int seed=810;seed<813;seed++) {
                    var g=new Game(seed,RuleProfile.Recovered,mode==4?OpponentKind.Prototype:OpponentKind.Original,mode==4?0:mode);
                    g.AutoPlace();Require(g.Start(),"Start");int shots=0;
                    while(g.Phase!=Phase.Finished&&shots<100) {
                        bool fired=false;
                        for(int y=0;y<10&&!fired;y++)for(int x=0;x<10&&!fired;x++)if(!g.Enemy.Shots[x,y]) {
                            Require(g.Fire(new Point(x,y)),"Player shot");shots++;fired=true;
                        }
                        if(g.Phase==Phase.OpponentTurn)Require(g.AdvanceOpponent()&&!g.Paused,"Opponent stalled in valid fixed-axis battle");
                    }
                    Require(g.Phase==Phase.Finished,"Battle failed to terminate");
                }
            });
            check("Native and v0.1 board picking across 400 cell centers",delegate {
                foreach(bool legacy in new[]{false,true})foreach(bool enemy in new[] {
                    false,true
                })for(int y=0; y<10; y++)for(int x=0; x<10; x++) {
                    Point? hit=BoardView.HitCell(enemy,BoardView.Project(enemy,(x+.5f)/10,(y+.5f)/10,legacy),legacy);
                    Require(hit.HasValue&&hit.Value==new Point(x,y),"Picking geometry");
                }
            });
            check("Board anchors against independent original border pixels",delegate {
                var image=Assets.Pcx(Path.Combine(assets,"Graphics","E_CLASS.PCX"));
                foreach(bool enemy in new[] {
                    false,true
                })foreach(Point uv in new[] {
                    new Point(0,0),new Point(1,0),new Point(1,1),new Point(0,1)
                }) {
                    PointF anchor=BoardView.Project(enemy,uv.X,uv.Y);
                    bool found=false;
                    for(int y=(int)anchor.Y-3; y<=(int)anchor.Y+3; y++)for(int x=(int)anchor.X-3; x<=(int)anchor.X+3; x++) {
                        if(x<0||y<0||x>=image.Width||y>=image.Height)continue;
                        Color c=image.Palette[image.Pixels[y*image.Width+x]];
                        if(enemy?(c.R>200&&c.G<55&&c.B<55):(c.B>220&&c.R<30&&c.G>80&&c.G<200))found=true;
                    }
                    Require(found,"Anchor does not meet the original board border");
                }
            });
            foreach(string name in new[] {
                "KEYCLIK2.WAV","CANNON1.WAV"
            })check("Original PCM fixture "+name,delegate {
                output.Add("  "+WaveInfo(Path.Combine(assets,"SFX",name)));
            });
            output.Add("Focused checks passed="+passed+" failed="+failed);
            output.Add("These checks do not establish original gameplay fidelity, audible playback or physical keyboard/focus behavior.");
            File.WriteAllLines(report,output.ToArray());
            return failed==0?0:1;
        }
        static string WaveInfo(string path) {
            byte[] b=File.ReadAllBytes(path);
            Require(b.Length>=12&&Encoding.ASCII.GetString(b,0,4)=="RIFF"&&Encoding.ASCII.GetString(b,8,4)=="WAVE","RIFF/WAVE signature");
            int p=12,rate=0,channels=0,bits=0,block=0,data=-1;
            while(p<=b.Length-8) {
                string id=Encoding.ASCII.GetString(b,p,4);
                uint length=BitConverter.ToUInt32(b,p+4);
                p+=8;
                Require(length<=b.Length-p,"WAV chunk truncated");
                if(id=="fmt ") {
                    Require(length>=16&&BitConverter.ToUInt16(b,p)==1,"Only PCM is expected");
                    channels=BitConverter.ToUInt16(b,p+2);
                    rate=BitConverter.ToInt32(b,p+4);
                    block=BitConverter.ToUInt16(b,p+12);
                    bits=BitConverter.ToUInt16(b,p+14);
                }
                if(id=="data")data=(int)length;
                p+=(int)length+(int)(length%2);
            }
            Require(rate>0&&channels>0&&block==channels*bits/8&&data>=0&&data%block==0,"Invalid PCM units");
            return channels+" channel, "+rate+" Hz, "+bits+" bit; "+(data/(double)(block*rate)).ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+" seconds; playback not auditioned";
        }
        static void DrainMovies(Game game){int count=0;while(game.Pending!=null){var pending=game.Pending;Require(game.CompleteMovie(pending,pending.Stage),"Movie completion rejected");if(++count>3)throw new Exception("Shot presentation did not terminate");}}
        static int AlphaCount(Bitmap image,Rectangle area) {
            int count=0;for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++)if(image.GetPixel(x,y).A!=0)count++;
            return count;
        }
    }
}






