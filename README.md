[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/kebechet)

# Blazor.Swiper
[![NuGet Version](https://img.shields.io/nuget/v/Kebechet.Blazor.Swiper)](https://www.nuget.org/packages/Kebechet.Blazor.Swiper/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Kebechet.Blazor.Swiper)](https://www.nuget.org/packages/Kebechet.Blazor.Swiper/)
[![Build](https://github.com/Kebechet/Blazor.Swiper/actions/workflows/build.yml/badge.svg)](https://github.com/Kebechet/Blazor.Swiper/actions/workflows/build.yml)
[![codecov](https://codecov.io/gh/Kebechet/Blazor.Swiper/graph/badge.svg)](https://codecov.io/gh/Kebechet/Blazor.Swiper)
[![Storybook](https://img.shields.io/badge/storybook-live%20demo-ff4785)](https://kebechet.github.io/Blazor.Swiper/)
![Last updated](https://img.shields.io/github/last-commit/Kebechet/Blazor.Swiper/main?label=last%20updated)
[![Twitter](https://img.shields.io/twitter/url/https/twitter.com/samuel_sidor.svg?style=social&label=Follow%20samuel_sidor)](https://x.com/samuel_sidor)

A Blazor wrapper for [Swiper](https://swiperjs.com), built on the framework-agnostic [Swiper Element](https://swiperjs.com/element) web component. The Swiper bundle is shipped inside the package and auto-registered on startup - no npm build step and no manual `<script>` tags.

**[Live storybook](https://kebechet.github.io/Blazor.Swiper/)** - interactive stories for every option.

## Installation

```bash
dotnet add package Kebechet.Blazor.Swiper
```

## Usage

```razor
@using Kebechet.Blazor.Swiper

<Swiper Options="new() { Pagination = true, AllowSlidePrev = true }">
    <SwiperSlide>Slide 1</SwiperSlide>
    <SwiperSlide>Slide 2</SwiperSlide>
    <SwiperSlide>Slide 3</SwiperSlide>
</Swiper>
```

Styling the shadow-DOM controls (pagination, arrows) is done via Swiper's CSS parts / variables - see the demo.

Any attribute you put on `<Swiper>` or `<SwiperSlide>` that isn't a parameter is forwarded to the underlying `<swiper-container>` / `<swiper-slide>` element, so `class`, `style` and `id` work as usual.

## Options

`SwiperOptions` is a strongly-typed subset of [Swiper's parameters](https://swiperjs.com/swiper-api#parameters). Nullable members that are left unset keep Swiper's own default.

| Option | Type | Default | Description |
| --- | --- | --- | --- |
| `Direction` | `string` | `horizontal` | Slider axis - use the `SwiperDirection` constants. |
| `SlidesPerView` | `double?` | `1` | Number of slides visible at once. |
| `SpaceBetween` | `int?` | `0` | Gap between slides, in px. |
| `Loop` | `bool` | `false` | Continuous loop mode. |
| `CenteredSlides` | `bool` | `false` | Center the active slide. |
| `AutoHeight` | `bool` | `false` | Track height follows the active slide's content height. |
| `InitialSlide` | `int?` | `0` | Slide shown first. |
| `Speed` | `int?` | `300` | Transition duration, in ms. |
| `AllowSlideNext` | `bool` | `true` | Allow moving to the next slide. |
| `AllowSlidePrev` | `bool` | `true` | Allow moving to the previous slide. |
| `AllowTouchMove` | `bool` | `true` | Allow touch/drag swiping at all. |
| `Pagination` | `bool` | `false` | Show the built-in pagination bullets. |
| `Navigation` | `bool` | `false` | Show the built-in prev/next arrows. |
| `Scrollbar` | `bool` | `false` | Show the built-in scrollbar. |
| `Keyboard` | `bool` | `false` | Enable keyboard control. |
| `Mousewheel` | `bool` | `false` | Enable mousewheel control. |
| `Observer` | `bool` | `false` | Swiper's MutationObserver, which auto-calls `update()` on any DOM change in the container. See below. |
| `CssMode` | `bool` | `false` | Use the browser's native CSS Scroll Snap API instead of JS transforms. See below. |
| `ResizeObserver` | `bool` | `true` | Whether Swiper watches its own element for size changes and re-measures. See below. |
| `A11y` | `SwiperA11yOptions?` | `null` | Accessibility parameters. `null` keeps Swiper's defaults. See below. |

### Reaching parameters `SwiperOptions` doesn't cover

The typed options are a curated subset. Any other [Swiper parameter](https://swiperjs.com/swiper-api#parameters) can be set as a plain attribute on `<Swiper>`: attributes that don't match a component parameter are forwarded to the underlying `<swiper-container>`, which reads them as parameters when it initializes. Use Swiper's kebab-case names, and mix them with typed options freely.

```razor
<Swiper Options="new() { SlidesPerView = 2 }" slides-per-group="2" grab-cursor="true">
    <SwiperSlide>Slide 1</SwiperSlide>
    <SwiperSlide>Slide 2</SwiperSlide>
</Swiper>
```

### Observer is off by default

Swiper Element turns its `MutationObserver` on by default; this wrapper turns it off. With a framework re-rendering slide content - and especially with `AutoHeight`, whose height writes are themselves DOM mutations - it drives a costly update/height feedback loop. Call `Update()` explicitly instead when the slide collection changes, or set `Observer = true` if you want Swiper's behaviour back.

### CssMode trades features for smoothness

`CssMode = true` moves the slider onto the browser's native CSS Scroll Snap API, so scrolling runs on the compositor thread - dramatically smoother for heavy or tall slides. In exchange (these are Swiper's own limitations, not the wrapper's):

- mouse-drag does not work - wheel, trackpad and touch still do, exactly like a native scroller
- `Speed` is ignored
- transition start/end events do not fire, so use `OnSlideChange` rather than `OnTransitionEnd`
- it is **not** compatible with `Loop`

### `ResizeObserver` and `A11y.ScrollOnFocus` can undo a move you just made

Both of these Swiper behaviours end by re-anchoring onto the index Swiper is currently holding, and both are deferred by a frame. When your code moves the slider during the same interaction that triggered one of them, the correction is scheduled with the *pre-move* index, lands after your move, and silently undoes it.

- `ResizeObserver = false` drops the per-element size observer. Use it when the slides resize as a matter of course (live content) and your code drives the position. Window resizes still re-measure; only the per-element observer is dropped.
- `A11y = new() { ScrollOnFocus = false }` stops Swiper sliding a slide into view when something inside it takes focus - which otherwise pulls the slider back to the slide holding the button that just advanced it.

The wrapper already re-asserts a programmatic move if a *resize* tries to undo it while the move is still in flight, so reach for these when you'd rather Swiper didn't schedule the correction in the first place.

## Events

| Parameter | Type | Raised when |
| --- | --- | --- |
| `OnSlideChange` | `EventCallback<int>` | The slide changes, whoever caused it; the argument is the new active index. |
| `OnUserSlideChange` | `EventCallback<int>` | The slide changes **because the user dragged**. See below. |
| `OnReachBeginning` | `EventCallback` | The first slide is reached. |
| `OnReachEnd` | `EventCallback` | The last slide is reached. |
| `OnReady` | `EventCallback` | Swiper has initialized and positioned its initial slide. |
| `OnTransitionEnd` | `EventCallback` | A transition finishes, i.e. the slider has settled at rest. |
| `OnSliderFirstMove` | `EventCallback` | A drag moves the slider for the first time. Not raised in `CssMode`, where the browser owns the scroll. |

```razor
<Swiper Options="new() { Pagination = true }"
        OnSlideChange="index => _current = index"
        OnReachEnd="() => _atEnd = true">
    <SwiperSlide>Slide 1</SwiperSlide>
    <SwiperSlide>Slide 2</SwiperSlide>
</Swiper>
```

### Prefer `OnUserSlideChange` when you react by changing state

`OnSlideChange` reports every change - including the one your own `SlideTo` call just caused, and including the echo `Loop` mode emits for the slide it left once a programmatic move finishes. A host that reacts to a change by updating its own state will feed its own move straight back into itself, and by index alone the two are indistinguishable.

`OnUserSlideChange` reports only the changes the user dragged, so it's the one to bind when the reaction has side effects:

```razor
<Swiper OnUserSlideChange="LoadPageFor">
    ...
</Swiper>
```

A plain tap raises neither callback, so a button inside a slide is never mistaken for a swipe. The slider keeps moving after the pointer is released, so treat the interaction as finished on `OnTransitionEnd` rather than on release.

## Programmatic control

Capture the component with `@ref` and drive it from C#. `ActiveIndex` is kept in sync with the underlying Swiper.

| Member | Description |
| --- | --- |
| `ActiveIndex` | The active slide index (read-only). |
| `SlideTo(int index, int? speed = null)` | Transition to the slide at `index`. |
| `SlideNext(int? speed = null)` | Transition to the next slide. |
| `SlidePrev(int? speed = null)` | Transition to the previous slide. |
| `Update()` | Recalculate Swiper after slides were added or removed. |
| `UpdateAndAnchor(int index)` | Recalculate **and** settle instantly on `index`, as one operation. See below. |
| `ArmAnchor(int index)` | Re-anchor onto `index` the moment the framework next adds or removes slide elements. Call it *before* mutating. See below. |
| `SetAllowSlideNext(bool value)` | Enable/disable moving forward at runtime. |
| `SetAllowSlidePrev(bool value)` | Enable/disable moving backward at runtime. |

```razor
<Swiper @ref="_swiper" Options="new() { AllowTouchMove = false }">
    <SwiperSlide>Slide 1</SwiperSlide>
    <SwiperSlide>Slide 2</SwiperSlide>
</Swiper>

<button @onclick="() => _swiper!.SlideNext()">Next</button>

@code {
    private Swiper? _swiper;
}
```

Every method is a no-op until the component has rendered and its JS module has loaded, so calling one before `OnReady` does nothing.

### Keeping the position when slides are added or removed

When Blazor itself adds or removes `<SwiperSlide>` elements, Swiper's transform still points at the old offset. Removing a slide that sits *before* the active one shifts every later slide sideways, so the wrong slide is on screen until Swiper is told.

`Update()` on its own isn't enough here: it finishes by sliding to the index it already held, and that index addresses a different slide once the collection changed.

- **`UpdateAndAnchor(index)`** recalculates and settles on `index` in one operation. Use it when your code changes the collection and knows where it wants to end up.
- **`ArmAnchor(index)`** is for when Blazor owns the mutation. Call it *before* the change: it arms a one-shot `MutationObserver` whose callback runs after Blazor's DOM patch but before the next paint, so the correction lands in the same frame. Correcting from a call made *after* the change is a race the browser can win by painting first.

```csharp
// About to remove a slide that sits before the active one.
await _swiper!.ArmAnchor(_activeIndex - 1);
_slides.RemoveAt(0);
```

Both ignore `AllowSlideNext` / `AllowSlidePrev` - a correction isn't navigation, so your swipe locks can't veto it.

### Vertical direction needs a height

A vertical Swiper (`Direction = SwiperDirection.Vertical`) sizes its slides from the `<Swiper>` element's own height, which defaults to its content height. Give the `<Swiper>` an explicit height, or it collapses and slides stack:

```razor
<Swiper Options="new() { Direction = SwiperDirection.Vertical }" style="height: 300px;">
    ...
</Swiper>
```

## License

[MIT](https://github.com/Kebechet/Blazor.Swiper/blob/main/LICENSE)
