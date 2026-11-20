import { writeFile } from "node:fs/promises";
import openapiTS, { astToString } from "openapi-typescript";

const source = process.env.API_OPENAPI_URL ?? "http://localhost:5000/openapi/v1.json";
const schema = await openapiTS(source);
const output = astToString(schema);

await writeFile(new URL("../src/lib/api/generated.ts", import.meta.url), output);
console.log(`Generated src/lib/api/generated.ts from ${source}`);
