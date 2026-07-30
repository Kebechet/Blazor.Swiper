using System.Text.Json;

namespace Blazor.Swiper.Demo.Stories;

/// <summary>
/// Running tally of the callbacks a <c>Swiper</c> raised, rendered by <c>SwiperState</c>.
/// </summary>
/// <remarks>
/// Counts rather than a boolean per event: the whole point of <c>OnUserSlideChange</c> is that it
/// fires for a subset of the changes <c>OnSlideChange</c> reports, and only comparing the two
/// tallies shows that. It also makes the distinction assertable by the E2E suite, which is the
/// only place a real swipe can be produced at all.
///
/// The by-name tally underneath is what lets one story subscribe to the whole event surface and one
/// E2E test assert that a given interaction actually raised the events it should have.
/// </remarks>
internal sealed class SwiperEventLog
{
    private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public int ActiveIndex { get; private set; }
    public int SlideChanges { get; private set; }
    public int UserSlideChanges { get; private set; }
    public bool IsReady { get; private set; }

    public void RecordSlideChange(int activeIndex)
    {
        ActiveIndex = activeIndex;
        SlideChanges++;
        Record("slideChange");
    }

    public void RecordUserSlideChange(int activeIndex)
    {
        UserSlideChanges++;
        Record("userSlideChange");
    }

    public void RecordReady()
    {
        IsReady = true;
    }

    /// <summary>Counts one occurrence of a named event.</summary>
    public void Record(string name)
    {
        _counts[name] = _counts.TryGetValue(name, out var count) ? count + 1 : 1;
    }

    /// <summary>How many times a named event has been recorded.</summary>
    public int Count(string name) => _counts.TryGetValue(name, out var count) ? count : 0;

    /// <summary>Counts one occurrence and keeps what it carried, for the events whose payload matters.</summary>
    public void Record(string name, object? value)
    {
        Record(name);
        _values[name] = value?.ToString() ?? string.Empty;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(
            new
            {
                activeIndex = ActiveIndex,
                slideChanges = SlideChanges,
                userSlideChanges = UserSlideChanges,
                isReady = IsReady,
                events = _counts,
                values = _values
            },
            new JsonSerializerOptions { WriteIndented = true });
    }
}
