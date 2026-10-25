import { z } from "zod";
export const skillNumbers = {
  cooldown: { label: "Hồi chiêu", unit: "s", min: 0 },
  castTime: { label: "Vận chiêu", unit: "s", min: 0 },
  range: { label: "Tầm chọn mục tiêu", unit: "m; 0 = tầm đánh", min: 0 },
  moveDistance: { label: "Khoảng di chuyển", unit: "m", min: 0 },
  radius: { label: "Bán kính", unit: "m", min: 0 },
  length: { label: "Chiều dài", unit: "m", min: 0 },
  width: { label: "Chiều rộng", unit: "m", min: 0 },
  angle: { label: "Góc quạt", unit: "độ", min: 1, max: 360 },
  castleMultiplier: { label: "Hệ số sát thương thành", unit: "×", min: 0 },
  executeThreshold: {
    label: "Ngưỡng kết liễu",
    unit: "tỷ lệ máu 0–1",
    min: 0,
    max: 1,
  },
  executeBonus: {
    label: "Sát thương thêm khi kết liễu",
    unit: "tỷ lệ",
    min: 0,
  },
  zoneDuration: { label: "Thời gian vùng tồn tại", unit: "s", min: 0 },
  tickInterval: { label: "Khoảng tick", unit: "s", min: 0.05 },
  zoneSway: { label: "Dao động hình ảnh vùng", unit: "m", min: 0 },
  repeatCount: { label: "Số lần đánh", unit: "lần", min: 1, integer: true },
  repeatInterval: { label: "Khoảng lặp", unit: "s", min: 0 },
  finalHitMultiplier: { label: "Hệ số đòn cuối", unit: "×", min: 0 },
} as const;
const numberShape = Object.fromEntries(
  Object.entries(skillNumbers).map(([key, meta]) => {
    let value = z
      .number()
      .finite()
      .min(meta.min)
      .max("max" in meta ? meta.max : 1000000);
    if ("integer" in meta) value = value.int();
    return [key, value];
  }),
) as Record<keyof typeof skillNumbers, z.ZodNumber>;
export const skillEditableSchema = z.object({
  displayName: z.string().trim().min(1).max(180),
  description: z.string().max(8000),
  ...numberShape,
  effects: z
    .array(
      z.object({
        kind: z.number().int().nonnegative(),
        affects: z.number().int().min(0).max(2),
        value: z.number().finite().min(0).max(1000000),
        duration: z.number().finite().min(0).max(86400),
        minimumTargets: z.number().int().min(0).max(10000),
      }),
    )
    .max(64),
  passives: z
    .array(
      z.object({
        kind: z.number().int().nonnegative(),
        value: z.number().finite().min(0).max(1000000),
        secondValue: z.number().finite().min(0).max(1000000),
        threshold: z.number().finite().min(0).max(1000000),
        radius: z.number().finite().min(0).max(1000000),
        count: z.number().int().min(0).max(10000),
      }),
    )
    .max(64),
});
export type SkillEditable = z.infer<typeof skillEditableSchema>;
export interface SkillEditor {
  revision: number;
  draft: SkillEditable | null;
  updatedAt: string | null;
  updatedBy: string | null;
  lastAppliedAt: string | null;
  baseVersion?: string | null;
}
export interface SkillUsage {
  status: "EQUIPPED" | "ANIMATION_ONLY" | "UNUSED";
  heroSlots: string[];
  detail: string;
}
export const skillDraftRequestSchema = z.object({
  sourceVersion: z.string().regex(/^[a-f0-9]{64}$/),
  revision: z.number().int().nonnegative(),
  values: skillEditableSchema,
});
export const skillApplyRequestSchema = z.object({
  sourceVersion: z.string().regex(/^[a-f0-9]{64}$/),
  revision: z.number().int().nonnegative(),
});
export function editableSkill(
  parameters: Record<string, unknown>,
): SkillEditable {
  const fallback = {
    cooldown: 10,
    castTime: 0.3,
    range: 0,
    moveDistance: 0,
    radius: 1.5,
    length: 5,
    width: 1,
    angle: 60,
    castleMultiplier: 1,
    executeThreshold: 0,
    executeBonus: 0,
    zoneDuration: 0,
    tickInterval: 0.5,
    zoneSway: 0,
    repeatCount: 1,
    repeatInterval: 0.25,
    finalHitMultiplier: 1,
  };
  return skillEditableSchema.parse({
    ...fallback,
    ...parameters,
    displayName: parameters.displayName ?? "Kỹ năng",
    description: parameters.description ?? "",
    effects: ((parameters.effects as unknown[]) ?? []).map((value: any) => ({
      minimumTargets: 0,
      ...value,
    })),
    passives: ((parameters.passives as unknown[]) ?? []).map((value: any) => ({
      value: 0,
      secondValue: 0,
      threshold: 0,
      radius: 0,
      count: 0,
      ...value,
    })),
  });
}
export const effectNames = [
  "Sát thương",
  "Choáng",
  "Làm chậm",
  "Suy yếu",
  "Giảm tốc đánh",
  "Độc",
  "Hất lùi",
  "Khiên",
  "Tăng tốc đánh",
  "Tăng tốc chạy",
  "Tăng Công",
  "Giảm sát thương nhận",
  "Miễn khống chế",
  "Bổ sung lính",
  "Hồi máu",
  "Kéo",
  "Khiêu khích",
  "Hất tung",
  "Tầng Cuồng Huyết",
  "Tăng nộ",
];
export const passiveNames = [
  "Không có",
  "Tăng tầm đánh",
  "Đánh xuyên mỗi N đòn",
  "Hút máu",
  "Tốc đánh khi thấp máu",
  "Hào quang Công lính",
  "Thêm lính",
  "Máu tối đa toàn quân",
  "Sát thương mục tiêu thấp máu",
  "Giảm sát thương",
  "Tầng liên kích",
  "Đòn đánh sau chạy",
  "Cuồng Huyết",
];
export function effectUnit(kind: number): string {
  return kind === 0
    ? "× Công"
    : kind === 5
      ? "× Công mỗi giây"
      : kind === 7
        ? "× máu tối đa người dùng"
        : kind === 14
          ? "× máu tối đa mục tiêu"
          : [3, 2, 4, 8, 9, 10, 11, 19].includes(kind)
            ? "tỷ lệ (0,3 = 30%)"
            : [6, 15].includes(kind)
              ? "m"
              : [13, 18].includes(kind)
                ? "lính / tầng"
                : "giá trị nguồn";
}
