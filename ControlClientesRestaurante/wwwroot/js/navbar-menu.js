// Accesibilidad del menú móvil: aria-expanded sincronizado con el estado real y cierre con Escape
document.addEventListener('DOMContentLoaded', function () {
    var menu = document.getElementById('lnavMobileMenu'), btn = document.getElementById('lnavMobileToggle');
    if (!menu || !btn) return;
    new MutationObserver(function () { btn.setAttribute('aria-expanded', menu.classList.contains('open') ? 'true' : 'false'); })
        .observe(menu, { attributes: true, attributeFilter: ['class'] });
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && menu.classList.contains('open')) { menu.classList.remove('open'); btn.focus(); }
    });
});
