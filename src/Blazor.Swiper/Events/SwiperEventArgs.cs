namespace Kebechet.Blazor.Swiper;

/// <summary>
/// A pointer interaction, as reported by the touch, click and scrollbar-drag events.
/// </summary>
/// <remarks>
/// Swiper hands these events the raw DOM event, which cannot cross the interop boundary, so what
/// arrives here is the part of it a host can act on.
/// </remarks>
public sealed record SwiperPointerEventArgs
{
    /// <summary>Pointer position relative to the viewport, in px.</summary>
    public double ClientX { get; init; }

    /// <summary>Pointer position relative to the viewport, in px.</summary>
    public double ClientY { get; init; }

    /// <summary>
    /// The slide the pointer was over, or -1 when Swiper does not track one for this event.
    /// Only the click and tap events resolve a slide.
    /// </summary>
    public int SlideIndex { get; init; } = -1;
}

/// <summary>How much of the current autoplay delay is left.</summary>
/// <remarks>
/// Raised per animation frame while autoplay runs, so it is one of the events the wrapper's
/// throttle applies to. Its usual purpose is driving a countdown ring or bar.
/// </remarks>
public sealed record SwiperAutoplayTimeLeft
{
    /// <summary>Milliseconds until the slider advances.</summary>
    public double TimeLeft { get; init; }

    /// <summary>Fraction of the delay still to run, from 1 down to 0.</summary>
    public double Percentage { get; init; }
}

/// <summary>A change in a slide's zoom factor.</summary>
public sealed record SwiperZoomChange
{
    /// <summary>The new zoom factor, where 1 is unzoomed.</summary>
    public double Scale { get; init; }

    /// <summary>The slide being zoomed, or -1 if it could not be resolved.</summary>
    public int SlideIndex { get; init; } = -1;
}

/// <summary>A wheel movement seen by the mousewheel module.</summary>
public sealed record SwiperMousewheelScroll
{
    /// <summary>Horizontal wheel delta.</summary>
    public double DeltaX { get; init; }

    /// <summary>Vertical wheel delta.</summary>
    public double DeltaY { get; init; }
}

/// <summary>
/// Everything the underlying Swiper knows about itself right now, read in one interop call.
/// </summary>
/// <remarks>
/// A snapshot, not a live view - it is what was true when <see cref="Swiper.GetStateAsync"/> ran.
/// Gathered in one call on purpose: each value read separately would be its own round trip, which on
/// Blazor Server is a network hop each.
/// </remarks>
public sealed record SwiperState
{
    /// <summary>The active slide's logical index, i.e. its index in your own collection.</summary>
    public int ActiveIndex { get; init; }

    /// <summary>
    /// Swiper's own active index, which in loop mode counts the duplicated slides. Present for
    /// diagnostics; <see cref="ActiveIndex"/> is the one to act on.
    /// </summary>
    public int RawActiveIndex { get; init; }

    /// <summary>The index held before the current one.</summary>
    public int PreviousIndex { get; init; }

    /// <summary>Index into Swiper's snap grid, which differs from the slide index when slides are grouped.</summary>
    public int SnapIndex { get; init; }

    /// <summary>How many slide elements Swiper currently has, duplicates included.</summary>
    public int SlidesCount { get; init; }

    /// <summary>Whether the slider is at the first slide.</summary>
    public bool IsBeginning { get; init; }

    /// <summary>Whether the slider is at the last slide.</summary>
    public bool IsEnd { get; init; }

    /// <summary>Whether the slides all fit, so there is nothing to slide.</summary>
    public bool IsLocked { get; init; }

    /// <summary>Whether a transition is running.</summary>
    public bool IsAnimating { get; init; }

    /// <summary>Whether the slider is responding to interaction at all.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>Position through the slider, from 0 at the first slide to 1 at the last.</summary>
    public double Progress { get; init; }

    /// <summary>The track's current translate, in px.</summary>
    public double Translate { get; init; }

    /// <summary>The slider's measured width, in px.</summary>
    public double Width { get; init; }

    /// <summary>The slider's measured height, in px.</summary>
    public double Height { get; init; }

    /// <summary>Which way the last move went - <c>"next"</c>, <c>"prev"</c> or empty.</summary>
    public string SwipeDirection { get; init; } = string.Empty;

    /// <summary>The active breakpoint key, or empty when none applies.</summary>
    public string CurrentBreakpoint { get; init; } = string.Empty;

    /// <summary>
    /// Indexes of the slides currently in view. Empty unless
    /// <see cref="SwiperOptions.WatchSlidesProgress"/> is on, since that is what measures them.
    /// </summary>
    public IReadOnlyList<int> VisibleSlideIndexes { get; init; } = Array.Empty<int>();

    /// <summary>Whether autoplay is running.</summary>
    public bool IsAutoplayRunning { get; init; }

    /// <summary>Whether autoplay is paused, e.g. by the pointer resting on the slider.</summary>
    public bool IsAutoplayPaused { get; init; }

    /// <summary>The active slide's zoom factor, 1 when unzoomed.</summary>
    public double ZoomScale { get; init; } = 1;
}
