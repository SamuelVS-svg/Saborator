// Avisos emergentes del servidor: stock bajo (admin) y mensajes de TempData. Los datos llegan en <script id="avisos-config" type="application/json">.
document.addEventListener('DOMContentLoaded', function () {
    var el = document.getElementById('avisos-config');
    if (!el) return;
    var cfg = {};
    try { cfg = JSON.parse(el.textContent) || {}; } catch (e) { return; }
    (cfg.stock || []).forEach(function (m) { showToastStock('warn', m); });
    if (cfg.mensaje) showToast('ok', cfg.mensaje);
    if (cfg.error) showToast('err', cfg.error);
    if (cfg.errorContrasena) showToast('err', cfg.errorContrasena);
});
