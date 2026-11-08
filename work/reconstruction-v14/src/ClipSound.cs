using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
namespace FleetLab {
    public sealed class SoundCue {
        public string Name;public int Start,End,ActualStart,Slot=-1;
    }
    public static class ClipSound {
        public const int Rate=11025,SamplesPerFrame=441;
        public static List<SoundCue> Sequence(string text){
            if(text==null||text.Length>8192)throw new InvalidDataException("SEQ is missing or too large");
            var cues=new List<SoundCue>();bool ended=false;int previous=-1;
            using(var reader=new StringReader(text)){string line;while((line=reader.ReadLine())!=null){line=line.Trim();if(ended){if(line.Length!=0)throw new InvalidDataException("SEQ data after terminator");continue;}if(line=="*END*"){ended=true;continue;}
                string[] p=line.Split(',');int start,end;
                if(line.Length>79||p.Length!=3||p[0].Length<1||p[0].Length>8||!int.TryParse(p[1],out start)||!int.TryParse(p[2],out end)||start<0||start>1023||end<0||end>1024||(end!=0&&end<start)||cues.Count>=63)throw new InvalidDataException("Unsupported SEQ record: "+line);
                foreach(char c in p[0])if(!((c>='A'&&c<='Z')||(c>='a'&&c<='z')||(c>='0'&&c<='9')||c=='_'))throw new InvalidDataException("Invalid SEQ resource name");
                // 0x4029F0 starts only ONE queued cue per call/frame, including ties.
                int actual=Math.Max(start,previous+1);cues.Add(new SoundCue{Name=p[0],Start=start,End=end,ActualStart=actual});previous=actual;
            }}if(!ended)throw new InvalidDataException("SEQ terminator missing");return cues;
        }
        public static short[] Pcm(byte[] bytes){
            if(bytes==null||bytes.Length<44||bytes.Length>4*1024*1024||Encoding.ASCII.GetString(bytes,0,4)!="RIFF"||Encoding.ASCII.GetString(bytes,8,4)!="WAVE"||BitConverter.ToUInt32(bytes,4)!=(uint)(bytes.Length-8))throw new InvalidDataException("Invalid PCM RIFF boundaries");
            int at=12,data=-1,length=0,bits=0;bool format=false;
            while(at<=bytes.Length-8){string tag=Encoding.ASCII.GetString(bytes,at,4);uint n=BitConverter.ToUInt32(bytes,at+4);at+=8;if(n>bytes.Length-at)throw new InvalidDataException("Truncated WAV chunk");
                if(tag=="fmt "){if(format||n<16)throw new InvalidDataException("Invalid PCM format chunk");bits=BitConverter.ToUInt16(bytes,at+14);int block=bits/8;if(BitConverter.ToUInt16(bytes,at)!=1||BitConverter.ToUInt16(bytes,at+2)!=1||BitConverter.ToInt32(bytes,at+4)!=Rate||(bits!=8&&bits!=16)||BitConverter.ToInt32(bytes,at+8)!=Rate*block||BitConverter.ToUInt16(bytes,at+12)!=block)throw new InvalidDataException("Only original 11025 Hz mono 8/16-bit PCM cues are supported");format=true;}
                if(tag=="data"){if(data>=0)throw new InvalidDataException("Duplicate PCM data chunk");data=at;length=(int)n;}
                at+=(int)n+(int)(n&1);if(at>bytes.Length)throw new InvalidDataException("Missing WAV chunk padding");
            }
            if(!format||data<0||length==0||at!=bytes.Length||length%(bits/8)!=0)throw new InvalidDataException("Incomplete or misaligned PCM WAV");
            var samples=new short[length/(bits/8)];for(int i=0;i<samples.Length;i++)samples[i]=bits==8?(short)((bytes[data+i]-128)*256):BitConverter.ToInt16(bytes,data+i*2);return samples;
        }
        sealed class Voice {public string Name;public int End;}
        public static short[] Mix(int frames,List<SoundCue> cues,Func<string,short[]> load){
            return Mix(frames,40,cues,load);
        }
        public static int FrameSample(int frame,int frameMilliseconds){if(frame<0||frame>4096||(frameMilliseconds!=40&&frameMilliseconds!=50))throw new ArgumentOutOfRangeException("frame");return (int)((long)frame*Rate*frameMilliseconds/1000);}
        public static short[] Mix(int frames,int frameMilliseconds,List<SoundCue> cues,Func<string,short[]> load){
            if(frames<1||frames>1024||cues==null||load==null)throw new ArgumentException("Invalid clip mixer input");
            int[] accum=new int[FrameSample(frames,frameMilliseconds)];var voices=new Voice[8];
            foreach(var cue in cues){
                if(cue==null||cue.ActualStart<0||cue.ActualStart>4096)throw new ArgumentException("Invalid cue onset");
                cue.Slot=-1;int start=FrameSample(cue.ActualStart,frameMilliseconds);if(start>=accum.Length)continue;
                short[] wave=load(cue.Name);if(wave==null||wave.Length==0||wave.Length>4*1024*1024)throw new InvalidDataException("Invalid decoded cue");
                // Native loader 0x41A990 uses slots [0,8): reuse a completed same-name
                // slot (reverse scan), otherwise first empty/completed; drop if all busy.
                int slot=-1;for(int i=7;i>=0;i--)if(voices[i]!=null&&voices[i].End<=start&&voices[i].Name==cue.Name){slot=i;break;}
                if(slot<0)for(int i=0;i<8;i++)if(voices[i]==null){slot=i;break;}
                if(slot<0)for(int i=0;i<8;i++)if(voices[i].End<=start){slot=i;break;}
                if(slot<0)continue;cue.Slot=slot;voices[slot]=new Voice{Name=cue.Name,End=start+wave.Length};
                // Finite (Play flag 0) source samples. The native valid-handle end
                // threshold branch does not stop them; End is preserved, not applied.
                for(int i=0;i<wave.Length&&start+i<accum.Length;i++)accum[start+i]+=wave[i];
            }
            short[] result=new short[accum.Length];for(int i=0;i<result.Length;i++)result[i]=(short)Math.Max(short.MinValue,Math.Min(short.MaxValue,accum[i]));return result;
        }
        public static short[] Load(string assets,string clip,int frames,out List<SoundCue> cues){
            return Load(assets,clip,frames,40,out cues);
        }
        public static short[] Load(string assets,string clip,int frames,int frameMilliseconds,out List<SoundCue> cues){
            string dir=Path.Combine(assets,"SFX",clip);cues=Sequence(File.ReadAllText(Path.Combine(dir,clip+".SEQ")));
            var cache=new Dictionary<string,short[]>(StringComparer.OrdinalIgnoreCase);
            return Mix(frames,frameMilliseconds,cues,delegate(string name){short[] wave;if(!cache.TryGetValue(name,out wave)){wave=Pcm(File.ReadAllBytes(Path.Combine(dir,name+".WAV")));cache.Add(name,wave);}return wave;});
        }
    }
    public static class FiringClips {
        static string[] All(){var names=new List<string>(SequenceNames);names.AddRange(new[]{"CAR_UMH","CAR_USH","BATT_UMH","BATT_UTH","BATT_USH","BATT_DMH","BATTSINK","SUB_USH","FRG_UMH","FRG_USH","TORPMISA","TORPMISB","TORPMISC","SHELMISA","SHELMISB","CRUISDIE","BATT_DTH","FRG_UTH","BATT_DSH","CAR_UTH","SHELMISC","SUB_UTH","FRIGSINK","SUB_UMH","SUB_DIE","CARRSINK","CAR_DSH","CAR_DTH","CAR_DMH","FRG_DTH","FRG_DMH","FRG_DSH","MIS_FLY2"});return names.ToArray();}
        public static readonly string[] FiringNames={"BATFIRE1","BATFIRE2","BATFIRE3","BATFIRE4","F18FIRE1","F18FIRE2","SUBTORP1","SUBTORP2","SUBMISS1","SUBMISS2","SML_LNC1","SML_LNC2","TWOFIRE1","TWOFIRE2","FRG_FTRP","BATT_LT1","BATT_LT2","FRG_LT2","MEDFIRE1","MEDFIRE2"};
        public static readonly string[] SequenceNames=Sequence();
        public static readonly string[] Names=All();
        static string[] Sequence(){var names=new List<string>(FiringNames);names.AddRange(new[]{"S_FLY1","S_FLY2","S_FLY3","TORPRUN","MIS_FLY3","CRS_FLY1","CL_MISS","CL_HIT"});return names.ToArray();}
    }
}

