"use client";
import { Suspense, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter, useSearchParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { loginSchema } from "@arena3q/contracts";
import {
  ArrowRight,
  Eye,
  EyeOff,
  LoaderCircle,
  LockKeyhole,
  ShieldCheck,
} from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { api } from "@/lib/api";
function Login() {
  const router = useRouter();
  const search = useSearchParams();
  const client = useQueryClient();
  const [error, setError] = useState("");
  const [visible, setVisible] = useState(false);
  const form = useForm<{ email: string; password: string }>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });
  async function submit(input: { email: string; password: string }) {
    setError("");
    try {
      await api("/auth/login", { method: "POST", body: JSON.stringify(input) });
      client.clear();
      let destination = "/admin/game/heroes";
      const target = search.get("returnTo");
      if (target) {
        const url = new URL(target, window.location.origin);
        if (
          url.origin === window.location.origin &&
          (url.pathname === "/admin" || url.pathname.startsWith("/admin/"))
        )
          destination = url.pathname + url.search;
      }
      router.replace(destination);
    } catch (error) {
      setError(error instanceof Error ? error.message : "Không thể đăng nhập.");
    }
  }
  return (
    <main className="grid min-h-screen lg:grid-cols-[1.1fr_1fr]">
      <section className="login-art relative hidden flex-col justify-between overflow-hidden border-r border-border p-12 lg:flex">
        <div className="flex items-center gap-3">
          <div className="grid size-9 place-items-center rounded-lg border border-primary/40 text-primary">
            <ShieldCheck className="size-5" />
          </div>
          <span className="font-semibold tracking-wide">
            ARENA3Q <span className="text-muted-foreground">/ ADMIN</span>
          </span>
        </div>
        <div className="relative z-10 max-w-lg">
          <img
            src="/arena3q-logo.png"
            width="420"
            height="141"
            alt="Arena3Q"
            className="mb-10 w-[380px] object-contain"
          />
          <span className="eyebrow text-primary/70">TRUNG TÂM QUẢN TRỊ</span>
          <h1 className="mt-5 text-4xl leading-snug">
            Một nơi quản lý
            <br />
            toàn bộ thế giới Arena3Q.
          </h1>
          <p className="mt-6 max-w-md text-sm leading-7 text-muted-foreground">
            Theo dõi dữ liệu tướng, kiểm tra tài nguyên và chuẩn bị nội dung cho
            những trận chiến tiếp theo.
          </p>
        </div>
        <p className="text-xs text-muted-foreground">
          Arena3Q · Công cụ nội bộ dành cho quản trị viên
        </p>
        <div className="pointer-events-none absolute -bottom-44 -right-40 size-[600px] rounded-full border border-primary/10" />
        <div className="pointer-events-none absolute -bottom-28 -right-24 size-[450px] rounded-full border border-primary/10" />
      </section>
      <section className="flex items-center justify-center p-7">
        <div className="w-full max-w-[390px]">
          <div className="mb-6 grid size-12 place-items-center rounded-xl border border-primary/30 bg-primary/5 text-primary">
            <LockKeyhole className="size-5" />
          </div>
          <span className="eyebrow">CHÀO MỪNG TRỞ LẠI</span>
          <h1 className="mt-3">Đăng nhập Admin</h1>
          <p className="mb-8 mt-3 text-sm leading-6 text-muted-foreground">
            Sử dụng tài khoản quản trị được cấp để tiếp tục.
          </p>
          <form onSubmit={form.handleSubmit(submit)} className="space-y-5">
            <div>
              <label className="mb-2 block text-sm" htmlFor="email">
                Email quản trị
              </label>
              <Input
                id="email"
                type="email"
                autoComplete="username"
                placeholder="Email của bạn"
                {...form.register("email")}
              />
              {form.formState.errors.email && (
                <p className="mt-2 text-xs text-red-400">Nhập email hợp lệ.</p>
              )}
            </div>
            <div>
              <label className="mb-2 block text-sm" htmlFor="password">
                Mật khẩu
              </label>
              <div className="relative">
                <Input
                  id="password"
                  type={visible ? "text" : "password"}
                  autoComplete="current-password"
                  placeholder="Nhập mật khẩu"
                  className="pr-12"
                  {...form.register("password")}
                />
                <button
                  type="button"
                  aria-label={visible ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                  onClick={() => setVisible(!visible)}
                  className="absolute right-3 top-3 text-muted-foreground"
                >
                  {visible ? (
                    <EyeOff className="size-4" />
                  ) : (
                    <Eye className="size-4" />
                  )}
                </button>
              </div>
              {form.formState.errors.password && (
                <p className="mt-2 text-xs text-red-400">Nhập mật khẩu.</p>
              )}
            </div>
            {error && (
              <p
                role="alert"
                className="rounded-lg border border-red-400/25 bg-red-400/5 p-3 text-sm text-red-300"
              >
                {error}
              </p>
            )}
            <Button
              type="submit"
              disabled={form.formState.isSubmitting}
              className="mt-2 w-full"
            >
              {form.formState.isSubmitting ? (
                <LoaderCircle className="animate-spin" />
              ) : null}
              Đăng nhập
              <ArrowRight className="ml-auto" />
            </Button>
          </form>
          <div className="mt-8 flex items-start gap-3 rounded-lg border border-border p-4 text-xs leading-6 text-muted-foreground">
            <ShieldCheck className="mt-1 size-4 shrink-0" />
            Quyền truy cập được kiểm tra theo tài khoản. Liên hệ người quản trị
            hệ thống nếu bạn chưa được cấp tài khoản.
          </div>
        </div>
      </section>
    </main>
  );
}
export default function LoginPage() {
  return (
    <Suspense>
      <Login />
    </Suspense>
  );
}
