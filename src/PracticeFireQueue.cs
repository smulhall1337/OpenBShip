using System;
using System.IO;

namespace FleetLab {
    public sealed class PracticeFireQueueResult {
        readonly byte[] orders;
        public int ReturnCode { get; private set; }
        public int SelectedSlot { get; private set; }
        public bool ClearedCoordinates { get; private set; }
        internal PracticeFireQueueResult(byte[] pool, int code, int slot, bool cleared) {
            orders = pool; ReturnCode = code; SelectedSlot = slot; ClearedCoordinates = cleared;
        }
        public byte[] CopyOrderPool() { return (byte[])orders.Clone(); }
    }

    // Supported RIP B9A8...6A569:4162A0 enqueue and416250 clear, under
    // network0/messageflag0/selectedUI0. This stores requests; it does not admit
    // or execute a shot. Source416090's active-order consumer remains separate.
    // 1500x52 records: DWORD0 order kind, DWORD4 preserved timer, ten X words
    // at8..27, ten Y words at28..47, normalized target-fleet reference at48.
    public static class PracticeFireQueue {
        static void Check(byte[] orders, int source) {
            if (orders == null || orders.Length != 78000) throw new InvalidDataException("Firing queue requires the complete 1500×52 order pool.");
            if (source < 0 || source >= 1500) throw new ArgumentOutOfRangeException("source");
        }
        static void ClearInPlace(byte[] orders, int at) {
            // Source clears only X sentinels, preserving Y, timer, kind and ref.
            for (int n = 0; n < 10; n++) BitConverter.GetBytes((short)-1).CopyTo(orders,at+8+n*2);
        }
        public static byte[] Clear(byte[] orderPool, int sourceShip) {
            Check(orderPool,sourceShip); byte[] result = (byte[])orderPool.Clone(); ClearInPlace(result,sourceShip*52); return result;
        }
        public static PracticeFireQueueResult Enqueue(byte[] shipPool, byte[] fleetPool, byte[] orderPool,
                int sourceShip, int orderKind, int targetFleet, int x, int y) {
            Check(orderPool,sourceShip);
            if (shipPool == null || shipPool.Length != 402000 || fleetPool == null || fleetPool.Length != 123000)
                throw new InvalidDataException("Firing queue requires complete ship and fleet pools.");
            if (targetFleet < 0 || targetFleet >= 250) throw new ArgumentOutOfRangeException("targetFleet");
            if (orderKind < 0 || orderKind > 34) throw new NotSupportedException("This raw firing-order kind is outside the admitted data helper.");
            if (x < short.MinValue || x > short.MaxValue || y < short.MinValue || y > short.MaxValue)
                throw new NotSupportedException("Firing queue coordinates must fit the admitted signed16 storage span.");
            if (BitConverter.ToInt16(shipPool,sourceShip*268+4) != sourceShip)
                throw new InvalidDataException("Firing queue source identity differs from its original slot.");
            byte[] orders = (byte[])orderPool.Clone(); int at = sourceShip*52; bool cleared = false;
            if (BitConverter.ToInt32(orders,at) != orderKind) { ClearInPlace(orders,at); cleared = true; }
            // Preserve this order: an empty first X (including after a kind
            // change) does not dereference the old target, which may be opaque.
            if (BitConverter.ToInt16(orders,at+8) != -1) {
                long delta = (long)BitConverter.ToUInt32(orders,at+48)-0x4B47A0;
                if (delta < 0 || delta >= 123000 || delta%492 != 0)
                    throw new InvalidDataException("Active firing queue has an unsupported target reference.");
                int previous = (int)(delta/492);
                // Source compares word IDs, not pointer identity. Preserve
                // alias-ID contexts rather than assuming ID==pool index.
                if (BitConverter.ToInt16(fleetPool,previous*492+4) != BitConverter.ToInt16(fleetPool,targetFleet*492+4)) {
                    ClearInPlace(orders,at); cleared = true;
                }
            }
            int slot;
            for (slot = 0; slot < 10; slot++) if (BitConverter.ToInt16(orders,at+8+slot*2) == -1) break;
            if (slot == 10) return new PracticeFireQueueResult(orders,1,-1,cleared);
            BitConverter.GetBytes(orderKind).CopyTo(orders,at);
            BitConverter.GetBytes((short)x).CopyTo(orders,at+8+slot*2);
            BitConverter.GetBytes((short)y).CopyTo(orders,at+28+slot*2);
            BitConverter.GetBytes(0x4B47A0+targetFleet*492).CopyTo(orders,at+48);
            // Coordinates are raw: X=-1 still writes the empty sentinel. No
            // clamping, deduplication, ammunition or range checks occur here.
            return new PracticeFireQueueResult(orders,slot==9?12:0,slot,cleared);
        }
    }
}
