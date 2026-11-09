using System;using System.IO;using System.Collections.Generic;
namespace FleetLab {
    // Deliberately bounded reader for the two exact ending programs, not a VM.
    public sealed class EndingProgram {
        public readonly string Movie,Card;public readonly int HoldMilliseconds;public readonly uint MenuPointer;
        EndingProgram(string movie,string card,int hold,uint menu){Movie=movie;Card=card;HoldMilliseconds=hold;MenuPointer=menu;}
        public static EndingProgram Read(byte[] bytes){
            if(bytes==null||bytes.Length!=64)throw new InvalidDataException("Ending program must contain16native words");
            uint[] w=new uint[16];for(int i=0;i<16;i++)w[i]=BitConverter.ToUInt32(bytes,i*4);
            int[] positions={0,2,3,4,6,7,8,10,11,12,14,15};uint[] expected={8,9,15,5,14,1,6,14,1,18,4,0};
            for(int i=0;i<positions.Length;i++)if(w[positions[i]]!=expected[i])throw new InvalidDataException("Unsupported ending instruction at byte "+(positions[i]*4));
            if(w[5]!=5000||w[13]!=0x44ef88)throw new InvalidDataException("Unsupported ending wait/menu boundary");
            if(w[9]==139&&w[1]==0x44ee64)return new EndingProgram("VICTORY","E_SUCCED.PCX",5000,w[13]);
            if(w[9]==140&&w[1]==0x44ee5c)return new EndingProgram("LOSE","E_FAIL.PCX",5000,w[13]);
            throw new InvalidDataException("Unresolved ending resource references");
        }
    }
    public enum EndingPhase {Idle,Reveal,Hold,CardFade,Movie,MovieFade,Complete}
    public sealed class EndingTimeline {
        // Modern explicit cadence. Native factor order is known; native display
        // cadence depends on the original outer loop and has not been measured.
        public const int FadeStepMilliseconds=40;
        public EndingPhase Phase {get;private set;}public int Factor {get;private set;}
        public bool Playing {get;private set;}public int RemainingHold {get;private set;}
        long elapsed;readonly EndingProgram program;
        public readonly List<string> Trace=new List<string>();
        public EndingTimeline(EndingProgram value){program=value??throwProgram();Reset();}
        static EndingProgram throwProgram(){throw new ArgumentNullException("program");}
        public void Reset(){Phase=EndingPhase.Idle;Playing=false;Factor=64;elapsed=0;RemainingHold=program.HoldMilliseconds;Trace.Clear();}
        public void Start(){Reset();Playing=true;Trace.Add("Card,"+(program.Movie=="VICTORY"?"e_succed":"e_fail"));Enter(EndingPhase.Reveal);}
        void Enter(EndingPhase phase){Phase=phase;elapsed=0;Factor=phase==EndingPhase.Reveal?12:64;
            if(phase==EndingPhase.Reveal||phase==EndingPhase.CardFade||phase==EndingPhase.MovieFade)Trace.Add("Fade,"+Factor);
            if(phase==EndingPhase.Hold){RemainingHold=program.HoldMilliseconds;Trace.Add("Wait,"+RemainingHold);}
            if(phase==EndingPhase.Movie){Trace.Add("Clear,0");Trace.Add("Movie,"+(program.Movie=="VICTORY"?139:140));}
            if(phase==EndingPhase.Complete){Trace.Add("Clear,0");Trace.Add("Boundary,18");Playing=false;Factor=0;}
        }
        public void Pause(){Playing=false;}public void Resume(){if(Phase!=EndingPhase.Idle&&Phase!=EndingPhase.Complete)Playing=true;}
        public bool Advance(long milliseconds){
            if(milliseconds<0)throw new ArgumentOutOfRangeException("milliseconds");if(!Playing||Phase==EndingPhase.Movie)return false;bool changed=false;
            while(milliseconds>0&&Playing&&Phase!=EndingPhase.Movie){
                if(Phase==EndingPhase.Hold){long used=Math.Min(milliseconds,RemainingHold);RemainingHold-=(int)used;milliseconds-=used;changed=true;if(RemainingHold==0)Enter(EndingPhase.CardFade);continue;}
                long step=Math.Min(milliseconds,FadeStepMilliseconds-elapsed);elapsed+=step;milliseconds-=step;if(elapsed<FadeStepMilliseconds)break;elapsed=0;changed=true;
                if(Phase==EndingPhase.Reveal){if(Factor==64)Enter(EndingPhase.Hold);else{Factor+=4;Trace.Add("Fade,"+Factor);}}
                else if(Phase==EndingPhase.CardFade||Phase==EndingPhase.MovieFade){if(Factor==0)Enter(Phase==EndingPhase.CardFade?EndingPhase.Movie:EndingPhase.Complete);else{Factor-=4;Trace.Add("Fade,"+Factor);}}
                else break;
            }return changed;
        }
        public bool CompleteMovie(){if(Phase!=EndingPhase.Movie||!Playing)return false;Enter(EndingPhase.MovieFade);return true;}
        public void SkipStage(){if(!Playing)return;if(Phase==EndingPhase.Movie){CompleteMovie();return;}if(Phase==EndingPhase.Reveal)Enter(EndingPhase.Hold);else if(Phase==EndingPhase.Hold)Enter(EndingPhase.CardFade);else if(Phase==EndingPhase.CardFade)Enter(EndingPhase.Movie);else if(Phase==EndingPhase.MovieFade)Enter(EndingPhase.Complete);}
    }
}
