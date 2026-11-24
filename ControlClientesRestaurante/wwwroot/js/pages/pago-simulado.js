window.SIMULAR_WOMPI = PAGO_CFG.simularWompi;
var swMetodoActual = 'Tarjeta';

function swMetodo(m) {
    swMetodoActual = m;
    document.querySelectorAll('.sw-tab').forEach(function (b) { b.classList.toggle('active', b.dataset.m === m); });
    ['Tarjeta', 'Nequi', 'PSE'].forEach(function (x) { document.getElementById('sw' + x).style.display = x === m ? 'block' : 'none'; });
    document.getElementById('swErr').style.display = 'none';
}

function swLlenar(tipo) {
    if (swMetodoActual === 'Tarjeta') {
        document.getElementById('swNumero').value = tipo === 'ok' ? '4242 4242 4242 4242' : '4111 1111 1111 1111';
        document.getElementById('swNombre').value = document.getElementById('pNombre')?.value || 'Cliente de Prueba';
        document.getElementById('swExp').value = '12/30';
        document.getElementById('swCvc').value = '123';
    } else if (swMetodoActual === 'Nequi') {
        document.getElementById('swCel').value = tipo === 'ok' ? '3991111111' : '3992222222';
    }
}

function swError(msg) {
    var e = document.getElementById('swErr');
    e.textContent = msg;
    e.style.display = 'block';
}

function abrirSimuladorWompi() {
    document.getElementById('swFormulario').style.display = 'block';
    document.getElementById('swProcesando').style.display = 'none';
    document.getElementById('swResultado').style.display = 'none';
    document.getElementById('swErr').style.display = 'none';
    document.getElementById('swBackdrop').classList.add('open');
}

function cerrarSimuladorWompi() {
    document.getElementById('swBackdrop').classList.remove('open');
    var btn = document.getElementById('btnWompiLauncher');
    if (btn) btn.disabled = false;
}

// Reglas del sandbox de Wompi: 4242… y 3991111111 aprueban; 4111… y 3992222222 rechazan
function swResultadoPrueba() {
    if (swMetodoActual === 'Tarjeta') {
        var n = document.getElementById('swNumero').value.replace(/\D/g, '');
        var exp = document.getElementById('swExp').value.trim();
        var cvc = document.getElementById('swCvc').value.trim();
        if (n.length < 13 || n.length > 19) return { error: 'Revisa el número de tarjeta.' };
        if (!document.getElementById('swNombre').value.trim()) return { error: 'Escribe el nombre que aparece en la tarjeta.' };
        if (!/^(0[1-9]|1[0-2])\/\d{2}$/.test(exp)) return { error: 'El vencimiento debe tener el formato MM/AA.' };
        if (!/^\d{3,4}$/.test(cvc)) return { error: 'Revisa el CVC.' };
        return { resultado: n.indexOf('4111') === 0 ? 'DECLINED' : 'APPROVED' };
    }
    if (swMetodoActual === 'Nequi') {
        var c = document.getElementById('swCel').value.replace(/\D/g, '');
        if (c.length !== 10) return { error: 'El celular de Nequi debe tener 10 dígitos.' };
        return { resultado: c === '3992222222' ? 'DECLINED' : 'APPROVED' };
    }
    return { resultado: document.getElementById('swBanco').value === 'no' ? 'DECLINED' : 'APPROVED' };
}

function swPagar() {
    var r = swResultadoPrueba();
    if (r.error) { swError(r.error); return; }

    document.getElementById('swFormulario').style.display = 'none';
    document.getElementById('swProcesando').style.display = 'block';

    setTimeout(function () {
        document.getElementById('swProcesando').style.display = 'none';
        var aprobado = r.resultado === 'APPROVED';
        var ico = document.getElementById('swIco');
        ico.style.background = aprobado ? '#00B37E' : '#D92D20';
        ico.textContent = aprobado ? '✓' : '✕';
        document.getElementById('swMsg').textContent = aprobado ? 'Transacción aprobada' : 'Transacción rechazada';
        document.getElementById('swSub').textContent = aprobado
            ? 'Wompi (simulado) aprobó tu pago.'
            : 'Wompi (simulado) rechazó el pago. Puedes intentar con otro método.';
        document.getElementById('swResultado').style.display = 'block';
        document.getElementById('swContinuar').onclick = function () { swEnviar(r.resultado); };
    }, 1800);
}

// Envía el resultado al servidor: es quien registra el pago, factura el pedido y genera el comprobante
function swEnviar(resultado) {
    var token = document.querySelector('#formPasarela input[name="__RequestVerificationToken"]');
    var campos = {
        __RequestVerificationToken: token ? token.value : '',
        tipoOrigen: PAGO_CFG.tipoOrigen,
        origenId: PAGO_CFG.origenId == null ? '' : String(PAGO_CFG.origenId),
        metodo: swMetodoActual,
        resultado: resultado,
        nombreCliente: document.getElementById('pNombre')?.value || '',
        correo: document.getElementById('pCorreo')?.value || '',
        telefono: document.getElementById('pTel')?.value || ''
    };
    var f = document.createElement('form');
    f.method = 'post';
    f.action = PAGO_CFG.urlAprobarSimulado;
    Object.keys(campos).forEach(function (k) {
        var i = document.createElement('input');
        i.type = 'hidden'; i.name = k; i.value = campos[k];
        f.appendChild(i);
    });
    document.body.appendChild(f);
    f.submit();
}

document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') cerrarSimuladorWompi();
});
