import type { DatabaseState } from "@arena3q/contracts";
export interface AdminStore {
  read(): Promise<DatabaseState>;
  update<T>(change: (state: DatabaseState) => T | Promise<T>): Promise<T>;
  close?(): Promise<void>;
}
export const ADMIN_STORE = Symbol("ADMIN_STORE");
export function protectAudit(
  before: DatabaseState,
  after: DatabaseState,
): void {
  if (
    after.audit.length < before.audit.length ||
    before.audit.some(
      (entry, index) =>
        JSON.stringify(entry) !== JSON.stringify(after.audit[index]),
    )
  )
    throw new Error("Audit log is append-only.");
}
