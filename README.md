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

### Vertical direction needs a height

A vertical Swiper (`Direction = SwiperDirection.Vertical`) sizes its slides from the `<Swiper>` element's own height, which defaults to its content height. Give the `<Swiper>` an explicit height, or it collapses and slides stack:

```razor
<Swiper Options="new() { Direction = SwiperDirection.Vertical }" style="height: 300px;">
    ...
</Swiper>
```

## License

[MIT](LICENSE)
