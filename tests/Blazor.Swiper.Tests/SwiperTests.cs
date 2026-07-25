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
    public void Renders_swiper_container_uninitialized()
    {
        // Arrange & Act
        var cut = _context.RenderComponent<Swiper>();

        // Assert
        cut.Markup.ShouldContain("<swiper-container");
        cut.Markup.ShouldContain("init=\"false\"");
    }

    [Fact]
    public void Renders_child_slides_as_swiper_slide_elements()
    {
        // Arrange & Act
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .AddChildContent<SwiperSlide>(slide => slide.AddChildContent("First slide")));

        // Assert
        cut.Markup.ShouldContain("<swiper-slide");
        cut.Markup.ShouldContain("First slide");
    }

    [Fact]
    public void Initializes_with_the_supplied_options_on_first_render()
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
    public async Task SlideNext_invokes_the_interop_method()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.SlideNext());

        // Assert
        _module.Invocations.ShouldContain(x => x.Identifier == "slideNext");
    }

    [Fact]
    public async Task SlideTo_passes_the_index_and_speed()
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
    public async Task SetAllowSlideNext_forwards_the_flag()
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
    public async Task OnSlideChangeInternal_updates_ActiveIndex_and_raises_the_callback()
    {
        // Arrange
        var reportedIndex = -1;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnSlideChange, EventCallback.Factory.Create<int>(this, index => reportedIndex = index)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnSlideChangeInternal(2));

        // Assert
        cut.Instance.ActiveIndex.ShouldBe(2);
        reportedIndex.ShouldBe(2);
    }

    [Fact]
    public async Task OnTransitionEndInternal_raises_the_callback()
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
    public async Task OnReachEndInternal_raises_the_callback()
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
    public void OnReady_is_raised_after_initialization()
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
    public async Task Disposing_destroys_the_underlying_swiper()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>();

        // Act
        await cut.Instance.DisposeAsync();

        // Assert
        _module.Invocations.ShouldContain(x => x.Identifier == "destroy");
    }
}
