import type { Metadata } from "next";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { ProfileTabs } from "@/features/profile/components/profile-tabs";
import { requireModule } from "@/lib/auth/get-session";
import { getTranslations } from "next-intl/server";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("pages.profile");
  return { title: t("metadata") };
}

function initials(fullName: string, email: string) {
  const parts = fullName.trim().split(/\s+/).filter(Boolean);
  const value = parts.length > 0 ? parts.slice(0, 2).map((part) => part[0]).join("") : email.slice(0, 2);
  return value.toUpperCase();
}

export default async function ProfilePage() {
  const t = await getTranslations("pages.profile");
  const session = await requireModule("profile");
  return (
    <div className="mx-auto max-w-3xl space-y-7">
      <div>
        <Link href="/dashboard" className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="size-4" /> {t("back")}
        </Link>
        <div className="mt-5 flex items-center gap-4">
          <span aria-hidden="true" className="grid size-12 place-items-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
            {initials(session.fullName, session.email)}
          </span>
          <div>
            <h1 className="text-3xl font-semibold tracking-tight">{t("title")}</h1>
            <p className="mt-1 text-muted-foreground">{t("description")}</p>
          </div>
        </div>
      </div>
      <div className="rounded-2xl border border-border bg-card p-6 shadow-sm md:p-8">
        <ProfileTabs />
      </div>
    </div>
  );
}
