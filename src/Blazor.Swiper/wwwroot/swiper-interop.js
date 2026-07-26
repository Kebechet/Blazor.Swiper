// Interop module for the Swiper Blazor component. The Swiper Element (<swiper-container>) is rendered
// with init="false" so we can assign parameters before calling initialize().
//
// Beyond forwarding calls, this module closes gaps that every host would otherwise have to work around
// itself: telling a user's swipe apart from a code-driven move, keeping autoHeight true after init,
// making programmatic navigation work in cssMode, and keeping Swiper's own re-anchors from undoing a
// move that is already in flight. Per-instance state lives on `swiper.__blazor`.

function state(swiper) {
    swiper.__blazor ??= {
        isUserDriven: false,
        intendedIndex: null,
        anchorIndex: null,
        anchorObserver: null,
        slideSetObserver: null,
        slideResizeObserver: null
    };
    return swiper.__blazor;
}

export async function initialize(element, options, dotNetRef) {
    // The host can tear the slider down before this runs - a conditionally rendered pager collapsing on the
    // very tap that scheduled it - and the reference then marshals to null. Awaiting the element definition
    // widens that window further, so re-check after it too. Without this, initialising a slider that no
    // longer exists throws out of OnAfterRenderAsync and into the host's error boundary.
    if (!element) {
        return;
    }

    await customElements.whenDefined("swiper-container");

    if (!element.isConnected) {
        return;
    }

    // Apply options as element properties (Swiper reads them on initialize). Skip null/undefined so
    // an unset option falls back to Swiper's own default rather than overriding it.
    if (options) {
        for (const [key, value] of Object.entries(options)) {
            if (value !== null && value !== undefined) {
                element[key] = value;
            }
        }
    }

    // Listeners are attached AFTER initialize() on purpose. Swiper announces its starting position from
    // inside init() (runCallbacksOnInit defaults on), and that announcement is not news to the host - it
    // is the position the host just asked for. Forwarding it makes every consumer write an "ignore the
    // first one" guard, and in a two-way binding it reports slide 0 back before the host has settled.
    element.initialize();

    const swiper = element?.swiper;

    element.addEventListener("swiperslidechange", () => {
        if (!element.swiper) {
            return;
        }
        // realIndex is the logical slide index; in loop mode it differs from activeIndex (which counts
        // the shifted/duplicated slides). It equals activeIndex when loop is off, so it is always correct.
        dotNetRef.invokeMethodAsync("OnSlideChangeInternal", element.swiper.realIndex, state(element.swiper).isUserDriven);
    });
    element.addEventListener("swiperreachend", () => {
        dotNetRef.invokeMethodAsync("OnReachEndInternal");
    });
    element.addEventListener("swiperreachbeginning", () => {
        dotNetRef.invokeMethodAsync("OnReachBeginningInternal");
    });
    element.addEventListener("swipertransitionend", () => {
        if (element.swiper) {
            const blazorState = state(element.swiper);
            // Disarm only once the move is settled or abandoned - NOT on any transition end. Swiper's own
            // re-anchors slide instantly (speed 0) and raise their own transitionend, so clearing here
            // unconditionally disarms the guard in the very frame it exists to correct, and the re-anchor
            // stands. A user drag supersedes the host's move outright, so that clears it too.
            const hasArrived = element.swiper.realIndex === blazorState.intendedIndex;
            if (blazorState.isUserDriven || hasArrived) {
                blazorState.intendedIndex = null;
            }
            blazorState.isUserDriven = false;
        }
        dotNetRef.invokeMethodAsync("OnTransitionEndInternal");
    });
    // The drag has moved the slider for the first time. This is the only signal that a slide change came
    // from the user rather than from code: loop mode re-announces the slide it LEFT after a programmatic
    // move, and that echo is indistinguishable from a swipe by index alone. Deliberately NOT touchstart -
    // that also fires for a plain tap on a button inside a slide, which is usually what triggered the
    // programmatic move in the first place. Cleared on transitionend, since the snap outlives the pointer.
    element.addEventListener("swipersliderfirstmove", () => {
        if (element.swiper) {
            state(element.swiper).isUserDriven = true;
        }
        dotNetRef.invokeMethodAsync("OnSliderFirstMoveInternal");
    });

    if (swiper) {
        attachIntendedIndexGuard(element, swiper);

        if (swiper.params.autoHeight) {
            attachLiveAutoHeight(element, swiper);
        }
    }
}

// --- programmatic navigation -------------------------------------------------------------------------

export function slideTo(element, index, speed) {
    const swiper = element?.swiper;
    if (!swiper) {
        return;
    }

    state(swiper).intendedIndex = index;

    if (swiper.params.cssMode) {
        scrollToSlide(swiper, index, speed);
        return;
    }

    // In loop mode slideTo takes a raw (shifted) index; slideToLoop takes the logical index, matching
    // the realIndex we report back. Route through it so programmatic navigation lines up with loop.
    if (swiper.params.loop) {
        swiper.slideToLoop(index, speed ?? undefined);
    } else {
        swiper.slideTo(index, speed ?? undefined);
    }
}

// cssMode moves the slider by scrolling the wrapper, and Swiper drives that with a native smooth scroll.
// That is unusable under scroll-snap: `scroll-snap-type: mandatory` yanks every intermediate position back
// to a snap point, and any re-render during the scroll cancels it outright, leaving the slider where it
// started. Writing scrollLeft per frame is not cancellable, so the animation always completes.
function scrollToSlide(swiper, index, speed) {
    const wrapper = swiper.wrapperEl;
    const target = swiper.slides[index]?.offsetLeft ?? 0;
    const start = wrapper.scrollLeft;
    const distance = target - start;
    if (Math.abs(distance) < 1) {
        return;
    }

    const durationMs = speed ?? swiper.params.speed ?? 300;
    if (durationMs <= 0) {
        wrapper.scrollLeft = target;
        return;
    }

    const previousSnapType = wrapper.style.scrollSnapType;
    wrapper.style.scrollSnapType = "none";

    const startTime = performance.now();
    const easeOutCubic = (progress) => 1 - Math.pow(1 - progress, 3);

    const step = (now) => {
        const progress = Math.min((now - startTime) / durationMs, 1);
        wrapper.scrollLeft = start + distance * easeOutCubic(progress);
        if (progress < 1) {
            requestAnimationFrame(step);
        } else {
            wrapper.style.scrollSnapType = previousSnapType;
        }
    };
    requestAnimationFrame(step);
}

export function slideNext(element, speed) {
    element?.swiper?.slideNext(speed ?? undefined);
}

export function slidePrev(element, speed) {
    element?.swiper?.slidePrev(speed ?? undefined);
}

// Swiper's resize handling ends by re-anchoring onto the index it holds, and it defers that by a frame.
// When the host moves the slider during the same interaction that changed the size, the deferred re-anchor
// carries the PRE-move index, lands last, and silently undoes the move. Re-asserting the index the host
// actually asked for - and only while its move is still in flight - keeps the host authoritative without
// disturbing a slider at rest or a move the user is making.
function attachIntendedIndexGuard(element, swiper) {
    element.addEventListener("swiperresize", () => {
        const blazorState = state(swiper);
        if (blazorState.intendedIndex === null || blazorState.isUserDriven) {
            return;
        }

        requestAnimationFrame(() => {
            if (!element.swiper || blazorState.intendedIndex === null || blazorState.isUserDriven) {
                return;
            }
            if (swiper.realIndex !== blazorState.intendedIndex) {
                applyAnchor(swiper, blazorState.intendedIndex);
            }
        });
    });
}

// --- live autoHeight ---------------------------------------------------------------------------------

// Swiper measures autoHeight once, against whatever the slides contained at init. Any content that arrives
// later - an async load resolving, an image decoding, a lazily mounted section - leaves the track pinned to
// the height it saw first, which clips the real content. Observing the slides keeps the measurement true.
function attachLiveAutoHeight(element, swiper) {
    const blazorState = state(swiper);

    // In cssMode the wrapper IS the scroll container, with overflow:auto on BOTH axes - so it stays
    // vertically scrollable down to the TALLEST slide no matter which one is active, and a short slide
    // scrolls on into empty space owned by a taller sibling. autoHeight already pins the wrapper to the
    // active slide, so clip the rest. Only in cssMode: elsewhere the wrapper is a plain transformed track,
    // and overflow-y:hidden would force computed overflow-x from visible to auto, turning it into a
    // one-slide-wide clip box that travels with the transform - every slide but the first would vanish.
    if (swiper.params.cssMode) {
        swiper.wrapperEl.style.overflowY = "hidden";
    }

    const observeSlides = () => {
        blazorState.slideResizeObserver?.disconnect();
        blazorState.slideResizeObserver = new ResizeObserver(() => swiper.updateAutoHeight(0));
        for (const slide of swiper.slides) {
            blazorState.slideResizeObserver.observe(slide);
        }
        swiper.updateAutoHeight(0);
    };

    // A slide added or removed by the host is a new set to observe, and a new height to measure.
    blazorState.slideSetObserver = new MutationObserver(observeSlides);
    blazorState.slideSetObserver.observe(swiper.slidesEl, { childList: true });

    // The ResizeObserver reacts to a slide's size changing, not to a different slide becoming active - and
    // in cssMode Swiper's own transition-driven autoHeight never runs, since it has no transition events.
    element.addEventListener("swiperslidechange", () => swiper.updateAutoHeight(0));

    observeSlides();
}

// --- anchoring ---------------------------------------------------------------------------------------

// Recalculate and settle instantly on `index`. update() on its own is not enough: it finishes with
// slideTo(swiper.activeIndex) using the index from BEFORE the mutation, which addresses a different slide
// once the collection changed. Both steps run in one turn so the intermediate position is never laid out.
function applyAnchor(swiper, index) {
    // slideTo obeys allowSlideNext/allowSlidePrev, but this is a correction rather than navigation, so the
    // caller's swipe locks must not be able to veto it.
    const wasSlideNextAllowed = swiper.allowSlideNext;
    const wasSlidePrevAllowed = swiper.allowSlidePrev;
    swiper.allowSlideNext = true;
    swiper.allowSlidePrev = true;

    swiper.update();
    swiper.slideTo(index, 0, false, true);

    swiper.allowSlideNext = wasSlideNextAllowed;
    swiper.allowSlidePrev = wasSlidePrevAllowed;
}

// Re-anchor onto `index` the moment the host framework next adds or removes slide elements itself.
//
// Call this BEFORE the mutation. A framework that owns the slide DOM (Blazor, React, ...) shifts every
// later slide sideways when it removes one that sits before the active slide, while Swiper's transform
// still points at the old offset - so the wrong slide is on screen until Swiper is told. Correcting from
// an interop call made after the mutation cannot win that race: the call is a separate task, so the
// browser is free to paint in between (measured at 9-66ms on real hardware, i.e. one to four wrong frames).
//
// A MutationObserver callback is delivered at the microtask checkpoint - after the framework's DOM patch
// but before the next paint - so the correction is guaranteed to land in the same frame as the mutation
// that caused it. This is a platform ordering guarantee rather than a timing race.
export function armAnchor(element, index) {
    const swiper = element?.swiper;
    if (!swiper) {
        return;
    }

    const blazorState = state(swiper);
    if (!blazorState.anchorObserver) {
        blazorState.anchorObserver = new MutationObserver(() => {
            const anchorIndex = blazorState.anchorIndex;
            if (anchorIndex === null || anchorIndex === undefined) {
                return;
            }
            // One-shot: only the mutation this was armed for should move the slider.
            blazorState.anchorIndex = null;
            blazorState.anchorObserver.disconnect();
            applyAnchor(swiper, anchorIndex);
        });
    }

    blazorState.anchorIndex = index;
    blazorState.anchorObserver.observe(swiper.slidesEl, { childList: true });
}

export function updateAndAnchor(element, index) {
    const swiper = element?.swiper;
    if (!swiper) {
        return;
    }
    applyAnchor(swiper, index);
}

export function update(element) {
    element?.swiper?.update();
}

// --- misc ---------------------------------------------------------------------------------------------

export function setAllowSlideNext(element, value) {
    if (element?.swiper) {
        element.swiper.allowSlideNext = value;
    }
}

export function setAllowSlidePrev(element, value) {
    if (element?.swiper) {
        element.swiper.allowSlidePrev = value;
    }
}

export function destroy(element) {
    const swiper = element?.swiper;
    if (!swiper) {
        return;
    }

    const blazorState = swiper.__blazor;
    blazorState?.anchorObserver?.disconnect();
    blazorState?.slideSetObserver?.disconnect();
    blazorState?.slideResizeObserver?.disconnect();

    swiper.destroy(true, true);
}
