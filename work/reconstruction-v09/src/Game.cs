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
        public bool Hit,Sunk,Repeated;
        public string ShipName;
        public string Description {
            get {
                return Coordinate(Cell)+": "+(Repeated?"repeat / no new damage":Sunk?"sunk "+ShipName:Hit?"hit":"miss");
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
        public int[,] ObservedShots() {
            var result=new int[10,10];
            for(int x=0;x<10;x++)for(int y=0;y<10;y++)if(Shots[x,y])result[x,y]=ShipAt(new Point(x,y))==null?1:8;
            return result;
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
    // OriginalOpponent recovers the target selector. Layout generation, timing,
    // Weapon lists/dispatch are recovered separately; presentation timing and UI
    // remain modern. Firing movies are identified but not yet decoded/played.
    public sealed class Game {
        public readonly RuleProfile Rules;
        public readonly OpponentKind Opponent;
        public readonly int OpponentLevel;
        public readonly bool MovieTurns;public PendingShot Pending {get;private set;}
        public TargetDecision LastOpponentDecision;
        public Board Player=new Board(),Enemy=new Board();
        public Phase Phase=Phase.Placement;
        public bool Paused,Vertical;
        public int Turn=1;
        public string Message;
        public readonly List<string> Events=new List<string>();
        public Shot LastPlayerShot,LastEnemyShot;
        public FiringPresentation LastPlayerFire,LastEnemyFire;
        public int PlayerShooter { get; private set; }
        public int EnemyShooter { get; private set; }
        readonly Random placementRandom,shotRandom;
        readonly NativeRandom originalRandom;
        readonly NativeRandom presentationRandom;
        readonly NativeRandom travelRandom;
        readonly Queue<Point> hunt=new Queue<Point>();
        public Game(int seed) : this(seed,RuleProfile.Recovered) { }
        public Game(int seed,RuleProfile rules) : this(seed,rules,rules!=null&&rules.FixedY?OpponentKind.Original:OpponentKind.Prototype,0) { }
        public Game(int seed,RuleProfile rules,OpponentKind opponent,int level) : this(seed,rules,opponent,level,false) { }
        public Game(int seed,RuleProfile rules,OpponentKind opponent,int level,bool movieTurns) {
            if(rules==null)throw new ArgumentNullException("rules");
            Rules=rules;MovieTurns=movieTurns&&rules.FixedY;
            if(opponent==OpponentKind.Original) {
                OriginalOpponent.Budget(level);
                if(!rules.FixedY)throw new ArgumentException("Original targeting requires the recovered fixed-axis fleet");
            }
            Opponent=opponent; OpponentLevel=level;
            Vertical=rules.FixedY;
            Message=PlacementMessage;
            placementRandom=new Random(seed);
            shotRandom=new Random(seed^0x17b5);
            // Original targeting algorithm, isolated modern stream. Native
            // weapons/movies consume the shared generator in a different schedule.
            originalRandom=new NativeRandom(unchecked((uint)(seed^0x17b5)));
            // Intentional isolated presentation stream preserves v0.4/v0.5 target
            // histories. Native movies/targeting share one not-yet-recovered schedule.
            presentationRandom=new NativeRandom(unchecked((uint)(seed^0x63d1e)));
            travelRandom=new NativeRandom(unchecked((uint)(seed^0x721cab)));
            PlayerShooter=EnemyShooter=-1;
        }
        string PlacementMessage {
            get { return "Place your "+Rules.Fleet[0].Name.ToLowerInvariant()+", or use Auto-place."; }
        }
        public bool PlacementVertical { get { return Rules.FixedY||Vertical; } }
        public Point PlacementStart(Point cursor) {
            return Rules.FixedY?new Point(cursor.X,cursor.Y-NextLength/2):cursor;
        }
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
            if(!Player.Place(PlacementStart(p),spec.Length,PlacementVertical,spec.Name,spec.SourceType)) {
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
            // Classic solo entry 0x423100 and 0x42311D select roster slot 1.
            PlayerShooter=EnemyShooter=Rules.FixedY?1:-1;
            Phase=Phase.PlayerTurn;
            Message="Your turn: choose a square on the upper-right board.";
            Events.Add("Battle started. Your shot first.");
            Events.Add(Opponent==OpponentKind.Original?"Opponent: original setting "+(OpponentLevel+1)+".":"Opponent: prototype pursuit.");
            return true;
        }
        public bool CanSelectShooter {
            get { return Rules.FixedY&&!Paused&&Pending==null&&Phase==Phase.PlayerTurn; }
        }
        public bool SelectShooter(int index) {
            if(!CanSelectShooter||index<0||index>=Player.Ships.Count||Player.Ships[index].Sunk)return false;
            PlayerShooter=index;
            Message="Firing ship: "+Player.Ships[index].Name+". Choose an opposing square.";
            return true;
        }
        public bool SelectShooter(Point cell) {
            Ship ship=Player.ShipAt(cell);
            return ship!=null&&SelectShooter(Player.Ships.IndexOf(ship));
        }
        static int SurvivingShooter(Board board,int index) {
            if(index>=0&&index<board.Ships.Count&&!board.Ships[index].Sunk)return index;
            for(int i=0;i<board.Ships.Count;i++)if(!board.Ships[i].Sunk)return i;
            return -1;
        }
        void RefreshShooters() {
            if(!Rules.FixedY)return;
            PlayerShooter=SurvivingShooter(Player,PlayerShooter);
            EnemyShooter=SurvivingShooter(Enemy,EnemyShooter);
        }
        public bool Fire(Point p) {
            if(Paused||Pending!=null||Phase!=Phase.PlayerTurn)return false;
            if(!Board.Inside(p)||Enemy.Shots[p.X,p.Y]){Message="Choose a square you have not fired at.";return false;}
            RefreshShooters();if(Rules.FixedY)LastPlayerFire=ClassicWeapons.Choose(Player.Ships[PlayerShooter],false,presentationRandom);
            if(MovieTurns){BeginMovie(false,p,LastPlayerFire);return true;}
            Shot shot=Resolve(false,p);FinishTurn(false,shot);return true;
        }
        void BeginMovie(bool opponent,Point target,FiringPresentation firing){
            // Native travel consumes a shared RNG draw for guns. This separate stream
            // deliberately retains the accepted target AND firing-choice histories.
            bool guns=firing.WeaponId>=14&&firing.WeaponId<=18;
            Pending=new PendingShot(target,opponent,firing,ClassicSequence.Travel(firing.WeaponId,guns?travelRandom.Next():0));
            Message=(opponent?"Opponent":"You")+" firing at "+Shot.Coordinate(target)+".";
        }
        Shot Resolve(bool opponent,Point target){
            Board board=opponent?Player:Enemy;Shot shot=board.Shoot(target);
            if(shot==null){if(!opponent||!Board.Inside(target))throw new InvalidOperationException("Invalid reserved impact");shot=new Shot{Cell=target,Repeated=true};}
            if(opponent)LastEnemyShot=shot;else LastPlayerShot=shot;
            FiringPresentation firing=opponent?LastEnemyFire:LastPlayerFire;
            Events.Add((opponent?"Opponent":"You")+" — "+(firing==null?"":firing.Description+" — ")+shot.Description);
            if(opponent&&Opponent==OpponentKind.Prototype&&shot.Hit&&!shot.Sunk){hunt.Enqueue(new Point(shot.Cell.X+1,shot.Cell.Y));hunt.Enqueue(new Point(shot.Cell.X-1,shot.Cell.Y));hunt.Enqueue(new Point(shot.Cell.X,shot.Cell.Y+1));hunt.Enqueue(new Point(shot.Cell.X,shot.Cell.Y-1));}
            RefreshShooters();Message=(opponent?"Opponent: ":"You: ")+shot.Description;return shot;
        }
        void FinishTurn(bool opponent,Shot shot){
            if((opponent?Player:Enemy).AllSunk){Phase=Phase.Finished;Message=opponent?"Defeat — your fleet has been sunk.":"Victory — all opposing ships sunk.";}
            else if(opponent){Phase=Phase.PlayerTurn;Turn++;Message="Opponent: "+shot.Description+". Your turn.";}
            else Phase=Phase.OpponentTurn;
        }
        public bool CompleteMovie(PendingShot expected,ShotStage stage){
            if(Paused||Pending==null||!object.ReferenceEquals(Pending,expected)||Pending.Stage!=stage)return false;
            if(stage==ShotStage.Firing){Pending.Stage=ShotStage.Travel;Message="Shot travelling to "+Shot.Coordinate(Pending.Cell)+".";return true;}
            if(stage==ShotStage.Travel){
                Pending.Result=Resolve(Pending.Opponent,Pending.Cell);
                bool occupiedRepeat=Pending.Result.Repeated&&(Pending.Opponent?Player:Enemy).ShipAt(Pending.Cell)!=null;
                Pending.FeedbackId=ClassicSequence.Feedback(OpponentLevel,Pending.Result,occupiedRepeat);
                if(Pending.FeedbackId!=0){Pending.Stage=ShotStage.Feedback;return true;}
            }
            PendingShot done=Pending;Pending=null;FinishTurn(done.Opponent,done.Result);return true;
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
            if(Paused||Pending!=null||Phase!=Phase.OpponentTurn)return false;RefreshShooters();Point target;
            if(Opponent==OpponentKind.Original){
                LastOpponentDecision=OriginalOpponent.Choose(Player.ObservedShots(),OpponentLevel,originalRandom);
                if(LastOpponentDecision.Stalled){Paused=true;Message="Original opponent stopped: "+LastOpponentDecision.Reason+". Use F2 to reset.";Events.Add(Message);return false;}target=LastOpponentDecision.Cell;
            }else target=NextTarget();
            if(Rules.FixedY)LastEnemyFire=ClassicWeapons.Choose(Enemy.Ships[EnemyShooter],true,presentationRandom);
            if(MovieTurns){BeginMovie(true,target,LastEnemyFire);return true;}
            Shot shot=Resolve(true,target);FinishTurn(true,shot);return true;
        }
        public void TogglePause() {
            if(Phase==Phase.PlayerTurn||Phase==Phase.OpponentTurn)Paused=!Paused;
        }
    }
}
