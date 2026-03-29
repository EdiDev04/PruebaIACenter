using Cotizador.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Cotizador.Tests.Domain.Exceptions;

public class FolioNotFoundExceptionTests
{
    [Fact]
    public void FolioNotFoundException_Should_SetFolioNumber_WhenCreated()
    {
        // Arrange
        const string folioNumber = "DAN-2026-00001";

        // Act
        FolioNotFoundException exception = new(folioNumber);

        // Assert
        exception.FolioNumber.Should().Be(folioNumber);
    }

    [Fact]
    public void FolioNotFoundException_Should_IncludeFolioInMessage_WhenCreated()
    {
        // Arrange
        const string folioNumber = "DAN-2026-00001";

        // Act
        FolioNotFoundException exception = new(folioNumber);

        // Assert
        exception.Message.Should().Contain(folioNumber);
    }
}
