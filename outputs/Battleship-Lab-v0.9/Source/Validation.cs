using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab {
    public static class Validation {
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
            check("Original solo phase and unavailable-movie transitions (528 controlled cases)",delegate {
                int rows=0;foreach(string line in File.ReadAllLines(Path.Combine(evidence,"Classic-Phase-Oracle.csv"))){if(line.StartsWith("Stage"))continue;string[] p=line.Split(',');int stage=int.Parse(p[0]),weapon=int.Parse(p[1]),roll=int.Parse(p[2]),unavailable=int.Parse(p[3]);bool travel=stage==1||stage==5;int next=stage==7?0:stage+1,impacts=stage==2||stage==6?1:0;if(travel&&unavailable==1){next++;impacts++;}Require(int.Parse(p[4])==next&&int.Parse(p[5])==(travel?ClassicSequence.Travel(weapon,roll):0)&&int.Parse(p[6])==impacts&&int.Parse(p[7])==(travel&&weapon>=14&&weapon<=18?1:0),"Native phase/availability output");rows++;}Require(rows==528,"Phase fixture count");
            });
            check("Complete 28-clip shot library: 2179 native frames, 117 cues and 84 PCM files",delegate {
                var movies=new Dictionary<string,PatMovie>();var cues=new Dictionary<string,List<SoundCue>>();int frames=0,totalCues=0;
                foreach(string name in FiringClips.Names){var movie=PatMovie.Load(File.ReadAllBytes(Path.Combine(assets,"LWEBS",name+".PAT")));movies.Add(name,movie);frames+=movie.Frames.Count;List<SoundCue> sequence;ClipSound.Load(assets,name,movie.Frames.Count,out sequence);cues.Add(name,sequence);totalCues+=sequence.Count;}
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
                Require(ClassicSequence.Feedback(0,new Shot{Hit=true},false)==0&&ClassicSequence.Feedback(1,new Shot{Hit=true},false)==137&&ClassicSequence.Feedback(2,new Shot(),false)==136,"Hit/miss level policy");Require(ClassicSequence.Feedback(3,new Shot{Hit=true,Sunk=true},false)==0&&ClassicSequence.Feedback(3,new Shot{Repeated=true},true)==0,"Sink/repeated occupied gate");
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
                for(int i=0;i<512;i++)many.AddRange(empty);many.AddRange(BitConverter.GetBytes(uint.MaxValue));Require(PatMovie.Load(many.ToArray()).Frames.Count==512,"Frame-count upper boundary");many.InsertRange(0,empty);Reject(delegate{PatMovie.Load(many.ToArray());});
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
