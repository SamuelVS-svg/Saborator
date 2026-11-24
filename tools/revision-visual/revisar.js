#!/usr/bin/env node
/*
 * Revisión visual automática de Saborator.
 *
 * Recorre las páginas del sitio como visitante y como administrador, en modo claro y oscuro, y avisa de:
 *   - páginas que no cargan (HTTP 4xx/5xx) y errores de consola o de JavaScript
 *   - desborde horizontal (la página se sale del ancho de la pantalla)
 *   - texto con poco contraste contra su fondo (solo texto visible y que nadie tapa)
 *   - cambios respecto a las capturas de referencia (--actualizar-base las guarda)
 *
 * Uso (con la aplicación ya corriendo, por ejemplo en http://localhost:5106):
 *   npm install                      (una sola vez)
 *   npx playwright install chromium  (una sola vez, si no tienes el navegador)
 *   node revisar.js                  revisa y compara con la referencia
 *   node revisar.js --actualizar-base   guarda las capturas actuales como referencia nueva
 *
 * Opciones: --base=http://localhost:5106  --movil  --contraste=3  --tolerancia=0.2
 *           --usuario=admin --clave=...   (o variables SABORATOR_USUARIO / SABORATOR_CLAVE)
 * Sale con código 1 si encuentra problemas, así sirve también antes de subir cambios.
 */
const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');
const { PNG } = require('pngjs');
const pixelmatch = require('pixelmatch');

const arg = (n, d) => { const a = process.argv.find(x => x.startsWith(`--${n}=`)); return a ? a.split('=').slice(1).join('=') : d; };
const flag = n => process.argv.includes(`--${n}`);

const BASE = arg('base', 'http://localhost:5106').replace(/\/$/, '');
const USUARIO = arg('usuario', process.env.SABORATOR_USUARIO || 'admin');
const CLAVE = arg('clave', process.env.SABORATOR_CLAVE || 'Admin#Saborator2026!');
const CONTRASTE_MIN = parseFloat(arg('contraste', '3'));
const TOLERANCIA = parseFloat(arg('tolerancia', '0.2')); // % de píxeles distintos que se acepta
const ACTUALIZAR = flag('actualizar-base');
const MOVIL = flag('movil');
const DIR_BASE = path.join(__dirname, 'baseline');
const DIR_SALIDA = path.join(__dirname, 'salida');

const PUBLICAS = ['/', '/Carta', '/Sede', '/Resenas', '/Nosotros', '/Contacto', '/Login', '/Pedido/Rastrear',
  '/Reserva/Nueva', '/Home/Privacy', '/Home/Terminos', '/Pedido/Checkout'];
const ADMIN = ['/Home/Dashboard', '/Mesa', '/Caja', '/Factura', '/Factura/Create', '/Pedido', '/Pedido/Cocina',
  '/Pedido/Domicilios', '/Pedido/Repartos', '/Reserva/Admin', '/Cliente', '/Producto', '/Proveedor', '/Cupon',
  '/Resena', '/Personal', '/Auditoria', '/Fidelizacion', '/Perfil'];

// Se ejecuta dentro de la página: devuelve hasta 5 textos visibles con contraste menor al mínimo.
function buscarBajoContraste(minimo) {
  const luminancia = ([r, g, b]) => {
    const f = v => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); };
    return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b);
  };
  const numeros = s => (s.match(/[\d.]+/g) || []).map(Number);
  // Color de fondo efectivo; si hay imagen o degradado se promedian sus colores; si no se puede saber, devuelve null.
  const fondo = el => {
    for (let e = el; e; e = e.parentElement) {
      const cs = getComputedStyle(e);
      if (cs.backgroundImage && cs.backgroundImage !== 'none') {
        const cols = cs.backgroundImage.match(/rgba?\([^)]*\)/g);
        if (!cols || /url\(/.test(cs.backgroundImage)) return null;
        const v = cols.map(numeros).filter(c => c.length < 4 || c[3] > 0.5);
        if (!v.length) return null;
        return [0, 1, 2].map(i => v.reduce((a, c) => a + c[i], 0) / v.length);
      }
      const m = numeros(cs.backgroundColor);
      if (m.length && (m.length < 4 || m[3] > 0.9)) return m.slice(0, 3);
    }
    return [255, 255, 255];
  };
  const visibleYLibre = el => {
    const r = el.getBoundingClientRect();
    if (r.width < 6 || r.height < 6 || r.bottom < 0 || r.top > innerHeight || r.right < 0 || r.left > innerWidth) return false;
    for (let e = el; e; e = e.parentElement) {
      const cs = getComputedStyle(e);
      if (cs.display === 'none' || cs.visibility === 'hidden' || parseFloat(cs.opacity) < 0.3) return false;
    }
    const x = Math.min(Math.max(r.left + r.width / 2, 1), innerWidth - 1);
    const y = Math.min(Math.max(r.top + r.height / 2, 1), innerHeight - 1);
    const top = document.elementFromPoint(x, y);
    return !!top && (top === el || el.contains(top) || top.contains(el));
  };
  const malos = [];
  for (const el of document.querySelectorAll('body *')) {
    if (malos.length >= 5) break;
    const texto = [...el.childNodes].find(n => n.nodeType === 3 && n.textContent.trim().length > 1);
    if (!texto || !visibleYLibre(el)) continue;
    const cs = getComputedStyle(el);
    const col = numeros(cs.color); if (col.length > 3 && col[3] < 0.5) continue;
    const bg = fondo(el); if (!bg) continue;
    const a = luminancia(col.slice(0, 3)), b = luminancia(bg);
    const ratio = (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
    if (ratio < minimo) malos.push(`${(el.className || el.tagName).toString().split(' ')[0].slice(0, 32)} ${ratio.toFixed(1)}:1 "${texto.textContent.trim().slice(0, 24)}"`);
  }
  return malos;
}

function comparar(rutaA, rutaB, rutaDif) {
  const a = PNG.sync.read(fs.readFileSync(rutaA)), b = PNG.sync.read(fs.readFileSync(rutaB));
  if (a.width !== b.width || a.height !== b.height) return 100;
  const dif = new PNG({ width: a.width, height: a.height });
  const n = pixelmatch(a.data, b.data, dif.data, a.width, a.height, { threshold: 0.1 });
  const pct = (n / (a.width * a.height)) * 100;
  if (pct > TOLERANCIA) fs.writeFileSync(rutaDif, PNG.sync.write(dif));
  return pct;
}

(async () => {
  fs.mkdirSync(DIR_BASE, { recursive: true });
  fs.mkdirSync(DIR_SALIDA, { recursive: true });
  const opciones = { headless: true };
  if (process.env.CHROMIUM_PATH) opciones.executablePath = process.env.CHROMIUM_PATH;
  const browser = await chromium.launch(opciones);
  const problemas = []; let paginas = 0, nuevas = 0;
  const viewport = MOVIL ? { width: 390, height: 800 } : { width: 1280, height: 800 };
  const sufijo = MOVIL ? 'movil' : 'escritorio';

  async function recorrer(rutas, conSesion) {
    const ctx = await browser.newContext({ viewport, reducedMotion: 'reduce' });
    const p = await ctx.newPage();
    const errores = [];
    p.on('console', m => { if (m.type() === 'error') errores.push(m.text().slice(0, 110)); });
    p.on('pageerror', e => errores.push('JS: ' + e.message.slice(0, 100)));
    p.on('response', r => { if (r.status() >= 400 && r.url().startsWith(BASE)) errores.push(`${r.status()} ${r.url().replace(BASE, '')}`); });
    if (conSesion) {
      await p.goto(BASE + '/Login');
      const ok = await p.evaluate(async ([u, c]) => {
        const r = await fetch('/Login/LoginAjax', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ usuario: u, password: c, returnUrl: '/' }) });
        return r.ok;
      }, [USUARIO, CLAVE]);
      if (!ok) { problemas.push('No se pudo iniciar sesión con el usuario de prueba: se omiten las páginas del personal.'); await ctx.close(); return; }
    }
    for (const ruta of rutas) for (const tema of ['claro', 'oscuro']) {
      errores.length = 0; paginas++;
      const etiqueta = `${conSesion ? 'admin' : 'publico'}${ruta.replace(/\//g, '_') || '_inicio'}-${tema}-${sufijo}`;
      const resp = await p.goto(BASE + ruta, { waitUntil: 'load' }).catch(() => null);
      await p.evaluate(t => {
        if (t === 'oscuro') document.documentElement.setAttribute('data-theme', 'dark'); else document.documentElement.removeAttribute('data-theme');
        document.querySelectorAll('*').forEach(e => { e.style.animation = 'none'; e.style.transition = 'none'; });
      }, tema);
      await p.keyboard.press('Escape');
      await p.waitForTimeout(600);
      const fallas = [];
      const estado = resp ? resp.status() : 0;
      if (estado === 0 || estado >= 400) fallas.push(`HTTP ${estado}`);
      const desborde = await p.evaluate(() => document.documentElement.scrollWidth - innerWidth);
      if (desborde > 4) fallas.push(`desborde horizontal de ${desborde}px`);
      if (errores.length) fallas.push('consola: ' + [...new Set(errores)].slice(0, 3).join(' | '));
      const bajo = await p.evaluate(buscarBajoContraste, CONTRASTE_MIN);
      if (bajo.length) fallas.push('bajo contraste: ' + bajo.join(' ; '));
      const actual = path.join(DIR_SALIDA, etiqueta + '.png');
      await p.screenshot({ path: actual });
      const referencia = path.join(DIR_BASE, etiqueta + '.png');
      if (ACTUALIZAR || !fs.existsSync(referencia)) { fs.copyFileSync(actual, referencia); nuevas++; }
      else {
        const pct = comparar(referencia, actual, path.join(DIR_SALIDA, etiqueta + '.diferencia.png'));
        if (pct > TOLERANCIA) fallas.push(`cambió ${pct.toFixed(2)}% de la imagen (ver salida/${etiqueta}.diferencia.png)`);
      }
      if (fallas.length) problemas.push(`[${tema}] ${ruta} -> ${fallas.join(' || ')}`);
    }
    await ctx.close();
  }

  try {
    await recorrer(PUBLICAS, false);
    await recorrer(ADMIN, true);
  } finally { await browser.close(); }

  console.log(`\nRevisadas ${paginas} vistas (${sufijo}). ${nuevas ? nuevas + ' capturas de referencia guardadas.' : ''}`);
  if (problemas.length) {
    console.log(`\n${problemas.length} problema(s):\n` + problemas.map(x => ' - ' + x).join('\n'));
    process.exit(1);
  }
  console.log('Sin problemas.');
})().catch(e => { console.error(e); process.exit(2); });
