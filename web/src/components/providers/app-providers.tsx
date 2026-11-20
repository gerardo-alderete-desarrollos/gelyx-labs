"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { ThemeProvider } from "next-themes";
import { useEffect, useState } from "react";
import { NextIntlClientProvider } from "next-intl";
import { NotifyToaster } from "@/lib/notify";
import { LoadingOverlay } from "@/components/ui/loading-overlay";
import type { AppLocale } from "@/i18n/routing";

type IntlMessages = typeof import("../../../messages/es.json");

export function AppProviders({
  children,
  locale,
  messages,
}: Readonly<{
  children: React.ReactNode;
  locale: AppLocale;
  messages: IntlMessages;
}>) {
  const router = useRouter();
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            retry: 1,
            refetchOnWindowFocus: true,
          },
        },
      }),
  );

  useEffect(() => {
    const channel = new BroadcastChannel("gelyx-labs-session");
    const handleExpired = () => {
      queryClient.clear();
      router.replace("/login?reason=session-expired");
      router.refresh();
    };
    const handleMessage = (event: MessageEvent<unknown>) => {
      if (event.data === "logout" || event.data === "password-changed") {
        queryClient.clear();
        const reason = event.data === "password-changed" ? "password-changed" : "signed-out";
        router.replace(`/login?reason=${reason}`);
        router.refresh();
      }
    };
    window.addEventListener("gelyx-labs:session-expired", handleExpired);
    channel.addEventListener("message", handleMessage);
    return () => {
      window.removeEventListener("gelyx-labs:session-expired", handleExpired);
      channel.removeEventListener("message", handleMessage);
      channel.close();
    };
  }, [queryClient, router]);

  return (
    <NextIntlClientProvider locale={locale} messages={messages} timeZone="America/Argentina/Buenos_Aires">
      <ThemeProvider attribute="class" defaultTheme="system" enableSystem>
        <QueryClientProvider client={queryClient}>
          <div id="app-content">{children}</div>
          <LoadingOverlay />
          <NotifyToaster />
        </QueryClientProvider>
      </ThemeProvider>
    </NextIntlClientProvider>
  );
}
