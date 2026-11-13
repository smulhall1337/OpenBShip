using System;
using System.IO;
using System.Security.Cryptography;

namespace FleetLab {
    // Raw 40x20 effect records. The first word is a fleet reference, not the
    // player/board selector used by ClassicVisualEntry. All opaque bytes remain.
    // Supported RIP B9A8...6A569:40E0D0; 29 visual records at44E040.
    public sealed class PracticeVisualPool {
        readonly byte[] effects, visuals;
        public PracticeVisualPool(byte[] effectPool, byte[] visualRecords) {
            if (effectPool == null || effectPool.Length != 800 || visualRecords == null || visualRecords.Length != 580)
                throw new InvalidDataException("Visual stepping requires complete effect and visual pools.");
            string hash; using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(visualRecords)).Replace("-", "");
            if (hash != "CF815C9CD12610B8D775DE811992A490A8DA627998CA202AE1BF43563AFDE5C3")
                throw new InvalidDataException("Unsupported original visual table identity.");
            effects = (byte[])effectPool.Clone(); visuals = (byte[])visualRecords.Clone();
        }
        public static PracticeVisualPool Empty(byte[] visualRecords) {
            var pool = new byte[800];
            for (int at = 0; at < pool.Length; at += 20) BitConverter.GetBytes((short)-1).CopyTo(pool, at);
            return new PracticeVisualPool(pool, visualRecords);
        }
        public int Count {
            get { int count = 0; for (int at = 0; at < effects.Length; at += 20)
                    if (BitConverter.ToInt16(effects, at) != -1) count++; return count; }
        }
        public PracticeVisualPool Advance() {
            // Validate before writing so an unsupported later record cannot
            // publish an earlier counter change. Inactive records stay opaque.
            for (int at = 0; at < effects.Length; at += 20)
                if (BitConverter.ToInt16(effects, at) != -1) {
                    int type = BitConverter.ToInt32(effects, at + 16);
                    if (type < 0 || type >= 29) throw new InvalidDataException("Visual type outside the original table.");
                }
            byte[] next = CopyEffectPool();
            for (int at = 0; at < next.Length; at += 20) {
                if (BitConverter.ToInt16(next, at) == -1) continue;
                short counter = unchecked((short)(BitConverter.ToInt16(next, at + 12) + 1));
                BitConverter.GetBytes(counter).CopyTo(next, at + 12);
                int type = BitConverter.ToInt32(next, at + 16);
                if (counter > BitConverter.ToInt16(visuals, type * 20 + 12))
                    BitConverter.GetBytes((short)-1).CopyTo(next, at);
            }
            return new PracticeVisualPool(next, visuals);
        }
        public byte[] CopyEffectPool() { return (byte[])effects.Clone(); }
    }
}
