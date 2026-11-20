import Link from "next/link";
import { appName } from "@/lib/env";
import { getTranslations } from "next-intl/server";

export default async function PublicLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  const t = await getTranslations("pages.public");
  return (
    <main className="grid min-h-screen lg:grid-cols-[1.05fr_0.95fr]">
      <section className="relative hidden overflow-hidden bg-slate-950 p-12 text-white lg:flex lg:flex-col lg:justify-between">
        <div className="absolute -right-40 -top-32 size-[520px] rounded-full bg-blue-600/30 blur-3xl" />
        <div className="absolute -bottom-48 -left-28 size-[520px] rounded-full bg-violet-600/25 blur-3xl" />
        <Link href="/" className="relative z-[var(--z-dropdown)] flex items-center gap-3 font-semibold">
          <span className="grid size-10 place-items-center rounded-xl bg-white/10 text-lg">C</span>
          {appName}
        </Link>
        <div className="relative z-[var(--z-dropdown)] max-w-xl">
          <p className="mb-4 text-sm font-medium uppercase tracking-[0.25em] text-blue-300">{t("tagline")}</p>
          <h1 className="text-5xl font-semibold leading-tight tracking-tight">{t("headline")}</h1>
          <p className="mt-6 max-w-lg text-lg leading-8 text-slate-300">
            {t("description")}
          </p>
        </div>
        <p className="relative z-[var(--z-dropdown)] text-sm text-slate-400">{t("footnote")}</p>
      </section>
      <section className="flex min-h-screen items-center justify-center px-6 py-12">
        <div className="w-full max-w-md">
          <Link href="/" className="mb-10 flex items-center gap-3 font-semibold lg:hidden">
            <span className="grid size-10 place-items-center rounded-xl bg-primary text-lg text-primary-foreground">C</span>
            {appName}
          </Link>
          {children}
        </div>
      </section>
    </main>
  );
}
