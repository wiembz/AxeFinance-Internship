# Tech Dashboard - Deployment Guide

## 🚀 Quick Start Deployment

### Prerequisites
- **.NET 8 SDK** or later
- **SQL Server** (LocalDB for development, SQL Server for production)
- **IIS** or **Docker** (for production deployment)
- **Node.js 18+** (for Angular frontend)

## 🔧 Development Deployment

### 1. Backend API Setup

#### Clone and Setup
```powershell
# Navigate to the backend directory
cd TechDashboardAPI

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build
```

#### Database Setup
```powershell
# Install Entity Framework CLI (if not installed)
dotnet tool install --global dotnet-ef

# Update database with migrations
dotnet ef database update --project .\TechDashboardAPI.Infrastructure --startup-project .\TechDashboardAPI.API
```

#### Run Development Server
```powershell
# Navigate to API project
cd .\TechDashboardAPI.API

# Run the API (Development mode)
dotnet run

# API will be available at:
# HTTP: http://localhost:5108
# HTTPS: https://localhost:7130
# Swagger: http://localhost:5108/swagger
```

### 2. Frontend Setup (Optional)

```bash
# Navigate to frontend directory
cd tech-dashboard-front

# Install dependencies
npm install

# Run development server
ng serve

# Frontend available at: http://localhost:4200
# Proxy to API: add --proxy-config proxy.conf.json (already included)
```

## 🏢 Production Deployment

### 1. Production Database Setup

#### SQL Server Configuration
```sql
-- Create production database
CREATE DATABASE TechDashboardDB_Production;

-- Create application user
CREATE LOGIN TechDashboardUser WITH PASSWORD = 'YourSecurePassword123!';
CREATE USER TechDashboardUser FOR LOGIN TechDashboardUser;
ALTER ROLE db_datareader ADD MEMBER TechDashboardUser;
ALTER ROLE db_datawriter ADD MEMBER TechDashboardUser;
ALTER ROLE db_ddladmin ADD MEMBER TechDashboardUser;
```

#### Connection String Update
```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=TechDashboardDB_Production;User Id=TechDashboardUser;Password=YourSecurePassword123!;TrustServerCertificate=true;"
  }
}
```

### 2. Production Configuration

#### Security Settings
```json
// appsettings.Production.json
{
  "Security": {
    "Swagger": { "AllowInProduction": false },
    "CSP": { "ReportUri": null },
    "HSTS": { "MaxAge": 31536000, "IncludeSubDomains": true, "Preload": false }
  },
  "JwtSettings": {
    "SecretKey": "YOUR_STRONG_JWT_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG",
    "Issuer": "https://yourdomain.com",
    "Audience": "https://yourdomain.com",
    "ExpirationHours": 24
  },
  "FakeAdp": {
    "Enabled": false
  },
  "EmailSettings": {
    "SmtpHost": "your-smtp-server.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@company.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "noreply@company.com",
    "FromName": "Tech Dashboard"
  }
}
```

> To enable Swagger UI in production temporarily for troubleshooting, set `Security:Swagger:AllowInProduction` to `true`. The middleware will relax CSP and X-Frame-Options only for `/swagger` paths. Revert to `false` after use.

#### CORS Configuration
```json
// appsettings.Production.json
{
  "AllowedOrigins": [
  "https://your-frontend.example.com",
  "https://www.your-frontend.example.com"
  ]
}
```

> Note: The API’s production CORS policy reads AllowedOrigins from configuration. If this array is missing or empty, browsers will block cross-origin requests from your frontend. Set these to your actual frontend URLs (scheme + host + optional port). Do not use wildcards in production.

### 3. IIS Deployment

#### Publish Application
```powershell
# Publish for production
dotnet publish -c Release -o ./publish

# Copy publish folder to IIS wwwroot
# Example: C:\inetpub\wwwroot\TechDashboardAPI\
```

#### IIS Configuration
```xml
<!-- web.config (auto-generated, but verify) -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\TechDashboardAPI.API.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

### 4. Docker Deployment

#### Dockerfile
```dockerfile
# Create Dockerfile in TechDashboardAPI.API directory
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TechDashboardAPI.API/TechDashboardAPI.API.csproj", "TechDashboardAPI.API/"]
COPY ["TechDashboardAPI.Application/TechDashboardAPI.Application.csproj", "TechDashboardAPI.Application/"]
COPY ["TechDashboardAPI.Infrastructure/TechDashboardAPI.Infrastructure.csproj", "TechDashboardAPI.Infrastructure/"]
COPY ["TechDashboardAPI.Domain/TechDashboardAPI.Domain.csproj", "TechDashboardAPI.Domain/"]

RUN dotnet restore "TechDashboardAPI.API/TechDashboardAPI.API.csproj"
COPY . .
WORKDIR "/src/TechDashboardAPI.API"
RUN dotnet build "TechDashboardAPI.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TechDashboardAPI.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TechDashboardAPI.API.dll"]
```

#### Docker Compose
```yaml
# docker-compose.yml
version: '3.8'
services:
  techdashboard-api:
    build:
      context: .
      dockerfile: TechDashboardAPI.API/Dockerfile
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sql-server;Database=TechDashboardDB;User Id=sa;Password=YourPassword123!
    depends_on:
      - sql-server
      
  sql-server:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourPassword123!"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

volumes:
  sqlserver_data:
```

#### Deploy with Docker
```powershell
# Build and run
docker-compose up -d

# Check logs
docker-compose logs -f techdashboard-api

# Scale if needed
docker-compose up -d --scale techdashboard-api=3
```

## 🔒 Security Checklist

### Pre-Production Security
- [ ] **JWT Secret**: Generate strong, unique JWT secret key
- [ ] **Fake ADP**: Disable fake authentication in production
- [ ] **HTTPS**: Enforce HTTPS-only connections
- [ ] **CORS**: Configure proper allowed origins
- [ ] **Email**: Configure production SMTP settings
- [ ] **Database**: Use dedicated database user with minimal permissions
- [ ] **Passwords**: Use strong passwords for all accounts
- [ ] **Logs**: Configure secure logging (don't log sensitive data)

### Runtime Security
```json
// Security headers configuration
{
  "SecurityHeaders": {
    "ContentSecurityPolicy": "default-src 'self'",
    "XFrameOptions": "DENY",
    "XContentTypeOptions": "nosniff",
    "ReferrerPolicy": "no-referrer",
    "StrictTransportSecurity": "max-age=31536000; includeSubDomains"
  }
}
```

## 📊 Health Monitoring

### Health Check Endpoints
- **Basic Health**: `GET /api/health` - Basic application health
- **Database Health**: Automatic database connectivity check
- **Memory Usage**: Application memory monitoring
- **Disk Space**: Storage availability monitoring

### Monitoring Setup
```powershell
# Test health endpoints
curl https://yourdomain.com/health/live; curl https://yourdomain.com/health/ready

# Expected response:
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "memory": "Healthy",
    "disk": "Healthy"
  }
}
```

## 🗄️ Database Migration

### Production Migration Strategy
```powershell
# 1. Backup production database
sqlcmd -S YourServer -Q "BACKUP DATABASE TechDashboardDB_Production TO DISK = 'C:\\Backups\\TechDashboardDB_Production.bak'"

# 2. Test migration on backup first
# 3. Apply migration during maintenance window
dotnet ef database update --project TechDashboardAPI.Infrastructure --startup-project TechDashboardAPI.API --environment Production

# 4. Verify migration success
# 5. Update application to use new features
```

### Migration Best Practices
- Always backup before migration
- Test migrations on production copy first
- Plan for rollback scenarios
- Schedule during low-traffic periods
- Monitor application post-migration

## 🔧 Configuration Management

### Environment Variables
```powershell
# Set environment variables (Linux/macOS)
export ASPNETCORE_ENVIRONMENT=Production
export JWT_SECRET_KEY=YourSecretKey
export DB_CONNECTION_STRING="Your Connection String"

# Set environment variables (Windows)
set ASPNETCORE_ENVIRONMENT=Production
set JWT_SECRET_KEY=YourSecretKey
set DB_CONNECTION_STRING="Your Connection String"
```

### Configuration Files
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production overrides
- `appsettings.Staging.json` - Staging environment (optional)

## 📝 Post-Deployment

### 1. Initial Setup
```bash
# Create first SuperAdmin user
POST /api/auth/register
{
  "firstName": "Super",
  "lastName": "Admin", 
  "email": "admin@yourcompany.com",
  "password": "SecurePassword123!"
}
```

### 2. Verify Deployment
- [ ] Health check returns healthy status
- [ ] Database connection successful
- [ ] Authentication endpoints working
- [ ] File upload functionality working
- [ ] Email service sending notifications
- [ ] HTTPS certificate valid and working
- [ ] CORS policy allowing frontend access

### 3. Performance Testing
```powershell
# Basic load testing
ab -n 1000 -c 10 https://yourdomain.com/api/health

# Authentication testing
curl -X POST https://yourdomain.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@yourcompany.com","password":"SecurePassword123!"}'
```

## 🚨 Troubleshooting

### Common Issues

#### Database Connection Issues
```powershell
# Check connection string
# Verify SQL Server accessibility
# Check firewall rules
# Verify user permissions
```

#### Authentication Issues
```powershell
# Check JWT secret key configuration
# Verify token expiration settings
# Check CORS configuration
# Validate user roles and permissions
```

#### Performance Issues
```powershell
# Check database query performance
# Verify connection pooling
# Monitor memory usage
# Check disk space availability
```

## 📞 Support

### Production Support Checklist
- [ ] Monitor health endpoints regularly
- [ ] Set up automated backup schedules
- [ ] Configure log rotation
- [ ] Set up alerting for critical errors
- [ ] Document rollback procedures
- [ ] Maintain staging environment for testing

---

## ✅ Deployment Success Criteria

Your Tech Dashboard deployment is successful when:
- Health endpoint returns "Healthy" status
- First SuperAdmin can register and login
- JWT authentication works properly
- Database migrations completed successfully
- Email notifications are sent
- File uploads work correctly
- API documentation available via Swagger
- Production security settings active

**Your enterprise-grade Tech Dashboard is now ready for production use!**

