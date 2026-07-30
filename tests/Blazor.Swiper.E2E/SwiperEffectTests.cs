using Xunit;

namespace Blazor.Swiper.E2E;

/// <summary>
/// Each transition effect, and the nested options record behind it.
/// </summary>
/// <remarks>
/// An effect is the one kind of option whose failure is entirely visual - a wrong parameter leaves a
/// slider that still moves to the right slide, so no index assertion would notice. What can be
/// checked is that the effect and its options reached Swiper, that Swiper installed the module, and
/// that moving through the slides raises nothing in the console.
/// </remarks>
[Collection(DemoCollectionDefinition.Name)]
public sealed class SwiperEffectTests(DemoFixture fixture)
{
    [Theory]
    [InlineData("fade", "fade-swiper")]
    [InlineData("cube", "cube-swiper")]
    [InlineData("coverflow", "coverflow-swiper")]
    [InlineData("flip", "flip-swiper")]
    [InlineData("cards", "cards-swiper")]
    [InlineData("creative", "creative-swiper")]
    public async Task Effect_Configured_ReachesSwiperAndSurvivesAMove(string effect, string testId)
    {
        // Arrange
        var storyId = $"components-swiper-effects--{effect}";
        var canvas = await fixture.NavigateToStoryAsync(storyId);

        // Act
        Assert.Equal(effect, await fixture.ReadParameterAsync<string>(testId, "effect"));
        await SwipeHelper.SwipeLeftAsync(fixture.Page, canvas.GetByTestId(testId));

        // Assert - the transform each effect writes is its own, and how far one drag travels depends on
        // how many slides are in view, so what is common is only that the slider moved forward at all
        // and that nothing threw on the way.
        await fixture.Page.WaitForFunctionAsync(
            $"() => document.querySelector('[data-testid=\"{testId}\"]').swiper.realIndex >= 1",
            null,
            new Microsoft.Playwright.PageWaitForFunctionOptions { Timeout = 10_000 });

        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task FadeEffect_CrossFade_ReachesSwiperAsANestedOption()
    {
        // Arrange
        await fixture.NavigateToStoryAsync("components-swiper-effects--fade");

        // Act & Assert
        Assert.True(await fixture.ReadParameterAsync<bool>("fade-swiper", "fadeEffect.crossFade"));
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task CoverflowEffect_ItsNumbers_ReachSwiperIntact()
    {
        // Arrange
        await fixture.NavigateToStoryAsync("components-swiper-effects--coverflow");

        // Act
        var rotate = await fixture.ReadParameterAsync<double>("coverflow-swiper", "coverflowEffect.rotate");
        var depth = await fixture.ReadParameterAsync<double>("coverflow-swiper", "coverflowEffect.depth");
        var stretch = await fixture.ReadParameterAsync<double>("coverflow-swiper", "coverflowEffect.stretch");

        // Assert - stretch is Swiper's `number | '<n>%'` union, and 0 has to survive as the number
        // it is rather than being read as "unset".
        Assert.Equal(40, rotate);
        Assert.Equal(120, depth);
        Assert.Equal(0, stretch);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task CreativeEffect_TransformArrays_ReachSwiperInOrder()
    {
        // Arrange - the transforms are arrays whose entries mix numbers and percentage strings, which
        // is the one place the options serializer has to keep a heterogeneous array intact.
        await fixture.NavigateToStoryAsync("components-swiper-effects--creative");

        // Act
        var translate = await fixture.ReadSwiperAsync<string[]>(
            "creative-swiper",
            "s.params.creativeEffect.prev.translate.map(String)");
        var rotate = await fixture.ReadSwiperAsync<double[]>("creative-swiper", "s.params.creativeEffect.prev.rotate");

        // Assert
        Assert.Equal(["-20%", "0", "-200"], translate);
        Assert.Equal([0d, 0d, -8d], rotate);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task CubeEffect_ItsShadowOptions_ReachSwiper()
    {
        // Arrange
        await fixture.NavigateToStoryAsync("components-swiper-effects--cube");

        // Act & Assert
        Assert.Equal(0.94, await fixture.ReadParameterAsync<double>("cube-swiper", "cubeEffect.shadowScale"));
        Assert.True(await fixture.ReadParameterAsync<bool>("cube-swiper", "cubeEffect.shadow"));
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task CardsEffect_ItsStackingOptions_ReachSwiper()
    {
        // Arrange
        await fixture.NavigateToStoryAsync("components-swiper-effects--cards");

        // Act & Assert
        Assert.Equal(10, await fixture.ReadParameterAsync<double>("cards-swiper", "cardsEffect.perSlideOffset"));
        Assert.Equal(3, await fixture.ReadParameterAsync<double>("cards-swiper", "cardsEffect.perSlideRotate"));
        fixture.AssertNoJsErrors();
    }
}
