using System;using System.IO;using System.Text;using System.Collections.Generic;
namespace FleetLab {
 // Modern integer-ratio box downsample/downmix into the existing11025Hz mono
 // mixer. This is not a reconstruction of the native device's conversion.
 public static class StartupSound {
  public static short[] Pcm(byte[] bytes){
   if(bytes==null||bytes.Length<44||bytes.Length>4*1024*1024||Encoding.ASCII.GetString(bytes,0,4)!="RIFF"||Encoding.ASCII.GetString(bytes,8,4)!="WAVE"||BitConverter.ToUInt32(bytes,4)!=(uint)(bytes.Length-8))throw new InvalidDataException("Invalid startup WAV RIFF boundaries");
   int at=12,data=-1,length=0,rate=0,bits=0,channels=0,block=0;bool format=false;
   while(at<=bytes.Length-8){string tag=Encoding.ASCII.GetString(bytes,at,4);uint n=BitConverter.ToUInt32(bytes,at+4);at+=8;if(n>bytes.Length-at)throw new InvalidDataException("Truncated startup WAV chunk");if(tag=="fmt "){if(format||n<16)throw new InvalidDataException("Invalid startup PCM format");format=true;channels=BitConverter.ToUInt16(bytes,at+2);rate=BitConverter.ToInt32(bytes,at+4);bits=BitConverter.ToUInt16(bytes,at+14);block=channels*(bits/8);if(BitConverter.ToUInt16(bytes,at)!=1||(channels!=1&&channels!=2)||(rate!=11025&&rate!=22050&&rate!=44100)||(bits!=8&&bits!=16)||BitConverter.ToUInt16(bytes,at+12)!=block||BitConverter.ToInt32(bytes,at+8)!=rate*block)throw new InvalidDataException("Unsupported startup PCM rate/channels/bits");}if(tag=="data"){if(data>=0)throw new InvalidDataException("Duplicate startup data");data=at;length=(int)n;}at+=(int)n+(int)(n&1);if(at>bytes.Length)throw new InvalidDataException("Startup WAV padding missing");}
   if(!format||data<0||length==0||at!=bytes.Length||length%block!=0)throw new InvalidDataException("Incomplete startup WAV");int ratio=rate/ClipSound.Rate,inputFrames=length/block,count=(inputFrames+ratio-1)/ratio;var output=new short[count];
   for(int i=0;i<count;i++){int first=i*ratio,end=Math.Min(first+ratio,inputFrames),sum=0;for(int f=first;f<end;f++)for(int c=0;c<channels;c++){int p=data+f*block+c*(bits/8);sum+=bits==8?(bytes[p]-128)*256:BitConverter.ToInt16(bytes,p);}output[i]=(short)(sum/((end-first)*channels));}return output;
  }
  public static string SequenceText(byte[] bytes){if(bytes==null||bytes.Length>8192)throw new InvalidDataException("Startup SEQ size");string text=Encoding.ASCII.GetString(bytes);int eof=text.IndexOf((char)26);if(eof>=0){for(int i=eof+1;i<text.Length;i++)if(!char.IsWhiteSpace(text[i]))throw new InvalidDataException("Data after startup DOS EOF");text=text.Substring(0,eof);}return text;}
  public static ClipMix LoadMix(string assets,string name,int frames,out List<SoundCue> cues){string dir=Path.Combine(assets,"SFX",StartupProgram.SoundDirectory(name)),path=Path.Combine(dir,StartupProgram.SoundDirectory(name)+".SEQ");
   if(name=="NMSLOGO1"&&!File.Exists(path)){cues=new List<SoundCue>();return ClipSound.MixRaw(frames,50,cues,delegate{throw new InvalidDataException("Unexpected silent-logo cue");});}
   cues=ClipSound.Sequence(SequenceText(File.ReadAllBytes(path)));return ClipSound.MixRaw(frames,50,cues,delegate(string wave){return Pcm(File.ReadAllBytes(Path.Combine(dir,wave+".WAV")));});
  }
 }
}
