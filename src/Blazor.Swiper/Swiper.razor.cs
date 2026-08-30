using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Kebechet.Blazor.Swiper;

/// <summary>
/// Renders a Swiper slider. Place <see cref="SwiperSlide"/> components in its content, bind
/// <see cref="ActiveIndex"/> to follow and drive the position, and capture it with <c>@ref</c> for
/// the rest of the API.
/// </summary>
public partial class Swiper : IAsyncDisposable
{
    /// <summary>Parameters passed to the underlying Swiper.</summary>
    /// <remarks>
    /// Changing this pushes the members that moved to the live slider, for the parameters Swiper can
    /// re-apply after init. The ones it only reads while initializing are ignored, and say so in the
    /// browser console rather than failing silently.
    /// </remarks>
    [Parameter] public SwiperOptions Options { get; set; } = new();

    /// <summary>The slides, i.e. a set of <see cref="SwiperSlide"/> components.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The active slide's logical index. Two-way bindable with <c>@bind-ActiveIndex</c>: setting it
    /// moves the slider, and the slider moving sets it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Always the index into your own collection. In <see cref="SwiperOptions.Loop"/> mode Swiper's
    /// internal index counts duplicated slides that your collection does not have, and a bound move
    /// routes through Swiper's loop-aware navigation so that both directions agree.
    /// </para>
    /// <para>
    /// A bound change animates at <see cref="SwiperOptions.Speed"/>. Call
    /// <see cref="SlideTo(int, int?)"/> with a speed of 0 for an instant jump.
    /// </para>
    /// <para>
    /// Supplying a value before the slider initializes chooses the slide it opens on, so binding an
    /// index of 2 starts on the third slide rather than starting at 0 and jumping.
    /// </para>
    /// </remarks>
    [Parameter]
    public int ActiveIndex
    {
        get => _activeIndex;
        set
        {
            if (_activeIndex == value)
            {
                return;
            }

            _activeIndex = value;
            _isHostIndexChangePending = true;
        }
    }

    /// <summary>Raised when <see cref="ActiveIndex"/> changes, whoever caused it.</summary>
    [Parameter] public EventCallback<int> ActiveIndexChanged { get; set; }

    /// <summary>
    /// Whether autoplay is running. Two-way bindable with <c>@bind-IsAutoplayRunning</c>, which is
    /// what a play/pause button wants: autoplay also stops itself, on interaction and at the last
    /// slide, and the binding follows that.
    /// </summary>
    [Parameter]
    public bool IsAutoplayRunning
    {
        get => _isAutoplayRunning;
        set
        {
            if (_isAutoplayRunning == value)
            {
                return;
            }

            _isAutoplayRunning = value;
            _isHostAutoplayChangePending = true;
        }
    }

    /// <summary>Raised when <see cref="IsAutoplayRunning"/> changes, whoever caused it.</summary>
    [Parameter] public EventCallback<bool> IsAutoplayRunningChanged { get; set; }

    /// <summary>
    /// A second slider acting as this one's thumbnail strip.
    /// </summary>
    /// <remarks>
    /// Wired once both sliders have initialized, whichever order that happens in. Configure the
    /// behaviour through <see cref="SwiperOptions.Thumbs"/>; the strip itself lives here because a
    /// component reference cannot be serialized into an options object.
    /// </remarks>
    [Parameter] public Swiper? Thumbs { get; set; }

    /// <summary>
    /// A second slider this one drives. Configure the behaviour through
    /// <see cref="SwiperOptions.Controller"/>.
    /// </summary>
    [Parameter] public Swiper? Controller { get; set; }

    /// <summary>
    /// Shortest interval between deliveries of the events that fire on every animation frame -
    /// progress, setTranslate, setTransition, sliderMove, touchMove, touchMoveOpposite,
    /// autoplayTimeLeft, zoomChange and scroll. Null delivers every one.
    /// </summary>
    /// <remarks>
    /// Worth setting on Blazor Server, where each delivery is a network round trip; on WebAssembly
    /// the call is in-process and the default is usually fine. The first event of a burst is always
    /// delivered, so a throttled <see cref="OnProgress"/> still sees a drag start immediately.
    /// </remarks>
    [Parameter] public TimeSpan? EventThrottle { get; set; }

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

    /// <summary>
    /// Raised once the underlying Swiper has initialized and positioned its initial slide, and before
    /// the slider is revealed. This is the wrapper's own moment rather than Swiper's
    /// <see cref="OnInit"/>, and it is where a host puts its own opening position.
    /// </summary>
    [Parameter] public EventCallback OnReady { get; set; }

    /// <summary>
    /// Raised when <see cref="SwiperOptions.Virtual"/> wants a different span of slides rendered.
    /// Render exactly that span; Swiper renders nothing itself while this is handled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is Swiper's <c>renderExternal</c> hook, which exists so a framework can keep ownership
    /// of the DOM: Swiper works out which slides are needed and where the window sits, and hands
    /// both over instead of creating and removing slide elements. Set
    /// <see cref="SwiperVirtualOptions.SlideCount"/> so it knows how far the collection goes, give
    /// each rendered <see cref="SwiperSlide"/> its <see cref="SwiperSlide.VirtualIndex"/>, and leave
    /// the window's offset alone - the wrapper applies it to the slides for you.
    /// </para>
    /// <para>
    /// Re-measuring is the wrapper's too: the window arrives before Blazor has rendered it, so
    /// Swiper is told to skip its own post-render pass and the wrapper runs it on the render that
    /// follows, once the slides it measures actually exist.
    /// </para>
    /// </remarks>
    [Parameter] public EventCallback<SwiperVirtualWindow> OnVirtualRender { get; set; }

    /// <summary>Attributes forwarded to the underlying <c>&lt;swiper-container&gt;</c> element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private const string ModulePath = "./_content/Kebechet.Blazor.Swiper/swiper-interop.js";
    private const string HiddenStyle = "visibility:hidden";

    private static readonly JsonSerializerOptions SnapshotSerializer = new(JsonSerializerDefaults.Web);

    private ElementReference _element;
    private IJSObjectReference? _module;
    private DotNetObjectReference<Swiper>? _selfReference;

    private int _activeIndex;
    private bool _isHostIndexChangePending;
    private bool _isAutoplayRunning;
    private bool _isHostAutoplayChangePending;

    private string? _virtualOffsetStyle;
    private bool _isVirtualRemeasurePending;

    private string? _appliedOptionsJson;
    private string[] _subscribedEvents = Array.Empty<string>();
    private Swiper? _wiredThumbs;
    private Swiper? _wiredController;
    private readonly HashSet<Swiper> _awaitedCompanions = new();

    /// <summary>
    /// Swiper Element lays its slides on top of one another until it has initialized and positioned the
    /// first one, and the module import that initializes it is asynchronous - so the host would otherwise
    /// paint a stack of overlapping slides, then the real thing. Hidden rather than not rendered, so the
    /// slides are still laid out and measurable (autoHeight, media loading) before they are shown.
    /// </summary>
    private bool _isPositioned;

    /// <summary>The element the underlying <c>&lt;swiper-container&gt;</c> was rendered onto.</summary>
    internal ElementReference Element => _element;

    /// <summary>Whether the interop module has loaded and the underlying Swiper exists.</summary>
    internal bool IsInitialized => _module is not null && _isPositioned;

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
        if (firstRender)
        {
            await InitializeAsync();
            return;
        }

        if (_isVirtualRemeasurePending)
        {
            _isVirtualRemeasurePending = false;

            // Swiper skipped its own post-render pass because the window was still only a promise
            // when it handed it over. The slides exist now, so this is the moment it can measure.
            await Update();
        }

        await WireCompanionsAsync();
    }

    private async Task InitializeAsync()
    {
        _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
        _selfReference = DotNetObjectReference.Create(this);
        _subscribedEvents = SubscribedEventNames();

        var options = WithInitialSlide(Options);

        // The caller's own options are the snapshot, not the derived ones: the initial slide folded in
        // above is the wrapper's doing, and comparing against it would report a change on the very next
        // parameter set and push an update nobody asked for.
        _appliedOptionsJson = SerializeOptions(Options);

        await _module.InvokeVoidAsync(
            "initialize",
            _element,
            options,
            _selfReference,
            _subscribedEvents,
            EventThrottle?.TotalMilliseconds ?? 0,
            OnVirtualRender.HasDelegate);

        // The host's own opening position belongs before the reveal, so that a slider told to start
        // somewhere other than slide 0 is never seen at slide 0 first.
        await OnReady.InvokeAsync();
        _isPositioned = true;
        await InvokeAsync(StateHasChanged);

        Initialized?.Invoke();

        await WireCompanionsAsync();
    }

    /// <summary>
    /// Raised once this slider has initialized, so a slider holding it as a companion can wire up.
    /// </summary>
    /// <remarks>
    /// Two sibling components initialize in render order and each one's initialization is
    /// asynchronous, so which finishes first is not something either can rely on. Without this the
    /// slider that names the other would wire only when it happened to re-render after the other was
    /// ready - which it usually does, and sometimes does not.
    /// </remarks>
    internal event Action? Initialized;

    /// <summary>
    /// The options actually sent at init, with a bound <see cref="ActiveIndex"/> folded in as the
    /// starting slide.
    /// </summary>
    /// <remarks>
    /// Seeding <c>initialSlide</c> rather than moving after the fact is what makes an opening index
    /// invisible: a <c>SlideTo</c> issued once the slider exists is a second frame, and the first one
    /// shows slide 0. An explicit <see cref="SwiperOptions.InitialSlide"/> wins, since that is the
    /// caller being specific.
    /// </remarks>
    private SwiperOptions WithInitialSlide(SwiperOptions options)
    {
        _isHostIndexChangePending = false;

        if (_activeIndex == 0 || options.InitialSlide is not null)
        {
            return options;
        }

        return options with { InitialSlide = _activeIndex };
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (_module is null)
        {
            return;
        }

        await ApplyChangedOptionsAsync();
        await ApplyChangedSubscriptionsAsync();

        if (_isHostIndexChangePending)
        {
            _isHostIndexChangePending = false;
            await SlideTo(_activeIndex);
        }

        if (_isHostAutoplayChangePending)
        {
            _isHostAutoplayChangePending = false;
            await (_isAutoplayRunning ? Autoplay.Start() : Autoplay.Stop());
        }
    }

    private async Task ApplyChangedOptionsAsync()
    {
        var snapshot = SerializeOptions(Options);
        if (snapshot == _appliedOptionsJson)
        {
            return;
        }

        _appliedOptionsJson = snapshot;
        await _module!.InvokeVoidAsync("updateOptions", _element, Options);
    }

    /// <summary>
    /// Keeps the listened-for events in step with the callbacks that are actually wired.
    /// </summary>
    /// <remarks>
    /// A host can assign a callback conditionally - a debug panel that only subscribes to
    /// <see cref="OnProgress"/> while it is open - and a subscription list fixed at init would
    /// either miss it or pay for it forever.
    /// </remarks>
    private async Task ApplyChangedSubscriptionsAsync()
    {
        var subscriptions = SubscribedEventNames();
        if (subscriptions.SequenceEqual(_subscribedEvents))
        {
            return;
        }

        _subscribedEvents = subscriptions;
        await _module!.InvokeVoidAsync("setSubscriptions", _element, subscriptions);
    }

    /// <summary>
    /// Links the thumbnail strip and the controlled slider once both sides exist.
    /// </summary>
    /// <remarks>
    /// A companion is routinely not ready when this slider is, so one that is still initializing is
    /// waited on rather than skipped - see <see cref="Initialized"/>. That way the host can write the
    /// two sliders in whichever order reads best rather than in the order the wrapper needs.
    /// </remarks>
    private async Task WireCompanionsAsync()
    {
        if (_module is null)
        {
            return;
        }

        if (!ReferenceEquals(_wiredThumbs, Thumbs) && await IsCompanionReadyAsync(Thumbs))
        {
            _wiredThumbs = Thumbs;
            await _module.InvokeVoidAsync("setThumbs", _element, Thumbs!.Element);
        }

        if (!ReferenceEquals(_wiredController, Controller) && await IsCompanionReadyAsync(Controller))
        {
            _wiredController = Controller;
            await _module.InvokeVoidAsync("setController", _element, Controller!.Element);
        }
    }

    /// <summary>
    /// Whether a companion can be wired now, arranging to be told when it can if it cannot.
    /// </summary>
    private Task<bool> IsCompanionReadyAsync(Swiper? companion)
    {
        if (companion is null)
        {
            return Task.FromResult(false);
        }

        if (companion.IsInitialized)
        {
            return Task.FromResult(true);
        }

        if (_awaitedCompanions.Add(companion))
        {
            companion.Initialized += OnCompanionInitialized;
        }

        return Task.FromResult(false);
    }

    private void OnCompanionInitialized()
    {
        _ = InvokeAsync(WireCompanionsAsync);
    }

    /// <summary>
    /// The options as JSON, which is both the change-detection snapshot and what the interop
    /// serializer will produce for the same object.
    /// </summary>
    internal static string SerializeOptions(SwiperOptions options) => JsonSerializer.Serialize(options, SnapshotSerializer);

    /// <summary>Interop callback for Swiper's <c>slideChange</c> event. Not intended to be called from your code.</summary>
    /// <param name="activeIndex">The new logical slide index.</param>
    /// <param name="isUserDriven">Whether a drag caused the change.</param>
    [JSInvokable]
    public async Task OnSlideChangeInternal(int activeIndex, bool isUserDriven)
    {
        // Assigned to the field rather than through the property, so that reporting a change the
        // slider made does not read back as the host asking for a move to the slide it is already on.
        _activeIndex = activeIndex;
        NotifySlides();

        await OnSlideChange.InvokeAsync(activeIndex);

        if (isUserDriven)
        {
            await OnUserSlideChange.InvokeAsync(activeIndex);
        }

        await ActiveIndexChanged.InvokeAsync(activeIndex);
    }

    /// <summary>
    /// Interop callback for Swiper's virtual <c>renderExternal</c> hook. Not intended to be called
    /// from your code.
    /// </summary>
    /// <param name="from">Index of the first slide Swiper wants rendered.</param>
    /// <param name="to">Index of the last slide Swiper wants rendered, inclusive.</param>
    /// <param name="offset">How far along the track the window sits, in px.</param>
    /// <param name="offsetProperty">The CSS property that offset belongs on, which direction and text direction decide.</param>
    [JSInvokable]
    public async Task OnVirtualRenderInternal(int from, int to, double offset, string offsetProperty)
    {
        _virtualOffsetStyle = FormattableString.Invariant($"{offsetProperty}:{offset}px");
        _isVirtualRemeasurePending = true;

        await OnVirtualRender.InvokeAsync(new SwiperVirtualWindow
        {
            From = from,
            To = to,
            Offset = offset
        });

        // The host's own re-render is not enough: the offset above lives on this component, and the
        // slides only pick it up when this one renders them again.
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// The offset the current virtual window sits at, as a style declaration for a slide to carry.
    /// Null whenever the slider is not rendering a virtual window.
    /// </summary>
    internal string? VirtualOffsetStyle => _virtualOffsetStyle;

    private readonly List<SwiperSlide> _registeredSlides = new();

    /// <summary>
    /// Records a slide so it can work out its own position and be told when the active one changes.
    /// </summary>
    /// <remarks>
    /// Registration order is render order, which is the collection's order for the case that matters:
    /// a fixed list, or one appended to. A slide inserted into the middle registers at the end, which
    /// is what <see cref="SwiperSlide.Index"/> is for.
    /// </remarks>
    internal void RegisterSlide(SwiperSlide slide)
    {
        _registeredSlides.Add(slide);
    }

    internal void UnregisterSlide(SwiperSlide slide)
    {
        _registeredSlides.Remove(slide);
    }

    internal int IndexOfSlide(SwiperSlide slide) => _registeredSlides.IndexOf(slide);

    private void NotifySlides()
    {
        foreach (var slide in _registeredSlides)
        {
            slide.NotifyStateChanged();
        }
    }

    private async Task SetAutoplayRunning(bool isRunning)
    {
        if (_isAutoplayRunning == isRunning)
        {
            return;
        }

        _isAutoplayRunning = isRunning;
        await IsAutoplayRunningChanged.InvokeAsync(isRunning);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var companion in _awaitedCompanions)
        {
            companion.Initialized -= OnCompanionInitialized;
        }

        _awaitedCompanions.Clear();

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
