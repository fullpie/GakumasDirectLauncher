[CmdletBinding()]
param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$Dist = Join-Path $ProjectRoot 'dist'
$Artifacts = Join-Path $ProjectRoot 'artifacts'
$Release = Join-Path $ProjectRoot 'release'

if (-not (Test-Path -LiteralPath $Compiler)) {
    throw 'The .NET Framework C# compiler was not found.'
}

New-Item -ItemType Directory -Path $Dist -Force | Out-Null
New-Item -ItemType Directory -Path $Artifacts -Force | Out-Null
New-Item -ItemType Directory -Path $Release -Force | Out-Null

$References = @(
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Security.dll',
    '/reference:System.Web.Extensions.dll',
    '/reference:System.Windows.Forms.dll'
)

$CoreSources = @(
    (Join-Path $ProjectRoot 'src\Models.cs'),
    (Join-Path $ProjectRoot 'src\DmmLogParser.cs'),
    (Join-Path $ProjectRoot 'src\CacheStore.cs'),
    (Join-Path $ProjectRoot 'src\SafeLogger.cs'),
    (Join-Path $ProjectRoot 'src\LauncherEnvironment.cs')
)

$AppSources = $CoreSources + @(
    (Join-Path $ProjectRoot 'src\UiStrings.cs'),
    (Join-Path $ProjectRoot 'src\ModernControls.cs'),
    (Join-Path $ProjectRoot 'src\MainForm.cs'),
    (Join-Path $ProjectRoot 'src\Program.cs'),
    (Join-Path $ProjectRoot 'src\AssemblyInfo.cs')
)

$AppOutput = Join-Path $Dist 'GakumasDirectLauncher.exe'
& $Compiler /nologo /target:winexe /optimize+ /platform:anycpu /warn:4 `
    "/win32manifest:$ProjectRoot\src\app.manifest" "/out:$AppOutput" $References $AppSources
if ($LASTEXITCODE -ne 0) {
    throw 'Application compilation failed.'
}

if (-not $SkipTests) {
    $TestOutput = Join-Path $Artifacts 'GakumasSmartLauncher.Tests.exe'
    & $Compiler /nologo /target:exe /optimize+ /platform:anycpu /warn:4 `
        "/out:$TestOutput" $References $CoreSources (Join-Path $ProjectRoot 'tests\TestRunner.cs')
    if ($LASTEXITCODE -ne 0) {
        throw 'Test compilation failed.'
    }

    & $TestOutput
    if ($LASTEXITCODE -ne 0) {
        throw 'Automated tests failed.'
    }
}

$DiagnosticsOutput = Join-Path $Artifacts 'GakumasSmartLauncher.Diagnostics.exe'
& $Compiler /nologo /target:exe /optimize+ /platform:anycpu /warn:4 `
    "/out:$DiagnosticsOutput" $References $CoreSources (Join-Path $ProjectRoot 'tests\DiagnosticsProgram.cs')
if ($LASTEXITCODE -ne 0) {
    throw 'Diagnostics compilation failed.'
}

$GuiSmokeOutput = Join-Path $Artifacts 'GakumasSmartLauncher.GuiSmokeTest.exe'
$GuiSmokeImage = Join-Path $Artifacts 'gui-smoke.png'
$GuiSmokeEnglishImage = Join-Path $Artifacts 'gui-smoke-en.png'
& $Compiler /nologo /target:exe /optimize+ /platform:anycpu /warn:4 `
    "/out:$GuiSmokeOutput" $References $CoreSources `
    (Join-Path $ProjectRoot 'src\UiStrings.cs') `
    (Join-Path $ProjectRoot 'src\ModernControls.cs') `
    (Join-Path $ProjectRoot 'src\MainForm.cs') `
    (Join-Path $ProjectRoot 'tests\GuiSmokeTest.cs')
if ($LASTEXITCODE -ne 0) {
    throw 'GUI smoke-test compilation failed.'
}

& $GuiSmokeOutput $GuiSmokeImage 'zh-cn'
if ($LASTEXITCODE -ne 0) {
    throw 'Traditional Chinese GUI layout smoke test failed.'
}

& $GuiSmokeOutput $GuiSmokeEnglishImage 'en'
if ($LASTEXITCODE -ne 0) {
    throw 'English GUI layout smoke test failed.'
}

$Hash = Get-FileHash -LiteralPath $AppOutput -Algorithm SHA256
$ChecksumPath = Join-Path $Artifacts 'SHA256SUMS.txt'
Set-Content -LiteralPath $ChecksumPath -Encoding Ascii -Value ($Hash.Hash + '  GakumasDirectLauncher.exe')

$ReleaseArchive = Join-Path $Release 'GakumasDirectLauncher-v1.2.0.zip'
$PackageItems = @(
    $AppOutput,
    $ChecksumPath,
    (Join-Path $ProjectRoot 'README.md'),
    (Join-Path $ProjectRoot 'README_EN.md'),
    (Join-Path $ProjectRoot 'SECURITY.md'),
    (Join-Path $ProjectRoot 'CHANGELOG.md'),
    (Join-Path $ProjectRoot 'build.ps1'),
    (Join-Path $ProjectRoot 'src'),
    (Join-Path $ProjectRoot 'tests'),
    (Join-Path $ProjectRoot 'docs'),
    (Join-Path $ProjectRoot '.github')
)
Compress-Archive -Path $PackageItems -DestinationPath $ReleaseArchive -CompressionLevel Optimal -Force

$ArchiveHash = Get-FileHash -LiteralPath $ReleaseArchive -Algorithm SHA256
[pscustomobject]@{
    Output = $AppOutput
    Size = (Get-Item -LiteralPath $AppOutput).Length
    SHA256 = $Hash.Hash
    Checksums = $ChecksumPath
    Archive = $ReleaseArchive
    ArchiveSHA256 = $ArchiveHash.Hash
} | Format-List
