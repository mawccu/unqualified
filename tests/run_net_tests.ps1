# Multi-process network tests for UNQUALIFIED (M1).
# Starts one headless Godot process per player on this machine (loopback only),
# runs a scenario, then checks what each process logged.
#   powershell -File tests\run_net_tests.ps1
#   powershell -File tests\run_net_tests.ps1 -Only contend3,far
# Logs are kept in %TEMP%\unqualified_nettests.
# Not covered: added latency or packet loss, two different networks.
param([string[]]$Only = @())

$Only = @($Only | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$ErrorActionPreference = "Stop"
$godot = "C:\Users\USER\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_win64_console.exe"
$root = Split-Path $PSScriptRoot -Parent
$logDir = Join-Path $env:TEMP "unqualified_nettests"
New-Item -ItemType Directory -Force $logDir | Out-Null
$script:passed = 0
$script:failed = 0
$rounds = 24

function Check([string]$label, [bool]$ok) {
    if ($ok) { $script:passed++; Write-Host "PASS  $label" }
    else { $script:failed++; Write-Host "FAIL  $label" -ForegroundColor Red }
}

function Lines($logs, [string]$name, [string]$pattern) {
    return @($logs[$name] | Where-Object { $_ -like "*$pattern*" })
}

function Run-Scenario([string]$label, [int]$port, [string[]]$clients, [hashtable]$extra) {
    $t0 = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() / 1000.0 + 10.0
    $t0s = $t0.ToString("F2", [Globalization.CultureInfo]::InvariantCulture)
    $names = @("host") + $clients
    $procs = @()
    foreach ($n in $names) {
        $role = if ($n -eq "host") { "host" } else { "client" }
        $argList = @("--headless", "--path", $root, "res://tests/net_scenario.tscn", "--",
            "--role=$role", "--name=$n", "--port=$port", "--expect=$($names.Count)", "--t0=$t0s", "--timeout=50")
        foreach ($k in $extra.Keys) { $argList += "--$k=$($extra[$k])" }
        $out = Join-Path $logDir "$label.$n.log"
        Remove-Item $out, "$out.err" -ErrorAction SilentlyContinue
        $p = Start-Process -FilePath $godot -ArgumentList $argList -RedirectStandardOutput $out `
            -RedirectStandardError "$out.err" -PassThru -NoNewWindow
        $procs += [pscustomobject]@{ Name = $n; Proc = $p; Log = $out }
        if ($n -eq "host") { Start-Sleep -Seconds 2 } else { Start-Sleep -Milliseconds 300 }
    }
    $deadline = (Get-Date).AddSeconds(75)
    foreach ($p in $procs) {
        $left = [int][Math]::Max(0, ($deadline - (Get-Date)).TotalMilliseconds)
        if (-not $p.Proc.WaitForExit($left)) { $p.Proc.Kill(); Write-Host "  ($label/$($p.Name) had to be killed)" }
    }
    $logs = @{}
    foreach ($p in $procs) {
        $logs[$p.Name] = @(Get-Content $p.Log -ErrorAction SilentlyContinue)
        $logs["$($p.Name).err"] = @(Get-Content "$($p.Log).err" -ErrorAction SilentlyContinue | Where-Object { $_ -match "\S" })
    }
    return $logs
}

function Assert-Quiet($logs, [string]$label) {
    $errs = @($logs.Keys | Where-Object { $_ -like "*.err" } | ForEach-Object { $logs[$_] })
    Check "${label}: nothing on stderr in any process" ($errs.Count -eq 0)
    if ($errs.Count -gt 0) { $errs | Select-Object -First 5 | ForEach-Object { Write-Host "      $_" } }
}

function Assert-Contend($logs, [string[]]$attackers, [string]$label) {
    $contested = 0; $bad = 0; $mismatch = 0; $wrongReason = 0
    for ($r = 0; $r -lt $rounds; $r++) {
        $att = @()
        foreach ($n in $attackers) {
            foreach ($line in (Lines $logs $n "ROUND $r ATTEMPT ")) {
                if ($line -match 'me=(\S+) ok=(\w+) reason=(\w+)') {
                    $att += [pscustomobject]@{ Me = $Matches[1]; Ok = ($Matches[2] -eq "true"); Reason = $Matches[3] }
                }
            }
        }
        if ($att.Count -ne $attackers.Count) { continue }
        $contested++
        $winners = @($att | Where-Object { $_.Ok })
        if ($winners.Count -ne 1) { $bad++; continue }
        $holder = ""
        foreach ($line in (Lines $logs "host" "ROUND $r HOLDER ")) { if ($line -match 'HOLDER (\S+) rev') { $holder = $Matches[1] } }
        if ($holder -ne $winners[0].Me) { $mismatch++ }
        foreach ($x in $att) { if (-not $x.Ok -and $x.Reason -ne "already_held") { $wrongReason++ } }
    }
    Check "${label}: at least 20 rounds where every player attempted (got $contested)" ($contested -ge 20)
    Check "${label}: exactly one winner in every such round ($bad bad)" ($bad -eq 0)
    Check "${label}: the host's holder is the winner every round ($mismatch mismatches)" ($mismatch -eq 0)
    Check "${label}: every loser was told already_held ($wrongReason wrong)" ($wrongReason -eq 0)
}

function Wanted([string]$name) { return ($Only.Count -eq 0) -or ($Only -contains $name) }

if (Wanted "lobby4") {
    Write-Host "`n== lobby4: host plus three clients"
    $l = Run-Scenario "lobby4" 24701 @("A", "B", "C") @{ scenario = "lobby" }
    foreach ($n in "host", "A", "B", "C") {
        Check "lobby4: $n sees all 4 players" ((Lines $l $n "JOINED ok=true players=4").Count -eq 1)
    }
    foreach ($n in "host", "A", "B", "C") {
        $roster = (Lines $l $n "ROSTER") -join ""
        $slots = @([regex]::Matches($roster, 'slot(\d)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
        $locals = [regex]::Matches($roster, 'local=true').Count
        Check "lobby4: $n has 4 distinct slots and exactly one local player" (($slots.Count -eq 4) -and ($locals -eq 1))
    }
    foreach ($n in "host", "A", "B", "C") {
        $moved = 0.0
        foreach ($line in (Lines $l $n "NPC moved=")) { if ($line -match "moved=([0-9.]+)") { $moved = [double]$Matches[1] } }
        Check "lobby4: $n saw the coordinator walk (moved $moved m, needs 0.5 or more)" ($moved -ge 0.5)
    }
    Assert-Quiet $l "lobby4"
}

if (Wanted "contend3") {
    Write-Host "`n== contend3: two clients race for one prop, $rounds rounds"
    $l = Run-Scenario "contend3" 24702 @("A", "B") @{ scenario = "contend"; rounds = $rounds; host_contends = 0 }
    Assert-Contend $l @("A", "B") "contend3"
    Assert-Quiet $l "contend3"
}

if (Wanted "contend_host") {
    Write-Host "`n== contend_host: the host and one client race, $rounds rounds"
    $l = Run-Scenario "contend_host" 24703 @("A") @{ scenario = "contend"; rounds = $rounds; host_contends = 1 }
    Assert-Contend $l @("host", "A") "contend_host"
    Assert-Quiet $l "contend_host"
}

if (Wanted "disconnect") {
    Write-Host "`n== disconnect: a client leaves while holding the prop"
    $l = Run-Scenario "disconnect" 24704 @("A", "B") @{ scenario = "disconnect"; how = "leave" }
    Check "disconnect: B picked the prop up" ((Lines $l "B" "PICKUP ok=true").Count -eq 1)
    Check "disconnect: host noticed B leave while it held the prop" ((Lines $l "host" "was holding 'cake_base'").Count -ge 1)
    Check "disconnect: host shows the prop free right after" ((Lines $l "host" "prop cake_base holder=''").Count -ge 1)
    Check "disconnect: A saw B holding it, then saw it freed" (((Lines $l "A" "SAW holder='peer_").Count -eq 1) -and ((Lines $l "A" "FREED ok=true").Count -eq 1))
    Check "disconnect: A could then pick it up" ((Lines $l "A" "PICKUP_AFTER ok=true").Count -eq 1)
    Assert-Quiet $l "disconnect"
}

if (Wanted "disconnect_kill") {
    Write-Host "`n== disconnect_kill: a client process is killed while holding the prop"
    $l = Run-Scenario "disconnect_kill" 24705 @("A", "B") @{ scenario = "disconnect"; how = "kill" }
    Check "disconnect_kill: host noticed B vanish while it held the prop" ((Lines $l "host" "was holding 'cake_base'").Count -ge 1)
    Check "disconnect_kill: A saw it freed" ((Lines $l "A" "FREED ok=true").Count -eq 1)
    Check "disconnect_kill: A could then pick it up" ((Lines $l "A" "PICKUP_AFTER ok=true").Count -eq 1)
    $freed = (Lines $l "A" "FREED ok=true after=") -join ""
    if ($freed -match 'after=([\d\.]+)s') { Write-Host "      host noticed the dead client about $($Matches[1]) s after A saw B holding" }
}

if (Wanted "far") {
    Write-Host "`n== far: out of range, teleport, and movement sync"
    $l = Run-Scenario "far" 24706 @("A", "B") @{ scenario = "far" }
    Check "far: B walked to the far side of the room" ((Lines $l "B" "WALKED to x=5.").Count -eq 1)
    Check "far: pickup from far away is rejected as out_of_range" ((Lines $l "B" "FAR_PICKUP ok=false reason=out_of_range").Count -eq 1)
    Check "far: teleporting next to the prop does not help (rejected)" ((Lines $l "B" "TELEPORT_PICKUP ok=false reason=out_of_range").Count -eq 1)
    Check "far: after the host has caught up at a believable speed it works" ((Lines $l "B" "ARRIVED_PICKUP ok=true").Count -eq 1)
    foreach ($n in "host", "A") {
        $best = -99.0
        foreach ($line in (Lines $l $n "SEEN ")) { if ($line -match 'maxx=([-\d\.]+)') { $best = [Math]::Max($best, [double]$Matches[1]) } }
        Check "${n}: saw B's walk (furthest x $best, needs 5.0 or more)" ($best -ge 5.0)
    }
    Assert-Quiet $l "far"
}

Write-Host "`n$($script:passed) passed, $($script:failed) failed"
if ($script:failed -gt 0) { exit 1 }
