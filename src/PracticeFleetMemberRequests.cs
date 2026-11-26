using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace FleetLab
{
    internal sealed class FleetMemberRequestSink
    {
        internal byte[] Orders;internal uint Random;readonly bool originalEquipment;
        internal FleetMemberRequestSink(byte[] orders):this(orders,false){}
        internal FleetMemberRequestSink(byte[] orders,bool originalEquipment){Orders=(byte[])orders.Clone();Random=731;this.originalEquipment=originalEquipment;}
        internal void Apply(byte[] ships,byte[] fleets,int source,int target){Orders=FleetMemberCellRequests.Apply(ships,fleets,Orders,source,target,Random,out Random,originalEquipment);}
    }
    public sealed class PracticeFleetQueuedCell
    {
        public int SourceUnit {get;private set;}public int TargetFleet {get;private set;}public int Slot {get;private set;}public int X {get;private set;}public int Y {get;private set;}public int Weapon {get;private set;}
        internal PracticeFleetQueuedCell(int unit,int target,int slot,int x,int y,int weapon){SourceUnit=unit;TargetFleet=target;Slot=slot;X=x;Y=y;Weapon=weapon;}
        public override string ToString(){return "Unit "+SourceUnit+" -> fleet "+TargetFleet+" · "+X+","+Y+" · slot "+Slot+" · weapon "+Weapon;}
    }
    // Original537 whole41D2B0,composed port538:648 captured input combinations.
    // One visit stores primary firing requests;416090 order execution is excluded.
    public sealed class PracticeFleetMemberRequests
    {
        readonly PracticeFleetMemberPreparation prepared,state;
        readonly byte[] orders;
        public int EquipmentProfile {get{return prepared.EquipmentProfile;}}
        public int InitialTask {get{return prepared.InitialTask;}}
        public int Difficulty {get{return prepared.Difficulty;}}
        public int ComparisonWord {get{return prepared.ComparisonWord;}}
        public int GlobalWeapon {get;private set;}
        public int Visibility {get;private set;}
        public int QueueProfile {get;private set;}
        public bool Executed {get;private set;}
        public bool UsesProducedVisibility {get{return prepared.UsesProducedVisibility;}}
        public int VisibilityPreset {get{return prepared.VisibilityPreset;}}
        public uint RandomState {get;private set;}
        public int Clock {get{return state.Clock;}}
        public int TaskKind {get{return state.TaskKind;}}
        public int SelectedWaypoint {get{return state.SelectedWaypoint;}}
        public int RouteUiRequests {get{return state.RouteUiRequests;}}
        public int MemberState(int member){return state.MemberState(member);}
        public int MemberDue(int member){return state.MemberDue(member);}
        public int MemberTarget(int member){return state.MemberTarget(member);}
        public int WeaponField(int member){return state.WeaponField(member);}
        public byte[] CopyShipPool(){return state.CopyShipPool();}
        public byte[] CopyFleetPool(){return state.CopyFleetPool();}
        public byte[] CopyTaskRecord(){return state.CopyTaskRecord();}
        public byte[] CopyOrderPool(){return (byte[])orders.Clone();}
        public byte[] CopyScratchBytes(){return state.CopyScratchBytes();}
        internal byte[] CopyRawList(){return state.CopyRawList();}
        public int PreparedState(int member){return prepared.MemberState(member);}
        public ReadOnlyCollection<PracticeFleetQueuedCell> QueuedCells
        {
            get
            {
                var list=new List<PracticeFleetQueuedCell>();for(int unit=30;unit<=31;unit++)
                {
                    int at=unit*52;uint reference=BitConverter.ToUInt32(orders,at+48);long offset=(long)reference-0x4b47a0;if(offset<0||offset>=123000||offset%492!=0)throw new InvalidDataException("Member request target reference.");int target=(int)(offset/492);
                    for(int n=0;n<10;n++){int x=BitConverter.ToInt16(orders,at+8+n*2);if(x==-1)continue;list.Add(new PracticeFleetQueuedCell(unit,target,n,x,BitConverter.ToInt16(orders,at+28+n*2),BitConverter.ToInt32(orders,at)));}
                }
                return list.AsReadOnly();
            }
        }
        PracticeFleetMemberRequests(PracticeFleetMemberPreparation prepared,PracticeFleetMemberPreparation state,byte[] orders,int globalWeapon,int visibility,int queue,bool executed,uint random)
        {this.prepared=prepared;this.state=state;this.orders=orders;GlobalWeapon=globalWeapon;Visibility=visibility;QueueProfile=queue;Executed=executed;RandomState=random;}
        public static PracticeFleetMemberRequests Create(string programs,PracticeMapStartup startup,int equipment,int initialTask,int difficulty,int comparisonWord,int globalWeapon,int visibility,int queueProfile)
        {
            if(equipment<3||equipment>5)throw new ArgumentOutOfRangeException("equipment");if(difficulty!=0&&difficulty!=2)throw new ArgumentOutOfRangeException("difficulty");if(globalWeapon!=0&&globalWeapon!=14&&globalWeapon!=19)throw new ArgumentOutOfRangeException("globalWeapon");if(visibility<1||visibility>3)throw new ArgumentOutOfRangeException("visibility");if(queueProfile<0||queueProfile>2)throw new ArgumentOutOfRangeException("queueProfile");
            var prefix=PracticeFleetMemberPreparation.Create(programs,startup,equipment,2,initialTask,difficulty,false,comparisonWord);while(prefix.CanAdvance)prefix=prefix.Advance();prefix=prefix.CopyForRequests(visibility);return new PracticeFleetMemberRequests(prefix,prefix,MakeOrders(prefix,queueProfile),globalWeapon,visibility,queueProfile,false,731);
        }
        public static PracticeFleetMemberRequests CreateFromPreparation(PracticeFleetMemberPreparation prepared,int globalWeapon,int queueProfile)
        {
            if(prepared==null)throw new ArgumentNullException("prepared");if(globalWeapon!=0&&globalWeapon!=17&&globalWeapon!=18&&globalWeapon!=22)throw new ArgumentOutOfRangeException("globalWeapon");if(queueProfile<0||queueProfile>2)throw new ArgumentOutOfRangeException("queueProfile");var prefix=prepared.CopyForProducedRequests();return new PracticeFleetMemberRequests(prefix,prefix,MakeOrders(prefix,queueProfile),globalWeapon,1,queueProfile,false,731);
        }
        static byte[] MakeOrders(PracticeFleetMemberPreparation prefix,int queueProfile)
        {
            var orders=new byte[78000];
            for(int unit=0;unit<1500;unit++){BitConverter.GetBytes(123456789+unit).CopyTo(orders,unit*52+4);for(int n=0;n<10;n++){BitConverter.GetBytes((short)-1).CopyTo(orders,unit*52+8+n*2);BitConverter.GetBytes((short)(-100-n)).CopyTo(orders,unit*52+28+n*2);}}
            var ships=prefix.CopyShipPool();var task=prefix.CopyTaskRecord();int filled=queueProfile==1?9:queueProfile==2?10:0;
            for(int member=0;member<2;member++)
            {
                int unit=30+member,at=unit*52,target=BitConverter.ToInt32(task,260+member*4);if(target<0||target>=250)target=14;BitConverter.GetBytes(BitConverter.ToInt32(ships,unit*268+84)).CopyTo(orders,at);BitConverter.GetBytes(0x4b47a0+target*492).CopyTo(orders,at+48);
                for(int n=0;n<10;n++){BitConverter.GetBytes((short)(n<filled?n:-1)).CopyTo(orders,at+8+n*2);BitConverter.GetBytes((short)(900+n)).CopyTo(orders,at+28+n*2);}
            }
            return orders;
        }
        public PracticeFleetMemberRequests Execute()
        {
            if(Executed)throw new InvalidOperationException("The one captured member request visit is complete;reset this copy.");var sink=new FleetMemberRequestSink(orders,UsesProducedVisibility);var next=prepared.ApplyRequestVisit(sink,GlobalWeapon);return new PracticeFleetMemberRequests(prepared,next,sink.Orders,GlobalWeapon,Visibility,QueueProfile,true,sink.Random);
        }
    }
}
