# MeshCentral – Projektteil 1 (Server im LAN)

Setup für den MeshCentral-Server aus
[`docs/projekt-remote-admin-meshcentral.md`](../../docs/projekt-remote-admin-meshcentral.md).
Läuft als Docker-Container auf einem **separaten Linux-Host im LAN**, nicht auf
dem Windows-Server.

| Datei | Zweck |
|---|---|
| `docker-compose.yml` | Container `ghcr.io/ylianst/meshcentral` (offizielles Image), Version gepinnt, Port nur auf LAN-IP |
| `config.json.template` | MeshCentral-Konfiguration; `install.sh` rendert daraus `meshcentral-data/config.json` |
| `.env.example` | Alle hostspezifischen Werte (LAN-IP, Netz, Passwörter). Vorlage für `.env` |
| `install.sh` | Docker prüfen/installieren, Konfig rendern, Admin anlegen, Container starten |
| `create-claude-user.sh` | Gerätegruppe + eingeschränkten API-Benutzer für Claude + Login-Token anlegen |

## Installation (per SSH auf dem Linux-Host)

```bash
# 1. Dateien auf den Host bringen, z. B.:
scp -r deploy/meshcentral user@LINUX-HOST:~/meshcentral
ssh user@LINUX-HOST
cd ~/meshcentral

# 2. Werte eintragen (alle CHANGE_ME ersetzen)
cp .env.example .env
nano .env

# 3. Installieren und starten (mit --install-docker, falls Docker noch fehlt)
./install.sh

# 4. API-Benutzer für Claude anlegen
./create-claude-user.sh
```

Ergebnis: Weboberfläche unter `https://<MESH_HOST>:<MESH_PORT>` (Login mit
`MESH_ADMIN_USER`) und die API-Zugangsdaten in `secrets/claude-api.txt`
(`token_user` / `token_pass`, nur für root/den Besitzer lesbar).

## Was die Konfiguration absichert

**Nur LAN**
- Port 443 wird nur auf `MESH_LAN_IP` veröffentlicht, nicht auf `0.0.0.0`.
- Kein HTTP-Port 80 (`redirPort: 0`), kein Intel-AMT-Port 4433 (`mpsPort: 0`).
- `userAllowedIP` und `agentAllowedIP` lassen nur Verbindungen aus `MESH_LAN_CIDR` zu.
- Kein Port-Forwarding am Router einrichten. Optional zusätzlich die Host-Firewall
  (ufw/nftables) auf das LAN beschränken.
- Bewusst **nicht** `LANonly: true`: Dieser Modus findet den Server per
  Multicast, das funktioniert im Docker-Bridge-Netz nicht zuverlässig. Die
  Agents verbinden sich stattdessen direkt zu `MESH_HOST`.

**Session-Recording**
- Alle Terminal-, PowerShell-, Desktop- und Dateisitzungen werden aufgezeichnet
  (Protokolle 1, 2, 5, 6, 8, 9) nach `meshcentral-recordings/`.
- Löschung nach `MESH_RECORDING_DAYS` Tagen (Standard 90, offene Entscheidung Nr. 4).
- Fehlgeschlagene Logins landen in `meshcentral-data/auth.log`, nach 5 Fehlversuchen
  in 10 Minuten wird die IP 30 Minuten gesperrt.

**Eingeschränkter API-Zugang für Claude**
- Benutzer `claude-api` ist kein Site-Admin und darf keine Benutzer verwalten
  (getestet: `adduser` mit dem Token ergibt `Permission denied`).
- Rechte nur in der Gerätegruppe `MESH_DEVICE_GROUP`:
  `RemoteControl` (nötig für das Terminal), Desktop nur Ansicht mit
  eingeschränkter Eingabe, keine Dateien, keine Registry, keine Software-Liste,
  kein AMT, nur eigene Ereignisse.
- Zugriff läuft über einen **Login-Token** statt über das Kontopasswort. Nur
  `claude-api` darf Tokens anlegen (`passwordRequirements.loginTokens`).
  Token widerrufen: Weboberfläche > Benutzer `claude-api` > Login-Tokens, oder
  den Benutzer aus der Gerätegruppe entfernen.
- Weboberfläche-Funktionen ohne Bedarf sind aus: Web-VNC, Web-RDP, Web-SSH, Plugins,
  Selbst-Update, Einbettung in iFrames.

**Ein manueller Schritt bleibt:** Das Recht „Kein Desktop“ lässt sich über
`meshctrl` nicht setzen. In der Weboberfläche: *Meine Geräte* > Gruppe
`12ps_claude_remote` > Benutzer `claude-api` bearbeiten > **Kein Desktop** anhaken.
Danach bleibt Claude wirklich nur das Terminal.

## Betrieb

```bash
docker compose ps                 # Status
docker compose logs -f            # Logs
docker compose down               # stoppen (Daten bleiben erhalten)
./install.sh                      # nach Änderung an .env / config.json.template
```

Update auf eine neue MeshCentral-Version: `MESHCENTRAL_VERSION` in `.env`
erhöhen, `./install.sh` ausführen. Ein tägliches Backup liegt in
`meshcentral-backups/` (14 Tage).

Wichtige Daten, die gesichert werden sollten: `meshcentral-data/` (enthält die
Server-Zertifikate; gehen sie verloren, müssen alle Agents neu installiert
werden) und `meshcentral-recordings/`.

## Getestet

Konfiguration, `install.sh` und `create-claude-user.sh` wurden mit
MeshCentral 1.2.5 (npm-Paket, gleiche Version wie das gepinnte Image)
durchlaufen: Server startet mit der gerenderten `config.json`, Admin wird
angelegt, Gerätegruppe, API-Benutzer, Rechte und Login-Token werden erzeugt,
der Token meldet sich an und Admin-Aktionen werden abgelehnt. Das
Docker-Image selbst konnte in der Testumgebung nicht gezogen werden;
`docker compose config` ist gültig.
