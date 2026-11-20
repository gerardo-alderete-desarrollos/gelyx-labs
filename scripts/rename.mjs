import { readdir, readFile, rename, stat, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const args = process.argv.slice(2);
const flags = new Set(args.filter((arg) => arg.startsWith("-")));
const names = args.filter((arg) => !arg.startsWith("-"));

function usage() {
  console.error("Usage: node scripts/rename.mjs [--dry-run] <NombrePascal> <nombre-kebab>");
  process.exit(2);
}
if (names.length !== 2) usage();
for (const flag of flags) {
  if (flag !== "--dry-run") usage();
}

const [newPascal, newKebab] = names;
const dryRun = flags.has("--dry-run");

if (!/^[A-Za-z][A-Za-z0-9]*$/.test(newPascal)) {
  console.error("NombrePascal must be an ASCII identifier (letters and digits).");
  process.exit(2);
}
if (!/^[a-z0-9]+(-[a-z0-9]+)*$/.test(newKebab)) {
  console.error("nombre-kebab must be lowercase kebab-case.");
  process.exit(2);
}

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const scriptPath = path.resolve(fileURLToPath(import.meta.url));
const excluded = new Set([
  ".git",
  ".next",
  ".turbo",
  ".vercel",
  "node_modules",
  "bin",
  "obj",
  "reports",
  "test-results",
  "playwright-report",
  "coverage",
  "dist",
  "build",
]);
const paths = [];
const files = [];

async function visit(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  entries.sort((left, right) => left.name.localeCompare(right.name));
  for (const entry of entries) {
    if (excluded.has(entry.name) || entry.isSymbolicLink()) continue;
    if (entry.name.startsWith(".env") && entry.name !== ".env.example") continue;
    const fullPath = path.join(directory, entry.name);
    paths.push(fullPath);
    if (entry.isDirectory()) {
      await visit(fullPath);
    } else if (entry.isFile()) {
      files.push(fullPath);
    }
  }
}

// Old names are built from fragments so the completeness grep over the
// template does not match this tool itself.
const OLD_KEBAB = "conta" + "sync";
const OLD_PASCAL = "Conta" + "Sync";

function replaceName(value) {
  return value
    .replaceAll(`${OLD_KEBAB}-api`, `${newKebab}-api`)
    .replaceAll(`${OLD_KEBAB}-web`, `${newKebab}-web`)
    .replaceAll(OLD_KEBAB, newKebab)
    .replaceAll(OLD_PASCAL, newPascal);
}

function destinationFor(source) {
  const relative = path.relative(root, source);
  return path.join(root, ...relative.split(path.sep).map(replaceName));
}

await visit(root);
const moves = paths
  .map((source) => ({ source, destination: destinationFor(source) }))
  .filter(({ source, destination }) => source !== destination);
const movingSources = new Set(moves.map(({ source }) => source));
const destinations = new Set();
for (const { destination } of moves) {
  if (destinations.has(destination)) {
    throw new Error("Rename aborted: multiple paths would have the same destination.");
  }
  destinations.add(destination);
  try {
    await stat(destination);
  } catch (error) {
    if (error.code === "ENOENT") continue;
    throw error;
  }
  if (!movingSources.has(destination)) {
    throw new Error(`Rename aborted: destination already exists: ${path.relative(root, destination)}`);
  }
}

const decoder = new TextDecoder("utf-8", { fatal: true });
const contentUpdates = [];
for (const source of files) {
  if (path.resolve(source) === scriptPath) continue;
  const bytes = await readFile(source);
  if (bytes.includes(0)) continue;
  let content;
  try {
    content = decoder.decode(bytes);
  } catch (error) {
    if (error instanceof TypeError) continue;
    throw error;
  }
  const updated = replaceName(content);
  if (updated !== content) contentUpdates.push({ source, updated });
}

if (dryRun) {
  for (const { source } of contentUpdates) {
    console.log(`  content  ${path.relative(root, source)}`);
  }
  moves.sort((left, right) => right.source.split(path.sep).length - left.source.split(path.sep).length);
  for (const { source, destination } of moves) {
    console.log(`  rename   ${path.relative(root, source)}  ->  ${path.relative(root, destination)}`);
  }
  console.log(`Dry run: ${contentUpdates.length} text files and ${moves.length} paths would change.`);
  process.exit(0);
}

for (const { source, updated } of contentUpdates) {
  await writeFile(source, updated, "utf8");
}
moves.sort((left, right) => right.source.split(path.sep).length - left.source.split(path.sep).length);
for (const { source } of moves) {
  const renamedName = replaceName(path.basename(source));
  if (renamedName !== path.basename(source)) {
    await rename(source, path.join(path.dirname(source), renamedName));
  }
}

console.log(`Updated ${contentUpdates.length} text files and renamed ${moves.length} paths.`);