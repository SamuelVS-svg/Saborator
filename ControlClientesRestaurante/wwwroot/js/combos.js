// Combos de Saborator (navegador). Refleja el cálculo del servidor (ComboService.CalcularDescuento):
// si el carrito contiene los productos de un combo, se ahorra la diferencia con el precio especial.
// El servidor vuelve a calcularlo con los precios reales al confirmar el pedido; esto es solo para mostrarlo.
(function () {
    'use strict';
    var combos = window.SABORATOR_COMBOS || [];

    function descuento(cart) {
        var tiene = {};
        (cart || []).forEach(function (i) { tiene[i.id] = (tiene[i.id] || 0) + (i.cantidad || 0); });
        var total = 0, detalle = [];
        combos.forEach(function (c) {
            if (!(c.ahorro > 0)) return;
            var veces = Infinity;
            c.lineas.forEach(function (l) { veces = Math.min(veces, Math.floor((tiene[l.productoId] || 0) / l.cantidad)); });
            if (!isFinite(veces) || veces <= 0) return;
            c.lineas.forEach(function (l) { tiene[l.productoId] -= l.cantidad * veces; });
            total += c.ahorro * veces;
            detalle.push({ nombre: c.nombre, veces: veces, ahorro: c.ahorro * veces });
        });
        return { total: total, detalle: detalle };
    }

    function porId(id) {
        for (var i = 0; i < combos.length; i++) if (combos[i].id === id) return combos[i];
        return null;
    }

    function moneda(n) { return '$' + Number(n || 0).toLocaleString('es-CO'); }

    window.SaboratorCombos = { lista: combos, descuento: descuento, porId: porId, moneda: moneda };
})();
