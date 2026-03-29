using Cotizador.Domain.Entities;
using Cotizador.Domain.ValueObjects;

namespace Cotizador.Application.Ports;

public interface IQuoteRepository
{
    /// <summary>Creates a new quote document. Throws if folioNumber already exists.</summary>
    Task CreateAsync(PropertyQuote quote, CancellationToken ct = default);

    /// <summary>Finds a quote by folioNumber. Returns null if not found.</summary>
    Task<PropertyQuote?> GetByFolioNumberAsync(string folioNumber, CancellationToken ct = default);

    /// <summary>Finds a quote by idempotencyKey. Returns null if not found.</summary>
    Task<PropertyQuote?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);

    /// <summary>
    /// Updates general info section. Throws VersionConflictException if version mismatch.
    /// $set: insuredData, conductionData, agentCode, businessType, riskClassification,
    ///       version+1, metadata.updatedAt, metadata.lastWizardStep=1
    /// </summary>
    Task UpdateGeneralInfoAsync(
        string folioNumber,
        int expectedVersion,
        InsuredData insuredData,
        ConductionData conductionData,
        string agentCode,
        string businessType,
        string riskClassification,
        CancellationToken ct = default);

    /// <summary>
    /// Updates layout configuration section.
    /// $set: layoutConfiguration, version+1, metadata.updatedAt, metadata.lastWizardStep=2
    /// </summary>
    Task UpdateLayoutAsync(
        string folioNumber,
        int expectedVersion,
        LayoutConfiguration layout,
        CancellationToken ct = default);

    /// <summary>
    /// Replaces the entire locations array.
    /// $set: locations, version+1, metadata.updatedAt, metadata.lastWizardStep=2
    /// </summary>
    Task UpdateLocationsAsync(
        string folioNumber,
        int expectedVersion,
        List<Location> locations,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a single location by index (PATCH semantics).
    /// $set: locations.&lt;index&gt;.* only provided fields, version+1, metadata.updatedAt, metadata.lastWizardStep=2
    /// </summary>
    Task PatchLocationAsync(
        string folioNumber,
        int expectedVersion,
        int locationIndex,
        Location patchData,
        CancellationToken ct = default);

    /// <summary>
    /// Updates coverage options section.
    /// $set: coverageOptions, version+1, metadata.updatedAt, metadata.lastWizardStep=3
    /// </summary>
    Task UpdateCoverageOptionsAsync(
        string folioNumber,
        int expectedVersion,
        CoverageOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Updates financial result without touching other sections.
    /// $set: netPremium, commercialPremium, premiumsByLocation, quoteStatus="calculated",
    ///       version+1, metadata.updatedAt, metadata.lastWizardStep=4
    /// </summary>
    Task UpdateFinancialResultAsync(
        string folioNumber,
        int expectedVersion,
        decimal netPremium,
        decimal commercialPremium,
        List<LocationPremium> premiumsByLocation,
        CancellationToken ct = default);
}
