const steps = [
  { n: "01", title: "Entendemos", desc: "Analizamos tu negocio, procesos y objetivos antes de construir." },
  { n: "02", title: "Diseñamos", desc: "Proponemos una solución práctica, escalable y sin sobredimensionar." },
  { n: "03", title: "Desarrollamos", desc: "Construimos con tecnología moderna, segura y mantenible." },
  { n: "04", title: "Acompañamos", desc: "Soporte, evolución y mejora continua junto a tu equipo." },
];

import { Reveal } from "./reveal";

export function Process() {
  return (
    <section id="proceso" className="border-t border-border bg-card/40 py-24">
      <div className="mx-auto max-w-7xl px-6">
        <Reveal>
          <h2 className="font-heading text-3xl font-bold tracking-tight md:text-4xl">Proceso</h2>
        </Reveal>
        <div className="mt-12 grid gap-6 md:grid-cols-4">
          {steps.map((s, i) => (
            <Reveal key={s.n} delay={i * 100}>
              <div className="rounded-2xl border border-border bg-card p-6 transition hover:-translate-y-1">
                <span className="font-heading text-3xl font-bold text-[var(--brand-cyan)]">{s.n}</span>
                <h3 className="mt-3 font-heading text-lg font-semibold">{s.title}</h3>
                <p className="mt-2 text-sm text-muted-foreground">{s.desc}</p>
              </div>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}