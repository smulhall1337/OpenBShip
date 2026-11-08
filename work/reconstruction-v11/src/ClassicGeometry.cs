using System;
using System.Drawing;
namespace FleetLab {
    // Bs.exe SHA B9A8...A569. Native units: cells; screen units: 640x512 pixels.
    // Classic camera 0x408A30, forward arithmetic 0x40D728, inverse 0x430D70.
    public static class ClassicGeometry {
        public const int ScaleX=117,ScaleY=78,CameraX=10,CameraY=5;
        public const int ShipPercent=100*ScaleX/147; // 79, deliberately integer division.
        public static PointF Project(bool enemy,float x,float y) {
            float worldX=x+(enemy?11:0);
            return new PointF(320+(int)((worldX+y-CameraX-CameraY)*ScaleX/6f),
                240+(int)((y-worldX+CameraX-CameraY)*ScaleY/6f));
        }
        public static Point? Pick(bool enemy,PointF pixel) {
            if(float.IsNaN(pixel.X)||float.IsNaN(pixel.Y)||float.IsInfinity(pixel.X)||float.IsInfinity(pixel.Y))return null;
            if(pixel.X<0||pixel.Y<0||pixel.X>=640||pixel.Y>=512)return null;
            int px=(int)pixel.X,py=(int)pixel.Y;
            int originX=320+(CameraX+CameraY)*ScaleX/-6;
            int originY=240+(CameraX-CameraY)*ScaleY/6;
            // The native inverse stores this intermediate as float32; its other
            // operand remains on the x87 stack. Double approximates that operand.
            // Exact x87 boundary ties are not claimed bit-identical.
            float horizontal=(float)((px-originX)/(double)ScaleX*3);
            double vertical=(py-originY)/(double)ScaleY*3;
            var cell=new Point((int)(horizontal-vertical)-(enemy?11:0),(int)(horizontal+vertical));
            return Board.Inside(cell)?(Point?)cell:null;
        }
        public static Point ShipAnchor(bool enemy,Point start,int length) {
            // 0x40D6CC: center index start.Y + trunc(length/2), orientation 0.
            PointF p=Project(enemy,start.X,start.Y+length/2);
            int dx=ScaleX/12,dy=ScaleY/-12;
            if((length&1)!=0) { dx+=dx; dy+=ScaleY/12; }
            return new Point((int)p.X+dx,(int)p.Y+dy);
        }
    }
}
