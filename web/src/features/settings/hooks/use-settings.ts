"use client";

import { useQuery } from "@tanstack/react-query";
import { settingsService } from "@/features/settings/services/settings.service";

export const settingsQueryKey = ["settings"] as const;

export function useSettings() {
  return useQuery({ queryKey: settingsQueryKey, queryFn: settingsService.list });
}
