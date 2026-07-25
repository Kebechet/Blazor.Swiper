// Interop module for the Swiper Blazor component. The Swiper Element (<swiper-container>) is rendered
// with init="false" so we can assign parameters and attach listeners before calling initialize().

export async function initialize(element, options, dotNetRef) {
    await customElements.whenDefined("swiper-container");

    // Apply options as element properties (Swiper reads them on initialize). Skip null/undefined so
    // an unset option falls back to Swiper's own default rather than overriding it.
    if (options) {
        for (const [key, value] of Object.entries(options)) {
            if (value !== null && value !== undefined) {
                element[key] = value;
            }
        }
    }

    element.addEventListener("swiperslidechange", () => {
        // realIndex is the logical slide index; in loop mode it differs from activeIndex (which counts
        // the shifted/duplicated slides). It equals activeIndex when loop is off, so it is always correct.
        const index = element.swiper ? element.swiper.realIndex : 0;
        dotNetRef.invokeMethodAsync("OnSlideChangeInternal", index);
    });
    element.addEventListener("swiperreachend", () => {
        dotNetRef.invokeMethodAsync("OnReachEndInternal");
    });
    element.addEventListener("swiperreachbeginning", () => {
        dotNetRef.invokeMethodAsync("OnReachBeginningInternal");
    });
    element.addEventListener("swipertransitionend", () => {
        dotNetRef.invokeMethodAsync("OnTransitionEndInternal");
    });

    element.initialize();
}

export function slideTo(element, index, speed) {
    const swiper = element.swiper;
    if (!swiper) {
        return;
    }
    // In loop mode slideTo takes a raw (shifted) index; slideToLoop takes the logical index, matching
    // the realIndex we report back. Route through it so programmatic navigation lines up with loop.
    if (swiper.params?.loop) {
        swiper.slideToLoop(index, speed ?? undefined);
    } else {
        swiper.slideTo(index, speed ?? undefined);
    }
}

export function slideNext(element, speed) {
    element.swiper?.slideNext(speed ?? undefined);
}

export function slidePrev(element, speed) {
    element.swiper?.slidePrev(speed ?? undefined);
}

export function update(element) {
    element.swiper?.update();
}

export function setAllowSlideNext(element, value) {
    if (element.swiper) {
        element.swiper.allowSlideNext = value;
    }
}

export function setAllowSlidePrev(element, value) {
    if (element.swiper) {
        element.swiper.allowSlidePrev = value;
    }
}

export function destroy(element) {
    element.swiper?.destroy(true, true);
}
