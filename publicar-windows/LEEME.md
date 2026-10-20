# Publicar Saborator desde tu PC con Tailscale Funnel

Todo se hace en **PowerShell**, dentro de la carpeta `publicar-windows` del proyecto.
Si PowerShell bloquea los scripts, ejecútalos así: `powershell -ExecutionPolicy Bypass -File .\nombre.ps1`

> Sigue las fases en orden y **no avances si una falla**.

---

## Fase 2 — Respaldo, compilación y arranque (en tu PC)

1. **Respaldo de la base de datos** (obligatorio antes de arrancar la versión nueva, porque al iniciar añade columnas):
   ```powershell
   .\1-respaldar-bd.ps1
   ```
   ✅ Debe terminar con `Respaldo creado y verificado`. Si falla, **detente**.

2. **Compilar en modo producción:**
   ```powershell
   .\2-publicar.ps1
   ```
   ✅ Debe terminar con `Listo: ...\publicado`.

3. **Arrancar:**
   ```powershell
   .\3-iniciar-saborator.ps1
   ```
   ✅ En la ventana debe aparecer `Now listening on: http://127.0.0.1:5106` y `Hosting environment: Production`.

4. **Comprobar** en el navegador de tu PC: <http://127.0.0.1:5106/salud> → debe mostrar `{"ok":true,...}`.

5. **Cambia ya las contraseñas** de las cuentas de ejemplo: entra como `admin`, ve a **Cambiar contraseña** y, en **Personal**, desactiva las cuentas que no uses (`cajero`, `mesero`, `cocinero`, `domiciliario`) o restablece sus contraseñas.

---

## Fase 3 — Tailscale

1. Instala Tailscale: <https://tailscale.com/download/windows> e inicia sesión.
2. Comprueba en PowerShell:
   ```powershell
   tailscale status
   ```
   ✅ Debe listar tu equipo (no debe decir `Logged out` ni `NeedsLogin`).
3. En el panel de Tailscale (<https://login.tailscale.com/admin>):
   - Pestaña **DNS** → asegúrate de que **MagicDNS** esté activado → en **HTTPS Certificates** pulsa **Enable HTTPS**.
   - Funnel se activa en el siguiente paso: si hace falta permiso, el propio comando te mostrará un enlace; ábrelo y pulsa **Enable Funnel** (o el botón equivalente que aparezca).

---

## Fase 4 — Publicar

Con Saborator en marcha (Fase 2, paso 3):
```powershell
tailscale funnel --bg 5106
tailscale funnel status
```
✅ `funnel status` debe mostrar algo como:
```
https://tu-equipo.tu-tailnet.ts.net (Funnel on)
|-- / proxy http://127.0.0.1:5106
```
Esa es tu **URL pública**.

Para dejar de publicar: `tailscale funnel reset`

---

## Fase 5 — Probar la URL pública

1. Abre `https://tu-equipo.tu-tailnet.ts.net/salud`
   ✅ Debe mostrar `"https":true` y `"host":"tu-equipo.tu-tailnet.ts.net"`.
   ⚠️ Si `host` muestra `127.0.0.1`, avísame: los pagos Wompi usarían el modo de pruebas.
2. Abre `https://tu-equipo.tu-tailnet.ts.net/`, inicia sesión con `admin` y recorre: Dashboard, Domicilios, Cocina, Caja.
3. Cookies seguras: en Chrome, **F12 → Application → Cookies** → `.Saborator.Auth` debe tener marcada la columna **Secure**.
4. Desde el **celular con datos móviles (Wi-Fi apagado)** abre la URL: debe cargar con el candado de HTTPS.
5. Comprueba que **no** se puede entrar por la red local: desde otro equipo de tu Wi-Fi, `http://IP-DE-TU-PC:5106` **no** debe abrir.

---

## Fase 6 — Que no se caiga

1. Prueba el vigilante a mano (cierra antes la ventana de la Fase 2):
   ```powershell
   .\4-vigilante.ps1
   ```
   Cierra la ventana minimizada de Saborator: en menos de 1 minuto el vigilante debe reiniciarla (lo verás en `vigilante.log`).

2. **Tarea programada** para que arranque sola al iniciar sesión en Windows. **Regístrala tú** (PowerShell como tu usuario, ajusta la ruta):
   ```powershell
   schtasks /Create /TN "Saborator Vigilante" /SC ONLOGON /RL LIMITED /TR "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Minimized -File \"C:\RUTA\AL\PROYECTO\publicar-windows\4-vigilante.ps1\""
   ```
   - Ver: `schtasks /Query /TN "Saborator Vigilante"`
   - Probar ya: `schtasks /Run /TN "Saborator Vigilante"`
   - Quitar: `schtasks /Delete /TN "Saborator Vigilante" /F`

   Tailscale ya arranca solo con Windows y `funnel --bg` queda guardado, así que no necesita tarea propia.

---

## Fase 7 — Día del evento

- [ ] **Suspensión desactivada:** Configuración → Sistema → Inicio/apagado y suspensión → "Nunca" (enchufado). Tapa del portátil: "No hacer nada" al cerrarla.
- [ ] **Cargador conectado** y Windows Update en pausa (Configuración → Windows Update → Pausar actualizaciones).
- [ ] **Conexión estable:** mejor por cable. Si es Wi-Fi, ten listo el hotspot del celular como respaldo.
- [ ] **Una hora antes:** abre la URL desde el celular con datos, inicia sesión, haz un pedido de prueba de punta a punta (cliente → cocina → domiciliario → entregado → cobrar) y cancélalo/elimínalo después.
- [ ] **Respaldo** de la base de datos esa mañana: `.\1-respaldar-bd.ps1`

**Si el enlace deja de responder:**
1. ¿Responde `http://127.0.0.1:5106/salud` en tu PC?
   - **No** → la app cayó: mira `vigilante.log`; si el vigilante no está corriendo, ejecuta `.\4-vigilante.ps1`.
   - **Sí** → el problema es Tailscale: sigue con 2.
2. `tailscale status` (¿conectado?) y `tailscale funnel status` (¿Funnel on?).
3. Si Funnel está apagado: `tailscale funnel --bg 5106`
4. Si no hay internet: cambia al hotspot del celular; Tailscale se reconecta solo en ~1 minuto.
