using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierManagement.Domain.Entities;

[Table("Suppliers")]
public class Supplier
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string LegalName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string TradeName { get; set; } = string.Empty;

    [Required]
    [MaxLength(11)]
    public string TaxId { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Website { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string PhysicalAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AnnualRevenue { get; set; }

    public DateTime LastEditedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<LegalRepresentative> LegalRepresentatives { get; set; } = new List<LegalRepresentative>();
}
