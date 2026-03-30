using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Settings;
using Cotizador.Application.UseCases;
using Cotizador.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cotizador.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // BusinessTypes configuration
        services.Configure<BusinessTypeSettings>(
            configuration.GetSection(BusinessTypeSettings.SectionName));

        // Use Cases — SPEC-003
        services.AddScoped<ICreateFolioUseCase, CreateFolioUseCase>();
        services.AddScoped<IGetQuoteSummaryUseCase, GetQuoteSummaryUseCase>();

        // Use Cases — SPEC-004
        services.AddScoped<IGetGeneralInfoUseCase, GetGeneralInfoUseCase>();
        services.AddScoped<IUpdateGeneralInfoUseCase, UpdateGeneralInfoUseCase>();

        // Use Cases — SPEC-005
        services.AddScoped<IGetLayoutUseCase, GetLayoutUseCase>();
        services.AddScoped<IUpdateLayoutUseCase, UpdateLayoutUseCase>();

        // Use Cases — SPEC-006
        services.AddScoped<IGetLocationsUseCase, GetLocationsUseCase>();
        services.AddScoped<IUpdateLocationsUseCase, UpdateLocationsUseCase>();
        services.AddScoped<IPatchLocationUseCase, PatchLocationUseCase>();
        services.AddScoped<IGetLocationsSummaryUseCase, GetLocationsSummaryUseCase>();
        services.AddScoped<IGetZipCodeUseCase, GetZipCodeUseCase>();
        services.AddScoped<IGetBusinessLinesUseCase, GetBusinessLinesUseCase>();

        // Use Cases — Proxy catálogos (SPEC-004)
        services.AddScoped<IGetSubscribersUseCase, GetSubscribersUseCase>();
        services.AddScoped<IGetAgentByCodeUseCase, GetAgentByCodeUseCase>();
        services.AddScoped<IGetRiskClassificationsUseCase, GetRiskClassificationsUseCase>();

        // FluentValidation
        services.AddScoped<IValidator<UpdateGeneralInfoRequest>, UpdateGeneralInfoRequestValidator>();
        services.AddScoped<IValidator<UpdateLayoutRequest>, UpdateLayoutRequestValidator>();
        services.AddScoped<IValidator<UpdateLocationsRequest>, UpdateLocationsRequestValidator>();
        services.AddScoped<IValidator<PatchLocationRequest>, PatchLocationRequestValidator>();

        return services;
    }
}
