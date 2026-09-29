# Validation

- Gemini-only release build and 32 assertions passed: GitHub key loading/cache, model discovery, authentication fallback, no retry on 429, clipboard failure preservation and concurrent-copy protection.
- Both owner-supplied raw key URLs were reachable and nonempty. No key values were printed. Free Gemini model-list authentication: key1 returned HTTP 401; key2 succeeded with 61 models. No paid generation request was performed. Response generation remains unverified.
- Installer syntax checked. Runtime installer tests are blocked by the local execution policy, which was not changed or bypassed.
- Interactive tray/UI, real provider, Windows logon startup and clean-machine install remain manual checks.
- Package is unsigned; self-contained single-file release bundles .NET 10.0.12.

Hotkey update: configuration/migration tests, live Windows registration-conflict preservation, recording capture and Esc cancellation tested without real key injection.
