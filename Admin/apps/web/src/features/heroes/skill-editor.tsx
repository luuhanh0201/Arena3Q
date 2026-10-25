"use client";
import { useState } from "react";
import { usePathname } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Pencil, Save, Upload, LoaderCircle, ImageIcon } from "lucide-react";
import {
  allows,
  editableSkill,
  skillEditableSchema,
  skillNumbers,
  effectNames,
  effectUnit,
  passiveNames,
  type Skill,
  type SkillEditable,
  type SkillEditor as EditorState,
} from "@arena3q/contracts";
import { useAdmin } from "@/components/admin-shell";
import { api } from "@/lib/api";
import { assetUrl, formatNumber } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog";
import { ErrorState, Loading } from "@/components/states";
export function SkillUsageBadge({ skill }: { skill: Skill }) {
  const status = skill.usage?.status;
  return (
    <span
      className={
        "badge " +
        (status === "EQUIPPED"
          ? "badge-green"
          : status === "ANIMATION_ONLY"
            ? "badge-amber"
            : "")
      }
      title={skill.usage?.detail}
    >
      {status === "EQUIPPED"
        ? "Đang dùng trong bộ kỹ năng"
        : status === "ANIMATION_ONLY"
          ? "Chỉ gán animation"
          : "Chưa gán trong game"}
    </span>
  );
}
export function SkillImage({ skill }: { skill: Skill }) {
  return (
    <span
      className="relative grid size-12 shrink-0 place-items-center overflow-hidden rounded-lg border border-border bg-secondary"
      title={
        skill.iconAssetId ? "Ảnh kỹ năng nguồn" : "Ảnh kỹ năng đang chờ duyệt"
      }
    >
      {skill.iconAssetId ? (
        <img
          src={assetUrl(skill.iconAssetId, true)}
          alt={skill.name}
          className="absolute inset-0 h-full w-full object-contain"
        />
      ) : (
        <ImageIcon className="size-5 text-muted-foreground" />
      )}
    </span>
  );
}
export function SkillParameterDetails({ skill }: { skill: Skill }) {
  let values: SkillEditable;
  try {
    values = editableSkill(skill.parameters);
  } catch {
    return (
      <p className="text-xs text-amber-300">
        Thông số nguồn chưa đủ để mở trình chỉnh sửa.
      </p>
    );
  }
  return (
    <div className="my-5 space-y-5 rounded-lg border border-border p-4">
      <div className="flex flex-wrap items-center gap-3">
        <SkillUsageBadge skill={skill} />
        {skill.editor?.draft && (
          <span className="badge badge-amber">Có bản nháp chưa áp dụng</span>
        )}
      </div>
      <p className="text-xs leading-6 text-muted-foreground">
        {skill.usage?.detail} Ảnh kỹ năng mới chỉ được gắn sau khi bạn duyệt.
      </p>
      <h3>Chỉ số chiến đấu đầy đủ</h3>
      <dl className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {Object.entries(skillNumbers).map(([key, meta]) => (
          <div key={key}>
            <dt className="data-label">{meta.label}</dt>
            <dd className="data-value ml-0">
              {formatNumber(values[key as keyof typeof skillNumbers])}{" "}
              <span className="text-[10px] text-muted-foreground">
                {meta.unit}
              </span>
            </dd>
          </div>
        ))}
      </dl>
      {values.passives.length > 0 && (
        <div>
          <h3 className="mb-3">Chỉ số nội tại</h3>
          {values.passives.map((value, index) => (
            <div
              key={index}
              className="mb-3 rounded-lg border border-border p-3"
            >
              <strong className="text-xs text-primary">
                {passiveNames[value.kind] ?? value.kind}
              </strong>
              <dl className="mt-3 grid grid-cols-2 gap-3 text-xs sm:grid-cols-3">
                {(
                  [
                    "value",
                    "secondValue",
                    "threshold",
                    "radius",
                    "count",
                  ] as const
                ).map((key) => (
                  <div key={key}>
                    <dt className="text-muted-foreground">
                      {
                        {
                          value: "Giá trị",
                          secondValue: "Giá trị phụ",
                          threshold: "Ngưỡng / thời gian",
                          radius: "Bán kính (m)",
                          count: "Số lượng / tầng",
                        }[key]
                      }
                    </dt>
                    <dd className="ml-0 mt-1">{formatNumber(value[key])}</dd>
                  </div>
                ))}
              </dl>
            </div>
          ))}
        </div>
      )}
      <SkillEditButton skill={skill} />
    </div>
  );
}
interface State {
  skillId: string;
  sourceVersion: string;
  sourceValues: SkillEditable;
  editor: EditorState;
  applyEnabled: boolean;
}
export function SkillEditButton({ skill }: { skill: Skill }) {
  const { user } = useAdmin();
  const [open, setOpen] = useState(false);
  if (
    !allows(user, ["admin.heroes.edit", "admin.skills.edit"]) ||
    !skill.sourceVersion
  )
    return null;
  return (
    <>
      <Button variant="outline" size="sm" onClick={() => setOpen(true)}>
        <Pencil />
        Chỉnh chỉ số và mô tả
      </Button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-w-5xl p-5">
          <DialogTitle>Chỉnh kỹ năng: {skill.name}</DialogTitle>
          <DialogDescription>
            Lưu bản nháp để kiểm tra, sau đó áp dụng vào nguồn Unity local. Trận
            mới và lần build tiếp theo sẽ dùng các chỉ số đã áp dụng.
          </DialogDescription>
          {open && <EditorLoader skill={skill} />}
        </DialogContent>
      </Dialog>
    </>
  );
}
function EditorLoader({ skill }: { skill: Skill }) {
  const heroId = useHeroId();
  const query = useQuery({
    queryKey: ["skill-editor", heroId, skill.id],
    queryFn: () =>
      api<State>(
        `/heroes/${encodeURIComponent(heroId)}/skills/${encodeURIComponent(skill.id)}/editor`,
      ),
  });
  return query.isPending ? (
    <Loading />
  ) : query.error ? (
    <ErrorState error={query.error} retry={() => query.refetch()} />
  ) : query.data ? (
    <EditorForm
      key={
        query.data.data.sourceVersion + ":" + query.data.data.editor.revision
      }
      skill={skill}
      state={query.data.data}
      heroId={heroId}
    />
  ) : null;
}
function useHeroId() {
  const pathname = usePathname();
  return decodeURIComponent(pathname.split("/detail/")[1] ?? "");
}
function EditorForm({
  skill,
  state,
  heroId,
}: {
  skill: Skill;
  state: State;
  heroId: string;
}) {
  const { user } = useAdmin();
  const client = useQueryClient();
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const form = useForm<SkillEditable>({
    resolver: zodResolver(skillEditableSchema),
    defaultValues: state.editor.draft ?? state.sourceValues,
  });
  const url = `/heroes/${encodeURIComponent(heroId)}/skills/${encodeURIComponent(skill.id)}`;
  async function refresh() {
    await client.invalidateQueries({ queryKey: ["hero-section"] });
    await client.invalidateQueries({ queryKey: ["hero"] });
    await client.invalidateQueries({ queryKey: ["heroes"] });
    await client.invalidateQueries({
      queryKey: ["skill-editor", heroId, skill.id],
    });
  }
  async function save(values: SkillEditable) {
    setBusy(true);
    setError("");
    try {
      await api(url + "/draft", {
        method: "PUT",
        body: JSON.stringify({
          sourceVersion: state.sourceVersion,
          revision: state.editor.revision,
          values,
        }),
      });
      setMessage("Đã lưu bản nháp, nguồn Unity chưa thay đổi.");
      form.reset(values);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Không thể lưu bản nháp.");
    } finally {
      setBusy(false);
    }
  }
  async function apply() {
    setBusy(true);
    setError("");
    try {
      await api(url + "/apply-unity", {
        method: "POST",
        body: JSON.stringify({
          sourceVersion: state.sourceVersion,
          revision: state.editor.revision,
        }),
      });
      setMessage(
        "Đã cập nhật nguồn Unity. Trận mới và lần build tiếp theo dùng thông số mới.",
      );
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Không thể áp dụng.");
    } finally {
      setBusy(false);
    }
  }
  const initial = state.editor.draft ?? state.sourceValues;
  return (
    <form noValidate onSubmit={form.handleSubmit(save)} className="space-y-5">
      <div className="grid gap-4">
        <label className="space-y-2 text-xs">
          Tên kỹ năng
          <Input {...form.register("displayName")} />
        </label>
        <label className="space-y-2 text-xs">
          Mô tả kỹ năng
          <textarea
            className="min-h-28 w-full rounded-lg border border-input bg-background p-3 text-sm leading-6"
            {...form.register("description")}
          />
        </label>
      </div>
      <fieldset>
        <legend className="mb-4 font-semibold">Thông số chiến đấu</legend>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Object.entries(skillNumbers).map(([key, meta]) => (
            <label key={key} className="space-y-1 text-xs">
              {meta.label}
              <Input
                aria-label={meta.label}
                type="number"
                step={"integer" in meta ? "1" : "any"}
                min={meta.min}
                {...("max" in meta ? { max: meta.max } : {})}
                {...form.register(key as keyof typeof skillNumbers, {
                  valueAsNumber: true,
                })}
              />
              <span className="text-[10px] text-muted-foreground">
                {meta.unit}
              </span>
            </label>
          ))}
        </div>
      </fieldset>
      <fieldset>
        <legend className="mb-3 font-semibold">Hiệu ứng chiến đấu</legend>
        {initial.effects.map((effect, index) => (
          <div key={index} className="mb-3 rounded-lg border border-border p-3">
            <p className="mb-3 text-xs text-primary">
              {effectNames[effect.kind] ?? effect.kind} ·{" "}
              {["Địch", "Đồng minh", "Bản thân"][effect.affects]}
            </p>
            <div className="grid gap-3 sm:grid-cols-3">
              {(["value", "duration", "minimumTargets"] as const).map((key) => (
                <label key={key} className="space-y-1 text-xs">
                  {
                    {
                      value: "Giá trị hiệu ứng",
                      duration: "Thời gian (s)",
                      minimumTargets: "Số mục tiêu tối thiểu",
                    }[key]
                  }
                  <Input
                    aria-label={`${effectNames[effect.kind]} ${key}`}
                    type="number"
                    min="0"
                    step={key === "minimumTargets" ? "1" : "any"}
                    {...form.register(`effects.${index}.${key}`, {
                      valueAsNumber: true,
                    })}
                  />
                  {key === "value" && (
                    <span className="text-[10px] text-muted-foreground">
                      {effectUnit(effect.kind)}
                    </span>
                  )}
                </label>
              ))}
            </div>
          </div>
        ))}
      </fieldset>
      {initial.passives.length > 0 && (
        <fieldset>
          <legend className="mb-3 font-semibold">Nội tại</legend>
          {initial.passives.map((passive, index) => (
            <div
              key={index}
              className="mb-3 rounded-lg border border-border p-3"
            >
              <p className="mb-3 text-xs text-primary">
                {passiveNames[passive.kind] ?? passive.kind}
              </p>
              <div className="grid gap-3 sm:grid-cols-3">
                {(
                  [
                    "value",
                    "secondValue",
                    "threshold",
                    "radius",
                    "count",
                  ] as const
                ).map((key) => (
                  <label key={key} className="space-y-1 text-xs">
                    {
                      {
                        value: "Giá trị",
                        secondValue: "Giá trị phụ",
                        threshold: "Ngưỡng / thời gian",
                        radius: "Bán kính (m)",
                        count: "Số lượng / tầng",
                      }[key]
                    }
                    <Input
                      type="number"
                      min="0"
                      step={key === "count" ? "1" : "any"}
                      {...form.register(`passives.${index}.${key}`, {
                        valueAsNumber: true,
                      })}
                    />
                  </label>
                ))}
              </div>
            </div>
          ))}
        </fieldset>
      )}
      {Object.keys(form.formState.errors).length > 0 && (
        <p role="alert" className="text-xs text-red-300">
          Có thông số không hợp lệ:{" "}
          {Object.keys(form.formState.errors).join(", ")}. Kiểm tra giá trị âm,
          số nguyên hoặc giới hạn của trường.
        </p>
      )}
      {error && (
        <p role="alert" className="text-sm text-red-300">
          {error}
        </p>
      )}
      {message && (
        <p role="status" className="text-sm text-emerald-300">
          {message}
        </p>
      )}
      {state.editor.draft && !message && (
        <p role="status" className="text-xs text-emerald-300">
          Đã lưu bản nháp. Bấm Áp dụng Unity để cập nhật số liệu trong game.
        </p>
      )}
      <div className="sticky bottom-0 flex flex-wrap gap-3 border-t border-border bg-card py-4">
        <Button type="submit" disabled={busy}>
          {busy ? <LoaderCircle className="animate-spin" /> : <Save />}Lưu bản
          nháp
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={
            busy ||
            form.formState.isDirty ||
            !state.editor.draft ||
            !state.applyEnabled ||
            !allows(user, ["admin.heroes.publish", "admin.skills.edit"])
          }
          onClick={() => void apply()}
        >
          <Upload />
          Áp dụng vào Unity local
        </Button>
        <span className="self-center text-[10px] text-muted-foreground">
          {form.formState.isDirty
            ? "Lưu bản nháp trước khi áp dụng."
            : state.editor.lastAppliedAt
              ? "Đã từng áp dụng vào nguồn Unity."
              : "Có sao lưu nguồn và lịch sử thay đổi."}
        </span>
      </div>
    </form>
  );
}
