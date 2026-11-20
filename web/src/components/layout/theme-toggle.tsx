"use client";

import { Monitor } from "lucide-react";
import { useTheme } from "next-themes";
import { Button } from "@/components/ui/button";
import { useTranslations } from "next-intl";

export function ThemeToggle() {
  const { resolvedTheme, setTheme } = useTheme();
  const t = useTranslations("layout");
  const dark = resolvedTheme === "dark";

  return (
    <Button
      aria-label={t("toggleTheme")}
      title={t("toggleThemeTitle")}
      variant="ghost"
      size="icon"
      onClick={() => setTheme(dark ? "light" : "dark")}
    >
      <Monitor />
    </Button>
  );
}
