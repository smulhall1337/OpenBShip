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
            check("Recovered manual fixed-axis placement, touching and edge cases",delegate {
                var g=new Game(801);
                Require(!g.Start()&&!g.Rotate(),"Incomplete start or rotation accepted");
                g.Vertical=false; // Even stale modern orientation cannot override the recovered rule.
                Require(!g.Place(new Point(9,6))&&g.Player.Ships.Count==0,"Long ship crossed edge");
                Require(g.Place(new Point(9,5)),"Boundary placement");
                Require(g.Player.Ships[0].Cells[4]==new Point(9,9),"Fixed Y axis");
                Require(!g.Place(new Point(9,5)),"Overlap accepted");
                Require(g.Place(new Point(8,5)),"Adjacent parallel ship rejected");
                Require(g.Place(new Point(9,0))&&g.Place(new Point(8,0))&&g.Place(new Point(7,8)),"Remaining fleet");
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
                g.Player.Place(new Point(0,0),1,false,"Fixture");
                for(int x=0; x<10; x++)for(int y=0; y<10; y++)g.Player.Shots[x,y]=!(x==0&&y==0);
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
            check("Projected board picking across 200 cell centers",delegate {
                foreach(bool enemy in new[] {
                    false,true
                })for(int y=0; y<10; y++)for(int x=0; x<10; x++) {
                    Point? hit=BoardView.HitCell(enemy,BoardView.Project(enemy,(x+.5f)/10,(y+.5f)/10));
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
    }
}
