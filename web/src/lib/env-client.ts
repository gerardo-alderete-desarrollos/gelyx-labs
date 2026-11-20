import { z } from "zod";

export const appName = z
  .string()
  .trim()
  .min(1)
  .parse(process.env.NEXT_PUBLIC_APP_NAME ?? "GelyxLabs");