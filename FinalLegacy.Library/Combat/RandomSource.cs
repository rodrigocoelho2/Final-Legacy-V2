namespace FinalLegacy.Library.Combat;

/// <summary>Where every roll in a battle comes from, so the tests can use loaded dice.</summary>
public interface IRandomSource
{
    /// <summary>A number from 0 (inclusive) to 1 (exclusive).</summary>
    double NextDouble();
}

/// <summary>Real dice. Pass a seed to replay the same game (the --seed option).</summary>
public sealed class SystemRandomSource(int? seed = null) : IRandomSource
{
    private readonly Random _random = seed is { } s ? new Random(s) : new Random();

    public double NextDouble() => _random.NextDouble();
}
