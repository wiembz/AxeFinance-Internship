using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace TechDashboardAPI.API.Extensions;
public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new ArgumentNullException(nameof(secretKey), 
                "JWT Secret Key must be configured in application settings under JwtSettings:SecretKey");
        }
        if (secretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT Secret Key must be at least 32 characters long for HS256 security");
        }
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var strictValidation = !System.Diagnostics.Debugger.IsAttached;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = strictValidation,
                ValidateAudience = strictValidation,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true,
                ValidateTokenReplay = false, // Set to true if token replay protection is needed
                RoleClaimType = ClaimTypes.Role
            };
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            if (strictValidation && (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience)))
            {
                throw new InvalidOperationException(
                    "In production, JwtSettings:Issuer and JwtSettings:Audience must be configured for strict JWT validation.");
            }
            if (!string.IsNullOrWhiteSpace(issuer))
            {
                options.TokenValidationParameters.ValidIssuer = issuer;
            }
            if (!string.IsNullOrWhiteSpace(audience))
            {
                options.TokenValidationParameters.ValidAudience = audience;
            }
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT Authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    var principal = context.Principal;
                    var uid = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? principal?.FindFirst("nameid")?.Value
                              ?? principal?.FindFirst("sub")?.Value
                              ?? "Unknown";
                    logger.LogDebug("JWT Token validated for user: {UserId}", uid);
                    return Task.CompletedTask;
                },

                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    // Debug: trace Authorization header for /api/auth/me calls
                    if (path.StartsWithSegments("/api/auth/me"))
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        var hasAuth = context.Request.Headers.TryGetValue("Authorization", out var authHdr);
                        var preview = hasAuth ? (authHdr.ToString().Length > 40 ? authHdr.ToString().Substring(0, 40) + "..." : authHdr.ToString()) : "<none>";
                        logger.LogDebug("OnMessageReceived: {Path} Authorization header: {Header}", path, preview);
                    }
                    
                    // If no Authorization header, try JWT from a cookie (opt-in for cookie-based auth)
                    if (string.IsNullOrEmpty(context.Token))
                    {
                        if (context.Request.Cookies.TryGetValue("td_auth", out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
                        {
                            context.Token = cookieToken;
                        }
                    }

                    if (!string.IsNullOrEmpty(accessToken) && 
                        (path.StartsWithSegments("/hubs") || path.StartsWithSegments("/download")))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
            options.RequireHttpsMetadata = !System.Diagnostics.Debugger.IsAttached;
            options.SaveToken = true;
        });

        return services;
    }
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("SuperAdminOnly", policy => 
                policy.RequireRole("SuperAdmin"));
            options.AddPolicy("AdminOrAbove", policy => 
                policy.RequireRole("SuperAdmin", "Admin"));
            options.AddPolicy("ContributorOrAbove", policy => 
                policy.RequireRole("SuperAdmin", "Admin", "Contributor"));
            options.AddPolicy("ViewerOrAbove", policy => 
                policy.RequireRole("SuperAdmin", "Admin", "Contributor", "Viewer"));
            options.AddPolicy("CanCreateProblems", policy =>
                policy.RequireRole("SuperAdmin", "Admin", "Contributor"));

            options.AddPolicy("CanDeleteProblems", policy =>
                policy.RequireRole("SuperAdmin", "Admin"));
            options.AddPolicy("CanCreateSolutions", policy =>
                policy.RequireRole("SuperAdmin", "Admin", "Contributor"));

            options.AddPolicy("CanApproveSolutions", policy =>
                policy.RequireRole("SuperAdmin", "Admin"));
            options.AddPolicy("CanManageDepartments", policy =>
                policy.RequireRole("SuperAdmin", "Admin"));
            options.AddPolicy("CanManageProjects", policy =>
                policy.RequireRole("SuperAdmin", "Admin"));
            options.AddPolicy("CanManageUsers", policy =>
                policy.RequireRole("SuperAdmin"));

            options.AddPolicy("CanViewUsers", policy =>
                policy.RequireRole("SuperAdmin", "Admin"));
            options.AddPolicy("CanViewReports", policy =>
                policy.RequireRole("SuperAdmin", "Admin", "Contributor"));

            options.AddPolicy("CanExportData", policy =>
                policy.RequireRole("SuperAdmin", "Admin"));
            options.AddPolicy("CanUploadFiles", policy =>
                policy.RequireRole("SuperAdmin", "Admin", "Contributor"));

            options.AddPolicy("CanDeleteFiles", policy =>
                policy.RequireRole("SuperAdmin", "Admin"));
        });

        return services;
    }
    public static bool HasRoleOrHigher(this System.Security.Claims.ClaimsPrincipal principal, string requiredRole)
    {
        var userRole = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                        ?? principal.FindFirst("role")?.Value; // fallback for legacy tokens
        if (string.IsNullOrEmpty(userRole))
            return false;

        var roleHierarchy = new[] { "Viewer", "Contributor", "Admin", "SuperAdmin" };
        
        var userRoleIndex = Array.IndexOf(roleHierarchy, userRole);
        var requiredRoleIndex = Array.IndexOf(roleHierarchy, requiredRole);

        return userRoleIndex >= 0 && requiredRoleIndex >= 0 && userRoleIndex >= requiredRoleIndex;
    }
}
