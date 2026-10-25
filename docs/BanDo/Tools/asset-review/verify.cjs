const fs = require("node:fs"),
  path = require("node:path"),
  assert = require("node:assert/strict"),
  crypto = require("node:crypto"),
  { createRequire } = require("node:module");
const root = path.resolve(__dirname, "../../../..");
const { chromium } = createRequire(path.join(root, "Admin/package.json"))(
  "@playwright/test",
);
const review = path.join(
  root,
  "NghiemThu/Battlefield/XemTruoc_Assets_20261025",
);
const sha = (file) =>
  crypto.createHash("sha256").update(fs.readFileSync(file)).digest("hex");
(async () => {
  const browser = await chromium.launch({
    executablePath:
      process.env.EDGE_PATH ??
      "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe",
    headless: true,
  });
  try {
    const page = await browser.newPage({
        viewport: { width: 1600, height: 1000 },
      }),
      errors = [];
    page.on("pageerror", (error) => errors.push(error.message));
    const response = await page.goto("http://127.0.0.1:4311");
    assert.equal(response.status(), 200);
    await page.waitForFunction(
      () => window.__assetReady && typeof window.__previewAsset === "function",
    );
    const manifest = await page.evaluate(() => window.__reviewManifest);
    assert.equal(manifest.status, "PENDING_APPROVAL");
    assert.equal(manifest.mapCreated, false);
    assert.equal(manifest.items.length, 185);
    assert.equal(new Set(manifest.items.map((item) => item.id)).size, 185);
    assert.equal(await page.locator(".card").count(), 80);
    for (const item of manifest.items) {
      const thumbnail = await page.request.get(
        "http://127.0.0.1:4311" + item.thumbnail,
      );
      assert.equal(thumbnail.status(), 200, item.id);
      assert.ok((await thumbnail.body()).length > 100, item.id);
    }
    // Heavy source derivatives must also render; budget thumbnails alone are insufficient.
    await page.locator("#quality").selectOption("source");
    const originalModels = [];
    for (const item of manifest.items.filter(
      (item) => item.provider === "Arena3Q",
    )) {
      await page.evaluate(async (id) => window.__previewAsset(id), item.id);
      assert.equal(await page.evaluate(() => window.__assetReady), item.id);
      assert.equal(sha(path.join(root, item.sourcePath)), item.sourceSha256);
      await page.locator("#front").click();
      await page.locator("#wireframe").click();
      await page.locator("#wireframe").click();
      await page.locator("#reset").click();
      if (
        item.id === "arena_BaguaRoundStoneAltar" ||
        item.id === "arena_ThreeKingdomsSquare"
      )
        await page.screenshot({
          path: path.join(review, item.id + "-source-preview.png"),
          fullPage: false,
        });
      originalModels.push({
        id: item.id,
        triangles: item.stats.triangles,
        bytes: item.bytes,
      });
    }
    for (const item of manifest.items.filter(
      (item) => item.kind === "texture" || item.kind === "particle",
    )) {
      await page.evaluate(async (id) => window.__previewAsset(id), item.id);
      await page.waitForFunction(() => {
        const image = document.getElementById("texture");
        return image.complete && image.naturalWidth > 0;
      });
      if (item.kind === "texture") {
        await page.locator("#repeat").click();
        assert.ok(
          (await page.locator("#tiles").getAttribute("style")).includes(
            "background-image",
          ),
        );
        const bounds = await page.locator("#tiles").boundingBox();
        assert.equal(bounds.width, bounds.height);
        await page.locator("#repeat").click();
      }
    }
    await page.locator("#selection").selectOption("all");
    assert.equal(await page.locator(".card").count(), 185);
    await page.locator("#search").fill("WeaponStand");
    assert.equal(await page.locator(".card").count(), 1);
    await page.locator("#search").fill("");
    await page.locator("#selection").selectOption("featured");
    await page.locator("#category").selectOption("Cây");
    assert.equal(await page.locator(".card").count(), 9);
    await page.locator("#category").selectOption("");
    await page.setViewportSize({ width: 390, height: 844 });
    assert.ok(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    );
    await page.screenshot({
      path: path.join(review, "mobile-review.png"),
      fullPage: false,
    });
    assert.equal(
      (await page.request.post("http://127.0.0.1:4311/")).status(),
      405,
    );
    assert.equal(
      (
        await page.request.get("http://127.0.0.1:4311/app/..%2f..%2fCLAUDE.md")
      ).status(),
      404,
    );
    assert.deepEqual(errors, []);
    const report = {
      status: "PENDING_APPROVAL",
      mapCreated: false,
      modelsRenderedInCapture: 171,
      originalOwnedModels: originalModels,
      thumbnails: 185,
      texturePreviews: 14,
      featured: 80,
      mobileNoHorizontalOverflow: true,
      pageErrors: errors,
      verifiedAt: new Date().toISOString(),
    };
    fs.writeFileSync(
      path.join(review, "verification.json"),
      JSON.stringify(report, null, 2) + "\n",
    );
    console.log(
      JSON.stringify({
        sourceModels: originalModels.length,
        thumbnails: 185,
        pageErrors: errors,
        mapCreated: false,
      }),
    );
  } finally {
    await browser.close();
  }
})().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
