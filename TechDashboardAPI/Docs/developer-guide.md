## Developer Guide

This guide is a quick reference for daily development. For the full onboarding, read `Docs/project-overview.md` (the hub).

### Prereqs
- .NET 8 SDK
- SQL Server (LocalDB or instance)
- PowerShell (Windows)

### First-time setup
1) Configure `TechDashboardAPI.API/appsettings.Development.json`:
   - `ConnectionStrings:DefaultConnection`
   - `JwtSettings:SecretKey` (dev value), `Issuer`, `Audience` (optional in dev)
   - `FakeAdp:Enabled` (true in dev if testing ADP flow)
2) Restore and build:
   - `dotnet restore`
   - `dotnet build .\TechDashboardAPI.sln -c Debug`
3) Apply migrations (optional but recommended):
   - `dotnet tool install --global dotnet-ef`
   - `dotnet ef database update --project .\TechDashboardAPI.Infrastructure --startup-project .\TechDashboardAPI.API`

### Run the API
- `dotnet run --project .\TechDashboardAPI.API -c Debug`
- Swagger is enabled in Development at `/swagger`.

### Health checks
- Liveness: `GET /health/live`
- Readiness: `GET /health/ready`
- Controller-based: `GET /api/health`

### Testing
- `dotnet test .\TechDashboardAPI.Tests\TechDashboardAPI.Tests.csproj -c Debug`

### Common tasks
- Add a new endpoint:
  - DTOs in `TechDashboardAPI.Application/DTOs`
  - Interface in `TechDashboardAPI.Application/Interfaces`
  - Implementation in `TechDashboardAPI.Infrastructure/Services`
  - Controller in `TechDashboardAPI.API/Controllers`
  - Tests in `TechDashboardAPI.Tests`

- Update DB schema:
  - Edit entities in `TechDashboardAPI.Domain/Entities`
  - `dotnet ef migrations add <Name> --project .\TechDashboardAPI.Infrastructure --startup-project .\TechDashboardAPI.API`
  - `dotnet ef database update --project .\TechDashboardAPI.Infrastructure --startup-project .\TechDashboardAPI.API`

### Security notes
- Roles use `ClaimTypes.Role`; policies use `RequireRole`.
- Passwords: PBKDF2-SHA256 with per-user salt; legacy hashes auto-upgrade on login.
- JWT: issuer/audience validated in production; zero clock skew outside dev.
- CORS: configure `AllowedOrigins` in production.
- Swagger in prod is disabled by default; enable via `Security:Swagger:AllowInProduction=true` only temporarily.

### File uploads
- Stored under `wwwroot/uploads`.
- Filenames sanitized; path normalized; MIME/extension whitelist enforced.
- Max size via `FileUpload:MaxFileSizeMB`.

### Quick troubleshooting
- 401/403: check `JwtSettings` and role claim.
- CORS: verify `AllowedOrigins`.
- Readiness failing: DB connectivity/migrations.

See also:
- `Docs/project-overview.md`
- `Docs/architecture.md`
- `Docs/deployment-guide.md`

