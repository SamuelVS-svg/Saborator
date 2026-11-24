// El pago pasa por la pasarela segura: el servidor calcula el monto y firma la sesión de Wompi
function pagarWompiSeguimiento() {
    window.location.href = JSON.parse(document.getElementById('seguimiento-config').textContent).urlPago;
}

const SEG_CFG = JSON.parse(document.getElementById('seguimiento-config').textContent);
const orderNumber = SEG_CFG.numero;

const esDomicilio = SEG_CFG.esDomicilio;
let estadoActual = SEG_CFG.estado;


// ── Simulación del recorrido del domiciliario ──
const RUTA_SEGUNDOS = 90;           // duración simulada de la ruta (demostración)
let rutaInicioMs = SEG_CFG.estado === 'EnCamino' ? performance.now() - (SEG_CFG.segundosEnRuta * 1000) : null;
const rutaOsm = { listo: false, pts: null, d: null, moto: null, recorrido: null, map: null };
const RUTA_DEMO_SEGUNDOS = 30;      // vista previa mientras el pedido aún no sale
const SEDE_COORD = [11.0101922, -74.8231794];   // Cra 4 Sur # 50D-107 (fija: así no se consulta Nominatim por la sede)
// Direcciones a buscar, de la más exacta a la más general (Nominatim no entiende torre/apto)
const DIRS_DESTINO = SEG_CFG.dirsDestino;

// ── Mapa de OpenStreetMap (Leaflet): sede → destino por las calles, con la moto del domiciliario ──
(function iniciarMapaOsm() {
    const el = document.getElementById('rutaMapaOsm');
    if (!el || typeof L === 'undefined') return;
    const SEDE_FALLBACK = [10.9455, -74.7925];   // aproximado, solo si falla la búsqueda de la dirección
    const SVG_MOTO = '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><circle cx="18.5" cy="17.5" r="3.5"/><circle cx="5.5" cy="17.5" r="3.5"/><circle cx="15" cy="5" r="1"/><path d="M12 17.5V14l-3-3 4-3 2 3h2"/></svg>';
    const SVG_SEDE = '<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>';
    const SVG_DESTINO = '<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/></svg>';

    const map = L.map(el, { zoomControl: false, scrollWheelZoom: false, tap: false, zoomAnimation: false, fadeAnimation: false, markerZoomAnimation: false }).setView(SEDE_FALLBACK, 14, { animate: false });
    L.control.zoom({ position: 'bottomright' }).addTo(map);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19, attribution: '&copy; OpenStreetMap' }).addTo(map);
    const pin = (clase, svg) => L.divIcon({ className: 'ruta-pin', html: '<div class="ruta-pin-bubble ' + clase + '">' + svg + '</div>', iconSize: [34, 34], iconAnchor: [17, 17] });

    function geocodificar(direccion) {
        const clave = 'geo:' + direccion;
        try { const c = sessionStorage.getItem(clave); if (c) return Promise.resolve(JSON.parse(c)); } catch (e) {}
        return fetch('https://nominatim.openstreetmap.org/search?format=json&limit=1&countrycodes=co&q=' + encodeURIComponent(direccion), { headers: { 'Accept': 'application/json' } })
            .then(r => r.json())
            .then(j => {
                if (!j || !j.length) return null;
                const p = [parseFloat(j[0].lat), parseFloat(j[0].lon)];
                try { sessionStorage.setItem(clave, JSON.stringify(p)); } catch (e) {}
                return p;
            }).catch(() => null);
    }
    function rutaPorCalles(a, b) {
        return fetch('https://router.project-osrm.org/route/v1/driving/' + a[1] + ',' + a[0] + ';' + b[1] + ',' + b[0] + '?overview=full&geometries=geojson')
            .then(r => r.json())
            .then(j => (j.routes && j.routes.length) ? j.routes[0].geometry.coordinates.map(c => [c[1], c[0]]) : null)
            .catch(() => null);
    }
    function rutaRespaldo(a, b) {   // línea curva suave si no hay servicio de rutas
        const pts = [], n = 60, dx = b[1] - a[1], dy = b[0] - a[0];
        for (let i = 0; i <= n; i++) { const t = i / n, curva = Math.sin(t * Math.PI) * 0.18; pts.push([a[0] + dy * t - dx * curva, a[1] + dx * t + dy * curva]); }
        return pts;
    }

    const limpiar = d => d.replace(/,?\s*(torre|apto|apartamento|apt|int|interior|casa|piso|local|bloque|oficina)\b[^,]*/gi, '').replace(/\s{2,}/g, ' ').trim();
    const espera = ms => new Promise(res => setTimeout(res, ms));
    // Una búsqueda a la vez y espaciadas (Nominatim permite ~1 por segundo); se queda con la primera que acierte
    function geocodificarVarios(lista) {
        return lista.reduce((prev, d, i) => prev.then(r => r ? r : espera(i ? 1200 : 0).then(() => geocodificar(limpiar(d)))), Promise.resolve(null));
    }

    geocodificarVarios(DIRS_DESTINO).then(function (dest) {
        const sede = SEDE_COORD;
        const destino = dest || [sede[0] - 0.02, sede[1] + 0.03];
        L.marker(sede, { icon: pin('ruta-pin-sede', SVG_SEDE) }).addTo(map);
        L.marker(destino, { icon: pin('ruta-pin-destino', SVG_DESTINO) }).addTo(map);
        return rutaPorCalles(sede, destino).then(p => p || rutaRespaldo(sede, destino));
    }).then(function (pts) {
        const acum = [0]; let total = 0;
        for (let i = 1; i < pts.length; i++) { total += map.distance(pts[i - 1], pts[i]); acum.push(total); }
        L.polyline(pts, { color: '#6B4226', weight: 5, opacity: 0.25, lineCap: 'round' }).addTo(map);
        rutaOsm.recorrido = L.polyline([pts[0]], { color: '#6B4226', weight: 5, opacity: 0.95, lineCap: 'round' }).addTo(map);
        rutaOsm.moto = L.marker(pts[0], { icon: L.divIcon({ className: 'ruta-moto', html: '<div class="ruta-moto-bubble">' + SVG_MOTO + '</div>', iconSize: [40, 40], iconAnchor: [20, 20] }), zIndexOffset: 1000 }).addTo(map);
        rutaOsm.pts = pts;
        rutaOsm.puntoEn = function (frac) {
            const tgt = Math.min(1, Math.max(0, frac)) * total; let i = 1;
            while (i < pts.length - 1 && acum[i] < tgt) i++;
            const seg = acum[i] - acum[i - 1] || 1, t = Math.min(1, Math.max(0, (tgt - acum[i - 1]) / seg));
            return { pos: [pts[i - 1][0] + (pts[i][0] - pts[i - 1][0]) * t, pts[i - 1][1] + (pts[i][1] - pts[i - 1][1]) * t], idx: i };
        };
        rutaOsm.map = map;
        map.invalidateSize(false);
        map.fitBounds(L.latLngBounds(pts), { padding: [34, 34], animate: false });
        setTimeout(function () { map.invalidateSize(false); map.fitBounds(L.latLngBounds(pts), { padding: [34, 34], animate: false }); }, 500);
        rutaOsm.listo = true;
        rutaEstado();
    });
    setTimeout(function () { map.invalidateSize(); }, 400);
})();

function rutaPintar(frac) {
    if (!rutaOsm.listo) return;
    const p = rutaOsm.puntoEn(frac);
    rutaOsm.moto.setLatLng(p.pos);
    rutaOsm.recorrido.setLatLngs(rutaOsm.pts.slice(0, p.idx).concat([p.pos]));
    const barra = document.getElementById('rutaBarra');
    if (barra) barra.style.width = Math.round(frac * 100) + '%';
}

function rutaEstado() {
    if (!rutaOsm.listo) return;
    const aviso = document.getElementById('rutaAviso');
    const texto = document.getElementById('rutaTexto');
    if (estadoActual === 'Entregado' || estadoActual === 'Facturado') {
        rutaPintar(1);
        if (aviso) aviso.hidden = true;
        if (texto) texto.textContent = 'Pedido entregado';
        return;
    }
    if (estadoActual !== 'EnCamino' || rutaInicioMs === null) {
        // El pedido aún no sale: vista previa en bucle del recorrido que hará el domiciliario
        const ciclo = RUTA_DEMO_SEGUNDOS + 4;
        const frac = Math.min(1, ((performance.now() / 1000) % ciclo) / RUTA_DEMO_SEGUNDOS);
        rutaPintar(frac);
        if (aviso) aviso.hidden = true;
        if (texto) texto.textContent = 'Simulación del recorrido · tu pedido aún no ha salido';
        return;
    }
    const frac = Math.min(1, (performance.now() - rutaInicioMs) / (RUTA_SEGUNDOS * 1000));
    rutaPintar(frac);
    const llego = frac >= 1;
    const yaAvisado = aviso && !aviso.hidden;
    if (aviso) aviso.hidden = !llego;
    if (texto) texto.textContent = llego ? 'El domiciliario llegó a tu dirección' : 'En camino · ' + Math.round(frac * 100) + '% del recorrido';
    if (llego && !yaAvisado && navigator.vibrate) { try { navigator.vibrate([200, 100, 200]); } catch (e) {} }
}
setInterval(rutaEstado, 500);
rutaEstado();

function aplicarEstadoVisual(estado, repartidor, enRutaSeg) {
    // Un cambio a Cancelado (o la reaparición de la página) requiere recargar para mostrar el aviso
    if (estado === "Cancelado" && estadoActual !== "Cancelado") { location.reload(); return; }
    const veniaEnCamino = estadoActual === 'EnCamino';
    estadoActual = estado;
    if (estado === 'EnCamino' && !veniaEnCamino) rutaInicioMs = performance.now() - ((enRutaSeg || 0) * 1000);
    if (estado !== 'EnCamino') rutaInicioMs = null;

    let step = 1;
    let texto = "Hemos recibido tu pedido y lo estamos confirmando";

    if (estado === "EnPreparacion") {
        step = 2;
        texto = "Nuestros chefs están cocinando tu orden al momento";
    } else if (estado === "Listo") {
        step = esDomicilio ? 2 : 3;
        texto = esDomicilio ? "Tu pedido está listo y saldrá en breve con nuestro domiciliario" : "¡Tu pedido está listo para recoger!";
    } else if (estado === "EnCamino") {
        step = 3;
        texto = repartidor ? `${repartidor} va en camino con tu pedido` : "Tu pedido va en camino a tu dirección";
    } else if (estado === "Entregado" || estado === "Facturado") {
        step = 4;
        texto = "Tu pedido ha sido entregado ¡Buen provecho!";
    }

    // Actualizar barra de progreso
    const fill = document.getElementById('progressFill');
    if (fill) fill.style.width = `${(step - 1) * 33.33}%`;

    // Actualizar texto descriptivo
    const lbl = document.getElementById('labelEstadoTexto');
    if (lbl) lbl.textContent = texto;
    rutaEstado();

    // Actualizar nodos visuales
    const nodes = document.querySelectorAll('.stepper-node');
    nodes.forEach((node, idx) => {
        const nodeStep = idx + 1;
        node.classList.toggle('active', nodeStep <= step);
        node.classList.toggle('current', nodeStep === step);
    });
}

// ── Conexión SignalR al grupo de este pedido ──
if (typeof signalR !== 'undefined') {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/cocina")
        .withAutomaticReconnect()
        .build();

    connection.on("EstadoPedidoCambiado", function (data) {
        // El hub no envía el nombre del domiciliario: consultar el detalle completo
        if (data && data.estado) pollOrderStatus();
    });

    connection.start()
        .then(function () {
            connection.invoke("SeguirPedido", orderNumber).catch(err => console.error(err));
        })
        .catch(function (err) {
            console.warn("[SignalR Tracker] Usando sondeo de respaldo:", err);
        });
}

function pollOrderStatus() {
    fetch(`/Pedido/EstadoApi?numero=${encodeURIComponent(orderNumber)}`)
        .then(res => res.json())
        .then(data => {
            if (data && data.estado) {
                aplicarEstadoVisual(data.estado, data.repartidor, data.segundosEnRuta);
            }
        })
        .catch(err => console.log('Polling check:', err));
}

// Polling de respaldo cada 12 segundos si no está completado
if (SEG_CFG.polling) setInterval(pollOrderStatus, 12000);
