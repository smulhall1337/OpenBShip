using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FleetLab
{
    public sealed class PracticeMemberProjectile
    {
        public int Slot {get;private set;}
        public int Weapon {get;private set;}
        public int SourceUnit {get;private set;}
        public int TargetFleet {get;private set;}
        public int X {get;private set;}
        public int Y {get;private set;}
        public int OffsetX {get;private set;}
        public int OffsetY {get;private set;}
        public uint Deadline {get;private set;}
        internal PracticeMemberProjectile(byte[] pool,int slot)
        {
            int at=slot*268;Slot=slot;Weapon=BitConverter.ToInt32(pool,at);
            SourceUnit=BitConverter.ToInt16(pool,at+16);TargetFleet=BitConverter.ToInt16(pool,at+12);
            X=BitConverter.ToInt16(pool,at+8);Y=BitConverter.ToInt16(pool,at+10);
            OffsetX=BitConverter.ToInt16(pool,at+18);OffsetY=BitConverter.ToInt16(pool,at+20);
            Deadline=BitConverter.ToUInt32(pool,at+24);
        }
        public override string ToString(){return "Slot "+Slot+" · unit "+SourceUnit+" -> fleet "+TargetFleet+" · weapon "+Weapon;}
    }

    // RIP B9A8...6A569: original551 whole416090 ->416570/416A00, six
    // copied contexts. The member producer is original537; position, observer,
    // timer and capacity overrides are explicit fixtures, not natural AI policy.
    public sealed class PracticeFleetRequestLaunch
    {
        readonly PracticeFiringRangeTable ranges;
        readonly PracticeVisibility visibility;
        readonly PracticeFiringRateTable rates;
        readonly PracticeTimedWeaponCatalog weapons;
        readonly PracticeUnitCatalog units;
        readonly PracticeTimedCreationState objects;
        readonly byte[] orders;
        readonly int[] created;
        public int Family {get;private set;}
        public int Capacity {get;private set;}
        public uint Clock {get;private set;}
        public bool Executed {get;private set;}
        public int ConsumedRequests {get;private set;}
        public int AttemptedObjects {get;private set;}
        public int CreatedObjects {get{return created.Length;}}
        public int LastTargetFleet {get{return objects.LastTargetFleet;}}
        public int Weapon {get{return Family==0?14:19;}}
        public uint RequestRandomState {get;private set;}
        public int RequestCount(int member)
        {
            Member(member);int count=0;for(int n=0;n<10;n++)if(BitConverter.ToInt16(orders,(30+member)*52+8+n*2)!=-1)count++;return count;
        }
        public int Ammunition(int member){Member(member);return BitConverter.ToInt16(objects.CopyShipPool(),(30+member)*268+196+Weapon*2);}
        public PracticeFiringCheck Eligibility(int member)
        {
            Member(member);return PracticeFiringEligibility.Evaluate(ranges,visibility,objects.CopyShipPool(),objects.CopyFleetPool(),30+member,15-member,Weapon,0,-2);
        }
        static void Member(int member){if(member<0||member>1)throw new ArgumentOutOfRangeException("member");}
        public byte[] CopyShipPool(){return objects.CopyShipPool();}
        public byte[] CopyFleetPool(){return objects.CopyFleetPool();}
        public byte[] CopyOrderPool(){return (byte[])orders.Clone();}
        public byte[] CopyTimedPool(){return objects.CopyTimedPool();}
        public int[] CopyCreatedSlots(){return (int[])created.Clone();}
        public ReadOnlyCollection<PracticeMemberProjectile> Projectiles
        {
            get{var result=new List<PracticeMemberProjectile>();var timed=objects.CopyTimedPool();foreach(int slot in created)result.Add(new PracticeMemberProjectile(timed,slot));return result.AsReadOnly();}
        }
        PracticeFleetRequestLaunch(PracticeFiringRangeTable ranges,PracticeVisibility visibility,PracticeFiringRateTable rates,
            PracticeTimedWeaponCatalog weapons,PracticeUnitCatalog units,PracticeTimedCreationState objects,byte[] orders,
            int family,int capacity,uint clock,uint random,bool executed,int consumed,int attempts,int[] created)
        {
            this.ranges=ranges;this.visibility=visibility;this.rates=rates;this.weapons=weapons;this.units=units;
            this.objects=objects;this.orders=(byte[])orders.Clone();this.created=(int[])created.Clone();Family=family;
            Capacity=capacity;Clock=clock;RequestRandomState=random;Executed=executed;ConsumedRequests=consumed;AttemptedObjects=attempts;
        }
        static void W16(byte[] data,int at,int value){BitConverter.GetBytes(checked((short)value)).CopyTo(data,at);}
        static void W32(byte[] data,int at,int value){BitConverter.GetBytes(value).CopyTo(data,at);}
        public static PracticeFleetRequestLaunch Create(string programs,PracticeMapStartup startup,int family,int capacity)
        {
            if(family<0||family>1)throw new ArgumentOutOfRangeException("family");
            if(capacity<0||capacity>2)throw new ArgumentOutOfRangeException("capacity");
            var ranges=PracticeFiringRangeTable.Load(programs);var visibility=PracticeVisibility.Load(programs);
            var rates=PracticeFiringRateTable.Load(programs);var weapons=PracticeTimedWeaponCatalog.Load(programs);var units=PracticeUnitCatalog.Load(programs);
            var requests=PracticeFleetMemberRequests.Create(programs,startup,family==0?3:4,family==0?0:8,2,0,0,1,0).Execute();
            var s=requests.CopyShipPool();var f=requests.CopyFleetPool();var o=requests.CopyOrderPool();
            // original547 case013: both targets at the exact F2 source range.
            // case037 preserves the original copied F3 target positions.
            if(family==0)for(int target=14;target<=15;target++)
            {
                W16(f,target*492+12,BitConverter.ToInt16(f,16*492+12)+ranges.SourceRange(14));
                W16(f,target*492+14,BitConverter.ToInt16(f,16*492+14));
            }
            // Explicit observer249, owner1, radii255. Raw AI byte+467 alone
            // does not make both targets visible to the numeric firing predicate.
            Array.Clear(f,249*492,492);W32(f,249*492,1);W16(f,249*492+4,249);W16(f,249*492+6,1);
            W16(f,249*492+12,BitConverter.ToInt16(f,14*492+12));W16(f,249*492+14,BitConverter.ToInt16(f,14*492+14));
            f[249*492+464]=f[249*492+465]=f[249*492+466]=255;
            for(int unit=30;unit<=31;unit++)W32(o,unit*52+4,0);
            var timed=new byte[21440];int filled=capacity==0?0:capacity==1?79:80;
            // Occupied kind99 records are capacity fixtures only. Never step them.
            for(int slot=0;slot<filled;slot++){W32(timed,slot*268,99);W16(timed,slot*268+4,slot);}
            return new PracticeFleetRequestLaunch(ranges,visibility,rates,weapons,units,
                new PracticeTimedCreationState(weapons,timed,s,f,-1),o,family,capacity,(uint)requests.Clock,requests.RandomState,false,0,0,new int[0]);
        }
        public PracticeFleetRequestLaunch Execute()
        {
            if(Executed)throw new InvalidOperationException("The one copied local launch pass is complete; reset this copy.");
            var result=PracticeFiringOrders.AdvanceWithLaunch(ranges,visibility,rates,weapons,units,objects,orders,Clock,0,-2);
            return new PracticeFleetRequestLaunch(ranges,visibility,rates,weapons,units,result.Objects,result.CopyOrderPool(),
                Family,Capacity,Clock,RequestRandomState,true,result.ConsumedOrders,result.AttemptedObjects,result.CopyCreatedSlots());
        }
    }
}
