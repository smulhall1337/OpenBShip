using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Media;
namespace FleetLab {
    public sealed class BoardView : Control {
        public Game Game;
        public Bitmap Art;
        public Bitmap[] Roster;
        public event Action<string> ActionDone;
        public Point KeyboardCell=new Point(0,0);
        Point? hover;
        // Inferred from the colored border pixels in E_CLASS.PCX, not legacy hit-test code.
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
            }
        }
        public BoardView() {
            DoubleBuffered=true;
            BackColor=Color.FromArgb(8,15,24);
            TabStop=true;
            ResizeRedraw=true;
        }
        RectangleF BoundsForArt() {
            float s=Math.Min(ClientSize.Width/640f,ClientSize.Height/512f);
            return new RectangleF((ClientSize.Width-640*s)/2,(ClientSize.Height-512*s)/2,640*s,512*s);
        }
        public static PointF Project(bool enemy,float u,float v) {
            var c=enemy?Opp:Own;
            return new PointF((1-u)*(1-v)*c[0].X+u*(1-v)*c[1].X+u*v*c[2].X+(1-u)*v*c[3].X,(1-u)*(1-v)*c[0].Y+u*(1-v)*c[1].Y+u*v*c[2].Y+(1-u)*v*c[3].Y);
        }
        public static PointF[] Polygon(bool enemy,int x,int y) {
            return new[] {
                Project(enemy,x/10f,y/10f),Project(enemy,(x+1)/10f,y/10f),Project(enemy,(x+1)/10f,(y+1)/10f),Project(enemy,x/10f,(y+1)/10f)
            };
        }
        static bool Contains(PointF[] poly,PointF p) {
            bool inside=false;
            for(int i=0,j=poly.Length-1; i<poly.Length; j=i++)if((poly[i].Y>p.Y)!=(poly[j].Y>p.Y)&&p.X<(poly[j].X-poly[i].X)*(p.Y-poly[i].Y)/(poly[j].Y-poly[i].Y)+poly[i].X)inside=!inside;
            return inside;
        }
        public static Point? HitCell(bool enemy,PointF p) {
            for(int y=0; y<10; y++)for(int x=0; x<10; x++)if(Contains(Polygon(enemy,x,y),p))return new Point(x,y);
            return null;
        }
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            RectangleF r=BoundsForArt();
            if(r.Width<=0)return;
            var at=new PointF((e.X-r.X)*640/r.Width,(e.Y-r.Y)*512/r.Height);
            hover=HitCell(Game.Phase!=Phase.Placement,at);
            if(hover.HasValue)KeyboardCell=hover.Value;
            Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            hover=null;
            Invalidate();
        }
        protected override void OnMouseClick(MouseEventArgs e) {
            base.OnMouseClick(e);
            Focus();
            RectangleF r=BoundsForArt();
            if(e.Button!=MouseButtons.Left||r.Width<=0)return;
            var at=new PointF((e.X-r.X)*640/r.Width,(e.Y-r.Y)*512/r.Height);
            var cell=HitCell(Game.Phase!=Phase.Placement,at);
            if(cell.HasValue) {
                KeyboardCell=cell.Value;
                Act();
            }
        }
        public void Act() {
            if(Game.Paused)return;
            bool placement=Game.Phase==Phase.Placement;
            bool accepted=placement?Game.Place(KeyboardCell):Game.Fire(KeyboardCell);
            if(ActionDone!=null)ActionDone(accepted?(placement?"place":"fire"):"none");
            Invalidate();
        }
        public void MoveCursor(int x,int y) {
            KeyboardCell=new Point(Math.Max(0,Math.Min(9,KeyboardCell.X+x)),Math.Max(0,Math.Min(9,KeyboardCell.Y+y)));
            hover=KeyboardCell;
            Invalidate();
        }
        void CellTint(Graphics g,bool enemy,Point p,Color color) {
            using(var b=new SolidBrush(color))g.FillPolygon(b,Polygon(enemy,p.X,p.Y));
        }
        void DrawBoard(Graphics g,Board board,bool enemy) {
            for(int y=0; y<10; y++)for(int x=0; x<10; x++) {
                var p=new Point(x,y);
                Ship ship=board.ShipAt(p);
                if(ship!=null&&(!enemy||Game.Phase==Phase.Finished))CellTint(g,enemy,p,Color.FromArgb(125,80,170,205));
                if(!board.Shots[x,y])continue;
                var center=Project(enemy,(x+.5f)/10,(y+.5f)/10);
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
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if(Game==null||Art==null)return;
            RectangleF r=BoundsForArt();
            if(r.Width<=0)return;
            var g=e.Graphics;
            var saved=g.Save();
            g.TranslateTransform(r.X,r.Y);
            g.ScaleTransform(r.Width/640,r.Height/512);
            g.InterpolationMode=InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawImage(Art,new Rectangle(0,0,640,512));
            DrawRoster(g,Game.Player,false);
            DrawRoster(g,Game.Enemy,true);
            g.SmoothingMode=SmoothingMode.AntiAlias;
            DrawBoard(g,Game.Player,false);
            DrawBoard(g,Game.Enemy,true);
            if(Game.Phase==Phase.Placement&&Game.NextLength>0) {
                Point p=hover??KeyboardCell;
                bool valid=Game.Player.CanPlace(p,Game.NextLength,Game.PlacementVertical);
                for(int i=0; i<Game.NextLength; i++) {
                    var cell=new Point(p.X+(Game.PlacementVertical?0:i),p.Y+(Game.PlacementVertical?i:0));
                    if(Board.Inside(cell))CellTint(g,false,cell,valid?Color.FromArgb(130,90,220,175):Color.FromArgb(160,240,75,65));
                }
            }
            if(!Game.Paused&&(Game.Phase==Phase.PlayerTurn||Game.Phase==Phase.Placement)) {
                bool enemy=Game.Phase==Phase.PlayerTurn;
                using(var pen=new Pen(Color.FromArgb(250,235,198,113),1.3f))g.DrawPolygon(pen,Polygon(enemy,KeyboardCell.X,KeyboardCell.Y));
            }
            using(var font=new Font("Segoe UI",10,FontStyle.Bold))using(var brush=new SolidBrush(Color.FromArgb(235,225,203))) {
                g.DrawString("YOUR FLEET",font,brush,16,10);
                g.DrawString("OPPONENT",font,brush,287,450);
            }
            using(var font=new Font("Segoe UI",9))using(var brush=new SolidBrush(Color.LightSteelBlue))g.DrawString(Game.Rules.FixedY?"Recovered fleet and status sprites  /  modern AI and board markers":"v0.1 provisional fleet  /  rotation enabled",font,brush,14,488);
            if(Game.Paused||Game.Phase==Phase.Finished) {
                using(var b=new SolidBrush(Color.FromArgb(210,7,18,30)))g.FillRectangle(b,0,175,640,115);
                using(var font=new Font("Segoe UI",24,FontStyle.Bold))using(var b=new SolidBrush(Color.FromArgb(239,203,128)))g.DrawString(Game.Paused?"PAUSED":Game.Enemy.AllSunk?"VICTORY":"FLEET LOST",font,b,190,191);
                using(var font=new Font("Segoe UI",10))using(var b=new SolidBrush(Color.WhiteSmoke))g.DrawString(Game.Paused?"Press P or Resume to continue.":"Use New battle (F2) to play again.",font,b,190,242);
            }
            g.Restore(saved);
        }
    }
    public sealed class MapView : Control {
        public Scenario Scene;
        public MapView() {
            DoubleBuffered=true;
            BackColor=Color.FromArgb(12,23,35);
            ResizeRedraw=true;
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if(Scene==null)return;
            var g=e.Graphics;
            float size=Math.Min(Width-40,Height-88),step=size/64;
            float left=(Width-size)/2,top=16;
            for(int y=0; y<64; y++)for(int x=0; x<64; x++) {
                Cell c=Scene.At(x,y);
                Color color=c.Kind==1?Color.FromArgb(24,55,77):c.Kind==2?Color.FromArgb(123,145,91):c.Kind==4?Color.FromArgb(Math.Min(255,40+25*c.Value),84,106):c.Kind==3?Color.FromArgb(236,192,98):c.Kind==7?Color.Turquoise:Color.IndianRed;
                using(var brush=new SolidBrush(color))g.FillRectangle(brush,left+x*step,top+y*step,step+.1f,step+.1f);
            }
            int embedded=0,records=0;
            foreach(Cell c in Scene.Cells)if(c.Payload!=null)embedded++;
            foreach(var player in Scene.Groups)foreach(var group in player)records+=group.Count;
            using(var brush=new SolidBrush(Color.LightSteelBlue))using(var font=new Font("Segoe UI",10)) {
                g.DrawString(Scene.Name+"  |  64 x 64  |  "+Scene.Players+" player records  |  "+embedded+" embedded / "+records+" grouped records",font,brush,16,Height-62);
                g.DrawString("Decoded cell kinds; colors are schematic. Entity fields and mission behavior remain unresolved.",font,brush,16,Height-36);
            }
        }
    }
}
