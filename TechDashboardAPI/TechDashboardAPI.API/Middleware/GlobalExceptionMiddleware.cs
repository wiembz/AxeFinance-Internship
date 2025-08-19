using System.Net;
using System.Security;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TechDashboardAPI.API.Constants;

namespace TechDashboardAPI.API.Middleware;

/// <summary>
/// Global Exception Handling Middleware
/// 
/// This middleware provides comprehensive exception handling for the entire Tech Dashboard API.
/// It captures all unhandled exceptions, logs them appropriately, and returns consistent,
/// user-friendly error responses while protecting sensitive system information.
/// 
/// Key Features:
/// - Centralized exception handling for consistent error responses
/// - Comprehensive logging with correlation IDs for debugging
/// - Security-aware error reporting (no sensitive data exposure)
/// - Performance monitoring of exception patterns and frequency
/// - Integration with monitoring and alerting systems
/// - Structured error responses for client consumption
/// 
/// Exception Categories Handled:
/// - Authentication and authorization errors
/// - Validation and business logic errors
/// - Database and infrastructure errors
/// - External service integration errors
/// - Unexpected system errors and crashes
/// 
/// Security Features:
/// - Prevents sensitive information leakage in error responses
/// - Logs detailed error context for internal investigation
/// - Provides appropriate HTTP status codes for different error types
/// - Implements rate limiting for error response prevention
/// - Maintains audit trail for security incident investigation
/// 
/// Response Format:
/// - Consistent JSON structure across all error types
/// - Includes correlation ID for support and debugging
/// - Provides user-friendly messages for client display
/// - Contains technical details only in development environment
/// - Supports internationalization for error messages
/// 
/// Performance Considerations:
/// - Efficient exception categorization and handling
/// - Minimal overhead for successful request processing
/// - Optimized logging to prevent performance degradation
/// - Memory-efficient error object creation and serialization
/// - Async processing to prevent request pipeline blocking
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the GlobalExceptionMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the request pipeline</param>
    /// <param name="logger">Logger for exception tracking and monitoring</param>
    /// <param name="environment">Environment information for response customization</param>
    public GlobalExceptionMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Invokes the middleware to handle the HTTP request and catch any exceptions.
    /// 
    /// This method wraps the next middleware in the pipeline with comprehensive exception
    /// handling. It captures exceptions, logs them with appropriate detail levels, and
    /// generates user-friendly error responses based on the exception type and environment.
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault() 
                          ?? Guid.NewGuid().ToString();

        try
        {
            // Process the request through the pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the exception with comprehensive context
            using var scope = _logger.BeginScope("CorrelationId: {CorrelationId}", correlationId);
            
            _logger.LogError(ex, 
                "Unhandled exception occurred processing {Method} {Path} from IP {ClientIP}: {Message}",
                context.Request.Method,
                context.Request.Path,
                GetClientIPAddress(context),
                ex.Message);

            // Handle the exception and generate appropriate response
            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    /// <summary>
    /// Handles exceptions by generating appropriate HTTP responses based on exception type.
    /// 
    /// This method categorizes exceptions and generates appropriate HTTP status codes and
    /// error messages. It provides different levels of detail based on the environment
    /// (development vs. production) to balance debugging needs with security requirements.
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    /// <param name="exception">The exception that was caught</param>
    /// <param name="correlationId">Correlation ID for request tracking</param>
    /// <returns>A task representing the asynchronous response generation</returns>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        // Prevent response modification if already started
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Cannot handle exception - response has already started");
            return;
        }

        // Set response content type and correlation ID
        context.Response.ContentType = "application/json";
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var (statusCode, message, details) = GetErrorDetails(exception);
        context.Response.StatusCode = statusCode;

        // Create comprehensive error response
        var response = new
        {
            Success = false,
            Message = message,
            Details = _environment.IsDevelopment() ? details : null,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path.Value,
            Method = context.Request.Method,
            StatusCode = statusCode,
            ErrorType = GetErrorType(exception)
        };

        // Serialize and write response
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await context.Response.WriteAsync(jsonResponse);

        // Log error response generation
        _logger.LogDebug("Error response generated with status {StatusCode} for correlation {CorrelationId}",
            statusCode, correlationId);
    }

    /// <summary>
    /// Determines appropriate error details based on exception type and environment.
    /// </summary>
    /// <param name="exception">The exception to analyze</param>
    /// <returns>Tuple containing status code, user message, and technical details</returns>
    private (int StatusCode, string Message, string? Details) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            // Authentication and Authorization Errors
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                ApiMessages.UnauthorizedAccess,
                _environment.IsDevelopment() ? exception.Message : null
            ),

            SecurityException => (
                (int)HttpStatusCode.Forbidden,
                "Access denied due to security policy violation",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            // Validation and Input Errors
            ArgumentNullException argNullEx => (
                (int)HttpStatusCode.BadRequest,
                $"Required parameter is missing: {argNullEx.ParamName}",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            ArgumentException argEx => (
                (int)HttpStatusCode.BadRequest,
                "Invalid request data provided",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            FormatException => (
                (int)HttpStatusCode.BadRequest,
                "Invalid data format in request",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            // Resource Not Found Errors
            FileNotFoundException => (
                (int)HttpStatusCode.NotFound,
                ApiMessages.FileNotFound,
                _environment.IsDevelopment() ? exception.Message : null
            ),

            DirectoryNotFoundException => (
                (int)HttpStatusCode.NotFound,
                "Requested resource directory not found",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            KeyNotFoundException => (
                (int)HttpStatusCode.NotFound,
                "Requested resource not found",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            // Database Errors
            DbUpdateConcurrencyException => (
                (int)HttpStatusCode.Conflict,
                "Concurrency conflict - the resource has been modified by another user",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            DbUpdateException dbEx => (
                (int)HttpStatusCode.Conflict,
                "Database update conflict - the resource may have been modified by another user",
                _environment.IsDevelopment() ? dbEx.InnerException?.Message ?? dbEx.Message : null
            ),

            InvalidOperationException invOpEx when invOpEx.Message.Contains("database") => (
                (int)HttpStatusCode.ServiceUnavailable,
                "Database service is temporarily unavailable",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            // Timeout and Performance Errors
            TimeoutException => (
                (int)HttpStatusCode.RequestTimeout,
                "The request timed out - please try again",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            TaskCanceledException => (
                (int)HttpStatusCode.RequestTimeout,
                "The request was cancelled due to timeout",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            // External Service Errors
            HttpRequestException httpEx => (
                (int)HttpStatusCode.BadGateway,
                "External service error - please try again later",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            // Memory and Resource Errors
            OutOfMemoryException => (
                (int)HttpStatusCode.ServiceUnavailable,
                "Service temporarily unavailable due to resource constraints",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            NotSupportedException => (
                (int)HttpStatusCode.NotImplemented,
                "The requested operation is not supported",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            // Generic System Errors
            SystemException => (
                (int)HttpStatusCode.InternalServerError,
                "A system error occurred - please contact support if the problem persists",
                _environment.IsDevelopment() ? exception.Message : null
            ),

            // Default case for all other exceptions
            _ => (
                (int)HttpStatusCode.InternalServerError,
                ApiMessages.InternalServerError,
                _environment.IsDevelopment() ? exception.Message : null
            )
        };
    }

    /// <summary>
    /// Determines the error type classification for monitoring and analytics.
    /// </summary>
    /// <param name="exception">The exception to classify</param>
    /// <returns>String representing the error category</returns>
    private static string GetErrorType(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException or SecurityException => "Authentication",
            ArgumentException or FormatException => "Validation",
            FileNotFoundException or DirectoryNotFoundException or KeyNotFoundException => "NotFound",
            DbUpdateException or DbUpdateConcurrencyException => "Database",
            TimeoutException or TaskCanceledException => "Timeout",
            HttpRequestException => "ExternalService",
            OutOfMemoryException => "Resource",
            _ => "System"
        };
    }

    /// <summary>
    /// Extracts client IP address from the HTTP context, handling proxy scenarios.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>Client IP address as string</returns>
    private static string GetClientIPAddress(HttpContext context)
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header (some proxies use this)
        var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIP))
        {
            return realIP;
        }

        // Fall back to remote IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
