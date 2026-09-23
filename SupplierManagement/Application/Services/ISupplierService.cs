using SupplierManagement.Application.DTOs;

namespace SupplierManagement.Application.Services;

public interface ISupplierService
{
    Task<List<SupplierResponseDto>> GetAllSuppliersAsync(CancellationToken cancellationToken = default);
    Task<SupplierResponseDto?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SupplierResponseDto> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default);
    Task<SupplierResponseDto?> UpdateSupplierAsync(int id, UpdateSupplierDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteSupplierAsync(int id, CancellationToken cancellationToken = default);
    Task<SupplierScreeningResponseDto?> ScreenSupplierAsync(int id, List<string> sources, CancellationToken cancellationToken = default);
}
