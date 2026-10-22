// Renders diagrams in this folder to .svg and a 3x .png, white background,
// width 1600, using mermaid-config.json. One browser is shared by all renders.
//
// Setup (once, from docs/diagrams; node_modules is git-ignored):
//   npm i --no-save --no-package-lock @mermaid-js/mermaid-cli@11
// Usage:
//   node render.mjs                     all .mmd files
//   node render.mjs design-class cloud-architecture
// Browser: installed Edge by default; set BROWSER_PATH to use Chrome instead.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { renderMermaid } from '@mermaid-js/mermaid-cli';

const dir = path.dirname(fileURLToPath(import.meta.url));
const args = process.argv.slice(2);
const names = args.length
  ? args.map((name) => name.replace(/\.mmd$/, ''))
  : fs.readdirSync(dir).filter((file) => file.endsWith('.mmd')).map((file) => file.slice(0, -4));
const mermaidConfig = JSON.parse(fs.readFileSync(path.join(dir, 'mermaid-config.json'), 'utf8'));
const browser = await puppeteer.launch({
  executablePath: process.env.BROWSER_PATH || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
  headless: 'shell',
});
try {
  for (const name of names) {
    const definition = fs.readFileSync(path.join(dir, `${name}.mmd`), 'utf8');
    const common = { backgroundColor: 'white', mermaidConfig };
    const svg = await renderMermaid(browser, definition, 'svg', { ...common, viewport: { width: 1600, height: 600 } });
    fs.writeFileSync(path.join(dir, `${name}.svg`), svg.data);
    const png = await renderMermaid(browser, definition, 'png', { ...common, viewport: { width: 1600, height: 600, deviceScaleFactor: 3 } });
    fs.writeFileSync(path.join(dir, `${name}.png`), png.data);
    const viewBox = Buffer.from(svg.data).toString().match(/viewBox="([^"]*)"/)?.[1];
    console.log(name, 'viewBox', viewBox);
  }
} finally {
  await browser.close();
}
