using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
namespace FleetLab {
    public sealed class PatFrame {
        public readonly int Offset,PayloadBytes;
        public readonly byte[] Pixels;
        public readonly int[] Opcodes;
        public PatFrame(int offset,int payload,byte[] pixels,int[] opcodes) {Offset=offset;PayloadBytes=payload;Pixels=pixels;Opcodes=opcodes;}
    }
    // Bounded recovered variants: LWEBS20,15,4,4 and HWEBS40,30,2,2.
    // Bs.exe B9A8...A569: 0x433AA5/0x433B2D, opcode table 0x451875.
    // The fourth header byte is preserved as an unresolved variant discriminator.
    public sealed class PatMovie {
        public const int Width=160,Height=120,Scale=4,HeaderVariant=4;
        public readonly int DecodedWidth,DecodedHeight,DisplayScale,Variant;
        PatMovie(int columns,int rows,int scale,int variant){DecodedWidth=columns*8;DecodedHeight=rows*8;DisplayScale=scale;Variant=variant;}
        public readonly List<PatFrame> Frames=new List<PatFrame>();
        sealed class Reader {
            readonly byte[] bytes,pixels;readonly int end,columns,width,blocks;int p;
            public Reader(byte[] bytes,int start,int end,byte[] previous,int columns,int rows){this.bytes=bytes;p=start;this.end=end;this.columns=columns;width=columns*8;blocks=columns*rows;pixels=(byte[])previous.Clone();}
            byte Byte(){if(p>=end)throw new InvalidDataException("PAT block reads past payload at byte "+p);return bytes[p++];}
            int Word(){return Byte()|(Byte()<<8);}
            void Set(int block,int x,int y,byte value){pixels[((block/columns)*8+y)*width+(block%columns)*8+x]=value;}
            void Fill(int block,int y,int x,int count,byte value){for(int i=0;i<count;i++)Set(block,x+i,y,value);}
            // 0x4540E1's 256 generated functions produce four MSB-first 2-bit
            // selectors: three prefix colors and literal escape 3, left to right.
            void Quartet(int block,int x,int y,int code,byte[] colors){for(int i=0;i<4;i++){int which=(code>>(6-2*i))&3;Set(block,x+i,y,which==3?Byte():colors[which]);}}
            byte Adaptive(int selector,byte a,byte b,ref byte last){if(selector==0)return a;if(selector==1)return b;if(selector==3)last=Byte();return last;}
            public PatFrame Decode(int offset,int payload){
                int[] counts=new int[13];
                for(int block=0;block<blocks;block++){
                    int op=Byte();if(op>48||(op&3)!=0)throw new InvalidDataException("Unsupported PAT opcode "+op+" at block "+block+", byte "+(p-1));counts[op/4]++;
                    if(op==44)continue; // Preserve the prior frame's entire block.
                    if(op==0){byte value=Byte();for(int y=0;y<8;y++)Fill(block,y,0,8,value);continue;}
                    if(op==28){
                        byte[] colors=new byte[7];for(int i=0;i<7;i++)colors[i]=Byte();
                        for(int y=0;y<8;y++){int codes=Byte()|(Byte()<<8)|(Byte()<<16);for(int x=0;x<8;x++){int which=(codes>>(x*3))&7;Set(block,x,y,which==0?Byte():colors[which-1]);}}continue;
                    }
                    byte a=Byte(),b=Byte();
                    if(op==36||op==40){
                        byte last=0;int mask=op==40?Word():0;
                        for(int y=0;y<8;y++){
                            if(op==36){int codes=(Byte()<<8)|Byte();for(int x=0;x<8;x++)Set(block,x,y,Adaptive((codes>>(14-2*x))&3,a,b,ref last));}
                            else for(int half=0;half<2;half++){
                                bool encoded=(mask&0x8000)!=0;mask=(mask<<1)&65535;
                                if(!encoded)Fill(block,y,half*4,4,a);
                                else {int codes=Byte();for(int x=0;x<4;x++)Set(block,half*4+x,y,Adaptive((codes>>(6-2*x))&3,a,b,ref last));}
                            }
                        }continue;
                    }
                    if(op==8){
                        int rowMask=Word();byte third=(byte)rowMask;for(int y=0;y<8;y++){
                            if((rowMask&0x8000)==0)Fill(block,y,0,8,a);
                            else {int left=Byte(),right=Byte(); // CL aliases the third color/low mask byte in this native variant.
                                byte[] colors={a,b,third};Quartet(block,0,y,left,colors);Quartet(block,4,y,right,colors);}
                            rowMask=(rowMask<<1)&65535;
                        }continue;
                    }
                    byte c=Byte();byte[] tri={a,b,c};
                    if(op==4){for(int y=0;y<8;y++){int left=Byte(),right=Byte();Quartet(block,0,y,left,tri);Quartet(block,4,y,right,tri);}continue;}
                    if(op==32){
                        int rows=Byte();for(int y=0;y<8;y++){
                            if((rows&128)!=0){int left=Byte();Quartet(block,0,y,left,tri);int right=Byte();Quartet(block,4,y,right,tri);}
                            else {int bits=Byte();for(int x=0;x<8;x++)Set(block,x,y,((bits>>(7-x))&1)==0?a:b);}
                            rows=(rows<<1)&255;
                        }continue;
                    }
                    int mask16=Word();
                    if(op==20||op==24){
                        for(int half=0;half<2;half++){
                            byte[] prior={a,a,a,a};int code=0;
                            for(int y=0;y<8;y++){
                                bool encoded=(mask16&0x8000)!=0;mask16=(mask16<<1)&65535;
                                if(op==24){if(encoded)code=Byte();Quartet(block,half*4,y,code,tri);}
                                else {if(encoded){int pattern=Byte();for(int x=0;x<4;x++){int which=(pattern>>(6-2*x))&3;prior[x]=which==3?Byte():tri[which];}}for(int x=0;x<4;x++)Set(block,half*4+x,y,prior[x]);}
                            }
                        }continue;
                    }
                    for(int y=0;y<8;y++)for(int half=0;half<2;half++){
                        bool encoded=(mask16&0x8000)!=0;mask16=(mask16<<1)&65535;
                        if(encoded){int pattern=Byte();Quartet(block,half*4,y,pattern,tri);}
                        else if(op==12)Fill(block,y,half*4,4,a);
                        else if(op==16)for(int x=0;x<4;x++)Set(block,half*4+x,y,Byte());
                        // op 48 preserves the prior four pixels.
                    }
                }
                if(p!=end)throw new InvalidDataException("Unresolved PAT payload bytes: "+(end-p));
                return new PatFrame(offset,payload,pixels,counts);
            }
        }
        public static PatMovie Load(byte[] bytes){
            if(bytes==null||bytes.Length<12||bytes.Length>16*1024*1024)throw new InvalidDataException("Invalid PAT file size");
            int columns=bytes[4],rows=bytes[5],scale=bytes[6],variant=bytes[7];
            if(!(columns==20&&rows==15&&scale==4&&variant==4)&&!(columns==40&&rows==30&&scale==2&&variant==2))throw new InvalidDataException("Unsupported PAT header; expected original low/high detail variant");
            var movie=new PatMovie(columns,rows,scale,variant);byte[] previous=new byte[movie.DecodedWidth*movie.DecodedHeight];int p=0;
            while(p<=bytes.Length-4){
                uint length=BitConverter.ToUInt32(bytes,p);
                if(length==uint.MaxValue){if(movie.Frames.Count==0||p+4!=bytes.Length)throw new InvalidDataException("Invalid PAT terminal/trailing bytes");return movie;}
                if(length<4||length>65532||length>bytes.Length-p-4||movie.Frames.Count>=1024)throw new InvalidDataException("Invalid PAT frame length/count at "+p);
                if(bytes[p+4]!=columns||bytes[p+5]!=rows||bytes[p+6]!=scale||bytes[p+7]!=variant)throw new InvalidDataException("PAT variant changed within one clip");
                int end=p+4+(int)length;PatFrame frame=new Reader(bytes,p+8,end,previous,columns,rows).Decode(p,(int)length);
                movie.Frames.Add(frame);previous=frame.Pixels;p=end;
            }
            throw new InvalidDataException("PAT completion marker is missing");
        }
        public static Color[] ClipColors(byte[] pal,byte[] island,bool sharedGame){
            if(pal==null||pal.Length!=768)throw new InvalidDataException("PAT palette requires 768 bytes");
            Color[] colors=ClassicPalette.Gameplay(island);
            // 0x402732: state==2 and script state!=0x29 -> 236 entries 10..245.
            // The other branch updates only 118 entries 20..137, retaining the rest.
            int first=sharedGame?20:10,last=sharedGame?137:245;
            for(int i=first;i<=last;i++){int at=i*3;if(pal[at]>63||pal[at+1]>63||pal[at+2]>63)throw new InvalidDataException("Unsupported non-six-bit PAT palette entry");colors[i]=Color.FromArgb(pal[at]<<2,pal[at+1]<<2,pal[at+2]<<2);}
            return colors;
        }
    }
    // Modern explicit study-player lifecycle. Frame cadence comes from Classic's
    // 40 ms loop; bounded catch-up, pause, stepping and reset are deliberate policy.
    public sealed class ClipTimeline {
        public readonly int Count,FrameMilliseconds;public int Frame {get;private set;}public bool Playing {get;private set;}public bool Completed {get;private set;}
        long remainder;
        public ClipTimeline(int count):this(count,40){}
        public ClipTimeline(int count,int frameMilliseconds){if(count<1||count>1024)throw new ArgumentOutOfRangeException("count");if(frameMilliseconds!=40&&frameMilliseconds!=50)throw new ArgumentOutOfRangeException("frameMilliseconds");Count=count;FrameMilliseconds=frameMilliseconds;}
        public void Reset(){Frame=0;Playing=false;Completed=false;remainder=0;}
        public void Play(){if(Completed)Reset();Playing=true;}
        public void Pause(){Playing=false;remainder=0;}
        public void End(){Frame=Count-1;Playing=false;Completed=true;remainder=0;}
        public void Step(int direction){Pause();Frame=Math.Max(0,Math.Min(Count-1,Frame+direction));Completed=false;}
        public long DurationMilliseconds {get{return (long)Count*FrameMilliseconds;}}
        public long PositionMilliseconds {get{return Completed?DurationMilliseconds:(long)Frame*FrameMilliseconds+remainder;}}
        public bool Synchronize(long milliseconds){if(milliseconds<0)throw new ArgumentOutOfRangeException("milliseconds");if(!Playing)return false;milliseconds=Math.Max(PositionMilliseconds,milliseconds);if(milliseconds>=DurationMilliseconds){End();return true;}int next=(int)(milliseconds/FrameMilliseconds);bool changed=next!=Frame;Frame=next;remainder=milliseconds%FrameMilliseconds;return changed;}
        public bool Advance(long milliseconds){if(milliseconds<0)throw new ArgumentOutOfRangeException("milliseconds");if(!Playing)return false;remainder+=Math.Min(milliseconds,200);int ticks=(int)(remainder/FrameMilliseconds);remainder%=FrameMilliseconds;bool changed=false;while(ticks-->0&&Playing){changed=true;if(Frame+1<Count)Frame++;else End();}return changed;}
    }
}
