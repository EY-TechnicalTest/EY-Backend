using RiskCompliance.Domain.Interfaces;
using RiskCompliance.Domain.Models;

namespace RiskCompliance.Application;

public class ScreeningService : IScreeningService
{
    private readonly ISmvScraper _smvScraper;
    private readonly ISecopScraper _secopScraper;
    private readonly IInterpolScraper _interpolScraper;

    public ScreeningService(ISmvScraper smvScraper, ISecopScraper secopScraper, IInterpolScraper interpolScraper)
    {
        _smvScraper = smvScraper;
        _secopScraper = secopScraper;
        _interpolScraper = interpolScraper;
    }

    public async Task<ScreeningResponse<SmvScreeningResult>> RunSmvScreeningAsync(SmvSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EntityName))
            return ScreeningResponse<SmvScreeningResult>.Fail("El nombre de la entidad a buscar en SMV es obligatorio.");

        try
        {
            var result = await _smvScraper.SearchSanctionsAsync(request.EntityName.Trim(), cancellationToken);
            return new ScreeningResponse<SmvScreeningResult>
            {
                Success = true,
                Message = result.Sanctions.Count > 0 
                    ? $"Se encontraron {result.Sanctions.Count} sanción(es) en SMV." 
                    : "No se encontraron sanciones registradas en SMV para la entidad.",
                ResultsCount = result.Sanctions.Count,
                Data = new List<SmvScreeningResult> { result }
            };
        }
        catch (Exception ex)
        {
            return ScreeningResponse<SmvScreeningResult>.Fail($"Error al realizar screening en SMV: {ex.Message}");
        }
    }

    public async Task<ScreeningResponse<SecopPenalty>> RunSecopScreeningAsync(SecopSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EntityName))
            return ScreeningResponse<SecopPenalty>.Fail("El nombre de la entidad a buscar en SECOP I es obligatorio.");

        try
        {
            var penalties = await _secopScraper.SearchPenaltiesAsync(request.EntityName.Trim(), cancellationToken);
            return new ScreeningResponse<SecopPenalty>
            {
                Success = true,
                Message = penalties.Count > 0
                    ? $"Se encontraron {penalties.Count} sanción(es) en SECOP I."
                    : "No se encontraron sanciones en SECOP I para la entidad consultada.",
                ResultsCount = penalties.Count,
                Data = penalties
            };
        }
        catch (Exception ex)
        {
            return ScreeningResponse<SecopPenalty>.Fail($"Error al realizar screening en SECOP I: {ex.Message}");
        }
    }

    public async Task<ScreeningResponse<InterpolPerson>> RunInterpolScreeningAsync(InterpolSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FamilyName) && string.IsNullOrWhiteSpace(request.Forename))
            return ScreeningResponse<InterpolPerson>.Fail("Debe ingresar al menos un apellido (FamilyName) o nombre (Forename) para la búsqueda en Interpol.");

        try
        {
            var criteria = new InterpolSearchCriteria
            {
                FamilyName = request.FamilyName,
                Forename = request.Forename,
                Nationality = request.Nationality,
                Gender = request.Gender,
                Age = request.Age,
                WantedBy = request.WantedBy
            };

            var (persons, totalHits) = await _interpolScraper.SearchRedNoticesAsync(criteria, cancellationToken);
            return new ScreeningResponse<InterpolPerson>
            {
                Success = true,
                Message = totalHits > 0
                    ? $"Se encontraron {totalHits} notificación(es) roja(s) en Interpol."
                    : "No se encontraron notificaciones rojas activas en Interpol para los criterios ingresados.",
                ResultsCount = totalHits,
                Data = persons
            };
        }
        catch (Exception ex)
        {
            return ScreeningResponse<InterpolPerson>.Fail($"Error al realizar screening en Interpol: {ex.Message}");
        }
    }

    public async Task<ScreeningResponse<MultiScreeningResult>> RunMultiScreeningAsync(MultiScreeningRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EntityName))
            return ScreeningResponse<MultiScreeningResult>.Fail("El nombre de la entidad es obligatorio.");

        if (request.Sources == null || request.Sources.Count < 1 || request.Sources.Count > 3)
            return ScreeningResponse<MultiScreeningResult>.Fail("Debe seleccionar entre 1 y 3 fuentes para el cruce de información.");

        var multiResult = new MultiScreeningResult
        {
            EntityName = request.EntityName.Trim()
        };

        var selectedSources = request.Sources.Select(s => s.Trim().ToUpperInvariant()).Distinct().ToList();
        var tasks = new List<Task>();

        // 1. SMV
        if (selectedSources.Contains("SMV"))
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var smvRes = await _smvScraper.SearchSanctionsAsync(request.EntityName.Trim(), cancellationToken);
                    multiResult.Smv = smvRes;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ScreeningService] SMV Warning: {ex.Message}");
                    // Fallback sin contaminar el resultado con logs de bajo nivel del sistema
                    multiResult.Smv ??= new SmvScreeningResult { SearchedEntity = request.EntityName.Trim() };
                }
            }, cancellationToken));
        }

        // 2. SECOP I
        if (selectedSources.Contains("SECOP") || selectedSources.Contains("SECOP I"))
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var secopRes = await _secopScraper.SearchPenaltiesAsync(request.EntityName.Trim(), cancellationToken);
                    multiResult.Secop = secopRes;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ScreeningService] SECOP Warning: {ex.Message}");
                    multiResult.Secop ??= new List<SecopPenalty>();
                }
            }, cancellationToken));
        }

        // 3. INTERPOL
        if (selectedSources.Contains("INTERPOL"))
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var personNameToSearch = !string.IsNullOrWhiteSpace(request.RepresentativeName)
                        ? request.RepresentativeName.Trim()
                        : request.EntityName.Trim();

                    var parts = personNameToSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string fore;
                    string family;
                    if (parts.Length >= 4)
                    {
                        fore = $"{parts[0]} {parts[1]}";
                        family = string.Join(" ", parts.Skip(2));
                    }
                    else if (parts.Length == 3)
                    {
                        fore = parts[0];
                        family = $"{parts[1]} {parts[2]}";
                    }
                    else if (parts.Length == 2)
                    {
                        fore = parts[0];
                        family = parts[1];
                    }
                    else
                    {
                        fore = string.Empty;
                        family = parts.Length > 0 ? parts[0] : string.Empty;
                    }

                    var criteria = new InterpolSearchCriteria
                    {
                        FamilyName = family,
                        Forename = fore
                    };

                    var (persons, totalHits) = await _interpolScraper.SearchRedNoticesAsync(criteria, cancellationToken);
                    multiResult.Interpol = persons;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ScreeningService] INTERPOL Warning: {ex.Message}");
                    multiResult.Interpol ??= new List<InterpolPerson>();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        multiResult.TotalHits = (multiResult.Smv?.SanctionsCount ?? 0)
            + (multiResult.Secop?.Count ?? 0)
            + (multiResult.Interpol?.Count ?? 0);

        return new ScreeningResponse<MultiScreeningResult>
        {
            Success = true,
            Message = $"Screening consolidado completado con {multiResult.TotalHits} coincidencia(s).",
            ResultsCount = multiResult.TotalHits,
            Data = new List<MultiScreeningResult> { multiResult }
        };
    }
}
