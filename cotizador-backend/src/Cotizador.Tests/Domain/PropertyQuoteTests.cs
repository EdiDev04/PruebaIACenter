using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;
using Cotizador.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cotizador.Tests.Domain;

public class PropertyQuoteTests
{
    [Fact]
    public void PropertyQuote_Should_HaveDefaultDraftStatus_WhenCreated()
    {
        // Arrange & Act
        PropertyQuote quote = new();

        // Assert
        quote.QuoteStatus.Should().Be(QuoteStatus.Draft);
    }

    [Fact]
    public void PropertyQuote_Should_HaveDefaultVersionOne_WhenCreated()
    {
        // Arrange & Act
        PropertyQuote quote = new();

        // Assert
        quote.Version.Should().Be(1);
    }

    [Fact]
    public void PropertyQuote_Should_HaveEmptyLocationsList_WhenCreated()
    {
        // Arrange & Act
        PropertyQuote quote = new();

        // Assert
        quote.Locations.Should().NotBeNull();
        quote.Locations.Should().BeEmpty();
    }

    [Fact]
    public void PropertyQuote_Should_HaveZeroPremiums_WhenCreated()
    {
        // Arrange & Act
        PropertyQuote quote = new();

        // Assert
        quote.NetPremium.Should().Be(0);
        quote.CommercialPremium.Should().Be(0);
        quote.PremiumsByLocation.Should().BeEmpty();
    }

    [Fact]
    public void PropertyQuote_Should_AcceptLocations_WhenAssigned()
    {
        // Arrange
        PropertyQuote quote = new();
        List<Location> locations = new()
        {
            new Location { Index = 1, LocationName = "Main Office", ZipCode = "06600" }
        };

        // Act
        quote.Locations = locations;

        // Assert
        quote.Locations.Should().HaveCount(1);
        quote.Locations[0].LocationName.Should().Be("Main Office");
    }

    [Theory]
    [InlineData(QuoteStatus.Draft)]
    [InlineData(QuoteStatus.InProgress)]
    [InlineData(QuoteStatus.Calculated)]
    [InlineData(QuoteStatus.Finalized)]
    public void PropertyQuote_Should_AcceptValidStatus_WhenSet(string status)
    {
        // Arrange
        PropertyQuote quote = new();

        // Act
        quote.QuoteStatus = status;

        // Assert
        quote.QuoteStatus.Should().Be(status);
    }
}
