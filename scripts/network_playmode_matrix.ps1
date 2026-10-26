# Source-authored acceptance entrypoint. Run only after the global source freeze/build approval.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ServerExe,
    [Parameter(Mandatory = $true)][string]$ClientAExe,
    [Parameter(Mandatory = $true)][string]$ClientBExe,
    [Parameter(Mandatory = $true)][ValidatePattern('^[a-f0-9]{32}$')][string]$RunId,
    [Parameter(Mandatory = $true)][ValidatePattern('^(?:[a-fA-F0-9]{40}|[a-fA-F0-9]{64})$')][string]$SnapshotSha,
    [Parameter(Mandatory = $true)][ValidateRange(1024, 65535)][int]$Port,
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [ValidateRange(2, 120)][int]$PhaseTimeout = 20,
    [ValidateRange(30, 3600)][int]$OverallTimeout = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'Windows standalone players are required.' }
if ($OverallTimeout -lt $PhaseTimeout) { throw 'OverallTimeout must cover PhaseTimeout.' }

$company = 'NeoQA_Network_' + $RunId
$products = @{ server = 'NetworkMatrix_server'; 'client-a' = 'NetworkMatrix_client-a'; 'client-b' = 'NetworkMatrix_client-b' }
$executables = @{ server = $ServerExe; 'client-a' = $ClientAExe; 'client-b' = $ClientBExe }
$roles = @('server', 'client-a', 'client-b')
$root = [IO.Path]::GetFullPath($OutputRoot).TrimEnd('\', '/')
if (Test-Path -LiteralPath $root) { throw 'OutputRoot must be a new directory; existing artifacts are never reused/deleted.' }
if ($root -eq [IO.Path]::GetPathRoot($root).TrimEnd('\', '/')) { throw 'OutputRoot cannot be a volume root.' }
$localLow = Join-Path ([IO.Directory]::GetParent([Environment]::GetFolderPath('LocalApplicationData')).FullName) 'LocalLow'
$persistentPaths = @{}
$registryPaths = @{}
foreach ($role in $roles) {
    $executables[$role] = (Resolve-Path -LiteralPath $executables[$role]).ProviderPath
    if ([IO.Path]::GetExtension($executables[$role]) -ne '.exe') { throw "Not a player executable: $role" }
    $persistentPaths[$role] = Join-Path (Join-Path $localLow $company) $products[$role]
    $registryPaths[$role] = 'Software\' + $company + '\' + $products[$role]
    if (Test-Path -LiteralPath $persistentPaths[$role]) { throw "Task persistence already exists: $role" }
    $existingKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($registryPaths[$role])
    if ($null -ne $existingKey) { $existingKey.Dispose(); throw "Task PlayerPrefs namespace already exists: $role" }
}
if (@($executables.Values | Select-Object -Unique).Count -ne 3) {
    throw 'Build three player executables with the three declared product identities.'
}

[void][IO.Directory]::CreateDirectory($root)
$utf8 = New-Object System.Text.UTF8Encoding($false, $true)
[IO.File]::WriteAllText((Join-Path $root 'owned-run.json'),
    (@{ runId = $RunId; snapshotSha = $SnapshotSha; company = $company } | ConvertTo-Json -Compress), $utf8)
foreach ($role in $roles) { [void][IO.Directory]::CreateDirectory((Join-Path $root $role)) }
$peerProcesses = @{}
$records = @{}
$cleanupFailures = New-Object 'System.Collections.Generic.List[string]'
$failure = $null
$clock = [Diagnostics.Stopwatch]::StartNew()
$portMutex = $null
$mutexOwned = $false
$reservation = $null

function Write-Receipt([string]$name, [object]$value) {
    [IO.File]::WriteAllText((Join-Path $root $name), ($value | ConvertTo-Json -Depth 24), $utf8)
}

function Quoted-Argument([string]$value) {
    # Windows paths cannot contain quotes. All supplied values are files or trimmed directories;
    # disallow terminal backslashes so CommandLineToArgvW cannot consume the closing quote.
    if ($value.Contains('"') -or $value.Contains("`r") -or $value.Contains("`n") -or $value.EndsWith('\')) {
        throw 'An argument contains unsupported quoting or a terminal backslash.'
    }
    return '"' + $value + '"'
}

function Start-OwnedPeer([string]$role) {
    $arguments = @('-batchmode', '-nographics', '-logFile', (Join-Path (Join-Path $root $role) 'player.log'),
        '-neoNetworkMatrix', '-neoNetworkMatrixRunId', $RunId, '-neoNetworkMatrixSnapshot', $SnapshotSha,
        '-neoNetworkMatrixPeer', $role, '-neoNetworkMatrixRoot', $root, '-neoNetworkMatrixCompany', $company,
        '-neoNetworkMatrixProduct', $products[$role], '-neoNetworkMatrixPersistentDataPath', $persistentPaths[$role],
        '-neoNetworkMatrixPort', $Port.ToString([Globalization.CultureInfo]::InvariantCulture),
        '-neoNetworkMatrixPhaseTimeout', $PhaseTimeout.ToString([Globalization.CultureInfo]::InvariantCulture),
        '-neoNetworkMatrixOverallTimeout', $OverallTimeout.ToString([Globalization.CultureInfo]::InvariantCulture))
    $commandLine = ($arguments | ForEach-Object { Quoted-Argument $_ }) -join ' '
    $process = Start-Process -FilePath $executables[$role] -ArgumentList $commandLine -PassThru -WindowStyle Hidden `
        -WorkingDirectory ([IO.Path]::GetDirectoryName($executables[$role]))
    $peerProcesses.Add($role, $process)
    Write-Receipt 'owned-processes.json' @($peerProcesses.Keys | ForEach-Object {
        @{ peerId = $_; processId = $peerProcesses[$_].Id; exe = $executables[$_]; product = $products[$_];
            persistentDataPath = $persistentPaths[$_]; registryNamespace = 'HKEY_CURRENT_USER\' + $registryPaths[$_] }
    })
}

function Read-PeerRecords([string]$role) {
    $path = Join-Path (Join-Path $root $role) 'events.jsonl'
    if (-not [IO.File]::Exists($path)) { return @() }
    $stream = New-Object IO.FileStream($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    try {
        if ($stream.Length -gt 8MB) { throw "Unbounded receipt stream: $role" }
        $bytes = New-Object byte[] ([int]$stream.Length)
        $offset = 0
        while ($offset -lt $bytes.Length) {
            $read = $stream.Read($bytes, $offset, $bytes.Length - $offset)
            if ($read -eq 0) { break }
            $offset += $read
        }
        $lastNewline = $offset - 1
        while ($lastNewline -ge 0 -and $bytes[$lastNewline] -ne 10) { $lastNewline-- }
        if ($lastNewline -lt 0) { return @() }
        # Decode only complete lines: a concurrent writer may be inside a multibyte UTF-8 scalar.
        $content = $utf8.GetString($bytes, 0, $lastNewline + 1)
    } finally { $stream.Dispose() }
    $lines = $content.Split([char]10)
    for ($index = 0; $index -lt $lines.Length - 1; $index++) {
        if ([string]::IsNullOrWhiteSpace($lines[$index])) { continue }
        $record = $lines[$index] | ConvertFrom-Json
        if ($record.schema -ne 1 -or $record.runId -cne $RunId -or $record.snapshotSha -cne $SnapshotSha `
            -or $record.peerId -cne $role -or $record.processId -ne $peerProcesses[$role].Id) {
            throw "Foreign/corrupt JSONL identity: $role"
        }
        $record
    }
}

function Refresh-Records {
    foreach ($role in @($peerProcesses.Keys)) {
        $records[$role] = @(Read-PeerRecords $role)
        foreach ($record in $records[$role]) {
            if ($record.kind -eq 'failure' -or -not $record.passed) { throw "$role : $($record.detail)" }
            if ($record.kind -eq 'environment') {
                $environment = $record.environment
                if ($environment.company -cne $company -or $environment.product -cne $products[$role] `
                    -or $environment.persistentDataPath -ine $persistentPaths[$role] `
                    -or $environment.saveRoot -ine (Join-Path (Join-Path $root $role) 'save') `
                    -or $environment.playerPrefsNamespace -ine ('HKEY_CURRENT_USER\' + $registryPaths[$role]) `
                    -or $environment.processId -ne $peerProcesses[$role].Id -or $environment.serverPort -ne $Port `
                    -or $environment.transport -ne 'Mirror.TelepathyTransport') { throw "Isolation probe failed: $role" }
            }
        }
    }
}

function Wait-MatrixEvent([string]$kind) {
    $phaseCount = 1
    if ($kind -eq 'launch-client-b') { $phaseCount = 4 }
    $end = [Math]::Min($OverallTimeout, $clock.Elapsed.TotalSeconds + $phaseCount * $PhaseTimeout)
    while ($clock.Elapsed.TotalSeconds -lt $end) {
        Refresh-Records
        if (@($records['server'] | Where-Object { $_.kind -eq $kind }).Count -gt 0) { return }
        foreach ($role in @($peerProcesses.Keys)) {
            if ($peerProcesses[$role].HasExited) { throw "Peer exited before server barrier '$kind': $role" }
        }
        Start-Sleep -Milliseconds 50 # Poll an explicit barrier; never infer completion from elapsed sleep.
    }
    throw "Realtime deadline waiting for server barrier: $kind"
}

function Single-Receiver([string]$role, [string]$caseId) {
    $phaseReceivers = @($records[$role] | Where-Object { $_.kind -eq 'receiver' -and $_.caseId -eq $caseId })
    if ($phaseReceivers.Count -ne 1) { throw "Expected exactly one receiver receipt: $role / $caseId" }
    return $phaseReceivers[0]
}

function Assert-Events([object]$state, [string]$name, [float[]]$wanted, [bool]$unordered = $false) {
    $actual = @($state.events | Where-Object { $_.name -eq $name } | ForEach-Object { $_.values })
    $expected = @()
    if ($null -ne $wanted) { $expected = @($wanted) }
    if ($unordered) { $actual = @($actual | Sort-Object); $expected = @($expected | Sort-Object) }
    if ($actual.Count -ne $expected.Count) { throw "Received count differs for $name" }
    for ($index = 0; $index -lt $actual.Count; $index++) {
        if ([Math]::Abs([double]$actual[$index] - [double]$expected[$index]) -gt 0.0001) {
            throw "Received payload differs for $name"
        }
    }
}

function Assert-NI7Events([object]$state, [string]$name, [single[]]$wanted) {
    # These are Unity float32 values; parse that domain before comparing large serialized balances.
    $actual = @($state.events | Where-Object { $_.name -eq $name } | ForEach-Object { $_.values })
    $expected = @()
    if ($null -ne $wanted) { $expected = @($wanted) }
    if ($actual.Count -ne $expected.Count) { throw "NI7 received count differs for $name" }
    for ($index = 0; $index -lt $actual.Count; $index++) {
        if ([single]$actual[$index] -ne [single]$expected[$index]) { throw "NI7 received payload differs for $name" }
    }
}

function Assert-NI7Wallet([object]$state, [single]$current, [single]$last, [single]$before) {
    $wallet = $state.precision
    if ($null -eq $wallet -or -not $wallet.exists -or -not $wallet.startCompleted `
        -or [single]$wallet.current -ne $current -or [single]$wallet.lifetime -ne 123 `
        -or [single]$wallet.level -ne 7 -or [single]$wallet.lastChange -ne $last `
        -or [single]$wallet.storedCurrent -ne $current -or [single]$wallet.storedLifetime -ne 123 `
        -or [single]$wallet.phaseBalanceBefore -ne $before `
        -or [single]$wallet.storedDebit -ne [single]($before - $current) `
        -or $wallet.persistenceSha256 -notmatch '^[a-f0-9]{64}$' -or $state.money -ne 213) {
        throw 'NI7 wallet/persistence/stored debit differs, or the original Money213 wallet changed.'
    }
    Assert-NI7Events $state 'ni7-lifetime' @()
    Assert-NI7Events $state 'ni7-level' @()
    Assert-NI7Events $state 'ni7-save' @()
}

function Assert-NI7Receipts {
    $deniedCase = 'money-ni7-zero-debit-denied'
    $roundedCase = 'money-ni7-rounded-debit-budget'
    $fractionalCase = 'money-ni7-fractional-control'
    foreach ($caseId in @($deniedCase, $roundedCase, $fractionalCase)) {
        $serverRows = @($records['server'] | Where-Object { $_.kind -eq 'server-receiver' -and $_.caseId -eq $caseId })
        if ($serverRows.Count -ne 1) { throw "Missing NI7 authoritative receiver: $caseId" }
        $states = @{ server = $serverRows[0].state;
            'client-a' = (Single-Receiver 'client-a' $caseId).state;
            'client-b' = (Single-Receiver 'client-b' $caseId).state }
        foreach ($role in $roles) {
            $state = $states[$role]
            if ($caseId -eq $deniedCase) {
                Assert-NI7Wallet $state 100000000 17 100000000
                if (-not $state.precision.persistenceFileUnchanged) { throw 'NI7 rejected spend changed the persisted file.' }
                foreach ($name in @('ni7-current', 'ni7-last-change', 'ni7-persist-current', 'ni7-persist-lifetime',
                    'ni7-rpc-op', 'ni7-rpc-amount')) { Assert-NI7Events $state $name @() }
                if ($role -eq 'server') {
                    Assert-NI7Events $state 'ni7-command-op' @(1)
                    Assert-NI7Events $state 'ni7-command-amount' @(1)
                }
            } else {
                $balance = [single]99999992
                $price = [single]5
                $before = [single]100000000
                if ($caseId -eq $fractionalCase) { $balance = [single]10.25; $price = [single]0.25; $before = [single]10.5 }
                Assert-NI7Wallet $state $balance (-$price) $before
                $currentEvents = @($balance)
                if ($caseId -eq $roundedCase -and $role -eq 'client-a') { $currentEvents = @(0, 100000000, 99999992) }
                Assert-NI7Events $state 'ni7-current' $currentEvents
                Assert-NI7Events $state 'ni7-last-change' @(-$price)
                Assert-NI7Events $state 'ni7-persist-current' @($balance)
                Assert-NI7Events $state 'ni7-persist-lifetime' @(123)
                if ($role -eq 'server') {
                    $commands = @($price)
                    $operations = @(1)
                    if ($caseId -eq $roundedCase) { $commands = @(1, 5); $operations = @(1, 1) }
                    Assert-NI7Events $state 'ni7-command-amount' $commands
                    Assert-NI7Events $state 'ni7-command-op' $operations
                    Assert-NI7Events $state 'ni7-rpc-amount' @($price, $price)
                    Assert-NI7Events $state 'ni7-rpc-op' @(1, 1)
                } else {
                    Assert-NI7Events $state 'ni7-rpc-amount' @($price)
                    Assert-NI7Events $state 'ni7-rpc-op' @(1)
                }
            }
        }
        $local = @($records['client-a'] | Where-Object { $_.kind -eq 'money-request-status' -and $_.caseId -eq $caseId })
        $remote = @($records['server'] | Where-Object {
            $_.kind -eq 'remote-request-status' -and $_.caseId -eq $caseId -and $_.fromPeer -eq 'client-a'
        })
        if ($local.Count -ne 1 -or $remote.Count -ne 1 -or $remote[0].step -ne $local[0].step `
            -or $remote[0].epoch -ne $local[0].epoch -or $remote[0].connectionId -lt 0 `
            -or ($remote[0].spendRequests | ConvertTo-Json -Depth 8 -Compress) -cne ($local[0].spendRequests | ConvertTo-Json -Depth 8 -Compress)) {
            throw "NI7 client return-status receipt was not received intact: $caseId"
        }
        $amounts = @(1)
        $localBefore = [single]100000000
        if ($caseId -eq $roundedCase) { $amounts = @(1, 5); $localBefore = [single]0 }
        if ($caseId -eq $fractionalCase) { $amounts = @(0.25); $localBefore = [single]10.5 }
        if (@($local[0].spendRequests).Count -ne $amounts.Count) { throw 'NI7 missing public TrySpend return values.' }
        for ($index = 0; $index -lt $amounts.Count; $index++) {
            $request = $local[0].spendRequests[$index]
            if ([single]$request.amount -ne [single]$amounts[$index] -or $request.status -cne 'RequestedServerAuthority' `
                -or $request.isConfirmed -or -not $request.isPendingServerAuthority `
                -or [single]$request.balanceBefore -ne $localBefore -or [single]$request.balanceAfter -ne $localBefore) {
                throw 'NI7 pending client return was confused with a confirmed server spend.'
            }
        }
    }
    $rounded = @($records['server'] | Where-Object { $_.kind -eq 'server-receiver' -and $_.caseId -eq $roundedCase })[0].state.precision
    if (@($rounded.ingressFrames).Count -ne 2 -or @($rounded.ingressTimes).Count -ne 2 `
        -or $rounded.ingressFrames[0] -ne $rounded.ingressFrames[1] `
        -or $rounded.ingressTimes[1] -lt $rounded.ingressTimes[0] `
        -or $rounded.ingressTimes[1] - $rounded.ingressTimes[0] -ge 0.05 -or [single]$rounded.storedDebit -ne 8) {
        throw 'NI7 denied1/accepted5 was not observed in one server budget window with actual stored debit8.'
    }
    foreach ($caseId in @($roundedCase, $fractionalCase)) {
        foreach ($role in $roles) {
            $reload = @($records[$role] | Where-Object { $_.kind -eq 'money-persistence-reload' -and $_.caseId -eq $caseId })
            $wanted = [single]99999992
            if ($caseId -eq $fractionalCase) { $wanted = [single]10.25 }
            if ($reload.Count -ne 1 -or [single]$reload[0].state.precision.storedCurrent -ne $wanted `
                -or [single]$reload[0].state.precision.storedLifetime -ne 123 `
                -or $reload[0].state.precision.persistenceFileUnchanged) { throw "NI7 missing actual persisted positive control: $role / $caseId" }
        }
    }
}

function Get-NI19Vectors {
    $nonfinite = @([single]::NaN, [single]::PositiveInfinity, [single]::NegativeInfinity)
    return @(
        @{ key = 'set-nonfinite'; operation = 'Set'; initial = [single]10.5; invalid = $nonfinite; valid = [single]12.5; result = [single]12.5; integer = $false },
        @{ key = 'add-nonfinite'; operation = 'Add'; initial = [single]10.5; invalid = $nonfinite; valid = [single]0.25; result = [single]10.75; integer = $false },
        @{ key = 'multiply-nonfinite'; operation = 'Multiply'; initial = [single]10.5; invalid = $nonfinite; valid = [single]2; result = [single]21; integer = $false },
        @{ key = 'add-positive-overflow'; operation = 'Add'; initial = [single]2e38; invalid = @([single]2e38); valid = [single](-2e38); result = [single]0; integer = $false },
        @{ key = 'add-negative-overflow'; operation = 'Add'; initial = [single](-2e38); invalid = @([single](-2e38)); valid = [single]2e38; result = [single]0; integer = $false },
        @{ key = 'multiply-positive-overflow'; operation = 'Multiply'; initial = [single]2e38; invalid = @([single]2); valid = [single]0.5; result = [single]1e38; integer = $false },
        @{ key = 'multiply-negative-overflow'; operation = 'Multiply'; initial = [single](-2e38); invalid = @([single]2); valid = [single]0.5; result = [single](-1e38); integer = $false },
        @{ key = 'int-multiply-overflow'; operation = 'Multiply'; initial = [single]2; invalid = @([single]::MaxValue); valid = [single]3; result = [single]6; integer = $true }
    )
}

function NI19-Bits([single]$value) {
    return [BitConverter]::ToUInt32([BitConverter]::GetBytes($value), 0).ToString('x8', [Globalization.CultureInfo]::InvariantCulture)
}

function Assert-NI19State([object]$state, [string]$key, [bool]$integer, [single]$value) {
    $counter = $state.counterInput
    $bits = NI19-Bits $value
    if ($null -eq $counter -or -not $counter.exists -or -not $counter.startCompleted -or $counter.netId -eq 0 `
        -or $counter.caseKey -cne $key -or $counter.integerMode -ne $integer `
        -or $counter.valueKind -cne 'Finite' -or $counter.storedKind -cne 'Finite' `
        -or $counter.valueBits -cne $bits -or $counter.storedBits -cne $bits `
        -or $counter.persistenceSha256 -notmatch '^[a-f0-9]{64}$' -or $state.counter -ne 12 -or $state.money -ne 213) {
        throw "NI19 value/persisted float bits or original Counter12/Money213 differ: $key"
    }
}

function Assert-NI19Effects([object]$state, [bool]$integer, [single[]]$values, [int]$recipients) {
    Assert-NI7Events $state 'ni19-reactive' $values
    Assert-NI7Events $state 'ni19-value' $values
    Assert-NI7Events $state 'ni19-key-write' $values
    $marker = @()
    if ($null -ne $values -and $values.Count -ne 0) { $marker = @(1) }
    if ($integer) {
        Assert-NI7Events $state 'ni19-int-event' $marker
        Assert-NI7Events $state 'ni19-float-event' @()
    } else {
        Assert-NI7Events $state 'ni19-float-event' $marker
        Assert-NI7Events $state 'ni19-int-event' @()
    }
    $rpcs = @()
    for ($index = 0; $index -lt $recipients; $index++) { $rpcs += $values[0] }
    Assert-NI7Events $state 'ni19-rpc' $rpcs
    foreach ($name in @('ni19-save', 'ni19-reactive-nonfinite', 'ni19-value-nonfinite',
        'ni19-key-write-nonfinite', 'ni19-rpc-nonfinite')) { Assert-NI7Events $state $name @() }
}

function NI19-ServerReceiver([string]$caseId) {
    $receiverRows = @($records['server'] | Where-Object { $_.kind -eq 'server-receiver' -and $_.caseId -eq $caseId })
    if ($receiverRows.Count -ne 1) { throw "NI19 missing authoritative receiver: $caseId" }
    return $receiverRows[0].state
}

function Assert-NI19Receipts {
    foreach ($vector in @(Get-NI19Vectors)) {
        foreach ($suffix in @('denied', 'budget')) {
            $caseId = 'counter-ni19-' + $vector.key + '-' + $suffix
            $server = NI19-ServerReceiver $caseId
            $wanted = $vector.initial
            if ($suffix -eq 'budget') { $wanted = $vector.result }
            foreach ($role in $roles) {
                $state = $server
                if ($role -ne 'server') { $state = (Single-Receiver $role $caseId).state }
                Assert-NI19State $state $vector.key $vector.integer $wanted
                $values = @()
                $recipients = 0
                if ($suffix -eq 'budget') {
                    $values = @($wanted)
                    $recipients = 1
                    if ($role -eq 'server') { $recipients = 2 }
                }
                Assert-NI19Effects $state $vector.integer $values $recipients
                if ($suffix -eq 'denied' -and -not $state.counterInput.persistenceFileUnchanged) {
                    throw "NI19 rejected request changed the persisted file: $role / $caseId"
                }
                if ($role -ne 'server' -and @($state.counterInput.commands).Count -ne 0) {
                    throw 'NI19 client evidence contains simulated server ingress.'
                }
            }
            $expectedOperands = @($vector.invalid)
            if ($suffix -eq 'budget') { $expectedOperands += $vector.valid }
            $commands = @($server.counterInput.commands)
            if ($commands.Count -ne $expectedOperands.Count) { throw "NI19 real Command count differs: $caseId" }
            for ($index = 0; $index -lt $commands.Count; $index++) {
                $command = $commands[$index]
                $operand = [single]$expectedOperands[$index]
                $kind = 'Finite'
                if ([single]::IsNaN($operand)) { $kind = 'NaN' }
                elseif ([single]::IsPositiveInfinity($operand)) { $kind = '+Infinity' }
                elseif ([single]::IsNegativeInfinity($operand)) { $kind = '-Infinity' }
                $bitsMatch = $command.operandBits -ceq (NI19-Bits $operand)
                if ($kind -eq 'NaN' -and $command.operandBits -match '^[a-f0-9]{8}$') {
                    $raw = [Convert]::ToUInt32($command.operandBits, 16)
                    $bitsMatch = [single]::IsNaN([BitConverter]::ToSingle([BitConverter]::GetBytes([uint32]$raw), 0))
                }
                if ($command.operation -cne $vector.operation -or $command.operandKind -cne $kind -or -not $bitsMatch `
                    -or $command.beforeKind -cne 'Finite' -or $command.beforeBits -cne (NI19-Bits $vector.initial) `
                    -or $command.valueEventsBefore -ne 0 -or $command.keyWritesBefore -ne 0 -or $command.rpcRecipientsBefore -ne 0) {
                    throw "NI19 malformed ingress affected state/events/save/RPC before the next Command: $caseId"
                }
            }
            if ($suffix -eq 'budget') {
                $last = $commands.Count - 1
                if ($commands[0].frame -ne $commands[$last].frame `
                    -or $commands[$last].unscaledTime -lt $commands[0].unscaledTime `
                    -or $commands[$last].unscaledTime - $commands[0].unscaledTime -ge 0.05) {
                    throw "NI19 malformed/valid requests did not share the server budget window: $caseId"
                }
                foreach ($role in $roles) {
                    $reloadRows = @($records[$role] | Where-Object {
                        $_.kind -eq 'counter-input-persistence-reload' -and $_.caseId -eq $caseId
                    })
                    if ($reloadRows.Count -ne 1 -or $reloadRows[0].state.counterInput.storedBits -cne (NI19-Bits $wanted) `
                        -or $reloadRows[0].state.counterInput.persistenceFileUnchanged) {
                        throw "NI19 missing actual positive-control file reload: $role / $caseId"
                    }
                }
            }
        }
    }
    $firstCase = 'counter-ni19-finite-peer-a-rate'
    $secondCase = 'counter-ni19-independent-peer-b'
    $first = NI19-ServerReceiver $firstCase
    $second = NI19-ServerReceiver $secondCase
    foreach ($role in $roles) {
        $a = $first
        $b = $second
        $recipients = 2
        if ($role -ne 'server') {
            $a = (Single-Receiver $role $firstCase).state
            $b = (Single-Receiver $role $secondCase).state
            $recipients = 1
        }
        Assert-NI19State $a 'finite-peers' $false 10.75
        Assert-NI19State $b 'finite-peers' $false 21.5
        Assert-NI19Effects $a $false @(10.75) $recipients
        Assert-NI19Effects $b $false @(21.5) $recipients
    }
    $aCommands = @($first.counterInput.commands)
    $bCommands = @($second.counterInput.commands)
    if ($aCommands.Count -ne 2 -or $bCommands.Count -ne 1 -or $aCommands[0].operation -cne 'Add' `
        -or $aCommands[1].operation -cne 'Add' -or $aCommands[0].operandBits -cne (NI19-Bits 0.25) `
        -or $aCommands[1].operandBits -cne (NI19-Bits 0.5) -or $aCommands[0].frame -ne $aCommands[1].frame `
        -or $aCommands[1].valueEventsBefore -ne 1 -or $aCommands[1].keyWritesBefore -ne 1 `
        -or $aCommands[1].rpcRecipientsBefore -ne 2 -or $bCommands[0].operation -cne 'Multiply' `
        -or $bCommands[0].operandBits -cne (NI19-Bits 2) -or $bCommands[0].beforeBits -cne (NI19-Bits 10.75) `
        -or $bCommands[0].unscaledTime - $aCommands[0].unscaledTime -ge 1000) {
        throw 'NI19 finite same-peer throttling/independent-peer control was not observed on the real server.'
    }
}

function Assert-MatrixReceipts {
    $allCases = @('late-join-initial', 'counter-concurrent-relative-add', 'owner-admission',
        'unknown-authority-denied', 'known-authority-positive-control', 'local-only-ingress-denied',
        'local-only-budget-intact', 'per-peer-rate-limit', 'scope-all-clients', 'scope-server-only',
        'scope-others-only', 'selector-nonowner-denied', 'selector-owner-additive', 'selector-complete-snapshot',
        'money-server-only-default', 'money-unknown-policy', 'money-malformed-budget-intact', 'money-cooperative-a',
        'money-cooperative-b', 'money-owner-only', 'handler-replaced-older-disposed', 'handler-current-disposed',
        'polling-paused-null-retry', 'physics-context-target', 'late-join-reconnect', 'handler-client-reconnect',
        'server-restart-snapshot', 'handler-server-restart', 'money-ni7-zero-debit-denied',
        'money-ni7-rounded-debit-budget', 'money-ni7-fractional-control', 'money-ni7-original-wallet-preserved')
    foreach ($vector in @(Get-NI19Vectors)) {
        $allCases += 'counter-ni19-' + $vector.key + '-denied'
        $allCases += 'counter-ni19-' + $vector.key + '-budget'
    }
    $allCases += @('counter-ni19-finite-peer-a-rate', 'counter-ni19-independent-peer-b',
        'counter-ni19-original-oracles-preserved')
    foreach ($role in $roles) {
        if (@($records[$role] | Where-Object { $_.kind -eq 'environment' }).Count -ne 1 `
            -or @($records[$role] | Where-Object { $_.kind -eq 'store-isolated' }).Count -ne 1 `
            -or @($records[$role] | Where-Object { $_.kind -eq 'cleanup' -and $_.passed }).Count -ne 1 `
            -or @($records[$role] | Where-Object { $_.kind -eq 'result' -and $_.passed }).Count -ne 1) {
            throw "Missing environment/store/cleanup/result receipt: $role"
        }
        if ($peerProcesses[$role].ExitCode -ne 0) { throw "Player exit code: $role / $($peerProcesses[$role].ExitCode)" }
    }
    $serverPhysics = @($records['server'] | Where-Object {
        $_.kind -eq 'server-receiver' -and $_.caseId -eq 'physics-context-target'
    })
    if ($serverPhysics.Count -ne 1) { throw 'Missing authoritative physics/context receiver.' }
    $physicsState = $serverPhysics[0].state
    $physicsActor = $physicsState.playerA
    if ($physicsActor -eq 0 -or $physicsActor -eq $physicsState.playerB `
        -or $physicsState.bonusA -or $physicsState.bonusB) {
        throw 'Dedicated AllClients context must preserve both inactive server children and identify A.'
    }
    Assert-Events $physicsState 'physics' @($physicsActor)
    $serverLate = @($records['server'] | Where-Object {
        $_.kind -eq 'server-receiver' -and $_.caseId -eq 'late-join-reconnect'
    })
    if ($serverLate.Count -ne 1) { throw 'Missing authoritative reconnect/context receiver.' }
    if ($serverLate[0].state.playerA -ne $physicsActor `
        -or $serverLate[0].state.bonusA -or $serverLate[0].state.bonusB) {
        throw 'B reconnect must retain A identity and both inactive dedicated-server children.'
    }
    foreach ($peer in @('client-a', 'client-b')) {
        foreach ($caseId in $allCases) {
            $local = Single-Receiver $peer $caseId
            $remote = @($records['server'] | Where-Object {
                $_.kind -eq 'remote-receiver' -and $_.caseId -eq $caseId -and $_.fromPeer -eq $peer
            })
            if ($remote.Count -ne 1 -or $remote[0].step -ne $local.step -or $remote[0].epoch -ne $local.epoch `
                -or ($remote[0].state | ConvertTo-Json -Depth 16 -Compress) -cne ($local.state | ConvertTo-Json -Depth 16 -Compress)) {
                throw "Server did not receive the client's exact observed snapshot: $peer / $caseId"
            }
        }
        $counter = (Single-Receiver $peer 'counter-concurrent-relative-add').state
        if ($counter.counter -ne 12) { throw 'Counter receiver is not 12.' }
        Assert-Events $counter 'counter' @(13, 12)
        Assert-Events (Single-Receiver $peer 'owner-admission').state 'owner' @(11)
        Assert-Events (Single-Receiver $peer 'scope-all-clients').state 'all' @(71)
        Assert-Events (Single-Receiver $peer 'scope-server-only').state 'server' @()
        Assert-Events (Single-Receiver $peer 'scope-others-only').state 'others' $(if ($peer -eq 'client-b') { @(73) } else { @() })
        $physics = (Single-Receiver $peer 'physics-context-target').state
        if ($physics.playerA -ne $physicsActor -or $physics.playerB -ne $physicsState.playerB `
            -or -not $physics.bonusA -or $physics.bonusB) {
            throw "AllClients context must activate only A's child on each current client: $peer"
        }
        Assert-Events $physics 'physics' @($physicsActor)
        $late = (Single-Receiver $peer 'late-join-reconnect').state
        $expectedLateBonusA = $peer -eq 'client-a'
        if ($late.counter -ne 12 -or $late.selector -ne 3 -or ($late.activeIndices -join ',') -ne '0,2,3' `
            -or ($late.excludedIndices -join ',') -ne '1' -or $late.money -ne 213 -or $late.hp -ne 73 `
            -or $late.maxHp -ne 120 -or $late.resource -ne 7 -or $late.property -ne 9 -or $late.reactive -ne 15 `
            -or $late.playerA -ne $physicsActor -or $late.playerB -ne $serverLate[0].state.playerB `
            -or $late.bonusA -ne $expectedLateBonusA -or $late.bonusB) { throw "Late-join payload differs: $peer" }
        foreach ($entry in @(@('handler-replaced-older-disposed', 101), @('handler-current-disposed', 103),
            @('handler-client-reconnect', 105), @('handler-server-restart', 107))) {
            Assert-Events (Single-Receiver $peer $entry[0]).state 'handler-client' @($entry[1])
        }
    }
    $initialRpg = (Single-Receiver 'client-a' 'rpg-final-initialization').state
    if ($initialRpg.hp -ne 120 -or $initialRpg.maxHp -ne 120 -or $initialRpg.resource -ne 10) {
        throw 'Client A did not receive the complete server initialization over its deliberately different local template.'
    }
    $serverCounter = @($records['server'] | Where-Object {
        $_.kind -eq 'server-receiver' -and $_.caseId -eq 'counter-concurrent-relative-add'
    })
    if ($serverCounter.Count -ne 2) { throw 'Missing pre-publication Counter delta receipt.' }
    Assert-Events $serverCounter[0].state 'counter' @(11, 12)
    $rate = @($records['server'] | Where-Object { $_.kind -eq 'server-receiver' -and $_.caseId -eq 'per-peer-rate-limit' })
    Assert-Events $rate[0].state 'rate' @(51, 52) $true
    Assert-NI7Receipts
    Assert-NI19Receipts
    if (@($peerProcesses.Values | ForEach-Object { $_.Id } | Select-Object -Unique).Count -ne 3) {
        throw 'The peers did not have three distinct OS process IDs.'
    }
}

try {
    $portMutex = New-Object Threading.Mutex($false, ('Local\NeoNetworkMatrix_Port_' + $Port))
    try { $mutexOwned = $portMutex.WaitOne(0) } catch [Threading.AbandonedMutexException] { $mutexOwned = $true }
    if (-not $mutexOwned) { throw 'Another matrix launcher owns this port lease.' }
    $reservation = New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback, $Port)
    $reservation.Server.ExclusiveAddressUse = $true
    $reservation.Start()
    Write-Receipt 'port-lease.json' @{ runId = $RunId; port = $Port; address = '127.0.0.1'; launcherProcessId = $PID }
    $reservation.Stop()
    $reservation = $null
    # Bind races fail the owned server; never enumerate or kill a foreign listener/process.
    Start-OwnedPeer 'server'
    Wait-MatrixEvent 'server-listening'
    Start-OwnedPeer 'client-a'
    Wait-MatrixEvent 'launch-client-b'
    Start-OwnedPeer 'client-b'
    while ($clock.Elapsed.TotalSeconds -lt $OverallTimeout) {
        Refresh-Records
        if (@($peerProcesses.Values | Where-Object { -not $_.HasExited }).Count -eq 0) { break }
        foreach ($role in @($peerProcesses.Keys)) {
            if ($peerProcesses[$role].HasExited -and @($records[$role] | Where-Object {
                $_.kind -eq 'result' -and $_.passed
            }).Count -eq 0) { throw "Peer exited without a successful result receipt: $role" }
        }
        Start-Sleep -Milliseconds 50
    }
    if (@($peerProcesses.Values | Where-Object { -not $_.HasExited }).Count -gt 0) { throw 'Overall realtime process deadline expired.' }
    Refresh-Records
    Assert-MatrixReceipts
} catch {
    $failure = $_.Exception.ToString()
} finally {
    if ($null -ne $reservation) { $reservation.Stop() }
    foreach ($role in @($peerProcesses.Keys)) {
        $process = $peerProcesses[$role]
        try {
            if (-not $process.HasExited) {
                $process.Kill()
                if (-not $process.WaitForExit(5000)) { throw 'Owned player survived termination deadline.' }
                $cleanupFailures.Add("$role required forced termination; player cleanup was not proven.")
            }
        } catch { $cleanupFailures.Add($role + ': ' + $_.Exception.Message) }
    }
    $registryEvidence = @()
    foreach ($role in $roles) {
        $key = $null
        try {
            $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($registryPaths[$role])
            $registryEvidence += @{ peerId = $role; path = 'HKEY_CURRENT_USER\' + $registryPaths[$role];
                exists = ($null -ne $key); valueNames = $(if ($null -ne $key) { @($key.GetValueNames()) } else { @() });
                persistentDataPath = $persistentPaths[$role]; persistentPathExists = [IO.Directory]::Exists($persistentPaths[$role]) }
        } catch { $cleanupFailures.Add('Persistence probe ' + $role + ': ' + $_.Exception.Message) }
        finally { if ($null -ne $key) { $key.Dispose() } }
    }
    try { Write-Receipt 'persistence-probe.json' $registryEvidence }
    catch { $cleanupFailures.Add('Persistence receipt: ' + $_.Exception.Message) }
    try {
        if ($mutexOwned) { $portMutex.ReleaseMutex() }
        if ($null -ne $portMutex) { $portMutex.Dispose() }
    } catch { $cleanupFailures.Add('Port lease: ' + $_.Exception.Message) }
    Write-Receipt 'matrix-result.json' @{ schema = 1; runId = $RunId; snapshotSha = $SnapshotSha;
        passed = ($null -eq $failure -and $cleanupFailures.Count -eq 0); firstFailure = $failure;
        cleanupFailures = $cleanupFailures.ToArray(); elapsedSeconds = $clock.Elapsed.TotalSeconds;
        retainedRoot = $root; port = $Port; company = $company }
}
if ($null -ne $failure -or $cleanupFailures.Count -gt 0) {
    Write-Error "Network matrix failed; see $(Join-Path $root 'matrix-result.json')"
    exit 1
}
Write-Output (Join-Path $root 'matrix-result.json')
exit 0
