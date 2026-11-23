# Runs the EditMode test suite in Unity batch mode.
#   powershell -ExecutionPolicy Bypass -File tools/run-tests.ps1
# Exit code 0 = all passed (or skipped because the editor has the project open).
param([string]$Platform = "EditMode")

$ErrorActionPreference = "Stop"
$project = Split-Path -Parent $PSScriptRoot

$version = (Select-String -Path "$project/ProjectSettings/ProjectVersion.txt" -Pattern '^m_EditorVersion:\s*(\S+)').Matches[0].Groups[1].Value
$unity = if ($env:UNITY_EXE) { $env:UNITY_EXE } else { "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe" }
if (-not (Test-Path $unity)) { Write-Error "Unity $version not found at $unity. Set UNITY_EXE to its path."; exit 1 }

# Batch mode cannot open a project the editor already has open.
$lock = "$project/Temp/UnityLockfile"
if (Test-Path $lock) {
    try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Close() }
    catch {
        Write-Warning "Unity has this project open, so tests were skipped. Run them from Window > General > Test Runner, or close Unity and push again."
        exit 0
    }
}

$results = "$project/Logs/test-results-$Platform.xml"
$log = "$project/Logs/test-run-$Platform.log"
New-Item -ItemType Directory -Force "$project/Logs" | Out-Null
Remove-Item $results -ErrorAction SilentlyContinue

Write-Host "Running $Platform tests with Unity $version (log: $log)..."
$p = Start-Process -FilePath $unity -Wait -PassThru -NoNewWindow -ArgumentList @(
    "-batchmode", "-nographics", "-projectPath", "`"$project`"",
    "-runTests", "-testPlatform", $Platform,
    "-testResults", "`"$results`"", "-logFile", "`"$log`"")

if (-not (Test-Path $results)) {
    Write-Host "No results file: the project probably failed to compile. Last log lines:"
    Get-Content $log -Tail 40
    exit 1
}

[xml]$xml = Get-Content $results
$run = $xml.'test-run'
Write-Host "Passed $($run.passed) / $($run.total), failed $($run.failed), skipped $($run.skipped)."
foreach ($case in $xml.SelectNodes("//test-case[@result='Failed']")) {
    Write-Host "  FAIL $($case.fullname)"
    Write-Host "       $($case.failure.message.'#cdata-section'.Trim())"
}
exit $p.ExitCode
