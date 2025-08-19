using System.Diagnostics;
using System.Text.Json;

namespace TechDashboardAPI.API.Extensions;


public static class LoggingExtensions
{
    
    public static IServiceCollection AddCustomLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            // Clear default providers for clean configuration
            builder.ClearProviders();
            
            // Configure console logging with enhanced formatting
            builder.AddConsole();
            
            // Add debug logging for development environments
            builder.AddDebug();
            
            // Configure Event Source logging for ETW integration
            builder.AddEventSourceLogger();
            
            // Add configuration from appsettings with enhanced structure
            var loggingSection = configuration.GetSection("Logging");
            builder.AddConfiguration(loggingSection);
            
            // Configure log filtering for performance and security
            builder.AddFilter((category, level) =>
            {
                // Exclude sensitive or high-volume categories in production
                if (category?.Contains("Microsoft.EntityFrameworkCore.Database.Command") == true)
                {
                    // Only log EF SQL commands in development
                    return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
                }
                
                // Filter out noisy system logs unless debugging
                if (category?.StartsWith("Microsoft.AspNetCore.Hosting") == true)
                {
                    return level >= LogLevel.Warning;
                }
                
                if (category?.StartsWith("Microsoft.AspNetCore.Mvc") == true)
                {
                    return level >= LogLevel.Information;
                }
                
                // Default filtering based on configuration
                return level >= LogLevel.Information;
            });
        });

        // Configure additional logging services
        services.AddSingleton<ILoggerFactory, LoggerFactory>();
        
        // Add correlation ID service for request tracking
        services.AddScoped<CorrelationIdService>();
        
        // Configure logging enrichment services
        services.AddTransient<ILogEnricher, SecurityLogEnricher>();
        services.AddTransient<ILogEnricher, PerformanceLogEnricher>();
        services.AddTransient<ILogEnricher, CorrelationLogEnricher>();

        return services;
    }

    
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var correlationService = context.RequestServices.GetRequiredService<CorrelationIdService>();
            
            // Generate correlation ID for request tracking
            var correlationId = correlationService.GetOrCreateCorrelationId(context);
            
            // Start performance timing
            var stopwatch = Stopwatch.StartNew();
            var startTime = DateTime.UtcNow;
            
            // Create log scope for correlation
            using var scope = logger.BeginScope("CorrelationId: {CorrelationId}", correlationId);
            
            try
            {
                // Log incoming request with security-aware information
                var requestInfo = new
                {
                    Method = context.Request.Method,
                    Path = context.Request.Path.Value,
                    QueryString = SanitizeQueryString(context.Request.QueryString.Value),
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    ClientIP = GetClientIPAddress(context),
                    ContentType = context.Request.ContentType,
                    ContentLength = context.Request.ContentLength,
                    UserId = context.User?.Identity?.Name ?? "Anonymous",
                    IsAuthenticated = context.User?.Identity?.IsAuthenticated ?? false,
                    Timestamp = startTime
                };

                logger.LogInformation("Incoming Request: {@RequestInfo}", requestInfo);

                // Log request body for POST/PUT requests (with sanitization)
                if (ShouldLogRequestBody(context.Request))
                {
                    var requestBody = await ReadAndSanitizeRequestBody(context.Request);
                    if (!string.IsNullOrEmpty(requestBody))
                    {
                        logger.LogDebug("Request Body: {RequestBody}", requestBody);
                    }
                }

                // Process the request
                await next.Invoke();
                
                // Calculate processing duration
                stopwatch.Stop();
                var duration = stopwatch.ElapsedMilliseconds;

                // Log response information
                var responseInfo = new
                {
                    StatusCode = context.Response.StatusCode,
                    ContentType = context.Response.ContentType,
                    ContentLength = context.Response.ContentLength,
                    Duration = duration,
                    Timestamp = DateTime.UtcNow
                };

                var logLevel = GetLogLevelForResponse(context.Response.StatusCode, duration);
                logger.Log(logLevel, "Request Completed: {@ResponseInfo}", responseInfo);

                // Log performance warnings for slow requests
                if (duration > GetSlowRequestThreshold(context))
                {
                    logger.LogWarning("Slow Request Detected: {Method} {Path} took {Duration}ms", 
                        context.Request.Method, context.Request.Path, duration);
                }

                // Log security events if applicable
                LogSecurityEvents(logger, context, duration);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var duration = stopwatch.ElapsedMilliseconds;

                // Log exception with comprehensive context
                logger.LogError(ex, "Request Processing Error after {Duration}ms: {Method} {Path}", 
                    duration, context.Request.Method, context.Request.Path);
                
                throw; // Re-throw to let global exception handler process
            }
        });
    }


    private static string? SanitizeQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString)) return queryString;

        var sensitiveParams = new[] { "password", "token", "key", "secret", "auth", "credential" };
        var sanitized = queryString;

        foreach (var param in sensitiveParams)
        {
            var pattern = $@"({param}=)[^&]*";
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized, pattern, $"$1***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return sanitized;
    }


    private static Dictionary<string, string> GetSanitizedHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        var sensitiveHeaders = new[] { "authorization", "cookie", "x-api-key", "x-auth-token" };
        var sanitized = new Dictionary<string, string>();

        foreach (var header in headers)
        {
            var key = header.Key.ToLower();
            if (sensitiveHeaders.Contains(key))
            {
                sanitized[header.Key] = "***";
            }
            else
            {
                sanitized[header.Key] = string.Join(", ", header.Value);
            }
        }

        return sanitized;
    }


    private static bool ShouldLogRequestBody(HttpRequest request)
    {
        if (request.ContentLength > 10000) return false; // Skip large bodies
        
        var contentType = request.ContentType?.ToLower() ?? "";
        return contentType.Contains("application/json") || 
               contentType.Contains("application/xml") ||
               contentType.Contains("text/");
    }

    private static async Task<string?> ReadAndSanitizeRequestBody(HttpRequest request)
    {
        try
        {
            request.EnableBuffering(); // Allow multiple reads
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0; // Reset for normal processing

            // Sanitize sensitive fields in JSON
            if (request.ContentType?.Contains("application/json") == true)
            {
                return SanitizeJsonBody(body);
            }

            return body;
        }
        catch
        {
            return null; // Ignore errors in logging
        }
    }

    private static string SanitizeJsonBody(string jsonBody)
    {
        try
        {
            var doc = JsonDocument.Parse(jsonBody);
            var sanitized = SanitizeJsonElement(doc.RootElement);
            return JsonSerializer.Serialize(sanitized, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            return jsonBody; // Return original if parsing fails
        }
    }


    private static object SanitizeJsonElement(JsonElement element)
    {
        var sensitiveFields = new[] { "password", "token", "key", "secret", "credential", "auth" };

        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                prop => prop.Name,
                prop => sensitiveFields.Any(field => prop.Name.ToLower().Contains(field))
                    ? "***"
                    : SanitizeJsonElement(prop.Value)
            ),
            JsonValueKind.Array => element.EnumerateArray().Select(SanitizeJsonElement).ToArray(),
            _ => element.GetRawText()
        };
    }

    private static string GetClientIPAddress(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private static LogLevel GetLogLevelForResponse(int statusCode, long durationMs)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ when durationMs > 5000 => LogLevel.Warning, // Slow requests
            _ => LogLevel.Information
        };
    }


    private static int GetSlowRequestThreshold(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Different thresholds for different endpoint types
        if (path.Contains("/health")) return 100;
        if (path.Contains("/auth")) return 500;
        if (path.Contains("/files") || path.Contains("/upload")) return 10000;
        
        return 2000; // Default threshold
    }

    private static void LogSecurityEvents(ILogger logger, HttpContext context, long duration)
    {
        // Log authentication failures
        if (context.Response.StatusCode == 401)
        {
            logger.LogWarning("Authentication Failed: {Method} {Path} from IP {ClientIP}",
                context.Request.Method, context.Request.Path, GetClientIPAddress(context));
        }

        // Log authorization failures
        if (context.Response.StatusCode == 403)
        {
            logger.LogWarning("Authorization Failed: User {User} attempted {Method} {Path}",
                context.User?.Identity?.Name ?? "Anonymous", context.Request.Method, context.Request.Path);
        }

        // Log suspicious request patterns
        if (context.Request.Path.Value?.Contains("..") == true ||
            context.Request.QueryString.Value?.Contains("<script") == true)
        {
            logger.LogWarning("Suspicious Request Pattern: {Method} {Path}{QueryString} from IP {ClientIP}",
                context.Request.Method, context.Request.Path, context.Request.QueryString, GetClientIPAddress(context));
        }
    }
}


public class CorrelationIdService
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get from header first
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) && 
            !string.IsNullOrEmpty(correlationId.FirstOrDefault()))
        {
            return correlationId.First()!;
        }

        // Generate new correlation ID
        var newCorrelationId = Guid.NewGuid().ToString();
        context.Response.Headers[CorrelationIdHeader] = newCorrelationId;
        return newCorrelationId;
    }
}


public interface ILogEnricher
{
    void Enrich(LogEvent logEvent, HttpContext? context);
}


public class SecurityLogEnricher : ILogEnricher
{
    public void Enrich(LogEvent logEvent, HttpContext? context)
    {
        if (context != null)
        {
            logEvent.Properties["ClientIP"] = GetClientIPAddress(context);
            logEvent.Properties["UserAgent"] = context.Request.Headers["User-Agent"].ToString();
            logEvent.Properties["UserId"] = context.User?.Identity?.Name ?? "Anonymous";
        }
    }

    private static string GetClientIPAddress(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}


public class PerformanceLogEnricher : ILogEnricher
{
    public void Enrich(LogEvent logEvent, HttpContext? context)
    {
        logEvent.Properties["MachineName"] = Environment.MachineName;
        logEvent.Properties["ProcessId"] = Environment.ProcessId;
        logEvent.Properties["ThreadId"] = Thread.CurrentThread.ManagedThreadId;
    }
}


public class CorrelationLogEnricher : ILogEnricher
{
    public void Enrich(LogEvent logEvent, HttpContext? context)
    {
        if (context?.Response.Headers.ContainsKey("X-Correlation-ID") == true)
        {
            logEvent.Properties["CorrelationId"] = context.Response.Headers["X-Correlation-ID"].ToString();
        }
    }
}


public class LogEvent
{
    public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();
}
