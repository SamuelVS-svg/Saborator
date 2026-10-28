// Compatibilidad: los enlaces antiguos con ancla (/#menu, /#ubicacion, /#resenas) llevan a la página nueva.
(function () {
    'use strict';
    var destinos = { '#menu': '/Carta', '#ubicacion': '/Sede', '#resenas': '/Resenas', '#experiencia': '/Nosotros' };
    var destino = destinos[location.hash];
    if (destino && location.pathname === '/') location.replace(destino);
})();

// ── Pasarela de imágenes (carrusel) ───────────────────────────────────────────────────────────────
// Desliza solo cada 6 s, se detiene al pasar el cursor o al enfocarla, admite flechas, puntos,
// teclado y deslizar con el dedo. Con "reducir movimiento" no avanza sola.
(function () {
    'use strict';
    var caja = document.getElementById('homeCarousel');
    if (!caja) return;
    var pista = caja.querySelector('.home-carousel-track');
    var slides = caja.querySelectorAll('.home-slide');
    var puntos = caja.querySelector('.home-carousel-dots');
    var total = slides.length, actual = 0, timer = null;
    var reducir = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    for (var i = 0; i < total; i++) {
        var b = document.createElement('button');
        b.type = 'button';
        b.setAttribute('aria-label', 'Ir a la diapositiva ' + (i + 1) + ' de ' + total);
        (function (n) { b.addEventListener('click', function () { ir(n, true); }); })(i);
        puntos.appendChild(b);
    }

    function ir(n, manual) {
        actual = (n + total) % total;
        pista.style.transform = 'translateX(' + (-100 * actual) + '%)';
        slides.forEach(function (s, k) { s.setAttribute('aria-hidden', k === actual ? 'false' : 'true'); s.toggleAttribute('inert', k !== actual); });
        Array.prototype.forEach.call(puntos.children, function (p, k) { p.setAttribute('aria-current', k === actual ? 'true' : 'false'); });
        if (manual) reiniciar();
    }
    function siguiente() { ir(actual + 1); }
    function parar() { if (timer) { clearInterval(timer); timer = null; } }
    function iniciar() { if (!reducir && total > 1 && !timer) timer = setInterval(siguiente, 6000); }
    function reiniciar() { parar(); iniciar(); }

    caja.querySelector('.prev').addEventListener('click', function () { ir(actual - 1, true); });
    caja.querySelector('.next').addEventListener('click', function () { ir(actual + 1, true); });
    caja.addEventListener('mouseenter', parar);
    caja.addEventListener('mouseleave', iniciar);
    caja.addEventListener('focusin', parar);
    caja.addEventListener('focusout', iniciar);
    caja.addEventListener('keydown', function (e) {
        if (e.key === 'ArrowLeft') ir(actual - 1, true);
        else if (e.key === 'ArrowRight') ir(actual + 1, true);
    });

    // Deslizar con el dedo
    var x0 = null;
    caja.addEventListener('pointerdown', function (e) { if (e.pointerType !== 'mouse') x0 = e.clientX; }, { passive: true });
    caja.addEventListener('pointerup', function (e) {
        if (x0 === null) return;
        var dx = e.clientX - x0; x0 = null;
        if (Math.abs(dx) > 40) ir(actual + (dx < 0 ? 1 : -1), true);
    }, { passive: true });
    document.addEventListener('visibilitychange', function () { if (document.hidden) parar(); else iniciar(); });

    ir(0);
    iniciar();
})();
