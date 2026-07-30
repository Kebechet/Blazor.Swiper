using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Xunit;

namespace Blazor.Swiper.E2E;

/// <summary>
/// Opens every story there is, works it, and fails on anything the console reports.
/// </summary>
/// <remarks>
/// The other e2e classes assert what a story does; this one asserts that no story does something
/// wrong on the way. It covers the ones with no test of their own - the purely visual stories, the
/// combinations in Recipes - which is where an interop slip would otherwise sit unnoticed, because a
/// slider can land on the right slide while the interop threw getting there.
/// </remarks>
[Collection(DemoCollectionDefinition.Name)]
public sealed class SwiperStorySweepTests(DemoFixture fixture)
{
    [Theory]
    [MemberData(nameof(EveryStoryId))]
    public async Task Story_OpenedAndExercised_ReportsNothingToTheConsole(string storyId)
    {
        // Arrange - navigating already waits for every slider on the page to have initialized, which
        // is itself half the assertion: a story whose options do not survive the trip never gets there.
        var canvas = await fixture.NavigateToStoryAsync(storyId);

        // Act - advance each slider, then press whatever controls the story exposes. Between them
        // these reach the interop paths a story cares about without knowing anything about it.
        await fixture.Page.EvaluateAsync(
            "() => document.querySelectorAll('swiper-container').forEach(c => c.swiper?.slideNext())");
        await fixture.Page.WaitForTimeoutAsync(400);

        var buttons = await canvas.Locator("css=button.demo-button").AllAsync();
        foreach (var button in buttons)
        {
            await button.ClickAsync(new LocatorClickOptions { Timeout = 5_000 });
            await fixture.Page.WaitForTimeoutAsync(200);
        }

        await fixture.Page.WaitForTimeoutAsync(300);

        // Assert
        fixture.AssertNoJsErrors();
        await AssertNoHorizontalOverflowAsync(storyId);
    }

    /// <summary>
    /// Fails if the story is wider than the page it is on.
    /// </summary>
    /// <remarks>
    /// Swiper sizes its slides from the container it is in, so a container that sizes itself from its
    /// content is a circular constraint - and a browser resolves that by growing to the CSS maximum
    /// rather than by failing. A CSS grid cell does exactly this, because its min-width defaults to
    /// auto. Nothing throws, so no console assertion would ever see it; the only symptom is a story
    /// thirty-three million pixels wide.
    /// </remarks>
    private async Task AssertNoHorizontalOverflowAsync(string storyId)
    {
        var overflow = await fixture.Page.EvaluateAsync<int>(
            "() => document.documentElement.scrollWidth - document.documentElement.clientWidth");

        Assert.True(
            overflow <= 4,
            $"'{storyId}' overflows its page by {overflow}px. A slider whose container sizes itself from " +
            "its content grows without bound - give the container a width that does not depend on the slides.");
    }

    /// <summary>
    /// Every story id, read out of the story sources rather than listed here.
    /// </summary>
    /// <remarks>
    /// A hand-kept list would silently stop covering a story the moment one was added, which is the
    /// one thing a sweep must not do. BlazingStory builds an id by lowercasing the title and the
    /// story name and replacing spaces with dashes - see the storybook notes in CLAUDE.md.
    /// </remarks>
    public static TheoryData<string> EveryStoryId()
    {
        var storiesDirectory = Path.Combine(DemoFixture.RepositoryRoot, "demo", "Stories");
        var ids = new TheoryData<string>();

        foreach (var file in Directory.GetFiles(storiesDirectory, "*.stories.razor").OrderBy(path => path))
        {
            var source = File.ReadAllText(file);
            var title = Regex.Match(source, @"\[Stories\(""(?<title>[^""]+)""\)\]").Groups["title"].Value;
            var prefix = title.ToLowerInvariant().Replace('/', '-').Replace(' ', '-');

            foreach (Match story in Regex.Matches(source, @"<Story Name=""(?<name>[^""]+)"""))
            {
                ids.Add($"{prefix}--{story.Groups["name"].Value.ToLowerInvariant().Replace(' ', '-')}");
            }
        }

        Assert.True(ids.Count > 40, $"Only {ids.Count} stories were discovered, which cannot be right.");

        return ids;
    }
}
