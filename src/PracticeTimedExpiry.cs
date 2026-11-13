using System;
using System.IO;

namespace FleetLab {
    public sealed class PracticeTimedExpiryUpdate {
        public PracticeTimedExpiryState State { get; private set; }
        public int InterpolatedRecords { get; private set; }
        public int ExpiredRecords { get; private set; }
        public int DroppedEffects { get; private set; }
        public int StoppedAtRecord { get; private set; }
        public int RandomCalls { get; private set; }
        internal PracticeTimedExpiryUpdate(PracticeTimedExpiryState state,int moved,int expired,int dropped,int stopped,int calls) {
            State=state;InterpolatedRecords=moved;ExpiredRecords=expired;DroppedEffects=dropped;StoppedAtRecord=stopped;RandomCalls=calls;
        }
    }
    // Complete416C40 caller composition for future objects and admitted shared
    // non-ship expiry callbacks only. Source RIP B9A8...6A569; network0,
    // local UI/media excluded. Visual-counter stepping40E0D0 is separate.
    // The extended manual study uses this caller and guards other callbacks.
    public sealed class PracticeTimedExpiryState {
        readonly byte[] timed,fleets,effects,visuals;
        public uint RandomState { get; private set; }
        public PracticeTimedExpiryState(byte[] records,byte[] fleetPool,byte[] effectPool,byte[] visualRecords,uint randomState) {
            if(records==null||records.Length!=21440||fleetPool==null||fleetPool.Length!=123000||effectPool==null||effectPool.Length!=800||visualRecords==null||visualRecords.Length!=580)
                throw new InvalidDataException("Timed expiry requires complete original pools.");
            timed=(byte[])records.Clone();fleets=(byte[])fleetPool.Clone();effects=(byte[])effectPool.Clone();visuals=(byte[])visualRecords.Clone();RandomState=randomState;
        }
        static int Word(byte[] b,int at){return BitConverter.ToInt16(b,at);}
        static void Word(byte[] b,int at,int value){BitConverter.GetBytes(unchecked((short)value)).CopyTo(b,at);}
        public PracticeTimedExpiryUpdate Advance(uint gameMilliseconds,uint priorDeltaMilliseconds) {
            for(int at=0;at<timed.Length;at+=268)if(BitConverter.ToInt32(timed,at)!=0){
                if(priorDeltaMilliseconds==0||priorDeltaMilliseconds>int.MaxValue)throw new NotSupportedException("Timed expiry requires a positive signed source delta.");
                int target=Word(timed,at+12);if(target<0||target>=250)throw new InvalidDataException("Timed target outside fleet pool.");
            }
            byte[] nextTimed=CopyTimedPool(),nextFleets=CopyFleetPool(),nextEffects=CopyEffectPool();uint random=RandomState;
            for(int at=483;at<nextFleets.Length;at+=492)nextFleets[at]=0;
            int moved=0,expired=0,dropped=0,stopped=-1,calls=0;
            for(int slot=0;slot<80;slot++){
                int at=slot*268;if(BitConverter.ToInt32(nextTimed,at)==0)continue;
                int remaining=unchecked((int)(BitConverter.ToUInt32(nextTimed,at+24)-gameMilliseconds));
                int updates=remaining/(int)priorDeltaMilliseconds;
                // Original computes the quotient BEFORE its expiry callback.
                if(remaining<=0){var impact=PracticeMissImpact.Resolve(nextTimed,nextFleets,nextEffects,visuals,slot,random);nextTimed=impact.CopyTimedPool();nextEffects=impact.CopyEffectPool();random=impact.RandomState;calls+=impact.RandomCalls;expired++;if(impact.EffectSlot<0)dropped++;}
                if(updates==0){stopped=slot;break;}
                // The source does not reread the active-kind word after expiry.
                // A negative quotient therefore changes the retired record's
                // coordinates too. Preserve these bytes for exact comparisons.
                int target=Word(nextTimed,at+12),fleet=target*492;
                int marked=BitConverter.ToInt32(nextFleets,fleet)==2?target&0xFFC:target;nextFleets[marked*492+483]=1;
                int x=Word(nextTimed,at+8),y=Word(nextTimed,at+10);
                int dx=Word(nextFleets,fleet+12)-x+Word(nextTimed,at+18),dy=Word(nextFleets,fleet+14)-y+Word(nextTimed,at+20);
                Word(nextTimed,at+8,x+PracticeTimedMotionState.Component(dx,updates));Word(nextTimed,at+10,y+PracticeTimedMotionState.Component(dy,updates));moved++;
            }
            return new PracticeTimedExpiryUpdate(new PracticeTimedExpiryState(nextTimed,nextFleets,nextEffects,visuals,random),moved,expired,dropped,stopped,calls);
        }
        public byte[] CopyTimedPool(){return (byte[])timed.Clone();}
        public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
        public byte[] CopyEffectPool(){return (byte[])effects.Clone();}
    }
}
