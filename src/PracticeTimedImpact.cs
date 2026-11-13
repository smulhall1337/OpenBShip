using System;
using System.IO;

namespace FleetLab {
    public sealed class PracticeTimedImpactUpdate {
        public PracticeTimedImpactState State { get; private set; }
        public int InterpolatedRecords { get; private set; }
        public int ExpiryCallbacks { get; private set; }
        public int RetiredRecords { get; private set; }
        public int DroppedEffects { get; private set; }
        public int StoppedAtRecord { get; private set; }
        public int RandomCalls { get; private set; }
        public PracticeImpactOutcome? LastImpactOutcome { get; private set; }
        public int LastVictimShip { get; private set; }
        public int LastPoints { get; private set; }
        public int LastScatterAttempts { get; private set; }
        internal PracticeTimedImpactUpdate(PracticeTimedImpactState state,int moved,int callbacks,int retired,int dropped,int stopped,int calls,PracticeImpactResult last) {
            State=state;InterpolatedRecords=moved;ExpiryCallbacks=callbacks;RetiredRecords=retired;DroppedEffects=dropped;StoppedAtRecord=stopped;RandomCalls=calls;
            LastImpactOutcome=last==null?(PracticeImpactOutcome?)null:last.Outcome;LastVictimShip=last==null?-1:last.VictimShip;LastPoints=last==null?0:last.Points;LastScatterAttempts=last==null?0:last.ScatterAttempts;
        }
    }

    // Complete416C40 with the admitted ordinary/nonlethal416DD0 callbacks.
    // Source RIP B9A8...6A569. This is a data pass, not the whole world/frame:
    // visual counters, paused deadlines, firing admission, AI and media remain
    // separate. A later unsupported callback rejects the entire result.
    public sealed class PracticeTimedImpactState {
        readonly PracticeUnitCatalog units;
        readonly PracticeDefenseTables tables;
        readonly byte[] visuals,timed,ships,fleets,effects,scores;
        public uint RandomState { get; private set; }
        public PracticeTimedImpactState(PracticeUnitCatalog unitCatalog,PracticeDefenseTables sourceTables,byte[] visualRecords,
                byte[] timedPool,byte[] shipPool,byte[] fleetPool,byte[] effectPool,byte[] scoreRegion,uint randomState) {
            if(unitCatalog==null)throw new ArgumentNullException("unitCatalog");if(sourceTables==null)throw new ArgumentNullException("sourceTables");
            if(timedPool==null||timedPool.Length!=21440||shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000||effectPool==null||effectPool.Length!=800||scoreRegion==null||scoreRegion.Length!=176)
                throw new InvalidDataException("Timed impact stepping requires complete original pools and score region.");
            new PracticeVisualPool(effectPool,visualRecords);
            units=unitCatalog;tables=sourceTables;visuals=(byte[])visualRecords.Clone();timed=(byte[])timedPool.Clone();ships=(byte[])shipPool.Clone();fleets=(byte[])fleetPool.Clone();effects=(byte[])effectPool.Clone();scores=(byte[])scoreRegion.Clone();RandomState=randomState;
        }
        static int Word(byte[] b,int at){return BitConverter.ToInt16(b,at);}
        static void Word(byte[] b,int at,int value){BitConverter.GetBytes(unchecked((short)value)).CopyTo(b,at);}
        public PracticeTimedImpactUpdate Advance(uint gameMilliseconds,uint priorDeltaMilliseconds) {
            for(int at=0;at<timed.Length;at+=268)if(BitConverter.ToInt32(timed,at)!=0){
                if(priorDeltaMilliseconds==0||priorDeltaMilliseconds>int.MaxValue)throw new NotSupportedException("Timed impact stepping requires a positive signed source delta.");
                int target=Word(timed,at+12);if(target<0||target>=250)throw new InvalidDataException("Timed target outside fleet pool.");
            }
            byte[] t=CopyTimedPool(),s=CopyShipPool(),f=CopyFleetPool(),e=CopyEffectPool(),score=CopyScoreRegion();uint random=RandomState;
            for(int at=483;at<f.Length;at+=492)f[at]=0;
            int moved=0,callbacks=0,retired=0,dropped=0,stopped=-1,calls=0;PracticeImpactResult last=null;
            for(int slot=0;slot<80;slot++){
                int at=slot*268;if(BitConverter.ToInt32(t,at)==0)continue;
                int remaining=unchecked((int)(BitConverter.ToUInt32(t,at+24)-gameMilliseconds)),updates=remaining/(int)priorDeltaMilliseconds;
                if(remaining<=0){
                    last=PracticeImpact.Resolve(tables,units,visuals,t,s,f,e,score,slot,random);t=last.CopyTimedPool();s=last.CopyShipPool();f=last.CopyFleetPool();e=last.CopyEffectPool();score=last.CopyScoreRegion();random=last.RandomState;calls+=last.RandomCalls;callbacks++;if(last.Retired)retired++;if(last.EffectRequested&&last.EffectSlot<0)dropped++;
                }
                // Original callback precedes the whole-pass quotient-zero
                // return, and a negative quotient still writes retired coords.
                if(updates==0){stopped=slot;break;}
                int target=Word(t,at+12),fleet=target*492,marked=BitConverter.ToInt32(f,fleet)==2?target&0xFFC:target;f[marked*492+483]=1;
                int x=Word(t,at+8),y=Word(t,at+10),dx=Word(f,fleet+12)-x+Word(t,at+18),dy=Word(f,fleet+14)-y+Word(t,at+20);
                Word(t,at+8,x+PracticeTimedMotionState.Component(dx,updates));Word(t,at+10,y+PracticeTimedMotionState.Component(dy,updates));moved++;
            }
            var next=new PracticeTimedImpactState(units,tables,visuals,t,s,f,e,score,random);
            return new PracticeTimedImpactUpdate(next,moved,callbacks,retired,dropped,stopped,calls,last);
        }
        public byte[] CopyTimedPool(){return (byte[])timed.Clone();}
        public byte[] CopyShipPool(){return (byte[])ships.Clone();}
        public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
        public byte[] CopyEffectPool(){return (byte[])effects.Clone();}
        public byte[] CopyScoreRegion(){return (byte[])scores.Clone();}
    }
}
