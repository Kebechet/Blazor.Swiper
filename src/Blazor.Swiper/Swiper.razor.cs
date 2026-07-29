using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Kebechet.Blazor.Swiper;

/// <summary>
/// Renders a Swiper slider. Place <see cref="SwiperSlide"/> components in its content, and
/// capture it with <c>@ref</c> to drive it from C# (<see cref="SlideTo"/>, <see cref="Update"/>, ...).
/// </summary>
public partial class Swiper : IAsyncDisposable
{
    /// <summary>Parameters passed to the underlying Swiper on init.</summary>
    [Parameter] public SwiperOptions Options { get; set; } = new();

    /// <summary>The slides, i.e. a set of <see cref="SwiperSlide"/> components.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised on every slide change, with the new active slide index - whoever caused it.</summary>
    [Parameter] public EventCallback<int> OnSlideChange { get; set; }

    /// <summary>
    /// Raised only for a slide change the <em>user</em> caused by dragging. Prefer this whenever the host
    /// reacts to a change by updating its own state: a code-driven move already knows where it is going,
    /// and re-processing it as if it were a swipe causes feedback loops. The distinction cannot be made
    /// from the index alone - <c>update()</c> emits a change too, and <see cref="SwiperOptions.Loop"/>
    /// re-announces the slide that was left <em>after</em> a programmatic move has finished.
    /// </summary>
    [Parameter] public EventCallback<int> OnUserSlideChange { get; set; }

    /// <summary>Raised when the last slide is reached.</summary>
    [Parameter] public EventCallback OnReachEnd { get; set; }

    /// <summary>Raised when the first slide is reached.</summary>
    [Parameter] public EventCallback OnReachBeginning { get; set; }

    /// <summary>Raised once the underlying Swiper has initialized and positioned its initial slide.</summary>
    [Parameter] public EventCallback OnReady { get; set; }

    /// <summary>Raised when a slide transition finishes, i.e. the slider has settled at rest.</summary>
    [Parameter] public EventCallback OnTransitionEnd { get; set; }

    /// <summary>
    /// Raised the first time a drag actually moves the slider. Use it to tell a user-driven slide change
    /// apart from a programmatic one - by index alone they are identical, and in <see cref="SwiperOptions.Loop"/>
    /// mode a programmatic move is followed by an echo announcing the slide that was left. A plain tap does
    /// not raise this, so a button inside a slide cannot be mistaken for a swipe. The slider keeps moving
    /// afterwards, so treat the interaction as over on <see cref="OnTransitionEnd"/>, not on pointer release.
    /// Not raised in <see cref="SwiperOptions.CssMode"/>, where the browser owns the scroll.
    /// </summary>
    [Parameter] public EventCallback OnSliderFirstMove { get; set; }

    /// <summary>Attributes forwarded to the underlying <c>&lt;swiper-container&gt;</c> element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>The active slide index, kept in sync with the underlying Swiper.</summary>
    public int ActiveIndex { get; private set; }

    private const string ModulePath = "./_content/Kebechet.Blazor.Swiper/swiper-interop.js";
    private const string HiddenStyle = "visibility:hidden";

    private ElementReference _element;
    private IJSObjectReference? _module;
    private DotNetObjectReference<Swiper>? _selfReference;

    /// <summary>
    /// Swiper Element lays its slides on top of one another until it has initialized and positioned the
    /// first one, and the module import that initializes it is asynchronous - so the host would otherwise
    /// paint a stack of overlapping slides, then the real thing. Hidden rather than not rendered, so the
    /// slides are still laid out and measurable (autoHeight, media loading) before they are shown.
    /// </summary>
    private bool _isPositioned;

    private IReadOnlyDictionary<string, object>? _renderedAttributes =>
        _isPositioned ? AdditionalAttributes : WithHiddenVisibility(AdditionalAttributes);

    /// <summary>
    /// The caller's attributes with <c>visibility:hidden</c> appended to whatever style they supplied.
    /// </summary>
    /// <remarks>
    /// Appended rather than replacing the style outright, because the caller's own style is what
    /// sizes the slider - a vertical Swiper collapses to nothing without the height it carries.
    /// </remarks>
    internal static IReadOnlyDictionary<string, object> WithHiddenVisibility(IReadOnlyDictionary<string, object>? attributes)
    {
        var rendered = attributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(attributes);

        rendered.TryGetValue("style", out var callerStyle);
        var style = callerStyle?.ToString();

        rendered["style"] = string.IsNullOrWhiteSpace(style)
            ? HiddenStyle
            : $"{style};{HiddenStyle}";

        return rendered;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
        _selfReference = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("initialize", _element, Options, _selfReference);

        // Revealed only after OnReady, since that is where a host puts its own initial positioning.
        await OnReady.InvokeAsync();
        _isPositioned = true;
        StateHasChanged();
    }

    /// <summary>Transition to the slide at <paramref name="index"/>.</summary>
    public async Task SlideTo(int index, int? speed = null)
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("slideTo", _element, index, speed);
        }
    }

    /// <summary>Transition to the next slide.</summary>
    public async Task SlideNext(int? speed = null)
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("slideNext", _element, speed);
        }
    }

    /// <summary>Transition to the previous slide.</summary>
    public async Task SlidePrev(int? speed = null)
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("slidePrev", _element, speed);
        }
    }

    /// <summary>Recalculate Swiper after the slide collection changed (add/remove of child slides).</summary>
    public async Task Update()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("update", _element);
        }
    }

    /// <summary>
    /// Re-anchor onto <paramref name="index"/> the moment the slide elements are next added or removed by
    /// the framework. Call this <em>before</em> mutating the slides. Removing a slide that sits before the
    /// active one shifts every later slide sideways, and correcting that from a call made after the change
    /// is a race the browser can win by painting first. This arms a <c>MutationObserver</c>, whose callback
    /// is delivered before the next paint, so the correction always lands in the same frame as the change.
    /// It is one-shot: only the next slide-collection mutation is anchored.
    /// </summary>
    public async Task ArmAnchor(int index)
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("armAnchor", _element, index);
        }
    }

    /// <summary>
    /// Recalculate after the slide collection changed AND settle instantly on <paramref name="index"/>, as
    /// one operation. Use this instead of <see cref="Update"/> + <see cref="SlideTo"/> whenever the change
    /// removed or inserted a slide <em>before</em> the active one: those shift every later slide sideways,
    /// and doing the two steps separately lets the slider be seen at the stale offset in between. Swipe
    /// locks are ignored for the correction and left as they were.
    /// </summary>
    public async Task UpdateAndAnchor(int index)
    {
        if (_module is null)
        {
            return;
        }

        // The correction has to land in the same turn as the DOM change that caused it. Awaiting the call
        // yields, and the browser paints the shifted-but-uncorrected track in that gap - measured at ~21ms
        // on a Pixel 6, i.e. a visible frame of the wrong slide. In-process interop is synchronous, so the
        // browser never gets a chance to paint; Blazor Server has no such option and keeps the async path.
        if (_module is IJSInProcessObjectReference inProcessModule)
        {
            inProcessModule.InvokeVoid("updateAndAnchor", _element, index);
            return;
        }

        await _module.InvokeVoidAsync("updateAndAnchor", _element, index);
    }

    /// <summary>Enable/disable moving forward (e.g. lock swiping so only a Next button advances).</summary>
    public async Task SetAllowSlideNext(bool value)
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("setAllowSlideNext", _element, value);
        }
    }

    /// <summary>Enable/disable moving backward.</summary>
    public async Task SetAllowSlidePrev(bool value)
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("setAllowSlidePrev", _element, value);
        }
    }

    /// <summary>Interop callback for Swiper's <c>slidechange</c> event. Not intended to be called from your code.</summary>
    [JSInvokable]
    public async Task OnSlideChangeInternal(int activeIndex, bool isUserDriven)
    {
        ActiveIndex = activeIndex;
        await OnSlideChange.InvokeAsync(activeIndex);

        if (isUserDriven)
        {
            await OnUserSlideChange.InvokeAsync(activeIndex);
        }
    }

    /// <summary>Interop callback for Swiper's <c>reachend</c> event. Not intended to be called from your code.</summary>
    [JSInvokable]
    public Task OnReachEndInternal()
    {
        return OnReachEnd.InvokeAsync();
    }

    /// <summary>Interop callback for Swiper's <c>reachbeginning</c> event. Not intended to be called from your code.</summary>
    [JSInvokable]
    public Task OnReachBeginningInternal()
    {
        return OnReachBeginning.InvokeAsync();
    }

    /// <summary>Interop callback for Swiper's <c>transitionend</c> event. Not intended to be called from your code.</summary>
    [JSInvokable]
    public Task OnTransitionEndInternal()
    {
        return OnTransitionEnd.InvokeAsync();
    }

    /// <summary>Interop callback for Swiper's <c>sliderFirstMove</c> event. Not intended to be called from your code.</summary>
    [JSInvokable]
    public Task OnSliderFirstMoveInternal()
    {
        return OnSliderFirstMove.InvokeAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("destroy", _element);
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // Circuit already gone - nothing to clean up on the JS side.
        }

        _selfReference?.Dispose();
    }
}
