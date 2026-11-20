import type { ManagedModule, ModuleFormValues } from "@/features/modules/schemas/modules.schema";

// Modules are not yet represented in src/lib/api/generated.ts, so this local contract
// is deliberately limited to the API payload consumed by this feature.
export type ModuleUpdateVariables = { module: ManagedModule; values: ModuleFormValues };
