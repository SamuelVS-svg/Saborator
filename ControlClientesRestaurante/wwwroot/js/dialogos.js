// Diálogos de Saborator: reemplazan alert() y confirm() del navegador por modales propios.
//  · alert(msg)                         → modal informativo (se sobrescribe window.alert)
//  · Saborator.confirmar(msg, opciones) → Promise<boolean>
//  · data-confirm="msg" en un <form>, <button> o <a>: pide confirmación en un modal antes de actuar
//      opcionales: data-confirm-titulo, data-confirm-ok, data-confirm-peligro="true"
(function () {
    'use strict';

    var cola = [], activo = null, raiz = null;

    function crear() {
        if (raiz) return raiz;
        raiz = document.createElement('div');
        raiz.className = 'dlg-fondo';
        raiz.hidden = true;
        raiz.innerHTML =
            '<div class="dlg" role="dialog" aria-modal="true" aria-labelledby="dlgTitulo" aria-describedby="dlgMensaje">' +
                '<div class="dlg-icono" id="dlgIcono" aria-hidden="true"></div>' +
                '<h2 class="dlg-titulo" id="dlgTitulo"></h2>' +
                '<p class="dlg-mensaje" id="dlgMensaje"></p>' +
                '<div class="dlg-acciones">' +
                    '<button type="button" class="dlg-btn dlg-btn-sec" id="dlgCancelar"></button>' +
                    '<button type="button" class="dlg-btn dlg-btn-pri" id="dlgAceptar"></button>' +
                '</div>' +
            '</div>';
        document.body.appendChild(raiz);
        raiz.addEventListener('mousedown', function (e) { if (e.target === raiz && activo && activo.cancelable) cerrar(false); });
        raiz.querySelector('#dlgCancelar').addEventListener('click', function () { cerrar(false); });
        raiz.querySelector('#dlgAceptar').addEventListener('click', function () { cerrar(true); });
        return raiz;
    }

    var ICONOS = {
        peligro: '<svg viewBox="0 0 24 24"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>',
        pregunta: '<svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>',
        info: '<svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>'
    };

    function mostrarSiguiente() {
        if (activo || !cola.length) return;
        activo = cola.shift();
        var r = crear(), o = activo.op;
        r.querySelector('.dlg').dataset.tono = o.tono;
        r.querySelector('#dlgIcono').innerHTML = ICONOS[o.tono] || ICONOS.info;
        r.querySelector('#dlgTitulo').textContent = o.titulo;
        r.querySelector('#dlgMensaje').textContent = o.mensaje;
        var cancelar = r.querySelector('#dlgCancelar'), aceptar = r.querySelector('#dlgAceptar');
        cancelar.hidden = !o.cancelable;
        cancelar.textContent = o.cancelar;
        aceptar.textContent = o.aceptar;
        aceptar.className = 'dlg-btn ' + (o.tono === 'peligro' ? 'dlg-btn-peligro' : 'dlg-btn-pri');
        activo.foco = document.activeElement;
        r.hidden = false;
        document.documentElement.classList.add('dlg-abierto');
        // En confirmaciones peligrosas el foco empieza en "Cancelar" para evitar aceptar sin querer
        (o.cancelable && o.tono === 'peligro' ? cancelar : aceptar).focus();
    }

    function cerrar(valor) {
        if (!activo) return;
        var a = activo; activo = null;
        raiz.hidden = true;
        document.documentElement.classList.remove('dlg-abierto');
        try { if (a.foco && a.foco.focus) a.foco.focus(); } catch (e) {}
        a.resolver(valor);
        mostrarSiguiente();
    }

    function abrir(op) {
        return new Promise(function (resolver) { cola.push({ op: op, resolver: resolver }); mostrarSiguiente(); });
    }

    document.addEventListener('keydown', function (e) {
        if (!activo) return;
        if (e.key === 'Escape') { e.preventDefault(); cerrar(false); }
        else if (e.key === 'Tab') {
            var bs = Array.prototype.filter.call(raiz.querySelectorAll('button'), function (b) { return !b.hidden; });
            if (!bs.length) return;
            var i = bs.indexOf(document.activeElement);
            e.preventDefault();
            bs[(i + (e.shiftKey ? -1 : 1) + bs.length) % bs.length].focus();
        }
    }, true);

    var Saborator = window.Saborator = window.Saborator || {};

    Saborator.confirmar = function (mensaje, opciones) {
        var o = opciones || {};
        return abrir({
            mensaje: String(mensaje == null ? '' : mensaje),
            titulo: o.titulo || (o.peligro ? 'Confirma esta acción' : 'Confirmar'),
            aceptar: o.aceptar || 'Sí, continuar',
            cancelar: o.cancelar || 'Cancelar',
            tono: o.peligro ? 'peligro' : 'pregunta',
            cancelable: true
        });
    };

    Saborator.avisar = function (mensaje, opciones) {
        var o = opciones || {};
        return abrir({
            mensaje: String(mensaje == null ? '' : mensaje),
            titulo: o.titulo || 'Aviso',
            aceptar: o.aceptar || 'Entendido',
            cancelar: '',
            tono: o.tono || 'info',
            cancelable: false
        });
    };

    // alert() nativo → modal (no bloquea, igual que el resto de la interfaz)
    window.alert = function (mensaje) { Saborator.avisar(mensaje); };

    // ── data-confirm ───────────────────────────────────────────────────────
    function opcionesDe(el) {
        return {
            titulo: el.getAttribute('data-confirm-titulo') || undefined,
            aceptar: el.getAttribute('data-confirm-ok') || undefined,
            peligro: el.getAttribute('data-confirm-peligro') === 'true'
        };
    }

    // Formularios: se detiene el envío, se pregunta y se reenvía con el mismo botón
    document.addEventListener('submit', function (e) {
        var f = e.target;
        if (!f.getAttribute || !f.hasAttribute('data-confirm') || f.__confirmado) return;
        e.preventDefault(); e.stopImmediatePropagation();
        var boton = e.submitter;
        Saborator.confirmar(f.getAttribute('data-confirm'), opcionesDe(f)).then(function (ok) {
            if (!ok) return;
            f.__confirmado = true;
            try { boton ? f.requestSubmit(boton) : f.requestSubmit(); } finally { f.__confirmado = false; }
        });
    }, true);

    // Botones y enlaces con data-confirm
    document.addEventListener('click', function (e) {
        var el = e.target.closest ? e.target.closest('[data-confirm]') : null;
        if (!el || el.tagName === 'FORM' || el.__confirmado) return;
        e.preventDefault(); e.stopImmediatePropagation();
        Saborator.confirmar(el.getAttribute('data-confirm'), opcionesDe(el)).then(function (ok) {
            if (!ok) return;
            el.__confirmado = true;
            try { el.click(); } finally { el.__confirmado = false; }
        });
    }, true);
})();