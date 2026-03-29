using Cotizador.API.Auth;
using Cotizador.API.Middleware;
using Cotizador.Application.Interfaces;
using Cotizador.Application.DTOs;
using Cotizador.Application.Settings;
using Cotizador.Application.UseCases;
using Cotizador.Application.Validators;
using Cotizador.Infrastructure;
using FluentValidation;
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

// BusinessTypes configuration
builder.Services.Configure<BusinessTypeSettings>(
    builder.Configuration.GetSection(BusinessTypeSettings.SectionName));

// Use Cases — SPEC-003
builder.Services.AddScoped<ICreateFolioUseCase, CreateFolioUseCase>();
builder.Services.AddScoped<IGetQuoteSummaryUseCase, GetQuoteSummaryUseCase>();

// Use Cases — SPEC-004
builder.Services.AddScoped<IGetGeneralInfoUseCase, GetGeneralInfoUseCase>();
builder.Services.AddScoped<IUpdateGeneralInfoUseCase, UpdateGeneralInfoUseCase>();

// Use Cases — Proxy catálogos (SPEC-004)
builder.Services.AddScoped<IGetSubscribersUseCase, GetSubscribersUseCase>();
builder.Services.AddScoped<IGetAgentByCodeUseCase, GetAgentByCodeUseCase>();
builder.Services.AddScoped<IGetRiskClassificationsUseCase, GetRiskClassificationsUseCase>();

// FluentValidation
builder.Services.AddScoped<IValidator<UpdateGeneralInfoRequest>, UpdateGeneralInfoRequestValidator>();

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
