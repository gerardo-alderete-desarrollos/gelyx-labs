import { z } from "zod";
import { locales } from "@/i18n/routing";

export const sessionUserSchema = z.object({
  id: z.string().uuid(),
  email: z.email(),
  fullName: z.string(),
  roles: z.array(z.string()),
  permissions: z.array(z.string()),
  modules: z.array(
    z.object({
      key: z.string(),
      name: z.string(),
      icon: z.string().nullable(),
      route: z.string().nullable(),
      sortOrder: z.number().int(),
    }),
  ),
  mustChangePassword: z.boolean(),
  locale: z.enum(locales).default("es"),
  theme: z.enum(["light", "dark", "system"]).default("system"),
});

export const authTokensSchema = z.object({
  accessToken: z.string().min(1),
  refreshToken: z.string().min(1),
  expiresIn: z.number().int().positive(),
});

export const twoFactorChallengeSchema = z.object({
  accessToken: z.null(),
  refreshToken: z.null(),
  expiresIn: z.literal(0),
  twoFactorRequired: z.literal(true),
  twoFactorToken: z.string().min(1),
});
