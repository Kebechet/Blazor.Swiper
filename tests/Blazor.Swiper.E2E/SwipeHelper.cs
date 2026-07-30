using Microsoft.Playwright;

namespace Blazor.Swiper.E2E;

/// <summary>
/// Produces a real drag, which is the only way to reach the code paths a swipe takes.
/// </summary>
internal static class SwipeHelper
{
    /// <summary>
    /// Drags from the right of the slider to the left, far enough past Swiper's long-swipe ratio
    /// to commit to the next slide.
    /// </summary>
    /// <remarks>
    /// Stepped with a frame's delay between moves rather than jumped in one go: Swiper decides
    /// whether a gesture is a swipe from the movement between successive pointer events, and a
    /// single large jump gives it nothing to measure. The first of those moves is also what raises
    /// sliderFirstMove, which is the entire basis of the user-driven distinction.
    /// </remarks>
    public static Task SwipeLeftAsync(IPage page, ILocator slider) => SwipeAsync(page, slider, 0.85f, 0.15f);

    /// <summary>The same drag the other way.</summary>
    public static Task SwipeRightAsync(IPage page, ILocator slider) => SwipeAsync(page, slider, 0.15f, 0.85f);

    private static async Task SwipeAsync(IPage page, ILocator slider, float fromRatio, float toRatio)
    {
        var box = await slider.BoundingBoxAsync() ?? throw new InvalidOperationException("The slider has no layout box.");
        var y = box.Y + (box.Height / 2);
        var startX = box.X + (box.Width * fromRatio);
        var endX = box.X + (box.Width * toRatio);

        await page.Mouse.MoveAsync(startX, y);
        await page.Mouse.DownAsync();

        const int steps = 15;
        for (var step = 1; step <= steps; step++)
        {
            await page.Mouse.MoveAsync(startX + ((endX - startX) * step / steps), y);
            await Task.Delay(16);
        }

        await page.Mouse.UpAsync();
    }
}
