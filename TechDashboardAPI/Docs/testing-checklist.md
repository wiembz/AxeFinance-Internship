# Manual Testing Checklist

## Setup
- [ ] appsettings.Development.json configured (ConnectionStrings, JwtSettings)
- [ ] Database migrated
- [ ] API running locally (http://localhost:5108, https://localhost:7130)

## Health
- [ ] GET /health/live returns 200
- [ ] GET /health/ready returns 200 (DB reachable)
- [ ] GET /api/health returns ApiResponse with status and checks

## Auth
- [ ] GET /api/auth/superadmin-exists returns expected boolean
- [ ] Register first user → becomes SuperAdmin
- [ ] Login returns JWT and user payload
- [ ] Protected endpoint requires Authorization header

## Users
- [ ] List users with filters and pagination
- [ ] Update own profile
- [ ] Admin can change roles and activate/deactivate users

## Departments & Projects
- [ ] CRUD departments (Admin+)
- [ ] CRUD projects (Admin+)
- [ ] List projects by department

## Problems & Solutions
- [ ] Create/update/delete problem (creator or Admin+)
- [ ] Create/approve solution (Contributor/Admin)
- [ ] Like/unlike problem
- [ ] Filtering by status/priority/search

## Forms
- [ ] Define form fields
- [ ] Submit problem with custom field values
- [ ] Validate required fields

## Files
- [ ] Upload file; verify stored path and URL
- [ ] Rejected invalid types and oversize files
- [ ] Download serves correct MIME type

## Security
- [ ] CORS blocks disallowed origin; allows configured origins in prod
- [ ] Swagger not accessible in prod unless enabled
- [ ] No PII in logs (emails/passwords/tokens)

## Tests
- [ ] `dotnet test` green

Notes:
- Use strong test passwords (e.g., `Str0ng#Passw0rd!`).
- For Fake ADP flows, ensure `FakeAdp.Enabled=true` in dev config.

