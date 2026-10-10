# Apple Health → HTTPS (nachts per Kurzbefehl)

## 1. Server
```
HEALTH_TOKEN=dein-langes-geheimes-token python3 server.py
```
HTTPS davor schalten, z. B. mit Caddy (`Caddyfile`):
```
health.deinedomain.de {
    reverse_proxy localhost:8080
}
```
Uploads landen als Datei in `data/`.

## 2. Kurzbefehl (App „Kurzbefehle“)
Neuer Kurzbefehl „Health Upload“:
1. **Datum** → „Aktuelles Datum“, dann **Datum anpassen**: −1 Tag (Beginn gestern).
2. **Health-Daten finden**: z. B. Schritte, Ruhepuls, Schlaf …; Filter „Startdatum ist nach [gestern]“. Pro Datentyp eine Aktion.
3. **Statistik berechnen** (Summe/Durchschnitt) oder „Wörterbuch“ bauen, z. B.
   `{ "date": <Datum>, "steps": <Summe>, "resting_hr": <Ø> }`.
4. **Inhalt von URL abrufen**
   - URL: `https://health.deinedomain.de/upload`
   - Methode: POST
   - Header: `Authorization` = `Bearer dein-langes-geheimes-token`
   - Anfragetext: JSON → das Wörterbuch.

## 3. Automation für nachts
Kurzbefehle → **Automation** → Neue Automation → **Tageszeit** (z. B. 03:00, täglich)
→ Kurzbefehl „Health Upload“ → **Sofort ausführen**, „Beim Ausführen benachrichtigen“ aus.

## Wichtig (iOS-Limitierung)
Health-Daten sind bei **gesperrtem** iPhone nicht lesbar. Damit es nachts klappt:
- iPhone nachts am Ladegerät lassen und die Automation lieber auf „Wenn Ladegerät verbunden wird“ legen, **oder**
- Zeit auf kurz nach dem Aufstehen legen (Handy wird entsperrt) – z. B. Automation „Wecker wird ausgeschaltet“.
- Beim ersten Lauf Zugriff auf alle Health-Typen „Immer erlauben“ bestätigen.
Wenn die Daten leer ankommen, ist das fast immer die Sperre – dann ist die App „Health Auto Export“ die zuverlässige Alternative.
