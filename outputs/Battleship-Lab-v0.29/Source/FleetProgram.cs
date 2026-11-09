using System;using System.IO;using System.Drawing;using System.Collections.Generic;
namespace FleetLab {
    // Strict bounded Classic branch through wait37, not a general script VM.
    public sealed class FleetProgram {
        public readonly List<SetupButton> Buttons=new List<SetupButton>();
        // PCX storage is640x512; native menus display only the top640x480.
        // Crop before drawing overlays so visible controls and hit regions agree.
        public static IndexedImage Viewport(IndexedImage source) {
            if(source==null||source.Width!=640||source.Height<480||source.Height>512||source.Pixels==null||source.Pixels.Length!=source.Width*source.Height||source.Palette==null||source.Palette.Length!=256||source.Visible!=null&&source.Visible.Length!=source.Pixels.Length)throw new InvalidDataException("Unsupported Fleet background");
            var result=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=source.Palette};
            Array.Copy(source.Pixels,result.Pixels,result.Pixels.Length);
            if(source.Visible!=null){result.Visible=new bool[result.Pixels.Length];Array.Copy(source.Visible,result.Visible,result.Visible.Length);}
            return result;
        }
        public static FleetProgram Read(byte[] flow,byte[] buttons,List<IndexedImage> sprites) {
            uint[] expected={21,0x457b58,2,13,5,34,0x4e3868,20,34,8,0x44eedc,16,33,13,34,35,0x55ea48,0,32,8,0x44eed0,16,33,13,32,8,0x44eec4,13,33,32,0,33,26,0x430660,0,0x457b58,9,26,0x430880,0,0x457b58,15,21,0x457b58,2,37};
            if(flow==null||flow.Length!=expected.Length*4)throw new InvalidDataException("Unsupported Fleet prefix length");
            for(int i=0;i<expected.Length;i++)if(BitConverter.ToUInt32(flow,i*4)!=expected[i])throw new InvalidDataException("Unsupported Fleet instruction at byte "+i*4);
            if(buttons==null||buttons.Length!=224||BitConverter.ToInt32(buttons,0)!=11||sprites==null||sprites.Count!=32||sprites[31]!=null)throw new InvalidDataException("Unsupported Fleet table");
            int[] records={102,108,25,31,21,421,108,24,31,22,15,431,26,31,97,498,431,27,31,96,539,117,7,31,1,540,154,1,31,2,543,194,6,31,3,545,248,5,31,4,539,285,3,31,5,546,328,2,31,6,551,369,4,31,7};
            for(int i=0;i<records.Length;i++)if(BitConverter.ToInt32(buttons,4+i*4)!=records[i])throw new InvalidDataException("Unsupported Fleet button at word "+i);
            var result=new FleetProgram();for(int i=0;i<11;i++){int p=i*5;var frame=sprites[records[p+2]];if(frame==null)throw new InvalidDataException("Missing Fleet button");result.Buttons.Add(new SetupButton(records[p],records[p+1],records[p+2],31,records[p+4],frame));}return result;
        }
        public int Hit(int x,int y){for(int i=0;i<Buttons.Count;i++)if(Buttons[i].Bounds.Contains(x,y))return i;return -1;}
    }
    public static class FleetGeometry {
        public static Point Project(int x,int y){return new Point(320+(x+y-10)*147/6,240+(y-x)*98/6);}
        public static Point? Pick(PointF p){if(float.IsNaN(p.X)||float.IsNaN(p.Y)||p.X<0||p.Y<0||p.X>=640||p.Y>=480)return null;float h=(float)(((int)p.X-75)/147.0*3);double v=((int)p.Y-240)/98.0*3;var cell=new Point((int)(h-v),(int)(h+v));return Board.Inside(cell)?(Point?)cell:null;}
        public static Point Anchor(Point start,int length){var p=Project(start.X,start.Y+length/2);p.Offset((length&1)!=0?24:12,(length&1)!=0?0:-8);return p;}
    }
}
