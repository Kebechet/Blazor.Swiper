using System.Text.Json;
using Xunit;

namespace Blazor.Swiper.E2E;

/// <summary>
/// A real <c>&lt;swiper-container&gt;</c> leaving the DOM. Its disconnectedCallback destroys the Swiper,
/// and Swiper's destroy strips every own property of the instance but clears <c>.swiper</c> only on the
/// shadow <c>.swiper</c> div - so the host keeps pointing at the gutted instance. Anything the wrapper
/// still has in flight at that moment (a late interop call, an observer, a pending selector) must treat
/// that instance as gone. The node suite can only fake the gutted shape; this is the real element.
/// </summary>
/// <remarks>
/// Each scenario builds its own slider on an already loaded story page and drives the interop module
/// directly, because the thing under test is what happens after the element is gone - which is exactly
/// the part a Blazor-hosted story can no longer be asked to do. Errors raised from observers and
/// promises are collected in the page and returned, so a failure names the exception rather than
/// surfacing later as an unrelated console line.
/// </remarks>
[Collection(DemoCollectionDefinition.Name)]
public sealed class SwiperDestroyedTests(DemoFixture fixture)
{
    private const string AnyStory = "components-swiper--programmatic-control";

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Shared page-side setup: the interop module, a way to build a slider that has not been initialised
    // yet, a collector for errors that no caller can catch, and a wait long enough for observers and
    // animation frames to deliver.
    private const string Harness = @"
        const interop = await import(new URL('./_content/Kebechet.Blazor.Swiper/swiper-interop.js', document.baseURI).href);

        const errors = [];
        const onError = (event) => errors.push(String(event.error?.message ?? event.message));
        const onRejection = (event) => errors.push(String(event.reason?.message ?? event.reason));
        window.addEventListener('error', onError);
        window.addEventListener('unhandledrejection', onRejection);

        const createHost = (id) => {
            const host = document.createElement('swiper-container');
            host.setAttribute('init', 'false');
            if (id) {
                host.id = id;
            }
            for (let i = 0; i < 3; i++) {
                const slide = document.createElement('swiper-slide');
                slide.textContent = `Slide ${i}`;
                slide.style.height = `${100 + i * 20}px`;
                host.appendChild(slide);
            }
            document.body.appendChild(host);
            return host;
        };

        // The wrapper renders init=""false"", so a reconnected container stays dead until something
        // initialises it again - done by hand here to put a second instance on the same host.
        const replaceInstance = (host) => {
            host.remove();
            document.body.appendChild(host);
            host.initialized = false;
            host.initialize();
        };

        const dotNetCalls = [];
        const dotNetRef = {
            invokeMethodAsync: (method, ...args) => {
                dotNetCalls.push([method, ...args]);
                return Promise.resolve();
            }
        };

        const settle = async () => {
            for (let i = 0; i < 3; i++) {
                await new Promise(resolve => requestAnimationFrame(resolve));
            }
            await new Promise(resolve => setTimeout(resolve, 150));
        };

        const finish = (result) => {
            window.removeEventListener('error', onError);
            window.removeEventListener('unhandledrejection', onRejection);
            return JSON.stringify({ ...result, errors });
        };
    ";

    [Fact]
    public async Task SlideTo_ContainerRemovedFromDom_IsIgnoredWithoutError()
    {
        // Arrange
        await fixture.NavigateToStoryAsync(AnyStory);

        // Act
        var result = await EvaluateAsync(@"
            const host = createHost();
            await interop.initialize(host, {}, null, [], 0, false);
            const instance = host.swiper;

            host.remove();

            let thrown = null;
            try {
                interop.slideTo(host, 1, 0);
            } catch (error) {
                thrown = String(error?.message ?? error);
            }

            await settle();
            return finish({
                isHostStillPointingAtIt: host.swiper === instance,
                isDestroyed: instance.destroyed === true,
                hasParams: instance.params !== undefined,
                thrown
            });");

        // Assert - the shape is asserted too, so the test cannot pass because the element stopped
        // producing the gutted instance this whole fix is about.
        Assert.True(result.IsHostStillPointingAtIt, "The host no longer references the destroyed instance.");
        Assert.True(result.IsDestroyed, "Removing the container did not destroy its Swiper.");
        Assert.False(result.HasParams, "The destroyed instance still has params.");
        Assert.Null(result.Thrown);
        Assert.Empty(result.Errors);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task LiveAutoHeight_ContainerRemovedFromDom_ObserversStopWithoutError()
    {
        // Arrange
        await fixture.NavigateToStoryAsync(AnyStory);

        // Act - removing the container resizes every slide to nothing (the ResizeObserver) and a slide
        // added afterwards changes the observed set (the MutationObserver). Both belong to the wrapper,
        // and native disconnection does not take them off - only the interop's own destroy() does.
        var result = await EvaluateAsync(@"
            const host = createHost();
            await interop.initialize(host, { autoHeight: true }, null, [], 0, false);
            const instance = host.swiper;

            host.remove();
            host.appendChild(document.createElement('swiper-slide'));
            host.querySelector('swiper-slide').style.height = '400px';

            await settle();
            return finish({
                isHostStillPointingAtIt: host.swiper === instance,
                isDestroyed: instance.destroyed === true,
                hasParams: instance.params !== undefined
            });");

        // Assert
        Assert.True(result.IsDestroyed, "Removing the container did not destroy its Swiper.");
        Assert.Empty(result.Errors);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task SelectorCompanion_OwnerRemovedWhileWaiting_ResolvesWithoutError()
    {
        // Arrange
        await fixture.NavigateToStoryAsync(AnyStory);

        // Act - the owner names a thumbnail strip that exists but has not initialised yet, so the wrapper
        // waits for it. The owner is gone by the time the strip arrives.
        var result = await EvaluateAsync(@"
            const target = createHost('destroyed-owner-thumbs');
            const owner = createHost();
            await interop.initialize(owner, { thumbs: { swiper: '#destroyed-owner-thumbs' } }, null, [], 0, false);
            const instance = owner.swiper;

            owner.remove();
            await interop.initialize(target, {}, null, [], 0, false);

            await settle();
            target.remove();
            return finish({
                isHostStillPointingAtIt: owner.swiper === instance,
                isDestroyed: instance.destroyed === true,
                hasParams: instance.params !== undefined
            });");

        // Assert
        Assert.True(result.IsDestroyed, "Removing the owner did not destroy its Swiper.");
        Assert.Empty(result.Errors);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task ArmAnchor_InstanceReplacedSinceFirstArm_AnchorsTheReplacement()
    {
        // Arrange - the first arm creates the one observer the host reuses from then on.
        await fixture.NavigateToStoryAsync(AnyStory);

        // Act
        var result = await EvaluateAsync(@"
            const host = createHost();
            await interop.initialize(host, {}, null, [], 0, false);
            interop.armAnchor(host, 0);
            host.appendChild(document.createElement('swiper-slide'));
            await settle();

            replaceInstance(host);
            const replacement = host.swiper;
            interop.armAnchor(host, 2);
            host.prepend(document.createElement('swiper-slide'));
            await settle();

            const anchoredIndex = replacement.activeIndex;
            host.remove();
            return finish({
                isHostStillPointingAtIt: host.swiper === replacement,
                isDestroyed: replacement.destroyed === true,
                hasParams: false,
                anchoredIndex
            });");

        // Assert
        Assert.Equal(2, result.AnchoredIndex);
        Assert.Empty(result.Errors);
        fixture.AssertNoJsErrors();
    }

    [Fact]
    public async Task Subscription_InstanceReplaced_ReadsTheEmittingInstance()
    {
        // Arrange
        await fixture.NavigateToStoryAsync(AnyStory);

        // Act - a replacement announces its slide count from inside its own constructor, while the host
        // still names the destroyed predecessor.
        var result = await EvaluateAsync(@"
            const host = createHost();
            await interop.initialize(host, {}, dotNetRef, ['slidesLengthChange'], 0, false);

            replaceInstance(host);
            await settle();

            const slideCounts = dotNetCalls
                .filter(call => call[0] === 'OnSwiperEventInternal' && call[1] === 'slidesLengthChange')
                .map(call => JSON.parse(call[2]));
            host.remove();
            return finish({
                isHostStillPointingAtIt: true,
                isDestroyed: true,
                hasParams: false,
                reportedSlideCounts: slideCounts
            });");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Contains(3, result.ReportedSlideCounts ?? []);
        fixture.AssertNoJsErrors();
    }

    private async Task<DestroyedResult> EvaluateAsync(string scenario)
    {
        var json = await fixture.Page.EvaluateAsync<string>($"async () => {{ {Harness} {scenario} }}");
        return JsonSerializer.Deserialize<DestroyedResult>(json, _jsonOptions)!;
    }

    private sealed record DestroyedResult(
        bool IsHostStillPointingAtIt,
        bool IsDestroyed,
        bool HasParams,
        string? Thrown,
        string[] Errors,
        int? AnchoredIndex,
        int[]? ReportedSlideCounts);
}
