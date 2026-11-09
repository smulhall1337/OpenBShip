using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using FleetLab;

// Independent managed replay: the delivered weapon/movie pair THEN target
// selector use ONE NativeRandom. This does not change the playable game's
// deliberately separate streams, nor cover initialization or later stages.
class CheckTargetOrderTraces {
    static void Require(bool value, string message) { if (!value) throw new InvalidDataException(message); }
    static int Main(string[] args) {
        try {
            if (args.Length != 3) return 2;
            var cases = new Dictionary<int, string[]>();
            foreach (string line in File.ReadAllLines(args[0])) {
                if (line.StartsWith("Probe,")) continue;
                string[] row = line.Split(','); Require(row.Length == 12, "Case column count");
                int id = int.Parse(row[0]); Require(!cases.ContainsKey(id), "Duplicate probe"); cases.Add(id, row);
            }
            int probes = 0, totalDraws = 0, targetDraws = 0, previous = -1, order = 0, phase = -1, calls = 0;
            uint nativeState = 0; string[] current = null; int expectedCalls = 0; uint expectedState = 0;
            var shapes = new HashSet<string>();
            var levels = new HashSet<int>(); var seeds = new HashSet<uint>(); var ships = new HashSet<int>();
            var combinations = new HashSet<string>(); var targetCallers = new HashSet<string>();
            var nativeDraws = new List<int>(); var expectedDraws = new List<int>();
            int x = -1, y = -1, weapon = -1, movie = -1;
            foreach (string line in File.ReadLines(args[1])) {
                if (line.StartsWith("Probe,")) continue;
                string[] row = line.Split(','); Require(row.Length == 8, "Draw column count");
                int id = int.Parse(row[0]), ordinal = int.Parse(row[1]), value = int.Parse(row[4]);
                string kind = row[2];
                if (id != previous) {
                    Require(previous == -1 || phase == 5, "Previous probe did not finish");
                    Require(id == previous + 1 && cases.TryGetValue(id, out current), "Missing/out-of-order case");
                    order = calls = 0; phase = -1; previous = id; probes++;
                    string name = current[1]; int level = int.Parse(current[2]); uint seed = uint.Parse(current[3]); int ship = int.Parse(current[4]);
                    nativeState = seed; shapes.Add(name); levels.Add(level); seeds.Add(seed); ships.Add(ship);
                    Require(combinations.Add(name + "," + level + "," + seed + "," + ship), "Duplicate input combination");
                    string[] cells = current[11].Split(';'); Require(cells.Length == 100, "Observation grid size");
                    var grid = new int[10, 10]; for (int i = 0; i < 100; i++) grid[i / 10, i % 10] = int.Parse(cells[i]);
                    var board = new Board(); Require(board.Place(new Point(0, 0), 1, true, "Probe", ship), "Fixture shooter");
                    var rng = new NativeRandom(seed);
                    FiringPresentation firing = ClassicWeapons.Choose(board.Ships[0], true, rng);
                    TargetDecision target = OriginalOpponent.Choose(grid, level, rng);
                    Require(!target.Stalled, "Unsupported native stall fixture");
                    weapon = firing.WeaponId; movie = firing.MovieId; x = target.Cell.X; y = target.Cell.Y;
                    expectedCalls = rng.Calls; expectedState = rng.State;
                    Require(int.Parse(current[5]) == weapon && int.Parse(current[6]) == movie, "Managed firing pair differs: " + id);
                    Require(int.Parse(current[7]) == x && int.Parse(current[8]) == y, "Managed target differs: " + id);
                    Require(int.Parse(current[9]) == expectedCalls && uint.Parse(current[10]) == expectedState, "Managed combined RNG history differs: " + id);
                    // Independent shape/budget invariants stop mislabeled or
                    // accidentally empty fixtures from inflating branch coverage.
                    int hits = 0, misses = 0, masked = 0;
                    foreach (int cell in grid) { if (cell == 8) hits++; if (cell == 1) misses++; if (cell == 2 || cell == 4) masked++; }
                    if (name == "empty" || name == "masked") {
                        Require(hits == 0 && misses == 0 && (name == "empty" ? masked == 0 : masked > 0), "Search fixture shape");
                        Require(expectedCalls == 2 + 3 * OriginalOpponent.Budget(level), "Empty search budget draw count");
                    } else if (name == "exhausted") {
                        Require(misses == 100 && expectedCalls == 2 + 2 * OriginalOpponent.Budget(level), "Resolved search budget draw count");
                        Require(grid[x, y] == 1, "Native exhausted-board coordinate policy");
                    } else if (name == "sparse") Require(misses == 99 && grid[2, 5] == 0, "Sparse fixture shape");
                    else if (name == "single-low" || name == "single-high") {
                        Require(hits == 1 && misses == 0 && x == 3 && y == (name == "single-low" ? 1 : 8) && expectedCalls > 2, "Forced edge pursuit");
                    } else if (name == "chain-negative" || name == "chain-positive") {
                        Require(hits == 2 && grid[6, 4] == 8 && grid[6, 5] == 8 && x == 6 && y == (name == "chain-negative" ? 3 : 6) && expectedCalls == 2, "Deterministic hit-chain extension");
                    } else if (name == "pattern") Require(hits == 0 && misses > 0 && misses < 100, "Pattern fixture shape");
                    else throw new InvalidDataException("Unknown fixture shape");
                    expectedDraws.Clear(); nativeDraws.Clear(); var replay = new NativeRandom(seed);
                    for (int i = 0; i < expectedCalls; i++) expectedDraws.Add(replay.Next());
                }
                Require(ordinal == order++, "Non-contiguous event order");
                if (kind == "Begin") { Require(phase == -1, "Duplicate begin"); phase = 0; }
                else if (kind == "rand") {
                    Require(phase == 0 || phase == 2, "Random draw outside firing/target phases");
                    string caller = row[3];
                    if (phase == 0) {
                        Require(calls < 2, "Too many pre-target draws");
                        if (calls == 0) Require(caller == "0041C8D9", "Weapon call site differs");
                        else {
                            uint va = Convert.ToUInt32(caller, 16);
                            Require(va >= 0x416640 && va < 0x41698c, "Firing movie call site outside original dispatcher");
                        }
                    } else {
                        Require(caller == "0041CA94" || caller == "0041CB4B" || caller == "0041CB5B" || caller == "0041CB8D", "Unexpected native target call site");
                        targetCallers.Add(caller); targetDraws++;
                    }
                    nativeState = unchecked(nativeState * 214013u + 2531011u);
                    Require(value == (int)((nativeState >> 16) & 32767), "Native CRT draw differs");
                    Require(calls < expectedDraws.Count && value == expectedDraws[calls], "Managed per-draw replay differs");
                    nativeDraws.Add(value); calls++; totalDraws++;
                }
                else if (kind == "MovieRequest") { Require(phase == 0 && calls == 2 && value == movie, "Movie must follow firing draws"); phase = 1; }
                else if (kind == "TargetEnter") { Require(phase == 1 && row[3] == "0041C91B" && value == weapon, "Target must follow movie request"); phase = 2; }
                else if (kind == "TargetReturn") { Require(phase == 2 && int.Parse(row[6]) == x && int.Parse(row[7]) == y, "Actual native target return differs"); phase = 3; }
                else if (kind == "NextStageStub") { Require(phase == 3 && row[3] == "0041C933", "Following stage order differs"); phase = 4; }
                else if (kind == "Boundary") { Require(phase == 4 && calls == expectedCalls && nativeState == expectedState, "Final history boundary differs"); phase = 5; }
                else throw new InvalidDataException("Unknown trace event " + kind);
                Require(uint.Parse(row[5]) == nativeState, "Recorded native shared state differs");
                if (phase >= 3) Require(int.Parse(row[6]) == x && int.Parse(row[7]) == y, "Post-target coordinates changed");
            }
            Require(phase == 5 && probes == cases.Count && probes == 720, "Incomplete case coverage");
            Require(shapes.Count == 9 && levels.Count == 4 && seeds.Count == 4 && ships.Count == 5 && combinations.Count == 720, "Fixture matrix differs");
            Require(targetCallers.Count == 4, "Not all four target RNG call sites observed");
            string report = "PASS " + probes + " bounded chained probes: 9 observation shapes x 4 difficulty levels x 4 seeds x 5 shooter types.\r\n" +
                "PASS " + totalDraws + " actual native CRT draws (" + targetDraws + " in target selection), every output/state and all four target call sites checked.\r\n" +
                "PASS original opponent order: firing pair -> movie request -> real target selector -> following-stage stub.\r\n" +
                "PASS delivered pure selectors replay the same shared stream and agree on weapon, movie, coordinates, call count and final state in all probes.\r\n" +
                "Scope: copied hash-locked numerical code in a newly owned x86 helper; media admission/following stage are stubs; shooter and observation grid are harness inputs. No native startup, fleet generation, complete turn/session, physical input or audiovisual acceptance. Runtime streams are unchanged.\r\n" +
                "Exhausted boards deliberately preserve native last-coordinate returns. Isolated hits with no open Y neighbor are excluded because original code can loop indefinitely.\r\n";
            File.WriteAllText(args[2], report); Console.WriteLine(report); return 0;
        } catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
