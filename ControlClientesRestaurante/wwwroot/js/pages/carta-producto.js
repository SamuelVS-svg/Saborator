// Ventana de detalle de un producto de la carta: foto grande, comentarios, cantidad y "Agregar".
(function () {
    'use strict';
    var modal, caja, estado = { card: null, cantidad: 1 };
    var formato = function (n) { return '$' + Number(n).toLocaleString('es-CO'); };

    function el(id) { return document.getElementById(id); }

    function precioTotal() { return estado.card ? estado.cantidad * Number(estado.card.dataset.precio || 0) : 0; }

    function actualizar() {
        el('productoCantidad').textContent = estado.cantidad;
        el('productoMenos').disabled = estado.cantidad <= 1;
        el('productoAgregarTotal').textContent = formato(precioTotal());
    }

    function abrir(card) {
        modal = modal || el('productoModal');
        if (!modal) return;
        caja = caja || modal.querySelector('.producto-caja');
        estado.card = card; estado.cantidad = 1;
        var d = card.dataset;
        el('productoNombre').textContent = d.nombre;
        el('productoPrecio').textContent = formato(d.precio);
        el('productoDesc').textContent = d.desc || '';
        el('productoEtiqueta').textContent = d.tag || '';
        el('productoTiempoTxt').textContent = d.tiempo || '';
        var img = el('productoImg'); img.src = d.img; img.alt = d.nombre;
        var nota = el('productoNotaTxt'); nota.value = ''; el('productoNotaContador').textContent = '0/140';
        modal.classList.add('abierto');
        document.body.style.overflow = 'hidden';
        actualizar();
        setTimeout(function () { el('productoCerrar').focus(); }, 30);
    }

    function cerrar() {
        if (!modal) return;
        modal.classList.remove('abierto');
        document.body.style.overflow = '';
        if (estado.card) estado.card.focus({ preventScroll: true });
        estado.card = null;
    }

    function agregar() {
        if (!estado.card) return;
        var d = estado.card.dataset;
        cartAgregar(Number(d.id), d.nombre, Number(d.precio), d.imgCarrito, null, estado.cantidad, el('productoNotaTxt').value.trim());
        cerrar();
        if (typeof abrirDrawerCarrito === 'function') abrirDrawerCarrito();
    }

    document.addEventListener('DOMContentLoaded', function () {
        var grid = document.querySelector('.menu-minimal-grid');
        if (!grid) return;
        grid.addEventListener('click', function (e) {
            if (e.target.closest('.btn-dish-add')) return;       // el "+" agrega directo
            var card = e.target.closest('.dish-card-minimal');
            if (card) abrir(card);
        });
        grid.addEventListener('keydown', function (e) {
            if ((e.key === 'Enter' || e.key === ' ') && e.target.classList && e.target.classList.contains('dish-card-minimal')) {
                e.preventDefault(); abrir(e.target);
            }
        });
        var m = el('productoModal');
        if (!m) return;
        m.addEventListener('click', function (e) { if (e.target === m) cerrar(); });
        el('productoCerrar').addEventListener('click', cerrar);
        el('productoMenos').addEventListener('click', function () { if (estado.cantidad > 1) { estado.cantidad--; actualizar(); } });
        el('productoMas').addEventListener('click', function () { if (estado.cantidad < 20) { estado.cantidad++; actualizar(); } });
        el('productoAgregar').addEventListener('click', agregar);
        el('productoNotaTxt').addEventListener('input', function () { el('productoNotaContador').textContent = this.value.length + '/140'; });
        document.addEventListener('keydown', function (e) {
            if (!m.classList.contains('abierto')) return;
            if (e.key === 'Escape') { cerrar(); return; }
            if (e.key === 'Tab') {                                  // mantiene el foco dentro de la ventana
                var f = [].slice.call(m.querySelectorAll('button:not(:disabled), textarea, [href]'));
                if (!f.length) return;
                var primero = f[0], ultimo = f[f.length - 1];
                if (e.shiftKey && document.activeElement === primero) { e.preventDefault(); ultimo.focus(); }
                else if (!e.shiftKey && document.activeElement === ultimo) { e.preventDefault(); primero.focus(); }
            }
        });
    });
})();
