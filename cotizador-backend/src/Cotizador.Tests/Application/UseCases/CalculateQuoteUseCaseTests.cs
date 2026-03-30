using Cotizador.Application.DTOs;
using Cotizador.Application.Ports;
using Cotizador.Application.UseCases;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Cotizador.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cotizador.Tests.Application.UseCases;

public class CalculateQuoteUseCaseTests
{
    private readonly Mock<IQuoteRepository> _mockRepository = new();
    private readonly Mock<ICoreOhsClient> _mockCoreOhsClient = new();
    private readonly Mock<ILogger<CalculateQuoteUseCase>> _mockLogger = new();

    private CalculateQuoteUseCase Sut => new(
        _mockRepository.Object,
        _mockCoreOhsClient.Object,
        _mockLogger.Object);

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static PropertyQuote BuildQuoteWith2CalculableAnd1Incomplete(int version = 5)
    {
        return new PropertyQuote
        {
            FolioNumber = "DAN-2026-00001",
            Version = version,
            QuoteStatus = QuoteStatus.Draft,
            Locations = new List<Location>
            {
                new()
                {
                    Index = 1,
                    LocationName = "Bodega Central",
                    ZipCode = "06600",
                    BusinessLine = new BusinessLine { FireKey = "B-03" },
                    CatZone = "A",
                    ValidationStatus = ValidationStatus.Calculable,
                    Guarantees = new List<LocationGuarantee>
                    {
                        new() { GuaranteeKey = GuaranteeKeys.BuildingFire, InsuredAmount = 5_000_000m },
                        new() { GuaranteeKey = GuaranteeKeys.CatTev, InsuredAmount = 5_000_000m },
                    }
                },
                new()
                {
                    Index = 2,
                    LocationName = "Sucursal Norte",
                    ZipCode = "64000",
                    BusinessLine = new BusinessLine { FireKey = "B-03" },
                    CatZone = "B",
                    ValidationStatus = ValidationStatus.Calculable,
                    Guarantees = new List<LocationGuarantee>
                    {
                        new() { GuaranteeKey = GuaranteeKeys.BuildingFire, InsuredAmount = 3_000_000m },
                    }
                },
                new()
                {
                    Index = 3,
                    LocationName = "Local sin datos",
                    ZipCode = string.Empty,
                    ValidationStatus = ValidationStatus.Incomplete,
                    Guarantees = new List<LocationGuarantee>()
                }
            }
        };
    }

    private void SetupDefaultTariffs()
    {
        _mockCoreOhsClient
            .Setup(x => x.GetFireTariffsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FireTariffDto>
            {
                new("B-03", 0.00125m, "Bodega")
            });

        _mockCoreOhsClient
            .Setup(x => x.GetCatTariffsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CatTariffDto>
            {
                new("A", 0.0035m, 0.0028m),
                new("B", 0.0015m, 0.0012m)
            });

        _mockCoreOhsClient
            .Setup(x => x.GetElectronicEquipmentFactorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ElectronicEquipmentFactorDto>
            {
                new("A", 2, 0.0052m)
            });

        _mockCoreOhsClient
            .Setup(x => x.GetCalculationParametersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CalculationParametersDto(0.05m, 0.10m, 0.03m, 0.16m, 0.02m, "2026-01-01"));

        _mockCoreOhsClient
            .Setup(x => x.GetZipCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ZipCodeDto("06600", "CDMX", "Cuauhtémoc", "Centro", "CDMX", "A", 2));

        _mockRepository
            .Setup(x => x.UpdateFinancialResultAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<List<LocationPremium>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ─── Happy Paths ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_Should_ReturnCalculateResultResponse_WhenTwoCalculableAndOneIncomplete()
    {
        // Arrange
        PropertyQuote quote = BuildQuoteWith2CalculableAnd1Incomplete(version: 5);
        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);
        SetupDefaultTariffs();

        // Act
        CalculateResultResponse result = await Sut.ExecuteAsync("DAN-2026-00001", new CalculateRequest(5));

        // Assert
        result.Should().NotBeNull();
        result.QuoteStatus.Should().Be(QuoteStatus.Calculated);
        result.Version.Should().Be(6);
        result.PremiumsByLocation.Should().HaveCount(3);

        LocationPremiumDto incomplete = result.PremiumsByLocation.First(p => p.LocationIndex == 3);
        incomplete.NetPremium.Should().Be(0m);
        incomplete.ValidationStatus.Should().Be(ValidationStatus.Incomplete);

        result.PremiumsByLocation.Where(p => p.LocationIndex != 3)
            .Should().AllSatisfy(p =>
            {
                p.ValidationStatus.Should().Be(ValidationStatus.Calculable);
                p.NetPremium.Should().BeGreaterThan(0m);
            });

        decimal expectedNet = result.PremiumsByLocation
            .Where(p => p.ValidationStatus == ValidationStatus.Calculable)
            .Sum(p => p.NetPremium);
        result.NetPremium.Should().Be(expectedNet);

        _mockRepository.Verify(x => x.UpdateFinancialResultAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
            It.IsAny<List<LocationPremium>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_Should_SetIncompleteLocation_WithZeroNetPremium()
    {
        // Arrange
        PropertyQuote quote = BuildQuoteWith2CalculableAnd1Incomplete(version: 5);
        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);
        SetupDefaultTariffs();

        // Act
        CalculateResultResponse result = await Sut.ExecuteAsync("DAN-2026-00001", new CalculateRequest(5));

        // Assert
        LocationPremiumDto incompleteDto = result.PremiumsByLocation.Single(p => p.LocationIndex == 3);
        incompleteDto.NetPremium.Should().Be(0m);
        incompleteDto.CoveragePremiums.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_Should_CalculateCommercialPremium_Correctly()
    {
        // Arrange
        // With params: expedition=0.05, commission=0.10, issuingRights=0.03, surcharges=0.02, iva=0.16
        // loadingFactor = 1.20; beforeTax = netPremium × 1.20; withTax = beforeTax × 1.16
        PropertyQuote quote = BuildQuoteWith2CalculableAnd1Incomplete(version: 5);
        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);
        SetupDefaultTariffs();

        // Act
        CalculateResultResponse result = await Sut.ExecuteAsync("DAN-2026-00001", new CalculateRequest(5));

        // Assert
        decimal expectedBeforeTax = Math.Round(result.NetPremium * 1.20m, 2);
        decimal expectedWithTax = Math.Round(expectedBeforeTax * 1.16m, 2);

        result.CommercialPremiumBeforeTax.Should().Be(expectedBeforeTax);
        result.CommercialPremium.Should().Be(expectedWithTax);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_Should_NotModifyNonFinancialFields_WhenPersisting()
    {
        // Arrange
        PropertyQuote quote = BuildQuoteWith2CalculableAnd1Incomplete(version: 5);
        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);
        SetupDefaultTariffs();

        // Act
        await Sut.ExecuteAsync("DAN-2026-00001", new CalculateRequest(5));

        // Assert — UpdateFinancialResultAsync called exactly once with correct version
        _mockRepository.Verify(x => x.UpdateFinancialResultAsync(
            "DAN-2026-00001",
            5,
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<List<LocationPremium>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_Should_UseDefaultEquipmentClass_ForElectronicEquipment()
    {
        // Arrange
        var quote = new PropertyQuote
        {
            FolioNumber = "DAN-2026-00002",
            Version = 1,
            QuoteStatus = QuoteStatus.Draft,
            Locations = new List<Location>
            {
                new()
                {
                    Index = 1,
                    LocationName = "Oficina TI",
                    ZipCode = "06600",
                    BusinessLine = new BusinessLine { FireKey = "B-03" },
                    CatZone = "A",
                    ValidationStatus = ValidationStatus.Calculable,
                    Guarantees = new List<LocationGuarantee>
                    {
                        new() { GuaranteeKey = GuaranteeKeys.ElectronicEquipment, InsuredAmount = 500_000m },
                    }
                }
            }
        };

        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-00002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        _mockCoreOhsClient
            .Setup(x => x.GetFireTariffsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FireTariffDto> { new("B-03", 0.00125m, "Bodega") });

        _mockCoreOhsClient
            .Setup(x => x.GetCatTariffsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CatTariffDto> { new("A", 0.0035m, 0.0028m) });

        _mockCoreOhsClient
            .Setup(x => x.GetElectronicEquipmentFactorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ElectronicEquipmentFactorDto>
            {
                new(SimplifiedTariffRates.DefaultEquipmentClass, 2, 0.0052m) // EquipmentClass = "A"
            });

        _mockCoreOhsClient
            .Setup(x => x.GetCalculationParametersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CalculationParametersDto(0.05m, 0.10m, 0.03m, 0.16m, 0.02m, "2026-01-01"));

        _mockCoreOhsClient
            .Setup(x => x.GetZipCodeAsync("06600", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ZipCodeDto("06600", "CDMX", "Cuauhtémoc", "Centro", "CDMX", "A", 2));

        _mockRepository
            .Setup(x => x.UpdateFinancialResultAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<List<LocationPremium>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CalculateResultResponse result = await Sut.ExecuteAsync("DAN-2026-00002", new CalculateRequest(1));

        // Assert — factor lookup used DefaultEquipmentClass "A" with techLevel 2 → rate 0.0052
        // premium = 500_000 × 0.0052 = 2_600
        LocationPremiumDto locationDto = result.PremiumsByLocation.Single();
        CoveragePremiumDto equipCoverage = locationDto.CoveragePremiums
            .Single(c => c.GuaranteeKey == GuaranteeKeys.ElectronicEquipment);
        equipCoverage.Premium.Should().Be(2_600m);
    }

    // ─── Error Paths ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_Should_ThrowFolioNotFoundException_WhenFolioDoesNotExist()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-99999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyQuote?)null);

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync("DAN-2026-99999", new CalculateRequest(1));

        // Assert
        await act.Should().ThrowAsync<FolioNotFoundException>();
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_Should_ThrowVersionConflictException_WhenVersionMismatch()
    {
        // Arrange
        PropertyQuote quote = BuildQuoteWith2CalculableAnd1Incomplete(version: 5);
        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act — request sends version 3 but quote has version 5
        Func<Task> act = async () => await Sut.ExecuteAsync("DAN-2026-00001", new CalculateRequest(3));

        // Assert
        await act.Should().ThrowAsync<VersionConflictException>();
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_Should_ThrowInvalidQuoteStateException_WhenNoCalculableLocations()
    {
        // Arrange
        var quote = new PropertyQuote
        {
            FolioNumber = "DAN-2026-00001",
            Version = 5,
            QuoteStatus = QuoteStatus.Draft,
            Locations = new List<Location>
            {
                new()
                {
                    Index = 1,
                    LocationName = "Sin datos",
                    ZipCode = string.Empty,
                    ValidationStatus = ValidationStatus.Incomplete,
                    Guarantees = new List<LocationGuarantee>()
                },
                new()
                {
                    Index = 2,
                    LocationName = "Sin datos 2",
                    ZipCode = string.Empty,
                    ValidationStatus = ValidationStatus.Incomplete,
                    Guarantees = new List<LocationGuarantee>()
                }
            }
        };

        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);
        SetupDefaultTariffs();

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync("DAN-2026-00001", new CalculateRequest(5));

        // Assert
        await act.Should().ThrowAsync<InvalidQuoteStateException>();
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_Should_ThrowCoreOhsUnavailableException_WhenClientThrows()
    {
        // Arrange
        PropertyQuote quote = BuildQuoteWith2CalculableAnd1Incomplete(version: 5);
        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        _mockCoreOhsClient
            .Setup(x => x.GetFireTariffsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CoreOhsUnavailableException("service is down"));

        _mockCoreOhsClient
            .Setup(x => x.GetCatTariffsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CatTariffDto>());

        _mockCoreOhsClient
            .Setup(x => x.GetElectronicEquipmentFactorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ElectronicEquipmentFactorDto>());

        _mockCoreOhsClient
            .Setup(x => x.GetCalculationParametersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CalculationParametersDto(0.05m, 0.10m, 0.03m, 0.16m, 0.02m, "2026-01-01"));

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync("DAN-2026-00001", new CalculateRequest(5));

        // Assert
        await act.Should().ThrowAsync<CoreOhsUnavailableException>();
    }

    // ─── Edge Cases ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_Should_UseFireRateZeroAndLogWarning_WhenFireKeyNotFound()
    {
        // Arrange — location uses fire key "X-99" which is not in the tariff list
        var quote = new PropertyQuote
        {
            FolioNumber = "DAN-2026-00001",
            Version = 5,
            QuoteStatus = QuoteStatus.Draft,
            Locations = new List<Location>
            {
                new()
                {
                    Index = 1,
                    LocationName = "Ubicación especial",
                    ZipCode = "06600",
                    BusinessLine = new BusinessLine { FireKey = "X-99" },
                    CatZone = "A",
                    ValidationStatus = ValidationStatus.Calculable,
                    Guarantees = new List<LocationGuarantee>
                    {
                        new() { GuaranteeKey = GuaranteeKeys.BuildingFire, InsuredAmount = 1_000_000m },
                    }
                }
            }
        };

        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        _mockCoreOhsClient
            .Setup(x => x.GetFireTariffsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FireTariffDto>
            {
                new("B-03", 0.00125m, "Bodega") // "X-99" not present
            });

        _mockCoreOhsClient
            .Setup(x => x.GetCatTariffsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CatTariffDto> { new("A", 0.0035m, 0.0028m) });

        _mockCoreOhsClient
            .Setup(x => x.GetElectronicEquipmentFactorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ElectronicEquipmentFactorDto>());

        _mockCoreOhsClient
            .Setup(x => x.GetCalculationParametersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CalculationParametersDto(0.05m, 0.10m, 0.03m, 0.16m, 0.02m, "2026-01-01"));

        _mockCoreOhsClient
            .Setup(x => x.GetZipCodeAsync("06600", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ZipCodeDto("06600", "CDMX", "Cuauhtémoc", "Centro", "CDMX", "A", 2));

        _mockRepository
            .Setup(x => x.UpdateFinancialResultAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<List<LocationPremium>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act — should NOT throw, should proceed with rate = 0
        CalculateResultResponse result = await Sut.ExecuteAsync("DAN-2026-00001", new CalculateRequest(5));

        // Assert — premium for building_fire with rate=0 is 0
        CoveragePremiumDto fireCoverage = result.PremiumsByLocation
            .Single()
            .CoveragePremiums
            .Single(c => c.GuaranteeKey == GuaranteeKeys.BuildingFire);

        fireCoverage.Premium.Should().Be(0m);

        // Logger.LogWarning should have been called at least once
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    // ─── RN-009-02b: Garantías deshabilitadas por CoverageOptions ─────────────

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_LocationWithDisabledGuarantee_TreatsAsIncomplete()
    {
        // Arrange
        var quote = new PropertyQuote
        {
            FolioNumber = "DAN-2026-00020",
            Version = 1,
            QuoteStatus = QuoteStatus.Draft,
            CoverageOptions = new CoverageOptions
            {
                EnabledGuarantees = new List<string> { GuaranteeKeys.BuildingFire }
            },
            Locations = new List<Location>
            {
                new()
                {
                    Index = 1,
                    LocationName = "Ubicación habilitada",
                    ZipCode = "06600",
                    BusinessLine = new BusinessLine { FireKey = "B-03" },
                    CatZone = "A",
                    ValidationStatus = ValidationStatus.Calculable,
                    Guarantees = new List<LocationGuarantee>
                    {
                        new() { GuaranteeKey = GuaranteeKeys.BuildingFire, InsuredAmount = 1_000_000m }
                    }
                },
                new()
                {
                    Index = 2,
                    LocationName = "Ubicación con garantía deshabilitada",
                    ZipCode = "64000",
                    BusinessLine = new BusinessLine { FireKey = "B-03" },
                    CatZone = "B",
                    ValidationStatus = ValidationStatus.Calculable,
                    Guarantees = new List<LocationGuarantee>
                    {
                        new() { GuaranteeKey = GuaranteeKeys.Glass, InsuredAmount = 500_000m }
                    }
                }
            }
        };

        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-00020", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        _mockCoreOhsClient
            .Setup(x => x.GetFireTariffsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FireTariffDto> { new("B-03", 0.00125m, "Bodega") });

        _mockCoreOhsClient
            .Setup(x => x.GetCatTariffsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CatTariffDto> { new("A", 0.0035m, 0.0028m) });

        _mockCoreOhsClient
            .Setup(x => x.GetElectronicEquipmentFactorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ElectronicEquipmentFactorDto>());

        _mockCoreOhsClient
            .Setup(x => x.GetCalculationParametersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CalculationParametersDto(0.05m, 0.10m, 0.03m, 0.16m, 0.02m, "2026-01-01"));

        _mockCoreOhsClient
            .Setup(x => x.GetZipCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ZipCodeDto("06600", "CDMX", "Cuauhtémoc", "Centro", "CDMX", "A", 2));

        _mockRepository
            .Setup(x => x.UpdateFinancialResultAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<List<LocationPremium>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CalculateResultResponse result = await Sut.ExecuteAsync("DAN-2026-00020", new CalculateRequest(1));

        // Assert
        result.PremiumsByLocation.Should().HaveCount(2);

        LocationPremiumDto loc1 = result.PremiumsByLocation.Single(p => p.LocationIndex == 1);
        loc1.ValidationStatus.Should().Be(ValidationStatus.Calculable);
        loc1.NetPremium.Should().BeGreaterThan(0m);

        LocationPremiumDto loc2 = result.PremiumsByLocation.Single(p => p.LocationIndex == 2);
        loc2.ValidationStatus.Should().Be(ValidationStatus.Incomplete);
        loc2.NetPremium.Should().Be(0m);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task ExecuteAsync_MixedGuarantees_OnlyEnabledOnesCalculated()
    {
        // Arrange
        var quote = new PropertyQuote
        {
            FolioNumber = "DAN-2026-00021",
            Version = 1,
            QuoteStatus = QuoteStatus.Draft,
            CoverageOptions = new CoverageOptions
            {
                EnabledGuarantees = new List<string> { GuaranteeKeys.BuildingFire, GuaranteeKeys.CatTev }
            },
            Locations = new List<Location>
            {
                new()
                {
                    Index = 1,
                    LocationName = "Bodega incendio",
                    ZipCode = "06600",
                    BusinessLine = new BusinessLine { FireKey = "B-03" },
                    CatZone = "A",
                    ValidationStatus = ValidationStatus.Calculable,
                    Guarantees = new List<LocationGuarantee>
                    {
                        new() { GuaranteeKey = GuaranteeKeys.BuildingFire, InsuredAmount = 1_000_000m }
                    }
                },
                new()
                {
                    Index = 2,
                    LocationName = "Bodega CAT TEV",
                    ZipCode = "06600",
                    BusinessLine = new BusinessLine { FireKey = "B-03" },
                    CatZone = "A",
                    ValidationStatus = ValidationStatus.Calculable,
                    Guarantees = new List<LocationGuarantee>
                    {
                        new() { GuaranteeKey = GuaranteeKeys.CatTev, InsuredAmount = 2_000_000m }
                    }
                },
                new()
                {
                    Index = 3,
                    LocationName = "Bodega robo (garantía deshabilitada)",
                    ZipCode = "06600",
                    BusinessLine = new BusinessLine { FireKey = "B-03" },
                    CatZone = "A",
                    ValidationStatus = ValidationStatus.Calculable,
                    Guarantees = new List<LocationGuarantee>
                    {
                        new() { GuaranteeKey = GuaranteeKeys.Theft, InsuredAmount = 500_000m }
                    }
                }
            }
        };

        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-00021", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        _mockCoreOhsClient
            .Setup(x => x.GetFireTariffsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FireTariffDto> { new("B-03", 0.00125m, "Bodega") });

        _mockCoreOhsClient
            .Setup(x => x.GetCatTariffsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CatTariffDto> { new("A", 0.0035m, 0.0028m) });

        _mockCoreOhsClient
            .Setup(x => x.GetElectronicEquipmentFactorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ElectronicEquipmentFactorDto>());

        _mockCoreOhsClient
            .Setup(x => x.GetCalculationParametersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CalculationParametersDto(0.05m, 0.10m, 0.03m, 0.16m, 0.02m, "2026-01-01"));

        _mockCoreOhsClient
            .Setup(x => x.GetZipCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ZipCodeDto("06600", "CDMX", "Cuauhtémoc", "Centro", "CDMX", "A", 2));

        _mockRepository
            .Setup(x => x.UpdateFinancialResultAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<List<LocationPremium>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CalculateResultResponse result = await Sut.ExecuteAsync("DAN-2026-00021", new CalculateRequest(1));

        // Assert
        result.PremiumsByLocation.Should().HaveCount(3);

        result.PremiumsByLocation
            .Where(p => p.LocationIndex == 1 || p.LocationIndex == 2)
            .Should().AllSatisfy(p =>
            {
                p.ValidationStatus.Should().Be(ValidationStatus.Calculable);
                p.NetPremium.Should().BeGreaterThan(0m);
            });

        LocationPremiumDto disabledLocation = result.PremiumsByLocation.Single(p => p.LocationIndex == 3);
        disabledLocation.ValidationStatus.Should().Be(ValidationStatus.Incomplete);
        disabledLocation.NetPremium.Should().Be(0m);

        decimal expectedNetPremium = result.PremiumsByLocation
            .Where(p => p.ValidationStatus == ValidationStatus.Calculable)
            .Sum(p => p.NetPremium);
        result.NetPremium.Should().Be(expectedNetPremium);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ExecuteAsync_AllLocationsHaveDisabledGuarantees_ThrowsInvalidQuoteStateException()
    {
        // Arrange
        var quote = new PropertyQuote
        {
            FolioNumber = "DAN-2026-00022",
            Version = 1,
            QuoteStatus = QuoteStatus.Draft,
            CoverageOptions = new CoverageOptions
            {
                EnabledGuarantees = new List<string> { GuaranteeKeys.BuildingFire }
            },
            Locations = new List<Location>
            {
                new()
                {
                    Index = 1,
                    LocationName = "Bodega sin coberturas habilitadas",
                    ZipCode = "06600",
                    BusinessLine = new BusinessLine { FireKey = "B-03" },
                    CatZone = "A",
                    ValidationStatus = ValidationStatus.Calculable,
                    Guarantees = new List<LocationGuarantee>
                    {
                        new() { GuaranteeKey = GuaranteeKeys.Theft, InsuredAmount = 500_000m }
                    }
                },
                new()
                {
                    Index = 2,
                    LocationName = "Sucursal sin coberturas habilitadas",
                    ZipCode = "64000",
                    BusinessLine = new BusinessLine { FireKey = "B-03" },
                    CatZone = "B",
                    ValidationStatus = ValidationStatus.Calculable,
                    Guarantees = new List<LocationGuarantee>
                    {
                        new() { GuaranteeKey = GuaranteeKeys.Glass, InsuredAmount = 300_000m }
                    }
                }
            }
        };

        _mockRepository
            .Setup(x => x.GetByFolioNumberAsync("DAN-2026-00022", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);
        SetupDefaultTariffs();

        // Act
        Func<Task> act = async () => await Sut.ExecuteAsync("DAN-2026-00022", new CalculateRequest(1));

        // Assert
        await act.Should().ThrowAsync<InvalidQuoteStateException>();
    }
}
