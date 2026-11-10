using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace FleetLab {
    public sealed class ClassicVisualSpec {
        public readonly int Type,SpriteBank,LastCounter,Length;
        public readonly string Resource;
        internal ClassicVisualSpec(int type,int bank,int last,int length,string resource) {
            Type=type;SpriteBank=bank;LastCounter=last;Length=length;Resource=resource;
        }
    }

    // Bs.exe B9A8...A569: 29 little-endian records at 0x44E040, 20 bytes each.
    // Only enabled flag, sprite bank and counter limit are interpreted here.
    // Keep the complete records, including fields outside this Classic subset.
    public sealed class ClassicVisualCatalog {
        readonly byte[] records;
        readonly Dictionary<int,ClassicVisualSpec> specs=new Dictionary<int,ClassicVisualSpec>();
        public byte[] OriginalRecords {get{return (byte[])records.Clone();}}
        static int Word(byte[] b,int p){return unchecked((short)(b[p]|b[p+1]<<8));}
        static int DWord(byte[] b,int p){return b[p]|b[p+1]<<8|b[p+2]<<16|b[p+3]<<24;}
        public ClassicVisualCatalog(byte[] source) {
            if(source==null||source.Length!=580)throw new InvalidDataException("Expected 29 Classic visual records.");
            records=(byte[])source.Clone();
            Add(7,43,60,5,"AUL.SPH");Add(1,47,57,5,"BUL.SPH");
            Add(2,59,43,3,"CUL.SPH");Add(12,75,37,4,"SBIGUL.SPH");
            Add(4,71,42,2,"FRIGUL.SPH");Add(27,37,32,0,"RINGS.SPH");
        }
        void Add(int type,int bank,int last,int length,string resource) {
            int at=type*20;
            if(Word(records,at)!=0||DWord(records,at+8)!=bank||Word(records,at+12)!=last)
                throw new InvalidDataException("Unsupported Classic visual record "+type+".");
            specs.Add(type,new ClassicVisualSpec(type,bank,last,length,resource));
        }
        public ClassicVisualSpec Get(int type) {
            ClassicVisualSpec spec;
            if(!specs.TryGetValue(type,out spec))throw new InvalidDataException("Unsupported Classic visual type "+type+".");
            return spec;
        }
    }

    public sealed class ClassicVisualEntry {
        public readonly ClassicVisualSpec Spec;
        public readonly bool Enemy;
        public readonly Point Start;
        public readonly int Slot;
        public int Counter {get;internal set;}
        internal ClassicVisualEntry(ClassicVisualSpec spec,bool enemy,Point start,int slot,int counter) {
            Spec=spec;Enemy=enemy;Start=start;Slot=slot;Counter=counter;
        }
    }

    // Original 0x420550 chooses the first free slot; 0x40E0D0 retires only when
    // counter > limit. The limit itself is an empty SPH terminal slot.
    public sealed class ClassicVisualTimeline {
        public const int Capacity=40;
        readonly ClassicVisualEntry[] entries=new ClassicVisualEntry[Capacity];
        public int Count {get{int count=0;foreach(var entry in entries)if(entry!=null)count++;return count;}}
        public IEnumerable<ClassicVisualEntry> Entries {get{foreach(var entry in entries)if(entry!=null)yield return entry;}}
        public ClassicVisualEntry Spawn(ClassicVisualSpec spec,bool enemy,Point start,int firstCounter) {
            if(spec==null)throw new ArgumentNullException("spec");
            if(firstCounter<0||firstCounter>spec.LastCounter)throw new ArgumentOutOfRangeException("firstCounter");
            for(int slot=0;slot<Capacity;slot++)if(entries[slot]==null) {
                var entry=new ClassicVisualEntry(spec,enemy,start,slot,firstCounter);
                entries[slot]=entry;return entry;
            }
            return null;
        }
        public bool Advance(long ticks) {
            if(ticks<0)throw new ArgumentOutOfRangeException("ticks");
            bool changed=false;
            if(ticks==0)return false;
            for(int slot=0;slot<Capacity;slot++) {
                var entry=entries[slot];if(entry==null)continue;
                if(ticks>entry.Spec.LastCounter-entry.Counter)entries[slot]=null;
                else entry.Counter+=(int)ticks;
                changed=true;
            }
            return changed;
        }
        public void Clear(){Array.Clear(entries,0,entries.Length);}
    }

    public static class ClassicVisualGeometry {
        // Orientation 0 and floor 0 of 0x40E110. Other rotations remain outside
        // the playable Classic board. Native integer divisions truncate.
        public static Point Anchor(ClassicVisualSpec spec,bool enemy,Point start) {
            if(spec==null)throw new ArgumentNullException("spec");
            int x=start.X+(enemy?11:0),y=start.Y+spec.Length/2;
            if(spec.Type==27)y--; // Original non-ship branch; template word14 is 0.
            int px=320+(x+y-15)*117/6,py=240+(y-x+5)*78/6;
            if(spec.Type==27)return new Point(px+117/6,py);
            int dx=2*(117/12),dy=2*(78/-12);
            if((spec.Length&1)!=0){dx+=117/12;dy+=78/12;}
            return new Point(px+dx,py+dy);
        }
        public static bool Impact(Board victim,Shot shot,out int type,out Point start) {
            type=0;start=new Point();
            if(victim==null||shot==null)return false;
            if(!Board.Inside(shot.Cell))throw new InvalidDataException("Visual impact outside Classic board.");
            Ship ship=victim.ShipAt(shot.Cell);
            // 0x42408D returns on a repeated hit against a surviving ship.
            // Water and already-removed sunk ships still use miss entry 27.
            if(shot.Repeated&&ship!=null&&!ship.Sunk)return false;
            if(!shot.Hit||shot.Repeated){type=27;start=shot.Cell;return true;}
            if(!shot.Sunk)return false;
            if(ship==null||!ship.Sunk||ship.Cells.Count==0)throw new InvalidDataException("Sinking visual requires the resolved ship.");
            type=ship.SourceType;start=ship.Cells[0];return true;
        }
    }

    // Six source banks bound to slots by 0x42AC10/0x42AC86 onward. Source SPH
    // frames and their unresolved tails are retained alongside owned bitmaps.
    public sealed class ClassicVisualSprites : IDisposable {
        sealed class Clip : IDisposable {
            public readonly List<IndexedImage> Source;
            public readonly Bitmap[] Images;
            public Clip(List<IndexedImage> source) {Source=source;Images=new Bitmap[source.Count];}
            public void Dispose(){foreach(var image in Images)if(image!=null)image.Dispose();}
        }
        readonly Dictionary<int,Clip> clips=new Dictionary<int,Clip>();
        bool disposed;
        public readonly ClassicVisualCatalog Catalog;
        public ClassicVisualSprites(string graphics,byte[] records,Color[] palette) {
            Catalog=new ClassicVisualCatalog(records);
            try {
                foreach(int type in new[]{7,1,2,12,4,27}) {
                    var spec=Catalog.Get(type);var source=Assets.Sph(Path.Combine(graphics,spec.Resource),palette);
                    if(source.Count!=spec.LastCounter+1||source[source.Count-1]!=null)
                        throw new InvalidDataException("Unsupported finite sprite table "+spec.Resource+".");
                    var clip=new Clip(source);clips.Add(type,clip);
                    for(int frame=0;frame<spec.LastCounter;frame++) {
                        var image=source[frame];
                        if(image==null)throw new InvalidDataException("Missing finite sprite frame.");
                        if(image.Width*79/100>0&&image.Height*79/100>0)clip.Images[frame]=ClassicShips.Scale(image,79).Bitmap();
                    }
                }
            }catch{Dispose();throw;}
        }
        public bool HasFrame(int type,int counter) {
            if(disposed)throw new ObjectDisposedException("ClassicVisualSprites");
            var spec=Catalog.Get(type);
            if(counter<0||counter>spec.LastCounter)throw new ArgumentOutOfRangeException("counter");
            return clips[type].Images[counter]!=null;
        }
        public Rectangle Bounds(int type,int counter,Point anchor) {
            if(!HasFrame(type,counter))return Rectangle.Empty;
            var clip=clips[type];var image=clip.Images[counter];var source=clip.Source[counter];
            return type==27?new Rectangle(anchor,image.Size):new Rectangle(anchor.X+source.OriginX-image.Width/2,anchor.Y+source.OriginY-image.Height/2,image.Width,image.Height);
        }
        public void DrawFrame(Graphics graphics,int type,int counter,Point anchor) {
            Rectangle bounds=Bounds(type,counter,anchor);
            if(!bounds.IsEmpty)graphics.DrawImage(clips[type].Images[counter],bounds);
        }
        public void Draw(Graphics graphics,ClassicVisualTimeline timeline) {
            if(disposed)throw new ObjectDisposedException("ClassicVisualSprites");
            var saved=graphics.Save();
            try {
                graphics.SetClip(new Rectangle(0,0,640,480),CombineMode.Intersect);
                foreach(var entry in timeline.Entries) {
                    DrawFrame(graphics,entry.Spec.Type,entry.Counter,ClassicVisualGeometry.Anchor(entry.Spec,entry.Enemy,entry.Start));
                }
            }finally{graphics.Restore(saved);}
        }
        public void Dispose(){if(disposed)return;disposed=true;foreach(var clip in clips.Values)clip.Dispose();clips.Clear();}
    }
}
