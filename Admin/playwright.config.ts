import { defineConfig } from "@playwright/test";
export default defineConfig({
  testDir: "./tests/browser",
  fullyParallel: false,
  workers: 1,
  timeout: 60000,
  use: {
    baseURL: process.env.ADMIN_TEST_URL ?? "http://127.0.0.1:3000",
    browserName: "chromium",
    launchOptions: {
      executablePath:
        process.env.EDGE_PATH ??
        "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe",
    },
    trace: "retain-on-failure",
  },
  reporter: [["list"], ["html", { open: "never" }]],
});
