"use client";

import { useState } from "react";
import Link from "next/link";
import { Menu, X } from "lucide-react";
import { UserMenu } from "@/components/layout/user-menu";
import { Sidebar } from "@/components/layout/sidebar";
import type { SessionUser } from "@/features/auth/types/session";
import { getModuleIcon } from "@/lib/icon-map";
import { useTranslations } from "next-intl";

export function AppHeader({ session }: Readonly<{ session: SessionUser }>) {
  const t = useTranslations("layout");
  const moduleNames = useTranslations("layout.module");
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <>
      <header className="sticky top-0 z-[var(--z-app-header)] flex h-[76px] items-center justify-between border-b border-border bg-background/90 px-5 backdrop-blur md:px-8">
        <div className="flex items-center gap-3">
          {/* Hamburger button - mobile only */}
          <button
            className="md:hidden rounded-lg p-2 text-muted-foreground hover:bg-muted"
            onClick={() => setSidebarOpen(true)}
            aria-label={t("openMenu")}
            aria-expanded={sidebarOpen}
            aria-controls="sidebar"
          >
            {sidebarOpen ? <X className="size-5" /> : <Menu className="size-5" />}
          </button>
          <div>
            <p className="text-xs text-muted-foreground">GelyxLabs /</p>
            <p className="font-medium">{t("controlPanel")}</p>
          </div>
        </div>
        <UserMenu />
      </header>
      <Sidebar modules={session.modules} isOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} />
    </>
  );
}
