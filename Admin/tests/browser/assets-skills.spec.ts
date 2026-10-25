import { test, expect } from "@playwright/test";
import { readFile, mkdir } from "node:fs/promises";
import sharp from "sharp";
const evidence = "../NghiemThu/Admin/XemTruoc_AssetsSkills_20261025";
test.beforeEach(async ({ page }) => {
  const credentials = JSON.parse(
    await readFile(".local/bootstrap-credentials.json", "utf8"),
  );
  expect(
    (
      await page.request.post("/api/admin/v1/auth/login", { data: credentials })
    ).ok(),
  ).toBe(true);
  await page.setViewportSize({ width: 1600, height: 1000 });
  await mkdir(evidence, { recursive: true });
});
test("source image fits its frame, retains full source pixels and opens zoom preview", async ({
  page,
}) => {
  await page.goto("/admin/game/heroes/detail/lu_bu?tab=media");
  const card = page.locator("article").first();
  const image = card.locator("img");
  await expect(image).toBeVisible();
  await expect
    .poll(() => image.evaluate((node: HTMLImageElement) => node.naturalWidth))
    .toBeGreaterThan(0);
  const bounds = await image.evaluate((node) => {
    const box = node.getBoundingClientRect();
    const frame = node.parentElement!.getBoundingClientRect();
    return {
      inside:
        box.bottom <= frame.bottom + 0.1 &&
        box.top >= frame.top - 0.1 &&
        box.left >= frame.left - 0.1 &&
        box.right <= frame.right + 0.1,
    };
  });
  expect(bounds.inside).toBe(true);
  const original = await page.request.get((await image.getAttribute("src"))!);
  expect(original.headers()["content-type"]).toContain("image/png");
  const actualPixels = await sharp(await original.body())
    .ensureAlpha()
    .raw()
    .toBuffer();
  const sourcePixels = await sharp(
    "../Assets/Arena3Q/Art/Heroes/LuBu/Portrait.png",
  )
    .ensureAlpha()
    .raw()
    .toBuffer();
  expect(actualPixels.equals(sourcePixels)).toBe(true);
  const thumbnail = await page.request.get(
    (await image.getAttribute("src"))! + "?variant=thumbnail",
  );
  expect(thumbnail.headers()["content-type"]).toContain("image/webp");
  await page.screenshot({
    path: evidence + "/media-after.png",
    fullPage: true,
  });
  await card.getByRole("button").click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.getByLabel("Phóng to", { exact: true }).fill("2");
  await page.screenshot({ path: evidence + "/image-zoom.png", fullPage: true });
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).not.toBeVisible();
});
test("actual Lu Bu and strategist GLBs render and every imported animation can be selected", async ({
  page,
}) => {
  test.setTimeout(180000);
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));
  for (const [hero, count] of [
    ["lu_bu", 7],
    ["lord_strategist", 11],
  ] as const) {
    await page.goto(`/admin/game/heroes/detail/${hero}?tab=model`);
    await expect(page.getByLabel("Clip GLB")).toBeVisible({ timeout: 60000 });
    await expect(page.getByLabel("Clip GLB").locator("option")).toHaveCount(
      count,
    );
    await page
      .getByRole("button", { name: "Tạm dừng animation", exact: true })
      .click();
    const names = await page
      .getByLabel("Clip GLB")
      .locator("option")
      .allTextContents();
    for (const name of names) {
      await page.getByLabel("Clip GLB").selectOption(name);
      await expect(page.getByLabel("Clip GLB")).toHaveValue(name);
      await page.getByLabel("Timeline animation").fill("0");
      const start = await page.locator("canvas").screenshot();
      const duration = Number(
        await page.getByLabel("Timeline animation").getAttribute("max"),
      );
      expect(duration).toBeGreaterThan(0);
      await page
        .getByLabel("Timeline animation")
        .fill(String(Math.round(duration * 50) / 100));
      await expect
        .poll(
          async () =>
            !(await page.locator("canvas").screenshot()).equals(start),
          {
            message:
              hero + "/" + name + " must change rendered pixels when seeking",
          },
        )
        .toBe(true);
    }
    await page.getByLabel("Clip GLB").selectOption("Idle");
    await page.getByLabel("Timeline animation").fill("0");
    await page.getByLabel("Góc nhìn model").selectOption("front");
    await page.screenshot({
      path: evidence + "/" + hero + "-model.png",
      fullPage: true,
    });
  }
  await page.goto("/admin/game/heroes/detail/lu_bu?tab=animations");
  await expect(page.getByLabel("Clip GLB")).toBeVisible({ timeout: 60000 });
  await page.screenshot({
    path: evidence + "/animations-after.png",
    fullPage: true,
  });
  expect(errors).toEqual([]);
});
test("skill usage, detailed numbers, editor validation and descriptions are visible", async ({
  page,
}) => {
  await page.goto("/admin/game/heroes/detail/lord_strategist?tab=skills");
  await page.getByLabel("Lọc kỹ năng đang dùng").selectOption("ANIMATION_ONLY");
  await expect(
    page.locator("main aside button").filter({ hasText: "Chỉ gán animation" }),
  ).toHaveCount(3);
  await page.getByLabel("Lọc kỹ năng đang dùng").selectOption("UNUSED");
  await expect(
    page
      .locator("main aside button")
      .filter({ hasText: "Chưa gán trong game" }),
  ).toHaveCount(3);
  await page.goto(
    "/admin/game/heroes/detail/lu_bu?tab=skills&skillId=lubu_phuong_thien_tram",
  );
  await expect(
    page.getByText("Chỉ số chiến đấu đầy đủ", { exact: true }),
  ).toBeVisible();
  await page.screenshot({
    path: evidence + "/skills-after.png",
    fullPage: true,
  });
  await page.getByRole("button", { name: "Chỉnh chỉ số và mô tả" }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog.getByLabel("Hồi chiêu", { exact: true })).toHaveValue(
    "8",
  );
  await expect(
    dialog.getByLabel("Mô tả kỹ năng", { exact: true }),
  ).not.toHaveValue("");
  await dialog.getByLabel("Hồi chiêu", { exact: true }).fill("-1");
  await dialog
    .getByRole("button", { name: "Lưu bản nháp", exact: true })
    .click();
  await expect(dialog.getByRole("alert")).toBeVisible();
  await dialog.getByLabel("Hồi chiêu", { exact: true }).fill("8");
  await page.screenshot({
    path: evidence + "/skill-editor.png",
    fullPage: true,
  });
  await page.keyboard.press("Escape");
});
