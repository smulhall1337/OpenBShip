using System;
using System.Collections.ObjectModel;

namespace FleetLab
{
    // Samples, initial clocks and timer are diagnostic inputs from original941.
    // No live timer or automatic world advancement is hidden behind this viewer.
    public sealed class PracticeFlag23ClockStudyState
    {
        static readonly string[] names = { "Variable samples · stored aircraft", "Repeated sample · source zero reset",
            "Wrapping samples · signed timer delta", "Exact 5000 activation · selection carry" };
        static readonly uint[][] samples = { new uint[] {1000,1016,1040,1100}, new uint[] {1000,1000,1020},
            new uint[] {0xfffffffe,1,0,40}, new uint[] {1000,1016,1040} };
        static readonly uint[] current = {984,984,0xfffffff0,984}, delta = {7,9,5,0}, timer = {4000,1000,1000,4984};
        public static ReadOnlyCollection<string> Presets { get { return Array.AsReadOnly(names); } }
        public int Preset { get; private set; }
        public int Frames { get; private set; }
        public int WindowFrames { get { return samples[Preset].Length; } }
        public bool CanAdvance { get { return Frames < WindowFrames; } }
        public uint NextSample { get { if (!CanAdvance) throw new InvalidOperationException("Recorded frame window complete."); return samples[Preset][Frames]; } }
        public uint NextPhase { get { return (uint)(31 + Frames); } }
        public int Activations { get; private set; }
        public int UiRequests { get; private set; }
        public int NeutralPasses { get; private set; }
        public PracticeFlag23ClockState State { get; private set; }
        public PracticeFlag23ClockUpdate LastUpdate { get; private set; }
        public int Timer { get { return BitConverter.ToInt32(State.Backing.CopyTaskRecord(23),252); } }
        public int Kind { get { return BitConverter.ToInt32(State.CopyFleetPool(),23*492); } }

        public PracticeFlag23ClockStudyState(string programs, PracticeAttachmentPatrolStudyBasis basis, int preset)
        {
            if (basis == null) throw new ArgumentNullException("basis");
            if (preset < 0 || preset >= names.Length) throw new ArgumentOutOfRangeException("preset");
            Preset = preset;
            var input = basis.CreateFixture(15,false,0,40,0,false,0,false,false).ForWholePass(0);
            var f = input.CopyFleetPool(); f[15*492+486] = 2;
            var r = input.Backing.CopyRegion(); BitConverter.GetBytes(timer[preset]).CopyTo(r,23*312+252);
            State = new PracticeFlag23ClockState(PracticeAttachmentPatrolTables.Load(programs), PracticeOwnerVisibilityRules.Load(programs),
                input.CopyShipPool(), f, input.CopyPrimaryOrders(), new PracticeAiSharedBacking(r), input.CopyCandidateRegion(),
                new OriginalFrameClock(991,current[preset],731,delta[preset]),4);
        }
        PracticeFlag23ClockStudyState(PracticeFlag23ClockStudyState old, PracticeFlag23ClockUpdate update)
        {
            Preset=old.Preset;Frames=old.Frames+1;State=update.State;LastUpdate=update;
            Activations=old.Activations+update.Driver.Prephase.Activations;UiRequests=old.UiRequests+update.Driver.RouteUiRequests;
            NeutralPasses=old.NeutralPasses+update.Driver.OwnerVisibilityPasses;
        }
        public PracticeFlag23ClockStudyState Advance()
        {
            if(!CanAdvance)throw new InvalidOperationException("The recorded frame window is complete; reset to compare again.");
            var context=new PracticeAiDriverContext(NextPhase,1,0,0,0,0x2468ace0,State.SelectedWaypoint);
            return new PracticeFlag23ClockStudyState(this,State.AdvanceFrame(NextSample,context));
        }
    }
}
