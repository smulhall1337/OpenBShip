using System;using System.IO;using System.Drawing;using System.Collections.Generic;
namespace FleetLab {
    // Exact two original entry/initialization prefixes; deliberately not a script VM.
    public sealed class TitleButton {
        public readonly int X,Y,Frame,Group,Action;public readonly Rectangle Bounds;
        public TitleButton(int x,int y,int frame,int group,int action,int width,int height):this(x,y,frame,group,action,width,height,200){}
        public TitleButton(int x,int y,int frame,int group,int action,int width,int height,int extraWidth){X=x;Y=y;Frame=frame;Group=group;Action=action;Bounds=new Rectangle(x,y,width+extraWidth,height);}
    }
    public sealed class TitleProgram {
        public readonly List<TitleButton> Buttons=new List<TitleButton>();
        static uint[] Words(byte[] b,int count){if(b==null||b.Length!=count*4)throw new InvalidDataException("Unsupported menu prefix length");var w=new uint[count];for(int i=0;i<count;i++)w[i]=BitConverter.ToUInt32(b,i*4);return w;}
        static void Equal(uint[] w,int at,params uint[] expected){for(int i=0;i<expected.Length;i++)if(w[at+i]!=expected[i])throw new InvalidDataException("Unsupported menu instruction/operand at byte "+((at+i)*4));}
        public static TitleProgram Read(byte[] entry,byte[] title,IList<IndexedImage> frames){
            uint[] e=Words(entry,14),w=Words(title,42);Equal(e,0,21,0x457b58,2,21,0x456ca0,1,26,0x4321f0,0,0x457b58,18,0x44efc0,4,0);
            Equal(w,0,8,0x44ef34,32,0,38,4);Equal(w,26,13,11,9,19,0x456ca0,12,15,21,0x456ca0,0,13,12,21,0x456a18,0,39);
            if(frames==null||frames.Count!=9||frames[8]!=null)throw new InvalidDataException("Unsupported title sprite table");
            var result=new TitleProgram();int[] ys={231,273,313,375},actions={1,2,5,4};
            for(int i=0;i<4;i++){Equal(w,6+i*5,128,(uint)ys[i],(uint)i,27,(uint)actions[i]);var left=frames[i];var right=frames[i+4];if(left==null||right==null||left.Width!=200||right.Width!=174||left.Height!=44||right.Height!=44)throw new InvalidDataException("Unsupported title button geometry");result.Buttons.Add(new TitleButton((int)w[6+i*5],(int)w[7+i*5],(int)w[8+i*5],(int)w[9+i*5],(int)w[10+i*5],left.Width,left.Height));}
            return result;
        }
        public int Hit(int x,int y){for(int i=0;i<Buttons.Count;i++)if(Buttons[i].Bounds.Contains(x,y))return i;return -1;}
        public static IndexedImage Compose(IndexedImage card,IList<IndexedImage> frames,TitleProgram program,int selected){
            if(card==null||card.Width!=640||card.Height<480||card.Height>512||selected<0||selected>=program.Buttons.Count)throw new InvalidDataException("Unsupported title composition");
            var image=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=card.Palette};Array.Copy(card.Pixels,image.Pixels,image.Pixels.Length);var b=program.Buttons[selected];
            for(int half=0;half<2;half++){var frame=frames[b.Frame+half*4];int x=b.X+half*200,y=b.Y;for(int py=0;py<frame.Height;py++)for(int px=0;px<frame.Width;px++){int index=py*frame.Width+px;byte value=frame.Pixels[index];if(frame.Visible[index]&&value!=0)image.Pixels[(y+py)*640+x+px]=value;}}
            return image;
        }
    }
}
