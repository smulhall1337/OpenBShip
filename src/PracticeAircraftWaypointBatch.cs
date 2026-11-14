using System;
using System.IO;
using System.Drawing;
namespace FleetLab
{
    public enum PracticeAircraftWaypointOutcome
    {
        NoWaypoint,Moved,ReachedWaypoint,ReachedLockedWaypoint,FleetRetired
    }
    public sealed class PracticeAircraftWaypointBatchResult
    {
        readonly byte[] auxiliary;
        public PracticeAircraftResourceBatchResult Batch
        {
            get;
            private set;
        }
        public PracticeAircraftWaypointOutcome Outcome
        {
            get;
            private set;
        }
        public Point Before
        {
            get;
            private set;
        }
        public Point After
        {
            get;
            private set;
        }
        public bool QueueConsumed
        {
            get;
            private set;
        }
        public int SelectedWaypoint
        {
            get;
            private set;
        }
        internal PracticeAircraftWaypointBatchResult(PracticeAircraftResourceBatchResult prefix,byte[] fleets,byte[] aux,PracticeAircraftWaypointOutcome outcome,Point before,Point after,bool consumed,int point)
        {
            Batch=new PracticeAircraftResourceBatchResult(prefix.CopyShipPool(),fleets,prefix.CopyScoreRegion(),prefix.CopyEffectPool(),prefix.NextSchedule,prefix.AircraftMembers,prefix.RetiredUnits,prefix.MovieCounter,prefix.RandomState);
            auxiliary=aux;
            Outcome=outcome;
            Before=before;
            After=after;
            QueueConsumed=consumed;
            SelectedWaypoint=point;
        }
        public byte[] CopyAuxiliary()
        {
            return (byte[])auxiliary.Clone();
        }
    }
    // B9A8...6A569: whole427670 resource prefix +427CC7..42822B air waypoint
    // body, state0/task1/2/4/flag0, one visited actor and recorded early-skip
    // neighbors. World coordinates0..639. Source selected OTHER fleet249,
    // localowner-2/network0/scenario1/playercount0, not a real frame/caller.
    // 0044CF30 collision-kind lists:13x13int32 SHA256 A22F...51D4;
    // rows3/4/5 are empty. Primary terrain only applies to kinds1/9.
    // Source input append, active return/attack tasks and missions remain separate.
    public static class PracticeAircraftWaypointBatch
    {
        static int W(byte[] b,int at)
        {
            return BitConverter.ToInt16(b,at);
        }
        static int I(byte[] b,int at)
        {
            return BitConverter.ToInt32(b,at);
        }
        static void W(byte[] b,int at,int value)
        {
            BitConverter.GetBytes((short)value).CopyTo(b,at);
        }
        static void I(byte[] b,int at,int value)
        {
            BitConverter.GetBytes(value).CopyTo(b,at);
        }
        internal static Point MoveToward(byte[] f,int actor,Point before,int tx,int ty)
        {
            int at=actor*492,speed=W(f,at+60);
            if(speed<1||speed>13)throw new NotSupportedException("Live waypoint movement needs verified positive speed1..13; batch preserved.");
            int dx=tx-before.X,dy=ty-before.Y,distance=Math.Max(Math.Abs(dx),Math.Abs(dy));
            float denominator=(float)(distance/(speed*.5));
            int px=before.X,py=before.Y;
            if(denominator!=0)
            {
                px+=PracticeMovementState.Component(dx,denominator);
                py+=PracticeMovementState.Component(dy,denominator);
                W(f,at+12,px);
                W(f,at+14,py);
                f[at+62]=(byte)(Math.Abs(dy)<Math.Abs(dx)?(dx>0?1:3):(dy>0?2:0));
            }
            return new Point(px,py);
        }
        public static PracticeAircraftWaypointBatchResult AdvanceOffscreen(PracticeAircraftDestructionTable tables,byte[] visuals,byte[] shipPool,byte[] fleetPool,byte[] scoreRegion,byte[] effectPool,byte[] auxiliaryData,PracticeFleetScheduler schedule,int actor,int selectedWaypoint,int movieCounter,uint randomState)
        {
            if(schedule==null)throw new ArgumentNullException("schedule");
            if(shipPool==null||shipPool.Length!=402000||fleetPool==null||fleetPool.Length!=123000||auxiliaryData==null||auxiliaryData.Length!=78336)throw new InvalidDataException("Aircraft waypoint batches require complete ship/fleet/auxiliary pools.");
            if(actor<0||actor>=249)throw new ArgumentOutOfRangeException("actor","The offscreen selected-other fleet249 cannot be the moving aircraft.");
            if(W(fleetPool,249*492+4)!=249)throw new NotSupportedException("The offscreen selected-other record must retain source identity249; batch preserved.");
            if(selectedWaypoint< -1||selectedWaypoint>4)throw new ArgumentOutOfRangeException("selectedWaypoint");
            var slots=schedule.TakeBatch().CopySlots();
            bool visited=false;
            foreach(int slot in slots)
            {
                if(slot==actor)visited=true;
                else if(!PracticeAircraftResourceBatch.IsRecordedEarlySkip(fleetPool,slot*492))throw new NotSupportedException("Other visited fleets must take the recorded early-skip branches; batch preserved.");
            }
            if(!visited)throw new NotSupportedException("This source batch does not visit the requested aircraft; batch preserved.");
            int at=actor*492,kind=I(fleetPool,at),task=I(fleetPool,at+476),speed=W(fleetPool,at+60);
            if(kind<3||kind>5||W(fleetPool,at+4)!=actor||I(fleetPool,at+472)!=0||(task!=1&&task!=2&&task!=4)||fleetPool[at+485]!=0)throw new NotSupportedException("Only state0/task1,2,4/flag0 active aircraft waypoint bodies are admitted; batch preserved.");
            if(speed<0||speed>13)throw new NotSupportedException("Aircraft source speed is outside the verified0..13 profile; batch preserved.");
            var before=new Point(W(fleetPool,at+12),W(fleetPool,at+14));
            if(before.X<0||before.X>=640||before.Y<0||before.Y>=640)throw new InvalidDataException("Aircraft origin is outside the admitted world.");
            for(int n=0;n<5;n++)
            {
                int x=W(fleetPool,at+40+n*2),y=W(fleetPool,at+50+n*2);
                if(x!=-1&&(x<0||x>=640||y<0||y>=640))throw new InvalidDataException("Aircraft waypoint is outside the admitted world.");
            }
            var prefix=PracticeAircraftResourceBatch.ApplyBeforeWaypoint(tables,visuals,shipPool,fleetPool,scoreRegion,effectPool,schedule,movieCounter,randomState,actor);
            return CompleteAfterResources(prefix,auxiliaryData,actor,selectedWaypoint,before);
        }
        internal static PracticeAircraftWaypointBatchResult CompleteAfterResources(PracticeAircraftResourceBatchResult prefix,byte[] auxiliaryData,int actor,int selectedWaypoint,Point before)
        {
            var f=prefix.CopyFleetPool();
            var aux=(byte[])auxiliaryData.Clone();
            int at=actor*492;
            for(int n=0;n<5;n++)
            {
                int x=W(f,at+40+n*2),y=W(f,at+50+n*2);
                if(x!=-1&&(x<0||x>=640||y<0||y>=640))throw new InvalidDataException("Aircraft waypoint is outside the admitted world.");
            }
            if(I(f,at)==0)return new PracticeAircraftWaypointBatchResult(prefix,f,aux,PracticeAircraftWaypointOutcome.FleetRetired,before,before,false,selectedWaypoint);
            int tx=W(f,at+40),ty=W(f,at+50);
            if(tx==-1)return new PracticeAircraftWaypointBatchResult(prefix,f,aux,PracticeAircraftWaypointOutcome.NoWaypoint,before,before,false,selectedWaypoint);
            var position=MoveToward(f,actor,before,tx,ty);
            int px=position.X,py=position.Y;
            bool arrived=px==tx&&py==ty,consumed=false;
            if(arrived&&I(aux,0x50+actor*312)!=1)
            {
                I(aux,0x50+249*312,0);
                aux[0x3c+249*312]=0;
                for(int n=0;n<4;n++)
                {
                    W(f,at+40+n*2,W(f,at+42+n*2));
                    W(f,at+50+n*2,W(f,at+52+n*2));
                }
                W(f,at+48,-1);
                consumed=true;
            }
            var outcome=arrived?(consumed?PracticeAircraftWaypointOutcome.ReachedWaypoint:PracticeAircraftWaypointOutcome.ReachedLockedWaypoint):PracticeAircraftWaypointOutcome.Moved;
            return new PracticeAircraftWaypointBatchResult(prefix,f,aux,outcome,before,new Point(px,py),consumed,selectedWaypoint);
        }
    }
}
