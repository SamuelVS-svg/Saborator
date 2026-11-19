#!/usr/bin/env bash
# Respaldo diario de la base de datos y las fotos de Saborator.
# Programarlo con cron (en la VM):  0 3 * * * /home/ubuntu/Saborator/deploy/oracle/respaldar.sh
set -euo pipefail

CARPETA="${CARPETA_RESPALDOS:-$HOME/respaldos-saborator}"
FECHA="$(date +%Y%m%d-%H%M)"
mkdir -p "$CARPETA"
cd "$(dirname "$0")"

# SQLite en modo WAL: se copian juntos el archivo de la base y su registro (-wal) para obtener una copia coherente
docker compose cp saborator:/data/saborator.db "$CARPETA/saborator-$FECHA.db"
docker compose cp saborator:/data/saborator.db-wal "$CARPETA/saborator-$FECHA.db-wal" 2>/dev/null || true
docker compose cp saborator:/app/wwwroot/uploads "$CARPETA/uploads-$FECHA" 2>/dev/null || true

# Conservar solo los últimos 14 días
find "$CARPETA" -maxdepth 1 -mtime +14 -exec rm -rf {} +
echo "Respaldo listo en $CARPETA ($FECHA)"
