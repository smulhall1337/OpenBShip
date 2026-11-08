using System;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab {
    public enum Phase {
        Placement,PlayerTurn,OpponentTurn,Finished
    }
    public sealed class Ship {
        public string Name;
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
        public bool Place(Point start,int length,bool vertical,string name) {
            if(!CanPlace(start,length,vertical))return false;
            var ship=new Ship {
                Name=name
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
    // Experimental rules: 5/4/3/3/2 fleet, adjacency allowed, every shot ends a turn.
    // The original's rule implementation and opponent strategy are not reconstructed.
    public sealed class Game {
        public static readonly int[] Lengths= {
            5,4,3,3,2
        };
        public static readonly string[] Names= {
            "Carrier","Battleship","Cruiser","Submarine","Patrol boat"
        };
        public Board Player=new Board(),Enemy=new Board();
        public Phase Phase=Phase.Placement;
        public bool Paused,Vertical;
        public int Turn=1;
        public string Message="Place your carrier, or use Auto-place.";
        public readonly List<string> Events=new List<string>();
        public Shot LastPlayerShot,LastEnemyShot;
        readonly Random placementRandom,shotRandom;
        readonly Queue<Point> hunt=new Queue<Point>();
        public Game(int seed) {
            placementRandom=new Random(seed);
            shotRandom=new Random(seed^0x17b5);
        }
        public int NextLength {
            get {
                return Player.Ships.Count<5?Lengths[Player.Ships.Count]:0;
            }
        }
        public bool Place(Point p) {
            if(Phase!=Phase.Placement||Player.Ships.Count>=5)return false;
            int n=Player.Ships.Count;
            if(!Player.Place(p,Lengths[n],Vertical,Names[n])) {
                Message="That ship overlaps another ship or crosses the edge.";
                return false;
            }
            Message=Player.Ships.Count==5?"Fleet ready. Start the battle.":"Place your "+Names[Player.Ships.Count].ToLowerInvariant()+" ("+NextLength+" cells).";
            return true;
        }
        public void ClearPlacement() {
            if(Phase!=Phase.Placement)return;
            Player=new Board();
            Message="Place your carrier, or use Auto-place.";
        }
        public void AutoPlace() {
            if(Phase!=Phase.Placement)return;
            Player=RandomFleet(placementRandom);
            Message="Fleet ready. Start the battle.";
        }
        static Board RandomFleet(Random random) {
            for(int attempt=0; attempt<20; attempt++) {
                var board=new Board();
                bool valid=true;
                for(int i=0; i<5; i++) {
                    bool placed=false;
                    for(int j=0; j<200; j++)if(board.Place(new Point(random.Next(10),random.Next(10)),Lengths[i],random.Next(2)==1,Names[i])) {
                        placed=true;
                        break;
                    }
                    if(!placed) {
                        valid=false;
                        break;
                    }
                }
                if(valid)return board;
            }
            throw new InvalidOperationException("Fleet generation failed");
        }
        public bool Start() {
            if(Phase!=Phase.Placement||Player.Ships.Count!=5)return false;
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
