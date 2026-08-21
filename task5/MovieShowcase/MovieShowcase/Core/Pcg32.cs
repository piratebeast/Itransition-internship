using System.Numerics;
using System.Runtime.CompilerServices;

namespace MovieShowcase.Core;

public sealed class Pcg32
{
    private const ulong Mult = 6364136223846793005UL;
    private ulong _state;
    private ulong _inc;

    public Pcg32(ulong seed, ulong seq = 54UL) 
    {
        _state = 0UL;
        _inc = (seq << 1) | 1UL;      // must be odd
        Step();
        unchecked { _state += seed; }
        Step();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Step() { unchecked { _state = _state * Mult + _inc; } }

    public uint NextUInt() 
    { 
        ulong old = _state;

        Step();

        uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);

        int rot = (int)(old >> 59);

        return BitOperations.RotateRight(xorshifted, rot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double NextDouble() => NextUInt() / 4294967296.0;
    public int NextInt(int lo, int hi) 
    {
        if (hi  <= lo ) throw new ArgumentOutOfRangeException(nameof(hi));

        uint span = (uint)(hi - lo);
        uint limit = uint.MaxValue - (uint.MaxValue % span);

        while (true)
        {
            uint r = NextUInt();
            if (r < limit) return lo + (int)(r % span);
        }
    }
}