using System;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab {
    public sealed class ShipSpec {
        public readonly string Name;
        public readonly int Length,SourceType;
        public ShipSpec(string name,int length,int type) {
            Name=name; Length=length; SourceType=type;
        }
    }
    public sealed class RuleProfile {
        public readonly string Name;
        public readonly bool FixedY;
        public readonly ShipSpec[] Fleet;
        RuleProfile(string name,bool fixedY,ShipSpec[] fleet) {
            Name=name; FixedY=fixedY; Fleet=fleet;
        }
        // Bs.exe B9A8...A569: Classic initializer 0x4238D0 and template table 0x448D70.
        // This recovers the fleet and axis, not the original AI or full presentation.
        public static readonly RuleProfile Recovered=new RuleProfile("Recovered Classic",true,new[] {
            new ShipSpec("Super Carrier",5,7),new ShipSpec("Battleship",5,1),
            new ShipSpec("Frigate",3,2),new ShipSpec("Missile Submarine",4,12),new ShipSpec("Destroyer",2,4)
        });
        public static readonly RuleProfile Legacy=new RuleProfile("v0.1 prototype",false,new[] {
            new ShipSpec("Carrier",5,-1),new ShipSpec("Battleship",4,-1),
            new ShipSpec("Cruiser",3,-1),new ShipSpec("Submarine",3,-1),new ShipSpec("Patrol boat",2,-1)
        });
    }
    public enum Phase {
        Placement,PlayerTurn,OpponentTurn,Finished
    }
    public sealed class Ship {
        public string Name;
        public int SourceType;
        public List<Point> Cells=new List<Point>();
        public int Hits;
        public bool Sunk {
            get {
                return Hits==Cells.Count;
            }
        }
    }
    public sealed class Shot {
        public Point Cell;
        public bool Hit,Sunk;
        public string ShipName;
        public string Description {
            get {
                return Coordinate(Cell)+": "+(Sunk?"sunk "+ShipName:Hit?"hit":"miss");
            }
        }
        public static string Coordinate(Point p) {
            return ((char)('A'+p.Y)).ToString()+(p.X+1);
        }
    }
    public sealed class Board {
        public readonly List<Ship> Ships=new List<Ship>();
        public readonly bool[,] Shots=new bool[10,10];
        readonly int[,] occupants=new int[10,10];
        public Board() {
            for(int x=0; x<10; x++)for(int y=0; y<10; y++)occupants[x,y]=-1;
        }
        public static bool Inside(Point p) {
            return p.X>=0&&p.Y>=0&&p.X<10&&p.Y<10;
        }
        public Ship ShipAt(Point p) {
            return Inside(p)&&occupants[p.X,p.Y]>=0?Ships[occupants[p.X,p.Y]]:null;
        }
        public bool CanPlace(Point start,int length,bool vertical) {
            if(length<1||length>10)return false;
            for(int i=0; i<length; i++) {
                var p=new Point(start.X+(vertical?0:i),start.Y+(vertical?i:0));
                if(!Inside(p)||occupants[p.X,p.Y]>=0)return false;
            }
            return true;
        }
        public bool Place(Point start,int length,bool vertical,string name,int sourceType=-1) {
            if(!CanPlace(start,length,vertical))return false;
            var ship=new Ship {
                Name=name,SourceType=sourceType
            };
            for(int i=0; i<length; i++) {
                var p=new Point(start.X+(vertical?0:i),start.Y+(vertical?i:0));
                ship.Cells.Add(p);
                occupants[p.X,p.Y]=Ships.Count;
            }
            Ships.Add(ship);
            return true;
        }
        public Shot Shoot(Point p) {
            if(!Inside(p)||Shots[p.X,p.Y])return null;
            Shots[p.X,p.Y]=true;
            Ship ship=ShipAt(p);
            if(ship!=null)ship.Hits++;
            return new Shot {
                Cell=p,Hit=ship!=null,Sunk=ship!=null&&ship.Sunk,ShipName=ship==null?"":ship.Name
            };
        }
        public bool AllSunk {
            get {
                if(Ships.Count==0)return false;
                foreach(var ship in Ships)if(!ship.Sunk)return false;
                return true;
            }
        }
    }
    // The recovered profile follows traced solo Classic fleet/placement/shot rules.
    // Random generation, AI targeting, timing and surrounding UI remain modern code.
    public sealed class Game {
        public readonly RuleProfile Rules;
        public Board Player=new Board(),Enemy=new Board();
        public Phase Phase=Phase.Placement;
        public bool Paused,Vertical;
        public int Turn=1;
        public string Message;
        public readonly List<string> Events=new List<string>();
        public Shot LastPlayerShot,LastEnemyShot;
        readonly Random placementRandom,shotRandom;
        readonly Queue<Point> hunt=new Queue<Point>();
        public Game(int seed) : this(seed,RuleProfile.Recovered) { }
        public Game(int seed,RuleProfile rules) {
            if(rules==null)throw new ArgumentNullException("rules");
            Rules=rules;
            Vertical=rules.FixedY;
            Message=PlacementMessage;
            placementRandom=new Random(seed);
            shotRandom=new Random(seed^0x17b5);
        }
        string PlacementMessage {
            get { return "Place your "+Rules.Fleet[0].Name.ToLowerInvariant()+", or use Auto-place."; }
        }
        public bool PlacementVertical { get { return Rules.FixedY||Vertical; } }
        public bool Rotate() {
            if(Phase!=Phase.Placement||Rules.FixedY)return false;
            Vertical=!Vertical;
            return true;
        }
        public int NextLength {
            get {
                return Player.Ships.Count<Rules.Fleet.Length?Rules.Fleet[Player.Ships.Count].Length:0;
            }
        }
        public bool Place(Point p) {
            if(Phase!=Phase.Placement||Player.Ships.Count>=Rules.Fleet.Length)return false;
            int n=Player.Ships.Count;
            ShipSpec spec=Rules.Fleet[n];
            if(!Player.Place(p,spec.Length,PlacementVertical,spec.Name,spec.SourceType)) {
                Message="That ship overlaps another ship or crosses the edge.";
                return false;
            }
            Message=Player.Ships.Count==Rules.Fleet.Length?"Fleet ready. Start the battle.":"Place your "+Rules.Fleet[Player.Ships.Count].Name.ToLowerInvariant()+" ("+NextLength+" cells).";
            return true;
        }
        public void ClearPlacement() {
            if(Phase!=Phase.Placement)return;
            Player=new Board();
            Message=PlacementMessage;
        }
        public void AutoPlace() {
            if(Phase!=Phase.Placement)return;
            Player=RandomFleet(placementRandom);
            Message="Fleet ready. Start the battle.";
        }
        Board RandomFleet(Random random) {
            var order=new List<int>();
            for(int i=0;i<Rules.Fleet.Length;i++)order.Add(i);
            if(Rules.FixedY)order.Sort(delegate(int a,int b) {
                int length=Rules.Fleet[b].Length.CompareTo(Rules.Fleet[a].Length);
                return length!=0?length:a.CompareTo(b);
            });
            // Native Classic also tries longer ships first and restarts failed layouts.
            // System.Random and these retry bounds are a deliberate bounded substitute.
            for(int attempt=0; attempt<100; attempt++) {
                var board=new Board();
                var positions=new Point[Rules.Fleet.Length];
                var orientations=new bool[Rules.Fleet.Length];
                bool valid=true;
                foreach(int i in order) {
                    ShipSpec spec=Rules.Fleet[i];
                    bool placed=false;
                    for(int j=0; j<200; j++) {
                        var p=new Point(random.Next(10),random.Next(Rules.FixedY?11-spec.Length:10));
                        bool vertical=Rules.FixedY||random.Next(2)==1;
                        if(board.Place(p,spec.Length,vertical,spec.Name,spec.SourceType)) {
                            positions[i]=p;
                            orientations[i]=vertical;
                            placed=true;
                            break;
                        }
                    }
                    if(!placed) {
                        valid=false;
                        break;
                    }
                }
                if(valid) {
                    // Restore roster order after longest-first placement.
                    board=new Board();
                    for(int i=0;i<Rules.Fleet.Length;i++) {
                        ShipSpec spec=Rules.Fleet[i];
                        if(!board.Place(positions[i],spec.Length,orientations[i],spec.Name,spec.SourceType))throw new InvalidOperationException("Fleet reorder failed");
                    }
                    return board;
                }
            }
            throw new InvalidOperationException("Fleet generation failed");
        }
        public bool Start() {
            if(Phase!=Phase.Placement||Player.Ships.Count!=Rules.Fleet.Length)return false;
            Enemy=RandomFleet(placementRandom);
            Phase=Phase.PlayerTurn;
            Message="Your turn: choose a square on the upper-right board.";
            Events.Add("Battle started. Your shot first.");
            return true;
        }
        public bool Fire(Point p) {
            if(Paused||Phase!=Phase.PlayerTurn)return false;
            Shot shot=Enemy.Shoot(p);
            if(shot==null) {
                Message="Choose a square you have not fired at.";
                return false;
            }
            LastPlayerShot=shot;
            Events.Add("You — "+shot.Description);
            Message="You: "+shot.Description;
            if(Enemy.AllSunk) {
                Phase=Phase.Finished;
                Message="Victory — all opposing ships sunk.";
            }
            else Phase=Phase.OpponentTurn;
            return true;
        }
        // Receives only the shot history and results, never the hidden ship layout.
        Point NextTarget() {
            while(hunt.Count>0) {
                Point p=hunt.Dequeue();
                if(Board.Inside(p)&&!Player.Shots[p.X,p.Y])return p;
            }
            var candidates=new List<Point>();
            for(int y=0; y<10; y++)for(int x=0; x<10; x++)if(!Player.Shots[x,y])candidates.Add(new Point(x,y));
            if(candidates.Count==0)throw new InvalidOperationException("No unshot target remains");
            return candidates[shotRandom.Next(candidates.Count)];
        }
        public bool AdvanceOpponent() {
            if(Paused||Phase!=Phase.OpponentTurn)return false;
            Shot shot=Player.Shoot(NextTarget());
            if(shot==null)throw new InvalidOperationException("Opponent repeated a shot");
            LastEnemyShot=shot;
            Events.Add("Opponent — "+shot.Description);
            if(shot.Hit&&!shot.Sunk) {
                hunt.Enqueue(new Point(shot.Cell.X+1,shot.Cell.Y));
                hunt.Enqueue(new Point(shot.Cell.X-1,shot.Cell.Y));
                hunt.Enqueue(new Point(shot.Cell.X,shot.Cell.Y+1));
                hunt.Enqueue(new Point(shot.Cell.X,shot.Cell.Y-1));
            }
            if(Player.AllSunk) {
                Phase=Phase.Finished;
                Message="Defeat — your fleet has been sunk.";
            }
            else {
                Phase=Phase.PlayerTurn;
                Turn++;
                Message="Opponent: "+shot.Description+". Your turn.";
            }
            return true;
        }
        public void TogglePause() {
            if(Phase==Phase.PlayerTurn||Phase==Phase.OpponentTurn)Paused=!Paused;
        }
    }
}
