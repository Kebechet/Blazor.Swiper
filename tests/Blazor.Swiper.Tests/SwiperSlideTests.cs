using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Kebechet.Blazor.Swiper.Tests;

/// <summary>
/// The slide-level parameters, which Swiper reads off the slide element rather than from the
/// slider's options - which is why they are parameters here rather than members of SwiperOptions.
/// </summary>
public sealed class SwiperSlideTests : IDisposable
{
    private readonly TestContext _context;

    public SwiperSlideTests()
    {
        _context = new TestContext();
        _context.JSInterop.SetupModule("./_content/Kebechet.Blazor.Swiper/swiper-interop.js").Mode = JSRuntimeMode.Loose;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void Render_Lazy_MarksTheSlideForDeferredLoading()
    {
        // Arrange & Act
        var cut = _context.RenderComponent<SwiperSlide>(parameters => parameters.Add(x => x.Lazy, true));

        // Assert
        cut.Markup.ShouldContain("lazy=\"true\"");
    }

    [Fact]
    public void Render_Hash_WritesTheAttributeHashNavigationReads()
    {
        // Arrange & Act
        var cut = _context.RenderComponent<SwiperSlide>(parameters => parameters.Add(x => x.Hash, "features"));

        // Assert
        cut.Markup.ShouldContain("data-hash=\"features\"");
    }

    [Fact]
    public void Render_AutoplayDelay_WritesThePerSlideDelayAttribute()
    {
        // Arrange & Act - this is how a single slide is held longer than the rest.
        var cut = _context.RenderComponent<SwiperSlide>(parameters => parameters.Add(x => x.AutoplayDelay, 5000));

        // Assert
        cut.Markup.ShouldContain("data-swiper-autoplay=\"5000\"");
    }

    [Fact]
    public void Render_VirtualIndex_WritesTheIndexTheVirtualModuleAddressesSlidesBy()
    {
        // Arrange & Act
        var cut = _context.RenderComponent<SwiperSlide>(parameters => parameters.Add(x => x.VirtualIndex, 12));

        // Assert
        cut.Markup.ShouldContain("data-swiper-slide-index=\"12\"");
    }

    [Fact]
    public void Render_ZoomMaxRatio_WritesThePerSlideZoomAttribute()
    {
        // Arrange & Act
        var cut = _context.RenderComponent<SwiperSlide>(parameters => parameters.Add(x => x.ZoomMaxRatio, 4));

        // Assert
        cut.Markup.ShouldContain("data-swiper-zoom=\"4\"");
    }

    [Fact]
    public void Render_Zoom_WrapsTheContentInSwipersZoomContainer()
    {
        // Arrange & Act - only content inside that element zooms, and forgetting it is the usual
        // reason zoom appears to do nothing.
        var cut = _context.RenderComponent<SwiperSlide>(parameters => parameters
            .Add(x => x.Zoom, true)
            .AddChildContent("content"));

        // Assert
        cut.Markup.ShouldContain("swiper-zoom-container");
    }

    [Fact]
    public void Render_NoZoom_LeavesTheContentUnwrapped()
    {
        // Arrange & Act
        var cut = _context.RenderComponent<SwiperSlide>(parameters => parameters
            .AddChildContent("content"));

        // Assert
        cut.Markup.ShouldNotContain("swiper-zoom-container");
    }

    [Fact]
    public void Render_CallerAttributes_AreForwardedAndLeftUntouched()
    {
        // Arrange - the dictionary belongs to the caller and is reused across renders, so the slide
        // copies it rather than adding its own attributes to it.
        var attributes = new Dictionary<string, object> { ["class"] = "card" };

        // Act
        var cut = _context.RenderComponent<SwiperSlide>(parameters => parameters
            .Add(x => x.Lazy, true)
            .Add(x => x.AdditionalAttributes, attributes));

        // Assert
        cut.Markup.ShouldContain("class=\"card\"");
        attributes.ShouldNotContainKey("lazy");
    }

    [Fact]
    public void Render_NoSlideLevelParameters_WritesNoExtraAttributes()
    {
        // Arrange & Act
        var cut = _context.RenderComponent<SwiperSlide>();

        // Assert - an unset slide parameter must not write an attribute at all, since Swiper reads
        // the presence of several of these rather than their value.
        cut.Markup.ShouldNotContain("data-");
        cut.Markup.ShouldNotContain("lazy");
    }

    [Fact]
    public void Context_TheActiveSlide_KnowsItIsActive()
    {
        // Arrange & Act
        var cut = RenderSlides(activeIndex: 1);

        // Assert
        cut.Markup.ShouldContain("slide-1:active");
    }

    [Fact]
    public void Context_TheSlidesEitherSide_KnowWhereTheyAre()
    {
        // Arrange & Act
        var cut = RenderSlides(activeIndex: 1);

        // Assert
        cut.Markup.ShouldContain("slide-0:previous");
        cut.Markup.ShouldContain("slide-2:next");
    }

    [Fact]
    public async Task Context_SliderMoved_FollowsTheNewActiveSlide()
    {
        // Arrange - the state is derived from the slider's active index rather than read back out of
        // Swiper, so the slides have to be told to re-render when it changes.
        var cut = RenderSlides(activeIndex: 0);
        var swiper = cut.FindComponent<Swiper>();

        // Act
        await cut.InvokeAsync(() => swiper.Instance.OnSlideChangeInternal(2, isUserDriven: true));

        // Assert
        cut.Markup.ShouldContain("slide-2:active");
        cut.Markup.ShouldNotContain("slide-0:active");
    }

    /// <summary>Three slides that render their own state, so the markup says what each one thinks it is.</summary>
    private IRenderedFragment RenderSlides(int activeIndex)
    {
        return _context.Render(builder =>
        {
            builder.OpenComponent<Swiper>(0);
            builder.AddAttribute(1, nameof(Swiper.ActiveIndex), activeIndex);
            builder.AddAttribute(2, nameof(Swiper.ChildContent), (RenderFragment)(slidesBuilder =>
            {
                for (var index = 0; index < 3; index++)
                {
                    var slideIndex = index;
                    slidesBuilder.OpenComponent<SwiperSlide>(slideIndex);
                    slidesBuilder.AddAttribute(1, nameof(SwiperSlide.Index), slideIndex);
                    slidesBuilder.AddAttribute(2, nameof(SwiperSlide.SlideContent), (RenderFragment<SwiperSlideContext>)(state => contentBuilder =>
                        contentBuilder.AddContent(0, $"slide-{state.Index}:{Describe(state)}")));
                    slidesBuilder.CloseComponent();
                }
            }));
            builder.CloseComponent();
        });
    }

    private static string Describe(SwiperSlideContext state)
    {
        if (state.IsActive)
        {
            return "active";
        }

        if (state.IsNext)
        {
            return "next";
        }

        return state.IsPrevious ? "previous" : "other";
    }
}
