using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace FleetLab
{
    // Original625-r2: two water cells, six complete histories/216 observations.
    // This bounded copy starts at617's last future visit. It does not run new firing.
    public sealed class PracticeProducedMisses
    {
        readonly PracticeProducedFlight boundary;
        readonly PracticeUnitCatalog units;readonly PracticeDefenseTables tables;readonly byte[] visuals;
        readonly PracticeTimedImpactState state;
        public PracticeProducedLaunch Launch {get{return boundary.Launch;}}
        public PracticeProducedFlight Boundary {get{return boundary;}}
        public int Mode {get;private set;}public uint Clock {get;private set;}public int Updates {get;private set;}
        public int ResolvedMisses {get;private set;}public int RandomCalls {get;private set;}
        public int LastStoppedAtRecord {get;private set;}
        public uint RandomState {get{return state.RandomState;}}public int MovieCounter {get{return state.MovieCounter;}}
        public bool Complete {get{return Projectiles.Count==0&&Effects.Count==0;}}
        public byte[] CopyShipPool(){return state.CopyShipPool();}public byte[] CopyFleetPool(){return state.CopyFleetPool();}
        public byte[] CopyTimedPool(){return state.CopyTimedPool();}public byte[] CopyEffectPool(){return state.CopyEffectPool();}
        public byte[] CopyScoreRegion(){return state.CopyScoreRegion();}public byte[] CopyOrderPool(){return boundary.CopyOrderPool();}
        public ReadOnlyCollection<PracticeMemberProjectile> Projectiles
        {get{var list=new List<PracticeMemberProjectile>();var timed=state.CopyTimedPool();foreach(int slot in Launch.CopyCreatedSlots())if(BitConverter.ToInt32(timed,slot*268)!=0)list.Add(new PracticeMemberProjectile(timed,slot));return list.AsReadOnly();}}
        public ReadOnlyCollection<PracticeMemberMissEffect> Effects
        {get{var list=new List<PracticeMemberMissEffect>();var e=state.CopyEffectPool();for(int n=0;n<40;n++)if(BitConverter.ToInt16(e,n*20)!=-1)list.Add(new PracticeMemberMissEffect(e,n));return list.AsReadOnly();}}
        PracticeProducedMisses(PracticeProducedFlight boundary,PracticeUnitCatalog units,PracticeDefenseTables tables,byte[] visuals,
            PracticeTimedImpactState state,int mode,uint clock,int updates,int misses,int calls,int stopped)
        {this.boundary=boundary;this.units=units;this.tables=tables;this.visuals=visuals;this.state=state;Mode=mode;Clock=clock;Updates=updates;ResolvedMisses=misses;RandomCalls=calls;LastStoppedAtRecord=stopped;}
        public static PracticeProducedMisses Create(string programs,PracticeProducedFlight boundary,int mode)
        {
            if(boundary==null)throw new ArgumentNullException("boundary");if(mode<0||mode>2)throw new ArgumentOutOfRangeException("mode");
            if(!boundary.AtBoundary||boundary.Clock!=35210||boundary.Projectiles.Count!=2||boundary.Projectiles[0].Weapon!=22||boundary.Projectiles[1].Weapon!=22)
                throw new NotSupportedException("Water impacts require the completed original-map weapon22 pre-impact boundary at35210.");
            var f=boundary.CopyFleetPool();if(BitConverter.ToInt32(f,15*492+64+51*4)!=0||BitConverter.ToInt32(f,15*492+64+54*4)!=0)throw new NotSupportedException("Both copied target cells must remain water.");
            var units=PracticeUnitCatalog.Load(programs);var tables=PracticeDefenseTables.Load(programs);var visuals=File.ReadAllBytes(Path.Combine(programs,"ClassicVisuals.seqbin"));
            var effects=PracticeVisualPool.Empty(visuals);var scores=new byte[176];BitConverter.GetBytes((short)-2).CopyTo(scores,172);
            var state=new PracticeTimedImpactState(units,tables,visuals,boundary.CopyTimedPool(),boundary.CopyShipPool(),f,effects.CopyEffectPool(),scores,boundary.Launch.RandomState);
            return new PracticeProducedMisses(boundary,units,tables,visuals,state,mode,boundary.Clock,0,0,0,-1);
        }
        public PracticeProducedMisses Step()
        {
            if(Complete)throw new InvalidOperationException("The two water misses and effects are complete. Reset to repeat.");
            uint clock=Updates==0?(Mode==0?35211u:Mode==1?35225u:35250u):Clock+40u;
            var update=state.Advance(clock,40);var effects=new PracticeVisualPool(update.State.CopyEffectPool(),visuals).Advance();
            var after=new PracticeTimedImpactState(units,tables,visuals,update.State.CopyTimedPool(),update.State.CopyShipPool(),update.State.CopyFleetPool(),effects.CopyEffectPool(),update.State.CopyScoreRegion(),update.State.RandomState,null,update.State.MovieCounter);
            return new PracticeProducedMisses(boundary,units,tables,visuals,after,Mode,clock,Updates+1,ResolvedMisses+update.RetiredRecords,RandomCalls+update.RandomCalls,update.StoppedAtRecord);
        }
        public PracticeProducedMisses UpTo(int maximum)
        {if(maximum<1||maximum>128)throw new ArgumentOutOfRangeException("maximum");if(Complete)throw new InvalidOperationException("Copied history complete.");var next=this;for(int n=0;n<maximum&&!next.Complete;n++)next=next.Step();return next;}
        public PracticeProducedMisses NextTransition()
        {if(Complete)throw new InvalidOperationException("Copied history complete.");int p=Projectiles.Count,e=Effects.Count;var next=this;for(int n=0;n<128;n++){next=next.Step();if(next.Projectiles.Count!=p||next.Effects.Count!=e)return next;}throw new NotSupportedException("No transition within128 copied updates.");}
        public PracticeProducedMisses Finish()
        {if(Complete)throw new InvalidOperationException("Copied history complete.");var next=UpTo(128);if(!next.Complete)throw new NotSupportedException("Copied history exceeded128 updates.");return next;}
    }
}

