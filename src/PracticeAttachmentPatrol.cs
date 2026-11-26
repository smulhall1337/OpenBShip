using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace FleetLab
{
    public sealed class PracticeAttachmentPatrolTables
    {
        internal readonly PracticeUnitCatalog Units;
        internal readonly PracticeAircraftTables Aircraft;
        internal readonly byte[] Metadata, Ranges;
        PracticeAttachmentPatrolTables(PracticeUnitCatalog units, PracticeAircraftTables aircraft, byte[] metadata, byte[] ranges)
        { Units = units; Aircraft = aircraft; Metadata = metadata; Ranges = ranges; }
        static byte[] Pinned(string folder, string name, int length, string expected)
        {
            var b = File.ReadAllBytes(Path.Combine(folder, name));
            using (var h = SHA256.Create())
                if (b.Length != length || BitConverter.ToString(h.ComputeHash(b)).Replace("-", "") != expected)
                    throw new InvalidDataException("Unsupported attachment-policy table: " + name);
            return b;
        }
        public static PracticeAttachmentPatrolTables Load(string programs)
        {
            return new PracticeAttachmentPatrolTables(PracticeUnitCatalog.Load(programs), PracticeAircraftTables.Load(programs),
                Pinned(programs, "EquipmentDispatchKinds.seqbin", 41, "02CE3E390AC8866AD1F584102B9D10011B3D62663642AF1B117DE8A8D5A63A74"),
                Pinned(programs, "AiCandidateRawRanges.seqbin", 82, "BAD7B5737AE23238DF5F15E582D54426208E9A226AFF78F48696198B702DB68F"));
        }
    }

    public sealed class PracticeAttachmentPatrolResult
    {
        readonly byte[] ships, fleets, orders, candidates;
        readonly ReadOnlyCollection<int> visited, activated;
        public PracticeAiSharedBacking Backing { get; private set; }
        public int RouteUiRequests { get; private set; }
        public int SelectedWaypoint { get; private set; }
        public int StoppedAtStateOne { get; private set; }
        public ReadOnlyCollection<int> VisitedAircraft { get { return visited; } }
        public ReadOnlyCollection<int> ActivatedAircraft { get { return activated; } }
        internal PracticeAttachmentPatrolResult(byte[] s, byte[] f, byte[] o, byte[] c, PracticeAiSharedBacking backing,
                                                 int requests, int waypoint, int stop, List<int> visits, List<int> launches)
        {
            ships = (byte[])s.Clone(); fleets = (byte[])f.Clone(); orders = (byte[])o.Clone(); candidates = (byte[])c.Clone();
            Backing = backing; RouteUiRequests = requests; SelectedWaypoint = waypoint; StoppedAtStateOne = stop;
            visited = new List<int>(visits).AsReadOnly(); activated = new List<int>(launches).AsReadOnly();
        }
        public byte[] CopyShipPool() { return (byte[])ships.Clone(); }
        public byte[] CopyFleetPool() { return (byte[])fleets.Clone(); }
        public byte[] CopyPrimaryOrders() { return (byte[])orders.Clone(); }
        public byte[] CopyCandidateRegion() { return (byte[])candidates.Clone(); }
    }

    // Pinned RIP41F690 flag1 attachment policy. This entry admits no visible
    // targets and the compared offscreen transition profile only. It executes
    // the no-target member semantics, not an invented replacement for a firing
    // visit. Original681 compares the real source callbacks;40A900 UI is counted.
    public static class PracticeAttachmentPatrol
    {
        public const int CandidateRegionBytes = 1184, CandidateListOffset = 176;
        static int I(byte[] b, int at) { return BitConverter.ToInt32(b, at); }
        static int W(byte[] b, int at) { return BitConverter.ToInt16(b, at); }
        static void I(byte[] b, int at, int n) { BitConverter.GetBytes(n).CopyTo(b, at); }
        static void W(byte[] b, int at, int n) { BitConverter.GetBytes(unchecked((short)n)).CopyTo(b, at); }
        static int Ship(uint pointer)
        { long at = (long)pointer - 0x4fc340; if (at < 0 || at >= 402000 || at % 268 != 0) throw new InvalidDataException("Attachment policy ship reference."); return (int)(at / 268); }
        static int Fleet(uint pointer)
        { long at = (long)pointer - 0x4b47a0; if (at < 0 || at >= 123000 || at % 492 != 0) throw new InvalidDataException("Attachment policy fleet reference."); return (int)(at / 492); }
        static void ValidateMembers(PracticeAttachmentPatrolTables tables, byte[] s, byte[] f, int child, bool stored)
        {
            int at = child * 492, kind = I(f, at), members = 0;
            for (int m = 0; m < 6; m++)
            {
                uint reference = BitConverter.ToUInt32(f, at + 16 + m * 4); if (reference == 0) continue;
                int unit = Ship(reference) * 268, length = W(s, unit + 14), type = I(s, unit);
                if (type < 8 || type > 10 || length < 1 || length > 3) throw new NotSupportedException("This policy subset requires the compared populated aircraft templates8..10.");
                bool equipment = false;
                for (int e = 0; e < length * 4; e++)
                {
                    int weapon = stored ? tables.Aircraft.Equipment(kind, 4, e) : I(s, unit + 100 + e * 4);
                    if (weapon < 0 || weapon >= 41) throw new InvalidDataException("Attachment member equipment ID.");
                    if (weapon != 0 && (tables.Metadata[weapon] == 0xf2 || tables.Metadata[weapon] == 0xf3)) equipment = true;
                }
                if (!equipment) throw new NotSupportedException("The no-equipment service fallback is outside this patrol subset.");
                members++;
            }
            if (members == 0) throw new NotSupportedException("The compared policy uses real populated aircraft groups.");
        }
        static int ClampWord(int value) { return value >= 630 ? 630 : value > 0 ? value : 0; }

        public static PracticeAttachmentPatrolResult EvaluateNoTargets(PracticeAttachmentPatrolTables tables,
            byte[] shipPool, byte[] fleetPool, byte[] primaryOrders, PracticeAiSharedBacking backing,
            byte[] candidateRegion, int parentFleet, int rawDelta, int selectedWaypoint)
        {
            if (tables == null || backing == null) throw new ArgumentNullException("tables/backing");
            if (shipPool == null || shipPool.Length != 402000 || fleetPool == null || fleetPool.Length != 123000 || primaryOrders == null || primaryOrders.Length != 78000 || candidateRegion == null || candidateRegion.Length != CandidateRegionBytes)
                throw new InvalidDataException("Attachment patrol requires complete ship/fleet/order/shared-candidate pools.");
            if (parentFleet < 0 || parentFleet >= 250 || selectedWaypoint < -1 || selectedWaypoint > 4) throw new ArgumentOutOfRangeException("parentFleet");
            int parent = parentFleet * 492, owner = W(fleetPool, parent + 6);
            if (I(fleetPool, parent) != 1 || fleetPool[parent + 486] != 1 || owner < 0 || owner > 1)
                throw new NotSupportedException("This offscreen policy entry requires a flag1 surface parent owned by0 or1.");
            if (I(backing.CopyTaskRecord(parentFleet), 204) != 0x4b47a0 + parent)
                throw new InvalidDataException("Parent task does not reference its normalized fleet slot.");
            for (int g = 0; g < 250; g++) if (fleetPool[g * 492 + 467] != 0)
                throw new NotSupportedException("Visible targets are outside the compared no-target policy path.");

            var s = (byte[])shipPool.Clone(); var f = (byte[])fleetPool.Clone(); var o = (byte[])primaryOrders.Clone(); var c = (byte[])candidateRegion.Clone(); var tasks = backing;
            var visits = new List<int>(); var launches = new List<int>(); int counter = 0, requests = 0, waypoint = selectedWaypoint, stopped = -1;
            for (int member = 0; member < 6; member++)
            {
                uint parentReference = BitConverter.ToUInt32(f, parent + 16 + member * 4); if (parentReference == 0) continue;
                int parentUnit = Ship(parentReference), up = parentUnit * 268, type = I(s, up);
                if (type != 5 && type != 6 && type != 7) continue; counter += 2;
                for (int link = 3; link >= 0; link--)
                {
                    uint reference = BitConverter.ToUInt32(s, up + 56 + link * 4); if (reference == 0) continue;
                    int child = Fleet(reference), at = child * 492, kind = I(f, at);
                    if (kind != 3 && kind != 4 && kind != 6 && kind != 7) continue;
                    if (W(f, at + 4) != child || W(f, at + 8) != parentUnit || W(s, up + 12) != parentFleet || W(f, at + 10) != link || W(f, at + 6) < 0 || W(f, at + 6) > 1)
                        throw new NotSupportedException("Only consistent copied attachment/parent identities are compared.");
                    var task = tasks.CopyTaskRecord(child);
                    if (I(task, 204) != 0x4b47a0 + at) throw new InvalidDataException("Aircraft task reference differs from its fleet ID.");
                    ValidateMembers(tables, s, f, child, kind == 6 || kind == 7);
                    if (kind == 6 || kind == 7)
                    {
                        int timer = unchecked(I(task, 252) + rawDelta); I(task, 252, timer); tasks = tasks.WithTaskRecord(child, task);
                        if (timer >= 5000)
                        {
                            var transition = PracticeAircraftTransitions.ApplyOffscreen(tables.Aircraft, tables.Units, s, f, o, child, 4, 249, waypoint, -2);
                            s = transition.CopyShipPool(); f = transition.CopyFleetPool(); o = transition.CopyPrimaryOrders(); waypoint = transition.SelectedWaypoint; requests += transition.Invalidations;
                            f[at + 486] = 1; task = tasks.CopyTaskRecord(child); I(task, 200, (counter + link) % 4); tasks = tasks.WithTaskRecord(child, task); launches.Add(child);
                        }
                    }
                    kind = I(f, at); if (kind != 3 && kind != 4) continue;
                    task = tasks.CopyTaskRecord(child);
                    var scan = PracticeFleetCandidates.Evaluate(s, f, task, tables.Metadata, tables.Ranges, child, 1, owner);
                    if (!scan.EquipmentFound || scan.Candidates.Count != 0) throw new NotSupportedException("The member callback left the compared equipment/no-target path.");
                    scan.CopyScratchBytes().CopyTo(c, 0); I(c, CandidateListOffset, -1);
                    // No-target41D2B0 clears six member states and returns.
                    Array.Clear(task, 212, 24); tasks = tasks.WithTaskRecord(child, task); visits.Add(child);
                    for (int n = 1; n < 5; n++) W(f, at + 40 + n * 2, -1);
                    if (I(f, at + 472) == 1)
                    {
                        I(task, 200, 0); I(task, 252, 0); tasks = tasks.WithTaskRecord(child, task); stopped = child;
                        return new PracticeAttachmentPatrolResult(s, f, o, c, tasks, requests, waypoint, stopped, visits, launches);
                    }
                    int phase = I(task, 200), x = W(f, at + 40), y = W(f, at + 50), px = W(f, parent + 12), py = W(f, parent + 14);
                    if (phase < 0 || phase > 3) { x = unchecked((short)(px - 30)); y = py; I(task, 200, 0); }
                    else if (x == -1)
                    {
                        I(task, 200, (phase + 1) % 4);
                        x = phase == 1 ? unchecked((short)(px - 30)) : phase == 3 ? unchecked((short)(px + 30)) : px;
                        y = phase == 0 ? unchecked((short)(py - 30)) : phase == 2 ? unchecked((short)(py + 30)) : py;
                    }
                    W(f, at + 40, ClampWord(x)); W(f, at + 50, ClampWord(y)); tasks = tasks.WithTaskRecord(child, task);
                }
            }
            return new PracticeAttachmentPatrolResult(s, f, o, c, tasks, requests, waypoint, stopped, visits, launches);
        }
    }
}
