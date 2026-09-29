Service1 1.0.3 unsigned preview

- Four ordered key sources: key1, key2, key3, key4. Each action starts with key1; duplicate values are removed. Authentication failures and HTTP 5xx try the next key, with a bounded delay for server errors. Network ambiguity, invalid requests, and quota errors stop rather than risking repeated charges.
- At most five generation attempts per rolling minute, including failed attempts. HTTP 429 pauses all keys for at least one minute and honors a longer Retry-After header. Daily quota exhaustion requires waiting for Google quota reset. A full action has a 90-second deadline.
- Auto model prefers available gemini-3.5-flash-lite, then gemini-3.1-flash-lite, then available text Flash models. Availability does not guarantee free quota. Explicit models remain configurable.
- Default coding instructions request simple readable code without Markdown fences or extra explanation. Mini Prompt can override the style.
- Ctrl+Alt+J default; Settings can record a custom shortcut. Tray Process clipboard remains available if a shortcut conflicts.

Google quotas are per project, not per key. More keys in one project do not increase capacity. 2-5 requests/minute and 10/day cannot be guaranteed by the app; check the account's model quota, input/output token limits and billing settings. No paid generation was used for validation.
Official references: https://ai.google.dev/gemini-api/docs/rate-limits and https://ai.google.dev/gemini-api/docs/models/gemini-3.5-flash-lite

Public repository keys are exposed and may be abused or blocked; this release cannot make them secret. Unsigned software may trigger Windows warnings. No security controls or execution policies are bypassed.
