using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

// Extends checkpoint 39's exact numerical fragments with the original phase
// controller. Media/view/impact calls are observations, not playback or damage.
// The harness supplies completion boundaries; there is no native frame clock.
class ClassicStageOrderOracle : ClassicTargetOrderOracle {
    [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p, UIntPtr size, uint flags, uint protect);
    [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p, UIntPtr size, uint protect, out uint old);
    [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p, UIntPtr size, uint flags);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Select();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Call();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Seed(uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int RandomCall(int returnAddress);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Target(IntPtr x, IntPtr y);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Movie(int id, IntPtr owner);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int View(int id);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Impact(int weapon, IntPtr shooter, IntPtr fleet, int x, int y);
    protected void CopyPhase() {
        byte[] bytes = Read(0x423540, 0x384); int changes = 0;
        var globals = new HashSet<uint>(new uint[] { 0x4fb6ec, 0x4e41ec, 0x4e41ee, 0x4b4794, 0x4fb6f4, 0x4ae818, 0x4ae81a, 0x4fbc4c, 0x4e375c, 0x4e3818, 0x4e381a, 0x4e42d0, 0x4238a4 });
        for (int at = 0; at <= bytes.Length - 4; at++) {
            uint operand = BitConverter.ToUInt32(bytes, at);
            bool table = at >= 0x364 && operand >= 0x423540 && operand < 0x423892;
            if (!globals.Contains(operand) && !table) continue;
            BitConverter.GetBytes(Address((int)(operand - 0x400000))).CopyTo(bytes, at); changes++; at += 3;
        }
        if (changes != 40) throw new InvalidDataException("Phase relocation count differs");
        Write(0x23540, bytes);
    }
    int Word(int offset) { return Marshal.ReadInt16(memory, offset); }
    int DWord(int offset) { return Marshal.ReadInt32(memory, offset); }
    uint State { get { return unchecked((uint)DWord(0x54d50)); } }
    void Evaluate(string exe, string destination) {
        source = File.ReadAllBytes(exe);
        if (IntPtr.Size != 4 || Hash(source) != Identity) throw new InvalidDataException("Matching image and x86 required");
        pe = BitConverter.ToInt32(source, 60); imageBase = BitConverter.ToUInt32(source, pe + 52);
        if (imageBase != 0x400000) throw new InvalidDataException("Unexpected source image base");
        Directory.CreateDirectory(destination);
        using (var provenance = new StreamWriter(Path.Combine(destination, "Native-Stage-Order-Provenance.csv"))) {
            provenance.WriteLine("SourceSHA256,VA,Bytes,SHA256,Role");
            uint[] starts = { 0x41c8c0, 0x416640, 0x41c940, 0x439790, 0x439780, 0x44d730, 0x423540, 0x423368 };
            int[] lengths = { 0x7b, 0x3b2, 0x348, 0x2a, 10, 0x48, 0x384, 0x13 };
            string[] roles = { "Opponent prefix", "Firing dispatcher and tables", "Target selector and tables", "CRT rand", "CRT srand including RET", "Weapon lists", "Phase controller and tables", "Read-only phase-4 caller guard - not executed" };
            for (int i = 0; i < starts.Length; i++)
                provenance.WriteLine(Identity + "," + starts[i].ToString("X8") + "," + lengths[i] + "," + Hash(Read(starts[i], lengths[i])) + "," + roles[i]);
        }
        memory = VirtualAlloc(IntPtr.Zero, new UIntPtr(0x200000), 0x3000, 4);
        if (memory == IntPtr.Zero) throw new Exception("Allocation failed");
        try {
            CopyPrefix(0x416640, 0x3b2, 24); CopyPrefix(0x41c8c0, 0x7b, 11);
            CopyPrefix(0x439790, 0x2a, 2); CopyPrefix(0x439780, 10, 1); CopyTarget(); CopyPhase();
            byte[] rand = new byte[0x2a], srand = new byte[10];
            Marshal.Copy(IntPtr.Add(memory, 0x39790), rand, 0, rand.Length);
            Marshal.Copy(IntPtr.Add(memory, 0x39780), srand, 0, srand.Length);
            Write(0x1000, rand); Write(0x1100, srand); Write(0x4d730, Read(0x44d730, 0x48));
            Marshal.WriteInt32(memory, 0xb499c, unchecked((int)Address(Ship)));
            Marshal.WriteByte(memory, 0xb4b60, 0);
            Marshal.WriteInt32(memory, 0xfb3d8, 7);
            Select nativeRandom = null; Target nativeTarget = null;
            int probe = 0, order = 0, firingMovie = -1, travelMovie = -1, x = -1, y = -1;
            int calls = 0, targetCalls = 0, impacts = 0, impactX = -1, impactY = -1, ownerOffset = -1, fleetOffset = -1, unavailable = 0;
            using (var trace = new StreamWriter(Path.Combine(destination, "Native-Stage-Order-Draws.csv")))
            using (var cases = new StreamWriter(Path.Combine(destination, "Native-Stage-Order-Cases.csv"))) {
                trace.WriteLine("Probe,Order,Kind,CallerVA,Value,StateAfter,X,Y,Phase,Side");
                cases.WriteLine("Probe,Case,Level,Seed,ShipType,Unavailable,Weapon,FiringMovie,TravelMovie,X,Y,CallsAtTarget,FinalCalls,FinalState,FinalPhase,FinalSide,Impacts,ImpactX,ImpactY,OwnerOffset,FleetOffset,CompletionCalls,PlayerSentinel,OpponentSentinel,CellsXMajor");
                Action<string, uint, int> record = delegate(string kind, uint caller, int value) {
                    trace.WriteLine(probe + "," + (order++) + "," + kind + "," + caller.ToString("X8") + "," + value + "," + State + "," + x + "," + y + "," + Word(0xb4794) + "," + Word(0xe42d0));
                };
                RandomCall random = delegate(int ret) {
                    int value = nativeRandom(); calls++;
                    uint offset = unchecked((uint)ret - Address(0) - 5);
                    uint caller = offset >= TargetCode && offset < TargetCode + 0x348 ? 0x41c940 + offset - TargetCode : 0x400000 + offset;
                    record("rand", caller, value); return value;
                };
                var wrapper = new List<byte> { 0xff, 0x34, 0x24, 0xb8 };
                wrapper.AddRange(BitConverter.GetBytes(Marshal.GetFunctionPointerForDelegate(random).ToInt32()));
                wrapper.AddRange(new byte[] { 0xff, 0xd0, 0x83, 0xc4, 4, 0xc3 }); Write(0x39790, wrapper.ToArray());
                Movie movie = delegate(int id, IntPtr owner) {
                    bool firing = Word(0xb4794) == 4;
                    if (firing) firingMovie = id; else travelMovie = id;
                    record(firing ? "FiringRequestStub" : "TravelRequestStub", 0, id);
                    int result = firing ? 0 : unavailable;
                    record("AdmissionReturnStub", 0, result); return result;
                };
                Target target = delegate(IntPtr px, IntPtr py) {
                    record("TargetEnter", 0x41c91b, DWord(0xfb6f4)); nativeTarget(px, py);
                    x = Marshal.ReadInt16(px); y = Marshal.ReadInt16(py); targetCalls = calls;
                    record("TargetReturn", 0x41c91b, 0);
                };
                View view = delegate(int id) { record("ViewStub", 0, id); return 0; };
                Impact impact = delegate(int weapon, IntPtr shooter, IntPtr fleet, int px, int py) {
                    impacts++; impactX = px; impactY = py;
                    ownerOffset = unchecked((int)((uint)shooter.ToInt32() - Address(0)));
                    fleetOffset = unchecked((int)((uint)fleet.ToInt32() - Address(0)));
                    record("ImpactStub", 0x423862, weapon);
                };
                Stub(0x2460, movie); Stub(0x1c940, target); Stub(0x12d0, view); Stub(0x23fb0, impact);
                foreach (int page in new int[] { 0x1000, 0x16000, 0x1c000, 0x23000, 0x39000, 0x2000, TargetCode }) {
                    uint old; if (!VirtualProtect(IntPtr.Add(memory, page), new UIntPtr(4096), 0x20, out old)) throw new Exception("Code protection failed");
                }
                nativeRandom = (Select)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory, 0x1000), typeof(Select));
                nativeTarget = (Target)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory, TargetCode), typeof(Target));
                var seed = (Seed)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory, 0x1100), typeof(Seed));
                var opponent = (Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory, 0x1c8c0), typeof(Call));
                var advance = (Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory, 0x23540), typeof(Call));
                foreach (uint inputSeed in new uint[] { 0, 1, 731, uint.MaxValue })
                foreach (int type in new int[] { 7, 1, 2, 12, 4 })
                for (int level = 0; level < 4; level++)
                foreach (string name in new string[] { "empty", "masked", "exhausted", "sparse", "single-low", "single-high", "chain-negative", "chain-positive", "pattern" })
                for (unavailable = 0; unavailable < 2; unavailable++) {
                    int[] grid = Fixture(name); order = calls = targetCalls = impacts = 0;
                    firingMovie = travelMovie = x = y = impactX = impactY = ownerOffset = fleetOffset = -1;
                    seed(inputSeed); Marshal.WriteInt32(memory, Ship, type); Marshal.WriteInt32(memory, 0xfbdf4, level);
                    Marshal.Copy(grid, 0, IntPtr.Add(memory, Grid), grid.Length);
                    Marshal.WriteInt16(memory, 0xae81a, -1); Marshal.WriteInt16(memory, 0xae818, -1);
                    Marshal.WriteInt16(memory, 0xb4794, 4); Marshal.WriteInt16(memory, 0xe42d0, 0);
                    Marshal.WriteInt32(memory, 0xfb6ec, 0);
                    Marshal.WriteInt16(memory, 0xe41ec, 0); Marshal.WriteInt16(memory, 0xe41ee, 0);
                    Marshal.WriteInt16(memory, 0xe3818, 0x1234); Marshal.WriteInt16(memory, 0xe381a, 0x1234);
                    record("Begin", 0, 0); opponent(); record("PrefixReturn", 0, 0);
                    int completions = 0;
                    record("FiringCompletionHarness", 0, 0); advance(); completions++; record("FiringCompletionReturn", 0, 0);
                    if (unavailable == 0) {
                        record("TravelCompletionHarness", 0, 0); advance(); completions++; record("TravelCompletionReturn", 0, 0);
                    }
                    record("PostImpactHarness", 0, 0); advance(); completions++; record("Boundary", 0, 0);
                    cases.WriteLine(probe + "," + name + "," + level + "," + inputSeed + "," + type + "," + unavailable + "," + DWord(0xfb6f4) + "," + firingMovie + "," + travelMovie + "," + x + "," + y + "," + targetCalls + "," + calls + "," + State + "," + Word(0xb4794) + "," + Word(0xe42d0) + "," + impacts + "," + impactX + "," + impactY + "," + ownerOffset + "," + fleetOffset + "," + completions + "," + Word(0xe3818) + "," + Word(0xe381a) + "," + string.Join(";", Array.ConvertAll(grid, delegate(int value) { return value.ToString(CultureInfo.InvariantCulture); })));
                    trace.Flush(); cases.Flush(); probe++;
                }
                GC.KeepAlive(random); GC.KeepAlive(movie); GC.KeepAlive(target); GC.KeepAlive(view); GC.KeepAlive(impact);
            }
            GC.KeepAlive(nativeRandom); GC.KeepAlive(nativeTarget);
            Console.WriteLine("Captured " + probe + " bounded opponent/target/phase probes. Media/view/impact are recording stubs; completion boundaries are harness-supplied.");
        } finally { VirtualFree(memory, UIntPtr.Zero, 0x8000); }
    }
    static int Main(string[] args) {
        try { if (args.Length != 2) return 2; new ClassicStageOrderOracle().Evaluate(args[0], args[1]); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
