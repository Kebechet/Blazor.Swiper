namespace Kebechet.Blazor.Swiper;

/// <summary>Swiper's <c>autoplay</c> module.</summary>
/// <remarks>
/// Bind <c>@bind-IsAutoplayRunning</c> to follow and drive the running state, or call
/// <c>swiper.Autoplay.Start()</c> and friends. Autoplay stops itself on interaction and at the last
/// slide, which is exactly what the binding is for.
/// </remarks>
public sealed record SwiperAutoplayOptions : SwiperToggleableOptions
{
    /// <summary>
    /// Time each slide is held, in ms. Null = Swiper's default (3000). A single slide can override
    /// this with a <c>data-swiper-autoplay</c> attribute, which
    /// <see cref="SwiperSlide.AutoplayDelay"/> writes for you.
    /// </summary>
    public int? Delay { get; set; }

    /// <summary>Stop once the last slide is reached. Has no effect in loop mode. Null = Swiper's default (false).</summary>
    public bool? StopOnLastSlide { get; set; }

    /// <summary>
    /// Stop for good after the user interacts. Null = Swiper's default (true). Set false and
    /// autoplay restarts after every interaction instead.
    /// </summary>
    public bool? DisableOnInteraction { get; set; }

    /// <summary>Play backwards. Null = Swiper's default (false).</summary>
    public bool? ReverseDirection { get; set; }

    /// <summary>Wait for the transition to finish before counting the next delay. Null = Swiper's default (true).</summary>
    public bool? WaitForTransition { get; set; }

    /// <summary>Pause while the pointer is over the slider. Null = Swiper's default (false).</summary>
    public bool? PauseOnMouseEnter { get; set; }

    /// <summary>Turns autoplay on or off without configuring it.</summary>
    /// <param name="enabled">Whether the slider advances by itself.</param>
    public static implicit operator SwiperAutoplayOptions(bool enabled) => new() { Enabled = enabled };
}

/// <summary>
/// Swiper's <c>controller</c> module, which drives a second slider from this one.
/// </summary>
/// <remarks>
/// The slider being controlled is set through the <see cref="Swiper.Controller"/> parameter rather
/// than here: it is a component reference, and a component reference cannot be serialized into an
/// options object.
/// </remarks>
public sealed record SwiperControllerOptions
{
    /// <summary>
    /// CSS selector for the slider to control, e.g. <c>"#captions"</c>.
    /// </summary>
    /// <remarks>
    /// The order-independent route, and the one to prefer. A <c>@ref</c> captured for the
    /// <see cref="Swiper.Controller"/> parameter is still null while the sibling's markup is being
    /// evaluated, so it only arrives once the host re-renders; a selector is resolved on the
    /// JavaScript side, which waits for the other slider however late it initializes.
    /// </remarks>
    public string? Control { get; set; }

    /// <summary>Move the controlled slider the opposite way. Null = Swiper's default (false).</summary>
    public bool? Inverse { get; set; }

    /// <summary>Whether the two are synced slide for slide or by container translate. Null = Swiper's default (slide).</summary>
    public SwiperControlBy? By { get; set; }
}

/// <summary>Swiper's <c>grid</c> module, which lays slides out in several rows.</summary>
/// <remarks>Not compatible with <see cref="SwiperOptions.Loop"/> unless slide counts divide evenly.</remarks>
public sealed record SwiperGridOptions
{
    /// <summary>Number of rows. Null = Swiper's default (1, i.e. no grid).</summary>
    public int? Rows { get; set; }

    /// <summary>Whether slides fill column by column or row by row. Null = Swiper's default (column).</summary>
    public SwiperGridFill? Fill { get; set; }
}

/// <summary>
/// Swiper's <c>hashNavigation</c> module: the active slide is written to the URL fragment.
/// </summary>
/// <remarks>
/// Each slide needs a <see cref="SwiperSlide.Hash"/>. Swiper's <c>getSlideIndex</c> hook is not
/// exposed: it is called synchronously while resolving the hash, which a C# delegate cannot answer.
/// </remarks>
public sealed record SwiperHashNavigationOptions : SwiperToggleableOptions
{
    /// <summary>Also follow hash changes made elsewhere, e.g. the back button. Null = Swiper's default (false).</summary>
    public bool? WatchState { get; set; }

    /// <summary>Replace the history entry instead of pushing one. Null = Swiper's default (false).</summary>
    public bool? ReplaceState { get; set; }

    /// <summary>Turns hash navigation on or off without configuring it.</summary>
    /// <param name="enabled">Whether the active slide is mirrored into the URL fragment.</param>
    public static implicit operator SwiperHashNavigationOptions(bool enabled) => new() { Enabled = enabled };
}

/// <summary>
/// Swiper's <c>history</c> module: the active slide is written to the URL path through the
/// History API.
/// </summary>
/// <remarks>
/// Each slide needs a <see cref="SwiperSlide.Hash"/>, which doubles as the history key. On Blazor
/// this rewrites the URL underneath the router, so it suits a standalone slider page rather than a
/// slider inside a routed layout.
/// </remarks>
public sealed record SwiperHistoryOptions : SwiperToggleableOptions
{
    /// <summary>Path the slide keys are appended to.</summary>
    public string? Root { get; set; }

    /// <summary>Replace the history entry instead of pushing one. Null = Swiper's default (false).</summary>
    public bool? ReplaceState { get; set; }

    /// <summary>Path segment the slide key sits under. Null = Swiper's default (<c>slides</c>).</summary>
    public string? Key { get; set; }

    /// <summary>Keep the query string when rewriting the URL. Null = Swiper's default (false).</summary>
    public bool? KeepQuery { get; set; }

    /// <summary>Turns history navigation on or off without configuring it.</summary>
    /// <param name="enabled">Whether the active slide is mirrored into the URL path.</param>
    public static implicit operator SwiperHistoryOptions(bool enabled) => new() { Enabled = enabled };
}

/// <summary>
/// Swiper's <c>parallax</c> module: elements inside the slides move at their own rate as the
/// slider does.
/// </summary>
/// <remarks>
/// Drive it from markup with <c>data-swiper-parallax</c> (and <c>-x</c>, <c>-y</c>,
/// <c>-opacity</c>, <c>-scale</c>, <c>-duration</c>) on elements inside a slide.
/// </remarks>
public sealed record SwiperParallaxOptions : SwiperToggleableOptions
{
    /// <summary>Turns parallax on or off.</summary>
    /// <param name="enabled">Whether parallax elements are transformed.</param>
    public static implicit operator SwiperParallaxOptions(bool enabled) => new() { Enabled = enabled };
}

/// <summary>
/// Swiper's <c>thumbs</c> module, i.e. a second slider acting as a thumbnail strip.
/// </summary>
/// <remarks>
/// The thumbnail slider itself is set through the <see cref="Swiper.Thumbs"/> parameter rather than
/// here, because it is a component reference.
/// </remarks>
public sealed record SwiperThumbsOptions
{
    /// <summary>
    /// CSS selector for the thumbnail slider, e.g. <c>"#gallery-thumbs"</c>.
    /// </summary>
    /// <remarks>
    /// The order-independent route, and the one to prefer. A <c>@ref</c> captured for the
    /// <see cref="Swiper.Thumbs"/> parameter is still null while the sibling's markup is being
    /// evaluated, so it only arrives once the host re-renders; a selector is resolved on the
    /// JavaScript side, which waits for the strip however late it initializes.
    /// </remarks>
    public string? Swiper { get; set; }

    /// <summary>Keep every visible thumbnail marked active, not only one. Null = Swiper's default (true).</summary>
    public bool? MultipleActiveThumbs { get; set; }

    /// <summary>Slides to keep visible past the active thumbnail when it scrolls into view. Null = Swiper's default (0).</summary>
    public int? AutoScrollOffset { get; set; }

    /// <summary>Class set on the thumbnail matching the active slide.</summary>
    public string? SlideThumbActiveClass { get; set; }

    /// <summary>Class set on the thumbnail slider's container.</summary>
    public string? ThumbsContainerClass { get; set; }
}

/// <summary>
/// Swiper's <c>virtual</c> module, which keeps only the slides near the viewport in the DOM.
/// </summary>
/// <remarks>
/// <para>
/// Swiper renders virtual slides itself, which puts it in direct conflict with Blazor's ownership
/// of the DOM - so the wrapper exposes the measurement side of the module and leaves rendering to
/// Blazor: keep your <see cref="SwiperSlide"/> loop, and give each one a
/// <see cref="SwiperSlide.VirtualIndex"/> so Swiper can address slides it did not create.
/// </para>
/// <para>
/// Handling <see cref="Swiper.OnVirtualRender"/> turns that into the full arrangement: set
/// <see cref="SlideCount"/>, render only the slides the window names, and Swiper never touches a
/// slide element. Without a handler the module is left to its own rendering, which a Blazor host
/// almost never wants - for a large collection with no handler, Blazor's own <c>Virtualize</c>
/// inside a plain Swiper is the better trade.
/// </para>
/// </remarks>
public sealed record SwiperVirtualOptions : SwiperToggleableOptions
{
    /// <summary>Keep rendered slides cached. Null = Swiper's default (true).</summary>
    public bool? Cache { get; set; }

    /// <summary>
    /// How many slides the collection holds. Required when <see cref="Swiper.OnVirtualRender"/>
    /// renders the window, and meaningless without it.
    /// </summary>
    /// <remarks>
    /// Swiper's own member is <c>slides</c>, the array it renders from. With rendering handed to
    /// Blazor the contents are never read - only the length, to know where the collection ends - so
    /// a count is the whole of what the module needs and the wrapper expands it on the JS side.
    /// </remarks>
    public int? SlideCount { get; set; }

    /// <summary>Extra slides kept before the visible range. Null = Swiper's default (0).</summary>
    public int? AddSlidesBefore { get; set; }

    /// <summary>Extra slides kept after the visible range. Null = Swiper's default (0).</summary>
    public int? AddSlidesAfter { get; set; }

    /// <summary>
    /// Assumed slide size, in px, while <c>SlidesPerView</c> is <see cref="SwiperSlidesPerView.Auto"/>
    /// and the real size is not yet known. Null = Swiper's default (320).
    /// </summary>
    public double? SlidesPerViewAutoSlideSize { get; set; }

    /// <summary>Turns virtual slides on or off without configuring them.</summary>
    /// <param name="enabled">Whether Swiper keeps only nearby slides measured.</param>
    public static implicit operator SwiperVirtualOptions(bool enabled) => new() { Enabled = enabled };
}
