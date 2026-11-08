using System;
using System.IO;
using System.Drawing;
namespace FleetLab {
    public enum OpponentKind { Original,Prototype }
    public sealed class NativeRandom {
        public uint State { get; private set; }
        public int Calls { get; private set; }
        public NativeRandom(uint seed) { State=seed; }
        // Bs.exe 0x439790: exact 32-bit wraparound and 15-bit result.
        public int Next() {
            State=unchecked(State*214013u+2531011u);
            Calls++;
            return (int)((State>>16)&32767);
        }
    }
    public sealed class TargetDecision {
        public Point Cell;
        public string Reason;
        public int Attempts,RandomCalls;
        public bool Stalled;
    }
    // Only targeting is reconstructed: Bs.exe B9A8...A569, 0x41C940..0x41CC5D.
    // Weapon/shooter selection and the original shared RNG schedule are separate.
    public static class OriginalOpponent {
        static readonly int[] Budgets={10,50,100,200};
        public static int Budget(int level) {
            if(level<0||level>3)throw new ArgumentOutOfRangeException("level");
            return Budgets[level];
        }
        public static int[,] Mask(int[,] raw) {
            if(raw==null||raw.GetLength(0)!=10||raw.GetLength(1)!=10)throw new InvalidDataException("Opponent requires a 10x10 observation grid");
            var observed=new int[10,10];
            for(int x=0;x<10;x++)for(int y=0;y<10;y++) {
                int value=raw[x,y];
                observed[x,y]=value==0||value==2||value==4?0:value;
            }
            return observed;
        }
        public static TargetDecision Choose(int[,] raw,int level,NativeRandom random) {
            if(random==null)throw new ArgumentNullException("random");
            int budget=Budget(level),before=random.Calls;
            int[,] seen=Mask(raw);
            for(int x=0;x<10;x++)for(int y=0;y<10;y++) {
                if(seen[x,y]!=8)continue;
                if(y>0&&seen[x,y-1]==8) {
                    if(y<9&&seen[x,y+1]==0)return Decision(x,y+1,"extend hit line",0,random.Calls-before);
                    continue;
                }
                if(y<9&&seen[x,y+1]==8) {
                    if(y>0&&seen[x,y-1]==0)return Decision(x,y-1,"extend hit line",0,random.Calls-before);
                    continue;
                }
                // Original retries indefinitely if an isolated hit has no open
                // Y neighbor. Fail visibly instead of hanging the modern UI.
                bool lower=y>0&&seen[x,y-1]==0,higher=y<9&&seen[x,y+1]==0;
                if(!lower&&!higher)return Stall("isolated hit has no open neighbor",random.Calls-before);
                for(int attempt=0;attempt<4096;attempt++) {
                    int direction=random.Next()%2;
                    if(direction==0&&lower)return Decision(x,y-1,"pursue single hit",attempt+1,random.Calls-before);
                    if(direction==1&&higher)return Decision(x,y+1,"pursue single hit",attempt+1,random.Calls-before);
                }
                return Stall("direction retry limit exceeded",random.Calls-before);
            }
            Point candidate=new Point();
            for(int attempt=0;attempt<budget;attempt++) {
                int x=random.Next()%10,y=random.Next()%8+1;
                candidate=new Point(x,y);
                if(seen[x,y]!=0)continue;
                int pattern=random.Next()%6;
                bool accept=false;
                switch(pattern) {
                    case 0:accept=y>2&&seen[x,y-1]==0&&seen[x,y-2]==1;break;
                    case 1:accept=y<8&&seen[x,y+1]==0&&seen[x,y+2]==1;break;
                    case 2:accept=x>0&&y>0&&seen[x-1,y-1]==1;break;
                    case 3:accept=x>0&&y<9&&seen[x-1,y+1]==1;break;
                    case 4:accept=x<9&&y>0&&seen[x+1,y-1]==1;break;
                    case 5:accept=x<9&&y<9&&seen[x+1,y+1]==1;break;
                }
                if(accept)return Decision(x,y,"search pattern",attempt+1,random.Calls-before);
            }
            // Native exhaustion returns its last sampled coordinate even if it
            // was already resolved. Do not silently replace it with a better AI.
            return Decision(candidate.X,candidate.Y,"search budget exhausted",budget,random.Calls-before);
        }
        static TargetDecision Decision(int x,int y,string reason,int attempts,int calls) {
            return new TargetDecision {Cell=new Point(x,y),Reason=reason,Attempts=attempts,RandomCalls=calls};
        }
        static TargetDecision Stall(string reason,int calls) {
            return new TargetDecision {Stalled=true,Reason=reason,RandomCalls=calls};
        }
    }
}
