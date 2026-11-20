"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { notify } from "@/lib/notify";

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
  isActive: boolean;
};

const empty: Partial<Project> = { name: "", shortDescription: "", url: "", category: "", isFeatured: false, sortOrder: 0, isActive: true };

export function ProjectsPage() {
  const [items, setItems] = useState<Project[]>([]);
  const [form, setForm] = useState<Partial<Project>>(empty);
  const [editing, setEditing] = useState<string | null>(null);

  async function load() {
    const res = await fetch("/api/projects");
    if (res.ok) setItems(await res.json());
  }

  useEffect(() => {
    let active = true;
    fetch("/api/projects")
      .then((res) => (res.ok ? res.json() : []))
      .then((data) => { if (active) setItems(data); })
      .catch(() => {});
    return () => { active = false; };
  }, []);

  async function save() {
    const method = editing ? "PUT" : "POST";
    const url = editing ? `/api/projects/${editing}` : "/api/projects";
    const res = await fetch(url, {
      method,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ ...form, name: form.name ?? "", isFeatured: !!form.isFeatured, sortOrder: Number(form.sortOrder ?? 0), isActive: form.isActive ?? true }),
    });
    if (res.ok) {
      notify.success(editing ? "Proyecto actualizado" : "Proyecto creado");
      setForm(empty);
      setEditing(null);
      load();
    } else {
      notify.error("No se pudo guardar el proyecto");
    }
  }

  async function remove(id: string) {
    const res = await fetch(`/api/projects/${id}`, { method: "DELETE" });
    if (res.ok) { notify.success("Proyecto eliminado"); load(); }
    else notify.error("No se pudo eliminar");
  }

  function edit(p: Project) {
    setEditing(p.id);
    setForm(p);
  }

  return (
    <div className="space-y-6">
      <h1 className="font-heading text-2xl font-bold">Proyectos</h1>
      <div className="rounded-2xl border border-border bg-card p-6 space-y-3">
        <Input placeholder="Nombre" value={form.name ?? ""} onChange={(e) => setForm({ ...form, name: e.target.value })} />
        <Input placeholder="Descripción corta" value={form.shortDescription ?? ""} onChange={(e) => setForm({ ...form, shortDescription: e.target.value })} />
        <Input placeholder="URL" value={form.url ?? ""} onChange={(e) => setForm({ ...form, url: e.target.value })} />
        <Input placeholder="Categoría" value={form.category ?? ""} onChange={(e) => setForm({ ...form, category: e.target.value })} />
        <div className="flex gap-3">
          <Button onClick={save}>{editing ? "Actualizar" : "Crear"}</Button>
          {editing && <Button variant="outline" onClick={() => { setEditing(null); setForm(empty); }}>Cancelar</Button>}
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        {items.map((p) => (
          <div key={p.id} className="rounded-2xl border border-border bg-card p-4">
            <div className="flex items-start justify-between">
              <h3 className="font-heading font-semibold">{p.name}</h3>
              <span className="text-xs text-muted-foreground">{p.category}</span>
            </div>
            <p className="mt-2 text-sm text-muted-foreground">{p.shortDescription}</p>
            <div className="mt-4 flex gap-2">
              <Button size="sm" variant="outline" onClick={() => edit(p)}>Editar</Button>
              <Button size="sm" variant="destructive" onClick={() => remove(p.id)}>Eliminar</Button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}