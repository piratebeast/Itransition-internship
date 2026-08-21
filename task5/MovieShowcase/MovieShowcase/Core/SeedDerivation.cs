namespace MovieShowcase.Core
{
    public static class SeedDerivation
    {
        private const ulong Mult = 6364136223846793005UL;
        private const ulong Phi = 0x9E3779B97F4A7C15UL;

        public static Pcg32 RngFor(ulong userSeed, long globalIndex, int field)
        => new Pcg32(RecordSeed(userSeed, globalIndex, field));

        public static ulong SplitMix64(ulong x)
        {
            unchecked
            {
                x += Phi;
                ulong z = x;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        public static ulong PageSeed(ulong userSeed, int page)
        {
            unchecked { return userSeed * Mult + (ulong)page; }
        }

        public static ulong RecordSeed(ulong userSeed, long globalIndex, int field)
        {
            ulong h = SplitMix64(userSeed ^ Phi);
            h = SplitMix64(h ^ (ulong)globalIndex);
            h = SplitMix64(h ^ ((ulong)field * Phi));
            return h;
        }
    }

    public static class SeedField
    {
        public const int Core = 0;   // title, genre, cast, year, synopsis
        public const int Likes = 1;
        public const int Reviews = 2;
        public const int Trailer = 3;
    }
}