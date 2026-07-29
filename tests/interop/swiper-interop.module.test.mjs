import assert from "node:assert/strict";
import test from "node:test";

// The interop module touches no browser global at module scope, so node can load it. That makes
// this the only automated check that the file parses, that its import of swiper-policy.js
// resolves, and that every name it imports actually exists. bUnit stubs the module out entirely,
// so a dangling reference here would otherwise reach a browser before anything noticed.
const modulePath = "../../src/Blazor.Swiper/wwwroot/swiper-interop.js";

test("InteropModule_Loaded_LinksAgainstThePolicyModule", async () => {
    await assert.doesNotReject(() => import(modulePath));
});

test("InteropModule_Loaded_ExportsEveryEntryPointTheComponentCalls", async () => {
    // Arrange - each name is invoked by Swiper.razor.cs through InvokeVoidAsync, which fails at
    // runtime rather than at build time when the export is missing or renamed.
    const expectedExports = [
        "initialize",
        "slideTo",
        "slideNext",
        "slidePrev",
        "update",
        "armAnchor",
        "updateAndAnchor",
        "setAllowSlideNext",
        "setAllowSlidePrev",
        "destroy"
    ];

    // Act
    const interop = await import(modulePath);

    // Assert
    for (const name of expectedExports) {
        assert.equal(typeof interop[name], "function", `swiper-interop.js must export ${name}()`);
    }
});
