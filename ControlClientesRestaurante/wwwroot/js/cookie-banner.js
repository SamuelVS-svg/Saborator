(function() {
    const STORAGE_KEY = 'saborator_cookie_consent';

    function obtenerConsentimiento() {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            return raw ? JSON.parse(raw) : null;
        } catch(e) {
            return null;
        }
    }

    function guardarConsentimiento(esenciales, preferencias, analiticas) {
        const data = {
            esenciales: true,
            preferencias: !!preferencias,
            analiticas: !!analiticas,
            fecha: new Date().toISOString()
        };
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
            localStorage.setItem('lamesa_cookie_accepted', 'true');
        } catch(e) {}

        const banner = document.getElementById('cookieBanner');
        if (banner) banner.style.display = 'none';

        cerrarModalPreferenciasCookies();
    }

    window.aceptarTodasCookies = function() {
        guardarConsentimiento(true, true, true);
        if (window.showToast) {
            showToast('ok', 'Preferencias de cookies guardadas: Todas habilitadas.');
        }
    };

    window.aceptarSoloNecesarias = function() {
        guardarConsentimiento(true, false, false);
        if (window.showToast) {
            showToast('ok', 'Preferencias guardadas: Solo cookies técnicas esenciales.');
        }
    };

    window.abrirModalPreferenciasCookies = function() {
        const consent = obtenerConsentimiento();
        const togglePref = document.getElementById('toggleCookiesPreferencias');
        const toggleAna = document.getElementById('toggleCookiesAnaliticas');

        if (togglePref) togglePref.checked = consent ? consent.preferencias : true;
        if (toggleAna) toggleAna.checked = consent ? consent.analiticas : false;

        const modal = document.getElementById('cookieModalOverlay');
        if (modal) modal.style.display = 'flex';
    };

    window.cerrarModalPreferenciasCookies = function() {
        const modal = document.getElementById('cookieModalOverlay');
        if (modal) modal.style.display = 'none';
    };

    window.guardarPreferenciasPersonalizadas = function() {
        const pref = document.getElementById('toggleCookiesPreferencias')?.checked ?? true;
        const ana = document.getElementById('toggleCookiesAnaliticas')?.checked ?? false;
        guardarConsentimiento(true, pref, ana);
        if (window.showToast) {
            showToast('ok', 'Preferencias de cookies personalizadas guardadas.');
        }
    };

    window.abrirConfiguracionCookies = window.abrirModalPreferenciasCookies;

    document.addEventListener('DOMContentLoaded', () => {
        const consent = obtenerConsentimiento();
        if (!consent) {
            const banner = document.getElementById('cookieBanner');
            if (banner) banner.style.display = 'block';
        }
    });
})();
