// Sube el encabezado de la página (icono + título + descripción) a la barra superior
document.addEventListener('DOMContentLoaded', function () {
    var top = document.getElementById('topbar');
    var h = document.querySelector('.main-content .pg-header, .page-body .pg-header') || document.querySelector('.pg-header');
    if (!top || !h || top.contains(h)) return;
    var spacer = top.querySelector('.top-spacer');
    var padre = h.parentElement;
    var hr = (h.nextElementSibling && h.nextElementSibling.matches('hr.div-gold')) ? h.nextElementSibling
           : (padre && padre.nextElementSibling && padre.nextElementSibling.matches('hr.div-gold')) ? padre.nextElementSibling : null;
    top.insertBefore(h, spacer);
    if (hr) hr.style.display = 'none';
    if (padre && padre.children.length === 0) padre.style.display = 'none';
});
