    const googleClientIdConfig = document.getElementById('ingreso-config')?.dataset.googleClientId || '';

function abrirModalIngreso() {
    const overlay = document.getElementById('modalIngresoOverlay');
    if (overlay) {
        overlay.classList.add('active');
        document.body.style.overflow = 'hidden';
        mostrarVista('main');
        setTimeout(ofrecerSelectorAlAbrir, 1500);
        // Activar / refrescar selector oficial de cuentas de Google en el dispositivo
        inicializarGoogleIdentityServices();
    }
}

function cerrarModalIngreso() {
    const overlay = document.getElementById('modalIngresoOverlay');
    if (overlay) {
        overlay.classList.remove('active');
        document.body.style.overflow = '';
        limpiarKfcFeedbacks();
        mostrarVista('main');
    }
}

// ── 2. Flujo Correo / Contraseña ──
async function ejecutarLoginClaveKfc(e) {
    e.preventDefault();
    const usuario = document.getElementById('kfcUsuarioInput')?.value.trim()
        || document.getElementById('kfcEmailInput')?.value.trim();
    const pass = document.getElementById('kfcPassInput')?.value;
    const btn = document.getElementById('btnLoginClaveSubmit');

    if (!usuario || !pass) {
        mostrarKfcFeedback('Por favor digita tu correo o nombre de usuario y tu contraseña.', false);
        return;
    }

    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<span>Ingresando...</span>';
    }

    try {
        const resp = await fetch('/Login/LoginAjax', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ usuario, password: pass, returnUrl: window.location.pathname })
        });
        const data = await resp.json();

        if (!resp.ok || !data.ok) {
            mostrarKfcFeedback(data.mensaje || 'Credenciales inválidas.', false);
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = '<span>Ingresar a mi cuenta</span>';
            }
            return;
        }

        mostrarKfcFeedback('¡Ingreso exitoso al Club Saborator! Redirigiendo...', true);
        setTimeout(() => {
            window.location.href = data.redirectUrl || '/';
        }, 600);
    } catch (err) {
        console.error(err);
        mostrarKfcFeedback('Error de conexión al ingresar.', false);
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = '<span>Ingresar a mi cuenta</span>';
        }
    }
}

// ── 3. Flujo Oficial Google Identity Services (GSI SDK) ──
// Dos caminos oficiales de Google, que conviven:
//  · el botón oficial de Google (invisible, encima de nuestra pastilla), si Google lo dibuja;
//  · el selector de cuentas "One Tap", que se muestra al abrir el modal y al pulsar la pastilla si el botón no está.
// Se inicializa UNA sola vez y nunca hay dos solicitudes de One Tap a la vez.
let gsiIniciado = false, gsiIntentos = 0, gsiBotonVisible = false, gsiSelectorPendiente = false;
const esEntornoLocal = ['localhost', '127.0.0.1'].includes(location.hostname);

function actualizarEstadoGoogle() {
    const contenedor = document.getElementById('googleOfficialBtnContainer');
    const marco = contenedor ? contenedor.querySelector('iframe') : null;
    gsiBotonVisible = !!marco && marco.getBoundingClientRect().width > 0;
    if (contenedor) contenedor.classList.toggle('listo', gsiBotonVisible);
    return gsiBotonVisible;
}

// Motivo por el que Google no mostró su selector, en palabras para la persona
function motivoGoogle(razon) {
    switch (razon) {
        case 'unregistered_origin':
            return esEntornoLocal
                ? 'Google no tiene registrada esta dirección (' + location.origin + '). Agrégala en "Orígenes autorizados de JavaScript" del ID de cliente en Google Cloud.'
                : 'El ingreso con Google no está disponible en este momento. Usa tu correo electrónico.';
        case 'opt_out_or_no_session':
            return 'No hay una sesión de Google abierta en este navegador. Inicia sesión en Google (accounts.google.com) e intenta de nuevo, o usa tu correo.';
        case 'suppressed_by_user':
            return 'Google ocultó el selector porque lo cerraste hace poco. Espera unos minutos, o usa tu correo.';
        case 'browser_not_supported':
            return 'Este navegador no es compatible con el acceso con Google. Usa tu correo electrónico.';
        default:
            return 'Google no pudo mostrar el selector de cuentas. Intenta de nuevo, o usa tu correo.';
    }
}

// Muestra el selector de cuentas de Google. Con avisar=true explica el motivo si no aparece.
function mostrarSelectorGoogle(avisar) {
    if (!gsiIniciado || gsiSelectorPendiente) return;
    gsiSelectorPendiente = true;
    try {
        google.accounts.id.prompt(function (notificacion) {
            if (notificacion.isNotDisplayed && notificacion.isNotDisplayed()) {
                gsiSelectorPendiente = false;
                const razon = notificacion.getNotDisplayedReason ? notificacion.getNotDisplayedReason() : '';
                console.info('Google One Tap no visible:', razon);
                if (avisar) mostrarKfcFeedback(motivoGoogle(razon), false);
            } else if ((notificacion.isSkippedMoment && notificacion.isSkippedMoment()) || (notificacion.isDismissedMoment && notificacion.isDismissedMoment())) {
                gsiSelectorPendiente = false;
            }
        });
    } catch (err) {
        gsiSelectorPendiente = false;
        console.warn('Google One Tap:', err);
    }
}

function inicializarGoogleIdentityServices() {
    const cId = googleClientIdConfig || '';
    if (!cId || gsiIniciado) return;

    if (typeof google !== 'undefined' && google.accounts && google.accounts.id) {
        gsiIniciado = true;
        try {
            google.accounts.id.initialize({
                client_id: cId,
                callback: handleGoogleCredentialResponse,
                auto_select: false,
                cancel_on_tap_outside: true
            });
            const interior = document.getElementById('googleOfficialBtnInner');
            if (interior && !interior.hasChildNodes()) {
                const ancho = Math.min(400, Math.round((document.getElementById('googleWrap') || {}).clientWidth || 340));
                google.accounts.id.renderButton(interior, {
                    type: 'standard', shape: 'pill', theme: 'outline', text: 'continue_with', size: 'large', logo_alignment: 'left', width: ancho
                });
            }
            // Cuando Google dibuja su botón oficial, pasa a recibir el clic (se revisa durante unos segundos)
            let revisiones = 0;
            const revisar = setInterval(function () { if (actualizarEstadoGoogle() || ++revisiones > 24) clearInterval(revisar); }, 500);
            // Si el modal ya está abierto, se ofrece el selector de cuentas
            if (document.getElementById('modalIngresoOverlay')?.classList.contains('active')) setTimeout(ofrecerSelectorAlAbrir, 1500);
        } catch (err) {
            console.warn('Google GSI init notice:', err);
        }
    } else if (gsiIntentos++ < 50) {
        // El SDK de Google aún se está descargando: se reintenta durante unos 10 segundos
        setTimeout(inicializarGoogleIdentityServices, 200);
    }
}

// Al abrir el modal: si Google dibujó su botón oficial no hace falta más; si no, se abre el selector de cuentas
function ofrecerSelectorAlAbrir() {
    if (!gsiIniciado) return;
    if (!actualizarEstadoGoogle()) mostrarSelectorGoogle(false);
}

// Clic en la pastilla: normalmente lo recibe el botón oficial que la cubre; si no está, se abre el selector de cuentas
function iniciarGoogleAuthOficial() {
    limpiarKfcFeedbacks();
    if (typeof google === 'undefined' || !google.accounts || !google.accounts.id || !gsiIniciado) {
        mostrarKfcFeedback('Cargando el selector oficial de Google... Intenta de nuevo en unos segundos.', false);
        inicializarGoogleIdentityServices();
        return;
    }
    gsiSelectorPendiente = false;   // un clic explícito siempre pide el selector de nuevo
    mostrarSelectorGoogle(true);
}

// Callback oficial que recibe el Token JWT emitido por Google accounts.google.com
async function handleGoogleCredentialResponse(response) {
    if (!response || !response.credential) return;

    mostrarKfcFeedback('Autenticando con cuenta de Google...', true);
    try {
        const resp = await fetch('/Login/GoogleCallback', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({
                credential: response.credential,
                returnUrl: window.location.pathname
            })
        });
        const data = await resp.json();

        if (data && data.ok) {
            mostrarKfcFeedback(data.mensaje || '¡Acceso concedido con Google!', true);
            setTimeout(() => {
                window.location.href = data.redirectUrl || '/Carta';
            }, 600);
        } else {
            mostrarKfcFeedback(data.mensaje || 'Error al autenticar tu cuenta de Google.', false);
        }
    } catch (err) {
        console.error('Error GoogleCallback:', err);
        mostrarKfcFeedback('Error de comunicación al autenticar con Google.', false);
    }
}

// ── Vistas del modal: 'main' (usuario o correo + contraseña, Google) y 'registro' (crear cuenta) ──
function mostrarVista(nombre) {
    const volvioDeRegistro = nombre === 'correo';          // 'correo' se conserva como alias de 'main' (enlace "Inicia sesión")
    if (volvioDeRegistro) nombre = 'main';
    const vistas = { main: 'viewClubSaboratorMain', registro: 'viewClubSaboratorRegistro' };
    limpiarKfcFeedbacks();
    Object.entries(vistas).forEach(([clave, id]) => { const el = document.getElementById(id); if (el) el.hidden = clave !== nombre; });
    const foco = volvioDeRegistro ? 'kfcUsuarioInput' : (nombre === 'registro' ? 'kfcRegNombre' : '');
    if (foco) setTimeout(() => document.getElementById(foco)?.focus(), 80);
}
// (se conserva por compatibilidad con enlaces anteriores)
function mostrarVistaRegistro(mostrar) { mostrarVista(mostrar ? 'registro' : 'main'); }

// ── Crear cuenta con nombre, apellido, usuario, correo y contraseña ──
async function ejecutarRegistroKfc(e) {
    e.preventDefault();
    const v = id => (document.getElementById(id)?.value || '');
    const nombre = v('kfcRegNombre').trim(), apellido = v('kfcRegApellido').trim();
    const usuario = v('kfcRegUsuario').trim(), correo = v('kfcRegCorreo').trim();
    const pass = v('kfcRegPass'), conf = v('kfcRegPassConf');
    const btn = document.getElementById('btnRegistroSubmit');

    let error = '';
    if (nombre.length < 2) error = 'Escribe tu nombre.';
    else if (apellido.length < 2) error = 'Escribe tu apellido.';
    else if (!/^[A-Za-z0-9._-]{3,30}$/.test(usuario)) error = 'El usuario debe tener entre 3 y 30 caracteres: letras, números, punto, guion o guion bajo.';
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(correo)) error = 'Escribe un correo electrónico válido.';
    else if (pass.length < 8) error = 'La contraseña debe tener al menos 8 caracteres.';
    else if (pass !== conf) error = 'Las contraseñas no coinciden.';
    if (error) { mostrarKfcFeedback(error, false); return; }

    const reabrir = () => { if (btn) { btn.disabled = false; btn.innerHTML = '<span>Crear mi cuenta</span>'; } };
    if (btn) { btn.disabled = true; btn.innerHTML = '<span>Creando cuenta...</span>'; }

    try {
        const token = document.querySelector('#formKfcRegistro input[name="__RequestVerificationToken"]')?.value || '';
        const resp = await fetch('/Login/RegistroRapidoAjax', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body: JSON.stringify({ nombre, apellido, usuario, correo, contrasena: pass, confirmar: conf, returnUrl: window.location.pathname })
        });
        const data = await resp.json();
        if (!resp.ok || !data.ok) {
            mostrarKfcFeedback(data.mensaje || 'No se pudo crear la cuenta.', false);
            reabrir();
            return;
        }
        mostrarKfcFeedback('¡Cuenta creada! Bienvenido al Club Saborator. Redirigiendo...', true);
        setTimeout(() => { window.location.href = data.redirectUrl || '/'; }, 700);
    } catch (err) {
        console.error(err);
        mostrarKfcFeedback('Error de conexión al crear la cuenta.', false);
        reabrir();
    }
}

function mostrarKfcFeedback(msg, esExito) {
    // El mensaje se muestra en la vista que está abierta (ingreso o registro)
    const registroAbierto = document.getElementById('viewClubSaboratorRegistro');
    const box = document.getElementById(registroAbierto && !registroAbierto.hidden ? 'kfcFeedbackBoxReg' : 'kfcFeedbackBox');
    if (box) {
        box.textContent = msg;
        box.className = 'kfc-feedback-box ' + (esExito ? 'success' : 'error');
        box.style.display = 'block';
    }
}

function limpiarKfcFeedbacks() {
    document.querySelectorAll('.kfc-feedback-box').forEach(b => {
        b.style.display = 'none';
        b.textContent = '';
    });
}

// Cerrar modal al presionar Escape o clic fuera
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        const overlay = document.getElementById('modalIngresoOverlay');
        if (overlay && overlay.classList.contains('active')) {
            cerrarModalIngreso();
        }
    }
});

document.getElementById('modalIngresoOverlay')?.addEventListener('click', function (e) {
    if (e.target === this) {
        cerrarModalIngreso();
    }
});

// "Iniciar sesión con otra cuenta": el inicio se abre con ?ingresar=1 y muestra este modal
(function () {
    var params = new URLSearchParams(location.search);
    if (!params.has('ingresar')) return;
    params.delete('ingresar');
    var query = params.toString();
    history.replaceState(null, '', location.pathname + (query ? '?' + query : '') + location.hash);
    document.addEventListener('DOMContentLoaded', abrirModalIngreso);
})();

// Auto-inicializar Google GSI cuando el DOM y el SDK estén listos
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', inicializarGoogleIdentityServices);
} else {
    inicializarGoogleIdentityServices();
}
