#!/usr/bin/env node
import { readFile, writeFile } from "node:fs/promises";
import { basename, dirname, extname, join, resolve } from "node:path";
import { PDFDocument } from "pdf-lib";

function parsePageSpec(spec, pageCount) {
  const pages = [];
  for (const part of spec.split(",").map((s) => s.trim()).filter(Boolean)) {
    const range = part.match(/^(\d+)-(\d+)$/);
    if (range) {
      const start = Number(range[1]);
      const end = Number(range[2]);
      if (start < 1 || end < 1 || start > end) {
        throw new Error(`Invalid range: ${part}`);
      }
      for (let i = start; i <= end; i++) pages.push(i);
    } else if (/^\d+$/.test(part)) {
      pages.push(Number(part));
    } else {
      throw new Error(`Invalid page token: ${part}`);
    }
  }
  for (const p of pages) {
    if (p < 1 || p > pageCount) {
      throw new Error(`Page ${p} out of range (1-${pageCount})`);
    }
  }
  return pages;
}

function defaultOutputPath(input) {
  const dir = dirname(input);
  const ext = extname(input);
  const stem = basename(input, ext);
  return join(dir, `${stem}.extracted${ext || ".pdf"}`);
}

function usage() {
  console.error(
    "Usage: extract-pages <input.pdf> <pages> [output.pdf]\n" +
      "  pages: comma-separated, supports ranges (e.g. \"1,3,5-7\")",
  );
}

async function main() {
  const [, , inputArg, pagesArg, outputArg] = process.argv;
  if (!inputArg || !pagesArg) {
    usage();
    process.exit(1);
  }

  const inputPath = resolve(inputArg);
  const inputBytes = await readFile(inputPath);
  const src = await PDFDocument.load(inputBytes);

  const pageNumbers = parsePageSpec(pagesArg, src.getPageCount());
  const indices = pageNumbers.map((n) => n - 1);

  const out = await PDFDocument.create();
  const copied = await out.copyPages(src, indices);
  for (const page of copied) out.addPage(page);

  const outputPath = resolve(outputArg ?? defaultOutputPath(inputPath));
  await writeFile(outputPath, await out.save());
  console.log(`Wrote ${pageNumbers.length} page(s) to ${outputPath}`);
}

main().catch((err) => {
  console.error(err.message);
  process.exit(1);
});
