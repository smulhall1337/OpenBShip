using System;
using System.IO;

namespace FleetLab
{
    // Mandatory source-context inputs, not a choice of natural stack values.
    // This profile fixes offline gameMode0,selectedF249/unit0/localOwner-2 as in727.
    public sealed class PracticeAiDriverContext
    {
        public uint PhaseCounter { get; private set; }
        public int Scenario { get; private set; }
        public int ScenarioEventCount { get; private set; }
        public int RawOwnerPassControl { get; private set; }
        public int CallerWord { get; private set; }
        public int IncomingFrameWord { get; private set; }
        public int SelectedWaypoint { get; private set; }
        public PracticeAiDriverContext(uint phaseCounter, int scenario, int scenarioEventCount,
            int rawOwnerPassControl, int callerWord, int incomingFrameWord)
            : this(phaseCounter, scenario, scenarioEventCount, rawOwnerPassControl, callerWord, incomingFrameWord, 4) { }
        public PracticeAiDriverContext(uint phaseCounter, int scenario, int scenarioEventCount,
            int rawOwnerPassControl, int callerWord, int incomingFrameWord, int selectedWaypoint)
        {
            if (scenario != 1 || scenarioEventCount != 0 || rawOwnerPassControl != 0)
                throw new NotSupportedException("The compared driver profile requires Practice scenario1, zero scenario events and raw owner-pass control0. Other planners/events/owner dispatchers are not implemented here.");
            if (selectedWaypoint != 4 && selectedWaypoint != -1)
                throw new NotSupportedException("The compared driver starts with waypoint counter 4 or carries its invalidated value -1.");
            PhaseCounter = phaseCounter; Scenario = scenario; ScenarioEventCount = scenarioEventCount;
            RawOwnerPassControl = rawOwnerPassControl; CallerWord = callerWord; IncomingFrameWord = incomingFrameWord;
            SelectedWaypoint = selectedWaypoint;
        }
    }

    public sealed class PracticeAiDriverGateResult
    {
        readonly byte[] fleets;
        public PracticeAiDriverContext Context { get; private set; }
        public PracticePrephaseFlag23Result Prephase { get; private set; }
        public uint PhaseCounter { get { return Context.PhaseCounter; } }
        public bool AtDecisionPhase { get { return (PhaseCounter & 31) == 0; } }
        public int ScenarioSchedulerVisits { get { return AtDecisionPhase ? 1 : 0; } }
        public int NeutralPlannerVisits { get { return AtDecisionPhase ? 1 : 0; } }
        public int OwnerVisibilityPasses { get { return AtDecisionPhase ? 1 : 0; } }
        public int OwnerDispatcherVisits { get { return 0; } }
        public int CallerScratchAfter { get; private set; }
        public int RouteUiRequests { get { return Prephase.RouteUiRequests; } }
        public int SelectedWaypoint { get { return Prephase.SelectedWaypoint; } }
        public PracticeAiSharedBacking Backing { get { return Prephase.Backing; } }
        internal PracticeAiDriverGateResult(PracticeAiDriverContext context, PracticePrephaseFlag23Result prephase, byte[] fleets)
        {
            Context = context; Prephase = prephase; this.fleets = (byte[])fleets.Clone();
            //4282C0's saved EBP occupies the earlier child-local slot. This is
            //a supplied diagnostic word, not a pointer or inferred game value.
            CallerScratchAfter = AtDecisionPhase ? context.IncomingFrameWord : prephase.CallerWordAfter;
        }
        public byte[] CopyShipPool() { return Prephase.CopyShipPool(); }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
        public byte[] CopyPrimaryOrders() { return Prephase.CopyPrimaryOrders(); }
        public byte[] CopyCandidateRegion() { return Prephase.CopyCandidateRegion(); }
    }

    // Whole41D640 bounded caller.727/728/730 compare1728 complete states.
    // The prephase runs BEFORE the low-five-bit phase gate. Aligned calls run
    // the empty event scheduler,direct-return scenario1 planner and owner-1
    //visibility. With no active neutral fleets there are no owner task visits.
    public static class PracticeAiDriverGate
    {
        public static PracticeAiDriverGateResult EvaluateOfflineFlag23(PracticeAttachmentPatrolTables policy,
            PracticeOwnerVisibilityRules visibility, byte[] ships, byte[] fleets, byte[] orders,
            PracticeAiSharedBacking backing, byte[] candidates, int rawDelta, PracticeAiDriverContext context)
        {
            if (policy == null || visibility == null || context == null) throw new ArgumentNullException("rules/context");
            if (fleets == null || fleets.Length != 123000) throw new InvalidDataException("The driver requires the complete fleet pool.");
            for (int g = 0; g < 250; g++)
                if (BitConverter.ToInt32(fleets, g * 492) != 0 && BitConverter.ToInt16(fleets, g * 492 + 6) == -1)
                    throw new NotSupportedException("An active unowned fleet would enter an unimplemented owner dispatcher. This driver profile requires none.");
            var prephase = PracticePrephaseFlag23.EvaluateNoTargets(policy, ships, fleets, orders, backing,
                candidates, rawDelta, context.CallerWord, 0, context.SelectedWaypoint);
            var after = prephase.CopyFleetPool();
            if ((context.PhaseCounter & 31) == 0)
                after = visibility.Apply(after, -1, 0, -2, 0, 249).CopyFleetPool();
            //41D640 restores source activeOwner and leaves the phase counter
            //unchanged. Clock increment/pause live outside this entry.
            return new PracticeAiDriverGateResult(context, prephase, after);
        }
    }
}
