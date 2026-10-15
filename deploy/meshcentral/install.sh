#!/usr/bin/env bash
# MeshCentral – Projektteil 1: Erstinstallation / Konfig-Update auf dem LAN-Linux-Host.
#
# Aufruf auf dem Zielhost (per SSH):
#   cd meshcentral && cp .env.example .env && nano .env
#   ./install.sh                 # prüft Docker, rendert config.json, legt Admin an, startet
#   ./install.sh --install-docker  # zusätzlich Docker Engine + Compose-Plugin installieren
#
# Erneut ausführen ist gefahrlos: config.json wird neu gerendert, der Admin
# wird nur angelegt, wenn es noch keine Datenbank gibt.
set -euo pipefail
cd "$(dirname "$0")"

MC=/opt/meshcentral/meshcentral/meshcentral   # Pfad im offiziellen Image
DATA=/opt/meshcentral/meshcentral-data

die() { echo "FEHLER: $*" >&2; exit 1; }

# --- 1. Docker prüfen / installieren ---------------------------------------
if ! command -v docker >/dev/null 2>&1; then
  if [[ "${1:-}" == "--install-docker" ]]; then
    echo "Installiere Docker über get.docker.com ..."
    curl -fsSL https://get.docker.com | sh
  else
    die "Docker fehlt. Mit './install.sh --install-docker' installieren oder manuell nachinstallieren."
  fi
fi
docker compose version >/dev/null 2>&1 || die "Docker Compose-Plugin fehlt (Paket docker-compose-plugin)."
docker info >/dev/null 2>&1 || die "Docker-Daemon nicht erreichbar (läuft er? Ist der Benutzer in der Gruppe 'docker'?)."

# --- 2. .env laden und prüfen ----------------------------------------------
[[ -f .env ]] || die ".env fehlt. 'cp .env.example .env' und Werte eintragen."
set -a; source ./.env; set +a
if grep -q '^[^#]*=.*CHANGE_ME' .env; then
  grep -n '^[^#]*=.*CHANGE_ME' .env >&2
  die "In .env stehen noch Platzhalter (CHANGE_ME)."
fi
for v in MESHCENTRAL_VERSION MESH_LAN_IP MESH_HOST MESH_PORT MESH_LAN_CIDR MESH_RECORDING_DAYS \
         MESH_ADMIN_USER MESH_ADMIN_PASS MESH_API_USER; do
  [[ -n "${!v:-}" ]] || die "$v ist in .env leer."
done
if command -v ip >/dev/null 2>&1 && ! ip -o addr show | grep -qw "inet ${MESH_LAN_IP}"; then
  echo "WARNUNG: ${MESH_LAN_IP} ist auf diesem Host keiner Netzwerkschnittstelle zugeordnet."
fi

# --- 3. Verzeichnisse + config.json rendern --------------------------------
mkdir -p meshcentral-data meshcentral-files meshcentral-backups meshcentral-recordings
chmod 700 meshcentral-data meshcentral-backups meshcentral-recordings

render() {
  sed -e "s|\${MESH_HOST}|${MESH_HOST}|g" \
      -e "s|\${MESH_PORT}|${MESH_PORT}|g" \
      -e "s|\${MESH_LAN_CIDR}|${MESH_LAN_CIDR}|g" \
      -e "s|\${MESH_RECORDING_DAYS}|${MESH_RECORDING_DAYS}|g" \
      -e "s|\${MESH_API_USER}|${MESH_API_USER}|g" \
      config.json.template
}
render > meshcentral-data/config.json.new
if grep -q '\${' meshcentral-data/config.json.new; then
  die "config.json enthält nicht ersetzte Platzhalter."
fi
mv meshcentral-data/config.json.new meshcentral-data/config.json
chmod 600 meshcentral-data/config.json
echo "config.json geschrieben."

docker compose pull

# --- 4. Admin-Account (nur bei frischer Installation) ----------------------
if [[ ! -f meshcentral-data/meshcentral.db ]]; then
  echo "Frische Installation: lege Admin '${MESH_ADMIN_USER}' an ..."
  docker compose down >/dev/null 2>&1 || true
  run_mc() { docker compose run --rm --no-deps --entrypoint node meshcentral "$MC" --configfile "$DATA/config.json" "$@"; }
  run_mc --createaccount "${MESH_ADMIN_USER}" --pass "${MESH_ADMIN_PASS}" --email "${MESH_ADMIN_EMAIL:-}" --name "${MESH_ADMIN_USER}"
  run_mc --adminaccount "${MESH_ADMIN_USER}"
else
  echo "Datenbank vorhanden, Admin wird nicht neu angelegt."
fi

# --- 5. Starten -----------------------------------------------------------
docker compose up -d --force-recreate
echo -n "Warte auf https://${MESH_LAN_IP}:${MESH_PORT} "
for _ in $(seq 1 60); do
  if curl -ksf --noproxy "*" -o /dev/null "https://${MESH_LAN_IP}:${MESH_PORT}/"; then
    echo " OK"
    echo
    echo "MeshCentral läuft:  https://${MESH_HOST}:${MESH_PORT}  (Login: ${MESH_ADMIN_USER})"
    echo "Nächster Schritt:   ./create-claude-user.sh"
    exit 0
  fi
  echo -n "."; sleep 2
done
echo
docker compose logs --tail 50 meshcentral
die "Server antwortet nicht. Logs oben prüfen."
