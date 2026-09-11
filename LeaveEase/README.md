# LeaveEase — Employee Leave Management System

LeaveEase is a full-stack, enterprise-style employee leave management application built with ASP.NET Core, React, SQL Server, and MailKit.

## Highlights

- JWT authentication and role-based authorization (Employee, Manager, Admin)
- Employee leave application with balance and overlap validation
- Manager approval/rejection with comments
- Transaction-safe leave balance updates
- Responsive leave balance dashboard and request history
- Direct-report approval queue for managers
- Status history/audit trail for every leave request
- MailKit SMTP notifications when requests are submitted or reviewed
- Admin controls for leave types, employee list, and yearly balance allocation
- SQL Server persistence with EF Core and automatic demo-data seeding
- Modern responsive React UI with no UI framework dependency

## Technology

- Backend: .NET 9 (SDK 9.0.318) / ASP.NET Core Web API / EF Core 9 / ASP.NET Core Identity / JWT
- Frontend: React 19.3 / React Router / Vite 8
- Database: Microsoft SQL Server
- Email: MailKit 4.17

## Demo accounts

All seeded users use the password: `Pass@123`

| Role | Email |
|---|---|
| Admin | admin@leaveease.local |
| Manager | manager@leaveease.local |
| Employee | employee@leaveease.local |
| Employee | priya@leaveease.local |

> Change or remove demo credentials before production use.

## Quick start (Windows + SQL Server LocalDB)

### 1. Start the API

Requirements: .NET SDK 9.0.318 and SQL Server LocalDB.

Verify the required SDK before starting:

```bash
dotnet --version
# Expected: 9.0.318
```

```powershell
cd backend/LeaveEase.Api
dotnet restore
dotnet run
```

The API runs at `http://localhost:5080` by default. On first start it creates `LeaveEaseDb` and seeds demo data.

### 2. Start the React app

Requirements: Node.js 22+.

```powershell
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173` and sign in with a demo account.

## SQL Server / Docker option

If you do not use LocalDB, override the API connection string with an environment variable:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=LeaveEaseDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
dotnet run
```

A `docker-compose.yml` is included for SQL Server. Run:

```bash
docker compose up -d db
```

Then use the connection string shown in the compose file.

## SMTP configuration

Email is optional in local development. If SMTP is disabled, all other leave workflows still work and the API logs the skipped email.

Configure via `appsettings.json`, environment variables, or user secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Smtp:Enabled" "true"
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:Username" "your-email@gmail.com"
dotnet user-secrets set "Smtp:Password" "your-app-password"
dotnet user-secrets set "Smtp:FromEmail" "your-email@gmail.com"
```

For Gmail, use an App Password rather than your normal account password.

## Main API routes

- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/dashboard/summary`
- `GET /api/leave-types`
- `GET /api/leave-balances/me`
- `POST /api/leave-requests`
- `GET /api/leave-requests/mine`
- `GET /api/leave-requests/pending`
- `PUT /api/leave-requests/{id}/decision`
- `PUT /api/leave-requests/{id}/cancel`
- `GET /api/admin/users`
- `POST /api/admin/leave-types`
- `PUT /api/admin/leave-types/{id}`
- `POST /api/admin/allocate-balance`

## Important production hardening

This repository is configured for easy local demonstration. Before production deployment:

1. Store JWT and SMTP secrets in a secret manager, never source control.
2. Replace demo accounts/passwords.
3. Use HTTPS only and restrict CORS to your deployed frontend.
4. Replace automatic `EnsureCreated` database creation with reviewed EF Core migrations.
5. Add refresh-token rotation or an external identity provider if long-lived sessions are required.
6. Add organization-specific holiday calendars and leave policies.
7. Add centralized logging/telemetry and SMTP retry/outbox processing for guaranteed mail delivery.

## Project structure

```text
LeaveEase/
├─ backend/LeaveEase.Api/
│  ├─ Controllers/
│  ├─ Data/
│  ├─ DTOs/
│  ├─ Models/
│  ├─ Services/
│  └─ Program.cs
├─ frontend/
│  └─ src/
│     ├─ api/
│     ├─ auth/
│     ├─ components/
│     └─ pages/
├─ docs/
└─ docker-compose.yml
```
