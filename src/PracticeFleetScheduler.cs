using System;

namespace FleetLab {
    // B9A8...6A569:427670..4282B2 cursor/wrap,410E37..410E57 caller.
    // This is batch enumeration only. Fleet bodies, secondary pool, AI and
    // elapsed-time pacing belong to separate systems.
    public sealed class PracticeFleetBatch {
        readonly int[] slots;
        public PracticeFleetScheduler Next { get; private set; }
        internal PracticeFleetBatch(int[] visited, PracticeFleetScheduler next) {
            slots = visited; Next = next;
        }
        public int[] CopySlots() { return (int[])slots.Clone(); }
    }

    public sealed class PracticeFleetPoll {
        readonly int[] slots;
        public int BatchCount { get; private set; }
        public PracticeFleetScheduler Next { get; private set; }
        internal PracticeFleetPoll(int[] visited, int batches, PracticeFleetScheduler next) {
            slots = visited; BatchCount = batches; Next = next;
        }
        public int[] CopySlots() { return (int[])slots.Clone(); }
    }

    public sealed class PracticeFleetScheduler {
        public int BatchStart { get; private set; }
        public uint CompletedCycles { get; private set; }
        public PracticeFleetScheduler(int batchStart, uint completedCycles) {
            if (batchStart < 0 || batchStart > 248 || batchStart % 4 != 0)
                throw new ArgumentOutOfRangeException("batchStart");
            BatchStart = batchStart; CompletedCycles = completedCycles;
        }
        public PracticeFleetBatch TakeBatch() {
            int count = Math.Min(4, 250 - BatchStart);
            var slots = new int[count];
            for (int i = 0; i < count; i++) slots[i] = BatchStart + i;
            int next = BatchStart + 4; uint cycles = CompletedCycles;
            if (next >= 250) { next = 0; cycles = unchecked(cycles + 1); }
            return new PracticeFleetBatch(slots, new PracticeFleetScheduler(next, cycles));
        }
        public PracticeFleetPoll TakeWorldPoll(bool extraPasses) {
            // Raw source flag4AA7F8 chooses63additional calls. Its UI/input
            // lifecycle is not implied by this explicit argument.
            int batches = extraPasses ? 65 : 2, count = 0;
            var visited = new int[batches * 4]; var cursor = this;
            for (int i = 0; i < batches; i++) {
                PracticeFleetBatch batch = cursor.TakeBatch();
                int[] slots = batch.CopySlots();
                Array.Copy(slots, 0, visited, count, slots.Length);
                count += slots.Length; cursor = batch.Next;
            }
            Array.Resize(ref visited, count);
            return new PracticeFleetPoll(visited, batches, cursor);
        }
    }
}
