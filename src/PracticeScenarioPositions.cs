using System;using System.Collections.Generic;using System.Collections.ObjectModel;using System.IO;
namespace FleetLab
{
    public sealed class PracticeScenarioPositionAttempt
    {
        public int X{get;private set;}public int Y{get;private set;}
        public int BlockingFleet{get;private set;}public int BlockingTerrain{get;private set;}
        public bool Accepted{get{return BlockingFleet<0&&BlockingTerrain<0;}}
        internal PracticeScenarioPositionAttempt(int x,int y,int fleet,int terrain){X=x;Y=y;BlockingFleet=fleet;BlockingTerrain=terrain;}
    }
    public sealed class PracticeScenarioPositionResult
    {
        public int X{get;private set;}public int Y{get;private set;}public uint RandomState{get;private set;}public int RandomCalls{get;private set;}
        public ReadOnlyCollection<PracticeScenarioPositionAttempt> Attempts{get;private set;}
        internal PracticeScenarioPositionResult(List<PracticeScenarioPositionAttempt> attempts,NativeRandom random){Attempts=attempts.AsReadOnly();var last=attempts[attempts.Count-1];X=last.X;Y=last.Y;RandomState=random.State;RandomCalls=random.Calls;}
    }
    // Bs.exe B9A8...6A569, whole00406060/real00439790. Original1047R3 /
    // independent1048R2 / fresh1050:2448 positions and complete RNG states.
    // Only defined dispatches are admitted. The finite retry limit is a modern
    // diagnostic guard; the original retries indefinitely. No constructor runs.
    public static class PracticeScenarioPositions
    {
        public static PracticeScenarioPositionResult Select(int scenario,int group,uint seed,byte[] primary,byte[] terrain,int maximumAttempts)
        {
            if(primary==null||primary.Length!=123000||terrain==null||terrain.Length!=246000)throw new InvalidDataException("Position selection requires complete 250/500 record pools.");
            if(group<0||group>3)throw new ArgumentOutOfRangeException("group");
            if(!(scenario==5||scenario==6||scenario==7||scenario==8&&group<2||scenario>=11&&scenario<=14||scenario==18))throw new NotSupportedException("This scenario/group can retain an uninitialized source position; no placement is invented.");
            if(maximumAttempts<1||maximumAttempts>4096)throw new ArgumentOutOfRangeException("maximumAttempts");
            var fleets=(byte[])primary.Clone();var objects=(byte[])terrain.Clone();
            if(scenario==18){bool eligible=false;for(int at=0;at<fleets.Length;at+=492)if(EligibleBase(fleets,at)){eligible=true;break;}if(!eligible)throw new NotSupportedException("Scenario 18 requires an eligible owner-1 base with orientation 1..4.");}
            var random=new NativeRandom(seed);var attempts=new List<PracticeScenarioPositionAttempt>();
            for(int visit=0;visit<maximumAttempts;visit++)
            {
                int x,y;Candidate(scenario,group,random,fleets,out x,out y);int blockedFleet=-1,blockedTerrain=-1;
                for(int at=0;at<fleets.Length;at+=492)if(BitConverter.ToInt32(fleets,at)!=0&&Math.Abs(BitConverter.ToInt16(fleets,at+12)-x)<10&&Math.Abs(BitConverter.ToInt16(fleets,at+14)-y)<10){blockedFleet=at/492;break;}
                if(blockedFleet<0)for(int at=0;at<objects.Length;at+=492)if(BitConverter.ToInt32(objects,at)==1&&BitConverter.ToInt16(objects,at+12)==x&&BitConverter.ToInt16(objects,at+14)==y){blockedTerrain=at/492;break;}
                var attempt=new PracticeScenarioPositionAttempt(x,y,blockedFleet,blockedTerrain);attempts.Add(attempt);if(attempt.Accepted)return new PracticeScenarioPositionResult(attempts,random);
            }
            throw new InvalidDataException("Position search reached the configured diagnostic retry limit; original spawning has not been completed.");
        }
        static bool EligibleBase(byte[] fleets,int at){return BitConverter.ToInt32(fleets,at)==2&&BitConverter.ToInt16(fleets,at+6)==1&&fleets[at+484]>=1&&fleets[at+484]<=4;}
        static void Candidate(int scenario,int group,NativeRandom random,byte[] fleets,out int x,out int y)
        {
            int r,p;switch(scenario)
            {
                case 5:r=random.Next()&31;x=group==1||group==2?630:0;y=r*10+(group>=2?320:0);return;
                case 6:if(group==0){x=random.Next()%63*10;y=630;return;}goto case 12;
                case 7:r=random.Next()&1;if(r==0){x=0;y=320+(random.Next()&31)*10;}else{x=(random.Next()&31)*10;y=630;}return;
                case 8:if(group==0){x=320+(random.Next()&31)*10;y=0;}else{x=random.Next()%63*10;y=630;}return;
                case 11:x=random.Next()%63*10;y=group==0?0:630;return;
                case 12:case 13:case 14:r=random.Next()&3;p=random.Next()%63*10;x=r==0?0:r==2?630:p;y=r==1?0:r==3?630:p;return;
                default:
                    x=y=0;for(int at=0;at<fleets.Length;at+=492)if(EligibleBase(fleets,at)){r=random.Next()&7;switch(fleets[at+484]){case 1:x=40+r*10;y=0;break;case 2:x=160+r*10;y=630;break;case 3:x=630;y=370+r*10;break;default:x=220+r*10;y=630;break;}}return;
            }
        }
    }
    internal sealed class PracticeScenarioPositionCopy
    {
        internal readonly byte[] Fleets=new byte[123000],Terrain=new byte[246000];
        internal readonly int Scenario,Group,Collision;internal readonly uint Seed;
        internal PracticeScenarioPositionResult Result{get;private set;}
        internal PracticeScenarioPositionCopy(int scenario,int group,uint seed,int collision)
        {
            if(collision<0||collision>8)throw new ArgumentOutOfRangeException("collision");Scenario=scenario;Group=group;Seed=seed;Collision=collision;
            if(scenario==18){Fleet(0,2,1,320,320,group+1);Fleet(5,2,1,330,330,(group+1)%4+1);Fleet(248,2,0,340,340,1);Fleet(249,2,1,350,350,0);}
            var first=PracticeScenarioPositions.Select(scenario,group,seed,Fleets,Terrain,128);int x=first.X,y=first.Y;
            if(collision==1)Fleet(20,1,0,x,y,0);if(collision==2)Fleet(20,1,0,x+9,y+9,0);if(collision==3)Fleet(20,1,0,x+10,y+9,0);
            if(collision==4)Object(499,1,x,y);if(collision==5)Object(499,1,x+1,y);if(collision==6)Fleet(20,0,0,x,y,0);if(collision==7)Object(499,2,x,y);
            if(collision==8)for(int n=0;n<3;n++){var next=PracticeScenarioPositions.Select(scenario,group,seed,Fleets,Terrain,128);Fleet(20+n,1,0,next.X,next.Y,0);}
            Result=PracticeScenarioPositions.Select(scenario,group,seed,Fleets,Terrain,128);
        }
        void Fleet(int slot,int kind,int owner,int x,int y,int orientation){int at=slot*492;BitConverter.GetBytes(kind).CopyTo(Fleets,at);BitConverter.GetBytes((short)owner).CopyTo(Fleets,at+6);BitConverter.GetBytes((short)x).CopyTo(Fleets,at+12);BitConverter.GetBytes((short)y).CopyTo(Fleets,at+14);Fleets[at+484]=(byte)orientation;}
        void Object(int slot,int kind,int x,int y){int at=slot*492;BitConverter.GetBytes(kind).CopyTo(Terrain,at);BitConverter.GetBytes((short)x).CopyTo(Terrain,at+12);BitConverter.GetBytes((short)y).CopyTo(Terrain,at+14);}
    }
}
