"use client";

import Link from "next/link";
import { ChevronDown, CircleUserRound } from "lucide-react";
import { useSession } from "@/features/auth/hooks/use-session";
import { ThemeToggle } from "@/components/layout/theme-toggle";
import { LogoutButton } from "@/components/layout/logout-button";
import { LocaleSwitcher } from "@/components/layout/locale-switcher";
import { useTranslations } from "next-intl";

export function UserMenu() {
  const { data: session } = useSession();
  const t = useTranslations("layout");

  return (
    <div className="flex items-center gap-2">
      <ThemeToggle />
      <details className="group relative">
        <summary className="flex cursor-pointer list-none items-center gap-2 rounded-xl border border-border bg-card px-3 py-2 hover:bg-muted">
          <span className="grid size-8 place-items-center rounded-full bg-primary/10 text-primary">
            <CircleUserRound className="size-5" />
          </span>
          <span className="hidden min-w-0 text-left sm:block">
            <span className="block max-w-40 truncate text-sm font-medium">{session?.fullName}</span>
            <span className="block max-w-40 truncate text-xs text-muted-foreground">{session?.email}</span>
          </span>
          <ChevronDown className="size-4 text-muted-foreground transition group-open:rotate-180" />
        </summary>
        <div className="absolute right-0 z-[var(--z-dropdown)] mt-2 w-56 rounded-xl border border-border bg-card p-2 shadow-xl">
          <div className="px-3 py-2"><LocaleSwitcher authenticated /></div>
          <div className="my-2 border-t border-border" />
          {session?.modules.some((module) => module.key === "profile") && (
            <>
              <Link href="/dashboard/profile" className="block rounded-lg px-3 py-2 text-sm hover:bg-muted">{t("myProfile")}</Link>
              <div className="my-2 border-t border-border" />
            </>
          )}
          <LogoutButton />
        </div>
      </details>
    </div>
  );
}
