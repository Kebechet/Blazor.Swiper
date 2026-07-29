# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

`Kebechet.Blazor.Swiper` is a typed Blazor wrapper over [Swiper](https://swiperjs.com), built on the
framework-agnostic Swiper Element web component. The pinned upstream bundle ships inside the package
as a static web asset - there is no npm step, no CDN and no `<script>` tag for consumers to add.

## Layout

| Path | What it is |
|---|---|
| `src/Blazor.Swiper/` | The package. RCL, `Microsoft.NET.Sdk.Razor`, namespace `Kebechet.Blazor.Swiper` |
| `src/Blazor.Swiper/wwwroot/swiper-element-bundle.min.js` | Vendored upstream bundle, pinned. Never swap for a CDN reference |
| `src/Blazor.Swiper/wwwroot/swiper-interop.js` | The interop module - the JS side of every call and event. Wiring only |
| `src/Blazor.Swiper/wwwroot/swiper-policy.js` | The decisions behind that wiring. Touches neither Swiper nor the DOM, so node can test it |
| `demo/` | BlazingStory storybook. Also the app the e2e suite drives |
| `tests/interop/` | `node --test` suite over the two JS modules |
| `tests/Blazor.Swiper.Tests/` | bUnit + xUnit v3 unit tests, plus the packaging contract |
| `tests/Blazor.Swiper.E2E/` | Playwright tests driving real Chrome |

Style follows the global Kebechet conventions (no `#region`, LINQ one method per line,
`.IsNullOrEmpty()`, `is null`, no comments on self-explaining code).

## Build & test

```bash
dotnet build src/Blazor.Swiper.slnx -c Release
node --test tests/interop/*.test.mjs
dotnet test tests/Blazor.Swiper.Tests/Blazor.Swiper.Tests.csproj -c Release
dotnet test tests/Blazor.Swiper.E2E/Blazor.Swiper.E2E.csproj -c Release
```

`node --test` needs explicit file arguments - pass the glob. Handing it the directory
(`node --test tests/interop/`) fails with `MODULE_NOT_FOUND` rather than discovering anything.

The e2e project is deliberately **not** in `Blazor.Swiper.slnx`, so `dotnet test` on the solution
stays fast and never drags a browser in. It runs on `pull_request` and `workflow_dispatch` only,
never on push. The fixture starts the demo itself on a dynamic port and launches the installed
system Chrome through Playwright's `chrome` channel, so there is no browser download step.

`.gitignore` carries an unignore for the e2e project. The stock Visual Studio template ignores
`*.e2e` (Visual Studio trace files) and git matches case-insensitively on Windows, so the pattern
silently swallows the whole `tests/Blazor.Swiper.E2E/` directory - `git check-ignore` blames
`.gitignore:130`. Do not remove `!/tests/Blazor.Swiper.E2E/`.

`GeneratePackageOnBuild` is on, so `dotnet build -c Release` already emits the `.nupkg` (and
`.snupkg`) under `src/Blazor.Swiper/bin/Release/`. Verify the static web assets actually made it in:

```bash
unzip -l src/Blazor.Swiper/bin/Release/*.nupkg | grep -E "staticwebassets|build/"
```

## What the tests cannot see

This matters more than it sounds, and it is why there are three suites rather than one.

- **bUnit never executes `swiper-interop.js`.** `SwiperTests` stubs the module out with
  `JSInterop.SetupModule` + `JSRuntimeMode.Loose`, so every assertion is about *which* interop
  function was called, never about what it does. A dangling reference or an inverted condition in
  that file passes the whole suite.
- **node can load `swiper-interop.js` but not run it.** The module touches no browser global at
  module scope, which is what lets `swiper-interop.module.test.mjs` prove it parses, that its import
  resolves and that every entry point the component calls by string still exists. The functions
  themselves need a DOM.
- **Only a browser sees the rest**: the `<swiper-container>` custom element upgrading, the reveal
  handshake, cssMode scrolling under scroll-snap, the autoHeight `ResizeObserver`, `ArmAnchor`'s
  same-frame guarantee, and any real swipe.
- **The console is part of the assertion.** The slider can land on the right slide while the interop
  threw on the way there, and the index alone still reads correctly. Every e2e scenario calls
  `AssertNoJsErrors`.

When a bug is reported, reproduce it as a failing test first, and pick the suite by what the bug
touches: a decision → `tests/interop`; a parameter or callback wiring question → bUnit; anything
involving real layout, pointers or timing → e2e. A fix verified by looking at the storybook is not
verified.

## Library gotchas, learned the hard way

**Attach listeners after `initialize()`, never before.** Swiper announces its starting position from
inside `init()` (`runCallbacksOnInit` defaults on), and that is not news to the host - it is the
position the host just asked for. Forwarding it makes every consumer write an "ignore the first one"
guard, and in a two-way binding it reports slide 0 back before the host has settled.

**`realIndex`, never `activeIndex`.** In loop mode Swiper duplicates slides, so `activeIndex` counts
positions the host's collection does not have. Everything crossing the interop boundary is the
logical index - which is also why programmatic moves route through `slideToLoop` rather than
`slideTo` when looping, so the two agree in both directions.

**`transitionend` is not proof the host's move finished.** Swiper's own re-anchors slide at speed 0
and raise one too, so clearing the intent guard on any transitionend drops it in the very frame it
exists to correct and lets the re-anchor stand. `shouldDisarmIntent` owns that rule.

**`sliderFirstMove`, not `touchstart`.** touchstart also fires for a plain tap on a button inside a
slide - usually the very thing that triggered the programmatic move. `sliderFirstMove` is the only
signal separating a swipe from a code-driven move, because loop mode re-announces the slide it left
after a programmatic move and that echo is identical by index.

**Slide 0 is falsy.** The intent guards test `!== null` explicitly (`isIntentArmed`). A truthiness
check silently refuses to defend the first slide, which is exactly where a reset lands.

**An unset option is null, not absent.** `SwiperOptions` serializes members the caller never set as
null, and applying them would override Swiper's own defaults with nothing. Only null and undefined
are skipped - `false`, `0` and `""` are all legitimate Swiper settings and must survive.

**Call `ArmAnchor` *before* mutating the slides.** A `MutationObserver` callback is delivered at the
microtask checkpoint - after the framework's DOM patch, before the next paint - so the correction
lands in the same frame as the change. An interop call made after the mutation is a separate task and
the browser is free to paint the shifted-but-uncorrected track in between.

**`UpdateAndAnchor` takes the in-process interop path deliberately.** Awaiting yields, and the browser
paints the wrong offset in that gap - measured at ~21ms on a Pixel 6, i.e. a visible frame of the
wrong slide. Blazor Server has no in-process option and keeps the async path.

**`overflow-y: hidden` on the wrapper is cssMode-only.** Everywhere else the wrapper is a plain
transformed track, and setting it forces computed `overflow-x` from visible to auto - turning the
wrapper into a one-slide-wide clip box that travels with the transform, so every slide but the first
vanishes.

**The element can be gone before `initialize` runs.** A conditionally rendered pager can collapse on
the very tap that scheduled it, and `await customElements.whenDefined(...)` widens the window
further. Check `element` and `element.isConnected` on both sides of that await.

**A new `wwwroot` module must be a sibling.** `swiper-interop.js` imports `./swiper-policy.js`, which
only resolves under `_content/Kebechet.Blazor.Swiper/` because both ship from the same folder.
`PackagingTests` pins that, and the relative-import check will catch a module that never shipped.

**The package version encodes the bundle version.** The first three parts track the vendored Swiper
release and the fourth is this wrapper's own revision, so re-vendoring and bumping go together.
`PackagingTests` reads the bundle banner and fails if the two drift.

## Storybook gotchas

- Story ids are `{title}--{story name}`, lowercased with spaces replaced by dashes:
  `[Stories("Components/Swiper")]` + `<Story Name="Loop">` → `components-swiper--loop`. The e2e suite
  addresses stories by that id, so renaming a story breaks it.
- `<Description>` renders **only** on the Docs tab - `DocsPage.razor` is its sole consumer. Anything
  a reader needs while looking at the canvas has to be in the canvas markup too, which is what the
  `.demo-hint` paragraphs are for.
- The e2e suite addresses `iframe.html` directly rather than the storybook shell: no sidebar, no
  panels, no cross-frame hop to reach the slider.
- Stories expose their state through `data-testid` hooks and a `SwiperState` panel, which is the only
  thing the e2e suite can assert against. A story a test needs to drive has to carry one.

## Driving a swipe from Playwright

Both rules are in `tests/Blazor.Swiper.E2E/README.md`, and breaking either makes the suite pass while
testing nothing:

1. **The page must not report touch support.** Swiper binds touch listeners when the browser
   advertises touch and pointer listeners otherwise. Playwright's touchscreen can tap but not drag,
   so `HasTouch = true` makes every swipe silently inert - the slider never moves and it reads as a
   broken assertion rather than a broken gesture.
2. **Step the pointer with a frame's delay between moves.** Swiper decides whether a gesture is a
   swipe from the movement between successive pointer events, so one large jump gives it nothing to
   measure. The first of those intermediate moves is also what raises `sliderFirstMove`.

Drag past half a slide or Swiper treats it as a short swipe and snaps back.

`WaitForStateAsync` prints the last observed state in its failure message. Read it before touching
readiness waits or retry loops - a swipe that moved the slider but reported `userSlideChanges = 0` is
a library bug, not a flaky harness.

## Testing gotchas

`someString.ShouldContain(expected, customMessage)` does not compile: Shouldly binds the two-argument
form to the `IEnumerable<char>` overload and the message is read as an `Expression<Func<char, bool>>`
(CS1503). Use `someString.Contains(expected).ShouldBeTrue(customMessage)` when the failure needs an
explanation.
