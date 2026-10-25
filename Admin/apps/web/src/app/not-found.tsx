import Link from "next/link";
export default function NotFound() {
  return (
    <main className="grid min-h-screen place-items-center p-8">
      <div className="state-card">
        <span className="eyebrow">404</span>
        <h1>Không tìm thấy trang</h1>
        <p>Đường dẫn này không có trong hệ thống Admin.</p>
        <Link className="text-primary" href="/admin/game/heroes">
          Về danh sách tướng
        </Link>
      </div>
    </main>
  );
}
