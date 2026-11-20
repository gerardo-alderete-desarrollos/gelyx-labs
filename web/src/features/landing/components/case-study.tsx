"use client";

import { useEffect, useRef, useState } from "react";
import { Reveal } from "./reveal";

export function CaseStudy() {
  const ref = useRef<HTMLElement>(null);
  const [offset, setOffset] = useState(0);

  useEffect(() => {
    const onScroll = () => {
      const el = ref.current;
      if (!el) return;
      const rect = el.getBoundingClientRect();
      const progress = (window.innerHeight - rect.top) / (window.innerHeight + rect.height);
      setOffset((progress - 0.5) * 80);
    };
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <section ref={ref} className="overflow-hidden py-24">
      <div className="mx-auto max-w-7xl px-6">
        <div className="grid items-center gap-10 lg:grid-cols-2">
          <Reveal>
            <p className="text-sm uppercase tracking-widest text-[var(--brand-cyan)]">Caso de éxito</p>
            <h2 className="mt-3 font-heading text-3xl font-bold tracking-tight md:text-4xl">GymerUp</h2>
            <p className="mt-4 text-muted-foreground">
              Plataforma de seguimiento de entrenamiento y ecosistema fitness. Diseñamos una experiencia
              ágil para gimnasios y atletas, con métricas en tiempo real y escalabilidad para crecer.
            </p>
            <ul className="mt-6 space-y-2 text-sm text-muted-foreground">
              <li>✓ Seguimiento de entrenamiento en tiempo real</li>
              <li>✓ Ecosistema fitness para gimnasios</li>
              <li>✓ Seguro, escalable y mantenible</li>
            </ul>
          </Reveal>
          <div className="relative h-80 overflow-hidden rounded-2xl border border-border">
            <div
              className="absolute inset-0 bg-[var(--brand-gradient)] opacity-20"
              style={{ transform: `translateY(${offset}px) scale(1.2)` }}
            />
            <div className="absolute inset-0 flex items-center justify-center">
              <span className="font-heading text-5xl font-bold text-foreground/80">GymerUp</span>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}