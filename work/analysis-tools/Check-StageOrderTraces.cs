using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using FleetLab;

// Delivered pure selectors replay a single RNG; an independent phase template
// checks the copied controller's exact callback order, including recursion.
class CheckStageOrderTraces {
    class Event {
        public string Kind, Caller;
        public int Value, Phase, Side, X, Y;
        public Event(string kind, string caller, int value, int phase, int side, int x, int y) {
            Kind = kind; Caller = caller; Value = value; Phase = phase; Side = side; X = x; Y = y;
        }
    }
    static void Need(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
    static List<Event> Expected(string[] c) {
        string[] cells = c[24].Split(';'); Need(cells.Length == 100, "Observation grid size");
        int level = int.Parse(c[2]), ship = int.Parse(c[4]), unavailable = int.Parse(c[5]);
        Need(unavailable == 0 || unavailable == 1, "Admission fixture");
        uint seed = uint.Parse(c[3]); var grid = new int[10, 10];
        for (int i = 0; i < 100; i++) grid[i / 10, i % 10] = int.Parse(cells[i]);
        var board = new Board(); Need(board.Place(new Point(0, 0), 1, true, "Probe", ship), "Fixture shooter");
        var rng = new NativeRandom(seed); var firing = ClassicWeapons.Choose(board.Ships[0], true, rng);
        var target = OriginalOpponent.Choose(grid, level, rng); Need(!target.Stalled, "Unsupported native stall");
        int prefixCalls = rng.Calls, x = target.Cell.X, y = target.Cell.Y;
        bool guns = firing.WeaponId >= 14 && firing.WeaponId <= 18;
        int travel = ClassicSequence.Travel(firing.WeaponId, guns ? rng.Next() : 0);
        Need(int.Parse(c[6]) == firing.WeaponId && int.Parse(c[7]) == firing.MovieId && int.Parse(c[8]) == travel, "Firing/travel output differs at " + c[0]);
        Need(int.Parse(c[9]) == x && int.Parse(c[10]) == y, "Target differs at " + c[0]);
        Need(int.Parse(c[11]) == prefixCalls && int.Parse(c[12]) == rng.Calls && uint.Parse(c[13]) == rng.State, "Shared random history differs at " + c[0]);
        Need(int.Parse(c[14]) == 0 && int.Parse(c[15]) == 0, "Final native phase/side not reset");
        Need(int.Parse(c[16]) == 1 && int.Parse(c[17]) == x && int.Parse(c[18]) == y, "Impact callback count/coordinate differs");
        Need(int.Parse(c[19]) == 0x1ff020 && int.Parse(c[20]) == 0xb47a0, "Impact owner/fleet argument differs");
        Need(int.Parse(c[21]) == 3 - unavailable, "Completion callback count differs");
        Need(int.Parse(c[22]) == 0x1234 && int.Parse(c[23]) == -1, "Wrong side's sentinel changed");
        // Fixed input categories cover mask, budget and pursuit boundaries.
        if (c[1] == "empty" || c[1] == "masked") Need(prefixCalls == 2 + 3 * OriginalOpponent.Budget(level), "Empty/masked budget");
        if (c[1] == "exhausted") Need(prefixCalls == 2 + 2 * OriginalOpponent.Budget(level) && grid[x, y] == 1, "Exhausted budget return");
        if (c[1] == "chain-negative" || c[1] == "chain-positive") Need(prefixCalls == 2 && x == 6 && y == (c[1] == "chain-negative" ? 3 : 6), "Hit-chain coordinate convention");
        var events = new List<Event>(); bool known = false;
        Action<string, string, int, int, int> add = delegate(string kind, string caller, int value, int phase, int side) {
            events.Add(new Event(kind, caller, value, phase, side, known ? x : -1, known ? y : -1));
        };
        var replay = new NativeRandom(seed);
        add("Begin", "00000000", 0, 4, 0);
        add("rand", "0041C8D9", replay.Next(), 4, 1);
        add("rand", "dispatcher", replay.Next(), 4, 1);
        add("FiringRequestStub", "00000000", firing.MovieId, 4, 1);
        add("AdmissionReturnStub", "00000000", 0, 4, 1);
        add("TargetEnter", "0041C91B", firing.WeaponId, 4, 1);
        for (int i = 2; i < prefixCalls; i++) add("rand", "target", replay.Next(), 4, 1);
        known = true;
        add("TargetReturn", "0041C91B", 0, 4, 1);
        add("ViewStub", "00000000", 2, 5, 1);
        add("PrefixReturn", "00000000", 0, 5, 1);
        add("FiringCompletionHarness", "00000000", 0, 5, 1);
        if (guns) add("rand", "0042378D", replay.Next(), 6, 1);
        add("TravelRequestStub", "00000000", travel, 6, 1);
        add("AdmissionReturnStub", "00000000", unavailable, 6, 1);
        if (unavailable == 1) add("ImpactStub", "00423862", firing.WeaponId, 7, 1);
        add("FiringCompletionReturn", "00000000", 0, unavailable == 1 ? 7 : 6, 1);
        if (unavailable == 0) {
            add("TravelCompletionHarness", "00000000", 0, 6, 1);
            add("ImpactStub", "00423862", firing.WeaponId, 7, 1);
            add("TravelCompletionReturn", "00000000", 0, 7, 1);
        }
        add("PostImpactHarness", "00000000", 0, 7, 1);
        add("ViewStub", "00000000", 1, 7, 1);
        add("Boundary", "00000000", 0, 0, 0);
        Need(replay.Calls == rng.Calls && replay.State == rng.State, "Expected per-draw history");
        return events;
    }
    static int Main(string[] args) {
        try {
            if (args.Length != 3) return 2;
            var cases = new Dictionary<int, string[]>(); var combinations = new HashSet<string>();
            var travelIds = new HashSet<int>(); var shapes = new HashSet<string>();
            var seeds = new HashSet<uint>(); var types = new HashSet<int>(); var levels = new HashSet<int>();
            foreach (string line in File.ReadAllLines(args[0])) {
                if (line.StartsWith("Probe,")) continue;
                string[] row = line.Split(','); Need(row.Length == 25, "Case column count"); int id = int.Parse(row[0]);
                Need(!cases.ContainsKey(id), "Duplicate case"); cases.Add(id, row);
                Need(combinations.Add(string.Join(",", row, 1, 5)), "Duplicate input combination");
                shapes.Add(row[1]); levels.Add(int.Parse(row[2])); seeds.Add(uint.Parse(row[3])); types.Add(int.Parse(row[4])); travelIds.Add(int.Parse(row[8]));
            }
            int previous = -1, eventIndex = 0, probes = 0, draws = 0, targetDraws = 0, travelDraws = 0;
            int available = 0, recursive = 0; uint state = 0;
            List<Event> expected = null; var observedTargetSites = new HashSet<string>();
            foreach (string line in File.ReadLines(args[1])) {
                if (line.StartsWith("Probe,")) continue;
                string[] row = line.Split(','); Need(row.Length == 10, "Event column count"); int id = int.Parse(row[0]);
                if (id != previous) {
                    Need(expected == null || eventIndex == expected.Count, "Previous event history truncated");
                    Need(id == previous + 1 && cases.ContainsKey(id), "Missing/out-of-order probe"); previous = id; probes++;
                    expected = Expected(cases[id]); eventIndex = 0; state = uint.Parse(cases[id][3]);
                    if (cases[id][5] == "0") available++; else recursive++;
                }
                Need(eventIndex < expected.Count && int.Parse(row[1]) == eventIndex, "Unexpected event ordinal");
                Event e = expected[eventIndex++]; string where = "probe " + id + ", event " + row[1];
                Need(row[2] == e.Kind && int.Parse(row[4]) == e.Value, "Event/value order differs: " + where);
                Need(int.Parse(row[6]) == e.X && int.Parse(row[7]) == e.Y, "Event coordinates differ: " + where);
                Need(int.Parse(row[8]) == e.Phase && int.Parse(row[9]) == e.Side, "Native phase/side differs: " + where);
                if (e.Caller == "dispatcher") {
                    uint va = Convert.ToUInt32(row[3], 16); Need(va >= 0x416640 && va < 0x41698c, "Firing caller outside span");
                } else if (e.Caller == "target") {
                    Need(row[3] == "0041CA94" || row[3] == "0041CB4B" || row[3] == "0041CB5B" || row[3] == "0041CB8D", "Target caller outside known sites");
                    observedTargetSites.Add(row[3]); targetDraws++;
                } else Need(row[3] == e.Caller, "Native call site differs: " + where);
                if (e.Kind == "rand") {
                    state = unchecked(state * 214013u + 2531011u); draws++;
                    Need(e.Value == (int)((state >> 16) & 32767), "Native CRT output differs");
                    if (e.Caller == "0042378D") travelDraws++;
                }
                Need(uint.Parse(row[5]) == state, "Native shared state differs: " + where);
            }
            Need(expected != null && eventIndex == expected.Count && probes == cases.Count && probes == 1440, "Incomplete probe history");
            Need(available == 720 && recursive == 720 && shapes.Count == 9 && levels.Count == 4 && seeds.Count == 4 && types.Count == 5, "Input matrix differs");
            Need(travelIds.SetEquals(new int[] { 21, 24, 129, 130, 131 }) && observedTargetSites.Count == 4, "Travel/target branch coverage differs");
            Need(draws == 141960 && targetDraws == 138360 && travelDraws == 720, "Independent draw totals differ from fixture matrix");
            string report = "PASS 1,440 bounded native opponent/target/phase probes: 720 admitted-travel and 720 unavailable-travel paths.\r\n" +
                "PASS 141,960 actual native CRT draws, including 138,360 target draws and 720 gun-travel draws; per-draw values/states and caller positions agree.\r\n" +
                "PASS delivered pure firing/target/travel selectors replay one stream and match movie IDs, target, call counts and final state.\r\n" +
                "PASS exact callback order and phase/side trace 4 -> 5 -> 6 -> 7 -> 0; one impact endpoint, correct target/owner/fleet arguments, side-specific sentinel and view requests.\r\n" +
                "PASS unavailable travel recurses to the impact endpoint before the firing-completion call returns; admitted travel waits for the harness travel-completion boundary.\r\n" +
                "Scope: source-identity-locked copied numerical code. Media admission, view and impact endpoints are stubs; completion boundaries, grids and shooter are harness inputs. No native frame clock, actual damage, initialization, fleet generation or full native gameplay/session is established. Production v0.30 streams are unchanged.\r\n";
            File.WriteAllText(args[2], report); Console.WriteLine(report); return 0;
        } catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
