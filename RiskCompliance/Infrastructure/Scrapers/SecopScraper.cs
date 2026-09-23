using System.Globalization;
using System.Text.Json;
using RiskCompliance.Domain.Interfaces;
using RiskCompliance.Domain.Models;

namespace RiskCompliance.Infrastructure.Scrapers;

public class SecopScraper : ISecopScraper
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://www.datos.gov.co/resource/4n4q-k399.json";

    public SecopScraper(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<SecopPenalty>> SearchPenaltiesAsync(string entityName, CancellationToken cancellationToken = default)
    {
        var results = new List<SecopPenalty>();
        if (string.IsNullOrWhiteSpace(entityName))
            return results;

        var cleanQuery = entityName.Trim().ToUpperInvariant();
        var filter = $"upper(nombre_entidad) like '%{cleanQuery}%' or upper(nombre_contratista) like '%{cleanQuery}%'";
        var url = $"{BaseUrl}?$where={Uri.EscapeDataString(filter)}&$order=fecha_de_publicacion DESC&$limit=100";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "EY-RiskCompliance-Service/1.0");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var fallbackFilter = $"upper(nombre_entidad) like '%{cleanQuery}%'";
            var fallbackUrl = $"{BaseUrl}?$where={Uri.EscapeDataString(fallbackFilter)}&$limit=100";
            response = await _httpClient.GetAsync(fallbackUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return results;
        }

        var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var items = JsonSerializer.Deserialize<List<SecopSocrataItem>>(jsonString, jsonOptions);
        if (items == null || items.Count == 0)
            return results;

        foreach (var item in items)
        {
            results.Add(new SecopPenalty
            {
                NombreEntidad = item.NombreEntidad?.ToString() ?? string.Empty,
                NitEntidad = item.NitEntidad?.ToString() ?? string.Empty,
                Nivel = item.Nivel?.ToString() ?? string.Empty,
                Orden = item.Orden?.ToString() ?? string.Empty,
                Municipio = item.Municipio?.ToString() ?? string.Empty,
                NumeroResolucion = item.NumeroResolucion?.ToString() ?? string.Empty,
                DocumentoContratista = item.DocumentoContratista?.ToString() ?? string.Empty,
                NombreContratista = item.NombreContratista?.ToString() ?? string.Empty,
                NumeroContrato = item.NumeroContrato?.ToString() ?? string.Empty,
                ValorSancion = FormatCurrency(item.ValorSancion?.ToString()),
                FechaPublicacion = FormatDate(item.FechaPublicacion?.ToString()),
                FechaFirmeza = FormatDate(item.FechaFirmeza?.ToString()),
                FechaCargue = FormatDate(item.FechaCargue?.ToString()),
                RutaProceso = item.RutaProceso?.ToString() ?? string.Empty
            });
        }

        return results;
    }

    private static string FormatCurrency(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed.ToString("C2", new CultureInfo("es-CO"));
        return raw;
    }

    private static string FormatDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dt))
            return dt.ToString("yyyy-MM-dd HH:mm");
        return raw;
    }
}
