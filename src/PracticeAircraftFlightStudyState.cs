using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace FleetLab
{
    // Copied manual waypoint study. Queue edits are supplied diagnostic data;
    // each visit repeats the actor's four-slot source batch, with no wall clock.
    public sealed class PracticeAircraftFlightStudyState
    {
        readonly PracticeUnitCatalog units;
        readonly PracticeAircraftTables modes;
        readonly PracticeAircraftDestructionTable destruction;
        readonly byte[] visuals;
        readonly byte[] ships,fleets,scores,effects,auxiliary,primary;
        public int FleetSlot
        {
            get;
            private set;
        }
        public int Visits
        {
            get;
            private set;
        }
        public int SelectedWaypoint
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
        public PracticeAircraftWaypointBatchResult LastUpdate
        {
            get;
            private set;
        }
        public int Kind
        {
            get
            {
                return BitConverter.ToInt32(fleets,FleetSlot*492);
            }
        }
        public int Task
        {
            get
            {
                return BitConverter.ToInt32(fleets,FleetSlot*492+476);
            }
        }
        public int ReturnState
        {
            get
            {
                return BitConverter.ToInt32(fleets,FleetSlot*492+472);
            }
        }
        public int Speed
        {
            get
            {
                return BitConverter.ToInt16(fleets,FleetSlot*492+60);
            }
        }
        public int Facing
        {
            get
            {
                return fleets[FleetSlot*492+62];
            }
        }
        public Point Origin
        {
            get
            {
                return new Point(BitConverter.ToInt16(fleets,FleetSlot*492+12),BitConverter.ToInt16(fleets,FleetSlot*492+14));
            }
        }
        public bool Active
        {
            get
            {
                return Kind>=3&&Kind<=5;
            }
        }
        public bool ProfileReady
        {
            get
            {
                return Active&&ReturnState==0&&(Task==1||Task==2||Task==4)&&fleets[FleetSlot*492+485]==0;
            }
        }
        public ReadOnlyCollection<Point> Route
        {
            get
            {
                var points=new List<Point>();
                for(int n=0;n<5;n++)
                {
                    int x=BitConverter.ToInt16(fleets,FleetSlot*492+40+n*2);
                    if(x==-1)break;
                    points.Add(new Point(x,BitConverter.ToInt16(fleets,FleetSlot*492+50+n*2)));
                }
                return points.AsReadOnly();
            }
        }
        public ReadOnlyCollection<PracticeUnitRecord> Members
        {
            get
            {
                var list=new List<PracticeUnitRecord>();
                for(int n=0;n<6;n++)
                {
                    uint reference=BitConverter.ToUInt32(fleets,FleetSlot*492+16+n*4);
                    if(reference==0)continue;
                    long at=(long)reference-0x4fc340;
                    if(at<0||at>=402000||at%268!=0)throw new InvalidDataException("Aircraft flight member reference crosses its pool.");
                    if(BitConverter.ToInt32(ships,(int)at)!=0)list.Add(new PracticeUnitRecord(ships,(int)at/268));
                }
                return list.AsReadOnly();
            }
        }
        public PracticeAircraftFlightStudyState(string programs,PracticeAircraftStudyState parent)
        {
            if(parent==null||!parent.HasGroup||parent.Kind<3||parent.Kind>5)throw new InvalidOperationException("Activate an aircraft group before opening its waypoint study.");
            units=PracticeUnitCatalog.Load(programs);
            modes=PracticeAircraftTables.Load(programs);
            destruction=PracticeAircraftDestructionTable.Load(programs);
            visuals=File.ReadAllBytes(Path.Combine(programs,"ClassicVisuals.seqbin"));
            ships=parent.CopyShipPool();
            fleets=parent.CopyFleetPool();
            primary=parent.CopyPrimaryOrders();
            FleetSlot=parent.FleetSlot;
            SelectedWaypoint=parent.SelectedWaypoint;
            scores=new byte[176];
            BitConverter.GetBytes((short)-2).CopyTo(scores,172);
            effects=new byte[800];
            for(int n=0;n<40;n++)BitConverter.GetBytes((short)-1).CopyTo(effects,n*20);
            auxiliary=new byte[78336];
            RandomState=731;
            MovieCounter=0;
        }
        PracticeAircraftFlightStudyState(PracticeAircraftFlightStudyState before,byte[] s,byte[] f,byte[] score,byte[] effect,byte[] aux,byte[] orders,int point,int visits,int movie,uint random,PracticeAircraftWaypointBatchResult last)
        {
            units=before.units;
            modes=before.modes;
            destruction=before.destruction;
            visuals=before.visuals;
            ships=s;
            fleets=f;
            scores=score;
            effects=effect;
            auxiliary=aux;
            primary=orders;
            FleetSlot=before.FleetSlot;
            SelectedWaypoint=point;
            Visits=visits;
            MovieCounter=movie;
            RandomState=random;
            LastUpdate=last;
        }
        public PracticeAircraftFlightStudyState QueueSuppliedPoint(int x,int y)
        {
            if(!Active)throw new InvalidOperationException("Reset the flight study before editing a retired group.");
            if(x<0||x>=640||y<0||y>=640)throw new ArgumentOutOfRangeException("point","Test waypoint coordinates must be0..639.");
            int n=0;
            while(n<5&&BitConverter.ToInt16(fleets,FleetSlot*492+40+n*2)!=-1)n++;
            if(n==5)throw new InvalidOperationException("The copied route already contains five waypoints.");
            var f=(byte[])fleets.Clone();
            BitConverter.GetBytes((short)x).CopyTo(f,FleetSlot*492+40+n*2);
            BitConverter.GetBytes((short)y).CopyTo(f,FleetSlot*492+50+n*2);
            return new PracticeAircraftFlightStudyState(this,ships,f,scores,effects,auxiliary,primary,SelectedWaypoint,Visits,MovieCounter,RandomState,null);
        }
        public PracticeAircraftFlightStudyState ClearSuppliedRoute()
        {
            var f=(byte[])fleets.Clone();
            for(int n=0;n<5;n++)BitConverter.GetBytes((short)-1).CopyTo(f,FleetSlot*492+40+n*2);
            return new PracticeAircraftFlightStudyState(this,ships,f,scores,effects,auxiliary,primary,SelectedWaypoint,Visits,MovieCounter,RandomState,null);
        }
        public PracticeAircraftFlightStudyState ApplyModeTwo()
        {
            if(!Active)throw new InvalidOperationException("Reset the flight study before applying a mode to a retired group.");
            var stored=PracticeAircraftTransitions.ApplyOffscreen(modes,units,ships,fleets,primary,FleetSlot,0,249,SelectedWaypoint,-2);
            var active=PracticeAircraftTransitions.ApplyOffscreen(modes,units,stored.CopyShipPool(),stored.CopyFleetPool(),stored.CopyPrimaryOrders(),FleetSlot,2,249,stored.SelectedWaypoint,-2);
            return new PracticeAircraftFlightStudyState(this,active.CopyShipPool(),active.CopyFleetPool(),scores,effects,auxiliary,active.CopyPrimaryOrders(),active.SelectedWaypoint,Visits,MovieCounter,RandomState,null);
        }
        public PracticeAircraftFlightStudyState VisitOnce()
        {
            var result=PracticeAircraftWaypointBatch.AdvanceOffscreen(destruction,visuals,ships,fleets,scores,effects,auxiliary,new PracticeFleetScheduler(FleetSlot/4*4,0),FleetSlot,SelectedWaypoint,MovieCounter,RandomState);
            var b=result.Batch;
            return new PracticeAircraftFlightStudyState(this,b.CopyShipPool(),b.CopyFleetPool(),b.CopyScoreRegion(),b.CopyEffectPool(),result.CopyAuxiliary(),primary,result.SelectedWaypoint,checked(Visits+1),b.MovieCounter,b.RandomState,result);
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
        public byte[] CopyAuxiliary()
        {
            return (byte[])auxiliary.Clone();
        }
        public byte[] CopyPrimaryOrders()
        {
            return (byte[])primary.Clone();
        }
    }
}
