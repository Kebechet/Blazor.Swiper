using Microsoft.AspNetCore.Components;

namespace Kebechet.Blazor.Swiper;

/// <summary>
/// One slide. Place these in a <see cref="Swiper"/>'s content.
/// </summary>
/// <remarks>
/// Its child content is handed the slide's own state, so <c>&lt;SwiperSlide Context="slide"&gt;</c>
/// can render differently while it is the active one - pausing a video on the slides that are not.
/// Plain content that ignores the state works exactly as before.
/// </remarks>
public partial class SwiperSlide : IDisposable
{
    /// <summary>The slide's content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The slide's content, handed the slide's own state - so it can render differently while it is
    /// the active one. Takes precedence over <see cref="ChildContent"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;SwiperSlide Index="i"&gt;
    ///     &lt;SlideContent Context="slide"&gt;
    ///         &lt;video autoplay="@slide.IsActive" ... /&gt;
    ///     &lt;/SlideContent&gt;
    /// &lt;/SwiperSlide&gt;
    /// </code>
    /// </example>
    [Parameter] public RenderFragment<SwiperSlideContext>? SlideContent { get; set; }

    /// <summary>
    /// The slide's position in your collection.
    /// </summary>
    /// <remarks>
    /// Only needed for <see cref="SwiperSlideContext"/> to be right. Left null, the slide takes its
    /// position from the order the slides registered themselves in, which is correct for a fixed list
    /// and for one that only grows at the end, but not for one that inserts in the middle. Supply it
    /// from your own loop whenever the collection is reordered or inserted into.
    /// </remarks>
    [Parameter] public int? Index { get; set; }

    /// <summary>
    /// Wraps the content in Swiper's zoom container, so <see cref="SwiperOptions.Zoom"/> can zoom it.
    /// </summary>
    [Parameter] public bool Zoom { get; set; }

    /// <summary>A zoom factor for this slide alone, overriding <see cref="SwiperZoomOptions.MaxRatio"/>.</summary>
    [Parameter] public double? ZoomMaxRatio { get; set; }

    /// <summary>Defers loading this slide's images until it is near the viewport.</summary>
    /// <remarks>
    /// Mark the images themselves with <c>loading="lazy"</c>. <see cref="SwiperOptions.LazyPreloadPrevNext"/>
    /// controls how many neighbouring slides are loaded ahead of time.
    /// </remarks>
    [Parameter] public bool Lazy { get; set; }

    /// <summary>
    /// This slide's key for <see cref="SwiperOptions.HashNavigation"/> and
    /// <see cref="SwiperOptions.History"/>, i.e. what appears in the URL when it is active.
    /// </summary>
    [Parameter] public string? Hash { get; set; }

    /// <summary>How long autoplay holds this slide, in ms, overriding <see cref="SwiperAutoplayOptions.Delay"/>.</summary>
    [Parameter] public int? AutoplayDelay { get; set; }

    /// <summary>
    /// This slide's index for <see cref="SwiperOptions.Virtual"/>, which needs to address slides it
    /// did not render itself.
    /// </summary>
    [Parameter] public int? VirtualIndex { get; set; }

    /// <summary>Attributes forwarded to the underlying <c>&lt;swiper-slide&gt;</c> element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter] private Swiper? Parent { get; set; }

    private SwiperSlideContext CurrentState => new()
    {
        Index = ResolvedIndex,
        IsActive = Parent is not null && Parent.ActiveIndex == ResolvedIndex,
        IsNext = Parent is not null && Parent.ActiveIndex + 1 == ResolvedIndex,
        IsPrevious = Parent is not null && Parent.ActiveIndex - 1 == ResolvedIndex
    };

    private int ResolvedIndex => Index ?? Parent?.IndexOfSlide(this) ?? -1;

    private IReadOnlyDictionary<string, object> RenderedAttributes => BuildAttributes();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Parent?.RegisterSlide(this);
    }

    /// <summary>
    /// Re-renders the slide when the active one changes, so its state stays true.
    /// </summary>
    internal void NotifyStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// The caller's attributes plus the ones the slide-level parameters stand for.
    /// </summary>
    /// <remarks>
    /// Swiper reads all of these off the slide element itself rather than from the slider's options,
    /// which is why they are parameters here rather than members of <see cref="SwiperOptions"/>. The
    /// caller's own attributes are copied rather than mutated - the dictionary belongs to them and is
    /// reused across renders.
    /// </remarks>
    private IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = AdditionalAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(AdditionalAttributes);

        if (Lazy)
        {
            attributes["lazy"] = "true";
        }

        if (!string.IsNullOrEmpty(Hash))
        {
            attributes["data-hash"] = Hash!;
        }

        if (AutoplayDelay is not null)
        {
            attributes["data-swiper-autoplay"] = AutoplayDelay.Value;
        }

        if (VirtualIndex is not null)
        {
            attributes["data-swiper-slide-index"] = VirtualIndex.Value;
        }

        if (ZoomMaxRatio is not null)
        {
            attributes["data-swiper-zoom"] = ZoomMaxRatio.Value;
        }

        return attributes;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Parent?.UnregisterSlide(this);
    }
}

/// <summary>
/// A slide's own state, handed to its content.
/// </summary>
/// <remarks>
/// Derived from the slider's active index rather than read back from Swiper, so it costs no interop
/// and is available during render. It follows <see cref="SwiperSlide.Index"/> - see its remarks for
/// when that needs supplying.
/// </remarks>
public sealed record SwiperSlideContext
{
    /// <summary>The slide's position, or -1 when it could not be resolved.</summary>
    public int Index { get; init; } = -1;

    /// <summary>Whether this is the active slide.</summary>
    public bool IsActive { get; init; }

    /// <summary>Whether this slide is immediately after the active one.</summary>
    public bool IsNext { get; init; }

    /// <summary>Whether this slide is immediately before the active one.</summary>
    public bool IsPrevious { get; init; }
}
