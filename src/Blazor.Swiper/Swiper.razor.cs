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

    /// <summary>Raised on slide change, with the new active slide index.</summary>
    [Parameter] public EventCallback<int> OnSlideChange { get; set; }

    /// <summary>Raised when the last slide is reached.</summary>
    [Parameter] public EventCallback OnReachEnd { get; set; }

    /// <summary>Raised when the first slide is reached.</summary>
    [Parameter] public EventCallback OnReachBeginning { get; set; }

    /// <summary>Raised once the underlying Swiper has initialized and positioned its initial slide.</summary>
    [Parameter] public EventCallback OnReady { get; set; }

    /// <summary>Raised when a slide transition finishes, i.e. the slider has settled at rest.</summary>
    [Parameter] public EventCallback OnTransitionEnd { get; set; }

    /// <summary>Attributes forwarded to the underlying <c>&lt;swiper-container&gt;</c> element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>The active slide index, kept in sync with the underlying Swiper.</summary>
    public int ActiveIndex { get; private set; }

    private const string ModulePath = "./_content/Kebechet.Blazor.Swiper/swiper-interop.js";

    private ElementReference _element;
    private IJSObjectReference? _module;
    private DotNetObjectReference<Swiper>? _selfReference;

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
        await OnReady.InvokeAsync();
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
    public Task OnSlideChangeInternal(int activeIndex)
    {
        ActiveIndex = activeIndex;
        return OnSlideChange.InvokeAsync(activeIndex);
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
