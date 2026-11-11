using System;
using System.IO;
using System.Collections.Generic;
namespace FleetLab {
    // Supported Bs.exe427070/427660: six registered styles of94 SPH glyphs.
    public sealed class ClassicMenuFont {
        readonly List<IndexedImage> frames;
        public ClassicMenuFont(List<IndexedImage> glyphs){
            if(glyphs==null||glyphs.Count!=565||glyphs[564]!=null)throw new InvalidDataException("Unsupported original menu font table");
            for(int i=0;i<564;i++)if(glyphs[i]==null||glyphs[i].Width<1||glyphs[i].Width>255)throw new InvalidDataException("Unsupported menu glyph metrics");
            frames=glyphs;
        }
        public int Draw(byte[] pixels,int style,int x,int y,string text){
            if(pixels==null||pixels.Length!=640*480||style<0||style>5||text==null||text.Length>8192||x< -32768||x>32767||y< -32768||y>32767)throw new InvalidDataException("Invalid original menu text draw");
            foreach(char c in text)if(c<32||c>126)throw new InvalidDataException("Original menu glyphs require printable ASCII");
            int cursor=x;
            foreach(char c in text){
                // The original space advance uses this style's E-glyph width.
                if(c==' '){cursor+=frames[style*94+36].Width;continue;}
                IndexedImage glyph=frames[c+style*94-33];
                Blit(pixels,glyph,cursor,y);
                cursor+=glyph.Width+(style==1?1:style==4?3:0);
            }
            return cursor;
        }
        internal static void Blit(byte[] pixels,IndexedImage image,int x,int y){
            for(int row=0;row<image.Height;row++)for(int column=0;column<image.Width;column++){
                int px=x+column,py=y+row,at=row*image.Width+column;
                if(px>=0&&px<640&&py>=0&&py<480&&image.Visible[at]&&image.Pixels[at]!=0)pixels[py*640+px]=image.Pixels[at];
            }
        }
    }
}
