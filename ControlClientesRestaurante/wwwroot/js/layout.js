// ── Toggle de Modo Oscuro / Claro ──────────────────────────
// Aplica data-theme + clase .dark en <html> (todo el sitio cambia por variables CSS), guarda la elección
// y, mientras el visitante no haya elegido, sigue los cambios de tema del sistema operativo.
function aplicarModoOscuro(oscuro) {
    var h = document.documentElement;
    if (oscuro) h.setAttribute('data-theme', 'dark'); else h.removeAttribute('data-theme');
    h.classList.toggle('dark', oscuro);
    h.style.colorScheme = oscuro ? 'dark' : 'light';
    document.querySelectorAll('.theme-toggle-btn').forEach(function (b) { b.setAttribute('aria-pressed', oscuro ? 'true' : 'false'); });
    document.dispatchEvent(new CustomEvent('saborator:tema', { detail: { oscuro: oscuro } }));
}
function toggleModoOscuro() {
    var t = document.documentElement.getAttribute('data-theme');
    var oscuro = !(t === 'dark' || t === 'noche');
    aplicarModoOscuro(oscuro);
    try { localStorage.setItem('saborator_theme', oscuro ? 'dark' : 'light'); } catch (e) {}
}
(function seguirSistema() {
    if (!window.matchMedia) return;
    var mq = matchMedia('(prefers-color-scheme: dark)');
    var alCambiar = function (e) {
        var guardado = null;
        try { guardado = localStorage.getItem('saborator_theme'); } catch (x) {}
        if (!guardado) aplicarModoOscuro(e.matches);
    };
    if (mq.addEventListener) mq.addEventListener('change', alCambiar); else if (mq.addListener) mq.addListener(alCambiar);
    var t = document.documentElement.getAttribute('data-theme');
    document.querySelectorAll('.theme-toggle-btn').forEach(function (b) { b.setAttribute('aria-pressed', (t === 'dark' || t === 'noche') ? 'true' : 'false'); });
})();

// ── Toast ─────────────────────────────────────────────────
function showToast(tipo, msg) {
    var wrap = document.getElementById('toastWrap');
    if (!wrap || !msg) return;
    var iconOk   = '<svg viewBox="0 0 24 24"><polyline points="20 6 9 17 4 12"/></svg>';
    var iconErr  = '<svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>';
    var t = document.createElement('div');
    t.className = 'toast ' + tipo;
    t.innerHTML = (tipo === 'ok' ? iconOk : iconErr) + '<span></span>';
    t.querySelector('span').textContent = msg; // el mensaje puede contener datos escritos por usuarios
    wrap.appendChild(t);
    setTimeout(function() { t.remove(); }, 3400);
}

// Toast de stock bajo — dura más tiempo y tiene ícono de advertencia
function showToastStock(tipo, msg) {
    var wrap = document.getElementById('toastWrap');
    if (!wrap || !msg) return;
    var icon = '<svg viewBox="0 0 24 24"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>';
    var t = document.createElement('div');
    t.className = 'toast warn';
    t.innerHTML = icon + '<span></span>';
    t.querySelector('span').textContent = msg;
    wrap.appendChild(t);
    setTimeout(function() { t.remove(); }, 6000);   // 6 segundos para leer bien
}

function toggleSidebar() { document.getElementById('sidebar')?.classList.toggle('open'); }
function toggleGourmetMobileNav() { document.getElementById('gourmetMobileMenu')?.classList.toggle('open'); }
document.addEventListener('click', function(e) {
    var sb = document.getElementById('sidebar'), tog = document.getElementById('sbToggle');
    if (sb && tog && !sb.contains(e.target) && !tog.contains(e.target)) sb.classList.remove('open');
    var gNav = document.getElementById('gourmetNavbar'), gMenu = document.getElementById('gourmetMobileMenu'), gBtn = document.getElementById('gourmetMobileToggle');
    if (gMenu && gBtn && !gMenu.contains(e.target) && !gBtn.contains(e.target)) gMenu.classList.remove('open');
    var lMenu = document.getElementById('lnavMobileMenu'), lBtn = document.getElementById('lnavMobileToggle');
    if (lMenu && lBtn && !lMenu.contains(e.target) && !lBtn.contains(e.target)) lMenu.classList.remove('open');
});

function toggleDropdown() { document.getElementById('profileDropdown').classList.toggle('show'); }
document.addEventListener('click', function(e) {
    var w = document.getElementById('profileWrap');
    if (w && !w.contains(e.target)) { var d = document.getElementById('profileDropdown'); if(d) d.classList.remove('show'); }
});

function abrirModalContrasena() {
    document.getElementById('profileDropdown').classList.remove('show');
    document.getElementById('modalContrasena').classList.add('show');
}
function cerrarModalContrasena() {
    document.getElementById('modalContrasena').classList.remove('show');
    document.getElementById('formContrasena').reset();
    document.getElementById('errConfirmar').style.display = 'none';
}
var cfm = document.getElementById('confirmarPass');
if (cfm) cfm.addEventListener('input', function() {
    var np = document.getElementById('nuevaPass').value;
    document.getElementById('errConfirmar').style.display = (this.value && this.value !== np) ? 'block' : 'none';
});
var fc = document.getElementById('formContrasena');
if (fc) fc.addEventListener('submit', function(e) {
    if (document.getElementById('nuevaPass').value !== document.getElementById('confirmarPass').value) {
        e.preventDefault(); document.getElementById('errConfirmar').style.display = 'block';
    }
});

var gs = document.getElementById('globalSearch');
if (gs) gs.addEventListener('input', function() {
    var q = this.value.toLowerCase().trim();
    var rows = document.querySelectorAll('table tbody tr'), vis = 0;
    rows.forEach(function(tr) { var m = tr.innerText.toLowerCase().includes(q); tr.style.display = m ? '' : 'none'; if(m) vis++; });
    var cnt = document.getElementById('conteo'); if (cnt) cnt.textContent = vis;
});

// ── Registro de Service Worker PWA para Tablets de Meseros y Cocina ──
if ('serviceWorker' in navigator) {
    window.addEventListener('load', function() {
        navigator.serviceWorker.register('/sw.js').catch(function(err) {
            console.log('SW registration note:', err);
        });
    });
}
