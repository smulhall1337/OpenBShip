using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
namespace FleetLab {
    public sealed class IndexedImage {
        public int Width,Height,OriginX,OriginY,TrailingBytes;
        public byte[] Pixels,UnresolvedTail;
        public bool[] Visible;
        public Color[] Palette;
        public Bitmap Bitmap() {
            Bitmap bitmap=new Bitmap(Width,Height,PixelFormat.Format32bppArgb);
            var data=bitmap.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try {
                byte[] row=new byte[Width*4];
                for(int y=0; y<Height; y++) {
                    Array.Clear(row,0,row.Length);
                    for(int x=0; x<Width; x++) {
                        int i=y*Width+x;
                        Color c=Palette[Pixels[i]];
                        row[x*4]=c.B;
                        row[x*4+1]=c.G;
                        row[x*4+2]=c.R;
                        row[x*4+3]=(Visible==null||Visible[i])?(byte)255:(byte)0;
                    }
                    Marshal.Copy(row,0,IntPtr.Add(data.Scan0,y*data.Stride),row.Length);
                }
            }
            finally {
                bitmap.UnlockBits(data);
            }
            return bitmap;
        }
    }
    public static class Assets {
        static int U16(byte[] b,int p) {
            return b[p]|(b[p+1]<<8);
        }
        static void Require(bool condition,string message) {
            if(!condition)throw new InvalidDataException(message);
        }
        public static Color[] Palette(string path) {
            byte[] b=File.ReadAllBytes(path);
            Require(b.Length==768,"Palette must contain exactly 256 RGB triples");
            return Colors(b,0);
        }
        static Color[] Colors(byte[] b,int p) {
            var colors=new Color[256];
            for(int i=0; i<256; i++)colors[i]=Color.FromArgb(b[p+i*3],b[p+i*3+1],b[p+i*3+2]);
            return colors;
        }
        public static IndexedImage Pcx(string path) {
            return Pcx(File.ReadAllBytes(path));
        }
        public static IndexedImage Pcx(byte[] b) {
            Require(b.Length>=897,"PCX is truncated");
            Require(b[0]==10&&b[2]==1,"Unsupported PCX signature/encoding");
            Require(b[3]==8&&b[65]==1,"Only 8-bit single-plane PCX is implemented");
            int w=U16(b,8)-U16(b,4)+1,h=U16(b,10)-U16(b,6)+1,stride=U16(b,66);
            Require(w>0&&h>0&&w<=4096&&h<=4096&&stride>=w&&stride<=8192,"Invalid PCX dimensions/stride");
            int end=b.Length-769;
            Require(b[end]==12,"PCX 256-color palette marker missing");
            byte[] pixels=new byte[w*h];
            int p=128,total=checked(stride*h),decoded=0;
            while(decoded<total) {
                Require(p<end,"Truncated PCX RLE stream");
                int value=b[p++],count=1;
                if((value&192)==192) {
                    count=value&63;
                    Require(count!=0&&p<end,"Invalid PCX run");
                    value=b[p++];
                }
                Require(count<=total-decoded,"PCX run exceeds image bounds");
                for(int i=0; i<count; i++) {
                    int x=decoded%stride,y=decoded/stride;
                    if(x<w)pixels[y*w+x]=(byte)value;
                    decoded++;
                }
            }
            byte[] tail=new byte[end-p];
            Array.Copy(b,p,tail,0,tail.Length);
            return new IndexedImage {
                Width=w,Height=h,Pixels=pixels,Palette=Colors(b,end+1),TrailingBytes=end-p,UnresolvedTail=tail
            };
        }
        public static List<IndexedImage> Sph(string path,Color[] palette) {
            return Sph(File.ReadAllBytes(path),palette);
        }
        public static List<IndexedImage> Sph(byte[] b,Color[] palette) {
            Require(b.Length>=6,"SPH header truncated");
            int count=U16(b,0),table=2+count*4;
            Require(count>0&&count<=4096&&table<=b.Length,"SPH frame table invalid");
            Require(palette!=null&&palette.Length==256,"SPH palette missing");
            uint[] offsets=new uint[count];
            for(int i=0; i<count; i++) {
                offsets[i]=BitConverter.ToUInt32(b,2+i*4);
                Require(offsets[i]==uint.MaxValue||(offsets[i]>=table&&offsets[i]<=b.Length-8),"SPH frame offset invalid");
            }
            var frames=new List<IndexedImage>();
            for(int i=0; i<count; i++) {
                if(offsets[i]==uint.MaxValue) {
                    frames.Add(null);
                    continue;
                }
                int p=(int)offsets[i],end=b.Length;
                for(int j=0; j<count; j++)if(offsets[j]>offsets[i]&&offsets[j]<end)end=(int)offsets[j];
                int ox=(short)U16(b,p),oy=(short)U16(b,p+2),w=U16(b,p+4),h=U16(b,p+6);
                p+=8;
                Require(w>0&&h>0&&w<=4096&&h<=4096&&w*h<=16777216,"SPH dimensions invalid");
                byte[] pixels=new byte[w*h];
                bool[] visible=new bool[w*h];
                for(int y=0; y<h; y++) {
                    Require(p+2<=end,"SPH scanline header truncated");
                    int skip=b[p++],run=b[p++];
                    Require(skip+run<=w&&p+run<=end,"SPH scanline exceeds frame bounds");
                    for(int x=0; x<run; x++) {
                        pixels[y*w+skip+x]=b[p++];
                        visible[y*w+skip+x]=true;
                    }
                }
                byte[] tail=new byte[end-p];
                Array.Copy(b,p,tail,0,tail.Length);
                frames.Add(new IndexedImage {
                    Width=w,Height=h,OriginX=ox,OriginY=oy,Pixels=pixels,Visible=visible,Palette=palette,TrailingBytes=end-p,UnresolvedTail=tail
                });
            }
            return frames;
        }
    }
    public sealed class Cell {
        public byte Kind,Value;
        public byte[] Payload;
    }
    public sealed class Scenario {
        public const int Size=64;
        public string Name;
        public Cell[] Cells=new Cell[4096];
        public int TerrainBytes,Players;
        public List<byte[]>[][] Groups;
        public Cell At(int x,int y) {
            return Cells[y*64+x];
        }
        public static Scenario Load(string path) {
            return Load(File.ReadAllBytes(path),Path.GetFileName(path));
        }
        public static Scenario Load(byte[] b,string name) {
            if(b.Length>4*1024*1024)throw new InvalidDataException("Scenario exceeds supported size");
            int p=0;
            var result=new Scenario {
                Name=name
            };
            // Confirmed from this build's loader at VA 0x41B230: outer x, inner y.
            for(int x=0; x<64; x++)for(int y=0; y<64; y++) {
                Need(b,p,1);
                var c=new Cell {
                    Kind=b[p++]
                };
                if(c.Kind==2||c.Kind==4||c.Kind==7) {
                    Need(b,p,1);
                    c.Value=b[p++];
                }
                else if(c.Kind==3||(c.Kind>=31&&c.Kind<=41)) {
                    Need(b,p,401);
                    c.Value=b[p++];
                    c.Payload=new byte[400];
                    Array.Copy(b,p,c.Payload,0,400);
                    p+=400;
                }
                result.Cells[y*64+x]=c;
            }
            result.TerrainBytes=p;
            Need(b,p,1);
            result.Players=b[p++];
            if(result.Players<1||result.Players>4)throw new InvalidDataException("Unsupported scenario player count at "+(p-1));
            result.Groups=new List<byte[]>[result.Players][];
            for(int player=0; player<result.Players; player++) {
                result.Groups[player]=new List<byte[]>[2];
                for(int group=0; group<2; group++) {
                    Need(b,p,4);
                    int count=BitConverter.ToInt32(b,p);
                    p+=4;
                    if(count<0||count>64)throw new InvalidDataException("Invalid scenario group count");
                    Need(b,p,count*400);
                    var records=new List<byte[]>();
                    for(int i=0; i<count; i++) {
                        byte[] raw=new byte[400];
                        Array.Copy(b,p,raw,0,400);
                        p+=400;
                        records.Add(raw);
                    }
                    result.Groups[player][group]=records;
                }
            }
            if(p!=b.Length)throw new InvalidDataException("Unsupported scenario tail: "+(b.Length-p)+" bytes remain");
            return result;
        }
        static void Need(byte[] b,int p,int n) {
            if(p<0||n<0||p>b.Length-n)throw new InvalidDataException("Scenario truncated at byte "+p);
        }
    }
}
