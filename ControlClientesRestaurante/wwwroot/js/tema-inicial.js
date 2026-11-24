// Aplica el tema antes de pintar la página para evitar el parpadeo claro/oscuro. El tema del perfil llega en data-tema-perfil del <html>.
(function() {
    try {
        var oscuros = { dark: 1, noche: 1, dorado: 1, esmeralda: 1, sunset: 1 };
        var perfil = document.documentElement.dataset.temaPerfil || '';
        var guardado = localStorage.getItem('saborator_theme');
        var t = guardado
            || ((perfil && perfil !== 'clasico') ? perfil : '')
            || ((window.matchMedia && matchMedia('(prefers-color-scheme: dark)').matches) ? 'dark' : 'light');
        var h = document.documentElement;
        if (oscuros[t]) h.setAttribute('data-theme', t); else h.removeAttribute('data-theme');
        var noche = (t === 'dark' || t === 'noche');
        h.classList.toggle('dark', noche);
        h.style.colorScheme = oscuros[t] ? 'dark' : 'light';
    } catch (e) {}
})();
