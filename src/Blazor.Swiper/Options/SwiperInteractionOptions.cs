namespace Kebechet.Blazor.Swiper;

/// <summary>Swiper's <c>keyboard</c> module.</summary>
public sealed record SwiperKeyboardOptions : SwiperToggleableOptions
{
    /// <summary>Only react when the slider is in the viewport. Null = Swiper's default (true).</summary>
    public bool? OnlyInViewport { get; set; }

    /// <summary>Also react to PageUp/PageDown, not just the arrow keys. Null = Swiper's default (true).</summary>
    public bool? PageUpDown { get; set; }

    /// <summary>Transition duration for a key-driven move, in ms. Null uses <see cref="SwiperOptions.Speed"/>.</summary>
    public int? Speed { get; set; }

    /// <summary>Turns keyboard control on or off without configuring it.</summary>
    /// <param name="enabled">Whether the keyboard moves the slider.</param>
    public static implicit operator SwiperKeyboardOptions(bool enabled) => new() { Enabled = enabled };
}

/// <summary>Swiper's <c>mousewheel</c> module.</summary>
public sealed record SwiperMousewheelOptions : SwiperToggleableOptions
{
    /// <summary>Only react to wheel movement along the slider's own axis. Null = Swiper's default (false).</summary>
    public bool? ForceToAxis { get; set; }

    /// <summary>Let the page scroll once the slider is at an edge. Null = Swiper's default (false).</summary>
    public bool? ReleaseOnEdges { get; set; }

    /// <summary>Reverse the wheel direction. Null = Swiper's default (false).</summary>
    public bool? Invert { get; set; }

    /// <summary>Multiplier applied to the wheel delta. Null = Swiper's default (1).</summary>
    public double? Sensitivity { get; set; }

    /// <summary>Where the wheel is listened for - <c>container</c>, <c>wrapper</c> or a CSS selector.</summary>
    public string? EventsTarget { get; set; }

    /// <summary>Ignore wheel events whose delta is below this. Null = Swiper's default (no threshold).</summary>
    public double? ThresholdDelta { get; set; }

    /// <summary>Ignore wheel events arriving faster than this, in ms. Null = Swiper's default (no threshold).</summary>
    public double? ThresholdTime { get; set; }

    /// <summary>Elements carrying this class do not react to the wheel.</summary>
    public string? NoMousewheelClass { get; set; }

    /// <summary>Turns mousewheel control on or off without configuring it.</summary>
    /// <param name="enabled">Whether the wheel moves the slider.</param>
    public static implicit operator SwiperMousewheelOptions(bool enabled) => new() { Enabled = enabled };
}

/// <summary>
/// Swiper's <c>freeMode</c> module: the slider stops wherever the drag left it rather than
/// snapping to a slide.
/// </summary>
public sealed record SwiperFreeModeOptions : SwiperToggleableOptions
{
    /// <summary>Keep gliding after the finger is released. Null = Swiper's default (true).</summary>
    public bool? Momentum { get; set; }

    /// <summary>Multiplier for the momentum distance. Null = Swiper's default (1).</summary>
    public double? MomentumRatio { get; set; }

    /// <summary>Multiplier for the momentum velocity. Null = Swiper's default (1).</summary>
    public double? MomentumVelocityRatio { get; set; }

    /// <summary>Bounce back at the edges. Null = Swiper's default (true).</summary>
    public bool? MomentumBounce { get; set; }

    /// <summary>Multiplier for the bounce distance. Null = Swiper's default (1).</summary>
    public double? MomentumBounceRatio { get; set; }

    /// <summary>Velocity below which momentum is not started. Null = Swiper's default (0.02).</summary>
    public double? MinimumVelocity { get; set; }

    /// <summary>Snap to the nearest slide once the glide ends. Null = Swiper's default (false).</summary>
    public bool? Sticky { get; set; }

    /// <summary>Turns free mode on or off without configuring it.</summary>
    /// <param name="enabled">Whether the slider glides freely.</param>
    public static implicit operator SwiperFreeModeOptions(bool enabled) => new() { Enabled = enabled };
}

/// <summary>
/// Swiper's <c>zoom</c> module: pinch or double-tap to zoom the content of a slide.
/// </summary>
/// <remarks>
/// Only content wrapped in an element carrying <see cref="ContainerClass"/> zooms. Set
/// <c>Zoom="true"</c> on a <see cref="SwiperSlide"/> and the wrapper adds that element for you.
/// </remarks>
public sealed record SwiperZoomOptions : SwiperToggleableOptions
{
    /// <summary>Largest zoom factor. Null = Swiper's default (3).</summary>
    public double? MaxRatio { get; set; }

    /// <summary>Smallest zoom factor. Null = Swiper's default (1).</summary>
    public double? MinRatio { get; set; }

    /// <summary>Never zoom past the image's own pixel size. Null = Swiper's default (false).</summary>
    public bool? LimitToOriginalSize { get; set; }

    /// <summary>Pan a zoomed slide by moving the mouse rather than dragging. Null = Swiper's default (false).</summary>
    public bool? PanOnMouseMove { get; set; }

    /// <summary>Whether a double tap toggles zoom. Null = Swiper's default (true).</summary>
    public bool? Toggle { get; set; }

    /// <summary>Class of the element inside a slide whose content zooms.</summary>
    public string? ContainerClass { get; set; }

    /// <summary>Class set on a slide that is currently zoomed.</summary>
    public string? ZoomedSlideClass { get; set; }

    /// <summary>Turns zoom on or off without configuring it.</summary>
    /// <param name="enabled">Whether slides can be zoomed.</param>
    public static implicit operator SwiperZoomOptions(bool enabled) => new() { Enabled = enabled };
}

/// <summary>Swiper's <c>a11y</c> module, which is on by default.</summary>
public sealed record SwiperA11yOptions : SwiperToggleableOptions
{
    /// <summary>Label for the previous-slide button.</summary>
    public string? PrevSlideMessage { get; set; }

    /// <summary>Label for the next-slide button.</summary>
    public string? NextSlideMessage { get; set; }

    /// <summary>Message announced when the first slide is reached.</summary>
    public string? FirstSlideMessage { get; set; }

    /// <summary>Message announced when the last slide is reached.</summary>
    public string? LastSlideMessage { get; set; }

    /// <summary>Label for a pagination bullet. <c>{{index}}</c> is replaced with the slide number.</summary>
    public string? PaginationBulletMessage { get; set; }

    /// <summary>Class of the live region Swiper announces through.</summary>
    public string? NotificationClass { get; set; }

    /// <summary>ARIA label for the slider container.</summary>
    public string? ContainerMessage { get; set; }

    /// <summary>ARIA role description for the slider container.</summary>
    public string? ContainerRoleDescriptionMessage { get; set; }

    /// <summary>ARIA role for the slider container.</summary>
    public string? ContainerRole { get; set; }

    /// <summary>ARIA role description for each slide.</summary>
    public string? ItemRoleDescriptionMessage { get; set; }

    /// <summary>Label for each slide. <c>{{index}}</c> and <c>{{slidesLength}}</c> are replaced.</summary>
    public string? SlideLabelMessage { get; set; }

    /// <summary>ARIA role for each slide. Null = Swiper's default (<c>group</c>).</summary>
    public string? SlideRole { get; set; }

    /// <summary>Id written onto the slider's ARIA wiring. Null lets Swiper generate one.</summary>
    public string? Id { get; set; }

    /// <summary>
    /// Whether focusing an element inside a non-active slide slides that slide into view.
    /// </summary>
    /// <remarks>
    /// Swiper schedules that correction through <c>requestAnimationFrame</c>, so with a host that
    /// moves the slider in response to the same interaction - a button inside a slide advancing the
    /// pager - the correction lands AFTER the intended move and pulls the slider back to the slide
    /// holding the button. Set false when your code drives the position.
    /// </remarks>
    public bool? ScrollOnFocus { get; set; }

    /// <summary>Whether the slides wrapper is an ARIA live region. Null = Swiper's default (true).</summary>
    public bool? WrapperLiveRegion { get; set; }

    /// <summary>Turns accessibility wiring on or off without configuring it.</summary>
    /// <param name="enabled">Whether Swiper manages ARIA attributes and announcements.</param>
    public static implicit operator SwiperA11yOptions(bool enabled) => new() { Enabled = enabled };
}
