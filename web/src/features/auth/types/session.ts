import type { components } from "@/lib/api/generated";

export type SessionModule = components["schemas"]["ModuleInfo"];
export type SessionUser = components["schemas"]["MeResponse"];
export type AuthTokens = components["schemas"]["AuthResponse"];
