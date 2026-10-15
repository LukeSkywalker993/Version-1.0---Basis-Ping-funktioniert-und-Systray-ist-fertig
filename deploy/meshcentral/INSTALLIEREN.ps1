# 12ps_claude_remote - installiert den MeshCentral-Container auf 10.199.199.15
$ErrorActionPreference = 'Continue'
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
Start-Transcript -Path (Join-Path $dir 'install-log.txt') -Force | Out-Null
$H = '10.199.199.15'; $U = 'docker'; $K = 'C:\Users\Dustin\.ssh\id_ed25519'
$FP = 'SHA256:bDD7P2qK9y5/nRFxA9R/1DHlamjWNNLGkY3zq0YtcuY'
$noBom = New-Object System.Text.UTF8Encoding $false
function Fail($m) { Write-Host "`nFEHLER: $m" -ForegroundColor Red; Stop-Transcript | Out-Null; exit 1 }

Write-Host "1/5 Pruefe Host-Schluessel von $H ..."
$kh = Join-Path $env:TEMP '12ps_known_hosts'; $tmpk = Join-Path $env:TEMP '12ps_k.pub'
$match = @()
foreach ($l in (ssh-keyscan $H 2>$null)) {
  if (-not $l -or $l -match '^\s*#') { continue }
  [IO.File]::WriteAllText($tmpk, "$l`n", $noBom)
  if ((ssh-keygen -lf $tmpk) -match [regex]::Escape($FP)) { $match += $l }
}
if (-not $match) { Fail "Host-Schluessel stimmt nicht mit $FP ueberein (oder Host nicht erreichbar). Abbruch." }
[IO.File]::WriteAllText($kh, (($match -join "`n") + "`n"), $noBom)
Write-Host "    OK, Fingerabdruck stimmt."
$o = @('-i', $K, '-o', "UserKnownHostsFile=$kh", '-o', 'StrictHostKeyChecking=yes', '-o', 'IdentitiesOnly=yes')
$target = "$U@$H"

Write-Host "2/5 Bereite Dateien vor ..."
ssh @o $target 'test -f ~/meshcentral/.env'
$envExists = ($LASTEXITCODE -eq 0)
$chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789'
$b = New-Object byte[] 20; (New-Object Security.Cryptography.RNGCryptoServiceProvider).GetBytes($b)
$pw = (-join ($b | ForEach-Object { $chars[$_ % $chars.Length] })) + 'Aa1-'
$st = Join-Path $env:TEMP '12ps_stage'
Remove-Item $st -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item ($dir) $st -Recurse
$envTxt = [IO.File]::ReadAllText((Join-Path $st '.env.example')) -replace 'MESH_ADMIN_PASS=CHANGE_ME', "MESH_ADMIN_PASS=$pw"
[IO.File]::WriteAllText((Join-Path $st '.env.neu'), $envTxt, $noBom)
$tar = Join-Path $env:TEMP '12ps_mc.tar'
tar -cf $tar -C $st .
if ($LASTEXITCODE -ne 0) { Fail "tar fehlgeschlagen." }

Write-Host "3/5 Kopiere Dateien auf $H ..."
scp @o $tar "${target}:12ps_mc.tar"
if ($LASTEXITCODE -ne 0) { Fail "Kopieren auf den Server fehlgeschlagen." }
ssh @o $target 'mkdir -p ~/meshcentral && tar -xf ~/12ps_mc.tar -C ~/meshcentral && rm ~/12ps_mc.tar'
if ($LASTEXITCODE -ne 0) { Fail "Entpacken auf dem Server fehlgeschlagen." }

Write-Host "4/5 Installiere MeshCentral (kann einige Minuten dauern) ..."
ssh -t @o $target 'bash ~/meshcentral/remote-setup.sh'
if ($LASTEXITCODE -ne 0) { Fail "Installation auf dem Server fehlgeschlagen (Meldungen oben)." }

Write-Host "5/5 Hole Gruppen-Schluessel und pruefe den Container ..."
scp @o "${target}:12ps_claude_remote.json" (Join-Path $dir '12ps_claude_remote.json')
if ($LASTEXITCODE -ne 0) { Fail "12ps_claude_remote.json konnte nicht geholt werden." }
if (-not $envExists) {
  [IO.File]::WriteAllText((Join-Path $dir 'meshadmin-zugang.txt'), "Weboberflaeche: https://$H`r`nBenutzer: meshadmin`r`nPasswort: $pw`r`n", $noBom)
}
ssh @o $target 'cd ~/meshcentral && (docker compose ps 2>/dev/null || sudo -n docker compose ps); curl -ks -o /dev/null -w "HTTPS-Status: %{http_code}\n" https://10.199.199.15/' | Out-Host
Write-Host "`nFERTIG. Container laeuft, 12ps_claude_remote.json liegt neben der exe." -ForegroundColor Green
Stop-Transcript | Out-Null
