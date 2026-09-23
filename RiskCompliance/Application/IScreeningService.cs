using RiskCompliance.Domain.Models;

namespace RiskCompliance.Application;

public interface IScreeningService
{
    Task<ScreeningResponse<SmvScreeningResult>> RunSmvScreeningAsync(SmvSearchRequest request, CancellationToken cancellationToken = default);
    Task<ScreeningResponse<SecopPenalty>> RunSecopScreeningAsync(SecopSearchRequest request, CancellationToken cancellationToken = default);
    Task<ScreeningResponse<InterpolPerson>> RunInterpolScreeningAsync(InterpolSearchRequest request, CancellationToken cancellationToken = default);
    Task<ScreeningResponse<MultiScreeningResult>> RunMultiScreeningAsync(MultiScreeningRequest request, CancellationToken cancellationToken = default);
}
