using System;
using System.IO;
using System.Drawing;
namespace FleetLab {
    // Native Credits polls return EOF while decoding the final raw frame.
    // The caller clears/replaces it in that same poll, before presentation.
    public static class CreditsPresentation {
        public static int VisibleFrames(int rawFrames) {
            if(rawFrames<2||rawFrames>1024)throw new InvalidDataException("Credits requires at least two decoded frames");
            return rawFrames-1;
        }
        public static int SkipFrame(int current,int rawFrames) {
            VisibleFrames(rawFrames);
            if(current<0||current>=rawFrames)throw new ArgumentOutOfRangeException("current");
            return Math.Min(rawFrames-1,current+1);
        }
        public static IndexedImage Scale(IndexedImage source) {
            if(source==null||source.Pixels==null||source.Palette==null||
                !((source.Width==160&&source.Height==120)||(source.Width==320&&source.Height==240))||
                source.Pixels.Length!=source.Width*source.Height||source.Palette.Length!=256)
                throw new InvalidDataException("Unsupported Credits image geometry");
            int scale=640/source.Width;var pixels=new byte[640*480];
            for(int y=0;y<480;y++)for(int x=0;x<640;x++)
                pixels[y*640+x]=source.Pixels[(y/scale)*source.Width+x/scale];
            return new IndexedImage{Width=640,Height=480,Pixels=pixels,Palette=(Color[])source.Palette.Clone()};
        }
    }
}
