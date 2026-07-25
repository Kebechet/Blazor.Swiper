// Blazor JS initializer. Blazor auto-discovers "{PackageId}.lib.module.js" and runs the exported
// hooks on startup, so consumers never add a <script> tag manually. We inject the Swiper Element
// bundle, which self-registers the <swiper-container> / <swiper-slide> custom elements and injects
// its own styles into their shadow DOM.

export function beforeWebStart(options, extensions) {
    injectSwiperElement();
}

export function beforeStart(options, extensions) {
    injectSwiperElement();
}

function injectSwiperElement() {
    const marker = "data-blazor-swiper";
    if (document.querySelector(`script[${marker}]`)) {
        return;
    }

    const script = document.createElement("script");
    script.src = "_content/Kebechet.Blazor.Swiper/swiper-element-bundle.min.js";
    script.setAttribute(marker, "");
    document.head.appendChild(script);
}
