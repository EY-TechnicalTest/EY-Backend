using RiskCompliance.Domain.Models;

namespace RiskCompliance.Domain.Interfaces;

public interface IInterpolScraper
{
    Task<(List<InterpolPerson> Persons, int TotalHits)> SearchRedNoticesAsync(InterpolSearchCriteria criteria, CancellationToken cancellationToken = default);
}
