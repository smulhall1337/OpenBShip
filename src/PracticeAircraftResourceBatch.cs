using System;
using System.IO;
using System.Collections.Generic;
namespace FleetLab
{
    public sealed class PracticeAircraftResourceBatchResult
    {
        readonly byte[] ships,fleets,scores,effects;
        public PracticeFleetScheduler NextSchedule
        {
            get;
            private set;
        }
        public int AircraftMembers
        {
            get;
            private set;
        }
        public int RetiredUnits
        {
            get;
            private set;
        }
        public int MovieCounter
        {
            get;
            private set;
        }
        public uint RandomState
        {
            get;
            private set;
        }
        internal PracticeAircraftResourceBatchResult(byte[] s,byte[] f,byte[] score,byte[] e,PracticeFleetScheduler next,int members,int retired,int movie,uint random)
        {
            ships=s;
            fleets=f;
            scores=score;
            effects=e;
            NextSchedule=next;
            AircraftMembers=members;
            RetiredUnits=retired;
            MovieCounter=movie;
            RandomState=random;
        }
        public byte[] CopyShipPool()
        {
            return (byte[])ships.Clone();
        }
        public byte[] CopyFleetPool()
        {
            return (byte[])fleets.Clone();
        }
        public byte[] CopyScoreRegion()
        {
            return (byte[])scores.Clone();
        }
        public byte[] CopyEffectPool()
        {
            return (byte[])effects.Clone();
        }
    }
    // Pinned RIP B9A8...6A569 whole427670 comparison: idle aircraft resource96
    // consumption/expiry with source cursor semantics. No waypoint movement,
    // automatic return/landing, attack tasks, UI/media or frame pacing claim.
    public static class PracticeAircraftResourceBatch
    {
        static int W(byte[] b,int p)
        {
            return BitConverter.ToInt16(b,p);
        }
        static int I(byte[] b,int p)
        {
            return BitConverter.ToInt32(b,p);
        }
        static void W(byte[] b,int p,int v)
        {
            BitConverter.GetBytes(unchecked((short)v)).CopyTo(b,p);
        }
        static int Ship(uint reference)
        {
            long delta=(long)reference-0x4fc340;
            if(delta<0||delta>=402000||delta%268!=0)throw new InvalidDataException("Aircraft member reference is outside its complete pool.");
            return (int)delta;
        }
        public static PracticeAircraftResourceBatchResult Apply(PracticeAircraftDestructionTable tables,byte[] visuals,byte[] shipPool,byte[] fleetPool, byte[] scoreRegion,byte[] effectPool,PracticeFleetScheduler schedule,int movieCounter,uint randomState)
        {
            if(tables==null||schedule==null)throw new ArgumentNullException("aircraft batch");
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000||scoreRegion==null||scoreRegion.Length!=176||effectPool==null||effectPool.Length!=800)throw new InvalidDataException("Aircraft resource batches require complete source pools.");
            if(W(scoreRegion,172)!=-2||movieCounter<short.MinValue||movieCounter>short.MaxValue)throw new NotSupportedException("Aircraft resource batches require the recorded offscreen owner context and signed16 movie counter.");
            new PracticeVisualPool(effectPool,visuals);
            var ships=(byte[])shipPool.Clone();
            var fleets=(byte[])fleetPool.Clone();
            var scores=(byte[])scoreRegion.Clone();
            var effects=(byte[])effectPool.Clone();
            var plan=schedule.TakeBatch();
            int members=0,retired=0;
            foreach(int slot in plan.CopySlots())
            {
                int at=slot*492,kind=I(fleets,at);
                fleets[at+482]=255;
                if(kind==0||kind==2||kind==6||kind==7||kind==8)continue;
                if((kind==1||kind==11)&&I(fleets,at+472)==0&&I(fleets,at+476)==0&&W(fleets,at+40)==-1&& (kind==1?(fleets[at+485]==2||fleets[at+485]==3):fleets[at+485]==0))continue;
                if(kind<3||kind>5||W(fleets,at+4)!=slot||I(fleets,at+472)!=0||W(fleets,at+40)!=-1||fleets[at+485]!=0) throw new NotSupportedException("Only idle active aircraft and the recorded neighboring early-skip fleets are admitted; batch preserved.");
                if(slot==249)throw new NotSupportedException("The recorded other selected fleet249 cannot also be the active aircraft in this profile.");
                var unique=new HashSet<uint>();
                for(int member=0;member<6;member++)
                {
                    uint reference=BitConverter.ToUInt32(fleets,at+16+member*4);
                    if(reference!=0&&!unique.Add(reference))throw new NotSupportedException("Duplicate aircraft member references require a separate batch study.");
                }
                for(int member=0;member<6;member++)
                {
                    uint reference=BitConverter.ToUInt32(fleets,at+16+member*4);
                    if(reference==0)continue;
                    int unit=Ship(reference),type=I(ships,unit);
                    if(type==0)continue;
                    if(type!=(kind==3?10:kind==4?9:8)||W(ships,unit+4)!=unit/268||W(ships,unit+12)!=slot)throw new InvalidDataException("Aircraft resource member kind/identity differs from its fleet.");
                    members++;
                    int amount=W(fleets,at+60)/2;
                    int remaining=unchecked((short)(W(ships,unit+96)-amount));
                    W(ships,unit+96,remaining);
                    if(remaining<=0)
                    {
                        W(ships,unit+96,0);
                        var loss=PracticeAircraftDestruction.RetireForEmptyResource(tables,visuals,ships,fleets,scores,effects,unit/268,member,movieCounter,randomState);
                        ships=loss.CopyShipPool();
                        fleets=loss.CopyFleetPool();
                        scores=loss.CopyScoreRegion();
                        effects=loss.CopyEffectPool();
                        retired++;
                    }
                }
                if(I(fleets,at)==0)continue;
                int associated=W(fleets,at+8);
                if(associated<0||associated>=1500)throw new InvalidDataException("Aircraft associated unit is outside its pool.");
                int home=W(ships,associated*268+12);
                if(home<0||home>=250)throw new InvalidDataException("Aircraft home fleet is outside its pool.");
                int threshold=Math.Max(Math.Abs(W(fleets,home*492+12)-W(fleets,at+12)),Math.Abs(W(fleets,home*492+14)-W(fleets,at+14)))+20;
                for(int member=0;member<6;member++)
                {
                    uint reference=BitConverter.ToUInt32(fleets,at+16+member*4);
                    if(reference!=0&&I(ships,Ship(reference))!=0&&W(ships,Ship(reference)+96)<=threshold)throw new NotSupportedException("The automatic aircraft return/landing threshold is outside this idle resource profile; batch preserved.");
                }
                int task=I(fleets,at+476);
                if(task!=1&&task!=2&&task!=4)throw new NotSupportedException("Aircraft attack/target-follow tasks require a separate batch study; batch preserved.");
            }
            return new PracticeAircraftResourceBatchResult(ships,fleets,scores,effects,plan.Next,members,retired,movieCounter,randomState);
        }
    }
}
