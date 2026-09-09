# Passkey Example (C# + Vue)

A local-first passkey demo:

- **Backend** – Azure Functions (isolated worker, .NET 10) exposing a REST API under `/api/auth/*`.
  Email/password **and** passkey authentication built on ASP.NET Core Identity (with the passkey
  support baked into `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.x) and SQLite.
- **Frontend** – Vue 3 + Vite app. The Vite dev server proxies `/api` to the Functions host, so the
  browser stays on one origin (`http://localhost:5173`) which keeps cookie auth and WebAuthn origin
  validation simple.

## Requirements

- [.NET SDK 10](https://dotnet.microsoft.com/download) (`dotnet --version` -> `10.0.x`)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
  (`func --version`). If `func` is not on `PATH`, use the full path, e.g.
  `C:\Program Files\Microsoft\Azure Functions Core Tools\func.exe`
- [Node.js 18+](https://nodejs.org) (`node --version`)

No external services are needed. SQLite stores everything in a local `passkeys.db` file.

## Run the backend

```powershell
cd backend
func start
```

The host:

- builds/copies the worker output to `backend/bin/output/`,
- applies EF Core migrations automatically on startup (creates `backend/bin/output/passkeys.db`),
- listens on `http://localhost:7071`.

Verify it is up by opening `http://localhost:7071/api/auth/me` – it returns `401` (not an error page)
when you are not signed in.

> The app registers itself with the Worker SDK equivalent of routes with the default `api` prefix,
> so a function with route `auth/register` is served at `http://localhost:7071/api/auth/register`.

## Run the frontend

```powershell
cd frontend
npm install
npm run dev
```

Then open **http://localhost:5173** in Chrome or Edge.

- Register an account with email + password.
- Sign in with the password, then on the dashboard click **Add a passkey** to register WebAuthn
  credentials (the browser prompt may ask for your platform authenticator or a security key).
- Sign out, then sign back in with either the password or a passkey.

WebAuthn requires a secure context; `http://localhost` is treated as a secure context, so the demo
works without HTTPS.

## API endpoints

| Method | Path                                  | Auth | Description                               |
| ------ | ------------------------------------- | ---- | ----------------------------------------- |
| POST   | `/api/auth/register`                  |      | Create account `{email, password}`        |
| POST   | `/api/auth/login`                     |      | Sign in with password                     |
| POST   | `/api/auth/logout`                    | X    | Sign out (any signed-in user)             |
| GET    | `/api/auth/me`                        | X    | Current user + passkey count              |
| POST   | `/api/auth/passkey/create-options`    | X    | Start passkey registration (WebAuthn)     |
| POST   | `/api/auth/passkey/register`          | X    | Complete passkey registration             |
| POST   | `/api/auth/passkey/login-options`     |      | Start passkey sign-in (WebAuthn)          |
| POST   | `/api/auth/passkey/login`             |      | Complete passkey sign-in                  |
| GET    | `/api/auth/passkeys`                  | X    | List the signed-in user's passkeys        |
| DELETE | `/api/auth/passkeys/{credentialId}`   | X    | Remove a passkey (base64url credential id)|

All requests are same-origin through the Vite proxy (`/api` -> `http://localhost:7071`). Sessions
are cookie-based (`PasskeyExample.Auth`, HttpOnly). There is intentionally no CORS configuration –
the proxy keeps the browser on a single origin.

## Database

- Tables: `Users`, `Roles`, `UserRoles`, `UserClaims`, `UserLogins`, `UserTokens`, `RoleClaims`,
  `UserPasskeys` (the default `AspNet` prefix is stripped in `backend/Data/AppDbContext.cs`).
- Identity schema version 3.0 includes the passkey store; see `Program.cs` and the design-time
  factory `backend/Data/AppDbContextFactory.cs`.
- The DB is created/migrated automatically at startup. To start from scratch: stop `func`, delete
  `backend/bin/output/passkeys.db`, run `func start` again.

### Regenerating migrations after model changes

The design-time factory (`AppDbContextFactory`) lets you generate migrations without running the
Functions host:

```powershell
cd backend
dotnet ef migrations add <MigrationName>
```

If you change the stored `IdentitySchemaVersions` or just want a clean slate, you can delete
`backend/Migrations/*` and run `dotnet ef migrations add InitialCreate` again.

## Project layout

```
backend/
  Functions/AuthFunctions.cs        # HTTP trigger functions (/api/auth/*)
  Functions/HttpContextAccessorMiddleware.cs  # bridges IHttpContextAccessor for SignInManager
  Data/                              # ApplicationUser, AppDbContext, AppDbContextFactory
  Models/                            # request/response records
  Migrations/                        # EF Core migrations
  Program.cs                         # Functions host: DI, Identity, migration on startup
  host.json / local.settings.json    # Functions config (route prefix, connection string)
frontend/
  src/views/                         # Login, Register, Dashboard
  src/api.js                         # fetch helpers for the /api endpoints
  src/passkey.js                     # WebAuthn helpers (@simplewebauthn/browser)
  vite.config.js                     # dev server + proxy /api -> localhost:7071
```