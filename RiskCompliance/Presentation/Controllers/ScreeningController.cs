using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RiskCompliance.Application;
using RiskCompliance.Domain.Models;

namespace RiskCompliance.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScreeningController : ControllerBase
{
    private readonly IScreeningService _screeningService;

    public ScreeningController(IScreeningService screeningService)
    {
        _screeningService = screeningService;
    }

    /// <summary>
    /// Consulta sanciones en la SMV (Superintendencia del Mercado de Valores - Perú).
    /// </summary>
    [HttpPost("smv")]
    [ProducesResponseType(typeof(ScreeningResponse<SmvScreeningResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchSmv([FromBody] SmvSearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EntityName))
            return BadRequest(ScreeningResponse<SmvScreeningResult>.Fail("El parámetro 'entityName' es obligatorio."));

        var response = await _screeningService.RunSmvScreeningAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Consulta sanciones en la SMV vía GET por query parameter.
    /// </summary>
    [HttpGet("smv")]
    [ProducesResponseType(typeof(ScreeningResponse<SmvScreeningResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSmvGet([FromQuery] string entityName, CancellationToken cancellationToken)
    {
        return await SearchSmv(new SmvSearchRequest { EntityName = entityName }, cancellationToken);
    }

    /// <summary>
    /// Consulta sanciones y multas en SECOP I (Colombia - Datos Abiertos).
    /// </summary>
    [HttpPost("secop")]
    [ProducesResponseType(typeof(ScreeningResponse<SecopPenalty>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchSecop([FromBody] SecopSearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EntityName))
            return BadRequest(ScreeningResponse<SecopPenalty>.Fail("El parámetro 'entityName' es obligatorio."));

        var response = await _screeningService.RunSecopScreeningAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Consulta sanciones en SECOP I vía GET por query parameter.
    /// </summary>
    [HttpGet("secop")]
    [ProducesResponseType(typeof(ScreeningResponse<SecopPenalty>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSecopGet([FromQuery] string entityName, CancellationToken cancellationToken)
    {
        return await SearchSecop(new SecopSearchRequest { EntityName = entityName }, cancellationToken);
    }

    /// <summary>
    /// Consulta notificaciones rojas (Red Notices) en Interpol.
    /// </summary>
    [HttpPost("interpol")]
    [ProducesResponseType(typeof(ScreeningResponse<InterpolPerson>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchInterpol([FromBody] InterpolSearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FamilyName) && string.IsNullOrWhiteSpace(request.Forename))
            return BadRequest(ScreeningResponse<InterpolPerson>.Fail("Debe proporcionar al menos un apellido o un nombre."));

        var response = await _screeningService.RunInterpolScreeningAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Consulta notificaciones rojas en Interpol vía GET por query parameters.
    /// </summary>
    [HttpGet("interpol")]
    [ProducesResponseType(typeof(ScreeningResponse<InterpolPerson>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchInterpolGet(
        [FromQuery] string? familyName,
        [FromQuery] string? forename,
        [FromQuery] string? nationality,
        [FromQuery] string? gender,
        [FromQuery] int? age,
        [FromQuery] string? wantedBy,
        CancellationToken cancellationToken)
    {
        var request = new InterpolSearchRequest
        {
            FamilyName = familyName,
            Forename = forename,
            Nationality = nationality,
            Gender = gender,
            Age = age,
            WantedBy = wantedBy
        };

        return await SearchInterpol(request, cancellationToken);
    }

    /// <summary>
    /// Ejecuta cruce simultáneo en 1 a 3 fuentes seleccionadas (SMV, SECOP, INTERPOL).
    /// </summary>
    [HttpPost("check")]
    [ProducesResponseType(typeof(ScreeningResponse<MultiScreeningResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckMultiSources([FromBody] MultiScreeningRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EntityName))
            return BadRequest(ScreeningResponse<MultiScreeningResult>.Fail("El nombre de la entidad es obligatorio."));

        if (request.Sources == null || request.Sources.Count < 1 || request.Sources.Count > 3)
            return BadRequest(ScreeningResponse<MultiScreeningResult>.Fail("Debe seleccionar entre 1 y 3 fuentes (SMV, SECOP, INTERPOL)."));

        var response = await _screeningService.RunMultiScreeningAsync(request, cancellationToken);
        return Ok(response);
    }
}
