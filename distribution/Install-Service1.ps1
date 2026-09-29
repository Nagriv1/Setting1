# Inspect this script and run it under your existing execution policy.
[CmdletBinding()]
param(
 [ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')][string]$OfficialRepository='Nagriv1/Setting1',
 [ValidatePattern('^v[0-9]+\.[0-9]+\.[0-9]+-[A-Za-z0-9.-]+$')][string]$Version='v1.0.3-single',
 [Parameter(Mandatory)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$Sha256,
 [ValidatePattern('^[A-Fa-f0-9]{40}$')][string]$SignerThumbprint,
 [switch]$StartWithWindows
)
$ErrorActionPreference='Stop'
$temp=Join-Path ([IO.Path]::GetTempPath()) ('Service1-'+[guid]::NewGuid()+'.exe')
try {
 Invoke-WebRequest "https://github.com/$OfficialRepository/releases/download/$Version/Service1.exe" -OutFile $temp -UseBasicParsing
 if((Get-FileHash -LiteralPath $temp -Algorithm SHA256).Hash -ne $Sha256){throw 'SHA-256 mismatch. Nothing was installed or launched.'}
 $signature=Get-AuthenticodeSignature -LiteralPath $temp
 if($signature.Status -notin @('Valid','NotSigned')){throw 'Invalid executable signature.'}
 if($SignerThumbprint -and ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Thumbprint -ne $SignerThumbprint)){throw 'Expected publisher signature missing.'}
 if($signature.Status -eq 'NotSigned'){Write-Warning 'Unsigned preview. Windows may warn or block it. Follow your security policy.'}
 $folder=Join-Path $env:LOCALAPPDATA "Programs\Service1\$Version"
 $exe=Join-Path $folder 'Service1.exe'
 if(Test-Path -LiteralPath $exe){throw 'This version is already installed. Exit Service1 and manage the existing installation first.'}
 New-Item -ItemType Directory -Path $folder -Force | Out-Null
 Move-Item -LiteralPath $temp -Destination $exe
 New-Item -ItemType Directory -Path (Join-Path $env:LOCALAPPDATA 'Service1\Config') -Force | Out-Null
 if($StartWithWindows){
  $runKey='HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
  New-Item -Path $runKey -Force | Out-Null
  New-ItemProperty -Path $runKey -Name Service1 -Value ('"'+$exe+'"') -PropertyType String -Force | Out-Null
 }
 Start-Process -FilePath $exe -WindowStyle Hidden
 Write-Host "Service1 installed and launched. Open its tray icon. Location: $folder"
} catch {Write-Error ('Service1 installation failed: '+$_.Exception.Message)}
finally {if(Test-Path -LiteralPath $temp){Remove-Item -LiteralPath $temp -Force}}
