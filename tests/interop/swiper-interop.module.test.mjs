import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync, readdirSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

// The interop module touches no browser global at module scope, so node can load it. That makes
// this the only automated check that the file parses, that its import of swiper-policy.js
// resolves, and that every name it imports actually exists. bUnit stubs the module out entirely,
// so a dangling reference here would otherwise reach a browser before anything noticed.
const modulePath = "../../src/Blazor.Swiper/wwwroot/swiper-interop.js";
const sourceRoot = join(dirname(fileURLToPath(import.meta.url)), "..", "..", "src", "Blazor.Swiper");

test("InteropModule_Loaded_LinksAgainstThePolicyModule", async () => {
    await assert.doesNotReject(() => import(modulePath));
});

test("InteropModule_Loaded_ExportsEveryEntryPointTheComponentCalls", async () => {
    // Arrange - the expected names are read out of the C# rather than listed here, so a method
    // added on one side and forgotten on the other fails immediately. Every one of these is called
    // by string through JS interop, which fails at runtime rather than at build time.
    const expectedExports = interopIdentifiersInSource();

    // Act
    const interop = await import(modulePath);

    // Assert
    assert.ok(expectedExports.length > 20, `expected to find the interop calls in the C# source, found ${expectedExports.length}`);
    for (const name of expectedExports) {
        assert.equal(typeof interop[name], "function", `swiper-interop.js must export ${name}()`);
    }
});

test("InteropModule_Loaded_ExportsNothingTheComponentNeverCalls", async () => {
    // The other direction: an export left behind after the C# stopped calling it is dead code that
    // still reads as part of the contract.
    const called = new Set(interopIdentifiersInSource());
    const interop = await import(modulePath);

    const orphans = Object.keys(interop).filter(name => !called.has(name));

    assert.deepEqual(orphans, [], `swiper-interop.js exports ${orphans.join(", ")}, which nothing in the component calls`);
});

/** Every interop identifier the component invokes, read straight out of the C# source. */
function interopIdentifiersInSource() {
    const identifiers = new Set();
    const pattern = /(?:InvokeVoidAsync|InvokeVoid|InvokeAsync<[^>]*>|CallAsync<[^>]*>|CallAsync)\(\s*"([a-zA-Z][a-zA-Z0-9]*)"/g;

    for (const file of csharpFiles(sourceRoot)) {
        const source = readFileSync(file, "utf8");
        for (const match of source.matchAll(pattern)) {
            // The module import itself is a call into the browser, not into this module.
            if (match[1] !== "import") {
                identifiers.add(match[1]);
            }
        }
    }

    return [...identifiers].sort();
}

function csharpFiles(directory) {
    const files = [];

    for (const entry of readdirSync(directory, { withFileTypes: true })) {
        const path = join(directory, entry.name);

        if (entry.isDirectory() && entry.name !== "bin" && entry.name !== "obj") {
            files.push(...csharpFiles(path));
            continue;
        }

        if (entry.isFile() && entry.name.endsWith(".cs")) {
            files.push(path);
        }
    }

    return files;
}
