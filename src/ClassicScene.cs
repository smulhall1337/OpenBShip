using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace FleetLab {
    public sealed class ClassicSceneState {
        public readonly Board Own,Enemy;
        public int Selected=1,Phase,OwnResult=-1,EnemyResult=-1,Tick,ActiveOwner;
        public int OwnVisibility=1,EnemyVisibility=1;
        public bool MovieActive,RestoreBackground=true;
        public ClassicVisualTimeline Effects;
        public bool ExtendedEffects;
        public Point Pointer=new Point(-10,-10);
        public ClassicSceneState(Board own,Board enemy){Own=own;Enemy=enemy;}
    }
    public sealed class ClassicSceneCommand {
        public readonly int Priority,X,Y,Frame,Bank,Mode,Percent;
        internal ClassicSceneCommand(int x,int y,int frame,int bank,int mode,int percent) {
            Priority=0;X=x;Y=y;Frame=frame;Bank=bank;Mode=mode;Percent=percent;
        }
    }
    // Reconstructed canonical Classic software world layer: grid/cells, own
    // hulls/smoke, and HUD commands. Native routines 408D50/407800/407890/407950.
    // MovieActive selects HUD layout; decoded movie pixels and text are outside
    // this comparison renderer. Optional effects are supplied by the controller;
    // rendering never advances their counters. RestoreBackground
    // is explicit so a controller can reproduce the original buffer retention.
    public sealed class ClassicScene {
        static readonly int[] Lengths={5,5,3,4,2},Types={7,1,2,12,4};
        static readonly int[] HullBanks={6,10,7,6,13},HullFrames={0,0,0,4,8};
        static readonly Point[] OwnRoster={new Point(13,61),new Point(115,61),new Point(64,94),new Point(13,127),new Point(115,127)};
        static readonly Point[] EnemyRoster={new Point(437,325),new Point(539,325),new Point(488,358),new Point(437,391),new Point(539,391)};
        readonly byte[] background,pixels=new byte[640*480];
        readonly string graphics;
        readonly Color[] palette;
        readonly IndexedImage surface;
        readonly Dictionary<int,List<IndexedImage>> banks=new Dictionary<int,List<IndexedImage>>();
        readonly Dictionary<string,IndexedImage> scaled=new Dictionary<string,IndexedImage>();
        readonly List<ClassicSceneCommand> commands=new List<ClassicSceneCommand>();
        readonly ReadOnlyCollection<ClassicSceneCommand> commandView;
        public ReadOnlyCollection<ClassicSceneCommand> Commands {get{return commandView;}}
        public ClassicScene(string graphics) {
            this.graphics=graphics;
            var pcx=Assets.Pcx(Path.Combine(graphics,"E_CLASS.PCX"));
            if(pcx.Width!=640||pcx.Height!=512)throw new InvalidDataException("Unsupported Classic scene background");
            background=new byte[640*480];Array.Copy(pcx.Pixels,background,background.Length);
            palette=ClassicPalette.Gameplay(File.ReadAllBytes(Path.Combine(graphics,"ISLAND.COL")));
            surface=new IndexedImage{Width=640,Height=480,Pixels=pixels,Palette=palette};
            string[] names={"ALLSHIPS.SPH","BATTSHIP.SPH","FRIGATE.SPH","NEWSHIPS.SPH","SMOKE.SPH","EO_CLASS.SPH","E_CLAS2.SPH"};
            int[] ids={6,10,7,13,15,40,41};
            for(int i=0;i<ids.Length;i++)banks.Add(ids[i],Assets.Sph(Path.Combine(graphics,names[i]),palette));
            commandView=commands.AsReadOnly();Reset();
        }
        public void Reset(){Array.Copy(background,pixels,pixels.Length);commands.Clear();}
        public void CopyPixelsTo(byte[] destination) {
            if(destination==null||destination.Length!=pixels.Length)throw new InvalidDataException("Classic scene destination must be 640x480");
            Array.Copy(pixels,destination,pixels.Length);
        }
        public IndexedImage Snapshot(){return new IndexedImage{Width=640,Height=480,Pixels=(byte[])pixels.Clone(),Palette=(Color[])palette.Clone()};}
        static void ValidateBoard(Board board) {
            if(board==null||board.Ships.Count!=5)throw new InvalidDataException("Classic scene requires the five-ship Classic fleet");
            for(int i=0;i<5;i++) {
                Ship ship=board.Ships[i];
                if(ship==null||ship.SourceType!=Types[i]||ship.Cells.Count!=Lengths[i]||ship.Hits<0||ship.Hits>Lengths[i])throw new InvalidDataException("Unsupported Classic scene ship");
                for(int n=0;n<ship.Cells.Count;n++)if(!Board.Inside(ship.Cells[n])||ship.Cells[n].X!=ship.Cells[0].X||ship.Cells[n].Y!=ship.Cells[0].Y+n)throw new InvalidDataException("Classic scene requires fixed-Y ship placement");
            }
        }
        static void Cells(Board board,out int[,] map,out Point[] centers) {
            map=new int[10,10];var alive=new List<Point>();
            for(int x=0;x<10;x++)for(int y=0;y<10;y++){bool ship=board.ShipAt(new Point(x,y))!=null;map[x,y]=board.Shots[x,y]?(ship?8:1):(ship?2:0);}
            foreach(var ship in board.Ships)if(!ship.Sunk)alive.Add(new Point(ship.Cells[0].X,ship.Cells[0].Y+ship.Cells.Count/2));
            centers=alive.ToArray();
        }
        void Add(int x,int y,int frame,int bank,int mode,int percent) {
            if(x< -100||x>740||y< -100||y>580||commands.Count>=270)return;
            commands.Add(new ClassicSceneCommand(x,y,frame,bank,mode,percent));
        }
        void Hulls(ClassicSceneState state) {
            for(int slot=0;slot<5;slot++) {
                Ship ship=state.Own.Ships[slot];
                if(ship.Sunk||slot==state.Selected&&(state.Tick&12)==0)continue;
                Point at=ClassicGeometry.ShipAnchor(false,ship.Cells[0],ship.Cells.Count);
                Add(at.X,at.Y,HullFrames[slot],HullBanks[slot],1,ClassicGeometry.ShipPercent);
                for(int segment=0;segment<ship.Cells.Count;segment++) {
                    Point cell=ship.Cells[segment];if(!state.Own.Shots[cell.X,cell.Y])continue;
                    at=ClassicDamage.Anchor(cell);Add(at.X,at.Y,ClassicDamage.Frame(state.Tick,segment),15,2,ClassicGeometry.ShipPercent);
                }
            }
        }
        void ValidateEffects(ClassicSceneState state) {
            if(state.Effects==null)return;
            foreach(var entry in state.Effects.Entries) {
                if(!Board.Inside(entry.Start)||entry.Counter<0||entry.Counter>entry.Spec.LastCounter)throw new InvalidDataException("Unsupported Classic scene effect");
                if(entry.Spec.Type<=26&&!state.ExtendedEffects)continue;
                if(banks.ContainsKey(entry.Spec.SpriteBank))continue;
                var frames=Assets.Sph(Path.Combine(graphics,entry.Spec.Resource),palette);
                if(frames.Count!=entry.Spec.LastCounter+1||frames[frames.Count-1]!=null)throw new InvalidDataException("Unsupported Classic scene effect bank");
                for(int i=0;i<frames.Count-1;i++)if(frames[i]==null)throw new InvalidDataException("Missing Classic scene effect frame");
                banks.Add(entry.Spec.SpriteBank,frames);
            }
        }
        void Effects(ClassicSceneState state,bool enemy) {
            if(state.Effects==null)return;
            foreach(var entry in state.Effects.Entries) {
                if(entry.Enemy!=enemy||entry.Spec.Type<=26&&!state.ExtendedEffects)continue;
                Point at=ClassicVisualGeometry.Anchor(entry.Spec,enemy,entry.Start);
                Add(at.X,at.Y,entry.Counter,entry.Spec.SpriteBank,entry.Spec.Type==27?0:3,ClassicGeometry.ShipPercent);
            }
        }
        void Hud(ClassicSceneState state) {
            if(state.MovieActive){Add(0,0,0,41,0,100);Add(200,0,2,41,0,100);Add(235,264,1,41,0,100);Add(435,264,3,41,0,100);}
            bool enemy=state.Phase>3;Add(enemy?382:232,enemy?392:56,enemy?1:0,40,0,100);
            Board board=enemy?state.Enemy:state.Own;Point[] roster=enemy?EnemyRoster:OwnRoster;
            for(int i=0;i<5;i++)Add(roster[i].X,roster[i].Y,(board.Ships[i].Sunk?9:4)+i,40,0,100);
            if((state.Phase==3||state.Phase==4)&&state.EnemyResult!=-1)Add(512,272,state.EnemyResult+2,40,0,100);
            if((state.Phase==7||state.Phase==8)&&state.OwnResult!=-1)Add(32,176,state.OwnResult+2,40,0,100);
        }
        void Paint(ClassicSceneCommand command) {
            List<IndexedImage> frames=banks[command.Bank];int frame=command.Frame;
            if(frame<0||frame>=frames.Count-1||frames[frame]==null)return;
            string key=command.Bank+":"+frame+":"+command.Percent;IndexedImage image;
            if(!scaled.TryGetValue(key,out image)){image=command.Percent==100?frames[frame]:ClassicShips.Scale(frames[frame],command.Percent);scaled.Add(key,image);}
            int x=command.X,y=command.Y;
            if(command.Mode==3){x+=frames[frame].OriginX;y+=frames[frame].OriginY;}
            if(command.Mode==1||command.Mode==3){x-=image.Width/2;y-=image.Height/2;}
            else if(command.Mode==2){x-=image.Width/2;y-=image.Height;}
            for(int sy=0;sy<image.Height;sy++) {
                int py=y+sy;if(py<0||py>=480)continue;
                for(int sx=0;sx<image.Width;sx++){int px=x+sx,at=sy*image.Width+sx;if(px>=0&&px<640&&image.Visible[at]&&image.Pixels[at]!=0)pixels[py*640+px]=image.Pixels[at];}
            }
        }
        public void Render(ClassicSceneState state) {
            if(state==null||state.Selected<0||state.Selected>4||state.Phase<0||state.Phase>8||state.OwnResult< -1||state.OwnResult>1||state.EnemyResult< -1||state.EnemyResult>1||state.Tick<0||state.Tick>int.MaxValue-80||state.ActiveOwner<0||state.ActiveOwner>1||state.OwnVisibility<0||state.OwnVisibility>255||state.EnemyVisibility<0||state.EnemyVisibility>255)throw new InvalidDataException("Unsupported Classic scene state");
            ValidateBoard(state.Own);ValidateBoard(state.Enemy);
            ValidateEffects(state);
            int[,] ownMap,enemyMap;Point[] ownCenters,enemyCenters;
            Cells(state.Own,out ownMap,out ownCenters);Cells(state.Enemy,out enemyMap,out enemyCenters);
            commands.Clear();if(state.RestoreBackground)Array.Copy(background,pixels,pixels.Length);
            ClassicGrid.Grid(surface,0);
            ClassicCellRaster.Overlay(pixels,ownMap,new Point(0,0),0,0,state.ActiveOwner,state.OwnVisibility,ownCenters);
            Effects(state,false);Hulls(state);ClassicGrid.Border(surface,0,13);ClassicGrid.Border(surface,11,243);ClassicGrid.Grid(surface,11);
            ClassicCellRaster.Overlay(pixels,enemyMap,new Point(11,0),0,1,state.ActiveOwner,state.EnemyVisibility,enemyCenters);
            Effects(state,true);
            ClassicGrid.Border(surface,0,13);
            if(state.Phase==0&&state.Pointer.X>=11&&state.Pointer.X<21&&state.Pointer.Y>=0&&state.Pointer.Y<10)ClassicCellRaster.Fill(pixels,state.Pointer.X,state.Pointer.Y,11,0);
            Hud(state);
            // Original descending selection sort. Equal priorities retain the
            // source swap behavior; canonical commands currently all use zero.
            for(int i=0;i<commands.Count;i++){int best=i,priority=0;for(int j=i;j<commands.Count;j++)if(commands[j].Priority>priority){best=j;priority=commands[j].Priority;}var previous=commands[i];commands[i]=commands[best];commands[best]=previous;}
            foreach(var command in commands)Paint(command);
        }
        static Board[] PreviewBoards() {
            var random=new NativeRandom(0);var boards=new[]{new Board(),new Board()};
            foreach(var board in boards){var placement=OriginalPlacement.Generate(Lengths,random.Next);for(int i=0;i<5;i++)if(!board.Place(placement.Positions[i],Lengths[i],true,RuleProfile.Recovered.Fleet[i].Name,Types[i]))throw new InvalidDataException("Classic software preview placement failed");}
            return boards;
        }
        public static IndexedImage Preview(string graphics) {
            var boards=PreviewBoards();
            var scene=new ClassicScene(graphics);scene.Render(new ClassicSceneState(boards[0],boards[1]){Tick=4});return scene.Snapshot();
        }
        // Replay the first eleven supplied frames of the native seed-0 miss
        // fixture. Decoder completion is controlled; movie pixels are absent.
        public static IndexedImage FirstMissPreview(string graphics,byte[] records) {
            var catalog=new ClassicVisualCatalog(records);var boards=PreviewBoards();
            var scene=new ClassicScene(graphics);var state=new ClassicSceneState(boards[0],boards[1]);
            for(int frame=0;frame<=9;frame++) {
                state.Tick=frame+1;state.Selected=frame==0?1:0;state.Phase=frame<4?0:frame<7?1:2;
                state.MovieActive=frame>=4;state.RestoreBackground=frame<=4;
                state.Pointer=frame==1?new Point(8,3):frame>=2&&frame<=4?new Point(11,0):new Point(-10,-10);
                scene.Render(state);
            }
            boards[1].Shoot(Point.Empty);var timeline=new ClassicVisualTimeline();
            timeline.Spawn(catalog.Get(27),true,Point.Empty,1);
            state.Effects=timeline;state.Tick=11;state.Phase=3;state.EnemyResult=0;state.Pointer=new Point(-10,-10);
            scene.Render(state);return scene.Snapshot();
        }
    }
}
