import { apiFetch } from "@/lib/api/client";
import { Hero } from "@/features/landing/components/hero";
import { Marquee } from "@/features/landing/components/marquee";
import { ClientLogos } from "@/features/landing/components/client-logos";
import { Services } from "@/features/landing/components/services";
import { Portfolio } from "@/features/landing/components/portfolio";
import { CaseStudy } from "@/features/landing/components/case-study";
import { Process } from "@/features/landing/components/process";
import { Testimonials } from "@/features/landing/components/testimonials";
import { QuoteSection } from "@/features/landing/components/quote-section";
import { SiteFooter } from "@/features/landing/components/footer";

type Project = {
  id: string;
  name: string;
  slug?: string | null;
  shortDescription?: string | null;
  description?: string | null;
  imageUrl?: string | null;
  url?: string | null;
  category?: string | null;
  tags?: string | null;
  isFeatured: boolean;
  sortOrder: number;
};

async function getProjects(): Promise<Project[]> {
  try {
    const response = await apiFetch("projects");
    if (!response.ok) return [];
    const data = await response.json();
    return Array.isArray(data) ? (data as Project[]) : [];
  } catch {
    return [];
  }
}

export default async function HomePage() {
  const projects = await getProjects();
  return (
    <main className="min-h-screen bg-background text-foreground">
      <Hero />
      <Marquee />
      <ClientLogos />
      <Services />
      <Portfolio projects={projects} />
      <CaseStudy />
      <Process />
      <Testimonials />
      <QuoteSection />
      <SiteFooter />
    </main>
  );
}