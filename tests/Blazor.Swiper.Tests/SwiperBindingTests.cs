using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Kebechet.Blazor.Swiper.Tests;

/// <summary>
/// Two-way binding, and the loop it has to avoid.
/// </summary>
/// <remarks>
/// A bound slider reports its move to the host, the host re-renders with the new value, and the
/// component sees a parameter it did not have before. Without a guard that reads as the host asking
/// for a move, and the slider chases its own tail. These assert both that the binding works and that
/// the echo goes nowhere.
/// </remarks>
public sealed class SwiperBindingTests : IDisposable
{
    private const string ModulePath = "./_content/Kebechet.Blazor.Swiper/swiper-interop.js";

    private readonly TestContext _context;
    private readonly BunitJSModuleInterop _module;

    public SwiperBindingTests()
    {
        _context = new TestContext();
        _module = _context.JSInterop.SetupModule(ModulePath);
        _module.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private int SlideToCallCount => _module.Invocations.Count(x => x.Identifier == "slideTo");

    [Fact]
    public void ActiveIndex_SetByTheHost_MovesTheSlider()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>();

        // Act
        cut.SetParametersAndRender(parameters => parameters.Add(x => x.ActiveIndex, 3));

        // Assert
        var slideTo = _module.Invocations.Last(x => x.Identifier == "slideTo");
        slideTo.Arguments[1].ShouldBe(3);
    }

    [Fact]
    public async Task ActiveIndex_SliderMoved_ReportsTheNewIndexBack()
    {
        // Arrange
        var reported = -1;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.ActiveIndexChanged, EventCallback.Factory.Create<int>(this, index => reported = index)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnSlideChangeInternal(2, isUserDriven: true));

        // Assert
        reported.ShouldBe(2);
        cut.Instance.ActiveIndex.ShouldBe(2);
    }

    [Fact]
    public async Task ActiveIndex_HostEchoingBackWhatTheSliderReported_DoesNotMoveItAgain()
    {
        // Arrange - this is the feedback loop the guard exists for: the slider moves, the host
        // re-renders with the index it was just told, and the component must read that as agreement
        // rather than as an instruction.
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.ActiveIndexChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        await cut.InvokeAsync(() => cut.Instance.OnSlideChangeInternal(2, isUserDriven: true));
        var movesBefore = SlideToCallCount;

        // Act
        cut.SetParametersAndRender(parameters => parameters
            .Add(x => x.ActiveIndex, 2)
            .Add(x => x.ActiveIndexChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert
        SlideToCallCount.ShouldBe(movesBefore);
    }

    [Fact]
    public void ActiveIndex_SuppliedBeforeInit_BecomesTheSlideTheSliderOpensOn()
    {
        // Arrange & Act - seeded as initialSlide rather than moved to afterwards, because a SlideTo
        // once the slider exists is a second frame and the first one shows slide 0.
        _context.RenderComponent<Swiper>(parameters => parameters.Add(x => x.ActiveIndex, 2));

        // Assert
        var options = (SwiperOptions)_module.Invocations.Single(x => x.Identifier == "initialize").Arguments[1]!;
        options.InitialSlide.ShouldBe(2);
        _module.Invocations.ShouldNotContain(x => x.Identifier == "slideTo");
    }

    [Fact]
    public void ActiveIndex_SuppliedAlongsideAnExplicitInitialSlide_LeavesTheExplicitOneAlone()
    {
        // Arrange & Act - an explicit InitialSlide is the caller being specific about the opening
        // slide, which is a different question from where the binding currently points.
        _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.ActiveIndex, 2)
            .Add(x => x.Options, new SwiperOptions { InitialSlide = 4 }));

        // Assert
        var options = (SwiperOptions)_module.Invocations.Single(x => x.Identifier == "initialize").Arguments[1]!;
        options.InitialSlide.ShouldBe(4);
    }

    [Fact]
    public async Task IsAutoplayRunning_AutoplayStoppedItself_ReportsItBack()
    {
        // Arrange - autoplay stops on interaction and at the last slide, so the host's own flag would
        // go stale without this. It is the case the binding is for.
        var running = true;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.IsAutoplayRunning, true)
            .Add(x => x.IsAutoplayRunningChanged, EventCallback.Factory.Create<bool>(this, value => running = value)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnSwiperEventInternal("autoplayStop", "null"));

        // Assert
        running.ShouldBeFalse();
    }

    [Fact]
    public void IsAutoplayRunning_TurnedOnByTheHost_StartsAutoplay()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>();

        // Act
        cut.SetParametersAndRender(parameters => parameters.Add(x => x.IsAutoplayRunning, true));

        // Assert
        _module.Invocations.ShouldContain(x => x.Identifier == "startAutoplay");
    }

    [Fact]
    public void IsAutoplayRunning_TurnedOffByTheHost_StopsAutoplay()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>(parameters => parameters.Add(x => x.IsAutoplayRunning, true));

        // Act
        cut.SetParametersAndRender(parameters => parameters.Add(x => x.IsAutoplayRunning, false));

        // Assert
        _module.Invocations.ShouldContain(x => x.Identifier == "stopAutoplay");
    }

    [Fact]
    public async Task IsAutoplayRunning_HostEchoingBackWhatAutoplayReported_DoesNotDriveItAgain()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.IsAutoplayRunningChanged, EventCallback.Factory.Create<bool>(this, _ => { })));

        await cut.InvokeAsync(() => cut.Instance.OnSwiperEventInternal("autoplayStart", "null"));
        var callsBefore = _module.Invocations.Count(x => x.Identifier is "startAutoplay" or "stopAutoplay");

        // Act
        cut.SetParametersAndRender(parameters => parameters
            .Add(x => x.IsAutoplayRunning, true)
            .Add(x => x.IsAutoplayRunningChanged, EventCallback.Factory.Create<bool>(this, _ => { })));

        // Assert
        _module.Invocations.Count(x => x.Identifier is "startAutoplay" or "stopAutoplay").ShouldBe(callsBefore);
    }

    [Fact]
    public void Options_ChangedAfterInit_ArePushedToTheLiveSlider()
    {
        // Arrange - the parameter used to be read once and never again, which made a changed option
        // look live while doing nothing.
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.Options, new SwiperOptions { SlidesPerView = 1 }));

        // Act
        cut.SetParametersAndRender(parameters => parameters
            .Add(x => x.Options, new SwiperOptions { SlidesPerView = 3 }));

        // Assert
        _module.Invocations.ShouldContain(x => x.Identifier == "updateOptions");
    }

    [Fact]
    public void Options_RebuiltButUnchanged_AreNotPushedAgain()
    {
        // Arrange - a record rebuilt on every render is a different instance each time, so identity
        // would report a change on every render and push an update to the slider each one.
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.Options, new SwiperOptions { SlidesPerView = 2, Pagination = true }));

        // Act
        cut.SetParametersAndRender(parameters => parameters
            .Add(x => x.Options, new SwiperOptions { SlidesPerView = 2, Pagination = true }));

        // Assert
        _module.Invocations.ShouldNotContain(x => x.Identifier == "updateOptions");
    }
}
