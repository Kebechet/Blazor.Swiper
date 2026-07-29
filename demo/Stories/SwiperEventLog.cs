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
/// </remarks>
internal sealed class SwiperEventLog
{
    public int ActiveIndex { get; private set; }
    public int SlideChanges { get; private set; }
    public int UserSlideChanges { get; private set; }
    public bool IsReady { get; private set; }

    public void RecordSlideChange(int activeIndex)
    {
        ActiveIndex = activeIndex;
        SlideChanges++;
    }

    public void RecordUserSlideChange(int activeIndex)
    {
        UserSlideChanges++;
    }

    public void RecordReady()
    {
        IsReady = true;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(
            new
            {
                activeIndex = ActiveIndex,
                slideChanges = SlideChanges,
                userSlideChanges = UserSlideChanges,
                isReady = IsReady
            },
            new JsonSerializerOptions { WriteIndented = true });
    }
}
