using System.Text.Json.Serialization;

namespace RiskCompliance.Domain.Models;

public class SecopPenalty
{
    public string NombreEntidad { get; set; } = string.Empty;
    public string NitEntidad { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public string Orden { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string NumeroResolucion { get; set; } = string.Empty;
    public string DocumentoContratista { get; set; } = string.Empty;
    public string NombreContratista { get; set; } = string.Empty;
    public string NumeroContrato { get; set; } = string.Empty;
    public string ValorSancion { get; set; } = string.Empty;
    public string FechaPublicacion { get; set; } = string.Empty;
    public string FechaFirmeza { get; set; } = string.Empty;
    public string FechaCargue { get; set; } = string.Empty;
    public string RutaProceso { get; set; } = string.Empty;
}

public class SecopSearchRequest
{
    public string EntityName { get; set; } = string.Empty;
}

/// <summary>
/// JSON raw mapping for Socrata Open Data API (4n4q-k399.json)
/// </summary>
public class SecopSocrataItem
{
    [JsonPropertyName("nombre_entidad")]
    public object? NombreEntidad { get; set; }

    [JsonPropertyName("nit_entidad")]
    public object? NitEntidad { get; set; }

    [JsonPropertyName("nivel")]
    public object? Nivel { get; set; }

    [JsonPropertyName("orden")]
    public object? Orden { get; set; }

    [JsonPropertyName("municipio")]
    public object? Municipio { get; set; }

    [JsonPropertyName("numero_de_resolucion")]
    public object? NumeroResolucion { get; set; }

    [JsonPropertyName("documento_contratista")]
    public object? DocumentoContratista { get; set; }

    [JsonPropertyName("nombre_contratista")]
    public object? NombreContratista { get; set; }

    [JsonPropertyName("numero_de_contrato")]
    public object? NumeroContrato { get; set; }

    [JsonPropertyName("valor_sancion")]
    public object? ValorSancion { get; set; }

    [JsonPropertyName("fecha_de_publicacion")]
    public object? FechaPublicacion { get; set; }

    [JsonPropertyName("fecha_de_firmeza")]
    public object? FechaFirmeza { get; set; }

    [JsonPropertyName("fecha_de_cargue")]
    public object? FechaCargue { get; set; }

    [JsonPropertyName("ruta_de_proceso")]
    public object? RutaProceso { get; set; }
}
