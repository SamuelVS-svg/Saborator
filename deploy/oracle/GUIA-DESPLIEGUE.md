# Guía: publicar Saborator gratis en Oracle Cloud (Always Free)

Resultado: Saborator en internet con HTTPS, base SQLite persistente y respaldo diario, sin costo mensual mientras Oracle mantenga su capa Always Free.

> Esta guía no se pudo probar contra una VM real de Oracle. La aplicación sí se probó completa sobre SQLite (58 + 20 comprobaciones de flujos). Los archivos de Docker y Caddy están sin probar en Docker; si algo falla al levantar, revisa `docker compose logs`.

## 0. Antes de empezar
- Cuenta en oracle.com/cloud/free (pide tarjeta para verificar identidad). Región de origen cercana: los recursos gratuitos solo existen en esa región.
- Una cuenta en duckdns.org (gratis) para tener un dominio como `tu-restaurante.duckdns.org`.
- Este repositorio en GitHub: https://github.com/SamuelVS-svg/Saborator

## 1. Crear la VM
1. En Oracle: **Compute → Instances → Create instance**.
2. Imagen: **Ubuntu 22.04 o 24.04**.
3. Forma (shape), en este orden de preferencia:
   - **VM.Standard.A1.Flex** (Arm): 2 OCPU y 12 GB alcanzan de sobra. Si aparece *Out of capacity*, reintenta más tarde u otra hora.
   - **VM.Standard.E2.1.Micro** (AMD, 1 GB): alcanza para este proyecto con poca carga; añade swap (paso 3).
4. Genera y **descarga la llave SSH** (la necesitas para entrar).
5. Anota la **IP pública** de la VM.

## 2. Abrir los puertos 80 y 443
1. En Oracle: **Networking → Virtual Cloud Networks → tu red → Security Lists → Default**.
2. **Add Ingress Rules**: origen `0.0.0.0/0`, TCP, puerto destino **80**; repite con **443**.
3. No abras el 8080 ni el 5106: la app solo debe ser accesible por Caddy.

## 3. Preparar la VM
Conéctate (`ssh -i llave.key ubuntu@IP_PUBLICA`) y ejecuta:

```bash
# Docker
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER && newgrp docker

# Firewall del sistema (Ubuntu en Oracle trae reglas de iptables que bloquean 80/443)
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 80 -j ACCEPT
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 443 -j ACCEPT
sudo apt-get install -y iptables-persistent && sudo netfilter-persistent save

# Solo si usas la VM de 1 GB: memoria de intercambio
sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

## 4. Dominio gratuito
En duckdns.org crea un subdominio y apúntalo a la **IP pública** de la VM.

## 5. Descargar y configurar Saborator
```bash
git clone https://github.com/SamuelVS-svg/Saborator.git
cd Saborator/deploy/oracle
cp .env.ejemplo .env
nano .env        # escribe DOMINIO y tus llaves de Wompi de PRUEBAS
```
No subas nunca el archivo `.env` a GitHub (ya está en `.gitignore`).

## 6. Levantar el sistema
```bash
docker compose up -d --build
docker compose ps
docker compose logs -f saborator     # Ctrl+C para salir
```
La primera compilación tarda varios minutos. Cuando termine, abre `https://TU-DOMINIO/salud`: debe responder `{"ok":true,"https":true,...}`.

## 7. Contraseña inicial del administrador
En producción no se crean cuentas con claves conocidas. La clave temporal del administrador queda en un archivo dentro del contenedor:

```bash
docker compose exec saborator cat /app/credenciales-iniciales.txt
```
Ingresa con `admin` y esa clave; el sistema te pedirá cambiarla. **Léela y cámbiala en el primer arranque**, y luego crea los usuarios del personal desde **Personal → Crear**.

## 8. Webhook de Wompi (opcional)
En el panel de Wompi, **Desarrollo → Programadores → URL de eventos**:
`https://TU-DOMINIO/api/pagos/wompi-webhook`

## 9. Respaldo diario
```bash
chmod +x ~/Saborator/deploy/oracle/respaldar.sh
crontab -e
# agrega esta línea:
0 3 * * * /home/ubuntu/Saborator/deploy/oracle/respaldar.sh
```
Los respaldos quedan en `~/respaldos-saborator` (14 días). **Descárgalos de vez en cuando a otro lugar**: si pierdes la VM, se pierden también.

## 10. Actualizar la aplicación
```bash
cd ~/Saborator && git pull
cd deploy/oracle && docker compose up -d --build
```
Los datos (base, claves de sesión y fotos) viven en volúmenes y no se pierden al actualizar.

## Si algo falla
| Síntoma | Qué revisar |
|---|---|
| No abre el sitio | Puertos 80/443 en la Security List y en iptables (pasos 2 y 3); que el dominio apunte a la IP correcta. |
| Caddy no obtiene el certificado | `docker compose logs caddy`: normalmente es el dominio mal apuntado o el puerto 80 cerrado. |
| La app se reinicia sola | `docker compose logs saborator`; en la VM de 1 GB, comprueba el swap con `free -h`. |
| Los usuarios pierden la sesión al actualizar | Comprueba que el volumen `datos` exista (`docker volume ls`). |
| Pagos con Wompi se quedan cargando | Revisa que las llaves del `.env` sean de pruebas y estén completas; usa el botón de simulación. |

## Advertencias
- "Gratis" depende de que Oracle mantenga la capa Always Free y de no superar sus límites; confírmalo en su página. Revisa también que Oracle no marque tu instancia como inactiva.
- Con dinero real (llaves de producción de Wompi), desactiva `WOMPI_PERMITIR_SIMULACION`.
- Una sola instancia: la aplicación no está pensada para varias réplicas.
