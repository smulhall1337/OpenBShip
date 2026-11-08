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
    }
}
