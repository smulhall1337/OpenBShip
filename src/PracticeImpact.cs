using System;
using System.IO;

namespace FleetLab {
    public enum PracticeImpactOutcome { NonshipCell, PrimaryStopped, ScatteredMiss, Damage, PassiveCell, NoVictim }
    public sealed class PracticeImpactResult {
        readonly byte[] timed, ships, fleets, effects, scores;
        public PracticeImpactOutcome Outcome { get; private set; }
        public uint RandomState { get; private set; }
        public int RandomCalls { get; private set; }
        public int VictimShip { get; private set; }
        public int EffectSlot { get; private set; }
        public int EffectType { get; private set; }
        public int ScatterAttempts { get; private set; }
        public int Points { get; private set; }
        public bool Retired { get; private set; }
        public bool VictimDestroyed { get; private set; }
        // Meaningful for the optional destruction continuation; default callers
        // do not model the original movie-selection counter.
        public int MovieCounter { get; private set; }
        public bool EffectRequested { get { return Outcome == PracticeImpactOutcome.NonshipCell || Outcome == PracticeImpactOutcome.ScatteredMiss || VictimDestroyed; } }
        internal PracticeImpactResult(byte[] t,byte[] s,byte[] f,byte[] e,byte[] score,PracticeImpactOutcome outcome,
                uint random,int calls,int victim,int effectSlot,int effectType,int attempts,int points,bool retired,bool destroyed=false,int movieCounter=0) {
            timed=(byte[])t.Clone();ships=(byte[])s.Clone();fleets=(byte[])f.Clone();effects=(byte[])e.Clone();scores=(byte[])score.Clone();
            Outcome=outcome;RandomState=random;RandomCalls=calls;VictimShip=victim;EffectSlot=effectSlot;EffectType=effectType;ScatterAttempts=attempts;Points=points;Retired=retired;VictimDestroyed=destroyed;MovieCounter=movieCounter;
        }
        public byte[] CopyTimedPool(){return (byte[])timed.Clone();}
        public byte[] CopyShipPool(){return (byte[])ships.Clone();}
        public byte[] CopyFleetPool(){return (byte[])fleets.Clone();}
        public byte[] CopyEffectPool(){return (byte[])effects.Clone();}
        public byte[] CopyScoreRegion(){return (byte[])scores.Clone();}
    }

    // Supported RIP B9A8...6A569: complete416DD0 ordinary-projectile callback,
    // including first matching unit,418030,417740/417F50,417990 and420550.
    // Network0, local-owner mismatch/mode0. Default Resolve excludes fatal
    // damage; the explicit destruction continuation also admits retired-fleet
    // water, passive and no-victim cells. Media/attached/special19/21 and a
    // complete application loop remain separate.
    public static class PracticeImpact {
        static int Word(byte[] b,int at){return BitConverter.ToInt16(b,at);}
        static int Dword(byte[] b,int at){return BitConverter.ToInt32(b,at);}
        public static PracticeImpactResult Resolve(PracticeDefenseTables tables,PracticeUnitCatalog units,byte[] visualRecords,
                byte[] timedPool,byte[] shipPool,byte[] fleetPool,byte[] effectPool,byte[] scoreRegion,int timedSlot,uint randomState) {
            return ResolveCore(tables,units,visualRecords,timedPool,shipPool,fleetPool,effectPool,scoreRegion,timedSlot,randomState,null,0);
        }
        // Explicit mode0/network0/scenario-selector0/localowner-2/other selected fleet
        // continuation. The default API retains its nonlethal boundary.
        public static PracticeImpactResult ResolveWithDestruction(PracticeDefenseTables tables,PracticeUnitCatalog units,byte[] visualRecords,
                byte[] timedPool,byte[] shipPool,byte[] fleetPool,byte[] effectPool,byte[] scoreRegion,int timedSlot,uint randomState,
                PracticeDestructionTables destruction,int movieCounter) {
            if(destruction==null)throw new ArgumentNullException("destruction");
            if(movieCounter<short.MinValue||movieCounter>short.MaxValue)throw new ArgumentOutOfRangeException("movieCounter");
            return ResolveCore(tables,units,visualRecords,timedPool,shipPool,fleetPool,effectPool,scoreRegion,timedSlot,randomState,destruction,movieCounter);
        }
        static PracticeImpactResult ResolveCore(PracticeDefenseTables tables,PracticeUnitCatalog units,byte[] visualRecords,
                byte[] timedPool,byte[] shipPool,byte[] fleetPool,byte[] effectPool,byte[] scoreRegion,int timedSlot,uint randomState,
                PracticeDestructionTables destruction,int movieCounter) {
            if(tables==null)throw new ArgumentNullException("tables");if(units==null)throw new ArgumentNullException("units");
            if(timedPool==null||timedPool.Length!=21440||shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000||effectPool==null||effectPool.Length!=800||scoreRegion==null||scoreRegion.Length!=176)
                throw new InvalidDataException("Impact resolution requires complete original pools and score region.");
            // Also checks the source visual-table identity. Active record types
            // stay opaque here: the visual-counter pass is a separate function.
            new PracticeVisualPool(effectPool,visualRecords);
            if(timedSlot<0||timedSlot>=80)throw new ArgumentOutOfRangeException("timedSlot");
            int at=timedSlot*268,weapon=Dword(timedPool,at),target=Word(timedPool,at+12),x=Word(timedPool,at+18),y=Word(timedPool,at+20);
            if(weapon<14||weapon>30||weapon==19||weapon==21)throw new NotSupportedException("This projectile uses an unreconstructed impact branch.");
            if(target<0||target>=250||x<0||x>=10||y<0||y>=10)throw new InvalidDataException("Impact target is outside the admitted fleet grid.");
            int fleet=target*492,kind=Dword(fleetPool,fleet),cell=Dword(fleetPool,fleet+64+(x*10+y)*4);
            bool retiredFleet=destruction!=null&&kind==0&&cell>=0&&cell<=8;
            if(!retiredFleet&&kind!=1&&kind!=2&&kind!=9&&kind!=11)throw new NotSupportedException("Unsupported impact fleet kind.");
            if(destruction!=null&&((!retiredFleet&&kind!=1&&kind!=9)||Word(scoreRegion,172)!=-2))throw new NotSupportedException("Destructive impacts require fleet1/9 or supported retired-fleet cells and the explicit offscreen local-owner context.");
            if(cell==0||cell==1||cell==3){var miss=retiredFleet?PracticeMissImpact.ResolveRetired(timedPool,fleetPool,effectPool,visualRecords,timedSlot,randomState):PracticeMissImpact.Resolve(timedPool,fleetPool,effectPool,visualRecords,timedSlot,randomState);return new PracticeImpactResult(miss.CopyTimedPool(),shipPool,fleetPool,miss.CopyEffectPool(),scoreRegion,PracticeImpactOutcome.NonshipCell,miss.RandomState,miss.RandomCalls,-1,miss.EffectSlot,miss.EffectType,0,0,true,false,movieCounter);}
            byte[] timed=(byte[])timedPool.Clone();
            if(cell==8){BitConverter.GetBytes(0).CopyTo(timed,at);return new PracticeImpactResult(timed,shipPool,fleetPool,effectPool,scoreRegion,PracticeImpactOutcome.PassiveCell,randomState,0,-1,-1,-1,0,0,true,false,movieCounter);}
            if(cell!=2&&cell!=4&&cell!=5&&cell!=6&&cell!=7)throw new NotSupportedException("Unsupported impact target-cell code.");
            int victim=-1,victimIndex=-1;
            for(int slot=0;slot<6;slot++){
                uint reference=BitConverter.ToUInt32(fleetPool,fleet+16+slot*4);if(reference==0)continue;
                long delta=(long)reference-0x4FC340;if(delta<0||delta>=402000||delta%268!=0)throw new InvalidDataException("Fleet unit reference is outside the normalized original ship pool.");
                int unit=(int)delta;if(Dword(shipPool,unit)==0||Word(shipPool,unit+8)!=x||y<Word(shipPool,unit+10))continue;
                int length=Word(shipPool,unit+14);if(length<0||length>6)throw new InvalidDataException("Unsupported impact unit row extent.");
                if(y>=Word(shipPool,unit+10)+length)continue;victim=unit/268;victimIndex=slot;break;
            }
            if(victim<0)return new PracticeImpactResult(timedPool,shipPool,fleetPool,effectPool,scoreRegion,PracticeImpactOutcome.NoVictim,randomState,0,-1,-1,-1,0,0,false,false,movieCounter);
            if(retiredFleet)throw new NotSupportedException("An inactive fleet still references a matching live unit; its damage continuation is not reconstructed.");
            int victimAt=victim*268,type=Dword(shipPool,victimAt);
            if(type<1||type>4)throw new NotSupportedException("This matched unit has unreconstructed impact callbacks.");
            if(Word(shipPool,victimAt+12)!=target)throw new InvalidDataException("Matched unit belongs to another fleet.");
            var defense=PracticeDefense.Resolve(tables,timedPool,shipPool,fleetPool,timedSlot,victim,randomState);
            byte[] ships=defense.CopyShipPool();var random=new NativeRandom(defense.RandomState);int effectSlot=-1,effectType=-1,attempts=0,points=0;bool destroyed=false;byte[] fleets=fleetPool,effects=effectPool,scores=scoreRegion;PracticeImpactOutcome outcome;
            if(defense.Outcome==PracticeDefenseOutcome.PrimaryStopped){
                outcome=PracticeImpactOutcome.PrimaryStopped;
                if((weapon<19||weapon>24)&&PracticeMissImpact.MovieRandomCalls(weapon,kind)!=0)random.Next();
            }else if(defense.Outcome==PracticeDefenseOutcome.Miss){
                outcome=PracticeImpactOutcome.ScatteredMiss;int sx=-1,sy=-1;
                // Original retries without a bound. Keep failed modern calls
                // atomic and diagnose an exhausted/unsupported local search.
                for(;attempts<10000;){attempts++;int px=x+random.Next()%3-1,py=y+random.Next()%3-1;if(px<0||px>=10||py<0||py>=10)continue;int code=Dword(fleetPool,fleet+64+(px*10+py)*4);if(code!=0&&code!=1&&code!=3)continue;sx=px;sy=py;break;}
                if(sx<0)throw new NotSupportedException("Nearby miss search exceeded the maintained diagnostic limit.");
                effectType=kind==1||kind==9?27:28;effects=PracticeMissImpact.SpawnEffect(effectPool,visualRecords,effectType,target,sx,sy,out effectSlot);
                // Scatter coordinates and allocation precede movie choice here.
                if(PracticeMissImpact.MovieRandomCalls(weapon,kind)!=0)random.Next();
            }else{
                outcome=PracticeImpactOutcome.Damage;PracticeSegmentDamageResult damage;
                if(destruction==null)damage=PracticeDamage.Apply(tables,units,ships,fleetPool,scoreRegion,victim,x,y,weapon,Word(timedPool,at+6));
                else{var complete=PracticeDamageWithDestruction.Apply(tables,units,destruction,visualRecords,ships,fleetPool,scoreRegion,effectPool,victim,x,y,victimIndex,weapon,Word(timedPool,at+6),movieCounter);damage=complete.Damage;effects=complete.CopyEffectPool();movieCounter=complete.MovieCounter;destroyed=complete.Destroyed;effectSlot=complete.EffectSlot;if(destroyed)effectType=type;}
                ships=damage.CopyShipPool();fleets=damage.CopyFleetPool();scores=damage.CopyScoreRegion();points=damage.Points;
            }
            BitConverter.GetBytes(0).CopyTo(timed,at);
            return new PracticeImpactResult(timed,ships,fleets,effects,scores,outcome,random.State,defense.RandomCalls+random.Calls,victim,effectSlot,effectType,attempts,points,true,destroyed,movieCounter);
        }
    }
}
