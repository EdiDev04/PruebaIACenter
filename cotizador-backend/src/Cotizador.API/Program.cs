using Cotizador.API.Auth;
using Cotizador.API.Middleware;
using Cotizador.Application;
using Cotizador.Infrastructure;
using Serilog;

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
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Expose for integration tests
public partial class Program { }
