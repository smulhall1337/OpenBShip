using System;
using System.IO;
using System.Text;
namespace FleetLab {
    // Original cinematic caller4223B6 and DirectSound setter41B0E0. The modern
    // adapter makes Off silent; native zero requests -40dB rather than -100dB.
    public static class SoundLevel {
        public static void Validate(int percent){if(percent<0||percent>100)throw new ArgumentOutOfRangeException("percent");}
        public static int NativeBufferVolume(int percent){Validate(percent);return percent*4;}
        public static int NativeHundredthsDb(int percent){return NativeBufferVolume(percent)*10-4000;}
        public static double Gain(int percent){Validate(percent);return percent==0?0:Math.Pow(10,NativeHundredthsDb(percent)/2000.0);}
        static int Scale(int sample,double gain,int low,int high){return Math.Max(low,Math.Min(high,(int)Math.Round(sample*gain,MidpointRounding.AwayFromZero)));}
        public static short[] Mix(int[] sums,int percent){
            if(sums==null)throw new ArgumentNullException("sums");double gain=Gain(percent);var output=new short[sums.Length];
            for(int i=0;i<output.Length;i++)output[i]=(short)Scale(sums[i],gain,short.MinValue,short.MaxValue);return output;
        }
        // The two application effects differ: KEYCLIK2 is22050Hz/16bit and
        // CANNON1 is11025Hz/8bit. Preserve the RIFF envelope/ancillary chunks.
        public static byte[] Wave(byte[] source,int percent){
            Validate(percent);
            if(source==null||source.Length<44||source.Length>4*1024*1024)throw new InvalidDataException("Invalid effect WAV length");
            if(Encoding.ASCII.GetString(source,0,4)!="RIFF"||Encoding.ASCII.GetString(source,8,4)!="WAVE"||BitConverter.ToUInt32(source,4)!=source.Length-8)throw new InvalidDataException("Invalid effect RIFF envelope");
            int at=12,data=-1,length=0,bits=0;bool format=false;
            while(at<=source.Length-8){
                string tag=Encoding.ASCII.GetString(source,at,4);uint count=BitConverter.ToUInt32(source,at+4);at+=8;
                if(count>source.Length-at)throw new InvalidDataException("Truncated effect WAV chunk");
                if(tag=="fmt "){
                    if(format||count<16)throw new InvalidDataException("Invalid effect PCM format");format=true;bits=BitConverter.ToUInt16(source,at+14);int rate=BitConverter.ToInt32(source,at+4),block=bits/8;
                    if(BitConverter.ToUInt16(source,at)!=1||BitConverter.ToUInt16(source,at+2)!=1||(bits!=8&&bits!=16)||(rate!=11025&&rate!=22050)||BitConverter.ToUInt16(source,at+12)!=block||BitConverter.ToInt32(source,at+8)!=rate*block)throw new InvalidDataException("Unsupported effect PCM format");
                }
                else if(tag=="data"){if(data>=0)throw new InvalidDataException("Duplicate effect PCM data");data=at;length=(int)count;}
                at+=(int)count+(int)(count&1);if(at>source.Length)throw new InvalidDataException("Missing effect WAV padding");
            }
            if(at!=source.Length||!format||data<0||length==0||length%(bits/8)!=0)throw new InvalidDataException("Incomplete effect PCM WAV");
            var output=(byte[])source.Clone();double gain=Gain(percent);
            if(bits==8){for(int i=data;i<data+length;i++)output[i]=(byte)(128+Scale(source[i]-128,gain,-128,127));}
            else for(int i=data;i<data+length;i+=2){short value=(short)Scale(BitConverter.ToInt16(source,i),gain,short.MinValue,short.MaxValue);output[i]=(byte)value;output[i+1]=(byte)(value>>8);}
            return output;
        }
    }
    public sealed class ClipMix {
        readonly int[] sums;
        internal ClipMix(int[] samples){sums=samples;}
        public short[] Pcm(int percent){return SoundLevel.Mix(sums,percent);}
        public int Count {get{return sums.Length;}}
    }
}
