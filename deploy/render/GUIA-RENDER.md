# Saborator en Render (demostración rápida, un solo servicio)

> Sin probar contra Render real. La app sí se probó en modo producción con SQLite. Datos y fotos son TEMPORALES:
> en el plan gratuito el disco se borra en cada reinicio o despliegue. Solo para demostraciones.

## Pasos
1. En render.com crea una cuenta (con GitHub).
2. **New ➜ Web Service ➜ Build and deploy from a Git repository** y elige `SamuelVS-svg/Saborator`.
3. Configura:
   - **Language / Runtime:** Docker
   - **Branch:** main
   - **Dockerfile Path:** `./Dockerfile.render`
   - **Instance Type:** Free
4. **Environment Variables** (Advanced ➜ Add Environment Variable):

   | Variable | Valor |
   |---|---|
   | `WOMPI_MODO_DEMO` | `true` (pago de Wompi simulado, sin cobros) |
   | `SABORATOR_ADMIN_CLAVE_INICIAL` | una clave tuya de 12 o más caracteres (será la clave inicial del usuario `admin`) |

   Render define `PORT` solo; no lo agregues. Las demás variables ya vienen en la imagen (SQLite, producción).
5. **Create Web Service.** La primera compilación tarda varios minutos. Al terminar, Render muestra la dirección `https://….onrender.com`.
6. Entra con usuario `admin` y la clave que pusiste; el sistema pedirá cambiarla. Crea los usuarios del personal en **Personal ➜ Crear**.

## Antes de la exposición
- El plan gratuito **duerme el servicio tras unos minutos sin visitas** y tarda en despertar. Abre la dirección unos 5 a 10 minutos antes y navega un poco para calentarla.
- Crea los usuarios y los datos de ejemplo **después** del último despliegue o reinicio: se pierden si Render reinicia.
- No necesitas Vercel ni configurar CORS: la aplicación entrega su propia interfaz.
