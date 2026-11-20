"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { loadingStore, useIsBlocking } from "@/lib/loading/loading-store";
import { Spinner } from "@/components/ui/spinner";
import { useReducedMotion } from "@/lib/hooks/use-reduced-motion";

const showDelayMs = 300;
const minimumVisibleMs = 400;
const cancelDelayMs = 10_000;
const trapKeys = ["Tab", "Enter", " "];

export function LoadingOverlay() {
  const t = useTranslations("loading");
  const blocking = useIsBlocking();
  const reducedMotion = useReducedMotion();
  const [visible, setVisible] = useState(false);
  const [canCancel, setCanCancel] = useState(false);
  const visibleAt = useRef(0);
  const previousFocus = useRef<HTMLElement | null>(null);
  const overlayRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (blocking) {
      if (visible) return;
      const timer = window.setTimeout(() => {
        previousFocus.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
        visibleAt.current = Date.now();
        setCanCancel(false);
        setVisible(true);
      }, showDelayMs);
      return () => window.clearTimeout(timer);
    }

    if (!visible) return;
    const remaining = Math.max(0, minimumVisibleMs - (Date.now() - visibleAt.current));
    const timer = window.setTimeout(() => setVisible(false), remaining);
    return () => window.clearTimeout(timer);
  }, [blocking, visible]);

  useEffect(() => {
    if (!visible) return;

    const content = document.getElementById("app-content");
    const originalOverflow = document.body.style.overflow;
    content?.setAttribute("inert", "");
    document.body.style.overflow = "hidden";
    overlayRef.current?.focus();

    const cancelTimer = window.setTimeout(() => setCanCancel(true), cancelDelayMs);
    const trapKeyboard = (event: KeyboardEvent) => {
      if (trapKeys.includes(event.key)) event.preventDefault();
    };
    document.addEventListener("keydown", trapKeyboard, true);

    return () => {
      window.clearTimeout(cancelTimer);
      document.removeEventListener("keydown", trapKeyboard, true);
      content?.removeAttribute("inert");
      document.body.style.overflow = originalOverflow;
      previousFocus.current?.focus();
      previousFocus.current = null;
    };
  }, [visible]);

  if (!visible) return null;

  return (
    <div
      ref={overlayRef}
      role="status"
      aria-live="polite"
      aria-busy="true"
      tabIndex={-1}
      className="fixed inset-0 z-[var(--z-overlay)] grid place-items-center bg-background/70 p-4 backdrop-blur-sm outline-none"
    >
      <div className="flex min-w-48 flex-col items-center gap-4 rounded-2xl border border-border bg-card p-6 text-center shadow-xl">
        {reducedMotion ? (
          <p className="animate-pulse text-sm font-medium">{t("processing")}</p>
        ) : (
          <>
            <Spinner size="lg" />
            <p className="text-sm font-medium">{t("processing")}</p>
          </>
        )}
        {canCancel && (
          <button
            type="button"
            className="rounded-lg border border-border px-3 py-2 text-sm font-medium focus-visible:outline-2 focus-visible:outline-ring"
            onClick={() => loadingStore.cancelAll()}
          >
            {t("cancel")}
          </button>
        )}
      </div>
    </div>
  );
}
