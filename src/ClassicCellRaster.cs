using System;
using System.Drawing;
using System.IO;
namespace FleetLab {
    // Supported Bs.exe: 4211A0 cell fill, 420870 clipping, 433991 pixel runs.
    // Fixed Classic camera (10,5), scales 117/78, viewport 640x480. Integer
    // division deliberately truncates toward zero, including clipped starts.
    public static class ClassicCellRaster {
        static void Surface(byte[] pixels) {
            if(pixels==null||pixels.Length!=640*480)throw new InvalidDataException("Classic cell raster requires a 640x480 indexed surface");
        }
        public static void Fill(byte[] pixels,int x,int y,int color,int elevation) {
            Surface(pixels);
            if(x< -40||x>40||y< -40||y>40||color<0||color>255||elevation< -1||elevation>1)
                throw new InvalidDataException("Unsupported Classic cell raster coordinates or color");
            int px=320+(x+y-15)*117/6,py=240+(y-x+5)*78/6-elevation*117;
            for(int i=0;i<117/6;i++)Down(pixels,px+i,py-2*i/3,117/6,(byte)color);
        }
        static void Down(byte[] pixels,int x,int y,int length,byte color) {
            if(x>=640||y>=480)return;
            if(x<0){length+=x;y+=(x*2)/-3;x=0;}
            if(y<0){int adjustment=y*3/2;length+=adjustment;x-=adjustment;y=0;}
            if(x+length+3>=640)length=640-x-3;
            if(y+(length*2+6)/3>480)length+=((-3-length)*2/3-y+480)*3/2;
            if(length<=0)return;
            for(int i=0;i<(length+2)/3;i++) {
                int at=(y+i*2)*640+x+i*3;
                pixels[at]=color;pixels[at+641]=color;pixels[at+642]=color;
            }
        }
        // 40C4B0 reads an unrotated x*10+y map and rotates drawing coordinates
        // through 42D8D0. Visibility is the original fleet byte at +467; its
        // connection to front-end settings has not been established.
        public static void Overlay(byte[] pixels,int[,] cells,Point origin,int rotation,
            int owner,int activeOwner,int visibility,Point[] aliveCenters) {
            Surface(pixels);
            if(cells==null||cells.GetLength(0)!=10||cells.GetLength(1)!=10||cells.GetLowerBound(0)!=0||cells.GetLowerBound(1)!=0||
                origin.Y!=0||(origin.X!=0&&origin.X!=11)||rotation<0||rotation>3||
                owner<0||owner>1||activeOwner<0||activeOwner>1||visibility<0||visibility>255||
                aliveCenters==null||aliveCenters.Length>6)
                throw new InvalidDataException("Unsupported Classic cell map or fleet context");
            foreach(Point center in aliveCenters)if(!Board.Inside(center))throw new InvalidDataException("Classic ship center outside map");
            for(int x=0;x<10;x++)for(int y=0;y<10;y++) {
                int color=-1;
                switch(cells[x,y]) {
                    case 1:color=10;break;
                    case 2:
                        if(owner!=activeOwner&&(visibility==3||visibility==2&&Array.IndexOf(aliveCenters,new Point(x,y))>=0))color=12;
                        break;
                    case 5:color=11;break;
                    case 6:color=14;break;
                    case 7:color=241;break;
                    case 8:color=242;break;
                }
                if(color<0)continue;
                Point p=rotation==0?new Point(x,y):rotation==1?new Point(9-y,x):rotation==2?new Point(9-x,9-y):new Point(y,9-x);
                Fill(pixels,origin.X+p.X,p.Y,color,0);
            }
        }
    }
}
