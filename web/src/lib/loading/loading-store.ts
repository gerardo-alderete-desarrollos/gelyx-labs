"use client";

import { useSyncExternalStore } from "react";

type ActiveRequest = {
  controller: AbortController;
};

const listeners = new Set<() => void>();
const activeRequests = new Map<string, ActiveRequest>();
let sequence = 0;

function emit() {
  listeners.forEach((listener) => listener());
}

function snapshot() {
  return activeRequests.size;
}

export const loadingStore = {
  begin(controller: AbortController): string {
    const id = `request-${++sequence}`;
    activeRequests.set(id, { controller });
    emit();
    return id;
  },
  end(id: string) {
    if (activeRequests.delete(id)) emit();
  },
  reset() {
    activeRequests.clear();
    emit();
  },
  cancelAll() {
    activeRequests.forEach(({ controller }) => controller.abort());
    activeRequests.clear();
    emit();
  },
  subscribe(listener: () => void) {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
  getSnapshot: snapshot,
};

export function useIsBlocking(): boolean {
  return useSyncExternalStore(loadingStore.subscribe, () => snapshot() > 0, () => false);
}
