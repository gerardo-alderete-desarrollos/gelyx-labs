import "server-only";

import { z } from "zod";

const serverEnvSchema = z.object({
  API_BASE_URL: z.url(),
});

export const serverEnv = serverEnvSchema.parse({
  API_BASE_URL: process.env.API_BASE_URL ?? "http://localhost:5000/api/v1",
});

export const appName = z
  .string()
  .trim()
  .min(1)
  .parse(process.env.NEXT_PUBLIC_APP_NAME ?? "GelyxLabs");