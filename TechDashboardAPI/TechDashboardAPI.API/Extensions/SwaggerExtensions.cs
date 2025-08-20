using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace TechDashboardAPI.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
    {
        // Add API Explorer for endpoint discovery
        services.AddEndpointsApiExplorer();

        // Configure comprehensive Swagger generation
        services.AddSwaggerGen(options =>
        {
            // Define comprehensive API information and metadata
            options.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "Tech Dashboard API",
                Version = "v1.0.0",
                Description = @"
                    <h2>Comprehensive Technical Problem Management API</h2>
                    <p>The Tech Dashboard API provides a robust, scalable solution for managing technical problems, solutions, and organizational workflows within enterprise environments.</p>
                    
                    <h3>🚀 Key Features</h3>
                    <ul>
                        <li><strong>Problem Management:</strong> Submit, track, and resolve technical issues efficiently</li>
                        <li><strong>Solution Database:</strong> Comprehensive knowledge base with searchable solutions</li>
                        <li><strong>Department Organization:</strong> Multi-department support with role-based access</li>
                        <li><strong>Project Integration:</strong> Link problems and solutions to specific projects</li>
                        <li><strong>File Management:</strong> Secure file upload and attachment capabilities</li>
                        <li><strong>Dynamic Forms:</strong> Customizable problem submission forms</li>
                        <li><strong>User Management:</strong> Complete user lifecycle with role-based permissions</li>
                        <li><strong>Reporting & Analytics:</strong> Comprehensive insights and metrics</li>
                    </ul>
                    
                    <h3>🔐 Authentication & Security</h3>
                    <p>This API uses <strong>JWT Bearer Token</strong> authentication with role-based authorization:</p>
                    <ul>
                        <li><strong>SuperAdmin:</strong> Full system access and configuration</li>
                        <li><strong>Admin:</strong> Administrative operations and user management</li>
                        <li><strong>Contributor:</strong> Create and manage problems and solutions</li>
                        <li><strong>Viewer:</strong> Read-only access to authorized resources</li>
                    </ul>
                    
                    <h3>📋 Quick Start</h3>
                    <ol>
                        <li>Obtain a JWT token via the <code>/api/Auth/login</code> endpoint</li>
                        <li>Click the <strong>'Authorize'</strong> button below and enter: <code>Bearer {your-token}</code></li>
                        <li>Explore and test API endpoints using the interactive interface</li>
                        <li>Review response schemas and examples for integration guidance</li>
                    </ol>
                ",
                Contact = new OpenApiContact
                {
                    Name = "Tech Dashboard Development Team",
                    Email = "api-support@techdashboard.com",
                    Url = new Uri("https://github.com/techdashboard/api")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                },
                TermsOfService = new Uri("https://techdashboard.com/terms"),
            });

            // Configure JWT Bearer Authentication Scheme
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = @"
                    <h4>JWT Authorization Header</h4>
                    <p>Enter your JWT Bearer token in the format: <code>Bearer {your-jwt-token}</code></p>
                    
                    <h5>How to get a token:</h5>
                    <ol>
                        <li>Use the <strong>POST /api/Auth/login</strong> endpoint with valid credentials</li>
                        <li>Copy the <code>token</code> value from the response</li>
                        <li>Click the <strong>'Authorize'</strong> button above</li>
                        <li>Enter: <code>Bearer {paste-your-token-here}</code></li>
                        <li>Click <strong>'Authorize'</strong> to apply to all requests</li>
                    </ol>
                    
                    <h5>Example:</h5>
                    <code>Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</code>
                "
            });

            // Apply JWT Bearer authentication to all secured endpoints
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                }
            });

            // Configure enhanced schema generation
            options.UseAllOfToExtendReferenceSchemas();
            options.UseOneOfForPolymorphism();
            
            // Configure response examples and descriptions
            options.SupportNonNullableReferenceTypes();
            
            // Include XML documentation comments for comprehensive endpoint descriptions
            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
            foreach (var xmlFile in xmlFiles)
            {
                try
                {
                    options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
                }
                catch (Exception ex)
                {
                    // Log XML documentation loading issues but continue
                    Console.WriteLine($"Warning: Could not load XML documentation from {xmlFile}: {ex.Message}");
                }
            }

            // Configure operation grouping and naming
            options.TagActionsBy(api =>
            {
                if (api.GroupName != null)
                    return new[] { api.GroupName };

                if (api.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerActionDescriptor)
                {
                    return new[] { controllerActionDescriptor.ControllerName };
                }

                throw new InvalidOperationException("Unable to determine tag for endpoint.");
            });

            // Configure custom document filters for enhanced functionality
            options.DocumentFilter<SwaggerDocumentFilter>();
            options.OperationFilter<SwaggerOperationFilter>();
        });

        return services;
    }

    /// <summary>
    /// Configures and enables Swagger UI with enhanced user experience and professional presentation.
    /// 
    /// This method sets up the interactive Swagger UI interface with customized settings for optimal
    /// developer experience. It includes enhanced themes, configuration options, and professional
    /// presentation suitable for both internal development and external API consumers.
    /// 
    /// Features:
    /// - Interactive API testing interface with enhanced UX
    /// - Professional theme and branding
    /// - Deep linking support for sharing specific endpoints
    /// - Enhanced request/response display and formatting
    /// - Comprehensive error handling and validation feedback
    /// - Customizable UI configuration for different environments
    /// 
    /// Configuration includes:
    /// - Custom routing and endpoint configuration
    /// - Enhanced UI theme and styling
    /// - Deep linking and navigation features
    /// - Request/response formatting and validation
    /// - Professional branding and metadata display
    /// </summary>
    /// <param name="app">The application builder to configure</param>
    /// <returns>The configured application builder for method chaining</returns>
    public static IApplicationBuilder UseCustomSwagger(this IApplicationBuilder app)
    {
        // Enable Swagger JSON generation middleware
        app.UseSwagger(options =>
        {
            // Configure Swagger JSON generation options
            options.RouteTemplate = "swagger/{documentName}/swagger.json";
            options.PreSerializeFilters.Add((swagger, httpReq) =>
            {
                // Ensure proper server configuration for different environments
                var serverUrl = $"{httpReq.Scheme}://{httpReq.Host.Value}";
                swagger.Servers = new List<OpenApiServer>
                {
                    new OpenApiServer { Url = serverUrl, Description = "Current Environment" }
                };
            });
        });

        // Enable enhanced Swagger UI with professional configuration
        app.UseSwaggerUI(options =>
        {
            // Configure API endpoint and metadata
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tech Dashboard API v1.0");
            options.RoutePrefix = "swagger"; // Serve at /swagger
            
            // Professional UI configuration and branding
            options.DocumentTitle = "Tech Dashboard API - Interactive Documentation";
            options.HeadContent = @"
                <style>
                    .swagger-ui .topbar { background-color: #2c3e50; }
                    .swagger-ui .topbar .download-url-wrapper { display: none; }
                    .swagger-ui .info .title { color: #2c3e50; }
                </style>
                <link rel='icon' type='image/x-icon' href='/favicon.ico' />
            ";

            // Enhanced user experience configuration
            options.DefaultModelsExpandDepth(2); // Show model details by default
            options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
            options.DisplayRequestDuration(); // Show request timing
            options.EnableDeepLinking(); // Enable deep linking to operations
            options.EnableFilter(); // Enable operation filtering
            options.ShowExtensions(); // Show OpenAPI extensions
            options.ShowCommonExtensions(); // Show common extensions
            
            // Configure API interaction settings
            options.EnableValidator(); // Enable response validation
            options.SupportedSubmitMethods(
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Get,
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Post,
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Put,
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Delete,
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Patch
            );

            // Configure additional UI enhancements
            options.ConfigObject.AdditionalItems.Add("syntaxHighlight", new Dictionary<string, object>
            {
                ["activated"] = true,
                ["theme"] = "nord"
            });

            // Add custom CSS for enhanced presentation
            options.InjectStylesheet("/swagger-custom.css");
        });

        return app;
    }
}

/// <summary>
/// Custom document filter for enhanced Swagger documentation processing.
/// </summary>
public class SwaggerDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Add custom tags for better organization
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new OpenApiTag 
            { 
                Name = "Authentication", 
                Description = "User authentication and authorization endpoints"
            },
            new OpenApiTag 
            { 
                Name = "Problems", 
                Description = "Technical problem management endpoints"
            },
            new OpenApiTag 
            { 
                Name = "Solutions", 
                Description = "Solution management and knowledge base endpoints"
            },
            new OpenApiTag 
            { 
                Name = "Users", 
                Description = "User management and profile endpoints"
            },
            new OpenApiTag 
            { 
                Name = "Departments", 
                Description = "Department organization and management endpoints"
            },
            new OpenApiTag 
            { 
                Name = "Projects", 
                Description = "Project management and tracking endpoints"
            },
            new OpenApiTag 
            { 
                Name = "Forms", 
                Description = "Dynamic form management endpoints"
            },
            new OpenApiTag 
            { 
                Name = "Health", 
                Description = "System health and monitoring endpoints"
            }
        };

        // Remove unwanted endpoints or add global configurations
        foreach (var path in swaggerDoc.Paths.Values)
        {
            foreach (var operation in path.Operations.Values)
            {
                // Add common response types
                if (!operation.Responses.ContainsKey("401"))
                {
                    operation.Responses.Add("401", new OpenApiResponse 
                    { 
                        Description = "Unauthorized - Invalid or missing JWT token" 
                    });
                }
                
                if (!operation.Responses.ContainsKey("403"))
                {
                    operation.Responses.Add("403", new OpenApiResponse 
                    { 
                        Description = "Forbidden - Insufficient permissions for this operation" 
                    });
                }

                if (!operation.Responses.ContainsKey("500"))
                {
                    operation.Responses.Add("500", new OpenApiResponse 
                    { 
                        Description = "Internal Server Error - An unexpected error occurred" 
                    });
                }
            }
        }
    }
}

/// <summary>
/// Custom operation filter for enhanced endpoint documentation.
/// </summary>
public class SwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add common headers or parameters if needed
        operation.Parameters ??= new List<OpenApiParameter>();

        // Add correlation ID parameter for tracking (optional)
        if (context.MethodInfo.DeclaringType?.Name.EndsWith("Controller") == true)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Correlation-ID",
                In = ParameterLocation.Header,
                Required = false,
                Description = "Optional correlation ID for request tracking and debugging",
                Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
            });
        }
    }
}
