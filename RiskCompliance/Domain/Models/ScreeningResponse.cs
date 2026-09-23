namespace RiskCompliance.Domain.Models;

public class ScreeningResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public int ResultsCount { get; set; }
    public List<T> Data { get; set; } = new();

    public static ScreeningResponse<T> Ok(List<T> data, string message = "Búsqueda completada exitosamente.")
    {
        return new ScreeningResponse<T>
        {
            Success = true,
            Message = message,
            ResultsCount = data.Count,
            Data = data
        };
    }

    public static ScreeningResponse<T> Fail(string errorMessage)
    {
        return new ScreeningResponse<T>
        {
            Success = false,
            Message = errorMessage,
            ResultsCount = 0,
            Data = new List<T>()
        };
    }
}
