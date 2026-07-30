using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace Blazor.Swiper.E2E;

/// <summary>
/// The behaviours that only exist in a browser. bUnit stubs the interop module out entirely, so
/// none of what is asserted here - the custom element upgrading, the reveal handshake, a real
/// pointer drag, loop's logical index - is reachable from the unit suite.
/// </summary>
[Collection(DemoCollectionDefinition.Name)]
public sealed class SwiperInteropTests(DemoFixture fixture)
{
    private const string ProgrammaticStory = "components-swiper--programmatic-control";
    private const string DistinctionStory = "components-swiper--user-vs-code-driven-changes";
    private const string LoopStory = "components-swiper--loop";
    private const string UntypedStory = "components-swiper--untyped-parameters";

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Slider_StoryLoaded_IsRevealedOnceItHasPositionedItsFirstSlide()
    {
        // Arrange & Act
        var canvas = await fixture.NavigateToStoryAsync(ProgrammaticStory, "programmatic-state");

        // Assert - the component renders the container with visibility:hidden until OnReady has
        // run, so that a stack of unpositioned slides is never painted. If the reveal regressed,
        // the slider would stay invisible forever rather than fail loudly anywhere else.
        await fixture.Page.WaitForFunctionAsync(
            @"() => {
                const container = document.querySelector('[data-testid=""programmatic-swiper""]');
                return !!container && getComputedStyle(container).visibility === 'visible';
            }",
            null,
            new PageWaitForFunctionOptions { Timeout = 15_000 });

        var state = await ReadStateAsync(canvas, "programmatic-state");
        Assert.True(state.IsReady, "OnReady never reached the host.");
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task ProgrammaticNext_ButtonClicked_AdvancesTheActiveSlide()
    {
        // Arrange
        var canvas = await fixture.NavigateToStoryAsync(ProgrammaticStory, "programmatic-state");

        // Act
        await canvas.GetByTestId("slide-next").ClickAsync();

        // Assert
        await WaitForStateAsync(canvas, "programmatic-state", state => state.ActiveIndex == 1, "the slider to advance to slide 1");

        // Act - and back to the first slide by index rather than by stepping.
        await canvas.GetByTestId("slide-first").ClickAsync();

        // Assert
        await WaitForStateAsync(canvas, "programmatic-state", state => state.ActiveIndex == 0, "the slider to return to slide 0");
        fixture.AssertNoJsErrors();
    }

    /// <summary>
    /// The half of the OnUserSlideChange contract that a unit test cannot fake: a move the host
    /// made itself must not come back as if the user had swiped, or a host that reacts to changes
    /// by moving the slider feeds its own move back into itself.
    /// </summary>
    [Fact]
    public async Task ProgrammaticMove_ButtonClicked_IsNotReportedAsAUserSlideChange()
    {
        // Arrange
        var canvas = await fixture.NavigateToStoryAsync(DistinctionStory, "distinction-state");

        // Act
        await canvas.GetByTestId("move-by-code").ClickAsync();

        // Assert
        var state = await WaitForStateAsync(
            canvas,
            "distinction-state",
            candidate => candidate.SlideChanges >= 1,
            "the code-driven move to be reported");

        Assert.Equal(1, state.ActiveIndex);
        Assert.Equal(0, state.UserSlideChanges);
        fixture.AssertNoJsErrors();
    }

    /// <summary>
    /// The other half: a real drag must raise both callbacks. Driven through the pointer rather
    /// than through Swiper's API, because sliderFirstMove - the only signal that distinguishes the
    /// two - is raised by the drag handling itself and by nothing else.
    /// </summary>
    [Fact]
    public async Task UserSwipe_RealPointerDrag_IsReportedAsAUserSlideChange()
    {
        // Arrange
        var canvas = await fixture.NavigateToStoryAsync(DistinctionStory, "distinction-state");

        // Act
        await SwipeHelper.SwipeLeftAsync(fixture.Page, canvas.GetByTestId("distinction-swiper"));

        // Assert
        var state = await WaitForStateAsync(
            canvas,
            "distinction-state",
            candidate => candidate.SlideChanges >= 1,
            "the swipe to move the slider");

        Assert.Equal(1, state.ActiveIndex);
        Assert.True(state.UserSlideChanges >= 1, $"The drag was not reported as user-driven. State: {state}");
        fixture.AssertNoJsErrors();
    }

    /// <summary>
    /// Loop mode duplicates slides, so Swiper's own activeIndex counts positions the host's
    /// collection does not have. Everything crossing the interop boundary must be the logical
    /// index instead, on both the way in and the way out.
    /// </summary>
    [Fact]
    public async Task LoopMode_ProgrammaticMove_SettlesOnTheLogicalIndexNotTheShiftedOne()
    {
        // Arrange
        var canvas = await fixture.NavigateToStoryAsync(LoopStory, "loop-state");

        // Act
        await canvas.GetByTestId("loop-to-third").ClickAsync();

        // Assert
        await WaitForStateAsync(canvas, "loop-state", state => state.ActiveIndex == 2, "loop navigation to settle on slide 2");
        fixture.AssertNoJsErrors();
    }

    /// <summary>
    /// SwiperOptions covers only a subset of Swiper's parameters, and the documented way to reach the
    /// rest is to put them on the component as plain attributes. That route runs entirely outside the
    /// typed options - Blazor forwards the attribute, and Swiper Element parses it at initialize() -
    /// so nothing but a browser can show that the two arrive together and neither clobbers the other.
    /// </summary>
    [Fact]
    public async Task UntypedParameter_SetAsAnAttribute_ReachesSwiperAlongsideTheTypedOptions()
    {
        // Arrange
        await fixture.NavigateToStoryAsync(UntypedStory, "untyped-state");

        // Act
        var slidesPerView = await fixture.ReadParameterAsync<double>("untyped-swiper", "slidesPerView");
        var slidesPerGroup = await fixture.ReadParameterAsync<int>("untyped-swiper", "slidesPerGroup");
        var grabCursor = await fixture.ReadParameterAsync<bool>("untyped-swiper", "grabCursor");

        // Assert - the first came from Options, the other two from attributes Swiper Element parsed.
        Assert.Equal(2d, slidesPerView);
        Assert.Equal(2, slidesPerGroup);
        Assert.True(grabCursor, "grab-cursor did not reach Swiper as a parameter.");
        fixture.AssertNoJsErrors();
    }

    private static async Task<SwiperState> WaitForStateAsync(
        ILocator canvas,
        string testId,
        Func<SwiperState, bool> predicate,
        string expectation)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        SwiperState? lastState = null;

        while (DateTime.UtcNow < deadline)
        {
            lastState = await ReadStateAsync(canvas, testId);
            if (predicate(lastState))
            {
                return lastState;
            }

            await Task.Delay(100);
        }

        // The last state is printed rather than just the expectation: almost every failure here is
        // the slider doing something specific and wrong, not the harness failing to observe it.
        Assert.Fail($"Timed out waiting for {expectation}. Last observed state: {lastState}");
        throw new InvalidOperationException("unreachable");
    }

    private static async Task<SwiperState> ReadStateAsync(ILocator canvas, string testId)
    {
        var json = await canvas.GetByTestId(testId).InnerTextAsync();
        return JsonSerializer.Deserialize<SwiperState>(json, _jsonOptions)
            ?? throw new InvalidOperationException($"The '{testId}' panel held no state.");
    }

    private sealed record SwiperState(int ActiveIndex, int SlideChanges, int UserSlideChanges, bool IsReady);
}
