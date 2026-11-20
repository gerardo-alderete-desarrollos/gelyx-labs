import { z } from "zod";

export const profileSchema = z.object({
  fullName: z
    .string()
    .trim()
    .min(1, "requiredName")
    .max(200, "nameTooLong"),
});

export type ProfileFormValues = z.infer<typeof profileSchema>;
