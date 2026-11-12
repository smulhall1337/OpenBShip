using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab {
    // Accepted modern 40 ms step (25 Hz); original outer-loop cadence is separate evidence.
    // Visual time is separate from game turns; pause/still/focus loss freeze it.
    public sealed class EffectClock {
        public long Tick {get;private set;}
        long remainder;
        public void Reset(){Tick=0;remainder=0;}
        public bool Advance(long milliseconds,bool running) {
            if(milliseconds<0)throw new ArgumentOutOfRangeException("milliseconds");
            if(!running)return false;
            remainder+=Math.Min(milliseconds,200); // At most five visual catch-up steps.
            long steps=remainder/40;remainder%=40;Tick+=steps;
            return steps!=0;
        }
    }
    public sealed class ClassicDamage : IDisposable {
        readonly List<Bitmap> frames=new List<Bitmap>();
        public ClassicDamage(string graphics,Color[] palette) {
            try {
                var source=Assets.Sph(Path.Combine(graphics,"SMOKE.SPH"),palette);
                if(source.Count!=34||source[33]!=null)throw new InvalidDataException("Unsupported Classic damage frame table");
                for(int i=0;i<33;i++) {
                    if(source[i]==null)throw new InvalidDataException("Missing Classic damage frame");
                    frames.Add(ClassicShips.Scale(source[i],ClassicGeometry.ShipPercent).Bitmap());
                }
            }catch{Dispose();throw;}
        }
        public static int Frame(long tick,int segment) {
            if(tick<0||segment<0||segment>9)throw new ArgumentOutOfRangeException();
            return (int)((tick%33+segment*8)%33);
        }
        public static Point Anchor(Point cell) {
            PointF point=ClassicGeometry.Project(false,cell.X,cell.Y);
            return new Point((int)point.X+ClassicGeometry.ScaleX/6,(int)point.Y);
        }
        public static Rectangle Bounds(Point anchor,Size size) {
            // Native draw mode 2: horizontally centered and bottom aligned.
            return new Rectangle(anchor.X-size.Width/2,anchor.Y-size.Height,size.Width,size.Height);
        }
        public void Draw(Graphics g,Board board,Ship ship,long tick) {
            if(ship.Sunk)return; // Native fleet slot disappears when sunk.
            for(int segment=0;segment<ship.Cells.Count;segment++) {
                Point cell=ship.Cells[segment];
                if(!board.Shots[cell.X,cell.Y])continue;
                Bitmap image=frames[Frame(tick,segment)];
                g.DrawImage(image,Bounds(Anchor(cell),image.Size));
            }
        }
        public void Dispose(){foreach(var image in frames)image.Dispose();frames.Clear();}
    }
}
