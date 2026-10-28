// Compatibilidad: los enlaces antiguos con ancla (/#menu, /#ubicacion, /#resenas) llevan a la página nueva.
(function () {
    'use strict';
    var destinos = { '#menu': '/Carta', '#ubicacion': '/Sede', '#resenas': '/Resenas', '#experiencia': '/Nosotros' };
    var destino = destinos[location.hash];
    if (destino && location.pathname === '/') location.replace(destino);
})();