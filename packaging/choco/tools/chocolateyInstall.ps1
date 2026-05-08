$ErrorActionPreference = 'Stop'

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$version  = $env:chocolateyPackageVersion

$packageArgs = @{
    packageName    = $env:chocolateyPackageName
    unzipLocation  = $toolsDir
    url64bit       = "https://github.com/HueByte/whodat/releases/download/v__VERSION__/whodat-x86_64-pc-windows-msvc.zip"
    checksum64     = '__CHECKSUM64__'
    checksumType64 = 'sha256'
}

Install-ChocolateyZipPackage @packageArgs

# Expose `whodat` on PATH via a Chocolatey shim.
$exePath = Join-Path $toolsDir 'whodat.exe'
Install-BinFile -Name 'whodat' -Path $exePath
