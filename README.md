# V2boardApi

API and admin panel integrated with [V2board](https://github.com/v2board/v2board) for selling V2Ray VPN subscriptions. Supports multi-tier resellers, Telegram bot commerce, wallet billing, and subscription delivery to VPN clients.

> **Note:** This repository contains the ASP.NET backend and Razor admin UI. A separate React panel may exist outside this repo (referenced in project history but not included here).

## Features

- **Reseller hierarchy** — Admin, senior agents, and agents with role-based access
- **V2board integration** — Direct MySQL access to V2board panel databases per server
- **Subscription management** — Create, renew, reset, ban, and delete V2board users
- **Telegram bot sales** — Automated plan purchase, wallet top-up, and support via webhooks
- **Payment gateways** — ZarinPal, TetraPay, Plisio (crypto), HubSmart, card-to-card SMS
- **Subscription proxy** — Serves VPN client configs at `/api/v1/client/subscribe`
- **Wallet billing** — Reseller and Telegram user wallets with automatic debiting
- **Auto-renew & alerts** — Background timers for renewal, expiry warnings, and test account cleanup
- **Sales dashboards** — Weekly/monthly reports, agent performance, traffic analytics
- **Invoices & payment links** — Agent factor management and TetraPay link generation
- **Notifications** — Broadcast notifications to resellers
- **Persian RTL UI** — Admin panel with Shamsi calendar support
- **Mobile app API** — Login and subscription info endpoints
- **Audit logging** — NLog database logging of controller actions

## Architecture

```
┌──────────────────┐     ┌──────────────────┐
│  Razor Admin UI  │     │  Web API / Bots  │
│  (Areas/App)     │     │  (Areas/api)     │
└────────┬─────────┘     └────────┬─────────┘
         │                        │
         └──────────┬─────────────┘
                    ▼
         ┌─────────────────────┐
         │  Tools & Services   │
         │  Auth, Bots, Timers │
         └──────────┬──────────┘
                    ▼
    ┌───────────────┴───────────────┐
    ▼                               ▼
 SQL Server (EF6)            MySQL (V2board)
 Resellers, Orders,           v2_user, v2_plan
 Bots, Payments               per tbServers
```

The application uses a **dual-database** pattern: SQL Server stores business logic and reseller data; each V2board server entry (`tbServers`) points to a remote MySQL database holding actual VPN subscriptions.

## Technology Stack

- **.NET Framework 4.8**
- **ASP.NET MVC 5.2.9** + **ASP.NET Web API 5.2.9**
- **Entity Framework 6.4.4** (SQL Server, database-first)
- **MySqlConnector 2.5.0** (V2board panel queries)
- **Telegram.Bot 22.9.0**
- **NLog 5.3.2** (database logging)
- **JWT** (System.IdentityModel.Tokens.Jwt 8.2.0)
- **Bootstrap 5.2.3**, jQuery 3.4.1, Vuexy admin template
- **Stimulsoft** (reporting)
- **IIS** hosting

## Prerequisites

- Windows Server or Windows 10/11 with IIS
- **.NET Framework 4.8** runtime
- **SQL Server** (local or remote)
- **Visual Studio 2022** (for development)
- One or more **V2board panels** with accessible MySQL databases
- **Telegram Bot Token(s)** (via [@BotFather](https://t.me/BotFather))
- Payment gateway credentials (as needed)

## Installation

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd V2boardApi
   ```

2. Restore NuGet packages:
   ```bash
   nuget restore V2boardApi.sln
   ```
   Or open `V2boardApi.sln` in Visual Studio and restore packages.

3. Build the solution:
   ```bash
   msbuild V2boardApi.sln /p:Configuration=Release
   ```

4. Publish `V2boardApi` project to an IIS site directory.

## Configuration

Edit `V2boardApi/Web.config`:

### App Settings

| Key | Description |
|-----|-------------|
| `JwtSecretKey` | HMAC secret for JWT tokens (**change before deployment**) |
| `GeminiApiKey` | Google Gemini API key (optional; currently unused) |

### Connection Strings

| Name | Description |
|------|-------------|
| `Entities` | SQL Server connection for EF6 (`metadata=...;provider connection string=...`) |

### Authentication

Forms authentication is configured with:
- Login URL: `App/Admin/Login`
- Default URL: `App/Subscriptions/Index`
- Timeout: 3600 minutes

### Production Checklist

- [ ] Replace all hardcoded secrets in `Web.config`
- [ ] Set `compilation debug="false"`
- [ ] Enable HTTPS and set JWT cookie `Secure=true`
- [ ] Restrict CORS origins (remove `*`)
- [ ] Configure NLog (`NLog.config`) for your database

## Database Setup

1. Create a SQL Server database (e.g., `V2boardSiteSharifi`).
2. Import or generate the schema from `DataLayer/DomainModel/Model.edmx` (database-first — schema must match the EDMX model).
3. Ensure stored procedures exist: `GetBotSales`, `GetMasterUserSales`, `GetUserSales`, `NLog_AddEntry_p`.
4. Configure at least one `tbServers` record with V2board MySQL credentials via the Settings panel or direct SQL insert.
5. Create an admin user in `tbUsers` with `Role = 1`.

**Note:** Full SQL schema scripts are not included in this repository.

## Running the Application

### Development

1. Open `V2boardApi.sln` in Visual Studio 2022.
2. Set `V2boardApi` as startup project.
3. Update `Web.config` connection string.
4. Press F5 (IIS Express on port 44363 per csproj).

### Production

1. Create an IIS Application Pool targeting **.NET CLR v4.0**.
2. Deploy published files to the site root.
3. Ensure the app pool identity can reach SQL Server and remote MySQL (V2board panels).
4. Configure HTTPS binding.
5. Set Telegram bot webhook URL to `https://your-domain/Bot/Update/?botName={username}`.

On startup, the application:
- Registers all MVC areas and routes
- Loads Telegram bots from users with `tbBotSettings.Bot_Token`
- Caches server configuration in `HttpRuntime.Cache`
- Starts `TimerService` background jobs

## API Documentation

### Web API Endpoints

Base route: `/{controller}/{action}`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/User/LoginAdmin` | None | Mobile admin login |
| GET | `/User/CheckOrder` | None | Card-to-card payment check |
| GET | `/User/VerifyPayZarinPal` | None | ZarinPal callback |
| GET | `/User/VerifyPay` | None | HubSmart payment callback |
| GET | `/User/GetFactors` | Forms `[Authorize]` | Pending invoices |
| POST | `/User/VerifyTetraPay` | None | TetraPay webhook |
| POST | `/User/VerifyTetraPayLink` | None | TetraPay link webhook |
| POST | `/User/VerifyPlisio` | None | Plisio crypto webhook |
| POST | `/Bot/Update?botName=` | Anonymous | Telegram webhook |
| GET | `/MobileApp/GetSubscriptionInfo` | Token header | Subscription usage |

### Subscription Client Endpoints

Base route: `api/v1/client/{action}`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/client/subscribe?token=` | VPN subscription config |
| GET | `/api/v1/client/android?token=` | Android setup page |
| GET | `/api/v1/client/ios?token=` | iOS setup page |
| GET | `/api/v1/client/windows?token=` | Windows setup page |
| GET | `/api/v1/client/linux?token=` | Linux setup page |

### Panel Routes

All panel routes: `App/{controller}/{action}` — requires login + JWT cookies + role authorization.

## Project Structure

```
V2boardApi/
├── V2boardApi.sln
├── DataLayer/
│   ├── DomainModel/          # EF6 entities (Model.edmx, tb*.cs)
│   ├── Repository/           # Repository<T>, ViewRepository
│   └── Interface/            # IRepository<T>
├── V2boardApi/
│   ├── Areas/
│   │   ├── App/              # MVC admin panel
│   │   │   ├── Controllers/
│   │   │   ├── Views/
│   │   │   └── Data/         # ViewModels
│   │   └── api/              # Web API + client views
│   │       ├── Controllers/
│   │       └── Views/
│   ├── App_Start/            # Route, bundle, filter config
│   ├── Models/               # DTOs and view models
│   ├── PaymentMethods/       # TetraPay integration
│   ├── Reports/              # Stimulsoft reports
│   ├── Tools/                # Auth, bots, timers, payments
│   ├── assets/               # Static frontend (Vuexy theme)
│   ├── Web.config
│   └── Global.asax.cs
└── packages/                 # NuGet packages
```

## Security Notes

> **Warning:** The repository currently contains hardcoded secrets in `Web.config`. Rotate all keys before any deployment.

- Change `JwtSecretKey`, database passwords, and API keys before production use
- Payment callback endpoints are unauthenticated — implement gateway signature verification
- Passwords are stored as unsalted SHA256 — plan migration to a proper password hasher
- MySQL queries in several controllers use string concatenation — SQL injection risk
- Set `compilation debug="false"` in production
- Enable HTTPS and set secure cookie flags
- Restrict CORS to known origins
- Review `AdminController.GetUserAccountLog` — authorization attribute is commented out

## Deployment

1. Build in **Release** configuration.
2. Publish to IIS on Windows Server with .NET 4.8 installed.
3. Configure SQL Server connection string.
4. Ensure outbound access to:
   - V2board MySQL servers
   - Telegram API (`api.telegram.org`)
   - Payment gateway APIs (ZarinPal, TetraPay, Plisio, HubSmart)
5. Configure SSL certificate for HTTPS.
6. Register Telegram webhooks pointing to your domain.

## Troubleshooting

| Issue | Possible Cause | Solution |
|-------|----------------|----------|
| Login fails | Wrong connection string or user not in `tbUsers` | Verify SQL Server connectivity and credentials |
| Bots not responding | Webhook not set or bot token invalid | Check `BotManager` cache; re-run StartBot from admin panel |
| Subscriptions empty | MySQL credentials wrong in `tbServers` | Use Settings → ScanPort to test connectivity |
| Payment not credited | Callback URL unreachable or no verification | Check IIS logs; verify gateway callback reaches server |
| 401 on panel pages | Missing or expired JWT/Role cookies | Re-login at `App/Admin/Login` |
| Timer jobs not running | App pool recycled or `Server` cache empty | Ensure app stays warm; verify `tbServers` config |

## Future Improvements

- Migrate secrets to secure configuration management
- Harden and verify all payment webhooks
- Parameterize all raw SQL queries
- Replace SHA256 with bcrypt/Argon2 password hashing
- Add Swagger/OpenAPI documentation
- Introduce dependency injection
- Split monolithic controllers (especially `BotController`)
- Add unit and integration tests
- Add CI/CD pipeline
- Consider migration to ASP.NET Core
- Include SQL schema migration scripts in repository
- Document role permissions matrix
- Remove dead code (GeminiAPI, NowPayments, ViewRepository)
- Fix `V2boardApiTools` initialization for online user features
- Add Redis for distributed caching (package already referenced)
