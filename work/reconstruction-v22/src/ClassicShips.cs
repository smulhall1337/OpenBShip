using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab {
    // Original orientation-0 frame dispatch at 0x40DD00; scale/centering at 0x407B59.
    public sealed class ClassicShips : IDisposable {
        readonly Dictionary<int,Bitmap> images=new Dictionary<int,Bitmap>();
        public static string Resource(int type) {
            switch(type) {
                case 1:return "BATTSHIP.SPH";
                case 2:return "FRIGATE.SPH";
                case 4:return "NEWSHIPS.SPH";
                case 7:case 12:return "ALLSHIPS.SPH";
                default:throw new InvalidDataException("Unsupported Classic ship type "+type);
            }
        }
        public static int Frame(int type) {
            switch(type) {
                case 1:case 2:case 7:return 0;
                case 4:return 8;
                case 12:return 4;
                default:throw new InvalidDataException("Unsupported Classic ship type "+type);
            }
        }
        public ClassicShips(string graphics,Color[] palette):this(graphics,palette,ClassicGeometry.ShipPercent) {}
        public ClassicShips(string graphics,Color[] palette,int percent) {
            try {
                foreach(var spec in RuleProfile.Recovered.Fleet) {
                    var frames=Assets.Sph(Path.Combine(graphics,Resource(spec.SourceType)),palette);
                    int index=Frame(spec.SourceType);
                    if(index>=frames.Count||frames[index]==null)throw new InvalidDataException("Missing Classic ship frame");
                    images.Add(spec.SourceType,Scale(frames[index],percent).Bitmap());
                }
            }
            catch { Dispose(); throw; }
        }
        public Bitmap Image(int type) {
            Bitmap bitmap;
            if(!images.TryGetValue(type,out bitmap))throw new InvalidDataException("Missing Classic ship image "+type);
            return bitmap;
        }
        public static Rectangle Bounds(Point anchor,Size size) {
            return new Rectangle(anchor.X-size.Width/2,anchor.Y-size.Height/2,size.Width,size.Height);
        }
        // Preserve native percent truncation, per-scanline skip/run scaling and
        // 16.16 sampling. In this scaled path palette index 0 is transparent.
        // This is intentionally separate from the generic, unscaled SPH reader.
        public static IndexedImage Scale(IndexedImage source,int percent) {
            if(source==null||source.Visible==null||percent<1||percent>400)throw new InvalidDataException("Invalid sprite scaling request");
            int w=checked(source.Width*percent/100),h=checked(source.Height*percent/100);
            if(w<1||h<1||w>4096||h>4096)throw new InvalidDataException("Scaled sprite dimensions are unsupported");
            int stepX=(source.Width<<16)/w,stepY=(source.Height<<16)/h;
            var result=new IndexedImage {Width=w,Height=h,Pixels=new byte[w*h],Visible=new bool[w*h],Palette=source.Palette};
            for(int y=0;y<h;y++) {
                int sy=(y*stepY)>>16,start=0;
                while(start<source.Width&&!source.Visible[sy*source.Width+start])start++;
                int run=0;
                while(start+run<source.Width&&source.Visible[sy*source.Width+start+run])run++;
                // The decoded supplied variant has exactly one stored run per row.
                for(int x=start+run;x<source.Width;x++)if(source.Visible[sy*source.Width+x])throw new InvalidDataException("Disjoint SPH scanline run");
                int left=start*percent/100,count=run*percent/100;
                for(int x=0;x<count;x++) {
                    int sx=start+((x*stepX)>>16);
                    if(left+x>=w||sx>=start+run)throw new InvalidDataException("Scaled scanline exceeds source bounds");
                    byte index=source.Pixels[sy*source.Width+sx];
                    result.Pixels[y*w+left+x]=index;
                    result.Visible[y*w+left+x]=index!=0;
                }
            }
            return result;
        }
        public void Dispose() {
            foreach(var bitmap in images.Values)bitmap.Dispose();
            images.Clear();
        }
    }
}
