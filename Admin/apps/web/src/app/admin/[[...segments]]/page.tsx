import { Suspense } from "react";
import { AdminShell } from "@/components/admin-shell";
import { AdminWorkspace } from "@/features/workspace";
export const dynamic = "force-dynamic";
export default function AdminPage() {
  return (
    <Suspense>
      <AdminShell>
        <AdminWorkspace />
      </AdminShell>
    </Suspense>
  );
}
