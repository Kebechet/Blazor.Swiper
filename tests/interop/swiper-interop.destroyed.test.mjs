import assert from "node:assert/strict";
import test from "node:test";

import * as interop from "../../src/Blazor.Swiper/wwwroot/swiper-interop.js";

// When <swiper-container> leaves the DOM, its disconnectedCallback calls this.swiper.destroy(). Swiper's
// destroy nulls and deletes every own property of the instance and clears `.swiper` on its own `el` - but
// `el` is the `.swiper` div inside the shadow root, not the host. The host keeps its reference, so
// `element.swiper` is still truthy while `params` is gone. A host that calls in during that window (a
// pending slideTo from a pager that just collapsed) must be treated as having no slider at all.

class FakeSwiper {
    constructor() {
        this.params = { cssMode: false, loop: false, speed: 300 };
        this.realIndex = 0;
        this.slidesEl = {};
        this.thumbs = { init() {}, update() {} };
        this.controller = {};
    }

    slideTo() { return this.params.speed; }
    slideNext() { return this.params.speed; }
    slidePrev() { return this.params.speed; }
    update() { return this.params.speed; }
    getTranslate() { return this.params.speed; }
    enable() { return this.params.speed; }

    // The same teardown Swiper's destroy(deleteInstance = true) performs, minus the DOM clean-up.
    destroy() {
        for (const key of Object.keys(this)) {
            try { this[key] = null; } catch { }
            try { delete this[key]; } catch { }
        }
        this.destroyed = true;
    }
}

function hostWithDestroyedSwiper() {
    const swiper = new FakeSwiper();
    swiper.destroy();
    return { swiper };
}

test("SlideTo_HostSwiperDestroyed_DoesNothing", () => {
    // Arrange
    const host = hostWithDestroyedSwiper();

    // Act & Assert
    assert.doesNotThrow(() => interop.slideTo(host, 2, 300));
});

test("EntryPoints_HostSwiperDestroyed_DoNotThrow", () => {
    // Arrange
    const host = hostWithDestroyedSwiper();
    const calls = {
        slideNext: () => interop.slideNext(host, 300),
        slidePrev: () => interop.slidePrev(host, 300),
        update: () => interop.update(host),
        enable: () => interop.enable(host),
        getTranslate: () => interop.getTranslate(host),
        getState: () => interop.getState(host),
        updateOptions: () => interop.updateOptions(host, { speed: 500 }),
        updateAndAnchor: () => interop.updateAndAnchor(host, 1),
        setThumbs: () => interop.setThumbs(host, hostWithDestroyedSwiper()),
        setController: () => interop.setController(host, hostWithDestroyedSwiper()),
    };

    // Act & Assert
    for (const [name, call] of Object.entries(calls)) {
        assert.doesNotThrow(call, `${name}() must treat a destroyed slider as absent`);
    }
});

test("ReadOnlyEntryPoints_HostSwiperDestroyed_ReportNoSlider", () => {
    // Arrange
    const host = hostWithDestroyedSwiper();

    // Act
    const state = interop.getState(host);
    const translate = interop.getTranslate(host);

    // Assert
    assert.equal(state, null);
    assert.equal(translate, 0);
});
