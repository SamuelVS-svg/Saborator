// Carrito de compras y modalidad de entrega de Saborator.
// Se carga en todas las páginas públicas (desde _Carrito.cshtml) para que el carrito funcione en cualquiera de ellas.
// Guarda los productos en localStorage ('saborator_cart'); los elementos de la página que no existan se ignoran.

// ══════════════════════════════════════════════════════════════════
// ══ MOTOR DEL CARRITO Y DOMICILIOS ESTILO KFC ═════════════════════
// ══════════════════════════════════════════════════════════════════

let cart = [];
let modalidadEntrega = localStorage.getItem('saborator_tipo_entrega') || 'Domicilio';
let direccionGuardada = localStorage.getItem('saborator_direccion') || '';
let costoEnvioGuardado = parseInt(localStorage.getItem('saborator_costo_envio')) || 4000;
let barrioGuardado = localStorage.getItem('saborator_barrio') || 'Ciudadela 20 de Julio';

function obtenerImagenPorNombre(nombre) {
    if (!nombre) return '/img/hamburguesa.jpg';
    const n = nombre.toLowerCase().trim();
    if (n.includes('perro') || n.includes('hot dog') || n.includes('hotdog')) return '/img/perrocaliente.jpg';
    if (n.includes('salchipapa')) return '/img/salchipapa.jpg';
    if (n.includes('papa')) return '/img/papasfrancesa.jpg';
    if (n.includes('coca')) return '/img/cocacola.png?v=3';
    if (n.includes('pepsi')) return '/img/pepsi.webp?v=2';
    if (n.includes('agua')) return '/img/agua.webp?v=2';
    if (n.includes('hamburguesa') || n.includes('burger')) return '/img/hamburguesa.jpg';
    return '/img/hamburguesa.jpg';
}

function initCartEngine() {
    try {
        cart = JSON.parse(localStorage.getItem('saborator_cart') || '[]');
    } catch(e) {
        cart = [];
    }

    // Auto-reparar imágenes guardadas para que cada producto tenga su foto real
    if (Array.isArray(cart) && cart.length > 0) {
        let modificado = false;
        cart.forEach(item => {
            const fotoReal = obtenerImagenPorNombre(item.nombre);
            if (!item.imgUrl || item.imgUrl === '0' || item.imgUrl === 0 || !item.imgUrl.startsWith('/') || (item.imgUrl.includes('hamburguesa') && fotoReal !== '/img/hamburguesa.jpg')) {
                item.imgUrl = fotoReal;
                modificado = true;
            }
        });
        if (modificado) {
            localStorage.setItem('saborator_cart', JSON.stringify(cart));
        }
    }

    actualizarModalidadUI();
    actualizarUI();
}

function guardarCart() {
    localStorage.setItem('saborator_cart', JSON.stringify(cart));
    actualizarUI();
}

function cartAgregar(id, nombre, precio, imgUrl, btn) {
    // Asegurar imagen específica del producto seleccionado
    let foto = (typeof imgUrl === 'string' && imgUrl.startsWith('/')) 
        ? imgUrl 
        : obtenerImagenPorNombre(nombre);

    const precioNum = (typeof precio === 'number') ? precio : (parseFloat(precio) || 0);

    const item = cart.find(x => x.id === id);
    if (item) {
        item.cantidad += 1;
        item.imgUrl = foto;
    } else {
        cart.push({
            id: id,
            nombre: nombre,
            precio: precioNum,
            imgUrl: foto,
            cantidad: 1
        });
    }
    guardarCart();

    // Feedback táctil visual en el botón
    if (btn) {
        const originalHtml = btn.innerHTML;
        btn.innerHTML = `
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
            <span>¡Agregado!</span>
        `;
        btn.style.transform = 'scale(1.05)';
        setTimeout(() => {
            btn.innerHTML = originalHtml;
            btn.style.transform = '';
        }, 900);
    }
}

function cartCambiarCantidad(id, delta) {
    const item = cart.find(x => x.id === id);
    if (!item) return;
    item.cantidad += delta;
    if (item.cantidad <= 0) {
        cart = cart.filter(x => x.id !== id);
    }
    guardarCart();
}

// Agrega al carrito los productos de un combo; el ahorro se aplica solo (ver combos.js)
function pedirCombo(id) {
    const combo = window.SaboratorCombos && SaboratorCombos.porId(id);
    if (!combo) return;
    combo.lineas.forEach(l => { for (let k = 0; k < l.cantidad; k++) cartAgregar(l.productoId, l.nombre, l.precioUnitario, l.imagen); });
    abrirDrawerCarrito();
}

function cartEliminar(id) {
    cart = cart.filter(x => x.id !== id);
    guardarCart();
}

function actualizarUI() {
    const totalItems = cart.reduce((acc, x) => acc + x.cantidad, 0);
    const subtotal = cart.reduce((acc, x) => acc + (x.precio * x.cantidad), 0);
    const envio = (modalidadEntrega === 'Domicilio' && totalItems > 0) ? costoEnvioGuardado : 0;
    const ahorroCombos = (window.SaboratorCombos ? SaboratorCombos.descuento(cart) : { total: 0, detalle: [] });
    const totalFinal = Math.max(0, subtotal - ahorroCombos.total) + envio;

    // 1. Badge Navbar
    const navBadge = document.getElementById('navCartCount');
    if (navBadge) {
        navBadge.textContent = totalItems;
        navBadge.style.display = totalItems > 0 ? 'flex' : 'none';
    }

    // 2. Barra Flotante Inferior
    const floatBar = document.getElementById('cartFloatingBar');
    if (floatBar) {
        if (totalItems > 0) {
            floatBar.classList.add('visible');
            document.getElementById('floatItemCount').textContent = totalItems;
            document.getElementById('floatSubtotalVal').textContent = `$${(subtotal - ahorroCombos.total).toLocaleString('es-CO')} COP`;
            document.getElementById('floatModeVal').textContent = modalidadEntrega === 'Domicilio' 
                ? `Domicilio ($${costoEnvioGuardado.toLocaleString('es-CO')} - ${barrioGuardado})` 
                : (modalidadEntrega === 'ParaLlevar' ? 'Retiro en Restaurante' : 'En Mesa');
        } else {
            floatBar.classList.remove('visible');
        }
    }

    // 3. Renderizar items en Drawer
    renderDrawerItems(subtotal, envio, totalFinal, ahorroCombos);
}

function renderDrawerItems(subtotal, envio, totalFinal, ahorroCombos) {
    const list = document.getElementById('drawerItemsList');
    if (!list) return;

    if (cart.length === 0) {
        list.innerHTML = `
            <div style="text-align:center;padding:3rem 1rem;color:rgba(255, 255, 255,0.5);">
                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" style="margin-bottom:0.75rem;"><path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/><line x1="3" y1="6" x2="21" y2="6"/><path d="M16 10a4 4 0 0 1-8 0"/></svg>
                <p style="font-size:1.1rem;font-weight:600;margin:0 0 0.35rem;color:#FFFFFF;">Tu carrito está vacío</p>
                <p style="font-size:0.85rem;margin:0;">Explora nuestra carta y añade tus platos favoritos.</p>
            </div>
        `;
    } else {
        let html = '';
        cart.forEach(item => {
            const itemTotal = item.precio * item.cantidad;
            const itemImg = (item.imgUrl && typeof item.imgUrl === 'string' && item.imgUrl.startsWith('/') && !item.imgUrl.includes('0')) 
                ? item.imgUrl 
                : obtenerImagenPorNombre(item.nombre);
            html += `
                <div class="drawer-item">
                    <div class="drawer-item-img" style="background-image:url('${itemImg}');"></div>
                    <div class="drawer-item-details">
                        <div class="drawer-item-top">
                            <span class="drawer-item-name">${item.nombre}</span>
                            <button type="button" class="drawer-item-remove" onclick="cartEliminar(${item.id})" title="Eliminar">&times;</button>
                        </div>
                        <div class="drawer-item-bottom">
                            <div class="cart-stepper">
                                <button type="button" class="cart-stepper-btn" onclick="cartCambiarCantidad(${item.id}, -1)">&minus;</button>
                                <span class="cart-stepper-count">${item.cantidad}</span>
                                <button type="button" class="cart-stepper-btn" onclick="cartCambiarCantidad(${item.id}, 1)">&plus;</button>
                            </div>
                            <span class="drawer-item-price">$${itemTotal.toLocaleString('es-CO')}</span>
                        </div>
                    </div>
                </div>
            `;
        });
        list.innerHTML = html;
    }

    document.getElementById('drawerSubtotal').textContent = `$${subtotal.toLocaleString('es-CO')} COP`;
    const filaCombo = document.getElementById('drawerComboRow');
    if (filaCombo) {
        const ahorro = ahorroCombos && ahorroCombos.total > 0 ? ahorroCombos : null;
        filaCombo.style.display = ahorro ? 'flex' : 'none';
        if (ahorro) {
            document.getElementById('drawerComboLabel').textContent = ahorro.detalle.map(d => (d.veces > 1 ? d.veces + ' × ' : '') + d.nombre).join(' + ');
            document.getElementById('drawerComboVal').textContent = `-$${ahorro.total.toLocaleString('es-CO')} COP`;
        }
    }
    document.getElementById('drawerShipping').textContent = envio > 0 ? `$${envio.toLocaleString('es-CO')} COP (${barrioGuardado})` : 'Gratis ($0)';
    document.getElementById('drawerTotal').textContent = `$${totalFinal.toLocaleString('es-CO')} COP`;

    // Validación Límite Mínimo de Domicilio ($20.000 COP)
    const minAlert = document.getElementById('drawerMinAlert');
    const checkoutBtn = document.getElementById('drawerCheckoutBtn');
    const LIMITE_MINIMO = 20000;

    if (modalidadEntrega === 'Domicilio' && subtotal < LIMITE_MINIMO && cart.length > 0) {
        if (minAlert) {
            minAlert.style.display = 'block';
            const faltante = LIMITE_MINIMO - subtotal;
            document.getElementById('drawerMinFaltante').textContent = `$${faltante.toLocaleString('es-CO')} COP`;
        }
        if (checkoutBtn) {
            checkoutBtn.style.opacity = '0.5';
            checkoutBtn.onclick = function(e) {
                e.preventDefault();
                alert(`El pedido mínimo para despacho a domicilio es de $${LIMITE_MINIMO.toLocaleString('es-CO')} COP.\nTe faltan $${(LIMITE_MINIMO - subtotal).toLocaleString('es-CO')} COP para poder enviar a domicilio.`);
            };
        }
    } else {
        if (minAlert) minAlert.style.display = 'none';
        if (checkoutBtn) {
            checkoutBtn.style.opacity = '1';
            checkoutBtn.onclick = null;
        }
    }
}

// Drawer Open / Close
function abrirDrawerCarrito() {
    document.getElementById('cartDrawerOverlay').classList.add('open');
    document.body.style.overflow = 'hidden';
    document.body.classList.add('carrito-abierto');      // oculta el botón de WhatsApp, que tapaba el botón de pago
}
function cerrarDrawerCarrito() {
    document.getElementById('cartDrawerOverlay').classList.remove('open');
    document.body.style.overflow = '';
    document.body.classList.remove('carrito-abierto');
}
function clickOverlayDrawer(e) {
    if (e.target === document.getElementById('cartDrawerOverlay')) {
        cerrarDrawerCarrito();
    }
}

// Modal de Modalidad (KFC)
function abrirModalModalidad() {
    document.getElementById('modalityModalOverlay').classList.add('open');
    // Marcar opción activa
    document.querySelectorAll('.mod-option-btn').forEach(btn => btn.classList.remove('active'));
    const activeBtn = document.getElementById(`opt${modalidadEntrega}`);
    if (activeBtn) activeBtn.classList.add('active');

    const addrBox = document.getElementById('quickAddressBox');
    if (addrBox) {
        addrBox.style.display = modalidadEntrega === 'Domicilio' ? 'block' : 'none';
        document.getElementById('quickAddressInput').value = direccionGuardada;
        // Sincronizar select si existe
        const sel = document.getElementById('quickBarrioSelect');
        if (sel) {
            for (let i = 0; i < sel.options.length; i++) {
                if (sel.options[i].value.includes(barrioGuardado)) {
                    sel.selectedIndex = i;
                    break;
                }
            }
            cambiarBarrioModalKfc();
        }
    }
}
function cerrarModalModalidad() {
    document.getElementById('modalityModalOverlay').classList.remove('open');
}

function seleccionarModalidad(tipo) {
    modalidadEntrega = tipo;
    localStorage.setItem('saborator_tipo_entrega', tipo);
    
    document.querySelectorAll('.mod-option-btn').forEach(btn => btn.classList.remove('active'));
    const activeBtn = document.getElementById(`opt${tipo}`);
    if (activeBtn) activeBtn.classList.add('active');

    const addrBox = document.getElementById('quickAddressBox');
    if (tipo === 'Domicilio') {
        addrBox.style.display = 'block';
        document.getElementById('quickAddressInput').focus();
    } else {
        addrBox.style.display = 'none';
        actualizarModalidadUI();
        actualizarUI();
        cerrarModalModalidad();
    }
}

function cambiarBarrioModalKfc() {
    const sel = document.getElementById('quickBarrioSelect').value;
    const display = document.getElementById('quickModalFeeDisplay');
    const parts = sel.split('|');
    const tarifa = parseInt(parts[0]) || 4000;
    if (display) display.textContent = `$${tarifa.toLocaleString('es-CO')} COP`;
}

function guardarDireccionRapida() {
    const dir = document.getElementById('quickAddressInput').value.trim();
    const sel = document.getElementById('quickBarrioSelect').value;
    const parts = sel.split('|');
    const tarifa = parseInt(parts[0]) || 4000;
    const barrio = parts[1] || 'Ciudadela 20 de Julio';

    costoEnvioGuardado = tarifa;
    barrioGuardado = barrio;
    localStorage.setItem('saborator_costo_envio', tarifa);
    localStorage.setItem('saborator_barrio', barrio);

    if (dir) {
        direccionGuardada = dir;
        localStorage.setItem('saborator_direccion', dir);
    }
    actualizarModalidadUI();
    actualizarUI();
    cerrarModalModalidad();
}

// Funciones del Cotizador de la Sección Home
function cambiarZonaDomicilioHome() {
    const sel = document.getElementById('homeZoneSelect').value;
    const parts = sel.split('|');
    const tarifa = parseInt(parts[0]) || 4000;
    const sector = parts[1] || 'Ciudadela 20 de Julio';
    document.getElementById('homeFeeDisplay').textContent = `$${tarifa.toLocaleString('es-CO')} COP`;
    document.getElementById('homeSectorDisplay').textContent = sector;
    const timeTxt = tarifa <= 4000 ? '20 - 30 minutos' : (tarifa <= 6000 ? '30 - 40 minutos' : '40 - 55 minutos');
    document.getElementById('homeTimeDisplay').innerHTML = `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg> <span>${timeTxt}</span>`;
}

function aplicarTarifaDomicilioHome() {
    const sel = document.getElementById('homeZoneSelect').value;
    const parts = sel.split('|');
    const tarifa = parseInt(parts[0]) || 4000;
    const barrio = parts[1] || 'Ciudadela 20 de Julio';
    const dir = document.getElementById('homeAddressInput').value.trim();

    costoEnvioGuardado = tarifa;
    barrioGuardado = barrio;
    localStorage.setItem('saborator_costo_envio', tarifa);
    localStorage.setItem('saborator_barrio', barrio);
    if (dir) {
        direccionGuardada = dir;
        localStorage.setItem('saborator_direccion', dir);
    }
    modalidadEntrega = 'Domicilio';
    localStorage.setItem('saborator_tipo_entrega', 'Domicilio');

    actualizarModalidadUI();
    actualizarUI();

    const notice = document.getElementById('homeFeeConfirmNotice');
    if (notice) {
        notice.style.display = 'block';
        setTimeout(() => { notice.style.display = 'none'; }, 4000);
    }

    const menuSec = document.getElementById('menu');
    if (menuSec && menuSec.classList.contains('is-open')) menuSec.scrollIntoView({ behavior: 'smooth' });
    else location.href = '/Carta';
}

function actualizarModalidadUI() {
    const navText = document.getElementById('navModalityText');
    const heroType = document.getElementById('heroModalityLabel');
    const heroAddr = document.getElementById('heroAddressLabel');
    const drawerLabel = document.getElementById('drawerModLabel');

    if (modalidadEntrega === 'Domicilio') {
        if (navText) navText.textContent = 'Domicilio';
        if (heroType) heroType.textContent = 'Domicilio Express';
        if (heroAddr) heroAddr.textContent = direccionGuardada ? `${direccionGuardada} (${barrioGuardado})` : 'Ingresa tu dirección de entrega';
        if (drawerLabel) drawerLabel.textContent = `Domicilio (${barrioGuardado})`;
    } else if (modalidadEntrega === 'ParaLlevar') {
        if (navText) navText.textContent = 'Para Llevar';
        if (heroType) heroType.textContent = 'Para Llevar';
        if (heroAddr) heroAddr.textContent = 'Retiro en Sede: Cra 4 Sur # 50D-107';
        if (drawerLabel) drawerLabel.textContent = 'Retiro en Restaurante';
    } else {
        if (navText) navText.textContent = 'En Mesa';
        if (heroType) heroType.textContent = 'Consumo en Mesa';
        if (heroAddr) heroAddr.textContent = 'Plano de 30 mesas exclusivo';
        if (drawerLabel) drawerLabel.textContent = 'Consumo en Salón';
    }
}

document.addEventListener('DOMContentLoaded', initCartEngine);
