namespace RiskCompliance.Domain.Models;

public class MultiScreeningRequest
{
    public string EntityName { get; set; } = string.Empty;
    public string? RepresentativeName { get; set; }
    public List<string> Sources { get; set; } = new();
}

public class MultiScreeningResult
{
    public string EntityName { get; set; } = string.Empty;
    public int TotalHits { get; set; }
    public SmvScreeningResult? Smv { get; set; }
    public List<SecopPenalty>? Secop { get; set; }
    public List<InterpolPerson>? Interpol { get; set; }
    public Dictionary<string, string> Errors { get; set; } = new();
}
