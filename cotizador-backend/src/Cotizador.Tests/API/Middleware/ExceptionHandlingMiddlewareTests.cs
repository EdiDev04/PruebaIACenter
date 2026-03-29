using System.Text.Json;
using Cotizador.API.Middleware;
using Cotizador.Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cotizador.Tests.API.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _mockLogger = new();

    private ExceptionHandlingMiddleware BuildMiddleware(Exception exceptionToThrow)
    {
        RequestDelegate next = _ => throw exceptionToThrow;
        return new ExceptionHandlingMiddleware(next, _mockLogger.Object);
    }

    private static DefaultHttpContext BuildContext()
    {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_Should_Return404_WhenFolioNotFoundException()
    {
        // Arrange
        ExceptionHandlingMiddleware middleware = BuildMiddleware(new FolioNotFoundException("DAN-2026-00001"));
        DefaultHttpContext context = BuildContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        string body = await ReadBodyAsync(context);
        body.Should().Contain("folioNotFound");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return409_WhenVersionConflictException()
    {
        // Arrange
        ExceptionHandlingMiddleware middleware = BuildMiddleware(new VersionConflictException("DAN-2026-00001", 3));
        DefaultHttpContext context = BuildContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        string body = await ReadBodyAsync(context);
        body.Should().Contain("versionConflict");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return422_WhenInvalidQuoteStateException()
    {
        // Arrange
        ExceptionHandlingMiddleware middleware = BuildMiddleware(
            new InvalidQuoteStateException("DAN-2026-00001", "draft", "Cannot calculate with no locations"));
        DefaultHttpContext context = BuildContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        string body = await ReadBodyAsync(context);
        body.Should().Contain("invalidQuoteState");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return503_WhenCoreOhsUnavailableException()
    {
        // Arrange
        ExceptionHandlingMiddleware middleware = BuildMiddleware(new CoreOhsUnavailableException("Service down"));
        DefaultHttpContext context = BuildContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        string body = await ReadBodyAsync(context);
        body.Should().Contain("coreOhsUnavailable");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return500_WhenUnhandledException()
    {
        // Arrange
        ExceptionHandlingMiddleware middleware = BuildMiddleware(new InvalidOperationException("unexpected"));
        DefaultHttpContext context = BuildContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        string body = await ReadBodyAsync(context);
        body.Should().Contain("internal");
        body.Should().NotContain("unexpected"); // Must NOT expose internal message
    }

    [Fact]
    public async Task InvokeAsync_Should_Return400_WhenValidationException()
    {
        // Arrange
        ValidationFailure failure = new("InsuredData.Name", "Insured name is required");
        ValidationException validationException = new(new[] { failure });
        ExceptionHandlingMiddleware middleware = BuildMiddleware(validationException);
        DefaultHttpContext context = BuildContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        string body = await ReadBodyAsync(context);
        body.Should().Contain("validationError");
    }

    [Fact]
    public async Task InvokeAsync_Should_NotExposeStackTrace_WhenAnyException()
    {
        // Arrange
        ExceptionHandlingMiddleware middleware = BuildMiddleware(new Exception("Internal details"));
        DefaultHttpContext context = BuildContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        string body = await ReadBodyAsync(context);
        body.Should().NotContain("StackTrace");
        body.Should().NotContain("at Cotizador");
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
