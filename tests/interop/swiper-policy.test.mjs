import assert from "node:assert/strict";
import test from "node:test";

import {
    applicableOptions,
    optionalSpeed,
    navigationMode,
    isIntentArmed,
    shouldDisarmIntent,
    shouldReanchor,
    scrollPlan,
    scrollPositionAt
} from "../../src/Blazor.Swiper/wwwroot/swiper-policy.js";

test("ApplicableOptions_UnsetMember_IsSkippedSoSwiperKeepsItsOwnDefault", () => {
    // Arrange - SwiperOptions serializes an unset member as null; applying it would override
    // Swiper's default with nothing.
    const options = { slidesPerView: 2, spaceBetween: null, initialSlide: undefined };

    // Act
    const applied = applicableOptions(options);

    // Assert
    assert.deepEqual(applied, [["slidesPerView", 2]]);
});

test("ApplicableOptions_FalseOrZero_IsAppliedRatherThanSkipped", () => {
    // Arrange - the difference between "unset" and "explicitly off" is the whole point of the
    // filter, and every falsy value here is a legitimate Swiper setting.
    const options = { loop: false, spaceBetween: 0, wrapperClass: "" };

    // Act
    const applied = applicableOptions(options);

    // Assert
    assert.deepEqual(applied, [["loop", false], ["spaceBetween", 0], ["wrapperClass", ""]]);
});

test("ApplicableOptions_NoOptionsAtAll_YieldsNothing", () => {
    assert.deepEqual(applicableOptions(null), []);
    assert.deepEqual(applicableOptions(undefined), []);
});

test("OptionalSpeed_NullFromDotNet_BecomesUndefinedSoSwiperUsesItsConfiguredSpeed", () => {
    // Swiper reads a missing argument as "use params.speed"; passing null instead would be
    // coerced to 0 and make every programmatic move jump.
    assert.equal(optionalSpeed(null), undefined);
    assert.equal(optionalSpeed(undefined), undefined);
});

test("OptionalSpeed_Zero_StaysZeroBecauseAnInstantMoveIsARealSpeed", () => {
    // Guards against "speed || undefined", which reads an intentional 0 as "unset".
    assert.equal(optionalSpeed(0), 0);
    assert.equal(optionalSpeed(400), 400);
});

test("NavigationMode_CssMode_ScrollsTheWrapperEvenWhenLooping", () => {
    // cssMode has no transform track to slide, so it wins over loop.
    assert.equal(navigationMode({ cssMode: true, loop: true }), "scroll");
    assert.equal(navigationMode({ cssMode: true, loop: false }), "scroll");
});

test("NavigationMode_Loop_TakesTheLogicalIndexRoute", () => {
    // slideTo would take the shifted index, which does not match the realIndex reported to .NET.
    assert.equal(navigationMode({ cssMode: false, loop: true }), "loop");
});

test("NavigationMode_PlainSlider_TakesTheDirectRoute", () => {
    assert.equal(navigationMode({ cssMode: false, loop: false }), "direct");
    assert.equal(navigationMode({}), "direct");
});

test("IsIntentArmed_SlideZero_CountsAsArmed", () => {
    // The resize guard schedules its deferred check on this alone, so treating falsy 0 as "nothing
    // pending" would leave a move to the first slide undefended.
    assert.equal(isIntentArmed(0), true);
});

test("IsIntentArmed_NothingPending_IsNotArmed", () => {
    assert.equal(isIntentArmed(null), false);
    assert.equal(isIntentArmed(undefined), false);
});

test("ShouldDisarmIntent_MoveArrived_ClearsTheIntent", () => {
    assert.equal(shouldDisarmIntent(3, 3, false), true);
});

test("ShouldDisarmIntent_UserDragged_ClearsTheIntentBecauseTheSwipeSupersedesIt", () => {
    assert.equal(shouldDisarmIntent(1, 3, true), true);
});

test("ShouldDisarmIntent_ReanchorMidFlight_KeepsTheIntentArmed", () => {
    // Swiper's own re-anchors slide at speed 0 and raise their own transitionend. Disarming on
    // any transitionend would drop the guard in the very frame it exists to correct, and the
    // re-anchor would stand.
    assert.equal(shouldDisarmIntent(1, 3, false), false);
});

test("ShouldDisarmIntent_NothingArmed_HasNothingToClear", () => {
    assert.equal(shouldDisarmIntent(0, null, false), false);
});

test("ShouldReanchor_ResizeDriftedOffTheIntendedSlide_Reanchors", () => {
    assert.equal(shouldReanchor(0, 3, false), true);
});

test("ShouldReanchor_IntendedSlideIsZero_StillReanchors", () => {
    // Slide 0 is falsy. A "!intendedIndex" guard would silently refuse to correct the first
    // slide - the one a reset lands on most often.
    assert.equal(shouldReanchor(2, 0, false), true);
});

test("ShouldReanchor_AlreadyOnTheIntendedSlide_LeavesTheSliderAlone", () => {
    assert.equal(shouldReanchor(3, 3, false), false);
});

test("ShouldReanchor_UserIsDragging_LeavesTheSliderAlone", () => {
    // The host's move is stale the moment the user takes over.
    assert.equal(shouldReanchor(0, 3, true), false);
});

test("ShouldReanchor_NoIntentArmed_LeavesASliderAtRestAlone", () => {
    assert.equal(shouldReanchor(2, null, false), false);
    assert.equal(shouldReanchor(2, undefined, false), false);
});

test("ScrollPlan_SubPixelDistance_DoesNothing", () => {
    // Sub-pixel corrections are invisible and would restart the snap-type dance for nothing.
    assert.equal(scrollPlan(100, 100.4, 300, 300).kind, "none");
});

test("ScrollPlan_ZeroSpeed_JumpsStraightToTheTarget", () => {
    // Act
    const plan = scrollPlan(0, 480, 0, 300);

    // Assert
    assert.equal(plan.kind, "instant");
    assert.equal(plan.target, 480);
});

test("ScrollPlan_NoSpeedGiven_FallsBackToTheSlidersConfiguredSpeed", () => {
    // Act
    const plan = scrollPlan(0, 480, null, 700);

    // Assert
    assert.equal(plan.kind, "animate");
    assert.equal(plan.duration, 700);
});

test("ScrollPlan_NoSpeedAnywhere_FallsBackToSwipersOwnDefault", () => {
    assert.equal(scrollPlan(0, 480, null, undefined).duration, 300);
});

test("ScrollPlan_BackwardsMove_AnimatesOverANegativeDistance", () => {
    // Act
    const plan = scrollPlan(480, 0, 300, 300);

    // Assert
    assert.equal(plan.kind, "animate");
    assert.equal(plan.distance, -480);
});

test("ScrollPositionAt_TheFirstFrame_IsStillTheStartingOffset", () => {
    assert.equal(scrollPositionAt(20, 100, 0, 300), 20);
});

test("ScrollPositionAt_PastTheDuration_LandsExactlyOnTargetWithoutOvershooting", () => {
    // A late frame must not carry the easing past 1, which would scroll beyond the slide.
    assert.equal(scrollPositionAt(20, 100, 300, 300), 120);
    assert.equal(scrollPositionAt(20, 100, 5000, 300), 120);
});

test("ScrollPositionAt_Midway_IsAlreadyMostOfTheWayBecauseTheEaseIsFrontLoaded", () => {
    // easeOutCubic(0.5) = 1 - 0.5^3 = 0.875
    assert.equal(scrollPositionAt(0, 100, 150, 300), 87.5);
});
