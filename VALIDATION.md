# Validation

- Gemini-only release build and 17 assertions passed: GitHub key loading/cache, model discovery, authentication fallback, no retry on 429, clipboard failure preservation and concurrent-copy protection.
- Both owner-supplied raw key URLs were reachable and nonempty. No key values were printed. No paid Gemini request was performed, so actual model/key validity is unverified.
- Installer syntax checked. Runtime installer tests are blocked by the local execution policy, which was not changed or bypassed.
- Interactive tray/UI, real provider, Windows logon startup and clean-machine install remain manual checks.
- Package is unsigned; self-contained single-file release bundles .NET 10.0.12.
