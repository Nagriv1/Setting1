# Service1 ? Gemini-only preview

Quiet Windows tray utility: **Ctrl+C ? Ctrl+Shift+F8 ? wait for Gemini ? Ctrl+V**.

[Download the Windows package](https://github.com/Nagriv1/Setting1/releases/tag/v1.0.1-single) ? [Security design](SECURITY.md)

## Install normally

Download **Service1.exe** from [the official release](https://github.com/Nagriv1/Setting1/releases/tag/v1.0.1-single), verify SHA-256 against SHA256SUMS.txt, and run it. This is a self-contained Windows x64 EXE: no separate .NET installation, local model or backend is required. For installation use `%LOCALAPPDATA%\Programs\Service1\v1.0.1-single`. It stays in the tray; open Settings there. Unsigned preview software may trigger Windows warnings or blocks. Do not bypass security policy.

## Default keys

Service1 fetches `key1` and `key2` from this repository's raw `main` files over HTTPS, only when you explicitly process clipboard text. It does not require pasted API keys, Ollama, a local model or a separate backend. Update those two files on GitHub to change the defaults. Keys are cached in memory for up to 10 minutes; Save in Settings clears the cache for the next action. They are never embedded in the executable, written to settings, or logged by Service1.

**These key files are public. Anyone can retrieve the keys and consume their quota. Public exposure may cause Google to revoke/block them. This distribution method is not secure secret storage.** A reachable file does not mean its key is valid for Gemini.

The first key is tried first; authentication rejection permits one attempt with the second. Quota/rate-limit errors do not trigger key rotation or repeated requests. Model `auto` asks Gemini for supported models and prefers a stable Flash Lite text model; you can select a specific Gemini model in Settings. Successful output is capped at 4,096 model output tokens. Incomplete/blocked responses preserve your original clipboard.

## Controls and privacy

Tray menu: Pause, Settings, Open Mini Prompt, Start with Windows, Exit. Default hotkey improves clarity while preserving meaning; Mini Prompt accepts a one-time instruction. Background requests never show processing windows, notifications or sounds, or take focus. Errors appear when you open Settings.

This final version is **cloud-only**, as requested. It has no LOCAL ONLY setting. Explicit processing sends the clipboard text and instruction to Google. No clipboard monitoring, history, analytics, crash uploads or automatic updates. Failed requests leave the clipboard untouched; copying new content during processing preserves the newer clipboard.

Startup is optional and uses the named current-user `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Service1` entry. Disable it in Settings or the tray. To remove, disable startup, exit, and delete the install folder. Optional config is `%LOCALAPPDATA%\Service1\Config\gemini-settings.json`.

## One-command PowerShell installation

Paste this entire command into ordinary PowerShell (no administrator required). It downloads, checks the pinned SHA-256 and signature status, installs per user, and launches Service1 in the tray. It does not change execution policy or security settings. This release is unsigned.

```powershell
& {
  $ErrorActionPreference='Stop'
  $url='https://github.com/Nagriv1/Setting1/releases/download/v1.0.1-single/Service1.exe'
  $hash='E50BF42E814D6D15246739A16F3CD4F747A642C305FCC0EB6F0C547746346FA8'
  $dir=Join-Path $env:LOCALAPPDATA 'Programs\Service1\v1.0.1-single'
  $tmp=Join-Path $env:TEMP ('Service1-'+[guid]::NewGuid()+'.exe')
  try {
    Invoke-WebRequest $url -OutFile $tmp -UseBasicParsing
    if((Get-FileHash $tmp -Algorithm SHA256).Hash -ne $hash){throw 'Download verification failed'}
    if((Get-AuthenticodeSignature $tmp).Status -notin @('Valid','NotSigned')){throw 'Invalid signature'}
    New-Item -ItemType Directory -Force -Path $dir,(Join-Path $env:LOCALAPPDATA 'Service1\Config') | Out-Null
    $exe=Join-Path $dir 'Service1.exe'
    if(Test-Path -LiteralPath $exe){
      if((Get-FileHash $exe -Algorithm SHA256).Hash -ne $hash){throw 'A different installation exists; exit and review it first'}
    }else{Move-Item -LiteralPath $tmp -Destination $exe}
    Start-Process -FilePath $exe -WindowStyle Hidden
    Write-Host 'Service1 launched. Look for its tray icon. Hotkey: Ctrl+Shift+F8.'
  }finally{if(Test-Path -LiteralPath $tmp){Remove-Item -LiteralPath $tmp -Force}}
}
```

Optional Start with Windows is available in the tray. The separately downloadable Install-Service1.ps1 offers the same installation with optional startup, where your existing execution policy allows scripts.

## Build

SDK 10.0.201 is pinned. Framework-only desktop app, no third-party NuGet dependencies.

```powershell
dotnet restore tests/Tests.csproj --configfile NuGet.Config
dotnet run --project tests/Tests.csproj -c Release --no-restore
dotnet build src/Service1.csproj -c Release --no-restore
```

See RELEASE.md for Authenticode signing and VALIDATION.md for test scope.
