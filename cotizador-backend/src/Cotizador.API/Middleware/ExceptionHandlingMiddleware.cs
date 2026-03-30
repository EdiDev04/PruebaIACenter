using System.Text.Json;
using Cotizador.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cotizador.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (FolioNotFoundException ex)
        {
            _logger.LogWarning(ex, "Folio not found: {FolioNumber}", ex.FolioNumber);
            await WriteErrorResponseAsync(context, StatusCodes.Status404NotFound, "folioNotFound", ex.Message);
        }
        catch (VersionConflictException ex)
        {
            _logger.LogWarning(ex, "Version conflict on folio: {FolioNumber}", ex.FolioNumber);
            await WriteErrorResponseAsync(context, StatusCodes.Status409Conflict, "versionConflict", "El folio fue modificado por otro proceso. Recargue para continuar");
        }
        catch (InvalidQuoteStateException ex)
        {
            _logger.LogWarning(ex, "Invalid quote state for folio: {FolioNumber}", ex.FolioNumber);
            await WriteErrorResponseAsync(context, StatusCodes.Status422UnprocessableEntity, "invalidQuoteState", ex.Message);
        }
        catch (CoreOhsUnavailableException ex)
        {
            _logger.LogError(ex, "Core OHS unavailable");
            // Do not expose internal path details — use generic message
            await WriteErrorResponseAsync(context, StatusCodes.Status503ServiceUnavailable, "coreOhsUnavailable", "Servicio de catálogos no disponible, intente más tarde.");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error");
            string field = ex.Errors.FirstOrDefault()?.PropertyName ?? string.Empty;
            string message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? ex.Message;
            await WriteErrorResponseAsync(context, StatusCodes.Status400BadRequest, "validationError", message, field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponseAsync(context, StatusCodes.Status500InternalServerError, "internal", "Internal server error");
        }
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext context,
        int statusCode,
        string type,
        string message,
        string? field = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        object body = field is not null
            ? new { type, message, field = (string?)field }
            : new { type, message, field = (string?)null };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
