// Material de Saborator: reemplaza el JavaScript de Bootstrap.
//  · Saborator.modal(elemento).show() / .hide()  → diálogos modales (los de .modal)
//  · data-dismiss="modal" / "alert"              → cierra el modal o la alerta que lo contiene
//  · Ondulación (ripple) en los botones principales
(function () {
    'use strict';

    // ── Modales ───────────────────────────────────────────────────────────
    var instancias = new WeakMap();

    function Modal(el) {
        this.el = el;
        this.fondo = null;
        var self = this;
        el.addEventListener('mousedown', function (e) { if (e.target === el) self.hide(); });
    }
    Modal.prototype.show = function () {
        var el = this.el, self = this;
        if (el.classList.contains('show')) return;
        this.fondo = document.createElement('div');
        this.fondo.className = 'modal-backdrop fade';
        document.body.appendChild(this.fondo);
        el.style.display = 'block';
        el.removeAttribute('aria-hidden');
        el.setAttribute('aria-modal', 'true');
        el.setAttribute('role', 'dialog');
        document.documentElement.classList.add('modal-open');
        void el.offsetWidth;                                   // fuerza el reflujo para animar
        el.classList.add('show');
        this.fondo.classList.add('show');
        this.previo = document.activeElement;
        var foco = el.querySelector('[autofocus], input, select, textarea, button:not(.btn-close)');
        if (foco) setTimeout(function () { try { foco.focus(); } catch (e) {} }, 60);
        this._tecla = function (ev) { if (ev.key === 'Escape') self.hide(); };
        document.addEventListener('keydown', this._tecla);
    };
    Modal.prototype.hide = function () {
        var el = this.el, fondo = this.fondo;
        if (!el.classList.contains('show')) return;
        el.classList.remove('show');
        if (fondo) fondo.classList.remove('show');
        document.removeEventListener('keydown', this._tecla);
        setTimeout(function () {
            el.style.display = '';
            el.setAttribute('aria-hidden', 'true');
            if (fondo && fondo.parentNode) fondo.parentNode.removeChild(fondo);
            if (!document.querySelector('.modal.show')) document.documentElement.classList.remove('modal-open');
        }, 200);
        try { if (this.previo && this.previo.focus) this.previo.focus(); } catch (e) {}
    };

    var Saborator = window.Saborator = window.Saborator || {};
    Saborator.modal = function (el) {
        if (!el) return null;
        if (!instancias.has(el)) instancias.set(el, new Modal(el));
        return instancias.get(el);
    };

    // ── data-dismiss ──────────────────────────────────────────────────────
    document.addEventListener('click', function (e) {
        var cierre = e.target.closest && e.target.closest('[data-dismiss]');
        if (!cierre) return;
        var que = cierre.getAttribute('data-dismiss');
        if (que === 'modal') {
            var m = cierre.closest('.modal');
            if (m) Saborator.modal(m).hide();
        } else if (que === 'alert') {
            var a = cierre.closest('.alert');
            if (a && a.parentNode) a.parentNode.removeChild(a);
        }
    });

    // ── Ripple ────────────────────────────────────────────────────────────
    var OBJETIVOS = '.btn-gold, .btn-ghost, .btn-entrar, .btn-kfc-cta, .btn-google-official, .btn, .dlg-btn';
    document.addEventListener('pointerdown', function (e) {
        if (e.button > 0 || (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches)) return;
        var b = e.target.closest && e.target.closest(OBJETIVOS);
        if (!b || b.disabled) return;
        var r = b.getBoundingClientRect();
        var lado = Math.max(r.width, r.height) * 2;
        var onda = document.createElement('span');
        onda.className = 'md-ripple';
        onda.style.width = onda.style.height = lado + 'px';
        onda.style.left = (e.clientX - r.left - lado / 2) + 'px';
        onda.style.top = (e.clientY - r.top - lado / 2) + 'px';
        // La ondulación necesita que el botón sea su referencia y recorte lo que sobresale
        if (getComputedStyle(b).position === 'static') b.style.position = 'relative';
        b.classList.add('md-ripple-host');
        b.appendChild(onda);
        setTimeout(function () { if (onda.parentNode) onda.parentNode.removeChild(onda); }, 650);
    }, { passive: true });
})();
