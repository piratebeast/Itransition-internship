using MovieShowcase.Core;
using Xunit;

public class SeedTests
{
    [Fact]
    public void Pcg32_MatchesReferenceVector()
    {
        var r = new Pcg32(42);
        Assert.Equal(2707161783u, r.NextUInt());
        Assert.Equal(2068313097u, r.NextUInt());
        Assert.Equal(3122475824u, r.NextUInt());
    }

    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var a = new Pcg32(99);
        var b = new Pcg32(99);
        for (int i = 0; i < 100; i++) Assert.Equal(a.NextUInt(), b.NextUInt());
    }

    [Fact]
    public void FieldsAreIndependent()
    {
        var core1 = SeedDerivation.RecordSeed(12345, 7, SeedField.Core);
        var core2 = SeedDerivation.RecordSeed(12345, 7, SeedField.Core);
        var likes = SeedDerivation.RecordSeed(12345, 7, SeedField.Likes);
        Assert.Equal(core1, core2);
        Assert.NotEqual(core1, likes);
    }

    [Fact]
    public void NextInt_IsUniform()
    {
        var r = new Pcg32(7);
        var buckets = new int[10];
        for (int i = 0; i < 100_000; i++) buckets[r.NextInt(0, 10)]++;
        foreach (var b in buckets) Assert.InRange(b, 9_700, 10_300);
    }
}