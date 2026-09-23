using System.Text.Json.Serialization;

namespace RiskCompliance.Domain.Models;

public class InterpolPerson
{
    public string FamilyName { get; set; } = string.Empty;
    public string Forename { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string WantedBy { get; set; } = string.Empty;
    public string PlaceOfBirth { get; set; } = string.Empty;
    public string Charges { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
}

public class InterpolSearchCriteria
{
    public string? FamilyName { get; set; }
    public string? Forename { get; set; }
    public string? Nationality { get; set; }
    public string? Gender { get; set; }
    public int? Age { get; set; }
    public string? WantedBy { get; set; }
}

public class InterpolSearchRequest
{
    public string? FamilyName { get; set; }
    public string? Forename { get; set; }
    public string? Nationality { get; set; }
    public string? Gender { get; set; }
    public int? Age { get; set; }
    public string? WantedBy { get; set; }
}

/// <summary>
/// Internal DTO for parsing Interpol's notice detail JSON
/// </summary>
public class InterpolNoticeJson
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("forename")]
    public string? Forename { get; set; }

    [JsonPropertyName("date_of_birth")]
    public string? DateOfBirth { get; set; }

    [JsonPropertyName("sex_id")]
    public string? SexId { get; set; }

    [JsonPropertyName("place_of_birth")]
    public string? PlaceOfBirth { get; set; }

    [JsonPropertyName("country_of_birth_id")]
    public string? CountryOfBirthId { get; set; }

    [JsonPropertyName("nationalities")]
    public List<string>? Nationalities { get; set; }

    [JsonPropertyName("arrest_warrants")]
    public List<InterpolArrestWarrant>? ArrestWarrants { get; set; }
}

public class InterpolArrestWarrant
{
    [JsonPropertyName("charge")]
    public string? Charge { get; set; }

    [JsonPropertyName("issuing_country_id")]
    public string? IssuingCountryId { get; set; }
}
