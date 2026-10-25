import { redirect } from "next/navigation";
export default function Home() {
  redirect("/admin/game/heroes");
}
