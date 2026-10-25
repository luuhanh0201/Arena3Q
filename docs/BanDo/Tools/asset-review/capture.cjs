const fs = require("node:fs"),
  path = require("node:path"),
  { createRequire } = require("node:module");
const root = path.resolve(__dirname, "../../../..");
const { chromium } = createRequire(path.join(root, "Admin/package.json"))(
  "@playwright/test",
);
const review = path.join(
  root,
  "NghiemThu/Battlefield/XemTruoc_Assets_20261025",
);
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
    });
    const errors = [];
    page.on("pageerror", (error) => errors.push(error.message));
    await page.goto("http://127.0.0.1:4311");
    await page.waitForFunction(
      () => typeof window.__previewAsset === "function" && window.__assetReady,
    );
    const manifest = await page.evaluate(() => window.__reviewManifest);
    let captured = 0;
    for (const item of manifest.items.filter((item) => item.kind === "model")) {
      const target = path.join(review, "thumbs", item.id + ".png");
      if (!fs.existsSync(target)) {
        const data = await page.evaluate(
          async (id) => window.__previewAsset(id, { thumbnail: true }),
          item.id,
        );
        fs.writeFileSync(target, Buffer.from(data.split(",")[1], "base64"));
      }
      captured++;
      if (captured % 20 === 0) console.log("MODEL_THUMBNAILS " + captured);
    }
    await page.reload();
    await page.waitForFunction(
      () =>
        window.__assetReady &&
        [...document.querySelectorAll(".card img")]
          .filter((img) => img.getBoundingClientRect().top < innerHeight)
          .every((img) => img.complete && img.naturalWidth > 0),
    );
    await page.screenshot({
      path: path.join(review, "web-overview.png"),
      fullPage: false,
    });
    for (const id of [
      "nature_CommonTree_1",
      "nature_Rock_Medium_1",
      "props_WeaponStand",
      "props_Stall_Cart_Empty",
    ]) {
      await page.evaluate(async (id) => window.__previewAsset(id), id);
      await page.screenshot({
        path: path.join(review, id + ".png"),
        fullPage: false,
      });
    }
    for (const item of manifest.items.filter(
      (item) => item.kind === "texture",
    )) {
      await page.evaluate(async (id) => window.__previewAsset(id), item.id);
      await page.waitForFunction(() => {
        const image = document.getElementById("texture");
        return image.complete && image.naturalWidth > 0;
      });
      await page.locator("#repeat").click();
      await page.screenshot({
        path: path.join(review, item.id + "-tiled.png"),
        fullPage: false,
      });
    }
    if (errors.length) throw Error(JSON.stringify(errors));
    console.log(
      JSON.stringify({
        modelsRendered: captured,
        pageErrors: errors,
        review: "http://127.0.0.1:4311",
      }),
    );
  } finally {
    await browser.close();
  }
})().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
