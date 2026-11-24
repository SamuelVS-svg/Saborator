const PAGO_CFG = JSON.parse(document.getElementById('pago-config').textContent);
var wompiKey = PAGO_CFG.wompiKey;
var wompiMontoCentavos = PAGO_CFG.montoCentavos;
var wompiRef = PAGO_CFG.referencia;
var wompiIntegrity = PAGO_CFG.integridad;
var wompiRedirect = window.location.origin + '/Pago/RetornoWompi';

var watchdogTimer = null;

async function ejecutarWidgetWompiCheckout() {
    // Sin llaves reales de pruebas: se abre el checkout simulado de Wompi
    if (window.SIMULAR_WOMPI) { abrirSimuladorWompi(); return; }
    var btn = document.getElementById('btnWompiLauncher');
    var btnTexto = document.getElementById('btnWompiTexto');
    var fallback = document.getElementById('wompiFallbackNotice');

    // Indicar estado de carga visual
    if (btn) {
        btn.disabled = true;
        btn.style.opacity = '0.85';
        btnTexto.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true" style="display:inline-block;width:1.1rem;height:1.1rem;border:2px solid currentColor;border-right-color:transparent;border-radius:50%;margin-right:8px;vertical-align:text-bottom;"></span> Generando sesión segura Wompi...';
    }

    // Watchdog de 4.5 segundos: Si el popup no abre o hay bloqueo por adblock/CORS
    if (watchdogTimer) clearTimeout(watchdogTimer);
    watchdogTimer = setTimeout(function () {
        if (btn) {
            btn.disabled = false;
            btn.style.opacity = '1';
            btnTexto.textContent = 'Reintentar Wompi o Pagar Directo';
        }
        if (fallback) {
            fallback.style.display = 'block';
        }
    }, 4500);

    try {
        // 1. Obtener sesión firmada desde el servidor (Blindaje: el cliente NO puede manipular montos ni referencias)
        var requestPayload = {
            tipoOrigen: PAGO_CFG.tipoOrigen,
            origenId: PAGO_CFG.origenId,
            nombreCliente: document.getElementById('pNombre')?.value || PAGO_CFG.nombreCliente,
            correo: document.getElementById('pCorreo')?.value || PAGO_CFG.correo,
            telefono: document.getElementById('pTel')?.value || PAGO_CFG.telefono
        };

        var res = await fetch('/api/pagos/wompi-iniciar-sesion', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestPayload)
        });

        if (!res.ok) {
            var errObj = await res.json();
            throw new Error(errObj.error || 'Error al comunicarse con el servidor para iniciar la transacción.');
        }

        var sesion = await res.json();

        // 2. Verificar que el script oficial de Wompi esté cargado en la ventana
        if (typeof WidgetCheckout === 'undefined') {
            clearTimeout(watchdogTimer);
            if (btn) {
                btn.disabled = false;
                btn.style.opacity = '1';
                btnTexto.textContent = 'Pagar con Wompi';
            }
            alert('El script de Wompi está cargando o fue bloqueado por una extensión de navegador. Conmutando a pasarela directa integrada.');
            conmutarAPasarelaDirecta();
            return;
        }

        // 3. Inicializar Widget oficial Wompi con parámetros certificados por el backend
        var configWompi = {
            currency: sesion.currency,
            amountInCents: sesion.amountInCents,
            reference: sesion.reference,
            publicKey: sesion.publicKey,
            redirectUrl: sesion.redirectUrl
        };

        if (sesion.integritySignature && sesion.integritySignature.trim().length > 0) {
            configWompi.signature = { integrity: sesion.integritySignature };
        }

        if (sesion.customerData) {
            // El widget rechaza campos vacíos: solo se envían los datos que el cliente sí escribió
            var cliente = {};
            Object.keys(sesion.customerData).forEach(function (k) {
                var v = sesion.customerData[k];
                if (v !== null && v !== undefined && String(v).trim() !== '') cliente[k] = v;
            });
            if (!cliente.phoneNumber) { delete cliente.phoneNumber; delete cliente.phoneNumberPrefix; }
            if (!cliente.legalId) { delete cliente.legalIdType; }
            if (Object.keys(cliente).length > 0) configWompi.customerData = cliente;
        }

        var checkout = new WidgetCheckout(configWompi);
        checkout.open(function (result) {
            clearTimeout(watchdogTimer);
            if (btn) {
                btn.disabled = false;
                btn.style.opacity = '1';
                btnTexto.textContent = 'Pagar $' + Number(sesion.montoPesos).toLocaleString('es-CO') + ' con Wompi';
            }
            var transaction = result.transaction;
            if (transaction && transaction.status === 'APPROVED') {
                window.location.href = '/Pago/RetornoWompi?id=' + encodeURIComponent(transaction.id);
            } else if (transaction && transaction.status) {
                alert('Resultado de transacción Wompi: ' + transaction.status);
                if (transaction.status === 'DECLINED') {
                    conmutarAPasarelaDirecta();
                }
            }
        });
    } catch (e) {
        clearTimeout(watchdogTimer);
        console.error('Error al iniciar Wompi:', e);
        if (btn) {
            btn.disabled = false;
            btn.style.opacity = '1';
            btnTexto.textContent = 'Pagar con Wompi';
        }
        if (fallback) fallback.style.display = 'block';
        conmutarAPasarelaDirecta();
    }
}

function copiarTexto(texto, btn) {
    if (navigator.clipboard) {
        navigator.clipboard.writeText(texto).then(function() {
            var originalHtml = btn.innerHTML;
            btn.innerHTML = '<svg class="ico" width="1em" height="1em" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" style="vertical-align:-.15em;margin-right:.35em;flex:none"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>Copiado!';
            btn.style.filter = 'brightness(1.2)';
            setTimeout(function() {
                btn.innerHTML = originalHtml;
                btn.style.filter = '';
            }, 2200);
        }).catch(function() {
            copiarFallback(texto, btn);
        });
    } else {
        copiarFallback(texto, btn);
    }
}

function copiarFallback(texto, btn) {
    var temp = document.createElement('input');
    temp.value = texto;
    document.body.appendChild(temp);
    temp.select();
    document.execCommand('copy');
    document.body.removeChild(temp);
    var originalHtml = btn.innerHTML;
    btn.innerHTML = '<svg class="ico" width="1em" height="1em" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" style="vertical-align:-.15em;margin-right:.35em;flex:none"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>Copiado!';
    setTimeout(function() {
        btn.innerHTML = originalHtml;
    }, 2200);
}

function conmutarAPasarelaDirecta() {
    var tabTarjeta = document.querySelector(".method-tab:nth-child(2)");
    if (tabTarjeta) {
        seleccionarMetodo('Tarjeta', tabTarjeta);
    }
}

function seleccionarMetodo(metodo, btn) {
    document.getElementById('hiddenMetodoPago').value = metodo;

    document.querySelectorAll('.method-tab').forEach(function(t) { t.classList.remove('active'); });
    if (btn) btn.classList.add('active');

    document.querySelectorAll('.method-pane').forEach(function(p) { p.classList.remove('active'); });
    var targetPane = document.getElementById('pane' + metodo);
    if (targetPane) targetPane.classList.add('active');

    var secSubmitDirecto = document.getElementById('seccionSubmitDirecto');
    var btnTexto = document.getElementById('btnPagarTexto');

    if (metodo === 'Wompi') {
        if (secSubmitDirecto) secSubmitDirecto.style.display = 'none';
    } else {
        if (secSubmitDirecto) secSubmitDirecto.style.display = 'block';
        if (metodo === 'Efectivo') {
            btnTexto.textContent = 'Confirmar Solicitud de Cobro en Mesa';
        } else if (metodo === 'Nequi' || metodo === 'Daviplata' || metodo === 'PSE') {
            btnTexto.textContent = 'Confirmar Transferencia ' + (metodo === 'PSE' ? 'Bancaria' : metodo) + ' ($' + PAGO_CFG.montoTexto + ')';
        } else {
            btnTexto.textContent = 'Autorizar Pago con ' + metodo + ' ($' + PAGO_CFG.montoTexto + ')';
        }
    }
}

function formatearTarjeta(input) {
    var v = input.value.replace(/\s+/g, '').replace(/[^0-9]/gi, '');
    var matches = v.match(/\d{4,16}/g);
    var match = matches && matches[0] || '';
    var parts = [];
    for (var i = 0, len = match.length; i < len; i += 4) {
        parts.push(match.substring(i, i + 4));
    }
    input.value = parts.length ? parts.join(' ') : v;

    // Reflejar en tarjeta visual
    var numVisual = document.getElementById('visualCardNumber');
    if (numVisual) numVisual.textContent = input.value || '•••• •••• •••• ••••';

    var brandVisual = document.getElementById('visualCardBrand');
    if (brandVisual) {
        if (v.startsWith('4')) { brandVisual.textContent = 'VISA'; }
        else if (v.startsWith('5')) { brandVisual.textContent = 'MASTERCARD'; }
        else if (v.startsWith('3')) { brandVisual.textContent = 'AMEX'; }
        else { brandVisual.textContent = 'CRÉDITO / DÉBITO'; }
    }
}

function formatearExpira(input) {
    var v = input.value.replace(/[^0-9]/g, '');
    if (v.length >= 2) {
        input.value = v.substring(0, 2) + '/' + v.substring(2, 4);
    } else {
        input.value = v;
    }
    var expVisual = document.getElementById('visualCardExpiry');
    if (expVisual) expVisual.textContent = input.value || 'MM/AA';
}

var pNom = document.getElementById('pNombre');
if (pNom) {
    pNom.addEventListener('input', function() {
        var el = document.getElementById('visualCardHolder');
        if (el) el.textContent = this.value.toUpperCase() || 'TU NOMBRE';
    });
}

function demoLlenarTarjeta() {
    var num = document.getElementById('cNumero');
    var exp = document.getElementById('cExp');
    var cvv = document.getElementById('cCvv');
    if (num) { num.value = '4242 4242 4242 4242'; formatearTarjeta(num); }
    if (exp) { exp.value = '12/28'; formatearExpira(exp); }
    if (cvv) { cvv.value = '888'; }
}

function demoLlenarNequi() {
    var comp = document.getElementById('nequiComprobante');
    var cel = document.getElementById('nequiCelularEmisor');
    if (comp) comp.value = 'M' + Math.floor(1000000 + Math.random() * 9000000);
    if (cel) cel.value = '3001234567';
}

function demoLlenarDaviplata() {
    var comp = document.getElementById('daviComprobante');
    var cel = document.getElementById('daviCelularEmisor');
    if (comp) comp.value = 'DP-' + Math.floor(100000 + Math.random() * 900000);
    if (cel) cel.value = '3109876543';
}

function demoLlenarPSE() {
    var comp = document.getElementById('pseComprobante');
    if (comp) comp.value = 'BC-' + Math.floor(100000 + Math.random() * 900000);
}

function animarProcesamientoPago(e) {
    var metodoActual = document.getElementById('hiddenMetodoPago').value;
    if (metodoActual === 'Wompi') {
        e.preventDefault();
        ejecutarWidgetWompiCheckout();
        return false;
    }

    // Validación de comprobante para transferencias directas
    if (metodoActual === 'Nequi' || metodoActual === 'Daviplata' || metodoActual === 'PSE') {
        var inputComp = metodoActual === 'Nequi' ? document.getElementById('nequiComprobante') :
                        (metodoActual === 'Daviplata' ? document.getElementById('daviComprobante') : document.getElementById('pseComprobante'));
        if (inputComp && !inputComp.value.trim()) {
            e.preventDefault();
            alert('Por favor ingresa el número de comprobante o referencia de tu transferencia ' + (metodoActual === 'PSE' ? 'Bancolombia' : metodoActual) + '.');
            inputComp.focus();
            return false;
        }
    }

    var btn = document.getElementById('btnPagarSubmit');
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true" style="display:inline-block;width:1.2rem;height:1.2rem;border:2px solid currentColor;border-right-color:transparent;border-radius:50%;margin-right:10px;vertical-align:text-bottom;"></span> Verificando transferencia y registrando comprobante...';
        btn.style.opacity = '0.85';
        btn.style.pointerEvents = 'none';
    }
    return true;
}
