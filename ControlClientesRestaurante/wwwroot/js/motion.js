// Movimiento de la portada pública: sobrio y con intención.
// Requiere gsap y ScrollTrigger (wwwroot/lib/gsap). Solo anima transform/opacity.
(function () {
    'use strict';
    if (!window.gsap || !window.ScrollTrigger) return;
    gsap.registerPlugin(ScrollTrigger);

    // En este sitio el que hace scroll es <body> (html tiene overflow oculto): ScrollTrigger debe leer de ahí.
    var esBody = /auto|scroll/.test(getComputedStyle(document.body).overflowY) && document.body.getBoundingClientRect().height <= window.innerHeight + 1;
    var scroller = esBody ? document.body : window;
    if (esBody) {
        // ScrollTrigger trata <body> como la ventana; aquí hace falta leer y escuchar su propio scroll.
        ScrollTrigger.scrollerProxy(document.body, {
            scrollTop: function (v) { if (arguments.length) document.body.scrollTop = v; return document.body.scrollTop; },
            getBoundingClientRect: function () { return { top: 0, left: 0, width: window.innerWidth, height: window.innerHeight }; }
        });
        document.body.addEventListener('scroll', ScrollTrigger.update, { passive: true });
    }
    ScrollTrigger.defaults({ scroller: scroller });
    var finScroll = function () { return esBody ? document.body.scrollHeight - window.innerHeight : ScrollTrigger.maxScroll(window); };

    var mm = gsap.matchMedia();

    mm.add('(prefers-reduced-motion: no-preference)', function () {
        var limpiezas = [];

        // ── 1 · Entrada del banner (espera a que termine la introducción) ─────────────────────
        var banner = document.querySelector('.home-banner');
        if (banner) {
            var h1 = banner.querySelector('h1');
            var eyebrow = banner.querySelector('.home-eyebrow');
            var parrafo = banner.querySelector('.home-banner-copy p');
            var cta = banner.querySelector('.home-cta');
            var plato = banner.querySelector('.home-banner-plate');
            var ins = [];

            if (h1 && !h1.querySelector('.mo-line')) {
                // Parte el titular en líneas (separadas por <br>) con máscara
                var lineas = [[]];
                Array.prototype.slice.call(h1.childNodes).forEach(function (n) {
                    if (n.nodeName === 'BR') lineas.push([]); else lineas[lineas.length - 1].push(n);
                });
                h1.textContent = '';
                lineas.forEach(function (nodos) {
                    var l = document.createElement('span'); l.className = 'mo-line';
                    var i = document.createElement('span'); i.className = 'mo-in';
                    nodos.forEach(function (n) { i.appendChild(n); });
                    l.appendChild(i); h1.appendChild(l); ins.push(i);
                });
            }

            var resto = [eyebrow, parrafo, cta].filter(Boolean);
            gsap.set(ins, { yPercent: 110 });
            gsap.set(resto, { autoAlpha: 0, y: 16 });
            if (plato) gsap.set(plato, { autoAlpha: 0, scale: 0.88, rotation: -5 });

            var entrada = gsap.timeline({ paused: true, defaults: { ease: 'expo.out' } });
            if (eyebrow) entrada.to(eyebrow, { autoAlpha: 1, y: 0, duration: 0.8 }, 0);
            entrada.to(ins, { yPercent: 0, duration: 1.1, stagger: 0.12 }, 0.1);
            if (parrafo) entrada.to(parrafo, { autoAlpha: 1, y: 0, duration: 0.9 }, 0.45);
            if (cta) entrada.to(cta, { autoAlpha: 1, y: 0, duration: 0.9 }, 0.6);
            if (plato) entrada.to(plato, { autoAlpha: 1, scale: 1, rotation: 0, duration: 1.4 }, 0.2);

            var lanzado = false;
            var lanzar = function () { if (!lanzado) { lanzado = true; entrada.play(); } };
            var intro = document.getElementById('cinematicIntro');
            if (!intro || intro.classList.contains('intro-done') || getComputedStyle(intro).display === 'none') {
                lanzar();
            } else {
                var obs = new MutationObserver(function () {
                    if (intro.classList.contains('intro-done')) { obs.disconnect(); lanzar(); }
                });
                obs.observe(intro, { attributes: true, attributeFilter: ['class'] });
                var seguro = setTimeout(lanzar, 9000);
                limpiezas.push(function () { obs.disconnect(); clearTimeout(seguro); lanzar(); });
            }

            // ── 2 · Parallax del plato (otro elemento: la imagen interior) ──────────────────────
            var dish = banner.querySelector('.home-banner-dish');
            if (dish) {
                gsap.to(dish, {
                    yPercent: -7, rotation: 3, ease: 'none',
                    scrollTrigger: { trigger: '.home-hero', start: 'top top', end: 'bottom top', scrub: 0.6 }
                });
            }

            // ── 5 · Atracción magnética del botón (solo ratón fino) ─────────────────────────────
            if (cta && window.matchMedia('(hover: hover) and (pointer: fine)').matches) {
                var qx = gsap.quickTo(cta, 'x', { duration: 0.5, ease: 'power3.out' });
                var qy = gsap.quickTo(cta, 'y', { duration: 0.5, ease: 'power3.out' });
                var mueve = function (e) {
                    var r = cta.getBoundingClientRect();
                    qx((e.clientX - (r.left + r.width / 2)) * 0.18);
                    qy((e.clientY - (r.top + r.height / 2)) * 0.25);
                };
                var suelta = function () { qx(0); qy(0); };
                cta.addEventListener('pointermove', mueve);
                cta.addEventListener('pointerleave', suelta);
                limpiezas.push(function () {
                    cta.removeEventListener('pointermove', mueve);
                    cta.removeEventListener('pointerleave', suelta);
                    gsap.set(cta, { clearProps: 'x,y' });
                });
            }
        }

        // ── 3 · Tarjetas en tandas ───────────────────────────────────────────────────────────
        var vistas = new WeakSet();
        function revelar(selector) {
            var nuevas = Array.prototype.filter.call(document.querySelectorAll(selector), function (el) {
                return !vistas.has(el);
            });
            if (!nuevas.length) return;
            nuevas.forEach(function (el) { vistas.add(el); });
            gsap.set(nuevas, { autoAlpha: 0, y: 24 });
            ScrollTrigger.batch(nuevas, {
                start: 'top 90%', once: true,
                onEnter: function (lote) {
                    gsap.to(lote, { autoAlpha: 1, y: 0, duration: 0.9, ease: 'power3.out', stagger: 0.08, overwrite: true, clearProps: 'transform' });
                }
            });
        }
        revelar('.home-order-card, .home-order-wide');
        revelar('.dish-card-minimal');

        var grilla = document.querySelector('.menu-minimal-grid');
        var mo = null;
        if (grilla) {
            var pendiente = 0;
            mo = new MutationObserver(function () {
                cancelAnimationFrame(pendiente);
                pendiente = requestAnimationFrame(function () { revelar('.dish-card-minimal'); ScrollTrigger.refresh(); });
            });
            mo.observe(grilla, { childList: true });
        }

        // ── 4 · Barra de avance de lectura (solo páginas largas) ─────────────────────────────
        var barra = document.createElement('div');
        barra.className = 'mo-progreso';
        barra.setAttribute('aria-hidden', 'true');
        document.body.appendChild(barra);
        var pt = gsap.to(barra, {
            scaleX: 1, ease: 'none',
            scrollTrigger: { start: 0, end: finScroll, scrub: 0.2 }
        });
        var mostrar = function () {
            var el = esBody ? document.body : document.documentElement;
            barra.style.display = el.scrollHeight > el.clientHeight * 1.4 ? '' : 'none';
        };
        mostrar();
        ScrollTrigger.addEventListener('refresh', mostrar);

        // ── Navbar compacta al hacer scroll ──────────────────────────────────────────────────
        var nav = document.querySelector('.lnav');
        var stNav = nav ? ScrollTrigger.create({
            start: 60, end: finScroll,
            onToggle: function (self) { nav.classList.toggle('mo-compacta', self.isActive); }
        }) : null;

        var alCargar = function () { ScrollTrigger.refresh(); };
        window.addEventListener('load', alCargar);
        limpiezas.push(function () { window.removeEventListener('load', alCargar); });

        return function () {
            limpiezas.forEach(function (f) { f(); });
            if (mo) mo.disconnect();
            ScrollTrigger.removeEventListener('refresh', mostrar);
            if (stNav) { stNav.kill(); nav.classList.remove('mo-compacta'); }
            if (pt) pt.kill();
            barra.remove();
        };
    });
})();
