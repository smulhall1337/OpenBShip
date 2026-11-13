using System;
using System.Numerics;

namespace FleetLab {
    // Finite integer-coordinate path of RIP B9A8...6A569:4453F0/445430,
    // immediately truncated by439A8C in416B37..416B3F. This is deliberately
    // narrower than a general CRT or IEEE floating-point implementation.
    // The CRT temporarily uses64bit x87 precision, with53bit memory stores.
    // Its y division remains in a register for multiplication by stored y;
    // x is reloaded from memory. Direct Sqrt(x*x+y*y) changes some deadlines.
    public static class OriginalTimedDistance {
        struct Binary {
            public BigInteger Significand;
            public int Exponent;
            public Binary(BigInteger n, int e) { Significand = n; Exponent = e; }
        }
        static int Bits(BigInteger n) {
            if (n.IsZero) return 0;
            byte[] b = n.ToByteArray(); int last = b.Length - 1;
            while (b[last] == 0) last--;
            int bits = last * 8; byte top = b[last];
            while (top != 0) { top >>= 1; bits++; } return bits;
        }
        static Binary Round(Binary value, int precision) {
            int shift = Bits(value.Significand) - precision;
            if (shift <= 0) return value;
            BigInteger q = value.Significand >> shift;
            BigInteger remainder = value.Significand - (q << shift), half = BigInteger.One << (shift - 1);
            if (remainder > half || (remainder == half && !q.IsEven)) q++;
            int exponent = value.Exponent + shift;
            if (Bits(q) > precision) { q >>= 1; exponent++; }
            return new Binary(q, exponent);
        }
        static Binary Ratio64(int numerator, int denominator) {
            if (numerator == 0) return new Binary(BigInteger.Zero, 0);
            BigInteger n = numerator, d = denominator;
            int exponent = Bits(n) - Bits(d);
            if (exponent >= 0 ? n < (d << exponent) : (n << -exponent) < d) exponent--;
            int shift = 63 - exponent; BigInteger remainder;
            BigInteger q = BigInteger.DivRem(n << shift, d, out remainder);
            int comparison = (remainder * 2).CompareTo(d);
            if (comparison > 0 || (comparison == 0 && !q.IsEven)) q++;
            return new Binary(q, exponent - 63);
        }
        static Binary Multiply64(Binary a, Binary b) {
            return Round(new Binary(a.Significand * b.Significand, a.Exponent + b.Exponent), 64);
        }
        static Binary Add64(Binary a, Binary b) {
            int exponent = Math.Min(a.Exponent, b.Exponent);
            return Round(new Binary((a.Significand << (a.Exponent - exponent)) +
                                    (b.Significand << (b.Exponent - exponent)), exponent), 64);
        }
        static Binary Sqrt64(Binary normalizedSum) {
            // Normalized sum is in[1,2], so sqrt has exponent0. Scaling by
            // 2^126 converts rounding its64bit significand to integer sqrt.
            int shift = normalizedSum.Exponent + 126;
            if (shift < 0) throw new InvalidOperationException("Distance normalization invariant.");
            BigInteger squared = normalizedSum.Significand << shift;
            BigInteger q = BigInteger.One << ((Bits(squared) + 1) / 2);
            for (;;) { BigInteger next = (q + squared / q) >> 1; if (next >= q) break; q = next; }
            BigInteger midpoint = 2 * q + 1;
            int comparison = (4 * squared).CompareTo(midpoint * midpoint);
            if (comparison > 0 || (comparison == 0 && !q.IsEven)) q++;
            return new Binary(q, -63);
        }
        public static int Truncated(int differenceX, int differenceY) {
            // Admitted source: signed16 world positions and signed16 target
            // offsets. This bound includes their largest possible difference.
            if (differenceX < -98303 || differenceX > 98303) throw new ArgumentOutOfRangeException("differenceX");
            if (differenceY < -98303 || differenceY > 98303) throw new ArgumentOutOfRangeException("differenceY");
            int x = Math.Abs(differenceX), y = Math.Abs(differenceY), largest = Math.Max(x, y);
            if (largest == 0) return 0;
            Binary xMemory = Round(Ratio64(x, largest), 53);
            Binary yRegister = Ratio64(y, largest), yMemory = Round(yRegister, 53);
            Binary sum = Round(Add64(Multiply64(yRegister, yMemory), Multiply64(xMemory, xMemory)), 53);
            Binary root = Round(Sqrt64(sum), 53);
            // frexp/ldexp only change powers of two on this bounded path.
            Binary result = Round(Multiply64(root, new Binary(largest, 0)), 53);
            BigInteger integer = result.Exponent >= 0 ? result.Significand << result.Exponent : result.Significand >> -result.Exponent;
            return (int)integer;
        }
    }
}
