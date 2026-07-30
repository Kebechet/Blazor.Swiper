using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Kebechet.Blazor.Swiper.Tests;

/// <summary>
/// The two halves of the event surface: which events are listened for, and what happens when one
/// arrives.
/// </summary>
/// <remarks>
/// bUnit never runs swiper-interop.js, so nothing here proves an event fires in a browser - that is
/// the e2e suite's job. What it does prove is the wiring either side of the boundary: that an
/// unsubscribed event is never asked for, and that a payload arriving as JSON reaches the right
/// callback as the right type.
/// </remarks>
public sealed class SwiperEventTests : IDisposable
{
    private const string ModulePath = "./_content/Kebechet.Blazor.Swiper/swiper-interop.js";

    private readonly TestContext _context;
    private readonly BunitJSModuleInterop _module;

    public SwiperEventTests()
    {
        _context = new TestContext();
        _module = _context.JSInterop.SetupModule(ModulePath);
        _module.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private string[] SubscriptionsSentToInitialize()
    {
        var initialize = _module.Invocations.Single(x => x.Identifier == "initialize");

        return (string[])initialize.Arguments[3]!;
    }

    [Fact]
    public void Initialize_NoCallbacksWired_SubscribesToNothing()
    {
        // Arrange & Act - the point of the opt-in: a slider nobody is listening to should cost no DOM
        // listeners and no interop traffic at all. Several of these events fire per animation frame.
        _context.RenderComponent<Swiper>();

        // Assert
        SubscriptionsSentToInitialize().ShouldBeEmpty();
    }

    [Fact]
    public void Initialize_SomeCallbacksWired_SubscribesToExactlyThose()
    {
        // Arrange & Act
        _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnProgress, EventCallback.Factory.Create<double>(this, _ => { }))
            .Add(x => x.OnReachEnd, EventCallback.Factory.Create(this, () => { }))
            .Add(x => x.OnZoomChange, EventCallback.Factory.Create<SwiperZoomChange>(this, _ => { })));

        // Assert
        SubscriptionsSentToInitialize().ShouldBe(["progress", "reachEnd", "zoomChange"], ignoreOrder: true);
    }

    [Fact]
    public void Initialize_AutoplayBoundButNoAutoplayCallbacks_StillSubscribesToStartAndStop()
    {
        // Arrange & Act - @bind-IsAutoplayRunning has nothing to follow unless those two are listened
        // for, and autoplay stopping itself is exactly what the binding exists to report.
        _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.IsAutoplayRunningChanged, EventCallback.Factory.Create<bool>(this, _ => { })));

        // Assert
        SubscriptionsSentToInitialize().ShouldBe(["autoplayStart", "autoplayStop"], ignoreOrder: true);
    }

    [Fact]
    public void Parameters_ACallbackWiredAfterInit_ResendsTheSubscriptions()
    {
        // Arrange - a host can subscribe conditionally, e.g. a diagnostics panel that watches progress
        // only while it is open. A list fixed at init would never hear it.
        var cut = _context.RenderComponent<Swiper>();

        // Act
        cut.SetParametersAndRender(parameters => parameters
            .Add(x => x.OnProgress, EventCallback.Factory.Create<double>(this, _ => { })));

        // Assert
        var subscriptions = (string[])_module.Invocations.Last(x => x.Identifier == "setSubscriptions").Arguments[1]!;
        subscriptions.ShouldBe(["progress"]);
    }

    [Fact]
    public void Parameters_NothingAboutTheSubscriptionsChanged_DoesNotResendThem()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnReachEnd, EventCallback.Factory.Create(this, () => { })));

        // Act
        cut.SetParametersAndRender(parameters => parameters
            .Add(x => x.OnReachEnd, EventCallback.Factory.Create(this, () => { })));

        // Assert
        _module.Invocations.ShouldNotContain(x => x.Identifier == "setSubscriptions");
    }

    [Fact]
    public async Task Dispatch_AnEventWithNoPayload_RaisesItsCallback()
    {
        // Arrange
        var raised = false;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnReachEnd, EventCallback.Factory.Create(this, () => raised = true)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnSwiperEventInternal("reachEnd", "null"));

        // Assert
        raised.ShouldBeTrue();
    }

    [Fact]
    public async Task Dispatch_ANumericPayload_ArrivesAsANumber()
    {
        // Arrange
        var progress = 0d;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnProgress, EventCallback.Factory.Create<double>(this, value => progress = value)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnSwiperEventInternal("progress", "0.25"));

        // Assert
        progress.ShouldBe(0.25);
    }

    [Fact]
    public async Task Dispatch_APointerPayload_ArrivesWithItsCoordinatesAndSlide()
    {
        // Arrange - Swiper hands these the raw DOM event, which cannot cross the boundary, so the
        // interop projects it. This asserts the shape those projections have to keep.
        SwiperPointerEventArgs? tapped = null;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnTap, EventCallback.Factory.Create<SwiperPointerEventArgs>(this, args => tapped = args)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnSwiperEventInternal("tap", """{"clientX":120,"clientY":40,"slideIndex":2}"""));

        // Assert
        tapped.ShouldNotBeNull();
        tapped!.ClientX.ShouldBe(120);
        tapped.ClientY.ShouldBe(40);
        tapped.SlideIndex.ShouldBe(2);
    }

    [Fact]
    public async Task Dispatch_AnObjectPayload_ArrivesAsItsRecord()
    {
        // Arrange
        SwiperAutoplayTimeLeft? timeLeft = null;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnAutoplayTimeLeft, EventCallback.Factory.Create<SwiperAutoplayTimeLeft>(this, args => timeLeft = args)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnSwiperEventInternal("autoplayTimeLeft", """{"timeLeft":1500,"percentage":0.5}"""));

        // Assert
        timeLeft.ShouldNotBeNull();
        timeLeft!.TimeLeft.ShouldBe(1500);
        timeLeft.Percentage.ShouldBe(0.5);
    }

    [Fact]
    public async Task Dispatch_AStringPayload_ArrivesAsAString()
    {
        // Arrange
        var key = string.Empty;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnKeyPress, EventCallback.Factory.Create<string>(this, value => key = value)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnSwiperEventInternal("keyPress", "\"ArrowRight\""));

        // Assert
        key.ShouldBe("ArrowRight");
    }

    [Fact]
    public async Task Dispatch_AnEventNothingHandles_IsIgnoredRatherThanThrowing()
    {
        // Arrange - a subscription can outlive by one interop hop the render that removed its
        // callback, and an exception here would surface in the host's error boundary.
        var cut = _context.RenderComponent<Swiper>();

        // Act & Assert
        await Should.NotThrowAsync(() => cut.InvokeAsync(() => cut.Instance.OnSwiperEventInternal("progress", "0.5")));
    }

    [Fact]
    public async Task Dispatch_APayloadThatNeverArrived_LeavesTheCallbackWithADefault()
    {
        // Arrange - the events that carry nothing still send the literal "null", so every payload
        // read has to survive it.
        SwiperPointerEventArgs? args = null;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnTouchStart, EventCallback.Factory.Create<SwiperPointerEventArgs>(this, value => args = value)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnSwiperEventInternal("touchStart", "null"));

        // Assert
        args.ShouldNotBeNull();
        args!.SlideIndex.ShouldBe(-1);
    }

    [Fact]
    public void Initialize_AThrottleConfigured_IsPassedToTheInterop()
    {
        // Arrange & Act
        _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.EventThrottle, TimeSpan.FromMilliseconds(50))
            .Add(x => x.OnProgress, EventCallback.Factory.Create<double>(this, _ => { })));

        // Assert
        var initialize = _module.Invocations.Single(x => x.Identifier == "initialize");
        initialize.Arguments[4].ShouldBe(50d);
    }

    [Fact]
    public void Initialize_NoThrottleConfigured_DeliversEveryEvent()
    {
        // Arrange & Act
        _context.RenderComponent<Swiper>();

        // Assert - zero rather than null, because the interop reads it as "no interval to wait out".
        var initialize = _module.Invocations.Single(x => x.Identifier == "initialize");
        initialize.Arguments[4].ShouldBe(0d);
    }
}
