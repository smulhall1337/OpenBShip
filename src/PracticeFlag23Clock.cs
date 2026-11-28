using System;
using System.IO;

namespace FleetLab
{
    // B9A8...6A569, original941: mode-0 clock header surrounding whole41D640.
    // This is an explicit offline comparison profile, not an application timer.
    public sealed class PracticeFlag23ClockState
    {
        readonly byte[] ships, fleets, orders, candidates;
        readonly PracticeAttachmentPatrolTables policy;
        readonly PracticeOwnerVisibilityRules visibility;
        public PracticeAiSharedBacking Backing { get; private set; }
        public OriginalFrameClock Clock { get; private set; }
        public int SelectedWaypoint { get; private set; }

        public PracticeFlag23ClockState(PracticeAttachmentPatrolTables policyRules,
            PracticeOwnerVisibilityRules visibilityRules, byte[] shipPool, byte[] fleetPool,
            byte[] orderPool, PracticeAiSharedBacking backing, byte[] candidateRegion,
            OriginalFrameClock clock, int selectedWaypoint)
        {
            if (policyRules == null || visibilityRules == null || backing == null || clock == null)
                throw new ArgumentNullException("rules/backing/clock");
            if (shipPool == null || shipPool.Length != 402000 || fleetPool == null || fleetPool.Length != 123000 ||
                orderPool == null || orderPool.Length != 78000 || candidateRegion == null || candidateRegion.Length != 1184)
                throw new InvalidDataException("Clock/policy stepping requires all five complete source pools.");
            if (selectedWaypoint != 4 && selectedWaypoint != -1)
                throw new NotSupportedException("The compared waypoint counter is 4 or its carried invalidation -1.");
            for (int g = 0; g < 250; g++)
            {
                int at = g * 492, kind = BitConverter.ToInt32(fleetPool, at), flag = fleetPool[at + 486];
                var task = backing.CopyTaskRecord(g);
                if (BitConverter.ToUInt32(task, 204) != 0x4b47a0u + (uint)at ||
                    fleetPool[at + 467] != 0 || fleetPool[at + 482] != 255 || BitConverter.ToInt32(task, 304) != 0)
                    throw new NotSupportedException("The clock profile requires source identities, no visible targets, no collisions and no active navigation.");
                if (kind != 0 && BitConverter.ToInt16(fleetPool, at + 6) == -1)
                    throw new NotSupportedException("An active unowned fleet would enter an unsupported task dispatcher.");
                if (flag != 0 && !(g == 15 && flag == 2) && !(g == 23 && flag == 1 && kind == 3))
                    throw new NotSupportedException("This clock profile compares parent 15 policy 2 and child 23's following policy 1 only.");
                int state = BitConverter.ToInt32(fleetPool, at + 472);
                if (state != 0 && !(g == 23 && state == 1))
                    throw new NotSupportedException("Other idle or policy states are outside this clock profile.");
            }
            if (BitConverter.ToInt32(fleetPool, 15 * 492) != 1 || BitConverter.ToInt16(fleetPool, 15 * 492 + 6) != 0 ||
                fleetPool[15 * 492 + 486] != 2 ||
                (BitConverter.ToInt32(fleetPool, 23 * 492) != 6 && BitConverter.ToInt32(fleetPool, 23 * 492) != 3))
                throw new NotSupportedException("Clock/policy stepping requires the compared owner-0 carrier and its stored or active child 23.");
            policy = policyRules; visibility = visibilityRules;
            ships = (byte[])shipPool.Clone(); fleets = (byte[])fleetPool.Clone(); orders = (byte[])orderPool.Clone(); candidates = (byte[])candidateRegion.Clone();
            Backing = backing; Clock = clock; SelectedWaypoint = selectedWaypoint;
        }

        public byte[] CopyShipPool() { return (byte[])ships.Clone(); }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
        public byte[] CopyPrimaryOrders() { return (byte[])orders.Clone(); }
        public byte[] CopyCandidateRegion() { return (byte[])candidates.Clone(); }

        public PracticeFlag23ClockUpdate AdvanceFrame(uint sample, PracticeAiDriverContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (context.SelectedWaypoint != SelectedWaypoint)
                throw new InvalidOperationException("Carry the applied selected waypoint counter into the next frame.");
            if (context.CallerWord != 0 || context.IncomingFrameWord != 0x2468ace0)
                throw new NotSupportedException("This clock comparison supplies caller word 0 and incoming frame word 0x2468ACE0 explicitly.");
            var bodyClock = Clock.BeginFrame(sample, 0);
            var driver = PracticeAiDriverGate.EvaluateOfflineFlag23(policy, visibility, ships, fleets, orders,
                Backing, candidates, unchecked((int)bodyClock.DeltaMilliseconds), context);
            // Complete only after a successful body. The source zero-delta quirk
            // and signed wrapping policy timer remain in their recovered modules.
            var next = new PracticeFlag23ClockState(policy, visibility, driver.CopyShipPool(), driver.CopyFleetPool(),
                driver.CopyPrimaryOrders(), driver.Backing, driver.CopyCandidateRegion(), bodyClock.CompleteFrame(), driver.SelectedWaypoint);
            return new PracticeFlag23ClockUpdate(this, bodyClock, driver, next, sample);
        }
    }

    public sealed class PracticeFlag23ClockUpdate
    {
        public PracticeFlag23ClockState Before { get; private set; }
        public OriginalFrameClock BodyClock { get; private set; }
        public PracticeAiDriverGateResult Driver { get; private set; }
        public PracticeFlag23ClockState State { get; private set; }
        public uint Sample { get; private set; }
        internal PracticeFlag23ClockUpdate(PracticeFlag23ClockState before, OriginalFrameClock body,
            PracticeAiDriverGateResult driver, PracticeFlag23ClockState state, uint sample)
        { Before = before; BodyClock = body; Driver = driver; State = state; Sample = sample; }
    }
}
