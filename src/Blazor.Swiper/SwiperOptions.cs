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
}

/// <summary>Values for <see cref="SwiperOptions.Direction"/>.</summary>
public static class SwiperDirection
{
    public const string Horizontal = "horizontal";
    public const string Vertical = "vertical";
}
