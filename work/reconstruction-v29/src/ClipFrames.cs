using System;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab {
    // Retain decoded index surfaces; realize only the currently presented bitmap.
    // A borrowed old bitmap remains valid through FrameChanged, then is released.
    internal sealed class ClipFrames : IDisposable {
        PatMovie movie;Color[] colors;Bitmap image;int index=-1;
        readonly List<Bitmap> retired=new List<Bitmap>();
        public ClipFrames(PatMovie decoded,Color[] palette){if(decoded==null||palette==null)throw new ArgumentNullException();movie=decoded;colors=palette;}
        public int Count {get{return movie==null?0:movie.Frames.Count;}}
        public int CachedBitmaps {get{return (image==null?0:1)+retired.Count;}}
        public Bitmap FadedFrame(int frame,int factor){if(movie==null)throw new ObjectDisposedException("ClipFrames");return new IndexedImage{Width=movie.DecodedWidth,Height=movie.DecodedHeight,Pixels=movie.Frames[frame].Pixels,Palette=ClassicPalette.FadeDisplay(colors,factor)}.Bitmap();}
        internal IndexedImage Indexed(int frame){if(movie==null)throw new ObjectDisposedException("ClipFrames");if(frame<0||frame>=Count)throw new ArgumentOutOfRangeException("frame");return new IndexedImage{Width=movie.DecodedWidth,Height=movie.DecodedHeight,Pixels=(byte[])movie.Frames[frame].Pixels.Clone(),Palette=(Color[])colors.Clone()};}
        public Bitmap Frame(int frame){
            if(movie==null)throw new ObjectDisposedException("ClipFrames");
            if(frame<0||frame>=Count)throw new ArgumentOutOfRangeException("frame");
            if(index!=frame){var next=new IndexedImage{Width=movie.DecodedWidth,Height=movie.DecodedHeight,Pixels=movie.Frames[frame].Pixels,Palette=colors}.Bitmap();if(image!=null)retired.Add(image);image=next;index=frame;}
            return image;
        }
        public void ReleaseRetired(){foreach(var bitmap in retired)bitmap.Dispose();retired.Clear();}
        public void Dispose(){ReleaseRetired();if(image!=null)image.Dispose();image=null;movie=null;colors=null;index=-1;}
    }
}
