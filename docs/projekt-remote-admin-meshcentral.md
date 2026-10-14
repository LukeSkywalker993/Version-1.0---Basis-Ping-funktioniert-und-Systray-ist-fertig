# Projektbeschreibung: Claude-gestützte Windows-Server-Fernwartung (LAN)

> Basis: **MeshCentral** (Open Source, MIT-Lizenz) — kein Eigenbau-Protokoll,
> sondern etabliertes, geprüftes Remote-Management-Werkzeug. Läuft komplett
> im LAN, kein Internet-Exposure nötig.

## Zielbild

Ein Helpdesk-Mitarbeiter (oder Claude, über die API) soll im Bedarfsfall
PowerShell-Zugriff auf einen bestimmten Windows-Server bekommen — ohne
dauerhaft offenen SSH-Port, mit vollständiger Protokollierung jedes
Befehls, und mit der Möglichkeit, den Zugriff jederzeit mit einem Klick
vollständig zu kappen.

```
┌─────────────────────┐        ausgehend (LAN)        ┌──────────────────────────┐
│  Windows-Server      │ ───────────────────────────▶ │  MeshCentral-Server       │
│  (MeshAgent.exe)     │   zeigt eigene Node-ID an     │  (Docker-Container, LAN)  │
└─────────────────────┘                                └──────────┬───────────────┘
                                                                   │ REST/WS-API
                                                                   ▼
                                                        ┌──────────────────────────┐
                                                        │  Claude (per API/CLI)     │
                                                        │  Befehl → Node-ID         │
                                                        └──────────────────────────┘
```

---

## Projektteil 1 — Server-Setup (Docker via SSH)

**Ziel:** MeshCentral als Container auf einem LAN-Host installieren.

- Zielmaschine: beliebiger Linux-Host im LAN (NICHT der Windows-Server
  selbst — getrennte Rolle: Management-Server ≠ verwaltetes Gerät).
- Installation per SSH auf **diesen** Linux-Host (nicht auf den Windows-
  Server — dort wird laut Vorgabe nichts installiert außer dem Agenten
  aus Teil 2).
- Aufgaben:
  1. Docker/Docker Compose auf dem Zielhost prüfen/installieren.
  2. `docker-compose.yml` mit dem offiziellen Image `meshcentral/meshcentral`
     ausrollen, Datenverzeichnis als Volume mounten (Persistenz der
     Gerätedaten + Session-Logs).
  3. Konfigurationsdatei (`config.json`) so setzen, dass:
     - der Server **nur im LAN** erreichbar ist (Bind auf LAN-IP, kein
       Port-Forwarding nach außen),
     - Session-Recording für Remote-Terminal-Sitzungen aktiviert ist,
     - ein API-Benutzer/Token für den programmatischen Zugriff (für
       Claude) angelegt wird, mit eingeschränkten Rechten (nur Terminal,
       keine Benutzerverwaltung).
  4. Admin-Account für die Web-Oberfläche anlegen.
- Ergebnis: Eine LAN-IP + Port, unter der der MeshCentral-Server läuft,
  sowie ein API-Token.

---

## Projektteil 2 — Windows-Agent

**Ziel:** Der zu verwaltende Windows-Server meldet sich beim
MeshCentral-Server an und erhält/zeigt seine eindeutige **Node-ID**.

- Vom MeshCentral-Server wird ein vorkonfigurierter Agent-Installer
  (`.exe`) generiert — das ist der einzige Softwarebaustein, der auf dem
  Windows-Server selbst läuft.
- Nach dem Start verbindet sich der Agent **ausgehend** zum
  MeshCentral-Server im LAN (keine eingehende Portfreigabe am Server
  nötig).
- Der Agent zeigt lokal die **Geräte-/Node-ID** an (Tray-Icon oder
  Konsolenausgabe) — das ist die ID, die du in Projektteil 3 an Claude
  übergibst.
- Rechteumfang des Agenten über MeshCentral-Benutzergruppen festlegen:
  z. B. nur "Remote-Terminal", kein Dateitransfer, keine
  Desktop-Fernsteuerung, falls das für den Anwendungsfall ausreicht.
- Trennung des Zugriffs:
  - Agent-Dienst stoppen/Prozess beenden → Verbindung ist augenblicklich
    weg, oder
  - serverseitig den Node aus der Gruppe entfernen/deaktivieren.

---

## Projektteil 3 — Claude-Anbindung ("Kommandozeile")

**Ziel:** Ein Prompt-Baustein, mit dem der Nutzer Claude anweist, sich
über die MeshCentral-API mit einem bestimmten Gerät zu verbinden, bevor
er fachliche Anweisungen gibt.

Beispieltext für den Nutzer:

```
Verbinde dich über den MeshCentral-Proxy unter 192.168.X.X:443
mit dem API-Token <TOKEN> zur Node-ID <NODE-ID>.
Sobald die Verbindung steht, führe Folgendes aus: <Aufgabe>
```

Danach z. B.:

```
Prüfe die Error-Logs der letzten 24 Stunden und fasse die kritischen
Einträge zusammen.
```

**Technische Umsetzung dieses Teils:**
- Claude braucht dafür ein **MCP-Tool** (oder ein kleines Wrapper-Skript),
  das die MeshCentral-REST-/WebSocket-API anspricht: Login mit Token,
  Node per ID auswählen, Remote-Terminal-Session öffnen, Befehl senden,
  Ausgabe zurücklesen.
- Jede Eingabe/Ausgabe läuft durch das von MeshCentral geführte
  Session-Log (Projektteil 1) — damit ist dokumentiert, was Claude
  tatsächlich ausgeführt hat.
- Dieses MCP-Tool/Wrapper-Skript kann ich dir im nächsten Schritt bauen,
  sobald Server (Teil 1) und Agent (Teil 2) stehen und ich die
  API-Zugangsdaten/Node-ID zum Testen habe.

---

## Offene Entscheidungen für die Umsetzung

1. Auf welchem Linux-Host im LAN soll der MeshCentral-Container laufen?
2. Soll der API-Zugriff für Claude read-only-artige Befehle
   (Log-Auswertung) erlauben, oder auch verändernde Aktionen? → bestimmt
   die Rechtegruppe in MeshCentral.
3. Wer bekommt den Admin-Zugang zur MeshCentral-Weboberfläche
   (Audit-Verantwortlicher)?
4. Aufbewahrungsdauer der Session-Logs (Compliance-Vorgabe?).

## Nächster Schritt

Sobald Punkt 1 geklärt ist, kann ich das `docker-compose.yml` +
`config.json` für Projektteil 1 konkret ausarbeiten.
