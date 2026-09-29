# Distribution and signing

Official source: https://github.com/Nagriv1/Setting1. This is an unsigned preview; no signing certificate has been supplied. Versioned packages and checksums are published under downloads/ in the official repository.

1. Create a publisher-controlled repository, enable account MFA, protect release credentials, and publish the reviewed source and documentation. Confirm the publisher/repository spelling independently.
2. Build and test using the pinned SDK. Use a supported patched .NET Desktop Runtime on deployment systems. Review SDK/runtime advisories before each release.
3. Obtain an Authenticode code-signing certificate from a publicly trusted certificate authority after their identity verification, or an enterprise CA trusted by your organization's managed devices. Use the CA's required hardware-backed token/HSM or supported managed signing service. A self-signed certificate does not establish public trust. Never commit private keys, PFX passwords or signing-service tokens.
4. Install the Windows SDK SignTool and your provider's signing integration. Example for a certificate in the Windows certificate store (substitute the actual thumbprint and CA-provided RFC3161 timestamp URL):

```powershell
signtool sign /sha1 CERTIFICATE_THUMBPRINT /fd SHA256 /tr TIMESTAMP_URL /td SHA256 Service1.exe
signtool sign /sha1 CERTIFICATE_THUMBPRINT /fd SHA256 /tr TIMESTAMP_URL /td SHA256 Service1.dll
signtool verify /pa /all /v Service1.exe
signtool verify /pa /all /v Service1.dll
```

`/sha1` selects a certificate by thumbprint; `/fd SHA256` is the file signature digest. Use your provider's authenticated signing service command if the key is held remotely.

5. Sign the PowerShell installer itself using the code-signing certificate, and verify it. No custom EXE/MSI installer is included; the ZIP and this script are the supported methods.

```powershell
$certificate = Get-Item Cert:\CurrentUser\My\CERTIFICATE_THUMBPRINT
Set-AuthenticodeSignature -FilePath .\distribution\Install-Service1.ps1 -Certificate $certificate -HashAlgorithm SHA256 -TimestampServer TIMESTAMP_URL
Get-AuthenticodeSignature .\distribution\Install-Service1.ps1
```

If an EXE/MSI installer is added later, sign its completed bytes with the same `signtool sign` command and verify with `signtool verify /pa /all /v`. Sign inner binaries before signing the outer installer. Publish the expected certificate thumbprint separately and update it transparently during certificate rotation.

6. Publish a self-contained Windows x64 single file with the pinned runtime, then sign the finished EXE before computing its digest:

```powershell
dotnet publish src/Service1.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:RuntimeFrameworkVersion=10.0.12 -p:DebugType=None --source https://api.nuget.org/v3/index.json -o release
Get-FileHash .\release\Service1.exe -Algorithm SHA256
```

The workflow publishes an unsigned preview when no certificate is configured. Publish signing only through a protected signing system; never place certificate private keys in this repository.

7. Publish Service1.exe, the installer script, SHA256SUMS.txt and signer identity on the official GitHub release. Replace README example values with that real repository, tag and hash. Protect the publication channel; an attacker who replaces both executable and published hash defeats hash-only authenticity checks.
8. Test both installation methods in a clean Windows account, including wrong hash, tampered archive, wrong signer, missing runtime, startup on/off and restricted organizational policies. Then promote the release.

Consumers should compare `Get-FileHash` with a trusted digest, and use `Get-AuthenticodeSignature` or SignTool to validate downloaded binaries. Do not use Unblock-File, execution-policy bypass, Defender exclusions or SmartScreen overrides as an installation step. Unsigned downloadsâ€”and even newly signed software without established reputationâ€”may trigger Windows security warnings. No warning-free execution guarantee is possible.

Authoritative reference: [Microsoft SignTool documentation](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool).
