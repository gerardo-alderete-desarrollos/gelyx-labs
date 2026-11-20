"use client";

import Link from "next/link";
import { X } from "lucide-react";
import { appName } from "@/lib/env-client";
import { getModuleIcon } from "@/lib/icon-map";
import type { SessionModule } from "@/features/auth/types/session";
import { useTranslations } from "next-intl";

interface SidebarProps {
  modules: SessionModule[];
  isOpen: boolean;
  onClose: () => void;
}

export function Sidebar({ modules, isOpen, onClose }: SidebarProps) {
  const t = useTranslations("layout");
  const orderedModules = [...modules].sort(
    (left, right) => Number(left.sortOrder) - Number(right.sortOrder),
  );

  return (
    <>
      {/* Mobile overlay */}
      {isOpen && (
        <div
          className="fixed inset-0 z-[var(--z-overlay)] bg-black/50 md:hidden"
          onClick={onClose}
          aria-hidden="true"
        />
      )}

      {/* Sidebar - fixed on desktop, drawer on mobile */}
      <aside
        className={`
          fixed inset-y-0 left-0 z-[var(--z-app-header)] w-64 shrink-0 border-r border-border bg-card px-5 py-7
          transform transition-transform duration-300 ease-in-out md:translate-x-0
          ${isOpen ? "translate-x-0" : "-translate-x-full"}
        `}
        aria-label={t("mainNavigation")}
      >
        {/* Mobile close button */}
        <button
          className="md:hidden absolute right-4 top-4 rounded-lg p-2 text-muted-foreground hover:bg-muted"
          onClick={onClose}
          aria-label={t("close")}
        >
          <X className="size-5" />
        </button>

        <Link className="mb-10 flex items-center gap-3 px-2" href="/dashboard" onClick={onClose}>
          <span className="grid size-10 place-items-center rounded-xl bg-primary text-lg font-bold text-primary-foreground">C</span>
          <span>
            <span className="block font-semibold">{appName}</span>
            <span className="text-xs text-muted-foreground">{t("controlCenter")}</span>
          </span>
        </Link>
        <p className="mb-3 px-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground">{t("workspace")}</p>
        <nav aria-label={t("mainNavigation")} className="space-y-1 overflow-y-auto h-[calc(100vh-280px)] md:h-auto pr-2">
          {orderedModules.map((module) => {
            const Icon = getModuleIcon(module.icon);
            const href = module.route ?? `/dashboard/${module.key}`;
            return (
              <Link
                key={module.key}
                href={href}
                onClick={onClose}
                className="flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-muted-foreground transition hover:bg-accent hover:text-accent-foreground"
              >
                <Icon className="size-[18px]" aria-hidden="true" />
                {module.name}
              </Link>
            );
          })}
        </nav>
      </aside>
    </>
  );
}
