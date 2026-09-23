namespace RiskCompliance.Domain.Models;

public class SmvSanction
{
    public string ResolutionDate { get; set; } = string.Empty;
    public string ResolutionNumber { get; set; } = string.Empty;
    public string ResolutionUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string HasAppeal { get; set; } = string.Empty;
    public string ResolutiveNumber { get; set; } = string.Empty;
    public string ResolutiveDate { get; set; } = string.Empty;
}

public class SmvScreeningResult
{
    public string SearchedEntity { get; set; } = string.Empty;
    public string MatchedEntity { get; set; } = string.Empty;
    public int SanctionsCount => Sanctions.Count;
    public List<SmvSanction> Sanctions { get; set; } = new();
}

public class SmvSearchRequest
{
    public string EntityName { get; set; } = string.Empty;
}
