#!/usr/bin/env bash
# MeshCentral – eingeschränkten API-Benutzer für Claude anlegen (Projektteil 1, Punkt 3).
#
# Legt an (idempotent, soweit MeshCentral das zulässt):
#   - Gerätegruppe ${MESH_DEVICE_GROUP} (für den Windows-Agent aus Teil 2)
#   - Benutzer ${MESH_API_USER} ohne Server-/Benutzerverwaltungsrechte
#   - Rechte in der Gruppe: nur Fernsteuerung (= Terminal), Desktop nur Ansicht
#     mit eingeschränkter Eingabe, KEINE Dateien/Registry/Software/AMT,
#     nur eigene Ereignisse sichtbar
#   - einen Login-Token (eigener Benutzername/Passwort für die API), der in
#     secrets/claude-api.txt abgelegt wird
#
# Aufruf: ./create-claude-user.sh   (nach ./install.sh, Server muss laufen)
set -euo pipefail
cd "$(dirname "$0")"
set -a; source ./.env; set +a

CTL=/opt/meshcentral/meshcentral/meshctrl.js
URL="wss://localhost:443"

ctl_admin() { docker compose exec -T meshcentral node "$CTL" --url "$URL" --loginuser "$MESH_ADMIN_USER" --loginpass "$MESH_ADMIN_PASS" "$@"; }
ctl_api()   { docker compose exec -T meshcentral node "$CTL" --url "$URL" --loginuser "$MESH_API_USER" --loginpass "$API_PASS" "$@"; }

mkdir -p secrets && chmod 700 secrets
SECRET_FILE=secrets/claude-api.txt

# 1. Gerätegruppe
if ctl_admin listdevicegroups | grep -qF "\"${MESH_DEVICE_GROUP}\""; then
  echo "Gerätegruppe '${MESH_DEVICE_GROUP}' existiert bereits."
else
  ctl_admin adddevicegroup --name "${MESH_DEVICE_GROUP}" --desc "Von Claude per Terminal verwaltete Windows-Server"
fi

# 2. API-Benutzer (zufälliges Passwort, wird nur für die Token-Erzeugung gebraucht)
if ctl_admin listusers | grep -qw "${MESH_API_USER}"; then
  echo "Benutzer '${MESH_API_USER}' existiert bereits."
  [[ -f "$SECRET_FILE" ]] && API_PASS=$(grep '^account_pass=' "$SECRET_FILE" | cut -d= -f2-) \
    || { echo "FEHLER: Passwort für ${MESH_API_USER} unbekannt ($SECRET_FILE fehlt)." >&2; exit 1; }
else
  API_PASS="$(openssl rand -base64 24 | tr -d '/+=' | cut -c1-24)Aa1!"
  ctl_admin adduser --user "${MESH_API_USER}" --pass "$API_PASS" --realname "Claude (API, eingeschraenkt)"
  printf 'account_user=%s\naccount_pass=%s\n' "$MESH_API_USER" "$API_PASS" > "$SECRET_FILE"
  chmod 600 "$SECRET_FILE"
fi

# 3. Rechte in der Gerätegruppe (Bitmaske siehe README)
ctl_admin addusertodevicegroup --group "${MESH_DEVICE_GROUP}" --userid "user//${MESH_API_USER}" \
  --remotecontrol --desktopviewonly --limiteddesktop \
  --nofiles --noregistry --nosoftware --noamt --limitedevents

# 4. Login-Token (Ersatz-Zugangsdaten für die API, jederzeit einzeln widerrufbar)
if grep -q '^token_user=' "$SECRET_FILE" 2>/dev/null; then
  echo "Login-Token existiert bereits in $SECRET_FILE."
else
  OUT=$(ctl_api logintokens --add claude --json)
  TOKEN_USER=$(echo "$OUT" | sed -n "s/.*tokenUser['\"]\{0,1\} *: *['\"]\([^'\"]*\)['\"].*/\1/p")
  TOKEN_PASS=$(echo "$OUT" | sed -n "s/.*tokenPass['\"]\{0,1\} *: *['\"]\([^'\"]*\)['\"].*/\1/p")
  if [[ -z "$TOKEN_USER" || -z "$TOKEN_PASS" ]]; then
    echo "Unerwartete Antwort beim Token-Anlegen:" >&2; echo "$OUT" >&2; exit 1
  fi
  printf 'token_user=%s\ntoken_pass=%s\nserver=wss://%s:%s\n' "$TOKEN_USER" "$TOKEN_PASS" "$MESH_HOST" "$MESH_PORT" >> "$SECRET_FILE"
fi

# 5. Konfigdatei für das Windows-Programm (windows-agent/): Server + Gruppen-Schlüssel (meshid).
#    Die meshid erlaubt nur das Herunterladen/Registrieren des Agents in dieser Gruppe.
GROUP_KEY=$(ctl_admin listdevicegroups | grep -F "\"${MESH_DEVICE_GROUP}\"" | head -1 | cut -d'"' -f2)
if [[ ${#GROUP_KEY} -eq 64 ]]; then
  printf '{\n  "server": "%s",\n  "groupKey": "%s"\n}\n' "${MESH_HOST}$( [[ "$MESH_PORT" != 443 ]] && echo ":$MESH_PORT" )" "$GROUP_KEY" > secrets/12ps_claude_remote.json
  chmod 600 secrets/12ps_claude_remote.json
  echo "Konfig für das Windows-Programm: $(pwd)/secrets/12ps_claude_remote.json (neben die .exe legen)"
else
  echo "WARNUNG: Gruppen-Schlüssel nicht ermittelt, Windows-Programm braucht ihn manuell." >&2
fi

echo
echo "Fertig. Zugangsdaten für Projektteil 3 liegen in: $(pwd)/$SECRET_FILE (chmod 600)"
echo "Noch manuell in der Weboberfläche: Gerätegruppe -> Benutzer ${MESH_API_USER} -> 'Kein Desktop' anhaken (siehe README)."
