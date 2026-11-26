using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FleetLab
{
    public sealed class PracticeMemberMissEffect
    {
        public int Slot {get;private set;}public int TargetFleet {get;private set;}
        public int X {get;private set;}public int Y {get;private set;}
        public int Counter {get;private set;}public int Type {get;private set;}public int SpriteBank {get;private set;}
        internal PracticeMemberMissEffect(byte[] pool,int slot)
        {
            int at=slot*20;Slot=slot;TargetFleet=BitConverter.ToInt16(pool,at);X=BitConverter.ToInt16(pool,at+2);Y=BitConverter.ToInt16(pool,at+4);
            Counter=BitConverter.ToInt16(pool,at+12);Type=BitConverter.ToInt32(pool,at+16);SpriteBank=BitConverter.ToInt32(pool,at+8);
        }
        public override string ToString(){return "Effect "+Slot+" · fleet "+TargetFleet+" · local "+X+","+Y+" · counter "+Counter;}
    }

    // Original568/569: four complete water-miss/effect histories on the exact
    // original551 F2 empty-pool endpoint. Remaining requests are deliberately
    // held: this is not an automatic firing/world loop. F3 is not admitted.
    public sealed class PracticeFleetRequestMisses
    {
        readonly PracticeFleetRequestLaunch launch;
        readonly PracticeUnitCatalog units;readonly PracticeDefenseTables tables;readonly byte[] visuals;
        readonly PracticeTimedImpactState state;
        public PracticeFleetRequestLaunch Launch {get{return launch;}}
        public int Mode {get;private set;}
        public uint Clock {get;private set;}
        public int Updates {get;private set;}
        public int ResolvedMisses {get;private set;}
        public int RandomCalls {get;private set;}
        public int LastStoppedAtRecord {get;private set;}
        public uint RandomState {get{return state.RandomState;}}
        public int MovieCounter {get{return state.MovieCounter;}}
        public bool Complete {get{return Projectiles.Count==0&&Effects.Count==0;}}
        public byte[] CopyShipPool(){return state.CopyShipPool();}
        public byte[] CopyFleetPool(){return state.CopyFleetPool();}
        public byte[] CopyTimedPool(){return state.CopyTimedPool();}
        public byte[] CopyEffectPool(){return state.CopyEffectPool();}
        public byte[] CopyScoreRegion(){return state.CopyScoreRegion();}
        public byte[] CopyOrderPool(){return launch.CopyOrderPool();}
        public ReadOnlyCollection<PracticeMemberProjectile> Projectiles
        {
            get{var list=new List<PracticeMemberProjectile>();var timed=state.CopyTimedPool();foreach(int slot in launch.CopyCreatedSlots())if(BitConverter.ToInt32(timed,slot*268)!=0)list.Add(new PracticeMemberProjectile(timed,slot));return list.AsReadOnly();}
        }
        public ReadOnlyCollection<PracticeMemberMissEffect> Effects
        {
            get{var list=new List<PracticeMemberMissEffect>();var effects=state.CopyEffectPool();for(int slot=0;slot<40;slot++)if(BitConverter.ToInt16(effects,slot*20)!=-1)list.Add(new PracticeMemberMissEffect(effects,slot));return list.AsReadOnly();}
        }
        PracticeFleetRequestMisses(PracticeFleetRequestLaunch launch,PracticeUnitCatalog units,PracticeDefenseTables tables,byte[] visuals,
            PracticeTimedImpactState state,int mode,uint clock,int updates,int misses,int calls,int stopped)
        {
            this.launch=launch;this.units=units;this.tables=tables;this.visuals=visuals;this.state=state;Mode=mode;Clock=clock;Updates=updates;
            ResolvedMisses=misses;RandomCalls=calls;LastStoppedAtRecord=stopped;
        }
        public static PracticeFleetRequestMisses Create(string programs,PracticeFleetRequestLaunch launch,int mode)
        {
            if(launch==null)throw new ArgumentNullException("launch");if(mode<0||mode>3)throw new ArgumentOutOfRangeException("mode");
            if(!launch.Executed||launch.Family!=0||launch.Capacity!=0||launch.CreatedObjects!=2)
                throw new NotSupportedException("The complete miss study requires the completed empty-pool F2 launch. F3 and capacity fixtures remain outside it.");
            var units=PracticeUnitCatalog.Load(programs);var tables=PracticeDefenseTables.Load(programs);var visuals=File.ReadAllBytes(Path.Combine(programs,"ClassicVisuals.seqbin"));
            var effects=PracticeVisualPool.Empty(visuals);var scores=new byte[176];BitConverter.GetBytes((short)-2).CopyTo(scores,172);
            var state=new PracticeTimedImpactState(units,tables,visuals,launch.CopyTimedPool(),launch.CopyShipPool(),launch.CopyFleetPool(),effects.CopyEffectPool(),scores,launch.RequestRandomState);
            return new PracticeFleetRequestMisses(launch,units,tables,visuals,state,mode,launch.Clock,0,0,0,-1);
        }
        public PracticeFleetRequestMisses Step()
        {
            if(Complete)throw new InvalidOperationException("The copied projectile/effect history is complete. Reset to repeat.");
            uint clock=Updates==0&&Mode!=0?(Mode==1?16345u:Mode==2?16385u:17245u):Clock+40u;
            var update=state.Advance(clock,40);
            var effects=new PracticeVisualPool(update.State.CopyEffectPool(),visuals).Advance();
            var after=new PracticeTimedImpactState(units,tables,visuals,update.State.CopyTimedPool(),update.State.CopyShipPool(),update.State.CopyFleetPool(),effects.CopyEffectPool(),update.State.CopyScoreRegion(),update.State.RandomState,null,update.State.MovieCounter);
            return new PracticeFleetRequestMisses(launch,units,tables,visuals,after,Mode,clock,Updates+1,
                ResolvedMisses+update.RetiredRecords,RandomCalls+update.RandomCalls,update.StoppedAtRecord);
        }
        public PracticeFleetRequestMisses UpTo(int maximum)
        {
            if(maximum<1||maximum>128)throw new ArgumentOutOfRangeException("maximum");if(Complete)throw new InvalidOperationException("This copied history is complete.");
            var next=this;for(int n=0;n<maximum&&!next.Complete;n++)next=next.Step();return next;
        }
        public PracticeFleetRequestMisses NextTransition()
        {
            if(Complete)throw new InvalidOperationException("This copied history is complete.");int projectiles=Projectiles.Count,effects=Effects.Count;var next=this;
            for(int n=0;n<1024;n++){next=next.Step();if(next.Projectiles.Count!=projectiles||next.Effects.Count!=effects||next.Complete)return next;}
            throw new NotSupportedException("No transition within the bounded copied miss history.");
        }
        public PracticeFleetRequestMisses Finish()
        {
            if(Complete)throw new InvalidOperationException("This copied history is complete.");var next=this;
            for(int n=0;n<1024&&!next.Complete;n++)next=next.Step();if(!next.Complete)throw new NotSupportedException("Copied miss/effect history exceeded its bound.");return next;
        }
    }
}
