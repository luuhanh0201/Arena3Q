import type { NextConfig } from "next";
import path from "node:path";
const config: NextConfig = {
  output: "standalone",
  outputFileTracingRoot: path.join(process.cwd(), "../.."),
  transpilePackages: ["@arena3q/contracts", "three"],
  async rewrites() {
    return [
      {
        source: "/api/admin/v1/:path*",
        destination: `${process.env.API_INTERNAL_URL ?? "http://127.0.0.1:4000"}/api/admin/v1/:path*`,
      },
    ];
  },
  poweredByHeader: false,
};
export default config;
