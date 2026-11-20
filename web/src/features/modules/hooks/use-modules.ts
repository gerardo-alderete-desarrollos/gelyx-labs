"use client";

import { useQuery } from "@tanstack/react-query";
import { modulesService } from "@/features/modules/services/modules.service";

export function useModules() {
  return useQuery({ queryKey: ["modules"], queryFn: modulesService.list });
}
