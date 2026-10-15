# 12ps_claude_remote – Windows-Programm (Projektteil 2)

Kleines Windows-Programm für den zu verwaltenden Windows-Server:

1. Server eintragen (DNS-Name oder IP des MeshCentral-Containers, z. B. `10.199.199.15`).
2. **Verbinden** lädt den passenden MeshCentral-Agent vom Server, installiert ihn als
   Dienst `12ps_claude_remote` und startet ihn. Der Agent verbindet sich ausgehend zum Server.
3. Das Programm zeigt den **Schlüsselcode** (Node-ID, `node//…`) an.
   **Text für Claude kopieren** legt den fertigen Einstiegssatz für Claude in die Zwischenablage.

Weitere Knöpfe:
- **Trennen** stoppt den Dienst, die Verbindung ist sofort weg. **Verbinden** startet ihn wieder.
- **Deinstallieren** entfernt den Agent vollständig.

Das Programm braucht Administratorrechte (Dienst-Installation) und fragt beim Start danach.

## Gruppen-Schlüssel

Für die Erstinstallation braucht das Programm neben dem Server den
Gruppen-Schlüssel der Gerätegruppe `12ps_claude_remote`. Ohne ihn liefert
der Server keinen Agent aus.

`deploy/meshcentral/create-claude-user.sh` schreibt beides in
`secrets/12ps_claude_remote.json`. Diese Datei neben die `.exe` legen, dann
sind beide Felder vorausgefüllt. Nach erfolgreicher Installation speichert das
Programm die Werte selbst dort.

## Bauen

```
dotnet publish windows-agent/12ps_claude_remote.csproj -c Release -o out
```

Ergebnis: `out/12ps_claude_remote.exe` (eine Datei, .NET ist eingebettet, läuft
ohne installierte .NET-Runtime). Bauen geht auch unter Linux/macOS.

## Technik

- Download: `https://<server>/meshagents?id=4&meshid=<Gruppen-Schlüssel>&installflags=2`
  (Windows x64, nur Hintergrunddienst). Der Installer enthält Serveradresse,
  Server-Zertifikats-Hash und Gruppe.
- Installation: `agent-setup.exe -fullinstall`.
- Node-ID: Der Agent schreibt sie beim Start nach
  `HKLM\SOFTWARE\Open Source\12ps_claude_remote\NodeId`.
- Das selbstsignierte Zertifikat des Servers wird beim Download akzeptiert.
  Der installierte Agent prüft den Server danach selbst über den eingebetteten Hash.
