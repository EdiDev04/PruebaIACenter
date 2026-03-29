using Cotizador.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Cotizador.Tests.Domain.Exceptions;

public class VersionConflictExceptionTests
{
    [Fact]
    public void VersionConflictException_Should_SetFolioAndVersion_WhenCreated()
    {
        // Arrange
        const string folioNumber = "DAN-2026-00001";
        const int expectedVersion = 5;

        // Act
        VersionConflictException exception = new(folioNumber, expectedVersion);

        // Assert
        exception.FolioNumber.Should().Be(folioNumber);
        exception.ExpectedVersion.Should().Be(expectedVersion);
    }

    [Fact]
    public void VersionConflictException_Should_IncludeVersionInMessage_WhenCreated()
    {
        // Arrange
        const string folioNumber = "DAN-2026-00001";
        const int expectedVersion = 5;

        // Act
        VersionConflictException exception = new(folioNumber, expectedVersion);

        // Assert
        exception.Message.Should().Contain(folioNumber);
        exception.Message.Should().Contain(expectedVersion.ToString());
    }
}
