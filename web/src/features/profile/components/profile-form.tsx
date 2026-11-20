"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FieldLabel } from "@/components/ui/field-label";
import { Input } from "@/components/ui/input";
import { profileSchema, type ProfileFormValues } from "@/features/profile/schemas/profile.schema";
import { useSession, sessionQueryKey } from "@/features/auth/hooks/use-session";
import { updateProfile } from "@/features/profile/services/profile.service";
import { notify } from "@/lib/notify";
import { useTranslations } from "next-intl";

export function ProfileForm() {
  const t = useTranslations("profile");
  const validation = useTranslations("auth.validation");
  const apiErrors = useTranslations("errors");
  const { data: session } = useSession();
  const queryClient = useQueryClient();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    values: { fullName: session?.fullName ?? "" },
  });

  const mutation = useMutation({
    mutationFn: updateProfile,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: sessionQueryKey });
      notify.success(t("updated"));
    },
    onError: (error) => {
      const code = error instanceof Error ? error.message : "generic";
      notify.error(apiErrors.has(code) ? apiErrors(code) : apiErrors("generic"));
    },
  });

  return (
    <form onSubmit={handleSubmit((values) => mutation.mutate(values))} className="space-y-5">
      <div className="space-y-2">
        <FieldLabel htmlFor="fullName">{t("fullName")}</FieldLabel>
        <Input id="fullName" autoComplete="name" {...register("fullName")} />
        {errors.fullName && <p className="text-sm text-destructive">{validation(errors.fullName.message ?? "requiredName")}</p>}
      </div>
      <div className="space-y-2">
        <FieldLabel htmlFor="email">{t("email")}</FieldLabel>
        <Input id="email" value={session?.email ?? ""} readOnly aria-readonly="true" className="bg-muted/60" />
        <p className="text-xs text-muted-foreground">{t("emailHelp")}</p>
      </div>
      <Button type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? t("saving") : t("save")}
      </Button>
    </form>
  );
}
