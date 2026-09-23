using RiskCompliance.Domain.Models;

namespace RiskCompliance.Domain.Interfaces;

public interface ISmvScraper
{
    Task<SmvScreeningResult> SearchSanctionsAsync(string entityName, CancellationToken cancellationToken = default);
}
