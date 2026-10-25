import { test, expect } from "@playwright/test";
import { readFile } from "node:fs/promises";
import { heroTabs, tabLabels, flattenMenu } from "@arena3q/contracts";
import { viewerFixture } from "./viewer-fixture";
test("real login, filters, deep links, eleven tabs, placeholders and responsive layout", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));
  await page.setViewportSize({ width: 1600, height: 1000 });
  await page.goto("/admin/game/heroes");
  await expect(
    page.getByRole("heading", { name: "Đăng nhập Admin" }),
  ).toBeVisible();
  await page.screenshot({
    path: "test-results/evidence/login.png",
    fullPage: true,
  });
  const credentials = JSON.parse(
    await readFile(".local/bootstrap-credentials.json", "utf8"),
  );
  await page.getByLabel("Email quản trị").fill(credentials.email);
  await page.getByLabel("Mật khẩu", { exact: true }).fill(credentials.password);
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: /Quản lý tướng/ }),
  ).toBeVisible();
  await expect(page.locator("tbody tr")).toHaveCount(10);
  await page.screenshot({
    path: "test-results/evidence/heroes-initial.png",
    fullPage: true,
  });
  await page.getByLabel("Tìm kiếm tướng").fill("lu bo");
  await page.getByLabel("Tìm kiếm tướng").press("Enter");
  await expect(page.locator("tbody tr")).toHaveCount(1);
  await page.locator("tbody a").filter({ hasText: "Lữ Bố" }).click();
  await expect(
    page.getByRole("tab", { name: tabLabels.overview, exact: true }),
  ).toBeVisible();
  for (const tab of heroTabs) {
    await page.getByRole("tab", { name: tabLabels[tab], exact: true }).click();
    await expect(page).toHaveURL(new RegExp("tab=" + tab));
    await expect(page.getByRole("tabpanel")).toBeVisible();
    await expect(page.getByRole("tabpanel")).not.toContainText(
      "Đang tải dữ liệu",
    );
    if (tab === "skills")
      await page.screenshot({
        path: "test-results/evidence/hero-skills.png",
        fullPage: true,
      });
  }
  await page.reload();
  await expect(
    page.getByRole("tab", { name: tabLabels.versions, exact: true }),
  ).toHaveAttribute("data-state", "active");
  await page
    .getByRole("link", { name: /Danh sách tướng/ })
    .last()
    .click();
  await expect(page).toHaveURL(/q=lu/);
  await expect(page.locator("tbody tr")).toHaveCount(1);
  const feature = flattenMenu().find(
    (item) =>
      !item.children?.length &&
      item.id !== "GAME.HEROES.LIST" &&
      item.id !== "DASHBOARD.DASHBOARD.GAME",
  )!;
  await page.goto(feature.route);
  await expect(
    page.getByText("Đang phát triển", { exact: true }),
  ).toBeVisible();
  await page.goto("/admin/path-does-not-exist");
  await expect(
    page.getByText("Không tìm thấy dữ liệu", { exact: true }),
  ).toBeVisible();
  await page.goto("/admin/game/heroes/detail/not-a-hero?tab=overview");
  await expect(
    page.getByText("Không tìm thấy", { exact: false }).first(),
  ).toBeVisible();
  await page.goto("/admin/game/heroes");
  await expect(page.locator("tbody tr")).toHaveCount(10);
  await page.getByRole("button", { name: "Tạo tướng", exact: true }).click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).not.toBeVisible();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({
    path: "test-results/evidence/heroes-mobile.png",
    fullPage: true,
  });
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBe(true);
  expect(errors).toEqual([]);
});

test("all 91 leaf routes render their implemented page or an honest planned state", async ({
  page,
}) => {
  test.setTimeout(240000);
  const credentials = JSON.parse(
    await readFile(".local/bootstrap-credentials.json", "utf8"),
  );
  expect(
    (
      await page.request.post("/api/admin/v1/auth/login", { data: credentials })
    ).ok(),
  ).toBe(true);
  for (const feature of flattenMenu().filter(
    (item) => !item.children?.length,
  )) {
    await page.goto(feature.route);
    if (feature.id === "GAME.HEROES.LIST")
      await expect(
        page.getByRole("heading", { name: /Quản lý tướng/ }),
      ).toBeVisible();
    else if (feature.id === "DASHBOARD.DASHBOARD.GAME")
      await expect(
        page.getByRole("heading", { name: "Tổng quan nội dung game" }),
      ).toBeVisible();
    else {
      await expect(
        page
          .locator("main")
          .getByRole("heading", { name: feature.label, exact: true }),
      ).toBeVisible();
      await expect(
        page.locator("main").getByText("Đang phát triển", { exact: true }),
      ).toBeVisible();
    }
  }
  await page.goto("/admin/dashboard/game");
  await expect(
    page.getByText("Tổng quan nội dung game", { exact: true }),
  ).toBeVisible();
  await page.screenshot({
    path: "test-results/evidence/overview.png",
    fullPage: true,
  });
});

test("3D viewer renders a genuine GLB test fixture and animation controls", async ({
  page,
}) => {
  const credentials = JSON.parse(
    await readFile(".local/bootstrap-credentials.json", "utf8"),
  );
  expect(
    (
      await page.request.post("/api/admin/v1/auth/login", { data: credentials })
    ).ok(),
  ).toBe(true);
  const payload = (data: unknown) => ({
    data,
    meta: { isDemo: false },
    error: null,
  });
  await page.route("**/api/admin/v1/heroes/lord_strategist/model", (route) =>
    route.fulfill({
      json: payload({
        assetId: "viewer-test-fixture",
        previewAssetId: "viewer-test-fixture",
        metadata: { kind: "BROWSER_TEST_FIXTURE" },
        sourcePath: "tests/browser/viewer-fixture.ts",
      }),
    }),
  );
  await page.route("**/api/admin/v1/assets/viewer-test-fixture", (route) =>
    route.fulfill({
      json: payload({
        id: "viewer-test-fixture",
        name: "Test fixture only.glb",
        type: "MODEL",
        category: "TEST",
        format: "glb",
        bytes: viewerFixture().length,
        width: null,
        height: null,
        checksum: null,
        sourcePath: "tests/browser/viewer-fixture.ts",
        guid: null,
        fileId: null,
        version: null,
        updatedAt: null,
        storageKey: null,
        thumbnailKey: null,
        contentType: "model/gltf-binary",
        available: true,
        browserCompatible: true,
        metadata: {},
      }),
    }),
  );
  await page.route(
    "**/api/admin/v1/assets/viewer-test-fixture/content",
    (route) =>
      route.fulfill({
        body: viewerFixture(),
        contentType: "model/gltf-binary",
      }),
  );
  await page.goto("/admin/game/heroes/detail/lord_strategist?tab=model");
  await expect(page.getByLabel("Clip GLB")).toHaveValue("FixtureMove", {
    timeout: 30000,
  });
  await expect(page.getByText("1 mesh", { exact: true })).toBeVisible();
  await page
    .getByRole("button", { name: "Tạm dừng animation", exact: true })
    .click();
  await page.getByLabel("Tốc độ animation").selectOption("0.5");
  await page.getByLabel("Góc nhìn model").selectOption("front");
  await page.getByLabel("Shading model").selectOption("wireframe");
  await page.getByLabel("Timeline animation").fill("0.5");
  await expect(page.getByLabel("Timeline animation")).toHaveValue("0.5");
  const downloadPromise = page.waitForEvent("download");
  await page.getByTitle("Chụp preview", { exact: true }).click();
  const download = await downloadPromise;
  expect(download.suggestedFilename()).toBe("arena3q-model-preview.png");
  await page.screenshot({
    path: "test-results/evidence/viewer-test-fixture.png",
    fullPage: true,
  });
});
