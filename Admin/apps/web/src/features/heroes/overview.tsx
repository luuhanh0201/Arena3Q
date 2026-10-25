"use client";
import { useQuery } from "@tanstack/react-query";
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import {
  ArrowUpRight,
  Box,
  Database,
  Film,
  Sparkles,
  UsersRound,
} from "lucide-react";
import Link from "next/link";
import type { Coverage } from "@arena3q/contracts";
import { api } from "@/lib/api";
import { formatDate } from "@/lib/utils";
import { EmptyState, ErrorState, Loading } from "@/components/states";
import { Button } from "@/components/ui/button";
interface Overview {
  heroes: number;
  assets: number;
  animations: number;
  skills: number;
  coverage: ({ id: string; name: string } & Coverage)[];
  infrastructure: {
    database: string;
    redis: boolean;
    storage: string;
    addressables: string;
  };
}
export function GameOverview() {
  const query = useQuery({
    queryKey: ["overview"],
    queryFn: () => api<Overview>("/overview"),
  });
  if (query.isPending) return <Loading />;
  if (query.error)
    return <ErrorState error={query.error} retry={() => query.refetch()} />;
  if (!query.data) return null;
  const data = query.data.data;
  const chart = [
    {
      name: "Ảnh",
      count: data.coverage.filter((hero) => hero.portrait).length,
    },
    { name: "Model", count: data.coverage.filter((hero) => hero.model).length },
    { name: "Rig 3D", count: data.coverage.filter((hero) => hero.rig).length },
    {
      name: "Animation",
      count: data.coverage.filter((hero) => hero.animations > 0).length,
    },
    {
      name: "Kỹ năng",
      count: data.coverage.filter((hero) => hero.skills > 0).length,
    },
    {
      name: "Chỉ số",
      count: data.coverage.filter((hero) => hero.stats).length,
    },
  ];
  return (
    <>
      <div className="page-heading">
        <div>
          <span className="eyebrow">TỔNG QUAN / GAME</span>
          <h1 className="mt-2">Tổng quan nội dung game</h1>
          <p>
            Số liệu từ snapshot Unity đã nhập lúc{" "}
            {formatDate(query.data.meta.source?.importedAt)}.
          </p>
        </div>
        <Button asChild>
          <Link href="/admin/game/heroes">
            Quản lý tướng
            <ArrowUpRight />
          </Link>
        </Button>
      </div>
      <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {[
          { label: "Định nghĩa tướng", value: data.heroes, Icon: UsersRound },
          { label: "Tham chiếu tài nguyên", value: data.assets, Icon: Box },
          { label: "Binding animation", value: data.animations, Icon: Film },
          { label: "Kỹ năng và binding", value: data.skills, Icon: Sparkles },
        ].map(({ label, value, Icon }) => (
          <div className="panel p-5" key={label}>
            <div className="flex justify-between text-xs text-muted-foreground">
              <span>{label}</span>
              <Icon className="size-4 text-primary" />
            </div>
            <strong className="mt-4 block text-3xl font-semibold">
              {value}
            </strong>
            <span className="mt-3 block text-[10px] text-muted-foreground">
              Dữ liệu nguồn, không phải telemetry người chơi
            </span>
          </div>
        ))}
      </div>
      <section className="panel p-6">
        <h2>Độ phủ dữ liệu tướng</h2>
        <p className="mb-7 mt-2 text-xs text-muted-foreground">
          Số tướng có từng nhóm dữ liệu. Không suy luận chất lượng QA hoặc phần
          trăm hoàn thiện từ số lượng file.
        </p>
        {data.heroes ? (
          <div className="h-72 min-w-0">
            <ResponsiveContainer width="100%" height="100%" minWidth={0}>
              <BarChart
                data={chart}
                margin={{ top: 10, right: 20, bottom: 0, left: -20 }}
              >
                <CartesianGrid stroke="#263449" vertical={false} />
                <XAxis
                  dataKey="name"
                  stroke="#91a0b6"
                  tickLine={false}
                  axisLine={false}
                  fontSize={11}
                />
                <YAxis
                  allowDecimals={false}
                  stroke="#91a0b6"
                  tickLine={false}
                  axisLine={false}
                  fontSize={11}
                />
                <Tooltip
                  contentStyle={{
                    background: "#162132",
                    border: "1px solid #34445b",
                    borderRadius: 8,
                    fontSize: 12,
                  }}
                  cursor={{ fill: "#263449" }}
                />
                <Bar
                  isAnimationActive={false}
                  dataKey="count"
                  name="Số tướng"
                  fill="#c9a46d"
                  radius={[5, 5, 0, 0]}
                  maxBarSize={55}
                />
              </BarChart>
            </ResponsiveContainer>
          </div>
        ) : (
          <EmptyState title="Chưa có dữ liệu để tổng hợp" />
        )}
      </section>
      <div className="mt-5 grid gap-5 lg:grid-cols-2">
        <section className="panel p-5">
          <h2>Nguồn dữ liệu</h2>
          <dl className="data-grid mt-5">
            <div>
              <dt className="data-label">Snapshot</dt>
              <dd className="data-value ml-0">
                {query.data.meta.source?.kind ?? "EMPTY"}
              </dd>
            </div>
            <div>
              <dt className="data-label">Git commit nguồn</dt>
              <dd className="data-value ml-0">
                <code className="text-xs">
                  {query.data.meta.source?.commit?.slice(0, 12) ?? "Chưa có"}
                </code>
              </dd>
            </div>
            <div>
              <dt className="data-label">Cấu hình game đã phát hành</dt>
              <dd className="data-value ml-0">Chưa kết nối</dd>
            </div>
            <div>
              <dt className="data-label">Telemetry / người chơi online</dt>
              <dd className="data-value ml-0">Chưa kết nối</dd>
            </div>
          </dl>
        </section>
        <section className="panel p-5">
          <h2>Hạ tầng cấu hình</h2>
          <dl className="data-grid mt-5">
            <div>
              <dt className="data-label">Lưu dữ liệu</dt>
              <dd className="data-value ml-0">
                {data.infrastructure.database === "json"
                  ? "Local JSON"
                  : "PostgreSQL"}
              </dd>
            </div>
            <div>
              <dt className="data-label">File tài nguyên</dt>
              <dd className="data-value ml-0">
                {data.infrastructure.storage === "local"
                  ? "Local private storage"
                  : "S3-compatible"}
              </dd>
            </div>
            <div>
              <dt className="data-label">Redis / hàng đợi</dt>
              <dd className="data-value ml-0">
                {data.infrastructure.redis
                  ? "Đã khai báo kết nối"
                  : "Chưa cấu hình"}
              </dd>
            </div>
            <div>
              <dt className="data-label">Addressables / CDN</dt>
              <dd className="data-value ml-0">Chưa kết nối</dd>
            </div>
          </dl>
        </section>
      </div>
    </>
  );
}
