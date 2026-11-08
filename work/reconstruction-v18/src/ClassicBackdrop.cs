using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace FleetLab {
    // Modern presentation mask requested by the user. Original art/media stay intact.
    // Native grid geometry is reused; HUD shape comes from separate original sprites.
    public static class ClassicBackdrop {
        public const byte SurfaceAlpha=179; // Approved 70% opacity, nearest 8-bit alpha.
        static readonly byte[] Mask=BuildMask();
        sealed class Line {
            readonly float x,y,dx,dy;readonly double length;
            public Line(PointF a,PointF b){x=a.X;y=a.Y;dx=b.X-a.X;dy=b.Y-a.Y;length=Math.Sqrt(dx*dx+dy*dy);}
            public bool Near(float px,float py){return Math.Abs(dx*(py-y)-dy*(px-x))<=1.5*length;}
        }
        static bool Inside(PointF[] polygon,float x,float y,float margin){
            for(int i=0;i<4;i++){PointF a=polygon[i],b=polygon[(i+1)%4];float dx=b.X-a.X,dy=b.Y-a.Y;if(dx*(y-a.Y)-dy*(x-a.X)<-margin*Math.Sqrt(dx*dx+dy*dy))return false;}return true;
        }
        static Line[] Grid(bool enemy){
            var lines=new Line[22];for(int i=0;i<=10;i++){lines[i]=new Line(ClassicGeometry.Project(enemy,i,0),ClassicGeometry.Project(enemy,i,10));lines[11+i]=new Line(ClassicGeometry.Project(enemy,0,i),ClassicGeometry.Project(enemy,10,i));}return lines;
        }
        static byte[] BuildMask(){
            byte[] alpha=new byte[640*512];
            {
                PointF[] own={ClassicGeometry.Project(false,0,0),ClassicGeometry.Project(false,10,0),ClassicGeometry.Project(false,10,10),ClassicGeometry.Project(false,0,10)};
                PointF[] enemy={ClassicGeometry.Project(true,0,0),ClassicGeometry.Project(true,10,0),ClassicGeometry.Project(true,10,10),ClassicGeometry.Project(true,0,10)};
                Line[] ownLines=Grid(false),enemyLines=Grid(true);
                for(int y=0;y<512;y++)for(int x=0;x<640;x++){
                    float px=x+.5f,py=y+.5f;bool inOwn=Inside(own,px,py,2),inEnemy=Inside(enemy,px,py,2);byte value=0;
                    if(inOwn||inEnemy){value=SurfaceAlpha;PointF[] polygon=inOwn?own:enemy;if(!Inside(polygon,px,py,0))value=255;else foreach(Line line in inOwn?ownLines:enemyLines)if(line.Near(px,py)){value=255;break;}}

                    alpha[y*640+x]=value;
                }
            }
            return alpha;
        }
        public static Bitmap Foreground(IndexedImage source){
            if(source==null||source.Width!=640||source.Height!=512||source.Pixels==null||source.Pixels.Length!=640*512)throw new InvalidDataException("Classic backdrop requires the 640x512 board artwork");
            Bitmap image=new IndexedImage{Width=640,Height=512,Pixels=source.Pixels,Palette=source.Palette}.Bitmap();
            try{
                var data=image.LockBits(new Rectangle(0,0,640,512),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
                try{byte[] row=new byte[640*4];for(int y=0;y<512;y++){IntPtr at=IntPtr.Add(data.Scan0,y*data.Stride);Marshal.Copy(at,row,0,row.Length);for(int x=0;x<640;x++)row[x*4+3]=Mask[y*640+x];Marshal.Copy(row,0,at,row.Length);}}finally{image.UnlockBits(data);}
                return image;
            }catch{image.Dispose();throw;}
        }
        public static RectangleF Cover(Size image,RectangleF area){
            if(image.Width<=0||image.Height<=0||area.Width<=0||area.Height<=0)throw new ArgumentOutOfRangeException("area");
            float scale=Math.Max(area.Width/image.Width,area.Height/image.Height);
            return new RectangleF(area.X+(area.Width-image.Width*scale)/2,area.Y+(area.Height-image.Height*scale)/2,image.Width*scale,image.Height*scale);
        }
        public static void Draw(Graphics graphics,Bitmap image,Rectangle area){
            var state=graphics.Save();try{graphics.SetClip(area);graphics.InterpolationMode=InterpolationMode.NearestNeighbor;graphics.PixelOffsetMode=PixelOffsetMode.Half;graphics.DrawImage(image,Cover(image.Size,area));}finally{graphics.Restore(state);}
        }
    }
}

