using Microsoft.JSInterop;

namespace Kebechet.Blazor.Swiper;

// The imperative half of the component: everything a host drives through @ref.
//
// Every method is a no-op until the component has rendered and its JS module has loaded, so calling
// one before OnReady does nothing rather than throwing.
//
// The <summary> for the type itself lives on the primary part, in Swiper.razor.cs.
public partial class Swiper
{
    private SwiperAutoplayController? _autoplay;
    private SwiperZoomController? _zoom;
    private SwiperKeyboardController? _keyboard;
    private SwiperMousewheelController? _mousewheel;
    private SwiperSlideCollection? _slides;
    private SwiperVirtualController? _virtual;

    /// <summary>Starting, stopping and pausing autoplay.</summary>
    public SwiperAutoplayController Autoplay => _autoplay ??= new SwiperAutoplayController(this);

    /// <summary>Zooming the active slide.</summary>
    public SwiperZoomController Zoom => _zoom ??= new SwiperZoomController(this);

    /// <summary>Turning keyboard control on and off at runtime.</summary>
    public SwiperKeyboardController Keyboard => _keyboard ??= new SwiperKeyboardController(this);

    /// <summary>Turning mousewheel control on and off at runtime.</summary>
    public SwiperMousewheelController Mousewheel => _mousewheel ??= new SwiperMousewheelController(this);

    /// <summary>Swiper's own slide manipulation. Read its remarks before using it from a Blazor host.</summary>
    public SwiperSlideCollection Slides => _slides ??= new SwiperSlideCollection(this);

    /// <summary>The virtual module's update hook.</summary>
    public SwiperVirtualController Virtual => _virtual ??= new SwiperVirtualController(this);

    /// <summary>Transition to the slide at <paramref name="index"/>.</summary>
    /// <param name="index">The logical slide index, i.e. the index in your own collection.</param>
    /// <param name="speed">Duration in ms. Null uses <see cref="SwiperOptions.Speed"/>; 0 is instant.</param>
    public Task SlideTo(int index, int? speed = null) => CallAsync("slideTo", index, speed);

    /// <summary>Transition to the next slide.</summary>
    /// <param name="speed">Duration in ms. Null uses <see cref="SwiperOptions.Speed"/>.</param>
    public Task SlideNext(int? speed = null) => CallAsync("slideNext", speed);

    /// <summary>Transition to the previous slide.</summary>
    /// <param name="speed">Duration in ms. Null uses <see cref="SwiperOptions.Speed"/>.</param>
    public Task SlidePrev(int? speed = null) => CallAsync("slidePrev", speed);

    /// <summary>Settle back onto the current slide, undoing a drag that did not commit.</summary>
    /// <param name="speed">Duration in ms. Null uses <see cref="SwiperOptions.Speed"/>.</param>
    public Task SlideReset(int? speed = null) => CallAsync("slideReset", speed);

    /// <summary>Snap to whichever slide is nearest the current position.</summary>
    /// <param name="speed">Duration in ms. Null uses <see cref="SwiperOptions.Speed"/>.</param>
    public Task SlideToClosest(int? speed = null) => CallAsync("slideToClosest", speed);

    /// <summary>Move to the slide the user last clicked. Needs <see cref="SwiperOptions.SlideToClickedSlide"/>.</summary>
    public Task SlideToClickedSlide() => CallAsync("slideToClickedSlide");

    /// <summary>Recalculate Swiper after the slide collection changed (add/remove of child slides).</summary>
    public Task Update() => CallAsync("update");

    /// <summary>Re-measure the slider's own box, without re-measuring the slides.</summary>
    public Task UpdateSize() => CallAsync("updateSize");

    /// <summary>Re-measure the slides.</summary>
    public Task UpdateSlides() => CallAsync("updateSlides");

    /// <summary>Recalculate the slider's progress.</summary>
    public Task UpdateProgress() => CallAsync("updateProgress");

    /// <summary>Recalculate the active/next/prev classes on the slides.</summary>
    public Task UpdateSlidesClasses() => CallAsync("updateSlidesClasses");

    /// <summary>Re-measure the track height. Only meaningful with <see cref="SwiperOptions.AutoHeight"/>.</summary>
    /// <param name="speed">Duration of the height transition in ms; 0 is instant.</param>
    public Task UpdateAutoHeight(int speed = 0) => CallAsync("updateAutoHeight", speed);

    /// <summary>
    /// Re-anchor onto <paramref name="index"/> the moment the slide elements are next added or removed by
    /// the framework. Call this <em>before</em> mutating the slides. Removing a slide that sits before the
    /// active one shifts every later slide sideways, and correcting that from a call made after the change
    /// is a race the browser can win by painting first. This arms a <c>MutationObserver</c>, whose callback
    /// is delivered before the next paint, so the correction always lands in the same frame as the change.
    /// It is one-shot: only the next slide-collection mutation is anchored.
    /// </summary>
    /// <remarks>
    /// Do not also move a bound <see cref="ActiveIndex"/> in the same operation. The anchor puts the
    /// slider where it belongs instantly; a bound index changed alongside it then sees a value it did
    /// not report, and issues a second - animated - move to the slide the slider is already on. Let
    /// the anchor own the correction and take the result back through <see cref="OnSlideChange"/>.
    /// </remarks>
    /// <param name="index">The slide to settle on once the mutation lands.</param>
    public Task ArmAnchor(int index) => CallAsync("armAnchor", index);

    /// <summary>
    /// Recalculate after the slide collection changed AND settle instantly on <paramref name="index"/>, as
    /// one operation. Use this instead of <see cref="Update"/> + <see cref="SlideTo"/> whenever the change
    /// removed or inserted a slide <em>before</em> the active one: those shift every later slide sideways,
    /// and doing the two steps separately lets the slider be seen at the stale offset in between. Swipe
    /// locks are ignored for the correction and left as they were.
    /// </summary>
    /// <param name="index">The slide to settle on.</param>
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
    /// <param name="value">Whether forward moves are allowed.</param>
    public Task SetAllowSlideNext(bool value) => CallAsync("setAllowSlideNext", value);

    /// <summary>Enable/disable moving backward.</summary>
    /// <param name="value">Whether backward moves are allowed.</param>
    public Task SetAllowSlidePrev(bool value) => CallAsync("setAllowSlidePrev", value);

    /// <summary>Enable/disable dragging, without touching the option the slider was built with.</summary>
    /// <param name="value">Whether the slider can be dragged.</param>
    public Task SetAllowTouchMove(bool value) => CallAsync("setAllowTouchMove", value);

    /// <summary>Bring a disabled slider back to life.</summary>
    public Task Enable() => CallAsync("enable");

    /// <summary>Disable the slider: it stops responding to interaction and hides its navigation.</summary>
    public Task Disable() => CallAsync("disable");

    /// <summary>Move to a position through the slider rather than to a slide.</summary>
    /// <param name="progress">0 at the first slide, 1 at the last.</param>
    /// <param name="speed">Duration in ms. Null uses <see cref="SwiperOptions.Speed"/>.</param>
    public Task SetProgress(double progress, int? speed = null) => CallAsync("setProgress", progress, speed);

    /// <summary>Switch the slider's axis at runtime.</summary>
    /// <param name="direction">The axis to switch to.</param>
    public Task ChangeDirection(SwiperDirection direction) =>
        CallAsync("changeDirection", direction == SwiperDirection.Vertical ? "vertical" : "horizontal");

    /// <summary>Switch the slider between left-to-right and right-to-left.</summary>
    /// <param name="isRightToLeft">Whether the slider should read right to left.</param>
    public Task ChangeLanguageDirection(bool isRightToLeft) =>
        CallAsync("changeLanguageDirection", isRightToLeft ? "rtl" : "ltr");

    /// <summary>Move the track to a translate value directly.</summary>
    /// <param name="translate">The translate, in px.</param>
    /// <param name="speed">Duration in ms; 0 is instant.</param>
    public Task TranslateTo(double translate, int speed = 0) => CallAsync("translateTo", translate, speed);

    /// <summary>Reads the track's current translate, in px.</summary>
    public Task<double> GetTranslateAsync() => CallAsync<double>("getTranslate");

    /// <summary>Stop Swiper listening for pointer and resize events.</summary>
    public Task DetachEvents() => CallAsync("detachEvents");

    /// <summary>Start Swiper listening again after <see cref="DetachEvents"/>.</summary>
    public Task AttachEvents() => CallAsync("attachEvents");

    /// <summary>
    /// Reads everything the underlying Swiper knows about itself, in one interop call.
    /// </summary>
    /// <returns>The snapshot, or null before the slider has initialized.</returns>
    public Task<SwiperState?> GetStateAsync() => CallAsync<SwiperState?>("getState");

    internal async Task CallAsync(string identifier, params object?[] arguments)
    {
        if (_module is null)
        {
            return;
        }

        await _module.InvokeVoidAsync(identifier, WithElement(arguments));
    }

    internal async Task<TValue> CallAsync<TValue>(string identifier, params object?[] arguments)
    {
        if (_module is null)
        {
            return default!;
        }

        return await _module.InvokeAsync<TValue>(identifier, WithElement(arguments));
    }

    private object?[] WithElement(object?[] arguments)
    {
        var call = new object?[arguments.Length + 1];
        call[0] = _element;
        Array.Copy(arguments, 0, call, 1, arguments.Length);

        return call;
    }
}

/// <summary>Autoplay's own methods, reached through <see cref="Swiper.Autoplay"/>.</summary>
/// <remarks>
/// <c>@bind-IsAutoplayRunning</c> covers the common play/pause case with no code at all; these are for
/// when the host needs to be explicit.
/// </remarks>
public sealed class SwiperAutoplayController
{
    private readonly Swiper _swiper;

    internal SwiperAutoplayController(Swiper swiper) => _swiper = swiper;

    /// <summary>Start advancing.</summary>
    public Task Start() => _swiper.CallAsync("startAutoplay");

    /// <summary>Stop advancing. <see cref="Start"/> begins the delay again from the top.</summary>
    public Task Stop() => _swiper.CallAsync("stopAutoplay");

    /// <summary>Hold the current delay where it is.</summary>
    /// <param name="speed">Optional transition speed for the move in progress, in ms.</param>
    public Task Pause(int? speed = null) => _swiper.CallAsync("pauseAutoplay", speed);

    /// <summary>Carry on from where <see cref="Pause"/> left off.</summary>
    public Task Resume() => _swiper.CallAsync("resumeAutoplay");
}

/// <summary>Zoom's own methods, reached through <see cref="Swiper.Zoom"/>.</summary>
public sealed class SwiperZoomController
{
    private readonly Swiper _swiper;

    internal SwiperZoomController(Swiper swiper) => _swiper = swiper;

    /// <summary>Zoom the active slide in.</summary>
    /// <param name="ratio">Target factor. Null uses <see cref="SwiperZoomOptions.MaxRatio"/>.</param>
    public Task In(double? ratio = null) => _swiper.CallAsync("zoomIn", ratio);

    /// <summary>Zoom the active slide back out.</summary>
    public Task Out() => _swiper.CallAsync("zoomOut");

    /// <summary>Zoom in if out, out if in.</summary>
    public Task Toggle() => _swiper.CallAsync("zoomToggle");

    /// <summary>Allow zooming.</summary>
    public Task Enable() => _swiper.CallAsync("enableZoom");

    /// <summary>Stop zooming being possible.</summary>
    public Task Disable() => _swiper.CallAsync("disableZoom");
}

/// <summary>Keyboard control, reached through <see cref="Swiper.Keyboard"/>.</summary>
public sealed class SwiperKeyboardController
{
    private readonly Swiper _swiper;

    internal SwiperKeyboardController(Swiper swiper) => _swiper = swiper;

    /// <summary>Start reacting to the arrow keys.</summary>
    public Task Enable() => _swiper.CallAsync("enableKeyboard");

    /// <summary>Stop reacting to the arrow keys.</summary>
    public Task Disable() => _swiper.CallAsync("disableKeyboard");
}

/// <summary>Mousewheel control, reached through <see cref="Swiper.Mousewheel"/>.</summary>
public sealed class SwiperMousewheelController
{
    private readonly Swiper _swiper;

    internal SwiperMousewheelController(Swiper swiper) => _swiper = swiper;

    /// <summary>Start reacting to the wheel.</summary>
    public Task Enable() => _swiper.CallAsync("enableMousewheel");

    /// <summary>Stop reacting to the wheel.</summary>
    public Task Disable() => _swiper.CallAsync("disableMousewheel");
}

/// <summary>
/// Swiper's own slide manipulation, reached through <see cref="Swiper.Slides"/>.
/// </summary>
/// <remarks>
/// These write slide elements Blazor did not render and does not know about, so the next render that
/// touches the slide collection will fight them. They are here as an escape hatch for a host that
/// owns the slider outright. When Blazor renders the slides - the normal case - keep the
/// <c>@foreach</c> and reach for <see cref="Swiper.ArmAnchor"/> or
/// <see cref="Swiper.UpdateAndAnchor"/> instead, which keep the position honest across a change
/// Blazor makes itself.
/// </remarks>
public sealed class SwiperSlideCollection
{
    private readonly Swiper _swiper;

    internal SwiperSlideCollection(Swiper swiper) => _swiper = swiper;

    /// <summary>Add a slide at the end.</summary>
    /// <param name="markup">The slide's outer HTML.</param>
    public Task Append(string markup) => _swiper.CallAsync("appendSlide", markup);

    /// <summary>Add a slide at the start.</summary>
    /// <param name="markup">The slide's outer HTML.</param>
    public Task Prepend(string markup) => _swiper.CallAsync("prependSlide", markup);

    /// <summary>Add a slide at a position.</summary>
    /// <param name="index">Where to insert it.</param>
    /// <param name="markup">The slide's outer HTML.</param>
    public Task Insert(int index, string markup) => _swiper.CallAsync("addSlide", index, markup);

    /// <summary>Remove the slide at a position.</summary>
    /// <param name="index">The slide to remove.</param>
    public Task Remove(int index) => _swiper.CallAsync("removeSlide", index);

    /// <summary>Remove every slide.</summary>
    public Task RemoveAll() => _swiper.CallAsync("removeAllSlides");
}

/// <summary>The virtual module, reached through <see cref="Swiper.Virtual"/>.</summary>
public sealed class SwiperVirtualController
{
    private readonly Swiper _swiper;

    internal SwiperVirtualController(Swiper swiper) => _swiper = swiper;

    /// <summary>Re-measure the virtual window after the underlying collection changed.</summary>
    /// <param name="force">Rebuild even when the window looks unchanged.</param>
    public Task Update(bool force = false) => _swiper.CallAsync("updateVirtual", force);
}
