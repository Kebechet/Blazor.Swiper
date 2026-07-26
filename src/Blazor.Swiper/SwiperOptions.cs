namespace Kebechet.Blazor.Swiper;

/// <summary>
/// Strongly-typed subset of Swiper parameters passed to the underlying Swiper Element on init.
/// Null-valued members are skipped, so leaving one unset keeps Swiper's own default.
/// Property names are camel-cased by the JS-interop serializer to match Swiper's parameter names.
/// </summary>
public sealed class SwiperOptions
{
    /// <summary>Slider axis. Use <see cref="SwiperDirection"/> constants.</summary>
    public string Direction { get; set; } = SwiperDirection.Horizontal;

    /// <summary>Number of slides visible at once. Null = Swiper default (1).</summary>
    public double? SlidesPerView { get; set; }

    /// <summary>Gap between slides in px. Null = Swiper default (0).</summary>
    public int? SpaceBetween { get; set; }

    /// <summary>Continuous loop mode.</summary>
    public bool Loop { get; set; }

    /// <summary>Center the active slide.</summary>
    public bool CenteredSlides { get; set; }

    /// <summary>Track height follows the active slide's content height.</summary>
    public bool AutoHeight { get; set; }

    /// <summary>Slide shown first. Null = 0.</summary>
    public int? InitialSlide { get; set; }

    /// <summary>Transition duration in ms. Null = Swiper default (300).</summary>
    public int? Speed { get; set; }

    /// <summary>Allow moving to the next slide (swipe/keys). Toggle at runtime via <c>SetAllowSlideNext</c>.</summary>
    public bool AllowSlideNext { get; set; } = true;

    /// <summary>Allow moving to the previous slide (swipe/keys). Toggle at runtime via <c>SetAllowSlidePrev</c>.</summary>
    public bool AllowSlidePrev { get; set; } = true;

    /// <summary>Allow touch/drag swiping at all.</summary>
    public bool AllowTouchMove { get; set; } = true;

    /// <summary>Show the built-in pagination bullets.</summary>
    public bool Pagination { get; set; }

    /// <summary>Show the built-in prev/next arrows.</summary>
    public bool Navigation { get; set; }

    /// <summary>Show the built-in scrollbar.</summary>
    public bool Scrollbar { get; set; }

    /// <summary>Enable keyboard control.</summary>
    public bool Keyboard { get; set; }

    /// <summary>Enable mousewheel control.</summary>
    public bool Mousewheel { get; set; }

    /// <summary>
    /// Swiper's built-in MutationObserver that auto-calls <c>update()</c> on any DOM change in the
    /// container. Off by default (Swiper Element itself defaults it on): with a framework re-rendering
    /// slide content - and especially with <see cref="AutoHeight"/>, whose height writes are themselves
    /// DOM mutations - it drives a costly update/height feedback loop. Call update explicitly instead.
    /// </summary>
    public bool Observer { get; set; }

    /// <summary>
    /// Use the browser's native CSS Scroll Snap API instead of JS transforms. Moves the slide on the
    /// compositor thread, which is dramatically smoother for heavy/tall slides. Trade-offs (per Swiper):
    /// mouse-drag does not work (wheel/trackpad/touch still do, exactly like a native scroller),
    /// <see cref="Speed"/> is ignored, transition start/end events do not fire (use
    /// <see cref="Swiper.OnSlideChange"/>), and it is NOT compatible with <see cref="Loop"/>.
    /// </summary>
    public bool CssMode { get; set; }

    /// <summary>Accessibility behaviour. Null = Swiper's defaults (a11y on, including scroll-on-focus).</summary>
    public SwiperA11yOptions? A11y { get; set; }

    /// <summary>
    /// Whether Swiper watches its own element for size changes and re-measures. Swiper's resize handling
    /// finishes by re-anchoring onto the CURRENT index, which is destructive when a programmatic move is
    /// already in flight: the re-anchor is scheduled with the pre-move index and lands after it, undoing it.
    /// Set false when the pages resize as a matter of course (live content) and the host drives the position.
    /// Window resizes still re-measure; only the per-element observer is dropped.
    /// </summary>
    public bool ResizeObserver { get; set; } = true;
}

/// <summary>Accessibility parameters, i.e. Swiper's <c>a11y</c> module.</summary>
public sealed class SwiperA11yOptions
{
    /// <summary>
    /// Whether focusing an element inside a non-active slide slides that slide into view. Swiper schedules
    /// that correction through <c>requestAnimationFrame</c>, so with a host framework that moves the slider
    /// in response to the same interaction (e.g. a button inside a slide advancing the pager), the
    /// correction lands AFTER the intended move and pulls the slider back to the slide holding the button.
    /// Set false when the host drives the position itself.
    /// </summary>
    public bool ScrollOnFocus { get; set; } = true;
}

/// <summary>Values for <see cref="SwiperOptions.Direction"/>.</summary>
public static class SwiperDirection
{
    /// <summary>Slides move left/right.</summary>
    public const string Horizontal = "horizontal";

    /// <summary>Slides move up/down. Needs an explicit height on the <c>Swiper</c> element.</summary>
    public const string Vertical = "vertical";
}
