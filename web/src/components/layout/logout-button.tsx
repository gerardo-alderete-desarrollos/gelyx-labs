"use client";

import { useQueryClient } from "@tanstack/react-query";
import { LogOut } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { fetchBff } from "@/lib/api/browser-client";
import { notify } from "@/lib/notify";
import { useTranslations } from "next-intl";

export function LogoutButton() {
  const queryClient = useQueryClient();
  const router = useRouter();
  const t = useTranslations("layout");
  const [pending, setPending] = useState(false);

  async function logout() {
    setPending(true);
    let response: Response;
    try {
      response = await fetchBff("/api/auth/logout", { method: "POST" });
    } catch {
      setPending(false);
      notify.error(t("logoutConnectionError"));
      return;
    }
    queryClient.clear();
    const channel = new BroadcastChannel("gelyx-labs-session");
    channel.postMessage("logout");
    channel.close();
    router.replace(response.ok ? "/login?reason=signed-out" : "/login?reason=logout-failed");
    router.refresh();
  }

  return (
    <Button variant="ghost" className="w-full justify-start gap-2" onClick={logout} disabled={pending}>
      <LogOut className="size-4" />
      {pending ? t("loggingOut") : t("logout")}
    </Button>
  );
}
