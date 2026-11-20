"use client";

import Link from "next/link";
import { useState } from "react";
import { ProfileForm } from "@/features/profile/components/profile-form";
import { PreferencesForm } from "@/features/profile/components/preferences-form";
import { TwoFactorSection } from "@/features/profile/components/two-factor-section";
import { SessionsSection } from "@/features/sessions/components/sessions-section";
import { useTranslations } from "next-intl";

type Tab = "details" | "preferences" | "security";

export function ProfileTabs() {
  const t = useTranslations("pages.profile");
  const [activeTab, setActiveTab] = useState<Tab>("details");
  const tabs: { id: Tab; label: string }[] = [
    { id: "details", label: t("detailsTab") },
    { id: "preferences", label: t("preferencesTab") },
    { id: "security", label: t("securityTab") },
  ];

  return (
    <div>
      <div role="tablist" aria-label={t("tabsLabel")} className="flex gap-1 overflow-x-auto border-b border-border">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            id={`${tab.id}-tab`}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.id}
            aria-controls={`${tab.id}-panel`}
            tabIndex={activeTab === tab.id ? 0 : -1}
            className={`border-b-2 px-4 py-3 text-sm font-medium ${activeTab === tab.id ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}
            onClick={() => setActiveTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>
      <section id="details-panel" role="tabpanel" aria-labelledby="details-tab" hidden={activeTab !== "details"} className="pt-6">
        <ProfileForm />
      </section>
      <section id="preferences-panel" role="tabpanel" aria-labelledby="preferences-tab" hidden={activeTab !== "preferences"} className="pt-6">
        <PreferencesForm />
      </section>
      <section id="security-panel" role="tabpanel" aria-labelledby="security-tab" hidden={activeTab !== "security"} className="space-y-6 pt-6">
        <div className="flex flex-col justify-between gap-4 rounded-xl border border-border p-5 sm:flex-row sm:items-center">
          <div>
            <h2 className="font-semibold">{t("security")}</h2>
            <p className="mt-1 text-sm text-muted-foreground">{t("securityDescription")}</p>
          </div>
          <Link href="/change-password" className="inline-flex h-10 items-center justify-center rounded-lg border border-border px-4 text-sm font-medium hover:bg-muted">
            {t("changePassword")}
          </Link>
        </div>
        <TwoFactorSection />
        <SessionsSection />
      </section>
    </div>
  );
}
