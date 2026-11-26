using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace FleetLab
{
    // Original609 whole416090 on exact601 outputs. Natural map/observers and
    // equipment are held; timers and timed-pool capacity are explicit fixtures.
    public sealed class PracticeProducedLaunch
    {
        readonly PracticeFiringRangeTable ranges;readonly PracticeVisibility visibility;readonly PracticeFiringRateTable rates;readonly PracticeTimedWeaponCatalog weapons;readonly PracticeUnitCatalog units;readonly PracticeTimedCreationState objects;readonly byte[] orders;readonly int[] created;
        public PracticeFleetMemberRequests Requests {get;private set;}
        public int TimerMode {get;private set;}public int Capacity {get;private set;}public bool Executed {get;private set;}
        public uint Clock {get{return (uint)Requests.Clock;}}public uint RandomState {get{return Requests.RandomState;}}
        public int ClampedTimers {get;private set;}public int RejectedRows {get;private set;}public int ConsumedRequests {get;private set;}public int AttemptedObjects {get;private set;}
        public int CreatedObjects {get{return created.Length;}}public int LastTargetFleet {get{return objects.LastTargetFleet;}}
        public byte[] CopyShipPool(){return objects.CopyShipPool();}public byte[] CopyFleetPool(){return objects.CopyFleetPool();}public byte[] CopyTimedPool(){return objects.CopyTimedPool();}public byte[] CopyOrderPool(){return (byte[])orders.Clone();}public int[] CopyCreatedSlots(){return (int[])created.Clone();}
        static void Member(int member){if(member<0||member>1)throw new ArgumentOutOfRangeException("member");}
        public int Weapon(int member){Member(member);return Requests.WeaponField(member);}
        public int Ammunition(int member){Member(member);return BitConverter.ToInt16(objects.CopyShipPool(),(30+member)*268+196+Weapon(member)*2);}
        public int Timer(int member){Member(member);return BitConverter.ToInt32(orders,(30+member)*52+4);}
        public int RequestCount(int member){Member(member);int count=0;for(int n=0;n<10;n++)if(BitConverter.ToInt16(orders,(30+member)*52+8+n*2)!=-1)count++;return count;}
        public PracticeFiringCheck Eligibility(int member){Member(member);return PracticeFiringEligibility.Evaluate(ranges,visibility,objects.CopyShipPool(),objects.CopyFleetPool(),30+member,15,Weapon(member),0,-2);}
        public ReadOnlyCollection<PracticeMemberProjectile> Projectiles {get{var list=new List<PracticeMemberProjectile>();var timed=objects.CopyTimedPool();foreach(int slot in created)list.Add(new PracticeMemberProjectile(timed,slot));return list.AsReadOnly();}}
        PracticeProducedLaunch(PracticeFiringRangeTable ranges,PracticeVisibility visibility,PracticeFiringRateTable rates,PracticeTimedWeaponCatalog weapons,PracticeUnitCatalog units,PracticeTimedCreationState objects,byte[] orders,PracticeFleetMemberRequests requests,int timer,int capacity,bool executed,int clamped,int rejected,int consumed,int attempts,int[] created)
        {this.ranges=ranges;this.visibility=visibility;this.rates=rates;this.weapons=weapons;this.units=units;this.objects=objects;this.orders=(byte[])orders.Clone();Requests=requests;TimerMode=timer;Capacity=capacity;Executed=executed;ClampedTimers=clamped;RejectedRows=rejected;ConsumedRequests=consumed;AttemptedObjects=attempts;this.created=(int[])created.Clone();}
        public static PracticeProducedLaunch Create(string programs,PracticeFleetMemberRequests requests,int timer,int capacity)
        {
            if(requests==null)throw new ArgumentNullException("requests");if(!requests.UsesProducedVisibility||!requests.Executed||requests.QueueProfile!=0||requests.GlobalWeapon!=0||requests.VisibilityPreset<0||requests.VisibilityPreset>1||requests.Difficulty!=2||requests.ComparisonWord!=0||(requests.InitialTask!=0&&requests.InitialTask!=8)||requests.QueuedCells.Count!=(requests.InitialTask==0?20:2))throw new NotSupportedException("The produced-request consumer admits completed original-equipment requests,global0,empty starting rows,task0/8,normal/prefilled map visibility only.");
            if(timer<0||timer>4)throw new ArgumentOutOfRangeException("timer");if(capacity<0||capacity>2)throw new ArgumentOutOfRangeException("capacity");var orders=requests.CopyOrderPool();for(int member=0;member<2;member++){int weapon=requests.WeaponField(member);if(weapon!=(requests.InitialTask==0?(member==0?18:17):22)||BitConverter.ToInt16(requests.CopyShipPool(),(30+member)*268+196+weapon*2)<=1)throw new NotSupportedException("Produced-request construction excludes unsupported weapons and last-round callbacks.");if(timer!=0){int clock=requests.Clock;BitConverter.GetBytes(timer==1?0:timer==2?clock:timer==3?clock-1:clock+15000).CopyTo(orders,(30+member)*52+4);}}
            var timed=new byte[21440];int occupied=capacity==0?0:capacity==1?79:80;for(int slot=0;slot<occupied;slot++){BitConverter.GetBytes(99).CopyTo(timed,slot*268);BitConverter.GetBytes((short)slot).CopyTo(timed,slot*268+4);}var ranges=PracticeFiringRangeTable.Load(programs);var visibility=new PracticeVisibility(File.ReadAllBytes(Path.Combine(programs,"VisibilityKinds.seqbin")));var rates=PracticeFiringRateTable.Load(programs);var weapons=PracticeTimedWeaponCatalog.Load(programs);var units=PracticeUnitCatalog.Load(programs);
            return new PracticeProducedLaunch(ranges,visibility,rates,weapons,units,new PracticeTimedCreationState(weapons,timed,requests.CopyShipPool(),requests.CopyFleetPool(),-1),orders,requests,timer,capacity,false,0,0,0,0,new int[0]);
        }
        public PracticeProducedLaunch Execute()
        {
            if(Executed)throw new InvalidOperationException("This one copied consumer pass is complete;reset or load another copy.");var result=PracticeFiringOrders.AdvanceWithLaunch(ranges,visibility,rates,weapons,units,objects,orders,Clock,0,-2);return new PracticeProducedLaunch(ranges,visibility,rates,weapons,units,result.Objects,result.CopyOrderPool(),Requests,TimerMode,Capacity,true,result.ClampedTimers,result.RejectedOrders,result.ConsumedOrders,result.AttemptedObjects,result.CopyCreatedSlots());
        }
    }
}
