using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
namespace FleetLab {
    // Original Bs.exe B9A8...A569 RT_CURSOR resources, referenced by the named
    // TARGET_CUR/HAND_CUR groups. The resource prefixes its DIB with a hotspot.
    public sealed class ClassicCursorData {
        public readonly Point Hotspot;
        readonly byte[] dib;
        ClassicCursorData(Point hotspot,byte[] image){Hotspot=hotspot;dib=image;}
        public static ClassicCursorData Read(byte[] data){
            if(data==null||data.Length!=308)throw new InvalidDataException("Expected original32x32monochrome cursor resource");
            int x=BitConverter.ToUInt16(data,0),y=BitConverter.ToUInt16(data,2);
            if(x>=32||y>=32||BitConverter.ToInt32(data,4)!=40||BitConverter.ToInt32(data,8)!=32||BitConverter.ToInt32(data,12)!=64||BitConverter.ToUInt16(data,16)!=1||BitConverter.ToUInt16(data,18)!=1||BitConverter.ToInt32(data,20)!=0||BitConverter.ToInt32(data,24)!=128||BitConverter.ToInt32(data,36)!=0||BitConverter.ToInt32(data,40)!=0)throw new InvalidDataException("Unsupported cursor DIB/header/hotspot");
            byte[] image=new byte[304];Array.Copy(data,4,image,0,image.Length);return new ClassicCursorData(new Point(x,y),image);
        }
        public byte[] CurFile(){
            using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){
                writer.Write((ushort)0);writer.Write((ushort)2);writer.Write((ushort)1);
                writer.Write((byte)32);writer.Write((byte)32);writer.Write((byte)2);writer.Write((byte)0);
                writer.Write((ushort)Hotspot.X);writer.Write((ushort)Hotspot.Y);writer.Write(dib.Length);writer.Write(22);writer.Write(dib);return stream.ToArray();
            }
        }
    }
    // Borrow system cursors; own only the two handles made from original data.
    internal sealed class ClassicCursors:IDisposable {
        public readonly Cursor Target,Hand;
        ClassicCursors(Cursor target,Cursor hand){Target=target;Hand=hand;}
        static Cursor Load(string path){using(var stream=new MemoryStream(ClassicCursorData.Read(File.ReadAllBytes(path)).CurFile()))return new Cursor(stream);}
        public static ClassicCursors TryLoad(string programs){
            string target=Path.Combine(programs,"TargetCursor.seqbin"),hand=Path.Combine(programs,"HandCursor.seqbin");
            if(!File.Exists(target)||!File.Exists(hand))return null;
            Cursor first=Load(target);try{return new ClassicCursors(first,Load(hand));}catch{first.Dispose();throw;}
        }
        bool disposed;
        public void Dispose(){if(disposed)return;disposed=true;Target.Dispose();Hand.Dispose();}
    }
    // Native 0x4012D0:0arrow,1TARGET_CUR,2system wait,3HAND_CUR.
    internal enum BoardPointer {Arrow,Target,Wait,Hand}
}
