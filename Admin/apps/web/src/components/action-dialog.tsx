"use client";
import { createContext, useContext, useState, type ReactNode } from "react";
import { Construction } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "./ui/dialog";
import { Button } from "./ui/button";
const Context = createContext<(name: string) => void>(() => {});
export const usePlannedAction = () => useContext(Context);
export function PlannedActionProvider({ children }: { children: ReactNode }) {
  const [name, setName] = useState<string | null>(null);
  return (
    <Context.Provider value={setName}>
      {children}
      <Dialog
        open={!!name}
        onOpenChange={(open) => {
          if (!open) setName(null);
        }}
      >
        <DialogContent>
          <div className="state-icon mx-0">
            <Construction />
          </div>
          <DialogTitle className="text-xl font-semibold">{name}</DialogTitle>
          <DialogDescription className="text-sm leading-6 text-muted-foreground">
            Chức năng đang phát triển. Bản Admin hiện tại hỗ trợ xem dữ liệu;
            thao tác này chưa được thực hiện.
          </DialogDescription>
          <Button variant="outline" onClick={() => setName(null)}>
            Đã hiểu
          </Button>
        </DialogContent>
      </Dialog>
    </Context.Provider>
  );
}
