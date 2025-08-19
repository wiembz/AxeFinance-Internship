namespace TechDashboardAPI.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            AddSecurityHeaders(context);
            await _next(context);
            AddPostProcessingHeaders(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SecurityHeadersMiddleware for {Method} {Path}", 
                context.Request.Method, context.Request.Path);
            throw;
        }
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;
        var request = context.Request;
        var headers = response.Headers;
        if (!headers.ContainsKey("X-Content-Type-Options"))
        {
            headers["X-Content-Type-Options"] = "nosniff";
        }
        if (!headers.ContainsKey("X-Frame-Options"))
        {
            var allowSwaggerInProd = _configuration.GetValue("Security:Swagger:AllowInProduction", false);
            var isSwaggerPath = request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);
            var frameOptions = _environment.IsDevelopment() || (allowSwaggerInProd && isSwaggerPath)
                ? "SAMEORIGIN"
                : "DENY";
            headers["X-Frame-Options"] = frameOptions;
        }
        if (!headers.ContainsKey("X-XSS-Protection"))
        {
            headers["X-XSS-Protection"] = "1; mode=block";
        }
        if (!headers.ContainsKey("Strict-Transport-Security") && request.IsHttps)
        {
            var hstsMaxAge = _configuration.GetValue("Security:HSTS:MaxAge", 31536000);
            var includeSubDomains = _configuration.GetValue("Security:HSTS:IncludeSubDomains", true);
            var preload = _configuration.GetValue("Security:HSTS:Preload", false);

            var hstsValue = $"max-age={hstsMaxAge}";
            if (includeSubDomains) hstsValue += "; includeSubDomains";
            if (preload) hstsValue += "; preload";

            headers["Strict-Transport-Security"] = hstsValue;
        }
        if (!headers.ContainsKey("Referrer-Policy"))
        {
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }
        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            var csp = BuildContentSecurityPolicy(context);
            headers["Content-Security-Policy"] = csp;
        }
        if (!headers.ContainsKey("Permissions-Policy"))
        {
            var permissionsPolicy = BuildPermissionsPolicy();
            headers["Permissions-Policy"] = permissionsPolicy;
        }
        if (!headers.ContainsKey("Cross-Origin-Embedder-Policy"))
        {
            var coep = _environment.IsDevelopment() ? "credentialless" : "require-corp";
            headers["Cross-Origin-Embedder-Policy"] = coep;
        }
        if (!headers.ContainsKey("Cross-Origin-Opener-Policy"))
        {
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
        }
        if (!headers.ContainsKey("Cross-Origin-Resource-Policy"))
        {
            var corp = request.Path.StartsWithSegments("/api") ? "cross-origin" : "same-origin";
            headers["Cross-Origin-Resource-Policy"] = corp;
        }
        if (!headers.ContainsKey("X-Permitted-Cross-Domain-Policies"))
        {
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
        }
        if (!headers.ContainsKey("X-Robots-Tag"))
        {
            if (request.Path.StartsWithSegments("/api") || 
                request.Path.StartsWithSegments("/admin") ||
                request.Path.StartsWithSegments("/swagger"))
            {
                headers["X-Robots-Tag"] = "noindex, nofollow, nosnippet, noarchive";
            }
        }
        if (!headers.ContainsKey("Cache-Control") && !IsStaticResource(request.Path))
        {
            headers["Cache-Control"] = "no-cache, no-store, must-revalidate, private";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";
        }
        _logger.LogDebug("Security headers applied for {Method} {Path}", 
            request.Method, request.Path);
    }
    private string BuildContentSecurityPolicy(HttpContext context)
    {
        var cspBuilder = new List<string>();
        var request = context.Request;
        var allowSwaggerInProd = _configuration.GetValue("Security:Swagger:AllowInProduction", false);
        var isSwaggerPath = request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);

        if (_environment.IsDevelopment())
        {
            cspBuilder.AddRange(new[]
            {
                "default-src 'self'",
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'",
                "style-src 'self' 'unsafe-inline'",
                "img-src 'self' data: blob:",
                "font-src 'self' data:",
                "connect-src 'self' ws: wss:",
                "media-src 'self'",
                "object-src 'none'",
                "frame-src 'self'",
                "base-uri 'self'",
                "form-action 'self'"
            });
        }
        else
        {
            if (allowSwaggerInProd && isSwaggerPath)
            {
                cspBuilder.AddRange(new[]
                {
                    "default-src 'self'",
                    "script-src 'self'",
                    "style-src 'self' 'unsafe-inline'",
                    "img-src 'self' data:",
                    "font-src 'self' data:",
                    "connect-src 'self'",
                    "media-src 'self'",
                    "object-src 'none'",
                    "frame-src 'self'",
                    "base-uri 'self'",
                    "form-action 'self'",
                    "frame-ancestors 'self'",
                    "upgrade-insecure-requests"
                });
            }
            else
            {
                cspBuilder.AddRange(new[]
                {
                    "default-src 'self'",
                    "script-src 'self'",
                    "style-src 'self'",
                    "img-src 'self' data:",
                    "font-src 'self'",
                    "connect-src 'self'",
                    "media-src 'self'",
                    "object-src 'none'",
                    "frame-src 'none'",
                    "base-uri 'self'",
                    "form-action 'self'",
                    "frame-ancestors 'none'",
                    "upgrade-insecure-requests"
                });
            }
        }

        var reportUri = _configuration.GetValue<string>("Security:CSP:ReportUri");
        if (!string.IsNullOrEmpty(reportUri))
        {
            cspBuilder.Add($"report-uri {reportUri}");
        }

        return string.Join("; ", cspBuilder);
    }
    private string BuildPermissionsPolicy()
    {
        var policies = new[]
        {
            "accelerometer=()",
            "ambient-light-sensor=()",
            "autoplay=()",
            "battery=()",
            "camera=()",
            "display-capture=()",
            "document-domain=()",
            "encrypted-media=()",
            "execution-while-not-rendered=()",
            "execution-while-out-of-viewport=()",
            "fullscreen=()",
            "geolocation=()",
            "gyroscope=()",
            "magnetometer=()",
            "microphone=()",
            "midi=()",
            "navigation-override=()",
            "payment=()",
            "picture-in-picture=()",
            "publickey-credentials-get=()",
            "speaker-selection=()",
            "sync-xhr=()",
            "usb=()",
            "web-share=()",
            "xr-spatial-tracking=()"
        };

        return string.Join(", ", policies);
    }
    private void AddPostProcessingHeaders(HttpContext context)
    {
        if (_environment.IsDevelopment() && 
            !context.Response.Headers.ContainsKey("X-Response-Time"))
        {
        }
        if (IsSensitiveOperation(context.Request))
        {
            _logger.LogInformation("Security headers applied to sensitive operation: {Method} {Path}", 
                context.Request.Method, context.Request.Path);
        }
    }
    private static bool IsStaticResource(PathString path)
    {
        var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".woff", ".woff2", ".ttf", ".eot" };
        return staticExtensions.Any(ext => path.Value?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) == true);
    }
    private static bool IsSensitiveOperation(HttpRequest request)
    {
        var sensitivePaths = new[] { "/api/auth", "/api/users", "/api/admin" };
        var sensitiveOperations = new[] { "POST", "PUT", "DELETE" };

        return sensitivePaths.Any(path => request.Path.StartsWithSegments(path)) &&
               sensitiveOperations.Contains(request.Method, StringComparer.OrdinalIgnoreCase);
    }
}
