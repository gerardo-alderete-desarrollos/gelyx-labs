import { cn } from "@/lib/utils";

export function Spinner({ size = "md" }: Readonly<{ size?: "sm" | "md" | "lg" }>) {
  const sizes = { sm: "size-4", md: "size-6", lg: "size-8" };
  return <span aria-hidden="true" className={cn("animate-spin rounded-full border-2 border-current border-t-transparent motion-reduce:animate-pulse", sizes[size])} />;
}
