import { z } from "zod";

export const loginSchema = z.object({
  email: z.email("invalidEmail"),
  password: z.string().min(1, "requiredPassword"),
  rememberMe: z.boolean().optional(),
});

export type LoginFormValues = z.infer<typeof loginSchema>;

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, "requiredCurrentPassword"),
    newPassword: z
      .string()
      .min(8, "passwordTooShort")
      .max(128, "passwordTooLong"),
    confirmPassword: z.string().min(1, "requiredConfirmPassword"),
  })
  .refine((value) => value.newPassword === value.confirmPassword, {
    path: ["confirmPassword"],
    message: "passwordsMismatch",
  });

export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;

export const forgotPasswordSchema = z.object({
  email: z.email("invalidEmail"),
});

export const resetPasswordSchema = z.object({
  email: z.email("invalidEmail"),
  token: z.string().min(1, "invalidResetLink"),
  newPassword: z.string().min(8, "passwordTooShort"),
});
