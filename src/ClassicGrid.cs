using System;
using System.Drawing;
using System.IO;
namespace FleetLab {
    // Bs.exe B9A8...A569: 40C350 grid, 421840 border, 420870/420950
    // lines and 433991/4339C2 three-by-two pixel pattern. Canonical Classic
    // camera only; the 640x512 PCX's final 32 rows are storage, not viewport.
    public static class ClassicGrid {
        public static IndexedImage Compose(IndexedImage source) {
            if(source==null||source.Width!=640||(source.Height!=480&&source.Height!=512)||
                source.Pixels==null||source.Pixels.Length!=640*source.Height||
                source.Visible!=null&&source.Visible.Length!=source.Pixels.Length)
                throw new InvalidDataException("Classic grid requires a 640x480 or 640x512 indexed background");
            var result=new IndexedImage {Width=source.Width,Height=source.Height,
                OriginX=source.OriginX,OriginY=source.OriginY,TrailingBytes=source.TrailingBytes,
                Pixels=(byte[])source.Pixels.Clone(),
                Visible=source.Visible==null?null:(bool[])source.Visible.Clone(),
                Palette=source.Palette==null?null:(Color[])source.Palette.Clone(),
                UnresolvedTail=source.UnresolvedTail==null?null:(byte[])source.UnresolvedTail.Clone()};
            Grid(result,0);Border(result,0,13);Border(result,11,243);
            // The original draws the other fleet's grid after its border.
            Grid(result,11);Border(result,0,13);
            return result;
        }
        static Point Project(int x,int y) {
            return new Point(320+(x+y-15)*117/6,240+(y-x+5)*78/6);
        }
        static void Pixel(IndexedImage image,int x,int y,byte color) {
            if(x<0||x>=640||y<0||y>=480)throw new InvalidDataException("Classic grid exceeded its canonical viewport");
            int at=y*640+x;image.Pixels[at]=color;
            if(image.Visible!=null)image.Visible[at]=true;
        }
        static void Line(IndexedImage image,Point start,int direction,byte color) {
            for(int i=0;i<(117*10/6+2)/3;i++) {
                int x=start.X+i*3,y=start.Y+i*direction*2;
                Pixel(image,x,y,color);Pixel(image,x+1,y+direction,color);Pixel(image,x+2,y+direction,color);
            }
        }
        internal static void Grid(IndexedImage image,int origin) {
            for(int i=0;i<=10;i++) {
                Line(image,Project(origin+i,0),1,234);
                Line(image,Project(origin,i),-1,234);
            }
        }
        internal static void Border(IndexedImage image,int origin,byte color) {
            foreach(int edge in new[]{0,10}) {
                Point p=Project(origin+edge,0);
                foreach(int offset in new[]{-2,0,-1,1})Line(image,new Point(p.X+offset,p.Y),1,color);
                p=Project(origin,edge);
                foreach(int offset in new[]{-2,0,1,-1})Line(image,new Point(p.X,p.Y+offset),-1,color);
            }
        }
    }
}
