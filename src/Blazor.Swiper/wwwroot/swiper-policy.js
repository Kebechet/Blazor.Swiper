// Decision logic for the interop module: what an option value means, which route a programmatic
// move takes, when a pending move is still worth defending, and where a cssMode scroll should be
// on a given frame. Kept apart from swiper-interop.js because none of it touches Swiper, the DOM
// or the interop channel - it works on plain values - so it can be reasoned about, and tested,
// without a browser.

/**
 * The option entries that should actually be written onto the element.
 *
 * SwiperOptions serializes a member the caller never set as null, and writing that would override
 * Swiper's own default with nothing. Absence is the signal, so only null and undefined are
 * dropped - `false`, `0` and `""` are all legitimate Swiper settings and must survive.
 */
export function applicableOptions(options) {
    return Object.entries(options ?? {})
        .filter(([, value]) => value !== null && value !== undefined);
}

/**
 * A speed argument as Swiper wants it.
 *
 * Swiper reads a missing argument as "use params.speed", but .NET sends an unset `int?` across
 * interop as null, which Swiper would coerce to 0 and jump. Only null becomes undefined: 0 is an
 * intentional instant move and has to survive, which `speed || undefined` would not manage.
 */
export function optionalSpeed(speed) {
    return speed ?? undefined;
}

/**
 * Which of the three programmatic-navigation routes applies.
 *
 * cssMode is checked first because it has no transform track to slide at all - the wrapper is a
 * scroll container - so it wins even when loop is also on. Loop then takes the logical-index
 * route, since slideTo would expect the shifted index while realIndex, the one reported to .NET,
 * is the logical one.
 */
export function navigationMode(params) {
    if (params.cssMode) {
        return "scroll";
    }

    return params.loop ? "loop" : "direct";
}

/**
 * Whether a settled transition should clear the host's pending move.
 *
 * Not every transitionend belongs to that move. Swiper's own re-anchors slide at speed 0 and raise
 * one too, so clearing unconditionally would drop the guard in the very frame it exists to correct
 * and let the re-anchor stand. Clear once the move has arrived, or as soon as the user drags -
 * a swipe supersedes the host's move outright.
 */
export function shouldDisarmIntent(realIndex, intendedIndex, isUserDriven) {
    if (!isIntentArmed(intendedIndex)) {
        return false;
    }

    return isUserDriven || realIndex === intendedIndex;
}

/**
 * Whether a resize should re-assert the index the host asked for.
 *
 * Swiper's resize handling ends by re-anchoring onto the index it holds, deferred by a frame. When
 * the host moves the slider during the same interaction that changed the size, that deferred
 * re-anchor carries the pre-move index, lands last, and silently undoes the move.
 */
export function shouldReanchor(realIndex, intendedIndex, isUserDriven) {
    if (!isIntentArmed(intendedIndex) || isUserDriven) {
        return false;
    }

    return realIndex !== intendedIndex;
}

/**
 * Whether the host has a move pending that the guards should defend.
 *
 * Slide 0 is a perfectly ordinary target and is falsy, so this is an explicit null check. A
 * truthiness test would refuse to defend the first slide - the one a reset lands on most.
 */
export function isIntentArmed(intendedIndex) {
    return intendedIndex !== null && intendedIndex !== undefined;
}

/**
 * How a cssMode move from `start` to `target` should be carried out.
 *
 * Sub-pixel distances are dropped: they are invisible, and running the animation for one would
 * suspend and restore scroll-snap for no reason.
 */
export function scrollPlan(start, target, speed, defaultSpeed) {
    const distance = target - start;
    if (Math.abs(distance) < 1) {
        return { kind: "none", target, distance: 0, duration: 0 };
    }

    const duration = speed ?? defaultSpeed ?? 300;
    if (duration <= 0) {
        return { kind: "instant", target, distance, duration: 0 };
    }

    return { kind: "animate", target, distance, duration };
}

/**
 * The scroll offset an animated cssMode move should hold at `elapsed` milliseconds in.
 *
 * Progress is clamped, so a frame delivered after the deadline lands exactly on the target rather
 * than carrying the ease past 1 and scrolling beyond the slide.
 */
export function scrollPositionAt(start, distance, elapsed, duration) {
    const progress = Math.min(elapsed / duration, 1);
    const eased = 1 - Math.pow(1 - progress, 3);

    return start + distance * eased;
}
