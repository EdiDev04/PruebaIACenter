using Cotizador.API.Auth;
using Cotizador.API.Middleware;
using Cotizador.Application;
using Cotizador.Infrastructure;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

// Authentication — Basic Auth
builder.Services.AddAuthentication("BasicAuth")
    .AddScheme<BasicAuthSchemeOptions, BasicAuthHandler>("BasicAuth", _ => { });
builder.Services.AddAuthorization();

// Infrastructure (MongoDB + CoreOhsClient)
builder.Services.AddInfrastructure(builder.Configuration);

// Application (Use Cases + Validators + Settings)
builder.Services.AddApplication(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Cotizador de Daños API",
        Version     = "v1",
        Description = "API REST del Cotizador de Seguros de Daños a la Propiedad. " +
                      "Gestiona folios, datos generales, ubicaciones de riesgo, opciones de cobertura y cálculo de primas.",
        Contact     = new OpenApiContact
        {
            Name  = "Equipo Cotizador",
            Email = "cotizador@empresa.com",
        },
    });

    // Basic Auth — todas las rutas excepto /health requieren credenciales
    var basicAuthScheme = new OpenApiSecurityScheme
    {
        Type        = SecuritySchemeType.Http,
        Scheme      = "basic",
        Description = "Autenticación HTTP Basic. Use las credenciales configuradas en appsettings.",
        Reference   = new OpenApiReference
        {
            Id   = "BasicAuth",
            Type = ReferenceType.SecurityScheme,
        },
    };
    options.AddSecurityDefinition("BasicAuth", basicAuthScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { basicAuthScheme, Array.Empty<string>() },
    });

    // Incluir comentarios XML de los controllers
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    // Agrupar por tag
    options.TagActionsBy(api =>
    {
        if (api.GroupName is not null) return new[] { api.GroupName };
        if (api.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor cad)
            return new[] { cad.ControllerName };
        return new[] { "General" };
    });
    options.DocInclusionPredicate((_, _) => true);
});

// CORS — permissive in Development, restrictive in Production
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    }
    else
    {
        string[] allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    }
});

WebApplication app = builder.Build();

// Middleware pipeline (ORDER IS CRITICAL)
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger UI — solo disponible en Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "api-docs/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/api-docs/v1/swagger.json", "Cotizador de Daños API v1");
        c.RoutePrefix          = "api-docs";
        c.DocumentTitle        = "Cotizador de Daños — API Docs";
        c.DefaultModelsExpandDepth(-1);          // oculta sección Models por defecto
        c.DisplayRequestDuration();
        c.EnableFilter();
    });
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint — used by E2E / load balancers (no auth required)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .AllowAnonymous()
   .WithTags("Health")
   .ExcludeFromDescription();

app.Run();

// Expose for integration tests
public partial class Program { }
