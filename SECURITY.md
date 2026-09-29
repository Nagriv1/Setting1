# Service1 security and privacy

## Final scope

The owner explicitly replaced the original local-only design with a Gemini-only application using public GitHub key files. There is no Ollama or private backend integration in this release. The earlier private backend was not deployed.

## Network

Only an explicit hotkey/Mini Prompt request performs network activity. HTTPS GET goes to the fixed raw.githubusercontent.com/Nagriv1/Setting1/refs/heads/main/key1 and key2 URLs to load credentials. No clipboard data is sent to GitHub. With model `auto`, HTTPS GET to generativelanguage.googleapis.com/v1beta/models selects a supported Flash text model. HTTPS POST to that host's generateContent endpoint sends clipboard text and instruction. Keys are in the x-goog-api-key header, never query strings. The app disables redirects, proxies and cookies. It has no listening socket, analytics, telemetry, crash uploader or automatic updater.

## Public credential risk

The two default credential files are publicly accessible by explicit owner request. Anyone can use their contents. HTTPS protects transport but cannot make a public repository secret. Service1 cannot guarantee key validity, secrecy or available quota; Google may reject exposed keys. Keys should be restricted and revoked/rotated by the owner as appropriate. No key values are embedded in source/binaries or copied into documentation. The app retains fetched keys only in process memory for at most ten minutes between refreshes; it never writes them to files or logs. Managed strings cannot be reliably zeroed. This design deliberately does not claim Windows Credential Manager storage for these public defaults.

## Processing and data

Only explicit actions read Unicode text. One request runs at a time; input is limited to 100,000 characters, key downloads to 4 KB, responses to 2 MB, output to 4,096 tokens, and HTTP requests to 90 seconds. Keys are tried sequentially only on authentication errors; 429 quota errors stop without retry. Response content is never executed. Model names are validated and URL-escaped. JSON is serialized normally; no prompt or model text becomes shell code.

The original clipboard remains intact until a successful complete response; a sequence check prevents stale output replacing newer clipboard contents. Clipboard APIs may fail under contention. Successful output remains on the clipboard for pasting. Temporary prompt/response strings become eligible for collection after processing; Windows clipboard history/sync, paging, dumps and Google retention are outside this app's controls.

No history or diagnostic log files are maintained. Settings persist only model/hotkey. User-visible errors are fixed technical messages: no raw exception messages, HTTP bodies, key values, prompts or responses. Installer temporary files contain packages only.

## Windows transparency

Normal tray icon, Service1 process name, no taskbar/console windows during processing, no hidden services/tasks. Optional startup uses the documented current-user Run entry and is easily disabled. No security bypass, exclusions, obfuscation or impersonation. The process opens no LAN services.

## Distribution

The self-contained single EXE bundles .NET 10.0.12. Native runtime files may be extracted by .NET to its normal temporary cache; clipboard contents never go there. SDK 10.0.201 and release actions are pinned. No third-party desktop NuGet packages are used. Future runtime security fixes require rebuilding this self-contained app.

SHA-256 verification precedes install/launch. Any present invalid Authenticode signature fails installation; a supplied signer thumbprint requires a valid matching signature. Unsigned releases warn. A hash from a compromised publisher is not an independent authenticity proof. No automatic updater exists. See RELEASE.md for certificate/signing practices.

Tests use fake HTTP and clipboard implementations; no real user's clipboard or paid Gemini requests are used. Remaining manual checks: tray/focus behavior, Windows clipboard contention, actual key/model validity, login startup and clean-machine installation. No production certification or guaranteed warning-free execution is claimed.


## Version 1.0.3 update

Service1 1.0.3 unsigned preview

- Four ordered key sources: key1, key2, key3, key4. Each action starts with key1; duplicate values are removed. Authentication failures and HTTP 5xx try the next key, with a bounded delay for server errors. Network ambiguity, invalid requests, and quota errors stop rather than risking repeated charges.
- At most five generation attempts per rolling minute, including failed attempts. HTTP 429 pauses all keys for at least one minute and honors a longer Retry-After header. Daily quota exhaustion requires waiting for Google quota reset. A full action has a 90-second deadline.
- Auto model prefers available gemini-3.5-flash-lite, then gemini-3.1-flash-lite, then available text Flash models. Availability does not guarantee free quota. Explicit models remain configurable.
- Default coding instructions request simple readable code without Markdown fences or extra explanation. Mini Prompt can override the style.
- Ctrl+Alt+J default; Settings can record a custom shortcut. Tray Process clipboard remains available if a shortcut conflicts.

Google quotas are per project, not per key. More keys in one project do not increase capacity. 2-5 requests/minute and 10/day cannot be guaranteed by the app; check the account's model quota, input/output token limits and billing settings. No paid generation was used for validation.
Official references: https://ai.google.dev/gemini-api/docs/rate-limits and https://ai.google.dev/gemini-api/docs/models/gemini-3.5-flash-lite

Public repository keys are exposed and may be abused or blocked; this release cannot make them secret. Unsigned software may trigger Windows warnings. No security controls or execution policies are bypassed.
