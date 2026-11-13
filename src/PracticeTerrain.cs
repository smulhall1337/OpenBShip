using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace FleetLab {
    public struct PracticeTerrainCell {
        public readonly int Slot, SourceValue, StoredValue;
        public readonly Point Origin;
        internal PracticeTerrainCell(int slot, int value, int stored, Point origin) {
            Slot = slot; SourceValue = value; StoredValue = stored; Origin = origin;
        }
    }

    // Supported Bs.exe B9A8...6A569:42B6A9 root IDs,402EE7..40303E scan.
    // The first Practice map has only the admitted1/2/4/7 cell kinds. The
    // separate kind5 pool and other maps require their own reconstruction.
    public sealed class PracticeTerrainPool {
        public const int Capacity = 500, RecordBytes = 492;
        readonly byte[] records = new byte[Capacity * RecordBytes];
        readonly ReadOnlyCollection<PracticeTerrainCell> cells;
        public ReadOnlyCollection<PracticeTerrainCell> Cells { get { return cells; } }
        public int Count { get { return cells.Count; } }

        public static PracticeTerrainPool FirstPractice(byte[] source) {
            if (source == null || source.Length != 6788)
                throw new InvalidDataException("Unsupported first Practice terrain length.");
            source = (byte[])source.Clone();
            string hash;
            using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(source)).Replace("-", "");
            if (hash != "BAA35BA0CF478F99290B390F51D37211D6BD99852325EFF4E15ECDA4394C9E9B")
                throw new InvalidDataException("Unsupported first Practice terrain identity.");
            return new PracticeTerrainPool(Scenario.Load(source, "BSSB201.SCN"));
        }

        internal PracticeTerrainPool(Scenario scene) {
            if (scene == null || scene.Cells == null || scene.Cells.Length != 4096)
                throw new InvalidDataException("Missing first Practice terrain.");
            for (int i = 0; i < Capacity; i++)
                BitConverter.GetBytes((short)i).CopyTo(records, i * RecordBytes + 4);
            var active = new List<PracticeTerrainCell>();
            for (int x = 0; x < 64; x++) for (int y = 0; y < 64; y++) {
                Cell cell = scene.At(x, y);
                if (cell == null || (cell.Kind != 1 && cell.Kind != 2 && cell.Kind != 4 && cell.Kind != 7))
                    throw new InvalidDataException("Unsupported first Practice terrain cell.");
                if (cell.Kind != 2) continue;
                if (active.Count == Capacity) throw new InvalidDataException("Terrain pool capacity exceeded.");
                int slot = active.Count, at = slot * RecordBytes;
                int value = unchecked((sbyte)cell.Value), stored = value == -1 ? 0 : value;
                BitConverter.GetBytes(1).CopyTo(records, at);
                BitConverter.GetBytes((short)stored).CopyTo(records, at + 6);
                BitConverter.GetBytes((short)(x * 10)).CopyTo(records, at + 12);
                BitConverter.GetBytes((short)(y * 10)).CopyTo(records, at + 14);
                // Word6 copies the signed SCN value. Its semantic meaning is
                // not inferred from its overlap with the fleet owner field.
                active.Add(new PracticeTerrainCell(slot, value, stored, new Point(x * 10, y * 10)));
            }
            if (active.Count != 54) throw new InvalidDataException("Unsupported first Practice terrain count.");
            cells = active.AsReadOnly();
        }

        public byte[] CopyRecords() { return (byte[])records.Clone(); }
    }
}
