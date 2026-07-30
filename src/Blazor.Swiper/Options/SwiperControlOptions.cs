namespace Kebechet.Blazor.Swiper;

/// <summary>
/// Swiper's <c>navigation</c> module, i.e. the prev/next arrows.
/// </summary>
/// <remarks>
/// Swiper Element creates the two buttons inside its shadow DOM and exposes them as the
/// <c>button-prev</c> / <c>button-next</c> CSS parts, so styling them is a
/// <c>::part(button-next)</c> rule rather than markup you supply.
/// </remarks>
public sealed record SwiperNavigationOptions : SwiperToggleableOptions
{
    /// <summary>CSS selector for your own "next" button. Null lets Swiper Element create one.</summary>
    public string? NextEl { get; set; }

    /// <summary>CSS selector for your own "previous" button. Null lets Swiper Element create one.</summary>
    public string? PrevEl { get; set; }

    /// <summary>Whether Swiper draws its chevron into the buttons it creates. Null = Swiper's default (true).</summary>
    public bool? AddIcons { get; set; }

    /// <summary>Toggle the buttons on click on the slider. Null = Swiper's default (false).</summary>
    public bool? HideOnClick { get; set; }

    /// <summary>Class set on a button that cannot be used, e.g. "next" on the last slide.</summary>
    public string? DisabledClass { get; set; }

    /// <summary>Class set on a hidden button.</summary>
    public string? HiddenClass { get; set; }

    /// <summary>Class set on both buttons when the slider is locked.</summary>
    public string? LockClass { get; set; }

    /// <summary>Class set on the slider when navigation is disabled.</summary>
    public string? NavigationDisabledClass { get; set; }

    /// <summary>Turns navigation on or off without configuring it.</summary>
    /// <param name="enabled">Whether the arrows are shown.</param>
    public static implicit operator SwiperNavigationOptions(bool enabled) => new() { Enabled = enabled };
}

/// <summary>
/// Swiper's <c>pagination</c> module, i.e. the bullets, fraction counter or progress bar.
/// </summary>
public sealed record SwiperPaginationOptions : SwiperToggleableOptions
{
    /// <summary>CSS selector for your own pagination container. Null lets Swiper Element create one.</summary>
    public string? El { get; set; }

    /// <summary>Which of the four pagination shapes to render. Null = Swiper's default (bullets).</summary>
    public SwiperPaginationType? Type { get; set; }

    /// <summary>Tag name used for each bullet. Null = Swiper's default (<c>span</c>).</summary>
    public string? BulletElement { get; set; }

    /// <summary>Show only a few bullets and scroll them, for sliders with many slides. Null = Swiper's default (false).</summary>
    public bool? DynamicBullets { get; set; }

    /// <summary>How many bullets stay at full size when <see cref="DynamicBullets"/> is on. Null = Swiper's default (1).</summary>
    public int? DynamicMainBullets { get; set; }

    /// <summary>Toggle the pagination on click on the slider. Null = Swiper's default (true).</summary>
    public bool? HideOnClick { get; set; }

    /// <summary>Whether clicking a bullet moves to that slide. Null = Swiper's default (false).</summary>
    public bool? Clickable { get; set; }

    /// <summary>Put the progress bar on the opposite side/axis. Null = Swiper's default (false).</summary>
    public bool? ProgressbarOpposite { get; set; }

    /// <summary>
    /// Markup for one bullet, with <c>{{index}}</c> (1-based) and <c>{{className}}</c> placeholders.
    /// </summary>
    /// <remarks>
    /// Swiper's own <c>renderBullet</c> is a JavaScript function it calls synchronously while
    /// rendering, which a C# delegate cannot answer - interop is asynchronous, and on Blazor Server
    /// it is a network round trip. The interop module builds the function from this template
    /// instead, so the common cases (numbered bullets, custom markup) stay reachable. This member is
    /// the wrapper's own, not a Swiper parameter.
    /// </remarks>
    /// <example><code>RenderBulletTemplate = "&lt;span class='{{className}}'&gt;{{index}}&lt;/span&gt;"</code></example>
    public string? RenderBulletTemplate { get; set; }

    /// <summary>
    /// Markup for the fraction counter, with <c>{{currentClass}}</c> and <c>{{totalClass}}</c>
    /// placeholders. Wrapper-side, for the reason given on <see cref="RenderBulletTemplate"/>.
    /// </summary>
    public string? RenderFractionTemplate { get; set; }

    /// <summary>
    /// Markup for the progress bar, with a <c>{{fillClass}}</c> placeholder. Wrapper-side, for the
    /// reason given on <see cref="RenderBulletTemplate"/>.
    /// </summary>
    public string? RenderProgressbarTemplate { get; set; }

    /// <summary>
    /// Markup for <see cref="SwiperPaginationType.Custom"/>, with <c>{{current}}</c> and
    /// <c>{{total}}</c> placeholders. Wrapper-side, for the reason given on
    /// <see cref="RenderBulletTemplate"/>.
    /// </summary>
    public string? RenderCustomTemplate { get; set; }

    /// <summary>
    /// Zero-pads the fraction's numbers to this many digits, so slide 3 of 12 reads "03 / 12".
    /// </summary>
    /// <remarks>
    /// Stands in for Swiper's <c>formatFractionCurrent</c> / <c>formatFractionTotal</c> functions,
    /// which are called synchronously during render and so cannot be C# delegates. Padding is what
    /// those two are almost always used for.
    /// </remarks>
    public int? FractionMinimumDigits { get; set; }

    /// <summary>Class of a single bullet.</summary>
    public string? BulletClass { get; set; }

    /// <summary>Class of the active bullet.</summary>
    public string? BulletActiveClass { get; set; }

    /// <summary>Prefix for the pagination's own modifier classes.</summary>
    public string? ModifierClass { get; set; }

    /// <summary>Class of the fraction's "current" number.</summary>
    public string? CurrentClass { get; set; }

    /// <summary>Class of the fraction's "total" number.</summary>
    public string? TotalClass { get; set; }

    /// <summary>Class set while the pagination is hidden.</summary>
    public string? HiddenClass { get; set; }

    /// <summary>Class of the progress bar's fill element.</summary>
    public string? ProgressbarFillClass { get; set; }

    /// <summary>Class of the opposite-side progress bar.</summary>
    public string? ProgressbarOppositeClass { get; set; }

    /// <summary>Class set when <see cref="Clickable"/> is on.</summary>
    public string? ClickableClass { get; set; }

    /// <summary>Class set when the slider is locked.</summary>
    public string? LockClass { get; set; }

    /// <summary>Class set on a horizontal pagination.</summary>
    public string? HorizontalClass { get; set; }

    /// <summary>Class set on a vertical pagination.</summary>
    public string? VerticalClass { get; set; }

    /// <summary>Class set on the slider when pagination is disabled.</summary>
    public string? PaginationDisabledClass { get; set; }

    /// <summary>Turns pagination on or off without configuring it.</summary>
    /// <param name="enabled">Whether the pagination is shown.</param>
    public static implicit operator SwiperPaginationOptions(bool enabled) => new() { Enabled = enabled };
}

/// <summary>Swiper's <c>scrollbar</c> module.</summary>
public sealed record SwiperScrollbarOptions : SwiperToggleableOptions
{
    /// <summary>CSS selector for your own scrollbar container. Null lets Swiper Element create one.</summary>
    public string? El { get; set; }

    /// <summary>Hide the scrollbar automatically after a move. Null = Swiper's default (true).</summary>
    public bool? Hide { get; set; }

    /// <summary>Whether the scrollbar itself can be dragged. Null = Swiper's default (false).</summary>
    public bool? Draggable { get; set; }

    /// <summary>Snap to the nearest slide when the drag is released. Null = Swiper's default (false).</summary>
    public bool? SnapOnRelease { get; set; }

    /// <summary>Size of the drag handle, in px or <see cref="SwiperLength.Auto"/>.</summary>
    public SwiperLength? DragSize { get; set; }

    /// <summary>Class set when the slider is locked.</summary>
    public string? LockClass { get; set; }

    /// <summary>Class of the drag handle.</summary>
    public string? DragClass { get; set; }

    /// <summary>Class set on the slider when the scrollbar is disabled.</summary>
    public string? ScrollbarDisabledClass { get; set; }

    /// <summary>Class of a horizontal scrollbar.</summary>
    public string? HorizontalClass { get; set; }

    /// <summary>Class of a vertical scrollbar.</summary>
    public string? VerticalClass { get; set; }

    /// <summary>Turns the scrollbar on or off without configuring it.</summary>
    /// <param name="enabled">Whether the scrollbar is shown.</param>
    public static implicit operator SwiperScrollbarOptions(bool enabled) => new() { Enabled = enabled };
}
