using Cotizador.Application.DTOs;
using Cotizador.Application.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace Cotizador.Tests.Application.Validators;

public class UpdateLayoutRequestValidatorTests
{
    private UpdateLayoutRequestValidator Sut => new();

    // ─── Happy Paths ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public void Validate_Should_PassValidation_WhenDisplayModeIsGridAndColumnsProvided()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "grid",
            VisibleColumns: new List<string> { "index", "locationName", "zipCode" },
            Version: 1
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Validate_Should_PassValidation_WhenDisplayModeIsList()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "list",
            VisibleColumns: new List<string> { "index", "locationName" },
            Version: 2
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_PassValidation_WhenAllValidColumnsProvided()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "grid",
            VisibleColumns: new List<string>
            {
                "index", "locationName", "address", "zipCode", "state", "municipality",
                "neighborhood", "city", "constructionType", "level", "constructionYear",
                "businessLine", "guarantees", "catZone", "validationStatus"
            },
            Version: 5
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // ─── Error Paths — DisplayMode ─────────────────────────────────────────────

    [Theory]
    [InlineData("table")]
    [InlineData("card")]
    [InlineData("GRID")]
    [InlineData("LIST")]
    [InlineData("")]
    public void Validate_Should_FailValidation_WhenDisplayModeIsInvalid(string invalidMode)
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: invalidMode,
            VisibleColumns: new List<string> { "index", "locationName" },
            Version: 1
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_ReportErrorOnDisplayModeField_WhenDisplayModeIsInvalid()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "table",
            VisibleColumns: new List<string> { "index", "locationName" },
            Version: 1
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayMode");
    }

    [Fact]
    public void Validate_Should_ReturnSpanishErrorMessage_WhenDisplayModeIsInvalid()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "table",
            VisibleColumns: new List<string> { "index", "locationName" },
            Version: 1
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.Errors.Should().Contain(e =>
            e.PropertyName == "DisplayMode" &&
            e.ErrorMessage.Contains("grid") &&
            e.ErrorMessage.Contains("list"));
    }

    // ─── Error Paths — VisibleColumns ──────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public void Validate_Should_FailValidation_WhenVisibleColumnsIsEmpty()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "grid",
            VisibleColumns: new List<string>(),
            Version: 1
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_ReportErrorOnVisibleColumnsField_WhenVisibleColumnsIsEmpty()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "grid",
            VisibleColumns: new List<string>(),
            Version: 1
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == "VisibleColumns");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Validate_Should_FailValidation_WhenVisibleColumnsContainsInvalidColumn()
    {
        // Arrange
        const string invalidColumn = "nonExistentColumn";
        var request = new UpdateLayoutRequest(
            DisplayMode: "grid",
            VisibleColumns: new List<string> { "index", invalidColumn },
            Version: 1
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_MentionInvalidColumnInErrorMessage_WhenVisibleColumnsContainsInvalidColumn()
    {
        // Arrange
        const string invalidColumn = "nonExistentColumn";
        var request = new UpdateLayoutRequest(
            DisplayMode: "grid",
            VisibleColumns: new List<string> { "index", invalidColumn },
            Version: 1
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains(invalidColumn));
    }

    [Fact]
    public void Validate_Should_FailValidation_WhenVisibleColumnsContainsColumnWithWrongCase()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "grid",
            VisibleColumns: new List<string> { "Index", "LocationName" }, // PascalCase instead of camelCase
            Version: 1
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    // ─── Error Paths — Version ─────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public void Validate_Should_FailValidation_WhenVersionIsZero()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "grid",
            VisibleColumns: new List<string> { "index", "locationName" },
            Version: 0
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_FailValidation_WhenVersionIsNegative()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "grid",
            VisibleColumns: new List<string> { "index", "locationName" },
            Version: -1
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_ReportErrorOnVersionField_WhenVersionIsZeroOrNegative()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "grid",
            VisibleColumns: new List<string> { "index", "locationName" },
            Version: 0
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == "Version");
    }

    [Fact]
    public void Validate_Should_PassValidation_WhenVersionIsOne()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "grid",
            VisibleColumns: new List<string> { "index", "locationName" },
            Version: 1
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // ─── Edge Cases ────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_Should_ReportMultipleErrors_WhenMultipleFieldsAreInvalid()
    {
        // Arrange
        var request = new UpdateLayoutRequest(
            DisplayMode: "invalid",
            VisibleColumns: new List<string>(),
            Version: 0
        );

        // Act
        ValidationResult result = Sut.Validate(request);

        // Assert
        result.Errors.Should().HaveCountGreaterThan(1);
    }
}
