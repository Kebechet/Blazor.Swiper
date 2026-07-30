using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace Kebechet.Blazor.Swiper.Tests;

/// <summary>
/// Guards the packaging contract: the vendored Swiper bundle, the Blazor JS initializer and the
/// interop modules must ship as static web assets, at the pinned version, under the packaged path
/// the component actually asks for. Get any of it wrong and the package builds, publishes and
/// installs cleanly while loading no JavaScript at all at runtime.
/// </summary>
public class PackagingTests
{
    /// <summary>
    /// Resolved from this file's compile-time path rather than the test assembly's location: an
    /// RCL's <c>wwwroot</c> is not copied into a referencing project's output, and these
    /// assertions are about repository content, not build output.
    /// </summary>
    private static string _wwwrootDirectory => Path.Combine(_repositoryRoot, "src", "Blazor.Swiper", "wwwroot");

    private static string _projectPath => Path.Combine(_repositoryRoot, "src", "Blazor.Swiper", "Blazor.Swiper.csproj");

    /// <summary>The vendored Swiper bundle, which is also what the surface tests read Swiper's own parameter list from.</summary>
    internal static string BundlePath => Path.Combine(_wwwrootDirectory, "swiper-element-bundle.min.js");

    private static string _repositoryRoot
    {
        get
        {
            var testsDirectory = Path.GetDirectoryName(GetThisFilePath())!;
            return Path.GetFullPath(Path.Combine(testsDirectory, "..", ".."));
        }
    }

    [Theory]
    [InlineData("swiper-element-bundle.min.js")]
    [InlineData("Kebechet.Blazor.Swiper.lib.module.js")]
    [InlineData("swiper-interop.js")]
    [InlineData("swiper-policy.js")]
    public void StaticWebAsset_ShippedModule_IsPresentInWwwroot(string fileName)
    {
        // Arrange
        var assetPath = Path.Combine(_wwwrootDirectory, fileName);

        // Act
        var doesAssetExist = File.Exists(assetPath);

        // Assert
        doesAssetExist.ShouldBeTrue($"'{fileName}' is missing from src/Blazor.Swiper/wwwroot.");
    }

    /// <summary>
    /// Every relative import in a shipped module must resolve to another shipped file.
    /// </summary>
    /// <remarks>
    /// A module split across files only works if all of them reach the consumer. A missing one
    /// fails at load time in the browser, long after any build or unit test would have noticed.
    /// </remarks>
    [Fact]
    public void StaticWebAsset_RelativeImport_ResolvesToAShippedFile()
    {
        // Arrange
        var modulePaths = Directory.GetFiles(_wwwrootDirectory, "*.js");
        var importPattern = new Regex("""from\s+["'](?<path>\.[^"']+)["']""");

        // Act
        var missingImports = modulePaths
            .SelectMany(modulePath => importPattern
                .Matches(File.ReadAllText(modulePath))
                .Select(match => new
                {
                    Module = Path.GetFileName(modulePath),
                    Target = Path.GetFullPath(Path.Combine(_wwwrootDirectory, match.Groups["path"].Value))
                }))
            .Where(import => !File.Exists(import.Target))
            .Select(import => $"{import.Module} imports missing '{Path.GetFileName(import.Target)}'")
            .ToArray();

        // Assert
        missingImports.ShouldBeEmpty();
    }

    /// <summary>
    /// Blazor auto-discovers the initializer purely by filename, as <c>{PackageId}.lib.module.js</c>.
    /// Rename the package without renaming the file and nothing runs it, so the Swiper bundle is
    /// never injected and every slider on the page silently stays an inert custom element.
    /// </summary>
    [Fact]
    public void Initializer_FileName_MatchesThePackageIdSoBlazorDiscoversIt()
    {
        // Arrange
        var expectedFileName = $"{ReadProjectProperty("PackageId")}.lib.module.js";

        // Act
        var initializerPath = Path.Combine(_wwwrootDirectory, expectedFileName);

        // Assert
        File.Exists(initializerPath).ShouldBeTrue($"Blazor looks for '{expectedFileName}' and it is not in wwwroot.");
    }

    [Fact]
    public void Initializer_BundleInjection_LoadsLocallyAndNeverFromACdn()
    {
        // Arrange
        var packageId = ReadProjectProperty("PackageId");
        var initializerPath = Path.Combine(_wwwrootDirectory, $"{packageId}.lib.module.js");

        // Act
        var initializer = File.ReadAllText(initializerPath);

        // Assert
        initializer.ShouldContain($"_content/{packageId}/swiper-element-bundle.min.js");
        initializer.ShouldNotContain("cdn.");
        initializer.ShouldNotContain("http://");
        initializer.ShouldNotContain("https://");
    }

    /// <summary>
    /// The package version's first three parts track the bundled Swiper release, and the fourth is
    /// this wrapper's own revision. Nothing but this test holds those together, so bumping the
    /// wrapper revision without re-vendoring the bundle - or re-vendoring without bumping - would
    /// ship a package whose version advertises a Swiper release it does not contain.
    /// </summary>
    [Fact]
    public void VendoredBundle_ShippedFile_IsTheSwiperReleaseThePackageVersionAdvertises()
    {
        // Arrange
        var packageVersion = ReadProjectProperty("Version");
        var advertisedSwiperVersion = string.Join('.', packageVersion.Split('.').Take(3));

        // Act
        var banner = string.Join(Environment.NewLine, File.ReadLines(BundlePath).Take(5));
        var doesBundleMatch = banner.Contains($"Swiper Custom Element {advertisedSwiperVersion}", StringComparison.Ordinal);

        // Assert
        doesBundleMatch.ShouldBeTrue(
            $"The package version is {packageVersion}, so the vendored bundle must be Swiper {advertisedSwiperVersion}.");
    }

    /// <summary>
    /// The component imports its interop module by packaged path, which is a runtime string the
    /// compiler never checks against the files that actually ship.
    /// </summary>
    [Fact]
    public void InteropModulePath_ReferencedByTheComponent_ResolvesToAShippedAsset()
    {
        // Arrange
        var componentPath = Path.Combine(_repositoryRoot, "src", "Blazor.Swiper", "Swiper.razor.cs");
        var componentSource = File.ReadAllText(componentPath);
        var packageId = ReadProjectProperty("PackageId");

        // Act
        var referencedPath = Regex.Match(componentSource, @"""\./_content/(?<package>[^/]+)/(?<file>[^""]+)""");

        // Assert
        referencedPath.Success.ShouldBeTrue("Swiper.razor.cs no longer references a _content module path.");
        referencedPath.Groups["package"].Value.ShouldBe(packageId);
        File.Exists(Path.Combine(_wwwrootDirectory, referencedPath.Groups["file"].Value)).ShouldBeTrue();
    }

    private static string ReadProjectProperty(string propertyName)
    {
        var project = File.ReadAllText(_projectPath);
        var match = Regex.Match(project, $@"<{propertyName}>(?<value>[^<]+)</{propertyName}>");
        match.Success.ShouldBeTrue($"<{propertyName}> is missing from Blazor.Swiper.csproj.");

        return match.Groups["value"].Value;
    }

    private static string GetThisFilePath([CallerFilePath] string path = "")
    {
        return path;
    }
}
