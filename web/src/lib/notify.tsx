"use client";

import { useEffect, useSyncExternalStore } from "react";
import { useTheme } from "next-themes";
import { useTranslations } from "next-intl";
import { toast, Toaster } from "sonner";

type ProblemDetails = {
  code?: unknown;
  status?: unknown;
};

type Translate = (key: string) => string;
type HasTranslation = (key: string) => boolean;

const identity: Translate = (key) => key;
let translate: Translate = identity;
let hasTranslation: HasTranslation = () => false;

function problemDetails(error: unknown): ProblemDetails | null {
  return typeof error === "object" && error !== null ? error as ProblemDetails : null;
}

function errorCode(error: unknown): string {
  const details = problemDetails(error);
  if (typeof details?.code === "string") return details.code;
  if (error instanceof Error) return error.message;
  return "generic";
}

function errorStatus(error: unknown): number | undefined {
  const status = problemDetails(error)?.status;
  return typeof status === "number" ? status : undefined;
}

function translatedError(error: unknown): string | null {
  if (errorStatus(error) === 422) return null;
  const key = `errors.${errorCode(error)}`;
  return hasTranslation(key) ? translate(key) : translate("errors.generic");
}

function setNotifyTranslator(nextTranslate: Translate, nextHasTranslation: HasTranslation): void {
  translate = nextTranslate;
  hasTranslation = nextHasTranslation;
}

export { setNotifyTranslator };
export const notify = {
  success(key: string) {
    toast.success(hasTranslation(key) ? translate(key) : key);
  },
  error(error: unknown) {
    const message = typeof error === "string" ? error : translatedError(error);
    if (message) toast.error(message);
  },
  promise<T>(
    promise: Promise<T> | (() => Promise<T>),
    messages: Parameters<typeof toast.promise>[1],
  ) {
    return toast.promise(promise, messages);
  },
};

export function NotifyToaster() {
  const t = useTranslations();
  const { resolvedTheme } = useTheme();

  return (
    <div className="z-[var(--z-toaster)]" aria-live="polite" aria-atomic="true">
      <Toaster
        richColors
        position="top-right"
        theme={resolvedTheme === "dark" ? "dark" : "light"}
        containerAriaLabel={t("notifications.ariaLabel")}
      />
    </div>
  );
}
