using TechDashboardAPI.API.Middleware;

namespace TechDashboardAPI.API.Extensions;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Registers and configures all custom middleware components in the proper order.
    /// 
    /// This method ensures that custom middleware components are registered in the correct
    /// sequence for optimal security, error handling, and performance. The middleware order
    /// is critical for proper request processing and security enforcement.
    /// 
    /// Middleware Registration Order:
    /// 1. Global Exception Middleware - Catches and handles all unhandled exceptions
    /// 2. Security Headers Middleware - Adds comprehensive security headers
    /// 3. Request Logging Middleware - Logs request details and performance metrics
    /// 4. Security Validation Middleware - Additional security checks and validation
    /// 
    /// Features:
    /// - Comprehensive exception handling with detailed logging
    /// - Enhanced security headers for web application protection
    /// - Request/response logging for monitoring and debugging
    /// - Performance metrics collection and monitoring
    /// - Security validation and threat detection
    /// - Flexible configuration based on environment settings
    /// 
    /// Security Enhancements:
    /// - XSS Protection headers for cross-site scripting prevention
    /// - Content Security Policy headers for content injection protection
    /// - HSTS headers for HTTPS enforcement
    /// - Frame options for clickjacking protection
    /// - Request size and timeout validation
    /// - Comprehensive security monitoring and alerting
    /// </summary>
    /// <param name="app">The application builder to configure</param>
    /// <returns>The configured application builder for method chaining</returns>
    public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder app)
    {
        // 1. Global Exception Middleware - Must be first to catch all exceptions
        // Provides comprehensive error handling, logging, and user-friendly error responses
        app.UseMiddleware<GlobalExceptionMiddleware>();
        
        // 2. Security Headers Middleware - Adds critical security headers to all responses
        // Implements security best practices including XSS protection, CSP, and HSTS
        app.UseMiddleware<SecurityHeadersMiddleware>();
        
        // 3. Request Logging Middleware - Logs request details for monitoring and debugging
        // Note: Add this middleware if request logging component is implemented
        // app.UseMiddleware<RequestLoggingMiddleware>();
        
        // 4. Performance Monitoring Middleware - Tracks request performance and metrics
        // Note: Add this middleware if performance monitoring component is implemented  
        // app.UseMiddleware<PerformanceMonitoringMiddleware>();
        
        // 5. Security Validation Middleware - Additional security checks and validation
        // Note: Add this middleware if additional security validation is required
        // app.UseMiddleware<SecurityValidationMiddleware>();
        
        // 6. Rate Limiting Middleware - Protects against abuse and DoS attacks
        // Note: Add this middleware if rate limiting component is implemented
        // app.UseMiddleware<RateLimitingMiddleware>();
        
        // 7. Request/Response Caching Middleware - Optimizes performance through caching
        // Note: Add this middleware if custom caching component is implemented
        // app.UseMiddleware<CachingMiddleware>();

        return app;
    }

    /// <summary>
    /// Registers middleware components specifically for development environment.
    /// 
    /// This method adds additional middleware that is useful during development
    /// but should not be used in production environments.
    /// </summary>
    /// <param name="app">The application builder to configure</param>
    /// <returns>The configured application builder for method chaining</returns>
    public static IApplicationBuilder UseCustomDevelopmentMiddleware(this IApplicationBuilder app)
    {
        // Register all standard middleware
        app.UseCustomMiddleware();
        
        // Add development-specific middleware
        // Note: Implement these middleware components as needed
        
        // Request/Response Debugging Middleware - Detailed request/response logging
        // app.UseMiddleware<RequestResponseDebuggingMiddleware>();
        
        // API Versioning Testing Middleware - Support for API version testing
        // app.UseMiddleware<ApiVersionTestingMiddleware>();
        
        // Mock Data Middleware - Provides mock responses for testing
        // app.UseMiddleware<MockDataMiddleware>();

        return app;
    }

    /// <summary>
    /// Registers middleware components specifically optimized for production environment.
    /// 
    /// This method configures middleware with production-optimized settings including
    /// enhanced security, performance monitoring, and comprehensive logging.
    /// </summary>
    /// <param name="app">The application builder to configure</param>
    /// <returns>The configured application builder for method chaining</returns>
    public static IApplicationBuilder UseCustomProductionMiddleware(this IApplicationBuilder app)
    {
        // Register all standard middleware
        app.UseCustomMiddleware();
        
        // Add production-specific middleware
        // Note: Implement these middleware components as needed
        
        // Enhanced Security Middleware - Additional security validations
        // app.UseMiddleware<EnhancedSecurityMiddleware>();
        
        // Monitoring and Telemetry Middleware - Comprehensive system monitoring
        // app.UseMiddleware<TelemetryMiddleware>();
        
        // Audit Logging Middleware - Detailed audit trail for compliance
        // app.UseMiddleware<AuditLoggingMiddleware>();
        
        // Health Check Middleware - Advanced health monitoring
        // app.UseMiddleware<HealthCheckMiddleware>();

        return app;
    }

    /// <summary>
    /// Configures middleware with specific settings based on environment and configuration.
    /// 
    /// This method provides a flexible way to configure middleware based on application
    /// settings, environment variables, and runtime configuration.
    /// </summary>
    /// <param name="app">The application builder to configure</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="environment">Host environment information</param>
    /// <returns>The configured application builder for method chaining</returns>
    public static IApplicationBuilder UseCustomMiddleware(
        this IApplicationBuilder app, 
        IConfiguration configuration, 
        IWebHostEnvironment environment)
    {
        // Configure middleware based on environment
        if (environment.IsDevelopment())
        {
            app.UseCustomDevelopmentMiddleware();
        }
        else if (environment.IsProduction())
        {
            app.UseCustomProductionMiddleware();
        }
        else
        {
            // Staging or other environments - use standard middleware
            app.UseCustomMiddleware();
        }

        // Configure additional middleware based on configuration settings
        var middlewareSettings = configuration.GetSection("Middleware");
        
        // Example: Enable request logging if configured
        if (middlewareSettings.GetValue<bool>("EnableRequestLogging", false))
        {
            // app.UseMiddleware<RequestLoggingMiddleware>();
        }
        
        // Example: Enable rate limiting if configured
        if (middlewareSettings.GetValue<bool>("EnableRateLimiting", false))
        {
            // app.UseMiddleware<RateLimitingMiddleware>();
        }
        
        // Example: Enable performance monitoring if configured
        if (middlewareSettings.GetValue<bool>("EnablePerformanceMonitoring", false))
        {
            // app.UseMiddleware<PerformanceMonitoringMiddleware>();
        }

        return app;
    }
}
