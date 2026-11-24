const _cfgCheckout = document.getElementById('checkout-config')?.dataset || {};
const esCajeroOAdmin = _cfgCheckout.esCajeroAdmin === 'true';
let tipoEntregaActual = _cfgCheckout.tipoDefecto || 'Domicilio';
let costoEnvio = 4000;

function toggleMapaCheckout() {
    const box = document.getElementById('contenedorMapaCheckout');
    const txt = document.getElementById('btnTxtMapaCheckout');
    if (box.style.display === 'none' || !box.style.display) {
        box.style.display = 'block';
        txt.textContent = 'Ocultar Mapa';
    } else {
        box.style.display = 'none';
        txt.textContent = 'Ver Mapa';
    }
}

function fijarTarifaManualCheckout(tarifa) {
    if (!esCajeroOAdmin) return;
    const inp = document.getElementById('inputFeeManualCheckout');
    if (inp) inp.value = tarifa;
    modificarTarifaManualCheckout(tarifa);
}

function modificarTarifaManualCheckout(val) {
    if (!esCajeroOAdmin) return;
    const tarifa = Math.max(0, parseInt(val) || 0);
    document.getElementById('inputCostoEnvio').value = tarifa;
    costoEnvio = (tipoEntregaActual === 'Domicilio') ? tarifa : 0;

    const badge = document.getElementById('txtTarifaSugeridaBadge');
    if (badge) {
        badge.textContent = `Tarifa Cajero: $${tarifa.toLocaleString('es-CO')} COP`;
        badge.style.background = '#FEF3C7';
        badge.style.color = '#92400E';
        badge.style.borderColor = '#FCD34D';
    }

    const barrio = document.getElementById('inputBarrio').value || 'Domicilio';
    if (tipoEntregaActual === 'Domicilio') {
        document.getElementById('envioVal').textContent = `$${tarifa.toLocaleString('es-CO')} COP (${barrio})`;
    } else {
        document.getElementById('envioVal').textContent = 'Gratis ($0)';
    }

    localStorage.setItem('saborator_costo_envio', tarifa);
    renderCheckoutCart();
}

function cambiarBarrioCheckout() {
    const sel = document.getElementById('selectBarrioCheckout').value;
    const colCustom = document.getElementById('colCustomFeeCheckout');
    let tarifa = 4000;
    let barrio = 'Ciudadela 20 de Julio';
    let zonaTxt = 'Zona Sur';

    if (esCajeroOAdmin && sel.startsWith('custom')) {
        if (colCustom) colCustom.style.display = 'block';
        tarifa = parseInt(document.getElementById('inputCustomFeeCheckout')?.value) || 5000;
        barrio = 'Sector Personalizado';
        zonaTxt = 'Personalizada';
    } else {
        if (colCustom) colCustom.style.display = 'none';
        const parts = sel.split('|');
        tarifa = parseInt(parts[0]) || 4000;
        barrio = parts[1] || 'Ciudadela 20 de Julio';
        zonaTxt = tarifa === 4000 ? 'Zona Sur' : (tarifa === 6000 ? 'Zona Centro' : 'Zona Norte');
    }

    document.getElementById('inputBarrio').value = barrio;
    document.getElementById('inputCostoEnvio').value = tarifa;
    const inpManual = document.getElementById('inputFeeManualCheckout');
    if (inpManual) inpManual.value = tarifa;

    const badge = document.getElementById('txtTarifaSugeridaBadge');
    if (badge) {
        badge.textContent = `${zonaTxt}: $${tarifa.toLocaleString('es-CO')} COP`;
        badge.style.background = '#ECFDF5';
        badge.style.color = '#065F46';
        badge.style.borderColor = '#A7F3D0';
    }

    if (tipoEntregaActual === 'Domicilio') {
        costoEnvio = tarifa;
        document.getElementById('envioVal').textContent = `$${tarifa.toLocaleString('es-CO')} COP (${barrio})`;
    } else {
        costoEnvio = 0;
        document.getElementById('envioVal').textContent = 'Gratis ($0)';
    }

    localStorage.setItem('saborator_costo_envio', tarifa);
    localStorage.setItem('saborator_barrio', barrio);

    renderCheckoutCart();
}

// Cambiar tipo de entrega
function cambiarTipoEntrega(tipo) {
    tipoEntregaActual = tipo;
    document.querySelectorAll('.delivery-tab').forEach(el => el.classList.remove('active'));
    const tab = document.querySelector(`input[name="tipoEntrega"][value="${tipo}"]`).closest('.delivery-tab');
    if (tab) tab.classList.add('active');
    document.querySelector(`input[name="tipoEntrega"][value="${tipo}"]`).checked = true;

    document.getElementById('seccionDomicilio').style.display = tipo === 'Domicilio' ? 'block' : 'none';
    document.getElementById('seccionMesa').style.display = tipo === 'EnMesa' ? 'block' : 'none';

    if (tipo === 'Domicilio') {
        cambiarBarrioCheckout();
    } else {
        costoEnvio = 0;
        document.getElementById('inputCostoEnvio').value = 0;
        document.getElementById('inputBarrio').value = '';
        document.getElementById('envioVal').textContent = 'Gratis ($0)';
        localStorage.setItem('saborator_costo_envio', 0);
        renderCheckoutCart();
    }

    localStorage.setItem('saborator_tipo_entrega', tipo);
}

// Seleccionar método de pago
function seleccionarPago(metodo) {
    document.querySelectorAll('.payment-box').forEach(el => el.classList.remove('active'));
    const box = document.querySelector(`input[name="metodoPago"][value="${metodo}"]`).closest('.payment-box');
    if (box) box.classList.add('active');
    document.querySelector(`input[name="metodoPago"][value="${metodo}"]`).checked = true;

    const cajaCambio = document.getElementById('cajaCambioEfectivo');
    if (cajaCambio) {
        cajaCambio.style.display = metodo === 'Efectivo' ? 'block' : 'none';
    }
}

// Renderizar carrito en Checkout
function renderCheckoutCart() {
    const cart = JSON.parse(localStorage.getItem('saborator_cart') || '[]');
    const container = document.getElementById('checkoutItemsList');
    const inputsContainer = document.getElementById('hiddenFormInputs');
    const alertaMin = document.getElementById('alertaMinimoCheckout');
    const btnConfirmar = document.getElementById('btnConfirmarPedido');

    if (cart.length === 0) {
        container.innerHTML = `
            <div style="text-align:center;padding:2rem 0;color:rgba(255, 255, 255,0.5);">
                <p style="margin:0 0 0.5rem;font-size:0.95rem;">Tu carrito está vacío</p>
                <a href="/Carta" style="color:var(--gold);text-decoration:none;font-weight:700;font-size:0.85rem;">← Ver la Carta</a>
            </div>
        `;
        document.getElementById('subtotalVal').textContent = '$0 COP';
        document.getElementById('totalFinalVal').textContent = '$0 COP';
        if (alertaMin) alertaMin.style.display = 'none';
        btnConfirmar.disabled = true;
        btnConfirmar.style.opacity = '0.5';
        btnConfirmar.style.cursor = 'not-allowed';
        return;
    }

    let subtotal = 0;
    let htmlItems = '';
    let htmlInputs = '';

    cart.forEach((item, index) => {
        const itemTotal = item.precio * item.cantidad;
        subtotal += itemTotal;

        htmlItems += `
            <div class="summary-item-row">
                <div class="summary-item-info">
                    <span class="summary-item-name">${item.nombre}</span>
                    <span class="summary-item-qty">${item.cantidad} x $${item.precio.toLocaleString('es-CO')}</span>
                </div>
                <span class="summary-item-price">$${itemTotal.toLocaleString('es-CO')}</span>
            </div>
        `;

        htmlInputs += `
            <input type="hidden" name="productoIds" value="${item.id}" />
            <input type="hidden" name="cantidades" value="${item.cantidad}" />
            <input type="hidden" name="notasLineas" value="${String(item.nota || '').replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;')}" />
        `;
    });

    // Cálculo de Descuento de Cupón Promocional
    let descuento = 0;
    const cuponGuardado = JSON.parse(localStorage.getItem('saborator_cupon') || 'null');
    const filaDescuento = document.getElementById('filaDescuentoCupon');
    const lblDescuento = document.getElementById('lblDescuentoCupon');
    const descuentoVal = document.getElementById('descuentoVal');
    const cuponInputBox = document.getElementById('cuponInputContainer');
    const cuponFeedback = document.getElementById('cuponFeedback');

    if (cuponGuardado && cuponGuardado.codigo) {
        if (cuponGuardado.tipoDescuento === 'Porcentaje') {
            descuento = Math.round(subtotal * (cuponGuardado.valor / 100));
            if (cuponGuardado.topeMaximo && descuento > cuponGuardado.topeMaximo) {
                descuento = cuponGuardado.topeMaximo;
            }
        } else {
            descuento = Math.min(cuponGuardado.valor, subtotal);
        }

        if (subtotal < (cuponGuardado.montoMinimo || 0)) {
            descuento = 0;
            if (filaDescuento) filaDescuento.style.display = 'none';
            if (cuponFeedback) {
                cuponFeedback.style.display = 'block';
                cuponFeedback.style.color = '#B45309';
                cuponFeedback.innerHTML = `<svg class="ico" width="1em" height="1em" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" style="vertical-align:-.15em;margin-right:.35em;flex:none"><path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>El cupón <strong>${cuponGuardado.codigo}</strong> requiere un pedido mínimo de $${(cuponGuardado.montoMinimo).toLocaleString('es-CO')} COP.`;
            }
        } else {
            if (filaDescuento) {
                filaDescuento.style.display = 'flex';
                if (lblDescuento) lblDescuento.innerHTML = `Descuento (<span style="font-family: var(--font-ui);letter-spacing:1px;">${cuponGuardado.codigo}</span>)`;
                if (descuentoVal) descuentoVal.textContent = `-$${descuento.toLocaleString('es-CO')} COP`;
            }

            htmlInputs += `<input type="hidden" name="codigoCupon" value="${cuponGuardado.codigo}" />`;

            if (cuponInputBox) {
                cuponInputBox.innerHTML = `
                    <div style="display:flex;align-items:center;justify-content:space-between;width:100%;background:#ECFDF5;border:1px solid #10B981;border-radius:10px;padding:0.5rem 0.85rem;">
                        <div style="display:flex;align-items:center;gap:6px;font-size:0.85rem;color:#065F46;font-weight:700;">
                            <span><svg class="ico" width="1em" height="1em" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" style="vertical-align:-.15em;margin-right:.35em;flex:none"><path d="m12 3-1.9 5.8a2 2 0 0 1-1.3 1.3L3 12l5.8 1.9a2 2 0 0 1 1.3 1.3L12 21l1.9-5.8a2 2 0 0 1 1.3-1.3L21 12l-5.8-1.9a2 2 0 0 1-1.3-1.3Z"/></svg>Cupón <strong>${cuponGuardado.codigo}</strong> (-$${descuento.toLocaleString('es-CO')} COP)</span>
                        </div>
                        <button type="button" onclick="quitarCuponCheckout()" style="background:none;border:none;color:#DC2626;font-size:0.78rem;font-weight:700;cursor:pointer;text-decoration:underline;">Quitar</button>
                    </div>
                `;
            }
            if (cuponFeedback) {
                cuponFeedback.style.display = 'none';
            }
        }
    } else {
        if (filaDescuento) filaDescuento.style.display = 'none';
        if (cuponInputBox && !document.getElementById('inputCodigoCupon')) {
            cuponInputBox.innerHTML = `
                <input type="text" id="inputCodigoCupon" class="input-field" placeholder="Ej: BURGER15" style="padding:0.55rem 0.75rem;font-size:0.88rem;text-transform:uppercase;letter-spacing:0.05em;font-weight:700;" />
                <button type="button" id="btnAplicarCupon" onclick="aplicarCuponCheckout()" style="background:var(--brand, #1F120C);color:#FFFFFF;border:none;padding:0.55rem 1rem;border-radius:10px;font-weight:700;font-size:0.82rem;cursor:pointer;white-space:nowrap;transition:background 150ms ease;">
                    Aplicar
                </button>
            `;
        }
    }

    container.innerHTML = htmlItems;
    inputsContainer.innerHTML = htmlInputs;

    // Ahorro por combos (el servidor lo vuelve a calcular con los precios reales)
    const combos = window.SaboratorCombos ? SaboratorCombos.descuento(cart) : { total: 0, detalle: [] };
    const filaCombo = document.getElementById('filaDescuentoCombo');
    if (filaCombo) {
        filaCombo.style.display = combos.total > 0 ? 'flex' : 'none';
        if (combos.total > 0) {
            document.getElementById('lblDescuentoCombo').textContent = combos.detalle.map(d => (d.veces > 1 ? d.veces + ' × ' : '') + d.nombre).join(' + ');
            document.getElementById('descuentoComboVal').textContent = `-$${combos.total.toLocaleString('es-CO')} COP`;
        }
    }
    const totalFinal = Math.max(0, subtotal - (descuento + combos.total)) + (tipoEntregaActual === 'Domicilio' ? costoEnvio : 0);
    document.getElementById('subtotalVal').textContent = `$${subtotal.toLocaleString('es-CO')} COP`;
    document.getElementById('totalFinalVal').textContent = `$${totalFinal.toLocaleString('es-CO')} COP`;

    // Validación de Pedido Mínimo para Domicilio ($20.000 COP)
    const MINIMO_DOMICILIO = 20000;
    if (tipoEntregaActual === 'Domicilio' && subtotal < MINIMO_DOMICILIO) {
        const falta = MINIMO_DOMICILIO - subtotal;
        if (alertaMin) {
            alertaMin.style.display = 'flex';
            alertaMin.innerHTML = `
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="flex-shrink:0;margin-top:2px;"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
                <div>
                    <div style="font-weight:700;margin-bottom:2px;">Mínimo de entrega no alcanzado</div>
                    <div style="font-size:0.78rem;opacity:0.95;">Para envíos a domicilio el pedido mínimo es de <strong>$20.000 COP</strong>. Te faltan <strong>$${falta.toLocaleString('es-CO')} COP</strong>.</div>
                    <a href="/Carta" style="display:inline-block;margin-top:4px;color:#7F1D1D;font-weight:800;text-decoration:underline;font-size:0.78rem;">+ Agregar más productos</a>
                </div>
            `;
        }
        btnConfirmar.disabled = true;
        btnConfirmar.style.opacity = '0.5';
        btnConfirmar.style.cursor = 'not-allowed';
    } else {
        if (alertaMin) alertaMin.style.display = 'none';
        btnConfirmar.disabled = false;
        btnConfirmar.style.opacity = '1';
        btnConfirmar.style.cursor = 'pointer';
    }
}

async function aplicarCuponCheckout() {
    const inp = document.getElementById('inputCodigoCupon');
    const feedback = document.getElementById('cuponFeedback');
    const btn = document.getElementById('btnAplicarCupon');
    if (!inp || !feedback) return;

    const codigo = inp.value.trim().toUpperCase();
    if (!codigo) {
        feedback.style.display = 'block';
        feedback.style.color = '#DC2626';
        feedback.textContent = 'Por favor escribe un código de cupón.';
        return;
    }

    const cart = JSON.parse(localStorage.getItem('saborator_cart') || '[]');
    let subtotal = 0;
    cart.forEach(item => subtotal += (item.precio * item.cantidad));

    if (btn) { btn.disabled = true; btn.textContent = '...'; }

    try {
        const resp = await fetch('/Cupon/Validar', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ codigo: codigo, subtotal: subtotal })
        });
        const data = await resp.json();

        if (data.ok) {
            localStorage.setItem('saborator_cupon', JSON.stringify({
                codigo: data.codigo,
                tipoDescuento: data.tipoDescuento,
                valor: data.valor,
                descuento: data.descuento,
                descripcion: data.descripcion
            }));
            feedback.style.display = 'block';
            feedback.style.color = '#059669';
            feedback.textContent = data.mensaje;
            renderCheckoutCart();
        } else {
            feedback.style.display = 'block';
            feedback.style.color = '#DC2626';
            feedback.textContent = data.mensaje || 'Cupón inválido.';
        }
    } catch (err) {
        feedback.style.display = 'block';
        feedback.style.color = '#DC2626';
        feedback.textContent = 'Error al comunicarse con el servidor.';
    } finally {
        if (btn) { btn.disabled = false; btn.textContent = 'Aplicar'; }
    }
}

function quitarCuponCheckout() {
    localStorage.removeItem('saborator_cupon');
    const feedback = document.getElementById('cuponFeedback');
    if (feedback) feedback.style.display = 'none';
    renderCheckoutCart();
}

document.addEventListener('DOMContentLoaded', () => {
    // Cargar barrio y costo guardados
    const savedBarrio = localStorage.getItem('saborator_barrio');
    const sel = document.getElementById('selectBarrioCheckout');
    if (savedBarrio && sel) {
        for (let i = 0; i < sel.options.length; i++) {
            if (sel.options[i].value.includes(savedBarrio)) {
                sel.selectedIndex = i;
                break;
            }
        }
    }

    // Cargar datos previos si existen
    const savedTipo = localStorage.getItem('saborator_tipo_entrega');
    if (savedTipo) {
        cambiarTipoEntrega(savedTipo);
    } else {
        cambiarBarrioCheckout();
    }

    const savedDir = localStorage.getItem('saborator_direccion');
    if (savedDir) document.getElementById('inputDireccion').value = savedDir;

    const savedTel = localStorage.getItem('saborator_telefono');
    if (savedTel) document.getElementById('inputTelefono').value = savedTel;

    renderCheckoutCart();

    // Al enviar formulario, guardar datos en storage y limpiar carrito
    document.getElementById('formCheckout').addEventListener('submit', () => {
        const dir = document.getElementById('inputDireccion').value;
        const bar = document.getElementById('inputBarrio').value;
        const tel = document.getElementById('inputTelefono').value;
        if (dir) localStorage.setItem('saborator_direccion', dir);
        if (bar) localStorage.setItem('saborator_barrio', bar);
        if (tel) localStorage.setItem('saborator_telefono', tel);
        localStorage.removeItem('saborator_cart');
    });
});
