using System;using System.IO;using System.Collections.Generic;
namespace FleetLab {
    // Exact post-fleet prefix at450E18, only the local Solo(-1) route.
    public sealed class EntryProgram {
        public const string Card="E_WAIT1.PCX";
        public static EntryProgram Read(byte[] bytes,int slot) {
            uint[] expected={8,0x44eeac,9,21,0x457b58,6,19,0x55ea48,10,35,0x55ea48,0,11,9,15,16,99,13,10,8,0x44eea4,9,33,15,16,99,13,11,15,16,99,13,98,14,16,5,13,96,14,18,0x44f3f8,13,99,4};
            if(bytes==null||bytes.Length!=expected.Length*4)throw new InvalidDataException("Unsupported battle entry length");
            for(int i=0;i<expected.Length;i++)if(BitConverter.ToUInt32(bytes,i*4)!=expected[i])throw new InvalidDataException("Unsupported battle entry word "+i);
            if(slot!=-1)throw new InvalidDataException("Only local Solo battle entry is implemented; network entry is unsupported");
            return new EntryProgram();
        }
    }
    public sealed class EntryTimeline {
        // Factor order is recovered;40ms is an explicit modern cadence.
        public const int StepMilliseconds=40;
        public int Factor {get;private set;}public bool Playing {get;private set;}public bool Complete {get;private set;}
        public readonly List<string> Trace=new List<string>();long elapsed;
        public EntryTimeline(EntryProgram program){if(program==null)throw new ArgumentNullException("program");Factor=12;Playing=true;Trace.Add("Card,e_wait1");Trace.Add("Fade,12");}
        public void Pause(){Playing=false;}public void Resume(){if(!Complete)Playing=true;}
        public bool Advance(long milliseconds){if(milliseconds<0)throw new ArgumentOutOfRangeException("milliseconds");if(!Playing||Complete)return false;bool changed=false;
            while(milliseconds>0&&!Complete){long used=Math.Min(milliseconds,StepMilliseconds-elapsed);elapsed+=used;milliseconds-=used;if(elapsed<StepMilliseconds)break;elapsed=0;changed=true;if(Factor<64){Factor+=4;Trace.Add("Fade,"+Factor);}else{Complete=true;Playing=false;Trace.Add("Boundary,4");}}
            return changed;
        }
    }
}
