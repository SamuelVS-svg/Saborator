/* Barra de búsqueda de la cabecera: al pulsar la lupa aparece sobre la barra y sugiere platos y combos mientras se escribe. */
(function () {
  'use strict';
  const btn = document.getElementById('lnavBuscarBtn');
  const panel = document.getElementById('lnavBuscar');
  const input = document.getElementById('lnavBuscarInput');
  const cerrar = document.getElementById('lnavBuscarCerrar');
  const res = document.getElementById('lnavBuscarRes');
  if (!btn || !panel || !input) return;

  let temporizador = 0, ultimaPeticion = 0, activo = -1;
  const pesos = n => '$' + Number(n).toLocaleString('es-CO');
  const esc = s => String(s == null ? '' : s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  function abrir() {
    panel.hidden = false;
    document.querySelector('.lnav')?.classList.add('buscando');
    btn.setAttribute('aria-expanded', 'true');
    input.value = '';
    ocultarResultados();
    setTimeout(() => input.focus(), 30);
  }
  function cerrarBarra() {
    panel.hidden = true;
    document.querySelector('.lnav')?.classList.remove('buscando');
    btn.setAttribute('aria-expanded', 'false');
    ocultarResultados();
  }
  function ocultarResultados() { res.hidden = true; res.innerHTML = ''; activo = -1; }

  function irA(texto) {
    const q = (texto || '').trim();
    if (!q) return;
    if (typeof buscarEnCarta === 'function' && document.getElementById('menu')?.classList.contains('is-open')) {   // la carta existe oculta en las demás páginas públicas
      buscarEnCarta(q);
      document.getElementById('cartaBuscar').scrollIntoView({ behavior: 'smooth', block: 'center' });
      cerrarBarra();
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
      if (id === ultimaPeticion) res.innerHTML = '<div class="lnav-buscar-vacio">No se pudo buscar ahora. Pulsa Enter para ver la carta.</div>', res.hidden = false;
    }
  }

  btn.addEventListener('click', () => (panel.hidden ? abrir() : cerrarBarra()));
  cerrar.addEventListener('click', cerrarBarra);

  input.addEventListener('input', () => {
    clearTimeout(temporizador);
    const q = input.value.trim();
    if (q.length < 2) { ocultarResultados(); return; }
    temporizador = setTimeout(() => buscar(q), 180);
  });

  input.addEventListener('keydown', e => {
    const items = [...res.querySelectorAll('.lnav-buscar-item')];
    if (e.key === 'Escape') { cerrarBarra(); btn.focus(); }
    else if (e.key === 'ArrowDown' && items.length) { e.preventDefault(); activo = (activo + 1) % items.length; marcar(items); }
    else if (e.key === 'ArrowUp' && items.length) { e.preventDefault(); activo = (activo - 1 + items.length) % items.length; marcar(items); }
    else if (e.key === 'Enter') { e.preventDefault(); irA(activo >= 0 && items[activo] ? items[activo].dataset.nombre : input.value); }
  });
  function marcar(items) { items.forEach((it, i) => it.classList.toggle('activo', i === activo)); items[activo]?.scrollIntoView({ block: 'nearest' }); }

  res.addEventListener('click', e => {
    const item = e.target.closest('.lnav-buscar-item');
    if (item) irA(item.dataset.nombre);
    else if (e.target.closest('[data-todos]')) irA(input.value);
  });

  document.addEventListener('click', e => { if (!panel.hidden && !panel.contains(e.target) && !btn.contains(e.target)) cerrarBarra(); });
})();
