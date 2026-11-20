type Project = {
  id: string;
  name: string;
  shortDescription?: string | null;
  url?: string | null;
  category?: string | null;
  isFeatured: boolean;
};

const fallback = [
  { id: "gymerup", name: "GymerUp", shortDescription: "Seguimiento de entrenamiento y ecosistema fitness.", url: null, category: "Fitness", isFeatured: true },
  { id: "cambiaeltrip", name: "Cambia el Trip", shortDescription: "Experiencias turísticas habilitadas por tecnología propia.", url: null, category: "Turismo", isFeatured: true },
  { id: "bookeritos", name: "Bookeritos", shortDescription: "Suscripción y gestión de libros infantiles.", url: null, category: "Educación", isFeatured: true },
  { id: "contasync", name: "ContaSync", shortDescription: "Automatización contable y conciliación.", url: null, category: "Fintech", isFeatured: true },
];

import { Reveal } from "./reveal";

export function Portfolio({ projects }: { projects: Project[] }) {
  const items = projects.length ? projects : fallback;
  return (
    <section id="portafolio" className="py-24">
      <div className="mx-auto max-w-7xl px-6">
        <h2 className="font-heading text-3xl font-bold tracking-tight md:text-4xl">Portafolio</h2>
        <p className="mt-3 max-w-2xl text-muted-foreground">
          Productos y plataformas desarrollados por GELYX Labs.
        </p>
        <div className="mt-12 grid gap-6 md:grid-cols-2 lg:grid-cols-4">
          {items.map((p, i) => (
            <Reveal key={p.id} delay={i * 80}>
              <article className="group overflow-hidden rounded-2xl border border-border bg-card transition-all duration-300 hover:-translate-y-2 hover:shadow-xl">
                <div className="flex h-40 items-center justify-center bg-[var(--brand-gradient)]/10 transition-transform duration-500 group-hover:scale-105">
                  <span className="font-heading text-2xl font-bold text-foreground">{p.name}</span>
                </div>
                <div className="p-6">
                  <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{p.category ?? "Producto"}</span>
                  <p className="mt-2 text-sm text-muted-foreground">{p.shortDescription}</p>
                  {p.url ? (
                    <a href={p.url} target="_blank" rel="noreferrer" className="mt-4 inline-block text-sm font-medium text-primary hover:underline">
                      Visitar →
                    </a>
                  ) : null}
                </div>
              </article>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}