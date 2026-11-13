using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace FleetLab {
    // Data from the supported B9A8...6A569 executable. No executable code is
    // loaded here. Setup extracts these five small, hash-pinned table regions.
    public sealed class PracticeUnitCatalog {
        internal readonly byte[] Templates, Ammunition, Offense, Colors, Markers;
        public const int TemplateCount = 15;
        public const int RecordBytes = 268;

        PracticeUnitCatalog(byte[] templates, byte[] ammunition, byte[] offense,
                            byte[] colors, byte[] markers) {
            Templates = Check(templates, 4020, "C861FE8C683C6E6918AD78185F8348903BF16726E77B17A7E177C8782AB01B33", "unit templates");
            Ammunition = Check(ammunition, 80, "70F5B7233E5FE38F5418FEBC502B4C1FD25020D8CACE907E4EB1BA1F7D5A6322", "ammunition table");
            Offense = Check(offense, 72, "4B86D59C0CB7BEEA2A9FBAE3521CA7D697D79DCEFCD7B2F99963D496C631ADC4", "weapon priorities");
            Colors = Check(colors, 33, "344B28FBC02F85C970ADF39F113810F6A07EE0382E3093E91CF28CDEFB67BDAE", "fleet colors");
            Markers = Check(markers, 30, "0307AF7C553F7D7D9CD6EA80F0ECE3142D06ECE493CA11A2640F6FAF96F6CB14", "unit markers");
        }

        static byte[] Check(byte[] data, int length, string identity, string name) {
            if (data == null || data.Length != length)
                throw new InvalidDataException("Unsupported " + name + " length. Run Setup again.");
            string hash;
            using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
            if (hash != identity)
                throw new InvalidDataException("Unsupported " + name + " identity. Run Setup again.");
            return (byte[])data.Clone();
        }

        public static PracticeUnitCatalog Read(byte[] templates, byte[] ammunition,
                                                byte[] offense, byte[] colors, byte[] markers) {
            return new PracticeUnitCatalog(templates, ammunition, offense, colors, markers);
        }

        public static PracticeUnitCatalog Load(string programs) {
            return Read(File.ReadAllBytes(Path.Combine(programs, "UnitTemplates.seqbin")),
                        File.ReadAllBytes(Path.Combine(programs, "UnitAmmunition.seqbin")),
                        File.ReadAllBytes(Path.Combine(programs, "UnitOffense.seqbin")),
                        File.ReadAllBytes(Path.Combine(programs, "FleetColors.seqbin")),
                        File.ReadAllBytes(Path.Combine(programs, "UnitMarkerMap.seqbin")));
        }
    }

    public sealed class PracticeUnitRecord {
        readonly byte[] data;
        internal PracticeUnitRecord(byte[] pool, int slot) {
            data = new byte[PracticeUnitCatalog.RecordBytes];
            Array.Copy(pool, slot * data.Length, data, 0, data.Length);
        }
        public int Type { get { return BitConverter.ToInt32(data, 0); } }
        public int Slot { get { return BitConverter.ToInt16(data, 4); } }
        public int Owner { get { return BitConverter.ToInt16(data, 6); } }
        public int X { get { return BitConverter.ToInt16(data, 8); } }
        public int Y { get { return BitConverter.ToInt16(data, 10); } }
        public int Length { get { return BitConverter.ToInt16(data, 14); } }
        public int HitPoints { get { return BitConverter.ToInt16(data, 16); } }
        public int Speed { get { return BitConverter.ToInt16(data, 72); } }
        public int SelectedEquipmentA { get { return BitConverter.ToInt32(data, 76); } }
        public int SelectedEquipmentB { get { return BitConverter.ToInt32(data, 80); } }
        public int SelectedEquipmentC { get { return BitConverter.ToInt32(data, 84); } }
        public int Ammunition(int weapon) {
            if (weapon < 0 || weapon >= 35) throw new ArgumentOutOfRangeException("weapon");
            return BitConverter.ToInt16(data, 196 + weapon * 2);
        }
        public byte[] CopyRecord() { return (byte[])data.Clone(); }
    }

    // Bounded fresh-pool constructor, recovered from 403210, 42CFE0, 42D070,
    // 42D120 and the color/speed/grid finalizers. World position is supplied;
    // this constructor does not execute spawning, mission logic or local UI code.
    public sealed class PracticeFleetSnapshot {
        public const int ShipSlots = 1500, FleetSlots = 250, FleetBytes = 492;
        readonly byte[] ships = new byte[ShipSlots * PracticeUnitCatalog.RecordBytes];
        readonly byte[] fleet = new byte[FleetBytes];
        readonly byte[] payload;
        readonly ReadOnlyCollection<PracticeUnitRecord> units;
        public ReadOnlyCollection<PracticeUnitRecord> Units { get { return units; } }
        public int Owner { get; private set; }
        public int Kind { get; private set; }
        public int FleetSlot { get; private set; }
        public int FirstShipSlot { get; private set; }
        public Point WorldOrigin { get; private set; }
        public uint InitialCounter { get; private set; }
        public uint NextCounter { get; private set; }

        static int Word(byte[] bytes, int at) { return BitConverter.ToInt16(bytes, at); }
        static int DWord(byte[] bytes, int at) { return BitConverter.ToInt32(bytes, at); }
        static void Word(byte[] bytes, int at, int value) {
            BitConverter.GetBytes(unchecked((short)value)).CopyTo(bytes, at);
        }
        static void DWord(byte[] bytes, int at, int value) {
            BitConverter.GetBytes(value).CopyTo(bytes, at);
        }

        PracticeFleetSnapshot(PracticeUnitCatalog catalog, byte[] source, int owner,
                              Point origin, uint counter, int kind = 2,
                              int fleetSlot = 0, int firstShipSlot = 0) {
            if (catalog == null) throw new ArgumentNullException("catalog");
            if (source == null || source.Length != 400)
                throw new InvalidDataException("A fleet record must contain exactly 400 bytes.");
            if (owner < 0 || owner > 3) throw new ArgumentOutOfRangeException("owner");
            if (kind != 1 && kind != 2 && kind != 9) throw new ArgumentOutOfRangeException("kind");
            if (fleetSlot < 0 || fleetSlot >= FleetSlots || firstShipSlot < 0 || firstShipSlot >= ShipSlots)
                throw new ArgumentOutOfRangeException("fleetSlot");
            if (origin.X < short.MinValue || origin.X > short.MaxValue ||
                origin.Y < short.MinValue || origin.Y > short.MaxValue)
                throw new ArgumentOutOfRangeException("origin");
            Owner = owner;
            Kind = kind;
            FleetSlot = fleetSlot;
            FirstShipSlot = firstShipSlot;
            WorldOrigin = origin;
            InitialCounter = counter;
            NextCounter = unchecked(counter + 1);
            payload = (byte[])source.Clone();
            DWord(fleet, 0, kind);
            Word(fleet, 6, owner);
            Word(fleet, 12, origin.X);
            Word(fleet, 14, origin.Y);
            fleet[63] = kind == 1 ? (byte)0 : (byte)255;
            fleet[484] = unchecked((byte)counter);
            fleet[485] = 2;

            var records = new List<PracticeUnitRecord>();
            int[] markerTypes = { 0, 12, 1, 7, 3, 2, 6, 5, 14, 4, 11 };
            for (int row = 0; row < 10; row++) {
                for (int column = 0; column < 10; column++) {
                    int cell = (row * 10 + column) * 4;
                    int code = unchecked((sbyte)payload[cell]);
                    if (code < 1 || code > catalog.Markers.Length || catalog.Markers[code - 1] >= markerTypes.Length)
                        throw new InvalidDataException("Unsupported fleet marker " + code + " at " + row + "," + column + ".");
                    int type = markerTypes[catalog.Markers[code - 1]];
                    if (type == 0) continue;
                    if (records.Count == 6)
                        throw new InvalidDataException("This fleet record exceeds the six-slot roster.");
                    for (int x = column; x < 10 && payload[(row * 10 + x) * 4] != 1; x++)
                        payload[(row * 10 + x) * 4] = 1;
                    int slot = firstShipSlot + records.Count;
                    if (slot >= ShipSlots) throw new InvalidDataException("Deployment exceeds the ship pool.");
                    Construct(catalog, type, slot, row, column);
                    DWord(fleet, 16 + records.Count * 4, 0x4fc340 + slot * PracticeUnitCatalog.RecordBytes);
                    records.Add(new PracticeUnitRecord(ships, slot));
                }
            }

            Array.Copy(catalog.Colors, kind * 3, fleet, 464, 3);
            int speed = 0;
            foreach (var record in records) {
                if (speed == 0) speed = unchecked((byte)record.Speed);
                if (speed > record.Speed) speed = unchecked((byte)record.Speed);
            }
            Word(fleet, 60, speed);
            for (int i = 0; i < 100; i++) DWord(fleet, 64 + i * 4, kind == 2 ? 3 : 0);
            foreach (var record in records) {
                for (int n = 0; n < record.Length; n++)
                    DWord(fleet, 64 + (record.X * 10 + record.Y + n) * 4, 2);
            }
            units = records.AsReadOnly();
        }

        void Construct(PracticeUnitCatalog catalog, int type, int slot, int row, int column) {
            if (type < 1 || type >= PracticeUnitCatalog.TemplateCount)
                throw new InvalidDataException("Unsupported unit template " + type + ".");
            int at = slot * PracticeUnitCatalog.RecordBytes;
            Array.Copy(catalog.Templates, type * PracticeUnitCatalog.RecordBytes, ships, at, PracticeUnitCatalog.RecordBytes);
            int length = Word(ships, at + 14);
            if (DWord(ships, at) != type || length < 1 || length > 6 || column + length > 10)
                throw new InvalidDataException("Unsupported unit shape or placement.");
            Word(ships, at + 4, slot);
            Word(ships, at + 6, Owner);
            Word(ships, at + 8, row);
            Word(ships, at + 10, column);
            Word(ships, at + 12, FleetSlot);

            for (int i = 0; i < 35; i++) Word(ships, at + 196 + i * 2, 0);
            var equipment = new List<int>();
            for (int i = 0; i < length * 4; i++) {
                int weapon = DWord(ships, at + 100 + i * 4);
                if (weapon < 0 || weapon > 39) throw new InvalidDataException("Unsupported unit equipment.");
                equipment.Add(weapon);
                int amount = Word(catalog.Ammunition, weapon * 2);
                int destination = at + 196 + weapon * 2;
                // Source IDs35..39 make zero-additions, including into the next
                // slot. Keep the original pool layout and preserve those bytes.
                Word(ships, destination, amount == -1 ? -1 : Word(ships, destination) + amount);
            }

            var offense = new HashSet<int>();
            for (int i = 0; i < catalog.Offense.Length; i += 4) {
                int weapon = DWord(catalog.Offense, i);
                if (weapon == 0) break;
                offense.Add(weapon);
            }
            foreach (int field in new[] { 76, 80, 84 }) {
                int current = DWord(ships, at + field);
                bool present = current != 0 && equipment.Contains(current) &&
                               (field == 80 || Word(ships, at + 196 + current * 2) != 0);
                if (present) continue;
                foreach (int weapon in equipment) {
                    bool eligible = field == 76 ? weapon >= 4 && weapon <= 8 && Word(ships, at + 196 + weapon * 2) != 0 :
                                    field == 80 ? weapon >= 9 && weapon <= 13 :
                                    offense.Contains(weapon) && Word(ships, at + 196 + weapon * 2) != 0;
                    if (eligible) { DWord(ships, at + field, weapon); break; }
                }
            }
        }

        public static PracticeFleetSnapshot Create(PracticeUnitCatalog catalog, byte[] payload,
                                                    int owner, Point worldOrigin, uint initialCounter) {
            return new PracticeFleetSnapshot(catalog, payload, owner, worldOrigin, initialCounter);
        }
        internal static PracticeFleetSnapshot CreateDeployment(PracticeUnitCatalog catalog, byte[] payload,
                int owner, Point origin, uint counter, int kind, int fleetSlot, int firstShipSlot) {
            return new PracticeFleetSnapshot(catalog, payload, owner, origin, counter, kind, fleetSlot, firstShipSlot);
        }
        public int Occupancy(int x, int y) {
            if (x < 0 || x >= 10 || y < 0 || y >= 10) throw new ArgumentOutOfRangeException("x");
            return DWord(fleet, 64 + (x * 10 + y) * 4);
        }
        public int Speed { get { return Word(fleet, 60); } }
        public byte[] CopyShipPool() { return (byte[])ships.Clone(); }
        public byte[] CopyFleetRecord() { return (byte[])fleet.Clone(); }
        public byte[] CopyFleetPool() {
            var result = new byte[FleetSlots * FleetBytes];
            Array.Copy(fleet, 0, result, FleetSlot * FleetBytes, fleet.Length);
            return result;
        }
        public byte[] CopyConsumedPayload() { return (byte[])payload.Clone(); }
    }
}
