import { z } from "zod";
/** Contract reserved for the future Unity build pipeline; not a published catalog or game integration. */
export const addressablesReleaseSchema = z.object({
  releaseId: z.string().min(1),
  platform: z.enum(["Android", "iOS", "StandaloneWindows64"]),
  unityVersion: z.string().min(1),
  addressablesVersion: z.string().min(1),
  catalogUrl: z.url(),
  catalogHash: z.string().min(1),
  builtAt: z.iso.datetime(),
  entries: z.array(
    z.object({
      address: z.string().min(1),
      heroId: z.string().nullable(),
      bundleUrl: z.url(),
      sha256: z.string().regex(/^[a-f0-9]{64}$/),
      bytes: z.number().int().nonnegative(),
      labels: z.array(z.string()),
    }),
  ),
});
export type AddressablesRelease = z.infer<typeof addressablesReleaseSchema>;
