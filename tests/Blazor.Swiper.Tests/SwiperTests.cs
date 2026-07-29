using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Kebechet.Blazor.Swiper.Tests;

public sealed class SwiperTests : IDisposable
{
    private const string ModulePath = "./_content/Kebechet.Blazor.Swiper/swiper-interop.js";

    private readonly TestContext _context;
    private readonly BunitJSModuleInterop _module;

    public SwiperTests()
    {
        _context = new TestContext();
        _module = _context.JSInterop.SetupModule(ModulePath);
        _module.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void Render_Default_RendersUninitializedSwiperContainer()
    {
        // Arrange & Act
        var cut = _context.RenderComponent<Swiper>();

        // Assert
        cut.Markup.ShouldContain("<swiper-container");
        cut.Markup.ShouldContain("init=\"false\"");
    }

    [Fact]
    public void Render_WithChildSlides_RendersSwiperSlideElements()
    {
        // Arrange & Act
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .AddChildContent<SwiperSlide>(slide => slide.AddChildContent("First slide")));

        // Assert
        cut.Markup.ShouldContain("<swiper-slide");
        cut.Markup.ShouldContain("First slide");
    }

    [Fact]
    public void Render_FirstRender_InitializesWithSuppliedOptions()
    {
        // Arrange
        var options = new SwiperOptions { SlidesPerView = 2 };

        // Act
        _context.RenderComponent<Swiper>(parameters => parameters.Add(x => x.Options, options));

        // Assert
        var initialize = _module.Invocations.Single(x => x.Identifier == "initialize");
        initialize.Arguments[1].ShouldBe(options);
    }

    [Fact]
    public async Task SlideNext_WhenCalled_InvokesInteropMethod()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.SlideNext());

        // Assert
        _module.Invocations.ShouldContain(x => x.Identifier == "slideNext");
    }

    [Fact]
    public async Task SlideTo_WithIndexAndSpeed_PassesBothToInterop()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.SlideTo(3, 500));

        // Assert
        var slideTo = _module.Invocations.Last(x => x.Identifier == "slideTo");
        slideTo.Arguments[1].ShouldBe(3);
        slideTo.Arguments[2].ShouldBe(500);
    }

    [Fact]
    public async Task SetAllowSlideNext_WithFlag_ForwardsFlagToInterop()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.SetAllowSlideNext(false));

        // Assert
        var setAllow = _module.Invocations.Last(x => x.Identifier == "setAllowSlideNext");
        setAllow.Arguments[1].ShouldBe(false);
    }

    [Fact]
    public async Task OnSlideChangeInternal_CodeDrivenChange_UpdatesActiveIndexAndRaisesOnSlideChange()
    {
        // Arrange
        var reportedIndex = -1;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnSlideChange, EventCallback.Factory.Create<int>(this, index => reportedIndex = index)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnSlideChangeInternal(2, isUserDriven: false));

        // Assert
        cut.Instance.ActiveIndex.ShouldBe(2);
        reportedIndex.ShouldBe(2);
    }

    [Fact]
    public async Task OnSlideChangeInternal_UserDrivenChange_RaisesOnUserSlideChangeToo()
    {
        // Arrange
        var userReportedIndexes = new List<int>();
        var allReportedIndexes = new List<int>();
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnSlideChange, EventCallback.Factory.Create<int>(this, allReportedIndexes.Add))
            .Add(x => x.OnUserSlideChange, EventCallback.Factory.Create<int>(this, userReportedIndexes.Add)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnSlideChangeInternal(1, isUserDriven: false));
        await cut.InvokeAsync(() => cut.Instance.OnSlideChangeInternal(2, isUserDriven: true));

        // Assert
        allReportedIndexes.ShouldBe([1, 2]);
        userReportedIndexes.ShouldBe([2]);
    }

    [Fact]
    public void Render_AfterPositioning_DropsVisibilityHidden()
    {
        // Arrange
        var markupWhileInitializing = string.Empty;

        // Act
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnReady, EventCallback.Factory.Create(this, () => markupWhileInitializing = "captured")));

        // Assert
        markupWhileInitializing.ShouldBe("captured");
        cut.Markup.ShouldNotContain("visibility:hidden");
    }

    [Fact]
    public void WithHiddenVisibility_CallerSuppliedAStyle_AppendsRatherThanReplacingIt()
    {
        // Arrange - the caller's style is what sizes the slider, so losing it would collapse a
        // vertical Swiper for as long as it stays hidden.
        var attributes = new Dictionary<string, object> { ["style"] = "margin-top:4px" };

        // Act
        var rendered = Swiper.WithHiddenVisibility(attributes);

        // Assert
        rendered["style"].ToString().ShouldBe("margin-top:4px;visibility:hidden");
    }

    [Fact]
    public void WithHiddenVisibility_NoAttributesAtAll_StillHidesTheSlider()
    {
        // Act
        var rendered = Swiper.WithHiddenVisibility(null);

        // Assert
        rendered["style"].ToString().ShouldBe("visibility:hidden");
    }

    [Fact]
    public void WithHiddenVisibility_CallerAttributes_AreLeftUntouched()
    {
        // Arrange - the dictionary handed in belongs to the caller and is reused across renders.
        var attributes = new Dictionary<string, object> { ["class"] = "pager" };

        // Act
        Swiper.WithHiddenVisibility(attributes);

        // Assert
        attributes.ShouldNotContainKey("style");
    }

    [Fact]
    public async Task OnTransitionEndInternal_WhenInvoked_RaisesOnTransitionEnd()
    {
        // Arrange
        var hasSettled = false;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnTransitionEnd, EventCallback.Factory.Create(this, () => hasSettled = true)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnTransitionEndInternal());

        // Assert
        hasSettled.ShouldBeTrue();
    }

    [Fact]
    public async Task OnReachEndInternal_WhenInvoked_RaisesOnReachEnd()
    {
        // Arrange
        var hasReachedEnd = false;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnReachEnd, EventCallback.Factory.Create(this, () => hasReachedEnd = true)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnReachEndInternal());

        // Assert
        hasReachedEnd.ShouldBeTrue();
    }

    [Fact]
    public void Render_AfterInitialization_RaisesOnReady()
    {
        // Arrange
        var isReady = false;

        // Act
        _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnReady, EventCallback.Factory.Create(this, () => isReady = true)));

        // Assert
        isReady.ShouldBeTrue();
    }

    [Fact]
    public async Task DisposeAsync_WhenDisposed_DestroysUnderlyingSwiper()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>();

        // Act
        await cut.Instance.DisposeAsync();

        // Assert
        _module.Invocations.ShouldContain(x => x.Identifier == "destroy");
    }
}
