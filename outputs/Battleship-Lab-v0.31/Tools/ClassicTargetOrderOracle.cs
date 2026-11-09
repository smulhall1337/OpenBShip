using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

// Hash-locked x86 numerical experiment. A newly owned helper executes copied
// original opponent prefix, firing dispatcher, target selector and CRT bytes.
// Media admission and the following stage are recording callbacks. No original
// process, assets, registry, graphics or sound device is opened for execution.
class ClassicTargetOrderOracle {
    [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p, UIntPtr size, uint flags, uint protect);
    [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p, UIntPtr size, uint protect, out uint old);
    [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p, UIntPtr size, uint flags);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Select();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Call();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Seed(uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int RandomCall(int returnAddress);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Target(IntPtr x, IntPtr y);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int LoadMovie(int id, IntPtr owner);
    protected const string Identity = "B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569";
    protected const int TargetCode = 0x120000, Ship = 0x1ff020, Grid = 0xb47e0;
    protected byte[] source;
    protected int pe;
    protected uint imageBase;
    protected IntPtr memory;
    int Offset(uint va) {
        uint rva = va - imageBase;
        int table = pe + 24 + BitConverter.ToUInt16(source, pe + 20);
        for (int i = 0; i < BitConverter.ToUInt16(source, pe + 6); i++) {
            int at = table + i * 40;
            uint start = BitConverter.ToUInt32(source, at + 12), size = BitConverter.ToUInt32(source, at + 16);
            if (rva >= start && rva - start < size)
                return checked((int)(BitConverter.ToUInt32(source, at + 20) + rva - start));
        }
        throw new InvalidDataException("Address outside raw PE sections");
    }
    protected byte[] Read(uint va, int length) {
        byte[] bytes = new byte[length]; Array.Copy(source, Offset(va), bytes, 0, length); return bytes;
    }
    protected uint Address(int offset) { return unchecked((uint)memory.ToInt32() + (uint)offset); }
    protected void Write(int offset, byte[] bytes) { Marshal.Copy(bytes, 0, IntPtr.Add(memory, offset), bytes.Length); }
    protected static string Hash(byte[] bytes) {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
    }
    void Patch(byte[] bytes, int offset, uint expected, uint replacement) {
        if (BitConverter.ToUInt32(bytes, offset) != expected) throw new InvalidDataException("Target relocation differs at " + offset.ToString("X"));
        BitConverter.GetBytes(replacement).CopyTo(bytes, offset);
    }
    protected void CopyPrefix(uint va, int length, int expected) {
        byte[] bytes = Read(va, length); int changes = 0;
        var globals = new HashSet<uint>(new uint[] { 0x4fb3d8, 0x4aa7fc, 0x55e810, 0x4b47a0, 0x4169d0, 0x41698c, 0x4e3814, 0x44d730, 0x44d750, 0x44d760, 0x4b4b60, 0x4e42d0, 0x4b499c, 0x4fb6f4, 0x4ae818, 0x4ae81a, 0x4e375c, 0x4fbc4c, 0x454d50 });
        for (int at = 0; at <= bytes.Length - 4; at++) {
            uint operand = BitConverter.ToUInt32(bytes, at);
            bool table = va == 0x416640 && at >= 0x34c && at < 0x390 && operand >= 0x416640 && operand <= 0x416988;
            if (!globals.Contains(operand) && !table) continue;
            BitConverter.GetBytes(Address((int)(operand - 0x400000))).CopyTo(bytes, at); changes++; at += 3;
        }
        if (changes != expected) throw new InvalidDataException("Prefix relocation count differs at " + va.ToString("X8"));
        Write((int)(va - 0x400000), bytes);
    }
    protected void CopyTarget() {
        // Same five absolute operands, four calls and ten table entries as the
        // independently established ClassicTargetOracle. Internal branches move
        // together. Only external rand calls require new relative displacements.
        byte[] bytes = Read(0x41c940, 0x348);
        Patch(bytes, 3, 0x4e42d0, Address(0xe42d0));
        Patch(bytes, 0x24, 0x4b498c, Address(0xb498c));
        Patch(bytes, 0x13e, 0x4fbdf4, Address(0xfbdf4));
        Patch(bytes, 0x14e, 0x41cc60, Address(TargetCode + 0x320));
        Patch(bytes, 0x266, 0x41cc70, Address(TargetCode + 0x330));
        foreach (uint call in new uint[] { 0x41ca94, 0x41cb4b, 0x41cb5b, 0x41cb8d }) {
            int at = (int)(call - 0x41c940);
            if (bytes[at] != 0xe8) throw new InvalidDataException("Target RNG opcode differs");
            Patch(bytes, at + 1, unchecked(0x439790 - (call + 5)), unchecked(Address(0x39790) - Address(TargetCode + at + 5)));
        }
        for (int i = 0; i < 10; i++) {
            int at = 0x320 + i * 4; uint original = BitConverter.ToUInt32(bytes, at);
            if (original < 0x41c940 || original >= 0x41cc5e) throw new InvalidDataException("Target jump table leaves source span");
            Patch(bytes, at, original, Address(TargetCode + (int)(original - 0x41c940)));
        }
        Write(TargetCode, bytes);
    }
    protected void Stub(int at, Delegate callback) {
        var bytes = new List<byte> { 0xb8 };
        bytes.AddRange(BitConverter.GetBytes(Marshal.GetFunctionPointerForDelegate(callback).ToInt32()));
        bytes.AddRange(new byte[] { 0xff, 0xe0 }); Write(at, bytes.ToArray());
    }
    protected static int[] Fixture(string name) {
        int[] grid = new int[100];
        if (name == "masked") for (int i = 0; i < 100; i++) grid[i] = i % 3 == 0 ? 0 : i % 3 == 1 ? 2 : 4;
        else if (name == "exhausted" || name == "sparse") {
            for (int i = 0; i < 100; i++) grid[i] = 1;
            if (name == "sparse") grid[25] = 0;
        }
        else if (name == "single-low") grid[30] = 8;
        else if (name == "single-high") grid[39] = 8;
        else if (name == "chain-negative" || name == "chain-positive") {
            grid[64] = grid[65] = 8;
            if (name == "chain-positive") grid[63] = 1;
        }
        else if (name == "pattern") {
            for (int x = 0; x < 10; x++) for (int y = 0; y < 10; y++)
                if ((x + y) % 3 == 0) grid[x * 10 + y] = 1;
        }
        else if (name != "empty") throw new ArgumentException("Unknown fixture");
        // Deliberately excludes the native isolated-hit/no-open-Y infinite loop.
        return grid;
    }
    void Run(string exe, string destination) {
        source = File.ReadAllBytes(exe);
        if (IntPtr.Size != 4 || Hash(source) != Identity) throw new InvalidDataException("Matching image and x86 required");
        pe = BitConverter.ToInt32(source, 60); imageBase = BitConverter.ToUInt32(source, pe + 52);
        if (imageBase != 0x400000) throw new InvalidDataException("Unexpected source image base");
        Directory.CreateDirectory(destination);
        using (var provenance = new StreamWriter(Path.Combine(destination, "Native-Target-Order-Provenance.csv"))) {
            provenance.WriteLine("SourceSHA256,VA,Bytes,SHA256,Role");
            uint[] starts = { 0x41c8c0, 0x416640, 0x41c940, 0x439790, 0x439780, 0x44d730 };
            int[] lengths = { 0x7b, 0x3b2, 0x348, 0x2a, 10, 0x48 };
            string[] roles = { "Opponent prefix", "Firing dispatcher and tables", "Target selector and tables", "CRT rand", "CRT srand including RET", "Weapon lists" };
            for (int i = 0; i < starts.Length; i++)
                provenance.WriteLine(Identity + "," + starts[i].ToString("X8") + "," + lengths[i] + "," + Hash(Read(starts[i], lengths[i])) + "," + roles[i]);
        }
        memory = VirtualAlloc(IntPtr.Zero, new UIntPtr(0x200000), 0x3000, 4);
        if (memory == IntPtr.Zero) throw new Exception("Allocation failed");
        try {
            CopyPrefix(0x416640, 0x3b2, 24); CopyPrefix(0x41c8c0, 0x7b, 11);
            CopyPrefix(0x439790, 0x2a, 2); CopyPrefix(0x439780, 10, 1); CopyTarget();
            byte[] rand = new byte[0x2a], srand = new byte[10];
            Marshal.Copy(IntPtr.Add(memory, 0x39790), rand, 0, rand.Length);
            Marshal.Copy(IntPtr.Add(memory, 0x39780), srand, 0, srand.Length);
            Write(0x1000, rand); Write(0x1100, srand); Write(0x4d730, Read(0x44d730, 0x48));
            Marshal.WriteInt32(memory, 0xb499c, unchecked((int)Address(Ship)));
            Marshal.WriteByte(memory, 0xb4b60, 0);
            Select nativeRandom = null; Target nativeTarget = null;
            int probe = 0, order = 0, movieId = -1, targetX = -1, targetY = -1, calls = 0;
            using (var trace = new StreamWriter(Path.Combine(destination, "Native-Target-Order-Draws.csv")))
            using (var cases = new StreamWriter(Path.Combine(destination, "Native-Target-Order-Cases.csv"))) {
                trace.WriteLine("Probe,Order,Kind,CallerVA,Value,StateAfter,X,Y");
                cases.WriteLine("Probe,Case,Level,Seed,ShipType,Weapon,Movie,X,Y,RandomCalls,FinalState,CellsXMajor");
                Action<string, uint, int> record = delegate(string kind, uint caller, int value) {
                    trace.WriteLine(probe + "," + (order++) + "," + kind + "," + caller.ToString("X8") + "," + value + "," + unchecked((uint)Marshal.ReadInt32(memory, 0x54d50)) + "," + targetX + "," + targetY);
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
                LoadMovie movie = delegate(int id, IntPtr owner) { movieId = id; record("MovieRequest", 0, id); return 0; };
                Target target = delegate(IntPtr x, IntPtr y) {
                    record("TargetEnter", 0x41c91b, Marshal.ReadInt32(memory, 0xfb6f4));
                    nativeTarget(x, y); targetX = Marshal.ReadInt16(x); targetY = Marshal.ReadInt16(y);
                    record("TargetReturn", 0x41c91b, 0);
                };
                Call stage = delegate { record("NextStageStub", 0x41c933, 0); };
                Stub(0x2460, movie); Stub(0x1c940, target); Stub(0x23540, stage);
                foreach (int page in new int[] { 0x1000, 0x16000, 0x1c000, 0x23000, 0x39000, 0x2000, TargetCode }) {
                    uint old; if (!VirtualProtect(IntPtr.Add(memory, page), new UIntPtr(4096), 0x20, out old)) throw new Exception("Code protection failed");
                }
                nativeRandom = (Select)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory, 0x1000), typeof(Select));
                nativeTarget = (Target)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory, TargetCode), typeof(Target));
                var seed = (Seed)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory, 0x1100), typeof(Seed));
                var opponent = (Call)Marshal.GetDelegateForFunctionPointer(IntPtr.Add(memory, 0x1c8c0), typeof(Call));
                foreach (uint inputSeed in new uint[] { 0, 1, 731, uint.MaxValue })
                foreach (int type in new int[] { 7, 1, 2, 12, 4 })
                for (int level = 0; level < 4; level++)
                foreach (string name in new string[] { "empty", "masked", "exhausted", "sparse", "single-low", "single-high", "chain-negative", "chain-positive", "pattern" }) {
                    int[] grid = Fixture(name); order = calls = 0; movieId = targetX = targetY = -1;
                    seed(inputSeed); Marshal.WriteInt32(memory, Ship, type); Marshal.WriteInt32(memory, 0xfbdf4, level);
                    Marshal.Copy(grid, 0, IntPtr.Add(memory, Grid), grid.Length);
                    Marshal.WriteInt16(memory, 0xae81a, -1); Marshal.WriteInt16(memory, 0xae818, -1);
                    record("Begin", 0, 0); opponent(); record("Boundary", 0, 0);
                    cases.WriteLine(probe + "," + name + "," + level + "," + inputSeed + "," + type + "," + Marshal.ReadInt32(memory, 0xfb6f4) + "," + movieId + "," + targetX + "," + targetY + "," + calls + "," + unchecked((uint)Marshal.ReadInt32(memory, 0x54d50)) + "," + string.Join(";", Array.ConvertAll(grid, delegate(int v) { return v.ToString(CultureInfo.InvariantCulture); })));
                    trace.Flush(); cases.Flush(); probe++;
                }
                GC.KeepAlive(random); GC.KeepAlive(movie); GC.KeepAlive(target); GC.KeepAlive(stage);
            }
            GC.KeepAlive(nativeRandom); GC.KeepAlive(nativeTarget);
            Console.WriteLine("Captured " + probe + " bounded opponent-prefix/real-target probes. Media and following stage remain stubs.");
        } finally { VirtualFree(memory, UIntPtr.Zero, 0x8000); }
    }
    static int Main(string[] args) {
        try { if (args.Length != 2) return 2; new ClassicTargetOrderOracle().Run(args[0], args[1]); return 0; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
