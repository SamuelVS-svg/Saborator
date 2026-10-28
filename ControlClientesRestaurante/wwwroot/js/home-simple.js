// Inicio simple: la portada muestra el banner, las formas de ordenar y la carta.
// Sede & Mapa y Reseñas se abren solo cuando se piden (menú, enlaces o #ancla).
(function () {
    'use strict';
    var ids = ['ubicacion', 'resenas'];
    var secciones = ids.map(function (id) { return document.getElementById(id); }).filter(Boolean);
    if (!secciones.length) return;

    function abrir(id) {
        var destino = null;
        secciones.forEach(function (s) {
            var abierta = s.id === id;
            s.classList.toggle('is-open', abierta);
            if (abierta) destino = s;
        });
        if (!destino) return;
        // Los mapas y componentes que se miden al mostrarse necesitan un aviso de cambio de tamaño
        window.dispatchEvent(new Event('resize'));
        setTimeout(function () { destino.scrollIntoView({ behavior: 'smooth', block: 'start' }); }, 40);
    }

    function cerrarTodas() {
        secciones.forEach(function (s) { s.classList.remove('is-open'); });
        history.replaceState(null, '', location.pathname + location.search);
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    secciones.forEach(function (s) {
        s.classList.add('home-extra');
        var volver = document.createElement('button');
        volver.type = 'button';
        volver.className = 'home-extra-back';
        volver.textContent = '← Volver al inicio';
        volver.addEventListener('click', cerrarTodas);
        s.insertBefore(volver, s.firstChild);
    });

    // Enlaces a #ubicacion o #resenas (también los de otras páginas: /#resenas)
    document.addEventListener('click', function (e) {
        var a = e.target.closest && e.target.closest('a[href]');
        if (!a) return;
        var m = (a.getAttribute('href') || '').match(/^\/?#(ubicacion|resenas)$/);
        if (!m) return;
        e.preventDefault();
        history.replaceState(null, '', '#' + m[1]);
        abrir(m[1]);
    });

    function desdeHash() {
        var m = location.hash.match(/^#(ubicacion|resenas)$/);
        if (m) abrir(m[1]);
    }
    window.addEventListener('hashchange', desdeHash);
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', desdeHash);
    else desdeHash();
})();
