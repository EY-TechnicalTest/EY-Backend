using Microsoft.EntityFrameworkCore;
using RiskCompliance.Application;
using RiskCompliance.Domain.Models;
using SupplierManagement.Application.DTOs;
using SupplierManagement.Domain.Entities;
using SupplierManagement.Infrastructure.Persistence;

namespace SupplierManagement.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly SupplierDbContext _context;
    private readonly IScreeningService _screeningService;

    public SupplierService(SupplierDbContext context, IScreeningService screeningService)
    {
        _context = context;
        _screeningService = screeningService;
    }

    public async Task<List<SupplierResponseDto>> GetAllSuppliersAsync(CancellationToken cancellationToken = default)
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.LegalRepresentatives)
            .OrderByDescending(s => s.LastEditedAt)
            .ToListAsync(cancellationToken);

        return suppliers.Select(MapToResponseDto).ToList();
    }

    public async Task<SupplierResponseDto?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.LegalRepresentatives)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return supplier != null ? MapToResponseDto(supplier) : null;
    }

    public async Task<SupplierResponseDto> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var taxIdExists = await _context.Suppliers.AnyAsync(s => s.TaxId == dto.TaxId.Trim(), cancellationToken);
        if (taxIdExists)
        {
            throw new InvalidOperationException($"Ya existe un proveedor registrado con la identificación tributaria '{dto.TaxId}'.");
        }

        var supplier = new Supplier
        {
            LegalName = dto.LegalName.Trim(),
            TradeName = dto.TradeName.Trim(),
            TaxId = dto.TaxId.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            Email = dto.Email.Trim(),
            Website = dto.Website.Trim(),
            PhysicalAddress = dto.PhysicalAddress.Trim(),
            Country = dto.Country.Trim(),
            AnnualRevenue = dto.AnnualRevenue,
            LastEditedAt = DateTime.UtcNow
        };

        if (dto.Representatives != null && dto.Representatives.Count > 0)
        {
            foreach (var rep in dto.Representatives)
            {
                supplier.LegalRepresentatives.Add(new LegalRepresentative
                {
                    Forename = rep.Forename.Trim(),
                    FamilyName = rep.FamilyName.Trim(),
                    Email = rep.Email?.Trim(),
                    DocumentNumber = rep.DocumentNumber?.Trim()
                });
            }
        }

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(supplier);
    }

    public async Task<SupplierResponseDto?> UpdateSupplierAsync(int id, UpdateSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.LegalRepresentatives)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (supplier == null)
            return null;

        var taxIdExistsOnOther = await _context.Suppliers.AnyAsync(s => s.TaxId == dto.TaxId.Trim() && s.Id != id, cancellationToken);
        if (taxIdExistsOnOther)
        {
            throw new InvalidOperationException($"La identificación tributaria '{dto.TaxId}' ya está en uso por otro proveedor.");
        }

        supplier.LegalName = dto.LegalName.Trim();
        supplier.TradeName = dto.TradeName.Trim();
        supplier.TaxId = dto.TaxId.Trim();
        supplier.PhoneNumber = dto.PhoneNumber.Trim();
        supplier.Email = dto.Email.Trim();
        supplier.Website = dto.Website.Trim();
        supplier.PhysicalAddress = dto.PhysicalAddress.Trim();
        supplier.Country = dto.Country.Trim();
        supplier.AnnualRevenue = dto.AnnualRevenue;
        supplier.LastEditedAt = DateTime.UtcNow;

        // Actualizar representantes si se enviaron
        if (dto.Representatives != null)
        {
            _context.LegalRepresentatives.RemoveRange(supplier.LegalRepresentatives);
            foreach (var rep in dto.Representatives)
            {
                supplier.LegalRepresentatives.Add(new LegalRepresentative
                {
                    Forename = rep.Forename.Trim(),
                    FamilyName = rep.FamilyName.Trim(),
                    Email = rep.Email?.Trim(),
                    DocumentNumber = rep.DocumentNumber?.Trim(),
                    SupplierId = supplier.Id
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(supplier);
    }

    public async Task<bool> DeleteSupplierAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (supplier == null)
            return false;

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<SupplierScreeningResponseDto?> ScreenSupplierAsync(int id, List<string> sources, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.LegalRepresentatives)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (supplier == null)
            return null;

        var representativeName = supplier.LegalRepresentatives.FirstOrDefault() != null
            ? $"{supplier.LegalRepresentatives.First().Forename} {supplier.LegalRepresentatives.First().FamilyName}".Trim()
            : string.Empty;

        var multiRequest = new MultiScreeningRequest
        {
            EntityName = !string.IsNullOrWhiteSpace(supplier.TradeName) ? supplier.TradeName : supplier.LegalName,
            RepresentativeName = representativeName,
            Sources = sources
        };

        var screeningResult = await _screeningService.RunMultiScreeningAsync(multiRequest, cancellationToken);

        var firstResult = screeningResult.Data.FirstOrDefault() ?? new MultiScreeningResult
        {
            EntityName = multiRequest.EntityName
        };

        return new SupplierScreeningResponseDto
        {
            SupplierId = supplier.Id,
            LegalName = supplier.LegalName,
            TradeName = supplier.TradeName,
            TaxId = supplier.TaxId,
            TotalHits = firstResult.TotalHits,
            ScreeningResults = firstResult
        };
    }

    private static SupplierResponseDto MapToResponseDto(Supplier s)
    {
        return new SupplierResponseDto
        {
            Id = s.Id,
            LegalName = s.LegalName,
            TradeName = s.TradeName,
            TaxId = s.TaxId,
            PhoneNumber = s.PhoneNumber,
            Email = s.Email,
            Website = s.Website,
            PhysicalAddress = s.PhysicalAddress,
            Country = s.Country,
            AnnualRevenue = s.AnnualRevenue,
            LastEditedAt = s.LastEditedAt,
            Representatives = s.LegalRepresentatives.Select(r => new LegalRepresentativeResponseDto
            {
                Id = r.Id,
                SupplierId = r.SupplierId,
                Forename = r.Forename,
                FamilyName = r.FamilyName,
                Email = r.Email,
                DocumentNumber = r.DocumentNumber
            }).ToList()
        };
    }
}
