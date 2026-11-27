using System.Text.Json;
using Xunit;

namespace Blazor.Swiper.E2E;

/// <summary>
/// What a cssMode slider does with a wheel event, which decides whether anything inside it can be
/// scrolled with a wheel or a trackpad. With Swiper's mousewheel module off, Swiper 14 puts a listener
/// on the wrapper that cancels every wheel event and steps one page; with it on, cssMode leaves the
/// wheel to the browser. The wrapper switches the module on for a cssMode slider unless the caller
/// configured it, so these read the real parameters Swiper ended up with and whether a real wheel
/// event was cancelled.
/// </summary>
/// <remarks>
/// The sliders in the standalone scenarios are handed the options exactly as the component serializes
/// them - an unset member as null - and not as a bare object, because that null is what the wrapper
/// tells "never set" from "set off" by.
/// </remarks>
[Collection(DemoCollectionDefinition.Name)]
public sealed class SwiperCssModeMousewheelTests(DemoFixture fixture)
{
    private const string CssModeStory = "components-swiper--css-mode";
    private const string CssModeState = "css-mode-state";

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string Harness = @"
        const interop = await import(new URL('./_content/Kebechet.Blazor.Swiper/swiper-interop.js', document.baseURI).href);

        const wait = (milliseconds) => new Promise(resolve => setTimeout(resolve, milliseconds));

        // dispatchEvent answers false when a listener cancelled the event, i.e. when the browser was
        // not left to scroll.
        const wheelOver = (target) => target.dispatchEvent(new WheelEvent('wheel', { deltaY: 40, bubbles: true, cancelable: true }));

        const standaloneSlider = async (options) => {
            const stage = document.createElement('div');
            stage.style.width = '420px';
            document.body.appendChild(stage);

            const el = document.createElement('swiper-container');
            el.setAttribute('init', 'false');
            for (let i = 0; i < 5; i++) {
                const slide = document.createElement('swiper-slide');
                slide.textContent = `Slide ${i}`;
                slide.style.height = '120px';
                el.appendChild(slide);
            }
            stage.appendChild(el);

            await interop.initialize(el, options, null, [], 0, false);
            return { stage, el };
        };

        const readSlider = ({ stage, el }) => {
            const result = JSON.stringify({
                isModuleEnabled: el.swiper.params.mousewheel.enabled,
                sensitivity: el.swiper.params.mousewheel.sensitivity ?? null,
                isWheelLeftToBrowser: wheelOver(el.swiper.wrapperEl),
                realIndex: el.swiper.realIndex
            });
            stage.remove();
            return result;
        };
    ";

    // The wheel listeners that were added and not yet removed, by target and handler. A cssMode slider with the
    // mousewheel module on has none of Swiper's own, so what is left is what the wrapper added.
    private const string WheelListenerSpy = @"
        const wheelListeners = [];
        const addEventListener = EventTarget.prototype.addEventListener;
        const removeEventListener = EventTarget.prototype.removeEventListener;
        EventTarget.prototype.addEventListener = function (type, handler, ...rest) {
            if (type === 'wheel') {
                wheelListeners.push({ target: this, handler });
            }
            return addEventListener.call(this, type, handler, ...rest);
        };
        EventTarget.prototype.removeEventListener = function (type, handler, ...rest) {
            if (type === 'wheel') {
                const index = wheelListeners.findIndex(entry => entry.target === this && entry.handler === handler);
                if (index >= 0) {
                    wheelListeners.splice(index, 1);
                }
            }
            return removeEventListener.call(this, type, handler, ...rest);
        };
    ";

    [Fact]
    public async Task Mousewheel_CssModeSliderNeverToldAnything_LeavesTheWheelToTheBrowser()
    {
        // Arrange - the story sets CssMode and nothing about the wheel, so this is the whole path from
        // the component's options to the live Swiper
        await fixture.NavigateToStoryAsync(CssModeStory, CssModeState);

        // Act
        var result = await EvaluateAsync(@"
            const host = document.querySelector('[data-testid=""css-mode-swiper""]');
            const isModuleEnabled = host.swiper.params.mousewheel.enabled;
            const isWheelLeftToBrowser = wheelOver(host.swiper.wrapperEl);
            await wait(500);
            return JSON.stringify({ isModuleEnabled, isWheelLeftToBrowser, realIndex: host.swiper.realIndex });");

        // Assert - with the module off Swiper cancels the wheel and steps to the next slide
        Assert.True(result.IsModuleEnabled);
        Assert.True(result.IsWheelLeftToBrowser);
        Assert.Equal(0, result.RealIndex);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task Mousewheel_CssModeSliderWithTheOptionUnset_LeavesTheWheelToTheBrowser()
    {
        // Arrange
        await fixture.NavigateToStoryAsync(CssModeStory, CssModeState);

        // Act
        var result = await EvaluateAsync(@"
            return readSlider(await standaloneSlider({ cssMode: true, mousewheel: null, spaceBetween: 16 }));");

        // Assert
        Assert.True(result.IsModuleEnabled);
        Assert.True(result.IsWheelLeftToBrowser);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task Mousewheel_CssModeSliderTurnedOffExplicitly_KeepsSwipersOwnWheelStep()
    {
        // Arrange
        await fixture.NavigateToStoryAsync(CssModeStory, CssModeState);

        // Act - `Mousewheel = false` reaches the page as an object that says it is disabled
        var result = await EvaluateAsync(@"
            return readSlider(await standaloneSlider({ cssMode: true, mousewheel: { enabled: false }, spaceBetween: 16 }));");

        // Assert - the caller asked for the module off, so Swiper's wheel listener is still there
        Assert.False(result.IsModuleEnabled);
        Assert.False(result.IsWheelLeftToBrowser);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task Mousewheel_CssModeSliderWithItsOwnWheelSettings_KeepsThem()
    {
        // Arrange
        await fixture.NavigateToStoryAsync(CssModeStory, CssModeState);

        // Act
        var result = await EvaluateAsync(@"
            return readSlider(await standaloneSlider({ cssMode: true, mousewheel: { sensitivity: 3, enabled: null }, spaceBetween: 16 }));");

        // Assert
        Assert.Equal(3, result.Sensitivity);
        Assert.True(result.IsModuleEnabled);
        Assert.True(result.IsWheelLeftToBrowser);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task Mousewheel_SliderWithoutCssMode_StaysOff()
    {
        // Arrange
        await fixture.NavigateToStoryAsync(CssModeStory, CssModeState);

        // Act
        var result = await EvaluateAsync(@"
            return readSlider(await standaloneSlider({ cssMode: false, mousewheel: null }));");

        // Assert - Swiper's own default outside cssMode is the module off
        Assert.False(result.IsModuleEnabled);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task Mousewheel_SliderWithoutCssModeThatAsksForTheWheel_GetsIt()
    {
        // Arrange
        await fixture.NavigateToStoryAsync(CssModeStory, CssModeState);

        // Act
        var result = await EvaluateAsync(@"
            return readSlider(await standaloneSlider({ cssMode: null, mousewheel: { enabled: null } }));");

        // Assert
        Assert.True(result.IsModuleEnabled);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task Destroy_AfterACssModeMove_RemovesTheWheelListenerTheMoveAdded()
    {
        // Arrange
        await fixture.NavigateToStoryAsync(CssModeStory, CssModeState);

        // Act - the wrapper listens for the wheel itself once a cssMode slider has made its first move
        var result = await EvaluateAsync(WheelListenerSpy + @"
            const slider = await standaloneSlider({ cssMode: true, mousewheel: {}, spaceBetween: 16 });
            interop.slideTo(slider.el, 2, 300);
            await wait(600);
            const listenersWhileLive = wheelListeners.length;
            interop.destroy(slider.el);
            slider.stage.remove();
            return JSON.stringify({ listenersWhileLive, listenersAfterDestroy: wheelListeners.length });");

        // Assert - the listener is there to be removed, and removing it leaves nothing behind on the wrapper
        Assert.Equal(1, result.ListenersWhileLive);
        Assert.Equal(0, result.ListenersAfterDestroy);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task SlideTo_OnASliderInitializedAgainWithoutDestroying_LeavesOneWheelListenerNotTwo()
    {
        // Arrange - the element leaves the DOM and comes back, which destroys its Swiper, and the host
        // initializes it again without disposing first
        await fixture.NavigateToStoryAsync(CssModeStory, CssModeState);

        // Act
        var result = await EvaluateAsync(WheelListenerSpy + @"
            const slider = await standaloneSlider({ cssMode: true, mousewheel: {}, spaceBetween: 16 });
            interop.slideTo(slider.el, 2, 300);
            await wait(600);
            slider.el.remove();
            slider.stage.appendChild(slider.el);
            await interop.initialize(slider.el, { cssMode: true, mousewheel: {}, spaceBetween: 16 }, null, [], 0, false);
            interop.slideTo(slider.el, 3, 300);
            await wait(600);
            const listenersWhileLive = wheelListeners.length;
            interop.destroy(slider.el);
            slider.stage.remove();
            return JSON.stringify({ listenersWhileLive, listenersAfterDestroy: wheelListeners.length });");

        // Assert
        Assert.Equal(1, result.ListenersWhileLive);
        Assert.Equal(0, result.ListenersAfterDestroy);
        fixture.AssertNoJsErrors();
    }

    private async Task<MousewheelResult> EvaluateAsync(string scenario)
    {
        var json = await fixture.Page.EvaluateAsync<string>($"async () => {{ {Harness} {scenario} }}");
        return JsonSerializer.Deserialize<MousewheelResult>(json, _jsonOptions)!;
    }

    private sealed record MousewheelResult(
        bool IsModuleEnabled,
        double? Sensitivity,
        bool IsWheelLeftToBrowser,
        int RealIndex,
        int? ListenersWhileLive,
        int? ListenersAfterDestroy);
}
