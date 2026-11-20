import { Reveal } from "./reveal";

const items = [
  { quote: "GELYX Labs nos ayudó a automatizar procesos que nos quitaban horas cada semana. El resultado superó nuestras expectativas.", author: "Cliente PYME", role: "Director de Operaciones" },
  { quote: "El equipo entendió nuestro negocio antes de escribir una línea de código. La solución fue práctica y escalable.", author: "Startup", role: "Fundador" },
  { quote: "Profesionales, ágiles y cercanos. Recomiendo GELYX Labs para cualquier proyecto digital.", author: "Empresa en crecimiento", role: "CEO" },
];

export function Testimonials() {
  return (
    <section className="border-t border-border bg-card/40 py-24">
      <div className="mx-auto max-w-7xl px-6">
        <Reveal>
          <h2 className="font-heading text-3xl font-bold tracking-tight md:text-4xl">Lo que dicen nuestros clientes</h2>
        </Reveal>
        <div className="mt-12 grid gap-6 md:grid-cols-3">
          {items.map((t, i) => (
            <Reveal key={i} delay={i * 120}>
              <figure className="rounded-2xl border border-border bg-card p-6">
                <blockquote className="text-sm text-muted-foreground">“{t.quote}”</blockquote>
                <figcaption className="mt-4 text-sm font-medium">{t.author} <span className="text-muted-foreground">· {t.role}</span></figcaption>
              </figure>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}