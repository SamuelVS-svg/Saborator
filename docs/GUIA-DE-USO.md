# Saborator — Guía de uso rápida

Sistema web para un restaurante de hamburguesas, salchipapas y perros calientes. Esta guía explica cómo ponerlo en marcha y cómo usa cada persona el sistema. El manual completo para la entrega está en `02-Manual-de-Usuario-Saborator.md`.

## 1. Poner el sistema en marcha

1. Abre PowerShell en la carpeta del proyecto (`ControlClientesRestaurante`, la que contiene `ControlClientesRestaurante.csproj`).
2. Actualiza el código (si trabajas con la rama de cambios): `git pull --no-edit origin mi-home-local`
3. Ejecuta `dotnet run`.
4. Abre en el navegador la dirección que aparece en la consola: normalmente `http://localhost:5106`.
5. Si ves una versión vieja, presiona **Ctrl+F5**.

Si el puerto está ocupado, cierra el `dotnet run` anterior (Ctrl+C) o usa otro puerto.

Sin SQL Server configurado, la aplicación usa una base SQLite propia y se crea sola.

## 2. Entrar al sistema

Pulsa **Ingresar** (arriba a la derecha). Hay tres formas:

- **Usuario o correo + contraseña**: escribe los datos y pulsa *Ingresar a mi cuenta*.
- **Continuar con Google**: elige tu cuenta de Google.
- **Crea una aquí**: registra una cuenta de cliente nueva.

Cuentas de demostración (solo en desarrollo; en producción se generan claves temporales y se pide cambiarlas):

| Usuario | Rol |
|---|---|
| `admin` | Administrador |
| `cajero` | Cajero |
| `mesero` | Mesero |
| `cocinero` | Cocinero |
| `domiciliario` | Domiciliario |
| `cliente` | Cliente |

Las claves de estas cuentas están en `Program.cs`, en el bloque `cuentasSemilla`. Cámbialas antes de publicar.

## 3. Cliente

1. En la página de inicio mira los combos del carrusel o entra a la **Carta**.
2. Agrega productos al **carrito** (icono del carrito, disponible en todas las páginas).
3. Elige entrega: **domicilio**, **recoger** o **en mesa**.
4. Confirma y paga.
5. Sigue el pedido en **Seguimiento**: al estar *En camino* se ve al repartidor en el mapa.
6. En tu **Perfil** consultas pedidos anteriores, y puedes dejar reseñas y usar cupones.

## 4. Mesero

Toma los pedidos de las mesas: elige la mesa, agrega productos, envía a cocina y revisa el estado de cada pedido.

## 5. Cocinero

Ve las comandas en tiempo real. Marca cada una como *En preparación* y luego *Lista*; el mesero y el cajero ven el cambio al instante.

## 6. Cajero

1. **Abre el turno** de caja.
2. Cobra los pedidos y genera la **factura**.
3. **Cierra el turno** al terminar.

## 7. Domiciliario

Ve los domicilios asignados, sale a repartir (el cliente lo ve *En camino*) y marca la entrega.

## 8. Administrador

Desde el **Panel** supervisa ventas e indicadores y administra productos, inventario, proveedores, mesas, personal, cupones, facturas y auditoría.

## 9. Problemas frecuentes

| Problema | Solución |
|---|---|
| `dotnet run` abre otro proyecto o falla | Estás en la carpeta equivocada: entra a la que tiene el `.csproj`. |
| "Puerto en uso" | Cierra el `dotnet run` anterior con Ctrl+C. |
| No ves los cambios nuevos | `git pull`, reinicia `dotnet run` y Ctrl+F5. |
| Google dice que la dirección no está registrada | Agrega `http://localhost:5106` en *Orígenes de JavaScript autorizados* en Google Cloud. |
| "Usuario o contraseña incorrectos" | Revisa mayúsculas y la clave de la cuenta; tras varios intentos se bloquea un rato. |
