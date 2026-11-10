using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
namespace FleetLab {
    // Own-fleet overview from 431B00/431D00: camera20/50, scale27/18.
    // Deployment has only unshot own cells; the original40C4B0 overlay skips them.
    public sealed class FleetOverview : IDisposable {
        ClassicShips ships;
        readonly Bitmap[] inactive=new Bitmap[4];
        Bitmap grid,border;Point gridAt,borderAt;
        bool disposed;
        public FleetOverview(string graphics,Color[] palette,List<IndexedImage> frames) {
            try {
                if(palette==null||palette.Length!=256||frames==null||frames.Count<14)throw new InvalidDataException("Missing Fleet overview resources");
                ships=new ClassicShips(graphics,palette,18);
                for(int i=0;i<4;i++)inactive[i]=ClassicShips.Scale(frames[10+i],100).Bitmap();
                grid=Layer(palette,false,out gridAt);border=Layer(palette,true,out borderAt);
            }
            catch {Dispose();throw;}
        }
        public static Point Project(int x,int y) {return new Point(320+(x+y-70)*27/6,240+(y-x-30)*18/6);}
        public static Point Anchor(Point start,int length) {
            var p=Project(start.X,start.Y+length/2);int dx=27/12,dy=18/-12;
            if((length&1)!=0){dx*=2;dy+=18/12;}p.Offset(dx,dy);return p;
        }
        // 420870/420950 and433991/4339C2 use a repeated three-by-two pattern.
        // These fixed overview lines lie wholly inside the canonical viewport.
        static void Line(IndexedImage image,Point start,int direction,byte color) {
            int groups=(27*10/6+2)/3;
            for(int i=0;i<groups;i++) {
                int x=start.X+i*3,y=start.Y+i*direction*2;
                Pixel(image,x,y,color);Pixel(image,x+1,y+direction,color);Pixel(image,x+2,y+direction,color);
            }
        }
        static void Pixel(IndexedImage image,int x,int y,byte color) {
            if(x<0||x>=640||y<0||y>=480)throw new InvalidDataException("Overview line outside its supported viewport");
            int at=y*640+x;image.Pixels[at]=color;if(image.Visible!=null)image.Visible[at]=true;
        }
        public static void Raster(IndexedImage image,bool selectedBorder) {
            if(image==null||image.Width!=640||image.Height!=480||image.Pixels==null||image.Pixels.Length!=640*480||image.Visible!=null&&image.Visible.Length!=image.Pixels.Length)throw new InvalidDataException("Fleet overview requires a640x480indexed surface");
            if(!selectedBorder) {
                for(int i=0;i<=10;i++){Line(image,Project(i,0),1,234);Line(image,Project(0,i),-1,234);}
            }
            else {
                foreach(int edge in new[]{0,10}) {
                    Point p=Project(edge,0);Line(image,p,1,13);p.Offset(-1,0);Line(image,p,1,13);
                    p=Project(0,edge);Line(image,p,-1,13);p.Offset(0,1);Line(image,p,-1,13);
                }
            }
        }
        static Bitmap Layer(Color[] palette,bool selected,out Point at) {
            var full=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Visible=new bool[640*480],Palette=palette};Raster(full,selected);
            int left=640,top=480,right=-1,bottom=-1;
            for(int y=0;y<480;y++)for(int x=0;x<640;x++)if(full.Visible[y*640+x]){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}
            if(right<left)throw new InvalidDataException("Empty Fleet overview layer");at=new Point(left,top);
            var image=new IndexedImage{Width=right-left+1,Height=bottom-top+1,Palette=palette};image.Pixels=new byte[image.Width*image.Height];image.Visible=new bool[image.Pixels.Length];
            for(int y=0;y<image.Height;y++){Array.Copy(full.Pixels,(top+y)*640+left,image.Pixels,y*image.Width,image.Width);Array.Copy(full.Visible,(top+y)*640+left,image.Visible,y*image.Width,image.Width);}return image.Bitmap();
        }
        public void Draw(Graphics g,Board board) {
            if(disposed||g==null||board==null)return;
            g.DrawImageUnscaled(grid,gridAt);
            foreach(var ship in board.Ships){var image=ships.Image(ship.SourceType);g.DrawImageUnscaled(image,ClassicShips.Bounds(Anchor(ship.Cells[0],ship.Cells.Count),image.Size).Location);}
            g.DrawImageUnscaled(border,borderAt);
            for(int i=0;i<4;i++)g.DrawImageUnscaled(inactive[i],17,183+i*60);
        }
        public void Dispose() {
            if(disposed)return;disposed=true;if(ships!=null)ships.Dispose();if(grid!=null)grid.Dispose();if(border!=null)border.Dispose();foreach(var image in inactive)if(image!=null)image.Dispose();
        }
    }
}
