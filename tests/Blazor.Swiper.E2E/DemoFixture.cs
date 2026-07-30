using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Blazor.Swiper.E2E;

[CollectionDefinition("BlazingStory demo")]
public sealed class DemoCollectionDefinition : ICollectionFixture<DemoFixture>
{
    public const string Name = "BlazingStory demo";
}

public sealed class DemoFixture : IAsyncLifetime
{
    private readonly ConcurrentQueue<string> _serverOutput = new();
    private Process? _demoProcess;
    private IPlaywright? _playwright;

    public string BaseUrl { get; private set; } = string.Empty;
    public IBrowser Browser { get; private set; } = null!;
    public IPage Page { get; private set; } = null!;

    /// <summary>
    /// Unhandled JavaScript errors and Blazor render exceptions seen since the last navigation.
    /// </summary>
    /// <remarks>
    /// The slider can end up on the right slide while the interop threw getting there - a missing
    /// export, a null <c>swiper</c> on a torn-down element, an observer firing after destroy. The
    /// index would still read correctly and the test would pass. Asserting the console is what
    /// makes those visible, and none of them are reachable from bUnit, which stubs the whole
    /// interop module out.
    /// </remarks>
    private readonly List<string> _jsErrors = new();

    public async ValueTask InitializeAsync()
    {
        var repositoryRoot = RepositoryRoot;
        var port = ReserveTcpPort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(Path.Combine(repositoryRoot, "demo", "Blazor.Swiper.Demo.csproj"));
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(BaseUrl);
        startInfo.Environment["DOTNET_NOLOGO"] = "1";

        _demoProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _demoProcess.OutputDataReceived += CaptureServerOutput;
        _demoProcess.ErrorDataReceived += CaptureServerOutput;
        if (!_demoProcess.Start())
        {
            throw new InvalidOperationException("Could not start the BlazingStory demo process.");
        }

        _demoProcess.BeginOutputReadLine();
        _demoProcess.BeginErrorReadLine();
        await WaitForDemoAsync(TimeSpan.FromMinutes(5));

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            // The system Chrome rather than Playwright's bundled Chromium, so neither CI nor a
            // fresh clone needs a browser download step.
            Channel = "chrome",
            Headless = true,
            Args = ["--disable-dev-shm-usage"]
        });
        await CreatePageAsync();
    }

    /// <summary>
    /// Replaces <see cref="Page"/> with a fresh one.
    /// </summary>
    /// <remarks>
    /// Tests share this fixture, and a page carries state a swipe can trip over: the pointer's
    /// position, a half-finished touch interaction, a slider still settling from the last test.
    /// One page per scenario removes that as a source of intermittent failures.
    /// </remarks>
    private async Task CreatePageAsync()
    {
        if (Page is not null)
        {
            await Page.CloseAsync();
        }

        // Deliberately no HasTouch. Swiper binds touch listeners when the browser reports touch
        // support and pointer/mouse listeners otherwise, and Playwright's touchscreen can only tap,
        // never drag - so a touch-capable page would make every swipe below silently inert.
        Page = await Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 1000 }
        });

        Page.Console += (_, message) =>
        {
            if (message.Type == "error" && !IsUnrelatedNoise(message.Text))
            {
                lock (_jsErrors) _jsErrors.Add(FirstLine(message.Text));
            }
        };
        Page.PageError += (_, error) =>
        {
            lock (_jsErrors) _jsErrors.Add(FirstLine(error));
        };
    }

    /// <summary>Fails the current test if the page logged an unhandled JavaScript or Blazor render error.</summary>
    public void AssertNoJsErrors()
    {
        string[] errors;
        lock (_jsErrors) errors = _jsErrors.Distinct().ToArray();
        Assert.True(
            errors.Length == 0,
            "The page reported unhandled errors:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }

    private static string FirstLine(string text)
    {
        var lines = text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        return lines.Length == 0 ? text : lines[0];
    }

    /// <summary>Browser-extension chatter and asset noise that says nothing about this library.</summary>
    private static bool IsUnrelatedNoise(string text)
    {
        return text.Contains("contentscript")
            || text.Contains("Failed to load resource")
            || text.Contains("preloaded using link preload");
    }

    public async ValueTask DisposeAsync()
    {
        if (Page is not null)
        {
            await Page.CloseAsync();
        }

        if (Browser is not null)
        {
            await Browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_demoProcess is { HasExited: false })
        {
            _demoProcess.Kill(entireProcessTree: true);
            await _demoProcess.WaitForExitAsync();
        }

        _demoProcess?.Dispose();
    }

    /// <summary>
    /// Navigates straight to the story canvas page rather than the storybook shell.
    /// </summary>
    /// <remarks>
    /// The canvas page (<c>iframe.html</c>) is what BlazingStory's shell hosts in an iframe anyway,
    /// so addressing it directly is both faster and steadier - no sidebar, no panels, no cross-frame
    /// hop to reach the slider.
    /// </remarks>
    public async Task<ILocator> NavigateToStoryAsync(string storyId, string stateTestId)
    {
        var canvas = await NavigateToStoryAsync(storyId);

        await canvas.GetByTestId(stateTestId).WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });

        return canvas;
    }

    /// <summary>
    /// Navigates to a story that has no state panel, e.g. one that is only about how it looks.
    /// </summary>
    public async Task<ILocator> NavigateToStoryAsync(string storyId)
    {
        lock (_jsErrors) _jsErrors.Clear();
        var url = $"{BaseUrl}/iframe.html?viewMode=story&id={storyId}&e2e={Guid.NewGuid():N}";
        await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Blazor paints the story before OnAfterRenderAsync has imported the interop module and
        // called element.initialize(). A swipe started in that window is silently inert, and the
        // slider is still hidden, so wait for Swiper itself rather than for Blazor's first paint.
        await Page.WaitForFunctionAsync(
            @"() => {
                const containers = Array.from(document.querySelectorAll('swiper-container'));
                return containers.length > 0 && containers.every(c => c.swiper && c.swiper.initialized);
            }",
            null,
            new PageWaitForFunctionOptions { Timeout = 60_000 });

        return Page.Locator("body");
    }

    /// <summary>Reads a Swiper parameter off a slider, as Swiper itself resolved it.</summary>
    /// <remarks>
    /// The one assertion that proves an option actually arrived: the wrapper serializes it, the
    /// element parses it, and Swiper merges it with its own defaults - and nothing short of asking
    /// Swiper afterwards shows that the value survived all three.
    /// </remarks>
    public Task<T> ReadParameterAsync<T>(string testId, string parameterPath)
    {
        return Page.EvaluateAsync<T>(
            $"() => document.querySelector('[data-testid=\"{testId}\"]').swiper.params.{parameterPath}");
    }

    /// <summary>Reads anything else off the live Swiper instance.</summary>
    public Task<T> ReadSwiperAsync<T>(string testId, string expression)
    {
        return Page.EvaluateAsync<T>(
            $"() => {{ const s = document.querySelector('[data-testid=\"{testId}\"]').swiper; return {expression}; }}");
    }

    private async Task WaitForDemoAsync(TimeSpan timeout)
    {
        using var client = new HttpClient();
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (_demoProcess?.HasExited == true)
            {
                throw new InvalidOperationException($"The demo exited before becoming ready.{Environment.NewLine}{ServerLog()}");
            }

            try
            {
                using var response = await client.GetAsync(BaseUrl);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"The demo was not ready after {timeout}.{Environment.NewLine}{ServerLog()}");
    }

    private void CaptureServerOutput(object sender, DataReceivedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.Data))
        {
            _serverOutput.Enqueue(args.Data);
        }
    }

    private string ServerLog() => string.Join(Environment.NewLine, _serverOutput.TakeLast(100));

    private static int ReserveTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>The repository root, found by walking up from the test assembly.</summary>
    public static string RepositoryRoot => FindRepositoryRoot();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "demo", "Blazor.Swiper.Demo.csproj")) &&
                File.Exists(Path.Combine(directory.FullName, "src", "Blazor.Swiper.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Blazor.Swiper repository root.");
    }
}
