using Microsoft.Playwright;
using Xunit;

namespace Blazor.Swiper.E2E;

/// <summary>
/// That handing Swiper's virtual module a host renderer really does keep the DOM small.
/// </summary>
/// <remarks>
/// This is the claim no other suite can reach. bUnit proves the window arrives and the offset lands
/// on the slides, and the interop tests prove the options are rewritten - but whether Swiper calls
/// the hook at all, keeps calling it as the slider travels, and leaves the elements alone while it
/// does, is a question about a live Swiper laying out real slides.
/// </remarks>
[Collection(DemoCollectionDefinition.Name)]
public sealed class SwiperVirtualTests(DemoFixture fixture)
{
    private const string StoryId = "components-swiper-modules--virtual-windowed";
    private const string SwiperTestId = "virtual-windowed-swiper";
    private const string StateTestId = "virtual-windowed-state";

    [Fact]
    public async Task VirtualWindowed_TwoHundredSlides_KeepsOnlyTheWindowInTheDom()
    {
        // Arrange
        var canvas = await fixture.NavigateToStoryAsync(StoryId, StateTestId);

        // Act
        var slideElements = await canvas.GetByTestId(SwiperTestId).Locator("css=swiper-slide").CountAsync();

        // Assert - the collection is 200; anything near it means the host rendered them all
        Assert.InRange(slideElements, 1, 8);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task VirtualWindowed_JumpDeepIntoTheCollection_MovesTheWindowRatherThanGrowingIt()
    {
        // Arrange
        var canvas = await fixture.NavigateToStoryAsync(StoryId, StateTestId);

        // Act
        await fixture.Page.EvaluateAsync(
            $"() => document.querySelector('[data-testid=\"{SwiperTestId}\"]').swiper.slideTo(100, 0)");

        var state = await StoryState.WaitForAsync(
            canvas,
            StateTestId,
            candidate => StoryState.Int(candidate, "from") > 90,
            "the window to follow the slider to slide 100");

        // Assert - the window brackets the active slide, and it is still only a handful of elements
        Assert.InRange(StoryState.Int(state, "from"), 95, 100);
        Assert.InRange(StoryState.Int(state, "to"), 100, 105);
        Assert.InRange(StoryState.Int(state, "renderedSlides"), 1, 8);

        var slideElements = await canvas.GetByTestId(SwiperTestId).Locator("css=swiper-slide").CountAsync();
        Assert.Equal(StoryState.Int(state, "renderedSlides"), slideElements);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task VirtualWindowed_AWindowFarFromTheStart_SitsWhereTheWholeCollectionWouldHavePutIt()
    {
        // Arrange
        var canvas = await fixture.NavigateToStoryAsync(StoryId, StateTestId);

        // Act
        await fixture.Page.EvaluateAsync(
            $"() => document.querySelector('[data-testid=\"{SwiperTestId}\"]').swiper.slideTo(100, 0)");

        var state = await StoryState.WaitForAsync(
            canvas,
            StateTestId,
            candidate => StoryState.Int(candidate, "from") > 90,
            "the window to follow the slider to slide 100");

        // Assert - the offset is what stops a three-slide window sitting at the track's origin, so
        // it has to equal the distance the slides it replaced would have covered
        var slideWidth = await fixture.Page.EvaluateAsync<double>(
            $"() => {{ const s = document.querySelector('[data-testid=\"{SwiperTestId}\"]').swiper; return s.slidesSizesGrid[0] + s.params.spaceBetween; }}");

        var expectedOffset = StoryState.Int(state, "from") * slideWidth;
        var actualOffset = state.GetProperty("offset").GetDouble();

        Assert.Equal(expectedOffset, actualOffset, 1d);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task VirtualWindowed_ARealSwipe_MovesTheWindowWithTheSlider()
    {
        // Arrange - a drag is the case programmatic navigation cannot stand in for: the window is
        // recomputed while the track is still moving under the finger, not once it has settled.
        var canvas = await fixture.NavigateToStoryAsync(StoryId, StateTestId);

        // Act
        await SwipeHelper.SwipeLeftAsync(fixture.Page, canvas.GetByTestId(SwiperTestId));

        var state = await StoryState.WaitForAsync(
            canvas,
            StateTestId,
            candidate => StoryState.Int(candidate, "activeIndex") > 0,
            "the swipe to advance the slider");

        // Assert - the window followed, and swiping did not leave the slides it passed behind
        Assert.Equal(1, StoryState.Int(state, "activeIndex"));
        Assert.InRange(StoryState.Int(state, "renderedSlides"), 1, 8);

        var slideElements = await canvas.GetByTestId(SwiperTestId).Locator("css=swiper-slide").CountAsync();
        Assert.Equal(StoryState.Int(state, "renderedSlides"), slideElements);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task VirtualWindowed_SwipingBackFromDeepInTheCollection_KeepsTheWindowConsistent()
    {
        // Arrange
        var canvas = await fixture.NavigateToStoryAsync(StoryId, StateTestId);
        await fixture.Page.EvaluateAsync(
            $"() => document.querySelector('[data-testid=\"{SwiperTestId}\"]').swiper.slideTo(100, 0)");

        await StoryState.WaitForAsync(
            canvas,
            StateTestId,
            candidate => StoryState.Int(candidate, "from") > 90,
            "the window to follow the slider to slide 100");

        // Act
        await SwipeHelper.SwipeRightAsync(fixture.Page, canvas.GetByTestId(SwiperTestId));

        var state = await StoryState.WaitForAsync(
            canvas,
            StateTestId,
            candidate => StoryState.Int(candidate, "activeIndex") == 99,
            "the swipe to go back one slide");

        // Assert - the elements the host rendered still are the window Swiper asked for
        var indices = await fixture.Page.EvaluateAsync<int[]>(
            $"() => [...document.querySelectorAll('[data-testid=\"{SwiperTestId}\"] swiper-slide')].map(s => Number(s.getAttribute('data-swiper-slide-index')))");

        Assert.Equal(StoryState.Int(state, "from"), indices.First());
        Assert.Equal(StoryState.Int(state, "to"), indices.Last());
        Assert.InRange(indices.Length, 1, 8);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task VirtualWindowed_TheRenderedSlides_CarryTheIndicesTheWindowNamed()
    {
        // Arrange
        var canvas = await fixture.NavigateToStoryAsync(StoryId, StateTestId);

        // Act
        await fixture.Page.EvaluateAsync(
            $"() => document.querySelector('[data-testid=\"{SwiperTestId}\"]').swiper.slideTo(100, 0)");

        var state = await StoryState.WaitForAsync(
            canvas,
            StateTestId,
            candidate => StoryState.Int(candidate, "from") > 90,
            "the window to follow the slider to slide 100");

        // Assert - Swiper addresses slides it did not create by this attribute, so a window whose
        // elements are not labelled is a window Swiper cannot line up
        var indices = await fixture.Page.EvaluateAsync<int[]>(
            $"() => [...document.querySelectorAll('[data-testid=\"{SwiperTestId}\"] swiper-slide')].map(s => Number(s.getAttribute('data-swiper-slide-index')))");

        Assert.Equal(StoryState.Int(state, "from"), indices.First());
        Assert.Equal(StoryState.Int(state, "to"), indices.Last());
        fixture.AssertNoJsErrors();
    }
}
