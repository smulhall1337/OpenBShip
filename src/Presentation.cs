using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Media;
namespace FleetLab {
    public sealed class BoardView : Control {
        public Game Game;
        internal BoardRevealTimeline Reveal;
        internal bool Revealing {get{return Reveal!=null&&!Reveal.Complete;}}
        public Bitmap Art;
        public Bitmap ForegroundArt,MovieFrame,LegacyArt; // Borrowed; owners release them after replacement.
        public ClassicHud Hud;public ClassicMessages Messages;public ClassicSpriteFont MessageFont;
        public Bitmap[] Roster;
        public ClassicShips Ships;
        public ClassicDamage Damage;
        public long DamageTick;
        public ClassicVisualSprites VisualSprites; // Borrowed from MainForm.
        public bool AnimateVisualEffects=true;
        readonly ClassicVisualTimeline visualEffects=new ClassicVisualTimeline();
        Game visualGame;
        Shot visualPlayerShot,visualEnemyShot;
        long visualTick;
        internal int VisualEffectCount {get{return visualEffects.Count;}}
        internal IEnumerable<ClassicVisualEntry> VisualEntries {get{return visualEffects.Entries;}}
        internal void ResetVisualEffects() {
            visualEffects.Clear();visualGame=Game;visualTick=DamageTick;
            visualPlayerShot=Game==null?null:Game.LastPlayerShot;
            visualEnemyShot=Game==null?null:Game.LastEnemyShot;
        }
        internal void SynchronizeVisualEffects() {
            if(!object.ReferenceEquals(visualGame,Game)||DamageTick<visualTick) {ResetVisualEffects();return;}
            if(Game==null||Legacy||Game.Phase==Phase.Placement||!AnimateVisualEffects||VisualSprites==null) {ResetVisualEffects();return;}
            long ticks=DamageTick-visualTick;visualTick=DamageTick;
            if(!Game.Paused)visualEffects.Advance(ticks);
            if(!object.ReferenceEquals(visualPlayerShot,Game.LastPlayerShot)) {
                visualPlayerShot=Game.LastPlayerShot;SpawnVisualImpact(Game.Enemy,visualPlayerShot,true);
            }
            if(!object.ReferenceEquals(visualEnemyShot,Game.LastEnemyShot)) {
                visualEnemyShot=Game.LastEnemyShot;SpawnVisualImpact(Game.Player,visualEnemyShot,false);
            }
        }
        void SpawnVisualImpact(Board victim,Shot shot,bool enemy) {
            int type;Point start;
            if(ClassicVisualGeometry.Impact(victim,shot,out type,out start))
                // Native Classic creates entries before the same frame's aging
                // pass. Its first draw therefore uses counter 1 (checkpoint59).
                visualEffects.Spawn(VisualSprites.Catalog.Get(type),enemy,start,1);
        }
        public event Action<string> ActionDone;
        public Point KeyboardCell=new Point(0,0);
        Point? hover;
        Point? pointerPosition;
        bool pressDuringReveal;
        ClassicCursors nativeCursors;
        internal bool HasOriginalCursors {get{return nativeCursors!=null;}}
        internal void LoadCursors(string programs){
            if(IsDisposed||Disposing)return;
            var next=ClassicCursors.TryLoad(programs);Cursor=Cursors.Default;
            if(nativeCursors!=null)nativeCursors.Dispose();nativeCursors=next;RefreshInteractionCursor();
        }
        internal BoardPointer PointerAt(PointF at){
            if(Game==null||Game.Paused||Game.Phase==Phase.Finished||Revealing)return BoardPointer.Arrow;
            if(Legacy)return SelectableShip(at)>=0?BoardPointer.Hand:BoardPointer.Arrow;
            if(!Legacy&&(Game.Pending!=null||Game.Phase==Phase.OpponentTurn))return BoardPointer.Wait;
            if(SelectableShip(at)>=0)return BoardPointer.Hand;
            var cell=HitCell(true,at,Legacy);
            if(Game.Phase==Phase.PlayerTurn&&Game.Pending==null&&cell.HasValue&&!Game.Enemy.Shots[cell.Value.X,cell.Value.Y])return BoardPointer.Target;
            return BoardPointer.Arrow;
        }
        internal void RefreshInteractionCursor(){
            if(IsDisposed||Disposing)return;
            BoardPointer kind=BoardPointer.Arrow;RectangleF r=BoundsForArt();
            if(pointerPosition.HasValue&&r.Width>0&&r.Contains(pointerPosition.Value)){
                Point p=pointerPosition.Value;kind=PointerAt(new PointF((p.X-r.X)*640/r.Width,(p.Y-r.Y)*ArtHeight/r.Height));
            }
            Cursor=kind==BoardPointer.Target?(nativeCursors==null?Cursors.Cross:nativeCursors.Target):kind==BoardPointer.Hand?(nativeCursors==null?Cursors.Hand:nativeCursors.Hand):kind==BoardPointer.Wait?Cursors.WaitCursor:Cursors.Default;
        }
        protected override void OnResize(EventArgs e){base.OnResize(e);RefreshInteractionCursor();}
        protected override void Dispose(bool disposing){
            if(disposing){Cursor=Cursors.Default;if(nativeCursors!=null){nativeCursors.Dispose();nativeCursors=null;}}
            base.Dispose(disposing);
        }
        bool Legacy { get { return Game!=null&&!Game.Rules.FixedY; } }
        internal List<MessageLine> PauseMessages {get{return Game!=null&&Game.Paused&&!Legacy&&MessageFont!=null&&Messages!=null?MessageFont.PauseLines(Messages,640,false):null;}}
        internal bool CinematicBackdropActive {get{return !Legacy&&MovieFrame!=null&&ForegroundArt!=null;}}
        internal int ArtHeight {get{return Legacy?512:ClassicGeometry.ViewHeight;}}
        bool NativeHudCues {get{return !Legacy&&Game.MovieTurns&&(Game.Phase==Phase.PlayerTurn||Game.Phase==Phase.OpponentTurn)&&Hud!=null;}}
        // Retained only for the v0.1 preset; recovered Classic uses native axes.
        static readonly PointF[] Own= {
            new PointF(222,175),new PointF(418,305),new PointF(222,436),new PointF(26,305)
        };
        static readonly PointF[] Opp= {
            new PointF(436,32),new PointF(632,162),new PointF(437,292),new PointF(240,162)
        };
        // Original EO_CLASS draw coordinates, Bs.exe 0x409434 / 0x40949E.
        static readonly Point[] OwnRoster= {new Point(13,61),new Point(115,61),new Point(64,94),new Point(13,127),new Point(115,127)};
        static readonly Point[] EnemyRoster= {new Point(437,325),new Point(539,325),new Point(488,358),new Point(437,391),new Point(539,391)};
        public static Point RosterPosition(bool enemy,int slot) { return (enemy?EnemyRoster:OwnRoster)[slot]; }
        public static int RosterFrame(Board board,int slot) { return (slot<board.Ships.Count&&board.Ships[slot].Sunk?9:4)+slot; }
        void DrawRoster(Graphics g,Board board,bool enemy) {
            if(Roster==null||!Game.Rules.FixedY)return;
            for(int i=0;i<5;i++) {
                int frame=RosterFrame(board,i);
                Point p=RosterPosition(enemy,i);
                if(frame<Roster.Length&&Roster[frame]!=null)g.DrawImage(Roster[frame],new Rectangle(p,Roster[frame].Size));
                if(!enemy&&i==Game.PlayerShooter&&Game.Phase!=Phase.Placement&&!board.Ships[i].Sunk)
                    using(var pen=new Pen(Color.FromArgb(250,235,198,113),1.5f))g.DrawRectangle(pen,p.X-1,p.Y-1,90,32);
            }
        }
        void DrawResult(Graphics g,Shot shot,bool enemy) {
            if(Legacy||Roster==null||shot==null)return;
            Bitmap image=Roster[shot.Hit?3:2];
            Point at=enemy?new Point(512,272):new Point(32,176);
            g.DrawImage(image,new Rectangle(at,image.Size));
        }
        public static bool RevealsShip(bool enemy,Phase phase,Ship ship) {
            return !ship.Sunk&&(!enemy||phase==Phase.Finished);
        }
        void DrawShip(Graphics g,bool enemy,Point start,int length,int type,bool preview) {
            Bitmap image=Ships.Image(type);
            Rectangle bounds=ClassicShips.Bounds(ClassicGeometry.ShipAnchor(enemy,start,length),image.Size);
            if(!preview)g.DrawImage(image,bounds);
            else using(var attributes=new ImageAttributes()) {
                var matrix=new ColorMatrix();
                matrix.Matrix33=.65f;
                attributes.SetColorMatrix(matrix);
                g.DrawImage(image,bounds,0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);
            }
        }
        void DrawShips(Graphics g,Board board,bool enemy) {
            if(Legacy||Ships==null)return;
            // Native fleet draw loop visits roster slots in order, orientation 0.
            foreach(var ship in board.Ships)if(RevealsShip(enemy,Game.Phase,ship)) {
                DrawShip(g,enemy,ship.Cells[0],ship.Cells.Count,ship.SourceType,false);
                if(!enemy&&Damage!=null)Damage.Draw(g,board,ship,DamageTick);
            }
        }
        public BoardView() {
            DoubleBuffered=true;
            BackColor=Color.FromArgb(8,15,24);
            TabStop=true;
            ResizeRedraw=true;
        }
        RectangleF BoundsForArt() {
            float s=Math.Min(ClientSize.Width/640f,ClientSize.Height/(float)ArtHeight);
            return new RectangleF((ClientSize.Width-640*s)/2,(ClientSize.Height-ArtHeight*s)/2,640*s,ArtHeight*s);
        }
        internal RectangleF ImageBounds { get { return BoundsForArt(); } }
        public static PointF Project(bool enemy,float u,float v,bool legacy=false) {
            if(!legacy)return ClassicGeometry.Project(enemy,u*10,v*10);
            var c=enemy?Opp:Own;
            return new PointF((1-u)*(1-v)*c[0].X+u*(1-v)*c[1].X+u*v*c[2].X+(1-u)*v*c[3].X,(1-u)*(1-v)*c[0].Y+u*(1-v)*c[1].Y+u*v*c[2].Y+(1-u)*v*c[3].Y);
        }
        public static PointF[] Polygon(bool enemy,int x,int y,bool legacy=false) {
            return new[] {
                Project(enemy,x/10f,y/10f,legacy),Project(enemy,(x+1)/10f,y/10f,legacy),Project(enemy,(x+1)/10f,(y+1)/10f,legacy),Project(enemy,x/10f,(y+1)/10f,legacy)
            };
        }
        static bool Contains(PointF[] poly,PointF p) {
            bool inside=false;
            for(int i=0,j=poly.Length-1; i<poly.Length; j=i++)if((poly[i].Y>p.Y)!=(poly[j].Y>p.Y)&&p.X<(poly[j].X-poly[i].X)*(p.Y-poly[i].Y)/(poly[j].Y-poly[i].Y)+poly[i].X)inside=!inside;
            return inside;
        }
        public static Point? HitCell(bool enemy,PointF p,bool legacy=false) {
            if(!legacy)return ClassicGeometry.Pick(enemy,p);
            for(int y=0; y<10; y++)for(int x=0; x<10; x++)if(Contains(Polygon(enemy,x,y,true),p))return new Point(x,y);
            return null;
        }
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            pointerPosition=e.Location;RefreshInteractionCursor();
            if(Revealing)return;
            RectangleF r=BoundsForArt();
            if(r.Width<=0||Game==null)return;
            var at=new PointF((e.X-r.X)*640/r.Width,(e.Y-r.Y)*ArtHeight/r.Height);
            hover=HitCell(Game.Phase!=Phase.Placement,at,Legacy);
            if(hover.HasValue)KeyboardCell=hover.Value;
            Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            pointerPosition=null;RefreshInteractionCursor();
            hover=null;
            Invalidate();
        }
        protected override void OnMouseClick(MouseEventArgs e) {
            base.OnMouseClick(e);
            Focus();
            if(Revealing||pressDuringReveal)return;
            RectangleF r=BoundsForArt();
            if(e.Button!=MouseButtons.Left||r.Width<=0)return;
            var at=new PointF((e.X-r.X)*640/r.Width,(e.Y-r.Y)*ArtHeight/r.Height);
            int selection=SelectableShip(at);
            if(selection>=0) {
                bool accepted=Game.SelectShooter(selection);
                if(ActionDone!=null)ActionDone(accepted?"place":"none");
                Invalidate();return;
            }
            var cell=HitCell(Game.Phase!=Phase.Placement,at,Legacy);
            if(cell.HasValue) {
                KeyboardCell=cell.Value;
                Act();
            }
        }
        protected override void OnMouseDown(MouseEventArgs e){pressDuringReveal=Revealing;base.OnMouseDown(e);}
        int SelectableShip(PointF at) {
            if(Game==null||!Game.CanSelectShooter)return -1;
            for(int i=0;i<Game.Player.Ships.Count;i++)if(!Game.Player.Ships[i].Sunk&&new Rectangle(OwnRoster[i],new Size(88,30)).Contains(Point.Round(at)))return i;
            Point? own=HitCell(false,at,false);
            Ship ship=own.HasValue?Game.Player.ShipAt(own.Value):null;
            return ship!=null&&!ship.Sunk?Game.Player.Ships.IndexOf(ship):-1;
        }
        public void Act() {
            if(Game.Paused||Revealing)return;
            bool placement=Game.Phase==Phase.Placement;
            bool accepted=placement?Game.Place(KeyboardCell):Game.Fire(KeyboardCell);
            if(ActionDone!=null)ActionDone(accepted?(placement?"place":"fire"):"none");
            Invalidate();
        }
        public void MoveCursor(int x,int y) {
            if(Revealing)return;
            KeyboardCell=new Point(Math.Max(0,Math.Min(9,KeyboardCell.X+x)),Math.Max(0,Math.Min(9,KeyboardCell.Y+y)));
            hover=KeyboardCell;
            Invalidate();
        }
        void CellTint(Graphics g,bool enemy,Point p,Color color) {
            using(var b=new SolidBrush(color))g.FillPolygon(b,Polygon(enemy,p.X,p.Y,Legacy));
        }
        void DrawBoard(Graphics g,Board board,bool enemy) {
            for(int y=0; y<10; y++)for(int x=0; x<10; x++) {
                var p=new Point(x,y);
                Ship ship=board.ShipAt(p);
                if(Legacy&&ship!=null&&(!enemy||Game.Phase==Phase.Finished))CellTint(g,enemy,p,Color.FromArgb(125,80,170,205));
                if(!board.Shots[x,y])continue;
                var center=Project(enemy,(x+.5f)/10,(y+.5f)/10,Legacy);
                using(var pen=new Pen(ship==null?Color.FromArgb(230,225,240,245):Color.FromArgb(255,255,144,72),1.7f)) {
                    if(ship==null)g.DrawEllipse(pen,center.X-2.7f,center.Y-2.7f,5.4f,5.4f);
                    else {
                        CellTint(g,enemy,p,Color.FromArgb(120,201,64,35));
                        g.DrawLine(pen,center.X-4,center.Y-4,center.X+4,center.Y+4);
                        g.DrawLine(pen,center.X-4,center.Y+4,center.X+4,center.Y-4);
                    }
                }
            }
        }
        internal List<ClassicHudCommand> CurrentHudCommands {get{
            if(!NativeHudCues)return null;
            int phase=ClassicHud.Phase(Game),mask=0;Board fleet=phase>3?Game.Enemy:Game.Player;for(int i=0;i<Math.Min(5,fleet.Ships.Count);i++)if(!fleet.Ships[i].Sunk)mask|=1<<i;
            int own=Game.LastEnemyShot==null?-1:Game.LastEnemyShot.Hit?1:0,enemy=Game.LastPlayerShot==null?-1:Game.LastPlayerShot.Hit?1:0;
            return ClassicHud.Commands(phase,CinematicBackdropActive,own,enemy,mask);
        }}
        internal List<MessageLine> CurrentMessages {get{if(Legacy||Game==null||Messages==null||Game.Phase==Phase.Placement||Game.Phase==Phase.Finished)return null;int phase=ClassicHud.Phase(Game),result=0,sunk=0,shooter=Game.PlayerShooter>=0&&Game.PlayerShooter<Game.Player.Ships.Count?Game.Player.Ships[Game.PlayerShooter].SourceType:1;if(Game.Pending!=null&&Game.Pending.Result!=null){result=Game.Pending.Result.Sunk?2:Game.Pending.Result.Hit?1:0;var ship=(Game.Pending.Opponent?Game.Player:Game.Enemy).ShipAt(Game.Pending.Cell);if(ship!=null)sunk=ship.SourceType;}return Messages.Commands(phase,0,shooter,sunk,result,Messages.Line(105));}}
        void DrawNativeHud(Graphics g){
            int phase=ClassicHud.Phase(Game);foreach(var command in CurrentHudCommands){
                Bitmap image=command.Panel?Hud.Panel(command.Frame):Roster[command.Frame];g.DrawImage(image,new Rectangle(command.At,image.Size));
                if(!command.Panel&&phase<=3&&command.Frame==4+Game.PlayerShooter)using(var pen=new Pen(Color.FromArgb(250,235,198,113),1.5f))g.DrawRectangle(pen,command.At.X-1,command.At.Y-1,90,32);
            }
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if(Game==null||Art==null)return;
            SynchronizeVisualEffects();
            RectangleF r=BoundsForArt();
            if(r.Width<=0)return;
            var g=e.Graphics;
            var saved=g.Save();
            bool backdrop=CinematicBackdropActive;
            if(backdrop)ClassicBackdrop.Draw(g,MovieFrame,ClientRectangle);
            g.TranslateTransform(r.X,r.Y);
            g.ScaleTransform(r.Width/640,r.Height/ArtHeight);
            g.InterpolationMode=InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawImage(Legacy&&LegacyArt!=null?LegacyArt:backdrop?ForegroundArt:Art,new Rectangle(0,0,640,ArtHeight),0,0,640,ArtHeight,GraphicsUnit.Pixel);
            DrawShips(g,Game.Player,false);
            DrawShips(g,Game.Enemy,true);
            if(VisualSprites!=null&&!Legacy)VisualSprites.Draw(g,visualEffects);
            g.SmoothingMode=SmoothingMode.AntiAlias;
            DrawBoard(g,Game.Player,false);
            DrawBoard(g,Game.Enemy,true);
            if(!Legacy&&Game.Phase!=Phase.Placement&&Game.PlayerShooter>=0) {
                Ship selected=Game.Player.Ships[Game.PlayerShooter];
                if(!selected.Sunk)using(var pen=new Pen(Color.FromArgb(245,235,198,113),1.3f))
                    foreach(Point cell in selected.Cells)g.DrawPolygon(pen,Polygon(false,cell.X,cell.Y,false));
            }
            if(Game.Phase==Phase.Placement&&Game.NextLength>0) {
                Point p=Game.PlacementStart(hover??KeyboardCell);
                bool valid=Game.Player.CanPlace(p,Game.NextLength,Game.PlacementVertical);
                for(int i=0; i<Game.NextLength; i++) {
                    var cell=new Point(p.X+(Game.PlacementVertical?0:i),p.Y+(Game.PlacementVertical?i:0));
                    if(Board.Inside(cell))CellTint(g,false,cell,valid?Color.FromArgb(130,90,220,175):Color.FromArgb(160,240,75,65));
                }
                if(valid&&!Legacy&&Ships!=null) {
                    var spec=Game.Rules.Fleet[Game.Player.Ships.Count];
                    DrawShip(g,false,p,spec.Length,spec.SourceType,true);
                }
            }
            if(!Game.Paused&&(Game.Phase==Phase.PlayerTurn||Game.Phase==Phase.Placement)) {
                bool enemy=Game.Phase==Phase.PlayerTurn;
                using(var pen=new Pen(Color.FromArgb(250,235,198,113),1.3f))g.DrawPolygon(pen,Polygon(enemy,KeyboardCell.X,KeyboardCell.Y,Legacy));
            }
            if(NativeHudCues)DrawNativeHud(g);
            else {DrawRoster(g,Game.Player,false);DrawRoster(g,Game.Enemy,true);DrawResult(g,Game.LastEnemyShot,false);DrawResult(g,Game.LastPlayerShot,true);}
            using(var font=new Font("Segoe UI",10,FontStyle.Bold))using(var brush=new SolidBrush(Color.FromArgb(235,225,203))) {
                if(CurrentMessages==null)g.DrawString("YOUR FLEET",font,brush,16,10);
                g.DrawString("OPPONENT",font,brush,287,450);
            }
            var messages=CurrentMessages;if(messages!=null&&MessageFont!=null)foreach(var line in messages)MessageFont.Draw(g,line);
            string caption=Legacy?"v0.1 provisional fleet and board mapping  /  rotation enabled":Game.Opponent==OpponentKind.Original?"Original ship art and targeting  /  modern hit markers":"Original ship art  /  prototype pursuit and modern hit markers";
            if(Legacy)using(var font=new Font("Segoe UI",9))using(var brush=new SolidBrush(Color.LightSteelBlue))g.DrawString(caption,font,brush,14,488);
            if(Game.Paused||Game.Phase==Phase.Finished) {
                var paused=PauseMessages;if(paused!=null){
                    // Readability scrim is modern; source text positions/font are native.
                    using(var b=new SolidBrush(Color.FromArgb(210,7,18,30)))g.FillRectangle(b,0,167,640,132);
                    foreach(var line in paused)MessageFont.Draw(g,line,4);
                }else{
                using(var b=new SolidBrush(Color.FromArgb(210,7,18,30)))g.FillRectangle(b,0,175,640,115);
                using(var font=new Font("Segoe UI",24,FontStyle.Bold))using(var b=new SolidBrush(Color.FromArgb(239,203,128)))g.DrawString(Game.Paused?"PAUSED":Game.Enemy.AllSunk?"VICTORY":"FLEET LOST",font,b,190,191);
                using(var font=new Font("Segoe UI",10))using(var b=new SolidBrush(Color.WhiteSmoke))g.DrawString(Game.Paused?"Press P or Resume to continue.":"Use New battle (F2) to play again.",font,b,190,242);
                }
            }
            g.Restore(saved);
            if(Reveal!=null&&!Reveal.Complete)using(var dim=new SolidBrush(Color.FromArgb(255-Reveal.Factor*255/64,0,0,0)))g.FillRectangle(dim,ClientRectangle);
        }
    }
    public sealed class MapView : Control {
        public Scenario Scene;
        public PracticeDeployment Deployment;
        public int SelectedFleet;
        public string PreviewProblem;
        public event Action<int> FleetSelected;
        RectangleF MapBounds {
            get { float size=Math.Max(0,Math.Min(Width-40,Height-104));return new RectangleF((Width-size)/2,16,size,size); }
        }
        internal Point FleetMarker(int index) {
            if(Deployment==null||index<0||index>=Deployment.Fleets.Count)throw new ArgumentOutOfRangeException("index");
            var bounds=MapBounds;var origin=Deployment.Fleets[index].WorldOrigin;
            return new Point((int)(bounds.X+(origin.X/10f+.5f)*bounds.Width/64),
                             (int)(bounds.Y+(origin.Y/10f+.5f)*bounds.Height/64));
        }
        Point[] FleetLabels() {
            if(Deployment==null)return new Point[0];
            var labels=new Point[Deployment.Fleets.Count];var bounds=MapBounds;
            Point[] offsets={Point.Empty,new Point(0,-18),new Point(0,18),new Point(-18,0),new Point(18,0),
                new Point(-18,-18),new Point(18,-18),new Point(-18,18),new Point(18,18),new Point(0,-36),new Point(0,36)};
            for(int i=0;i<labels.Length;i++){
                Point marker=FleetMarker(i);labels[i]=marker;
                foreach(Point offset in offsets){
                    Point candidate=new Point((int)Math.Max(bounds.Left+11,Math.Min(bounds.Right-11,marker.X+offset.X)),
                                              (int)Math.Max(bounds.Top+11,Math.Min(bounds.Bottom-11,marker.Y+offset.Y)));
                    bool clear=true;
                    for(int prior=0;prior<i&&clear;prior++)clear=Math.Abs(candidate.X-labels[prior].X)>=18||Math.Abs(candidate.Y-labels[prior].Y)>=18;
                    for(int other=0;other<labels.Length&&clear;other++)if(other!=i){Point point=FleetMarker(other);clear=Math.Abs(candidate.X-point.X)>=13||Math.Abs(candidate.Y-point.Y)>=13;}
                    if(clear){labels[i]=candidate;break;}
                }
            }
            return labels;
        }
        internal Point FleetLabel(int index) { return FleetLabels()[index]; }
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if(e.Button!=MouseButtons.Left||Deployment==null)return;
            Point[] labels=FleetLabels();
            int nearest=-1;double distance=101;
            for(int i=0;i<Deployment.Fleets.Count;i++){
                Point marker=FleetMarker(i);double dx=e.X-marker.X,dy=e.Y-marker.Y,d=dx*dx+dy*dy;
                dx=e.X-labels[i].X;dy=e.Y-labels[i].Y;d=Math.Min(d,dx*dx+dy*dy);
                if(d<distance){nearest=i;distance=d;}
            }
            if(nearest>=0&&FleetSelected!=null)FleetSelected(nearest);
        }
        public MapView() {
            DoubleBuffered=true;
            BackColor=Color.FromArgb(12,23,35);
            ResizeRedraw=true;
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if(Scene==null)return;
            var g=e.Graphics;
            var bounds=MapBounds;if(bounds.Width<1)return;
            float step=bounds.Width/64,left=bounds.X,top=bounds.Y;
            for(int y=0; y<64; y++)for(int x=0; x<64; x++) {
                Cell c=Scene.At(x,y);
                Color color=c.Kind==1?Color.FromArgb(24,55,77):c.Kind==2?Color.FromArgb(123,145,91):c.Kind==4?Color.FromArgb(Math.Min(255,40+25*c.Value),84,106):c.Kind==3?Color.FromArgb(236,192,98):c.Kind==7?Color.Turquoise:Color.IndianRed;
                using(var brush=new SolidBrush(color))g.FillRectangle(brush,left+x*step,top+y*step,step+.1f,step+.1f);
            }
            if(Deployment!=null)using(var font=new Font("Segoe UI",8,FontStyle.Bold)) {
                Color[] colors={Color.LightCyan,Color.Orange,Color.LightGreen,Color.Plum};
                Point[] labels=FleetLabels();
                for(int i=0;i<Deployment.Fleets.Count;i++) {
                    var fleet=Deployment.Fleets[i];Point marker=FleetMarker(i),center=labels[i];
                    if(marker!=center)using(var pen=new Pen(colors[fleet.Owner]))g.DrawLine(pen,marker,center);
                    using(var fill=new SolidBrush(colors[fleet.Owner]))g.FillEllipse(fill,marker.X-2,marker.Y-2,4,4);
                    int radius=i<9?6:8;
                    using(var fill=new SolidBrush(colors[fleet.Owner]))g.FillEllipse(fill,center.X-radius,center.Y-radius,radius*2,radius*2);
                    if(i==SelectedFleet)using(var pen=new Pen(Color.Gold,2))g.DrawEllipse(pen,center.X-radius-3,center.Y-radius-3,radius*2+6,radius*2+6);
                    string number=(i+1).ToString();SizeF text=g.MeasureString(number,font);
                    g.DrawString(number,font,Brushes.Black,center.X-text.Width/2,center.Y-7);
                }
            }
            int embedded=0,records=0;
            foreach(Cell c in Scene.Cells)if(c.Payload!=null)embedded++;
            foreach(var player in Scene.Groups)foreach(var group in player)records+=group.Count;
            using(var brush=new SolidBrush(Color.LightSteelBlue))using(var font=new Font("Segoe UI",9)) {
                g.DrawString(Scene.Name+" · 64 x 64 · "+Scene.Players+" players · "+embedded+" payload cells / "+records+" grouped records",font,brush,16,Height-72);
                string detail=Deployment==null?"Schematic terrain and fleet records. Missions remain unavailable.":
                    "Seed "+Deployment.Seed+" · "+Deployment.RandomCalls+" RNG calls · click a numbered fleet to inspect";
                g.DrawString(PreviewProblem??detail,font,brush,new RectangleF(16,Height-50,Math.Max(0,Width-32),44));
                if(Deployment!=null)g.DrawString(Deployment.IncludesMapObjects?
                    "First Practice startup data · 7 map objects · other terrain/UI/missions excluded":
                    "Fresh grouped deployment · earlier startup and embedded entities excluded",font,brush,16,Height-25);
            }
        }
    }
}
