using RiskCompliance.Domain.Models;

namespace RiskCompliance.Domain.Interfaces;

public interface ISecopScraper
{
    Task<List<SecopPenalty>> SearchPenaltiesAsync(string entityName, CancellationToken cancellationToken = default);
}
