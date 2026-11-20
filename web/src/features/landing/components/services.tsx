const services = [
  { title: "GELYX Consulting", desc: "Diagnóstico tecnológico, estrategia de transformación digital, arquitectura de soluciones y roadmaps." },
  { title: "GELYX Studio", desc: "Landing pages, e-commerce, aplicaciones web y sistemas empresariales con diseño UX/UI." },
  { title: "GELYX Data", desc: "Dashboards, ingeniería e integración de datos, Data Warehouse y gobierno de datos." },
  { title: "GELYX AI", desc: "Agentes inteligentes, automatización de procesos, IA generativa e integración de modelos." },
];

import { Reveal } from "./reveal";

export function Services() {
  return (
    <section id="servicios" className="border-t border-border bg-card/40 py-24">
      <div className="mx-auto max-w-7xl px-6">
        <Reveal>
          <h2 className="font-heading text-3xl font-bold tracking-tight md:text-4xl">Servicios</h2>
          <p className="mt-3 max-w-2xl text-muted-foreground">
            Cuatro líneas de especialización tecnológica para acompañar a tu empresa en cada etapa.
          </p>
        </Reveal>
        <div className="mt-12 grid gap-6 md:grid-cols-2 lg:grid-cols-4">
          {services.map((s, i) => (
            <Reveal key={s.title} delay={i * 90}>
              <div className="rounded-2xl border border-border bg-card p-6 transition hover:shadow-lg hover:-translate-y-1">
                <h3 className="font-heading text-lg font-semibold">{s.title}</h3>
                <p className="mt-3 text-sm text-muted-foreground">{s.desc}</p>
              </div>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}