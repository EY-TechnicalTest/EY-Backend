using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierManagement.Domain.Entities;

[Table("LegalRepresentatives")]
public class LegalRepresentative
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SupplierId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Forename { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FamilyName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? DocumentNumber { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }
}
