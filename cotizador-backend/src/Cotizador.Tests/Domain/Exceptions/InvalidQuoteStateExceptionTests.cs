using Cotizador.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Cotizador.Tests.Domain.Exceptions;

public class InvalidQuoteStateExceptionTests
{
    [Fact]
    public void InvalidQuoteStateException_Should_SetAllProperties_WhenCreated()
    {
        // Arrange
        const string folioNumber = "DAN-2026-00001";
        const string currentState = "draft";
        const string message = "Cannot calculate with no locations";

        // Act
        InvalidQuoteStateException exception = new(folioNumber, currentState, message);

        // Assert
        exception.FolioNumber.Should().Be(folioNumber);
        exception.CurrentState.Should().Be(currentState);
        exception.Message.Should().Be(message);
    }
}
