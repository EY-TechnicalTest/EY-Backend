using System.ComponentModel.DataAnnotations;
using RiskCompliance.Domain.Models;

namespace SupplierManagement.Application.DTOs;

public class CreateSupplierDto
{
    [Required(ErrorMessage = "La razón social es obligatoria.")]
    [StringLength(200, ErrorMessage = "La razón social no puede exceder 200 caracteres.")]
    public string LegalName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre comercial es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre comercial no puede exceder 200 caracteres.")]
    public string TradeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La identificación tributaria es obligatoria.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "La identificación tributaria debe contener exactamente 11 dígitos numéricos.")]
    public string TaxId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número telefónico es obligatorio.")]
    [RegularExpression(@"^[\+]?[(]?[0-9]{1,4}[)]?[-\s\./0-9]{6,15}$", ErrorMessage = "El formato del número telefónico no es válido.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El sitio web es obligatorio.")]
    [Url(ErrorMessage = "El sitio web debe ser una URL válida (ej. https://www.ejemplo.com).")]
    public string Website { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección física es obligatoria.")]
    [StringLength(300, ErrorMessage = "La dirección no puede exceder 300 caracteres.")]
    public string PhysicalAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "El país es obligatorio.")]
    public string Country { get; set; } = string.Empty;

    [Range(0, (double)decimal.MaxValue, ErrorMessage = "La facturación anual debe ser un valor numérico mayor o igual a cero.")]
    public decimal AnnualRevenue { get; set; }

    public List<CreateLegalRepresentativeDto>? Representatives { get; set; } = new();
}

public class UpdateSupplierDto : CreateSupplierDto
{
}

public class CreateLegalRepresentativeDto
{
    [Required(ErrorMessage = "El nombre del representante legal es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
    public string Forename { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido del representante legal es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres.")]
    public string FamilyName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo del representante no es válido.")]
    public string? Email { get; set; }

    public string? DocumentNumber { get; set; }
}

public class SupplierResponseDto
{
    public int Id { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string PhysicalAddress { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal AnnualRevenue { get; set; }
    public string FormattedAnnualRevenue => AnnualRevenue.ToString("C2", new System.Globalization.CultureInfo("en-US"));
    public DateTime LastEditedAt { get; set; }
    public List<LegalRepresentativeResponseDto> Representatives { get; set; } = new();
}

public class LegalRepresentativeResponseDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string Forename { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string FullName => $"{Forename} {FamilyName}".Trim();
    public string? Email { get; set; }
    public string? DocumentNumber { get; set; }
}

public class SupplierScreeningRequestDto
{
    public List<string> Sources { get; set; } = new() { "SMV", "SECOP", "INTERPOL" };
}

public class SupplierScreeningResponseDto
{
    public int SupplierId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public int TotalHits { get; set; }
    public MultiScreeningResult ScreeningResults { get; set; } = new();
}
