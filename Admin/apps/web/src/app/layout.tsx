import type { Metadata } from "next";
import localFont from "next/font/local";
import { Providers } from "@/components/providers";
import "./globals.css";
const vietnam = localFont({
  src: [
    { path: "../../public/fonts/BeVietnamPro-Regular.ttf", weight: "400" },
    { path: "../../public/fonts/BeVietnamPro-SemiBold.ttf", weight: "600" },
  ],
  variable: "--font-vietnam",
  display: "swap",
});
export const metadata: Metadata = {
  title: { default: "Arena3Q Admin", template: "%s | Arena3Q Admin" },
  description: "Trung tâm quản trị nội dung và vận hành Arena3Q",
  robots: { index: false, follow: false },
};
export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="vi" className={vietnam.variable}>
      <body>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
