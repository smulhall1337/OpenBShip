using System;

namespace FleetLab {
    // Supported RIP SHA256 B9A8...6A569 only: 4109F0 mode0 header,
    // 410A0C..410A5F/410D04 and the 410D77..410D97 wait predicate.
    // This explicit, immutable model is for reconstruction comparisons. The
    // modern game does not schedule playback or simulation through it.
    public sealed class OriginalFrameClock {
        public uint PreviousMilliseconds { get; private set; } // 5C3340
        public uint CurrentMilliseconds { get; private set; }  // 5C3348
        public uint GameMilliseconds { get; private set; }     // 4FBE60
        public uint DeltaMilliseconds { get; private set; }    // 4AE824

        public OriginalFrameClock(uint previous, uint current, uint game, uint delta) {
            PreviousMilliseconds = previous; CurrentMilliseconds = current;
            GameMilliseconds = game; DeltaMilliseconds = delta;
        }

        // The source samples once BEFORE calling the world body. That body
        // observes this sample but still observes the preceding frame delta.
        // The signed network-mode comparison is intentional.
        public OriginalFrameClock BeginFrame(uint sampledMilliseconds, int networkMode) {
            return new OriginalFrameClock(CurrentMilliseconds, sampledMilliseconds,
                networkMode <= 1 ? sampledMilliseconds : GameMilliseconds, DeltaMilliseconds);
        }

        // Call on the body-visible state after an admitted body that preserves
        // these clock fields. Subtraction wraps at32bits. The zero branch also
        // resets both stored clocks to1, even in network mode>1. This source
        // quirk can create a large next delta and is deliberately retained here.
        public OriginalFrameClock CompleteFrame() {
            uint delta = unchecked(CurrentMilliseconds - PreviousMilliseconds);
            return delta == 0
                ? new OriginalFrameClock(PreviousMilliseconds, 1, 1, 1)
                : new OriginalFrameClock(PreviousMilliseconds, CurrentMilliseconds, GameMilliseconds, delta);
        }

        // The original busy loop compares SIGNED subtraction with40. Exposing
        // a predicate avoids introducing a busy wait or a live timer dependency.
        public bool WorldWaitSatisfied(uint observedMilliseconds) {
            return unchecked((int)(observedMilliseconds - PreviousMilliseconds)) >= 40;
        }
    }
}
