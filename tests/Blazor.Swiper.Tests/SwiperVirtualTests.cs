using Bunit;
using Shouldly;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Kebechet.Blazor.Swiper.Tests;

/// <summary>
/// The host-rendered half of Swiper's virtual module: the window arriving from
/// <c>renderExternal</c>, and the offset the wrapper puts on the slides for it.
/// </summary>
/// <remarks>
/// bUnit never runs the interop, so what these pin is the .NET side of the boundary - the callback
/// reaching the host and the offset landing on the elements. That Swiper actually calls it, and
/// that the window it computes is right, only a browser can show.
/// </remarks>
public sealed class SwiperVirtualTests : IDisposable
{
    private const string ModulePath = "./_content/Kebechet.Blazor.Swiper/swiper-interop.js";

    private readonly TestContext _context;

    public SwiperVirtualTests()
    {
        _context = new TestContext();
        _context.JSInterop.SetupModule(ModulePath).Mode = JSRuntimeMode.Loose;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task OnVirtualRenderInternal_AWindow_IsHandedToTheHost()
    {
        // Arrange
        SwiperVirtualWindow? received = null;
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnVirtualRender, window => received = window));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnVirtualRenderInternal(4, 9, 1280, "left"));

        // Assert
        received.ShouldNotBeNull();
        received!.From.ShouldBe(4);
        received.To.ShouldBe(9);
        received.Offset.ShouldBe(1280);
    }

    [Fact]
    public async Task OnVirtualRenderInternal_AWindow_PutsItsOffsetOnTheSlides()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnVirtualRender, _ => { })
            .AddChildContent<SwiperSlide>(slide => slide
                .Add(x => x.VirtualIndex, 4)
                .AddChildContent("windowed slide")));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnVirtualRenderInternal(4, 9, 1280, "left"));

        // Assert - a window of a few slides has to sit where the whole collection would have put it
        cut.Find("swiper-slide").GetAttribute("style").ShouldBe("left:1280px");
    }

    [Fact]
    public async Task OnVirtualRenderInternal_AVerticalSlider_OffsetsOnThePropertyItWasGiven()
    {
        // Arrange - direction and text direction decide the property, and only the live Swiper knows it
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnVirtualRender, _ => { })
            .AddChildContent<SwiperSlide>(slide => slide.AddChildContent("windowed slide")));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnVirtualRenderInternal(0, 2, 640, "top"));

        // Assert
        cut.Find("swiper-slide").GetAttribute("style").ShouldBe("top:640px");
    }

    [Fact]
    public async Task OnVirtualRenderInternal_AFractionalOffset_IsWrittenInvariantly()
    {
        // Arrange - a comma decimal separator would be an invalid CSS length
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnVirtualRender, _ => { })
            .AddChildContent<SwiperSlide>(slide => slide.AddChildContent("windowed slide")));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnVirtualRenderInternal(0, 2, 12.5, "left"));

        // Assert
        cut.Find("swiper-slide").GetAttribute("style").ShouldBe("left:12.5px");
    }

    [Fact]
    public async Task OnVirtualRenderInternal_ASlideWithItsOwnStyle_KeepsItAndAddsTheOffset()
    {
        // Arrange
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .Add(x => x.OnVirtualRender, _ => { })
            .AddChildContent<SwiperSlide>(slide => slide
                .AddUnmatched("style", "background:red")
                .AddChildContent("windowed slide")));

        // Act
        await cut.InvokeAsync(() => cut.Instance.OnVirtualRenderInternal(0, 2, 100, "left"));

        // Assert - the style is the caller's to write; the offset only adds to it
        cut.Find("swiper-slide").GetAttribute("style").ShouldBe("background:red;left:100px");
    }

    [Fact]
    public void Render_NoVirtualRenderHandler_LeavesTheSlidesUnoffset()
    {
        // Arrange & Act
        var cut = _context.RenderComponent<Swiper>(parameters => parameters
            .AddChildContent<SwiperSlide>(slide => slide.AddChildContent("plain slide")));

        // Assert
        cut.Find("swiper-slide").HasAttribute("style").ShouldBeFalse();
    }
}
