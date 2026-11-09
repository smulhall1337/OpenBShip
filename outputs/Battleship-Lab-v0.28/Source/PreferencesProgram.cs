using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace FleetLab {
    public sealed class PreferenceValues {
        public int Quality=1,Radar,Difficulty,Volume=100;
        public PreferenceValues Copy(){return new PreferenceValues{Quality=Quality,Radar=Radar,Difficulty=Difficulty,Volume=Volume};}
        public void Validate(){if(Quality<1||Quality>2||Radar<0||Radar>1||Difficulty<0||Difficulty>3||Volume<0||Volume>100||Volume%5!=0)throw new InvalidDataException("Unsupported Preferences state");}
    }
    public sealed class PreferenceButton {
        public readonly int Frame,Action,Group;public readonly Point At;public readonly Size Size;
        public Rectangle Bounds {get{return new Rectangle(At,Size);}}
        public PreferenceButton(int x,int y,int frame,int group,int action,Size size){At=new Point(x,y);Frame=frame;Group=group;Action=action;Size=size;}
    }
    // Bounded original English script450EC8..451250, not a general script VM.
    public sealed class PreferencesProgram {
        public readonly byte[] OriginalScript;
        public readonly List<PreferenceButton> Buttons=new List<PreferenceButton>();
        public static readonly Rectangle VolumeBar=new Rectangle(142,354,263,30);
        static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}
        PreferencesProgram(byte[] bytes){OriginalScript=(byte[])bytes.Clone();}
        public static PreferencesProgram Read(byte[] bytes,IList<IndexedImage> frames){
            if(bytes==null||bytes.Length!=904||Hash(bytes)!="04B5AF428E3B3226AF229882D12AC697B2F8F3383CB46A8300A794220FE37887")throw new InvalidDataException("Unsupported Preferences script");
            if(frames==null||frames.Count!=34||frames[33]!=null)throw new InvalidDataException("Unsupported Preferences sprite table");
            int[] smallWidths={164,163,164,163,79,80,79,79};for(int i=0;i<33;i++){int width=i<8?smallWidths[i]:i<29?64:i==29?100:i==30?162:125;if(frames[i]==null||frames[i].Height!=30||frames[i].Width!=width)throw new InvalidDataException("Unsupported Preferences sprite metrics");}
            var result=new PreferencesProgram(bytes);int[] actions={1,2,3,4,5,6,7,8,9,55,10,97,99};
            for(int i=0;i<13;i++){
                int at=40+i*20,x=BitConverter.ToInt32(bytes,at),y=BitConverter.ToInt32(bytes,at+4),frame=BitConverter.ToInt32(bytes,at+8),group=BitConverter.ToInt32(bytes,at+12),action=BitConverter.ToInt32(bytes,at+16);
                if(group!=30||action!=actions[i]||(frame<0&&frame!=-2)||frame>=33)throw new InvalidDataException("Unsupported Preferences button record");
                Size size=frame==-2?new Size(263,36):new Size(frames[frame].Width,frames[frame].Height);
                if(x<0||y<0||x+size.Width>640||y+size.Height>480)throw new InvalidDataException("Preferences button outside viewport");
                result.Buttons.Add(new PreferenceButton(x,y,frame,group,action,size));
            }
            return result;
        }
        public int Hit(int x,int y,bool right){
            for(int i=0;i<Buttons.Count;i++){var button=Buttons[i];if(right?button.Action!=55:button.Action==55)continue;if(button.Bounds.Contains(x,y))return i;}return -1;
        }
        public static int BarValue(int x){if(x<VolumeBar.Left||x>=VolumeBar.Right)throw new ArgumentOutOfRangeException("x");int value=Math.Min(100,(x-142)*100/263+6);if(value<10)value=0;return value/5*5;}
        public static PreferenceValues Change(PreferenceValues current,int action){
            current.Validate();var next=current.Copy();
            // Opcode30 subtracts its operand: -5 increments; +5 decrements.
            switch(action){case 1:case 2:next.Quality=action;break;case 3:case 4:next.Radar=action-3;break;case 5:case 6:case 7:case 8:next.Difficulty=action-5;break;case 9:next.Volume=next.Volume==100?0:next.Volume+5;break;case 55:next.Volume=Math.Max(0,next.Volume-5);break;case 98:next.Volume=0;break;default:throw new InvalidDataException("Preferences action requires an external boundary: "+action);}
            return next;
        }
        public IndexedImage Compose(IndexedImage card,IList<IndexedImage> frames,PreferenceValues state,int hover){
            state.Validate();if(card==null||card.Width!=640||card.Height<480||card.Height>512||card.Pixels==null||card.Pixels.Length!=card.Width*card.Height||hover< -1||hover>=Buttons.Count)throw new InvalidDataException("Unsupported Preferences composition");
            var output=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=card.Palette};Array.Copy(card.Pixels,output.Pixels,output.Pixels.Length);
            Action<int,int,int> draw=delegate(int frame,int x,int y){var source=frames[frame];for(int sy=0;sy<source.Height;sy++)for(int sx=0;sx<source.Width;sx++){int p=sy*source.Width+sx;if(source.Visible[p]&&source.Pixels[p]!=0)output.Pixels[(y+sy)*640+x+sx]=source.Pixels[p];}};
            foreach(int index in new[]{state.Quality-1,state.Radar+2,state.Difficulty+4}){var b=Buttons[index];draw(b.Frame,b.At.X,b.At.Y);}
            draw(8+state.Volume/5,410,354);
            int width=263*state.Volume/100;for(int y=354;y<384;y++)for(int x=142;x<142+width;x++)output.Pixels[y*640+x]=18;
            foreach(var p in new[]{new Point(142,381),new Point(142,382),new Point(142,383),new Point(143,383),new Point(144,383)})output.Pixels[p.Y*640+p.X]=0;
            if(hover==10||hover==12){var b=Buttons[hover];draw(b.Frame,b.At.X,b.At.Y);}else if(hover==11){draw(29,15,431);draw(30,115,431);}return output;
        }
    }
}
