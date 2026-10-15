#!/usr/bin/env bash
# Wird von INSTALLIEREN.ps1 auf dem Linux-Host ausgefuehrt.
set -euo pipefail
cd ~/meshcentral
sed -i 's/\r$//' ./*.sh .env.example config.json.template docker-compose.yml 2>/dev/null || true
[ -f .env.neu ] && sed -i 's/\r$//' .env.neu
chmod +x install.sh create-claude-user.sh
if [ -f .env ]; then rm -f .env.neu; echo "Vorhandene .env bleibt unveraendert."; else mv .env.neu .env; chmod 600 .env; fi
S=""
if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
  S="sudo"; echo ">>> Fuer Docker wird sudo gebraucht. Bitte das Passwort vom Benutzer $(id -un) eingeben."
fi
if ! command -v docker >/dev/null 2>&1; then $S ./install.sh --install-docker; else $S ./install.sh; fi
$S ./create-claude-user.sh
$S cp secrets/12ps_claude_remote.json ~/12ps_claude_remote.json
$S chown "$(id -u):$(id -g)" ~/12ps_claude_remote.json
echo; $S docker compose ps
