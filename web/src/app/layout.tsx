import type { Metadata } from "next";
import { Inter, Space_Grotesk } from "next/font/google";
import "./globals.css";
import { AppProviders } from "@/components/providers/app-providers";
import { appName } from "@/lib/env";
import { getLocale, getMessages, getTranslations } from "next-intl/server";
import { isAppLocale } from "@/i18n/routing";

const inter = Inter({ subsets: ["latin"], variable: "--font-inter", display: "swap" });
const spaceGrotesk = Space_Grotesk({ subsets: ["latin"], variable: "--font-space-grotesk", display: "swap" });

export async function generateMetadata() {
  const t = await getTranslations("metadata");
  return {
    title: { default: appName, template: `%s | ${appName}` },
    description: t("description"),
    ...(process.env.NEXT_INTL_TIME_ZONE && { timeZone: process.env.NEXT_INTL_TIME_ZONE }),
  };
}

export default async function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  const requestedLocale = await getLocale();
  const locale = isAppLocale(requestedLocale) ? requestedLocale : "es";
  const messages = await getMessages();
  return (
    <html lang={locale} suppressHydrationWarning className={`${inter.variable} ${spaceGrotesk.variable}`}>
      <body className="min-h-screen bg-background font-sans text-foreground antialiased">
        <AppProviders
          locale={locale}
          messages={messages as typeof import("../../messages/es.json")}
        >
          {children}
        </AppProviders>
      </body>
    </html>
  );
}
