function abrirModalAuthHome(tab) {
    document.getElementById('homeAuthModalOverlay').classList.add('show');
    cambiarHomeAuthTab(tab || 'login');
}
function cerrarModalAuthHome() {
    document.getElementById('homeAuthModalOverlay').classList.remove('show');
    var fb = document.getElementById('homeAuthFeedback');
    fb.className = 'auth-feedback';
    fb.textContent = '';
}
function cambiarHomeAuthTab(tab) {
    var isLogin = tab === 'login';
    document.getElementById('homeTabLogin').classList.toggle('active', isLogin);
    document.getElementById('homeTabReg').classList.toggle('active', !isLogin);
    document.getElementById('homePaneLogin').classList.toggle('active', isLogin);
    document.getElementById('homePaneReg').classList.toggle('active', !isLogin);
}
function ejecutarLoginHome(e) {
    e.preventDefault();
    var u = document.getElementById('hUsuario').value.trim();
    var p = document.getElementById('hPass').value;
    var fb = document.getElementById('homeAuthFeedback');
    fetch('/Login/LoginAjax', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ usuario: u, contrasena: p, password: p, contraseña: p, returnUrl: '/Reserva/Nueva' })
    })
    .then(function(res) { return res.json(); })
    .then(function(data) {
        if (data.ok) {
            window.location.href = data.redirectUrl || '/Reserva/Nueva';
        } else {
            fb.className = 'auth-feedback error';
            fb.textContent = data.mensaje;
        }
    });
}
function ejecutarRegHome(e) {
    e.preventDefault();
    var nom  = document.getElementById('hrNom').value.trim();
    var tDoc = document.getElementById('hrTipoDoc').value;
    var doc  = document.getElementById('hrDoc').value.trim();
    var fn   = document.getElementById('hrFechaNac').value;
    var cor  = document.getElementById('hrCorreo').value.trim();
    var tel  = document.getElementById('hrTel').value.trim();
    var usu  = document.getElementById('hrUsu').value.trim();
    var pas  = document.getElementById('hrPass').value;
    var cnf  = document.getElementById('hrConf').value;
    var fb   = document.getElementById('homeAuthFeedback');
    if (pas !== cnf) {
        fb.className = 'auth-feedback error';
        fb.textContent = 'Las contraseñas no coinciden.';
        return;
    }
    fetch('/Login/RegistroAjax', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
            nombreCompleto: nom, 
            tipoDocumento: tDoc,
            documento: doc, 
            fechaNacimiento: fn,
            correo: cor,
            telefono: tel, 
            usuario: usu, 
            contraseña: pas, 
            confirmarContraseña: cnf, 
            returnUrl: '/Reserva/Nueva' 
        })
    })
    .then(function(res) { return res.json(); })
    .then(function(data) {
        if (data.ok) {
            window.location.href = data.redirectUrl || '/Login/RegistroExitoso';
        } else {
            fb.className = 'auth-feedback error';
            fb.textContent = data.mensaje;
        }
    });
}
document.getElementById('homeAuthModalOverlay').addEventListener('click', function(e) {
    if (e.target === this) cerrarModalAuthHome();
});

// ══════════════════════════════════════════════════════════════════
// ══ MODAL DE RESEÑAS EN EL HOME (AJAX) ═════════════════════════════
// ══════════════════════════════════════════════════════════════════

let homeRatingActual = 5;
const homeDescripcionesEstrellas = {
    1: "Por mejorar (1 de 5 estrellas)",
    2: "Regular (2 de 5 estrellas)",
    3: "Buena (3 de 5 estrellas)",
    4: "Muy buena (4 de 5 estrellas)",
    5: "Excelente (5 de 5 estrellas) — ¡Insuperable!"
};

function abrirModalResenaHome() {
    const overlay = document.getElementById('homeModalResenaOverlay');
    if (overlay) {
        overlay.classList.add('open');
        document.body.style.overflow = 'hidden';
        seleccionarEstrellaHome(5);
        const inputNom = document.getElementById('homeResenaNombre');
        if (inputNom && !inputNom.value) inputNom.focus();
    }
}

function cerrarModalResenaHome() {
    const overlay = document.getElementById('homeModalResenaOverlay');
    if (overlay) {
        overlay.classList.remove('open');
        document.body.style.overflow = '';
        const fb = document.getElementById('homeResenaFeedback');
        if (fb) {
            fb.style.display = 'none';
            fb.className = 'resena-feedback-box';
        }
    }
}

function clickOverlayResenaHome(e) {
    if (e.target === document.getElementById('homeModalResenaOverlay')) {
        cerrarModalResenaHome();
    }
}

function seleccionarEstrellaHome(val) {
    homeRatingActual = val;
    const inp = document.getElementById('homeInputRating');
    if (inp) inp.value = val;
    const txt = document.getElementById('homeStarPickerTxt');
    if (txt) txt.textContent = homeDescripcionesEstrellas[val] || `${val} estrellas`;
    actualizarIconosEstrellasHome(val);
}

function hoverEstrellaHome(val) {
    actualizarIconosEstrellasHome(val);
    const txt = document.getElementById('homeStarPickerTxt');
    if (txt) txt.textContent = homeDescripcionesEstrellas[val] || `${val} estrellas`;
}

function resetEstrellasHome() {
    actualizarIconosEstrellasHome(homeRatingActual);
    const txt = document.getElementById('homeStarPickerTxt');
    if (txt) txt.textContent = homeDescripcionesEstrellas[homeRatingActual] || `${homeRatingActual} estrellas`;
}

function actualizarIconosEstrellasHome(val) {
    const icons = document.querySelectorAll('#homeStarPickerIcons .btn-pick-star');
    icons.forEach(btn => {
        const star = parseInt(btn.getAttribute('data-star'));
        if (star <= val) btn.classList.add('active');
        else btn.classList.remove('active');
    });
}

function actualizarContadorHome(textarea) {
    const count = textarea.value.length;
    const counter = document.getElementById('homeCharCounter');
    if (counter) counter.textContent = `${count} / 1000`;
}

// Escapa texto del usuario antes de insertarlo con innerHTML (prevención de XSS)
function escapeHtml(valor) {
    return String(valor ?? '').replace(/[&<>"']/g, function (c) {
        return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
    });
}

async function enviarResenaHomeAjax(e) {
    e.preventDefault();
    const fb = document.getElementById('homeResenaFeedback');
    const btn = document.getElementById('homeBtnEnviarResena');

    const nombre = document.getElementById('homeResenaNombre').value.trim();
    const correo = document.getElementById('homeResenaCorreo').value.trim();
    const calificacion = parseInt(document.getElementById('homeInputRating').value) || 5;
    const tipoExp = document.getElementById('homeResenaTipoExp').value;
    const plato = document.getElementById('homeResenaPlato').value.trim();
    const comentario = document.getElementById('homeResenaComentario').value.trim();

    if (nombre.length < 2) {
        fb.style.display = 'block';
        fb.className = 'resena-feedback-box error';
        fb.textContent = 'Por favor ingresa tu nombre completo.';
        return;
    }

    if (comentario.length < 5) {
        fb.style.display = 'block';
        fb.className = 'resena-feedback-box error';
        fb.textContent = 'Tu reseña debe tener al menos 5 caracteres.';
        return;
    }

    btn.disabled = true;
    btn.style.opacity = '0.6';

    try {
        const resp = await fetch('/Resena/CrearApi', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                nombreCliente: nombre,
                correo: correo,
                calificacion: calificacion,
                tipoExperiencia: tipoExp,
                platoRecomendado: plato,
                comentario: comentario
            })
        });

        const data = await resp.json();

        if (data.ok) {
            fb.style.display = 'block';
            fb.className = 'resena-feedback-box success';
            fb.textContent = data.mensaje;

            const grid = document.getElementById('homeResenasGrid');
            if (grid && data.resena) {
                let starsHtml = '';
                for (let s = 1; s <= 5; s++) {
                    starsHtml += `
                        <svg width="15" height="15" viewBox="0 0 24 24" style="${s <= data.resena.calificacion ? 'fill:#F59E0B;stroke:#D97706;' : 'fill:rgba(0,0,0,0.1);stroke:rgba(0,0,0,0.2);'}">
                            <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
                        </svg>
                    `;
                }

                const tagDish = data.resena.platoRecomendado ? `
                    <span style="font-size:0.7rem;background:rgba(244, 168, 23,0.14);color:var(--gold-text);border:1px solid rgba(244, 168, 23,0.3);padding:2px 7px;border-radius:6px;font-weight:600;">
                        Recomendó: <strong>${escapeHtml(data.resena.platoRecomendado)}</strong>
                    </span>
                ` : '';

                const newCard = document.createElement('div');
                newCard.className = 'resena-card-item';
                newCard.style.animation = 'fadeInModal 300ms ease-out';

                const initial = data.resena.nombreCliente ? data.resena.nombreCliente.charAt(0).toUpperCase() : 'C';

                newCard.innerHTML = `
                    <div>
                        <div style="display:flex;align-items:flex-start;margin-bottom:0.75rem;">
                            <div style="display:flex;align-items:center;gap:0.85rem;width:100%;min-width:0;">
                                <div style="width:42px;height:42px;min-width:42px;min-height:42px;aspect-ratio:1/1;flex-shrink:0;border-radius:50%;background:linear-gradient(135deg, #1F120C, var(--gold));color:#FFFFFF;font-weight:800;font-size:1rem;line-height:1;display:flex;align-items:center;justify-content:center;box-shadow:0 2px 8px rgba(31, 18, 12,0.25);">
                                    ${escapeHtml(initial)}
                                </div>
                                <div style="display:flex;flex-direction:column;gap:0.2rem;min-width:0;flex:1;">
                                    <div style="font-size:0.92rem;font-weight:700;color:var(--text);display:flex;align-items:center;flex-wrap:wrap;gap:6px;line-height:1.25;">
                                        <span>${escapeHtml(data.resena.nombreCliente)}</span>
                                        <span style="font-size:0.65rem;background:rgba(16,185,129,0.14);color:var(--success);border:1px solid rgba(16,185,129,0.35);border-radius:999px;padding:1px 6px;font-weight:700;display:inline-flex;align-items:center;gap:2px;flex-shrink:0;">
                                            <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><polyline points="20 6 9 17 4 12"/></svg>
                                            Verificado
                                        </span>
                                    </div>
                                    <div style="display:flex;align-items:center;gap:3px;margin:1px 0;">${starsHtml}</div>
                                    <div style="font-size:0.74rem;color:var(--muted);line-height:1.2;">Justo ahora</div>
                                </div>
                            </div>
                        </div>
                        <div style="display:flex;flex-wrap:wrap;gap:0.4rem;margin-bottom:0.75rem;">
                            <span style="font-size:0.7rem;background:var(--surface-2);color:var(--muted);padding:2px 7px;border-radius:6px;font-weight:600;">
                                ${escapeHtml(data.resena.tipoExperiencia)}
                            </span>
                            ${tagDish}
                        </div>
                        <p style="font-size:0.88rem;color:var(--text);line-height:1.55;margin:0;font-style:italic;">
                            "${escapeHtml(data.resena.comentario)}"
                        </p>
                    </div>
                `;

                grid.insertBefore(newCard, grid.firstChild);
            }

            if (data.stats) {
                const score = document.getElementById('homeScoreDisplay');
                if (score) score.textContent = data.stats.promedio.toFixed(1);
                const totalTxt = document.getElementById('homeTotalReviewsDisplay');
                if (totalTxt) totalTxt.innerHTML = `Basado en <strong>${data.stats.totalResenas}</strong> experiencias verificadas`;
            }

            document.getElementById('homeFormNuevaResena').reset();
            actualizarContadorHome(document.getElementById('homeResenaComentario'));

            setTimeout(() => {
                cerrarModalResenaHome();
            }, 1400);
        } else {
            fb.style.display = 'block';
            fb.className = 'resena-feedback-box error';
            fb.textContent = data.mensaje || 'Error al enviar reseña.';
        }
    } catch (err) {
        console.error(err);
        fb.style.display = 'block';
        fb.className = 'resena-feedback-box error';
        fb.textContent = 'Error de conexión al enviar la reseña.';
    } finally {
        btn.disabled = false;
        btn.style.opacity = '1';
    }
}
