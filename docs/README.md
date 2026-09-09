# Passkey Workflow Documentation

This directory documents how passkeys work in this application, end to end.

| Document | Covers |
| --- | --- |
| [passkey-registration.md](passkey-registration.md) | Registering (adding) a passkey to an account with password/cookie auth |
| [passkey-authentication.md](passkey-authentication.md) | Signing in (authenticating) with an existing passkey |
| [architecture.md](architecture.md) | How the Azure Functions host + ASP.NET Core Identity integration is wired together, and the cookie/HTTP-context mechanics behind the scenes |

## At a glance

```
Browser (localhost:5173)
   │  WebAuthn ceremonies run here (navigator.credentials.create / .get)
   ▼
Vue 3 + Vite dev server ── /api/* proxied ──► Azure Functions host (localhost:7071)
                                                AuthFunctions (HttpTrigger endpoints)
                                                      │
                                                      ▼
                                   ASP.NET Core Identity (SignInManager + PasskeyHandler)
                                                      │
                                                      ▼
                                                 SQLite (Users, UserPasskeys, ...)
```

- The **Vite proxy** keeps the browser on a single origin (`http://localhost:5173`), so the
  `Origin` header, WebAuthn `clientData.origin`, and the `PasskeyExample.Auth` cookie all agree.
- The server's relying-party ID is `localhost` (set via `IdentityPasskeyOptions.ServerDomain` in
  `Program.cs`) and the origin used for validation is the request `Origin` header.
- `http://localhost` requires no HTTPS in the browser (it is a "secure context"),
  so the whole demo works locally.

## Read this first

1. [passkey-registration.md](passkey-registration.md) — the add-a-passkey ceremony.
2. [passkey-authentication.md](passkey-authentication.md) — the sign-in-with-a-passkey ceremony.
3. [architecture.md](architecture.md) — the plumbing: worker middleware, cookie schemes, key
   Identity internals (`Make…Async` / `Perform…Async` / `PasskeySignInAsync`), and HTTP context
   bridging in the isolated worker model.