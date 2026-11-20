"use client";

import { useQuery } from "@tanstack/react-query";
import { settingValues, settingsService } from "@/features/settings/services/settings.service";

export function useFeatureFlag(name: string): boolean {
  const query = useQuery({ queryKey: ["public-settings"], queryFn: settingsService.public });
  return settingValues(query.data ?? [])[`flags.${name}`] === true;
}
