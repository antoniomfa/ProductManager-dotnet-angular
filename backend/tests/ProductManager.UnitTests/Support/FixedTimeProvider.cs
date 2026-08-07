namespace ProductManager.UnitTests.Support;

/// <summary>
/// A <see cref="TimeProvider"/> pinned to a known instant, so assertions on timestamps are
/// exact rather than approximate. <see cref="Advance"/> moves it forward when a test needs to
/// prove that one operation was recorded after another.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    private DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset? now = null) => _now = now ?? DefaultNow;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan amount) => _now = _now.Add(amount);
}
