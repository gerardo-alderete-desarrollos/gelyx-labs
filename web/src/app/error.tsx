"use client";

import { ErrorBoundaryContent } from "@/components/error-boundary-content";

export default function AppError({
  error,
  reset,
}: Readonly<{ error: Error & { digest?: string }; reset: () => void }>) {
  return <ErrorBoundaryContent error={error} reset={reset} />;
}
