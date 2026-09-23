using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupplierManagement.Application.DTOs;
using SupplierManagement.Application.Services;

namespace SupplierManagement.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/suppliers")]
[Authorize]
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>
    /// Obtiene todos los proveedores ordenados por fecha de última edición descendente.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SupplierResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var suppliers = await _supplierService.GetAllSuppliersAsync(cancellationToken);
        return Ok(suppliers);
    }

    /// <summary>
    /// Obtiene el detalle de un proveedor por ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SupplierResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetSupplierByIdAsync(id, cancellationToken);
        if (supplier == null)
            return NotFound(new { success = false, message = $"Proveedor con ID {id} no fue encontrado." });

        return Ok(supplier);
    }

    /// <summary>
    /// Crea un nuevo proveedor con validaciones según el tipo de dato.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SupplierResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var createdSupplier = await _supplierService.CreateSupplierAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = createdSupplier.Id }, createdSupplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un proveedor existente y refresca la fecha de última edición.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SupplierResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updatedSupplier = await _supplierService.UpdateSupplierAsync(id, dto, cancellationToken);
            if (updatedSupplier == null)
                return NotFound(new { success = false, message = $"Proveedor con ID {id} no fue encontrado." });

            return Ok(updatedSupplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un proveedor por ID.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await _supplierService.DeleteSupplierAsync(id, cancellationToken);
        if (!success)
            return NotFound(new { success = false, message = $"Proveedor con ID {id} no fue encontrado." });

        return Ok(new { success = true, message = "Proveedor eliminado exitosamente." });
    }

    /// <summary>
    /// Ejecuta el cruce automático con listas de alto riesgo para el proveedor indicado (1 a 3 fuentes).
    /// </summary>
    [HttpPost("{id}/screening")]
    [ProducesResponseType(typeof(SupplierScreeningResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScreenSupplier(int id, [FromBody] SupplierScreeningRequestDto request, CancellationToken cancellationToken)
    {
        if (request.Sources == null || request.Sources.Count < 1 || request.Sources.Count > 3)
            return BadRequest(new { success = false, message = "Debe seleccionar entre 1 y 3 fuentes (SMV, SECOP, INTERPOL)." });

        var result = await _supplierService.ScreenSupplierAsync(id, request.Sources, cancellationToken);
        if (result == null)
            return NotFound(new { success = false, message = $"Proveedor con ID {id} no fue encontrado para realizar el cruce." });

        return Ok(result);
    }
}
