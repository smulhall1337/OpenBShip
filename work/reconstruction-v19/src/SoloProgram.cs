using System;using System.IO;using System.Drawing;using System.Collections.Generic;
namespace FleetLab {
    public sealed class SetupButton {public readonly int X,Y,Frame,Group,Action;public readonly Rectangle Bounds;public SetupButton(int x,int y,int frame,int group,int action,IndexedImage image){X=x;Y=y;Frame=frame;Group=group;Action=action;Bounds=new Rectangle(x,y,image.Width,image.Height);}}
    public sealed class SetupDraw {public readonly int Index,Frame;public SetupDraw(int index,int frame){Index=index;Frame=frame;}}
    public sealed class SoloState {
        public int Type {get;private set;}public int Players {get;private set;}readonly SoloProgram program;
        public SoloState(SoloProgram value){program=value;Type=1;Players=2;}
        public void SelectType(int value){if(value!=1&&value!=2&&value!=3&&value!=20)throw new ArgumentOutOfRangeException("value");Type=value;Clamp();}
        public void SelectPlayers(int value){if(value<2||value>4)throw new ArgumentOutOfRangeException("value");Players=value;Clamp();}
        void Clamp(){if(Players>program.MaxPlayers(Type))Players=2;}
        public bool Playable {get{return Type==20&&Players==2;}}
    }
    // Bounded Practice setup reader. Other original screens/scripts remain unsupported.
    public sealed class SoloProgram {
        public readonly List<SetupButton> Buttons=new List<SetupButton>();readonly Dictionary<int,List<IndexedImage>> sprites;readonly string[] descriptions;readonly int[] limits;
        SoloProgram(Dictionary<int,List<IndexedImage>> resources,string[] text,int[] max){sprites=resources;descriptions=text;limits=max;}
        static uint[] Words(byte[] b,int count){if(b==null||b.Length!=count*4)throw new InvalidDataException("Unsupported Solo prefix length");var result=new uint[count];for(int i=0;i<count;i++)result[i]=BitConverter.ToUInt32(b,i*4);return result;}
        static void Equal(uint[] b,int at,params uint[] expected){for(int i=0;i<expected.Length;i++)if(b[at+i]!=expected[i])throw new InvalidDataException("Unsupported Solo instruction/operand at byte "+((at+i)*4));}
        public static SoloProgram Read(byte[] entry,byte[] setup,byte[] playerLimits,Dictionary<int,List<IndexedImage>> resources,string[] text){
            var e=Words(entry,18);Equal(e,0,21,0x457b58,2,21,0x456ca0,1,26,0x4321b0,0,0x457b58,8,0x44ef10,9,15,18,0x44f440,4,0);
            var w=Words(setup,97);Equal(w,0,21,0x456a18,2,21,0x457b58,2,21,0x4e3868,1,21,0x456a00,0,21,0x55e804,0,8,0x44ef10,38,14);Equal(w,89,9,26,0x432490,0,0x457b58,13,99,39);
            int[] groups={28,29,38},counts={25,7,16};for(int i=0;i<3;i++){List<IndexedImage> f;if(resources==null||!resources.TryGetValue(groups[i],out f)||f.Count!=counts[i]||f[f.Count-1]!=null)throw new InvalidDataException("Unsupported Solo sprite table");}
            if(text==null||text.Length!=21)throw new InvalidDataException("Expected21original mission descriptions");foreach(string line in text)if(line==null||line.Length==0||line.Length>8192||line.IndexOf('\0')>=0)throw new InvalidDataException("Invalid mission description");var raw=Words(playerLimits,21);var max=new int[21];for(int i=0;i<21;i++){uint expected=i==8||i>=18?2u:4u;if(raw[i]!=expected)throw new InvalidDataException("Unsupported player limit table");max[i]=(int)raw[i];}
            uint[] records={15,101,0,28,99,118,100,1,28,21,238,100,2,28,22,15,179,3,28,23,124,179,4,28,24,232,179,6,28,25,462,71,0,29,26,462,160,1,29,1,462,248,2,29,2,462,336,3,29,3,15,431,4,29,27,498,431,5,29,28,232,221,11,38,29,314,221,12,38,30};Equal(w,19,records);
            var result=new SoloProgram(resources,(string[])text.Clone(),max);for(int i=0;i<14;i++){int at=19+i*5,group=(int)w[at+3],frame=(int)w[at+2];var image=resources[group][frame];if(image==null||image.Width<1||image.Height<1||w[at]+image.Width>640||w[at+1]+image.Height>480)throw new InvalidDataException("Solo button outside viewport");result.Buttons.Add(new SetupButton((int)w[at],(int)w[at+1],frame,group,(int)w[at+4],image));}return result;
        }
        public int MaxPlayers(int type){if(type<0||type>=limits.Length)throw new ArgumentOutOfRangeException("type");return limits[type];}
        public string Description(int type){if(type<0||type>=descriptions.Length)throw new ArgumentOutOfRangeException("type");return descriptions[type];}
        public int Hit(int x,int y){for(int i=0;i<Buttons.Count;i++)if(Buttons[i].Bounds.Contains(x,y))return i;return -1;}
        public List<SetupDraw> Commands(SoloState state,int hover){var result=new List<SetupDraw>{new SetupDraw(0,Buttons[0].Frame),new SetupDraw(state.Players+1,Buttons[state.Players+1].Frame)};if(MaxPlayers(state.Type)==2){result.Add(new SetupDraw(4,Buttons[4].Frame+1));result.Add(new SetupDraw(5,Buttons[5].Frame+1));}int mode=state.Type==20?6:state.Type+6;result.Add(new SetupDraw(mode,Buttons[mode].Frame));if(hover==10||hover==11)result.Add(new SetupDraw(hover,Buttons[hover].Frame));return result;}
        public IndexedImage Compose(IndexedImage card,SoloState state,int hover=-1){if(card==null||card.Width!=640||card.Height<480||card.Height>512)throw new InvalidDataException("Unsupported Solo background");var output=new IndexedImage{Width=640,Height=480,Pixels=new byte[640*480],Palette=card.Palette};Array.Copy(card.Pixels,output.Pixels,output.Pixels.Length);foreach(var command in Commands(state,hover)){var b=Buttons[command.Index];var frame=sprites[b.Group][command.Frame];if(frame==null)throw new InvalidDataException("Missing Solo frame");for(int y=0;y<frame.Height;y++)for(int x=0;x<frame.Width;x++){int at=y*frame.Width+x;if(frame.Visible[at]&&frame.Pixels[at]!=0)output.Pixels[(b.Y+y)*640+b.X+x]=frame.Pixels[at];}}return output;}
    }
}
