# ERP

Multi-company ERP built on **.NET 10 / ASP.NET Core**, **EF Core + Dapper** over **SQL Server**, with a static web portal
served by the API itself. Modules: General Ledger & Banking, Inventory, Procure-to-Pay (AP), Order-to-Cash (AR),
HR & Payroll, Fixed Assets, Real Estate, Manufacturing, Data Migration / Cutover, PDF reporting (QuestPDF).

## Repository layout

```
ERP/
├─ src/
│  ├─ ERP.Common.Domain/            Entities, value types, Result, tenancy/audit/soft-delete markers. No dependencies.
│  ├─ ERP.Common.Application/       Use-case contracts: Abstractions/<Module>/I*Service, Models/<Module> DTOs,
│  │                                Security (permissions, JwtSettings). Depends on Domain only.
│  ├─ ERP.Common.Infrastructure/    Implementations: Data (DbContext + EF configurations per module),
│  │                                Services/<Module> (mirrors Abstractions), Reporting (QuestPDF documents),
│  │                                DependencyInjection.cs (composition root for this layer).
│  └─ ERP.Web.Api/                  HTTP adapter: thin Controllers, Program.cs, Health checks, wwwroot (web portal).
├─ tests/ERP.UnitTests/             xUnit tests (EF in-memory + pure unit tests).
├─ database/
│  ├─ migrations/                   Ordered SQL migrations (001…012).
│  ├─ scripts/                      Operational scripts (deploy_migrations.ps1, create_app_user.sql, backup_restore.sql).
│  └─ legacy/                       Legacy schema inventory used for data-migration analysis.
├─ scripts/e2e/                     PowerShell end-to-end checks against a running API.
├─ deploy/nginx/                    Reverse-proxy configuration and dev-certificate helper.
├─ docs/                            Working notes (git workflow).
├─ Dockerfile, docker-compose.yml, .env.example, .github/workflows/ci-cd.yml
└─ Directory.Build.props            Shared TargetFramework / Nullable / NuGet-audit settings.
```

Dependency direction: `Web.Api → Infrastructure → Application → Domain` (Web.Api also references Application for contracts).
Controllers contain no business rules or data access; they call `I*Service` abstractions.

> The entire solution, projects, assemblies, namespaces, and types are unified under the **ERP** identity.

## Configuration and secrets

No secrets are committed. The API refuses to start without a JWT signing key.

| Setting | Development | Deployment |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | `dotnet user-secrets set ...` | env `ConnectionStrings__DefaultConnection` |
| `Jwt:SecretKey` (≥ 32 chars) | `dotnet user-secrets set ...` | env `Jwt__SecretKey` / `JWT_SECRET_KEY` in `.env` |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | `appsettings*.json` | same |
| `Cors:AllowedOrigins` | any origin allowed | explicit list (empty = same-origin only) |

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=ERP;User Id=...;Password=...;TrustServerCertificate=True;" --project src/ERP.Web.Api
dotnet user-secrets set "Jwt:SecretKey" "<random string, at least 32 characters>" --project src/ERP.Web.Api
```

## Build, test, run

```powershell
dotnet build ERP.slnx
dotnet test  ERP.slnx
dotnet run --project src/ERP.Web.Api          # portal at http://localhost:5174, API reference at /scalar/v1 (Development)
```

Database: apply `database/migrations/*.sql` in order, e.g.
`$env:ERP_DB_PASSWORD='...'; ./database/scripts/deploy_migrations.ps1 -Server <host> -Database ERP`.

End-to-end checks (need a running API and a user password):
`$env:ERP_E2E_PASSWORD='...'; ./scripts/e2e/payroll.e2e.ps1`.
`cutover.e2e.ps1` performs **write** operations (ETL run, cutover sign-off); do not run it against production data.

Container deployment: copy `.env.example` to `.env`, fill in `SA_PASSWORD` and `JWT_SECRET_KEY` (compose refuses to start without them), then `docker compose up -d`.

## Conventions

- Every business table carries an integer key, an internal `SysGuid`, a user-facing code, audit columns and soft delete; physical deletes are blocked by the DbContext.
- Tenancy: `ICurrentUserContext.CompanyId` (JWT `company_id` claim or `X-Company-Id` header) drives global query filters.
- Services signal business-rule violations with `InvalidOperationException` and missing records with `KeyNotFoundException`; controllers translate them to 400/404.
- New module checklist: entities in `Domain/Entities/<Module>`, EF configuration in `Data/Configurations/<Module>`, contract in `Application/Abstractions/<Module>`, DTOs in `Application/Models/<Module>`, implementation in `Infrastructure/Services/<Module>`, registration in `DependencyInjection.cs`, a controller, and tests.
