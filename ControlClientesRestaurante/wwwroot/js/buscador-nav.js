/* Búsqueda de la cabecera. Dos formas de la misma función:
   · Pantallas anchas: campo siempre visible en la cabecera (#lnavBuscarInline).
   · Pantallas estrechas: la lupa abre una barra que cubre la cabecera (#lnavBuscar).
   En ambas se sugieren platos y combos mientras se escribe; Enter o elegir un resultado lleva a la carta filtrada. */
(function () {
  'use strict';
  const pesos = n => '$' + Number(n).toLocaleString('es-CO');
  const esc = s => String(s == null ? '' : s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  const btn = document.getElementById('lnavBuscarBtn');
  const panel = document.getElementById('lnavBuscar');           // barra emergente (pantallas estrechas)
  const cerrar = document.getElementById('lnavBuscarCerrar');
  const inline = document.getElementById('lnavBuscarInline');    // campo fijo (pantallas anchas)

  // Cada buscador (emergente o fijo) tiene su propio campo y su lista de resultados
  function crearBuscador(input, res, alCerrar) {
    let temporizador = 0, ultimaPeticion = 0, activo = -1;

    function ocultar() { res.hidden = true; res.innerHTML = ''; activo = -1; }

    function irA(texto) {
      const q = (texto || '').trim();
      if (!q) return;
      if (typeof buscarEnCarta === 'function' && document.getElementById('menu')?.classList.contains('is-open')) {
        // la carta existe oculta en las demás páginas públicas: solo se filtra ahí si está abierta
        buscarEnCarta(q);
        document.getElementById('cartaBuscar').scrollIntoView({ behavior: 'smooth', block: 'center' });
        ocultar(); if (alCerrar) alCerrar();
      } else {
        location.href = '/Carta?q=' + encodeURIComponent(q);
      }
    }

    function pintar(lista, q) {
      if (!lista.length) {
        res.innerHTML = '<div class="lnav-buscar-vacio">No encontramos &ldquo;' + esc(q) + '&rdquo;. Prueba con otra palabra.</div>';
      } else {
        res.innerHTML = lista.map((r, i) =>
          '<button type="button" class="lnav-buscar-item" role="option" data-nombre="' + esc(r.nombre) + '" data-i="' + i + '">' +
            '<img src="' + esc(r.imagen) + '" alt="" loading="lazy" />' +
            '<span class="lnav-buscar-txt"><strong>' + esc(r.nombre) + '</strong><small>' + (r.tipo === 'combo' ? 'Combo · ' : '') + esc(r.detalle) + '</small></span>' +
            '<span class="lnav-buscar-precio">' + pesos(r.precio) + '</span>' +
          '</button>').join('') +
          '<button type="button" class="lnav-buscar-todos" data-todos="1">Ver todos los resultados de &ldquo;' + esc(q) + '&rdquo;</button>';
      }
      res.hidden = false; activo = -1;
    }

    async function buscar(q) {
      const id = ++ultimaPeticion;
      try {
        const r = await fetch('/Home/BuscarCarta?q=' + encodeURIComponent(q), { headers: { Accept: 'application/json' } });
        if (!r.ok) throw new Error(r.status);
        const lista = await r.json();
        if (id === ultimaPeticion) pintar(lista, q);   // descarta respuestas viejas
      } catch (e) {
        if (id === ultimaPeticion) { res.innerHTML = '<div class="lnav-buscar-vacio">No se pudo buscar ahora. Pulsa Enter para ver la carta.</div>'; res.hidden = false; }
      }
    }

    function marcar(items) { items.forEach((it, i) => it.classList.toggle('activo', i === activo)); items[activo]?.scrollIntoView({ block: 'nearest' }); }

    input.addEventListener('input', () => {
      clearTimeout(temporizador);
      const q = input.value.trim();
      if (q.length < 2) { ocultar(); return; }
      temporizador = setTimeout(() => buscar(q), 180);
    });
    input.addEventListener('focus', () => { if (input.value.trim().length >= 2 && res.innerHTML) res.hidden = false; });
    input.addEventListener('keydown', e => {
      const items = [...res.querySelectorAll('.lnav-buscar-item')];
      if (e.key === 'Escape') { ocultar(); if (alCerrar) alCerrar(); else input.blur(); }
      else if (e.key === 'ArrowDown' && items.length) { e.preventDefault(); activo = (activo + 1) % items.length; marcar(items); }
      else if (e.key === 'ArrowUp' && items.length) { e.preventDefault(); activo = (activo - 1 + items.length) % items.length; marcar(items); }
      else if (e.key === 'Enter') { e.preventDefault(); irA(activo >= 0 && items[activo] ? items[activo].dataset.nombre : input.value); }
    });
    res.addEventListener('click', e => {
      const item = e.target.closest('.lnav-buscar-item');
      if (item) irA(item.dataset.nombre);
      else if (e.target.closest('[data-todos]')) irA(input.value);
    });
    return { ocultar, limpiar() { input.value = ''; ocultar(); } };
  }

  // ── Barra emergente (pantallas estrechas) ─────────────────────────────────────────────────────
  let emergente = null;
  if (btn && panel) {
    const input = document.getElementById('lnavBuscarInput');
    const res = document.getElementById('lnavBuscarRes');
    const abrir = () => {
      panel.hidden = false;
      document.querySelector('.lnav')?.classList.add('buscando');
      btn.setAttribute('aria-expanded', 'true');
      emergente.limpiar();
      setTimeout(() => input.focus(), 30);
    };
    const cerrarBarra = () => {
      panel.hidden = true;
      document.querySelector('.lnav')?.classList.remove('buscando');
      btn.setAttribute('aria-expanded', 'false');
      emergente.ocultar();
    };
    emergente = crearBuscador(input, res, cerrarBarra);
    btn.addEventListener('click', () => (panel.hidden ? abrir() : cerrarBarra()));
    cerrar?.addEventListener('click', cerrarBarra);
    document.addEventListener('click', e => { if (!panel.hidden && !panel.contains(e.target) && !btn.contains(e.target)) cerrarBarra(); });
  }

  // ── Campo fijo (pantallas anchas) ─────────────────────────────────────────────────────────────
  if (inline) {
    const input = document.getElementById('lnavBuscarInlineInput');
    const res = document.getElementById('lnavBuscarInlineRes');
    const fijo = crearBuscador(input, res, null);
    document.addEventListener('click', e => { if (!inline.contains(e.target)) fijo.ocultar(); });
    // Atajo: "/" lleva el cursor al campo (como en las cadenas), salvo si ya se está escribiendo en otro campo
    document.addEventListener('keydown', e => {
      if (e.key === '/' && !/^(INPUT|TEXTAREA|SELECT)$/.test((document.activeElement || {}).tagName) && inline.offsetParent) { e.preventDefault(); input.focus(); }
    });
  }
})();
