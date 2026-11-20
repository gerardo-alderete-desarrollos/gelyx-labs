type Project = {
  id: string;
  name: string;
  shortDescription?: string | null;
  url?: string | null;
  imageUrl?: string | null;
  category?: string | null;
  isFeatured: boolean;
};

const thumb = (u: string) => `https://image.thum.io/get/width/800/crop/600/noanimate/${u}`;

const fallback = [
  { id: "gymerup", name: "GymerUp", shortDescription: "Seguimiento de entrenamiento y ecosistema fitness.", url: "https://gymerup-web.vercel.app/", imageUrl: thumb("https://gymerup-web.vercel.app/"), category: "Fitness", isFeatured: true },
  { id: "bookeritos", name: "Bookeritos", shortDescription: "Suscripción y gestión de libros infantiles.", url: "https://bookeritos-web-v2.vercel.app/", imageUrl: thumb("https://bookeritos-web-v2.vercel.app/"), category: "Educación", isFeatured: true },
  { id: "cambiaeltrip", name: "Cambia el Trip", shortDescription: "Experiencias turísticas habilitadas por tecnología propia.", url: "https://cambiandoeltrip-web.vercel.app/", imageUrl: thumb("https://cambiandoeltrip-web.vercel.app/"), category: "Turismo", isFeatured: true },
  { id: "contasync", name: "ContaSync", shortDescription: "Automatización contable y conciliación.", url: "https://contasync-web.vercel.app/", imageUrl: thumb("https://contasync-web.vercel.app/"), category: "Fintech", isFeatured: true },
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
              <article className="group overflow-hidden rounded-2xl border border-border bg-card transition-all duration-300 hover:-translate-y-2 hover:shadow-2xl">
                <div className="relative h-52 overflow-hidden bg-black">
                  {p.imageUrl ? (
                    <img src={p.imageUrl} alt={p.name} className="h-full w-full object-contain transition-transform duration-700 group-hover:scale-105" />
                  ) : (
                    <div className="flex h-full items-center justify-center bg-[var(--brand-gradient)]">
                      <span className="font-heading text-2xl font-bold text-white">{p.name}</span>
                    </div>
                  )}
                </div>
                <div className="p-5">
                  <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{p.category ?? "Producto"}</span>
                  <h3 className="mt-1 font-heading text-xl font-bold">{p.name}</h3>
                  <p className="mt-1 text-sm text-muted-foreground">{p.shortDescription}</p>
                  {p.url ? (
                    <a href={p.url} target="_blank" rel="noreferrer" className="mt-3 inline-flex w-fit items-center gap-1 rounded-full bg-[var(--brand-gradient)] px-4 py-1.5 text-sm font-medium text-white transition hover:opacity-90">
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