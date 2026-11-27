using System;
using System.IO;

namespace FleetLab
{
    // RIP B9A8...6A569: whole41FCC0, inactive415DF0, then two427670 batches.
    // Original864/875/884 compare short,barrier and actual-collision histories.
    // This explicit offline
    // profile has actor16/owner1/kind1, selected-other249/point4, dormant
    // policy/idle callbacks,kind0/1 terrain and inactive secondary pools. The caller
    // supplies the clock. Owner dispatch and natural frame pacing are absent.
    public sealed class PracticeNavigationMovementState
    {
        readonly byte[] fleets,terrain,secondary;
        public PracticeAiSharedBacking Backing { get; private set; }
        public PracticeAiRouteGrid Grid { get; private set; }
        public PracticeFleetScheduler Schedule { get; private set; }
        public int SelectedFleet { get { return 249; } }
        public int SelectedWaypoint { get; private set; }

        public PracticeNavigationMovementState(byte[] fleetPool,PracticeAiSharedBacking backing,PracticeAiRouteGrid grid,
            byte[] terrainPool,byte[] secondaryPool,PracticeFleetScheduler schedule,int selectedWaypoint)
            : this(fleetPool,backing,grid,terrainPool,secondaryPool,schedule,selectedWaypoint,false) { }

        PracticeNavigationMovementState(byte[] f,PracticeAiSharedBacking backing,PracticeAiRouteGrid grid,
            byte[] terrainPool,byte[] secondaryPool,PracticeFleetScheduler schedule,int selectedWaypoint,bool share)
        {
            if(f==null||f.Length!=123000||terrainPool==null||terrainPool.Length!=246000||secondaryPool==null||secondaryPool.Length!=61500)
                throw new InvalidDataException("Navigation/movement requires complete primary, terrain and secondary pools.");
            if(backing==null||grid==null||grid.TaskSlot!=16||schedule==null)
                throw new InvalidDataException("Navigation/movement requires the compared actor 16 grid, backing and scheduler.");
            if(selectedWaypoint!=4)
                throw new NotSupportedException("The compared selected-other profile retains raw waypoint counter 4.");
            for(int slot=0;slot<250;slot++)
            {
                int at=slot*492,kind=BitConverter.ToInt32(f,at);var task=backing.CopyTaskRecord(slot);
                if(BitConverter.ToInt16(f,at+4)!=slot||BitConverter.ToUInt32(task,204)!=0x4b47a0u+(uint)at)
                    throw new InvalidDataException("Navigation/movement pool identities disagree.");
                if(f[at+486]!=0||BitConverter.ToInt32(f,at+472)!=0)
                    throw new NotSupportedException("Unrelated policy and idle callbacks must be dormant.");
                if(slot==16)
                {
                    int goal=BitConverter.ToInt32(task,200),nav=BitConverter.ToInt32(task,304),speed=BitConverter.ToInt16(f,at+60);
                    int x=BitConverter.ToInt16(f,at+12),y=BitConverter.ToInt16(f,at+14);
                    if(kind!=1||BitConverter.ToInt16(f,at+6)!=1||BitConverter.ToInt32(f,at+476)!=0||
                        (f[at+485]!=2&&f[at+485]!=3)||speed<1||speed>7||BitConverter.ToInt32(task,0)!=11||
                        (goal!=2&&goal!=4)||(nav!=0&&nav!=1)||x<0||x>630||y<0||y>630)
                        throw new NotSupportedException("Navigation/movement requires the compared owner 1 task 11 and admitted kind 1 movement profile.");
                }
                else
                {
                    if(BitConverter.ToInt32(task,304)!=0||f[at+482]!=255)
                        throw new NotSupportedException("Only actor 16 may have navigation or collision work.");
                    if(kind!=0&&(kind!=11||BitConverter.ToInt32(f,at+476)!=0||f[at+485]!=0||BitConverter.ToInt16(f,at+40)!=-1))
                        throw new NotSupportedException("Other fleets must be inactive or the compared stationary kind 11 blockers.");
                }
            }
            for(int slot=0;slot<500;slot++)
            {
                int at=slot*492,kind=BitConverter.ToInt32(terrainPool,at);
                if(kind!=0&&kind!=1)throw new NotSupportedException("Navigation/movement compares inactive or kind1 terrain only.");
                if(kind==1&&(BitConverter.ToInt16(terrainPool,at+12)<0||BitConverter.ToInt16(terrainPool,at+12)>630||
                    BitConverter.ToInt16(terrainPool,at+14)<0||BitConverter.ToInt16(terrainPool,at+14)>630))
                    throw new NotSupportedException("Compared terrain coordinates lie in0..630.");
            }
            for(int slot=0;slot<125;slot++)
                if(BitConverter.ToInt32(secondaryPool,slot*492)!=0)throw new NotSupportedException("This composition requires inactive secondary records.");
            fleets=(byte[])f.Clone();terrain=share?terrainPool:(byte[])terrainPool.Clone();secondary=share?secondaryPool:(byte[])secondaryPool.Clone();
            Backing=backing;Grid=grid;Schedule=schedule;SelectedWaypoint=selectedWaypoint;
        }

        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
        public byte[] CopyTerrainPool() { return (byte[])terrain.Clone(); }
        public byte[] CopySecondaryPool() { return (byte[])secondary.Clone(); }

        public PracticeNavigationMovementUpdate AdvanceOfflinePoll(int sourceClock)
        {
            var task=Backing.CopyTaskRecord(16);byte[] f;PracticeAiSharedBacking backing;PracticeAiRouteGrid grid;
            PracticeQueuedNavigationResult navigation=null;PracticePrephaseNavigationResult dormant=null;
            if(BitConverter.ToInt32(task,304)==0&&fleets[16*492+482]==255)
            {
                dormant=PracticePrephaseNavigation.Evaluate(fleets,Backing);f=fleets;backing=dormant.Backing;grid=Grid;
            }
            else
            {
                navigation=PracticeQueuedNavigation.EvaluateForMovement(Grid,fleets,terrain,Backing,249,SelectedWaypoint,sourceClock,32000,0);
                f=navigation.CopyFleetPool();backing=navigation.Backing;grid=navigation.Grid;
            }
            // Original415DF0 has no effects for the supplied Practice selector1;
            // original864 verifies that intervening call at every poll.
            var prepared=new PracticeNavigationMovementState(f,backing,grid,terrain,secondary,Schedule,SelectedWaypoint,true);
            var movement=new PracticeMovementState(f,terrain,backing.CopyMovementAuxiliary(),249,SelectedWaypoint);
            var update=new PracticeFleetBatchState(movement,Schedule,secondary).AdvanceNormalPoll();
            var next=new PracticeNavigationMovementState(update.State.Movement.CopyFleetPool(),
                backing.WithMovementAuxiliary(update.State.Movement.CopyAuxiliary()),grid,terrain,secondary,
                update.State.Schedule,update.State.Movement.SelectedWaypoint,true);
            return new PracticeNavigationMovementUpdate(prepared,next,navigation,dormant,update,sourceClock);
        }
    }

    public sealed class PracticeNavigationMovementUpdate
    {
        public PracticeNavigationMovementState Prephase { get; private set; }
        public PracticeNavigationMovementState State { get; private set; }
        public PracticeQueuedNavigationResult Navigation { get; private set; }
        public PracticePrephaseNavigationResult Dormant { get; private set; }
        public PracticeFleetBatchAdvance Movement { get; private set; }
        public int SourceClock { get; private set; }
        internal PracticeNavigationMovementUpdate(PracticeNavigationMovementState prephase,PracticeNavigationMovementState state,
            PracticeQueuedNavigationResult navigation,PracticePrephaseNavigationResult dormant,PracticeFleetBatchAdvance movement,int sourceClock)
        { Prephase=prephase;State=state;Navigation=navigation;Dormant=dormant;Movement=movement;SourceClock=sourceClock; }
    }
}
