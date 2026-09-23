using Microsoft.EntityFrameworkCore;
using SupplierManagement.Domain.Entities;

namespace SupplierManagement.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(SupplierDbContext context)
    {
        // Asegurar que la base de datos y tablas existan
        await context.Database.EnsureCreatedAsync();

        if (await context.Suppliers.AnyAsync())
        {
            return; // Ya tiene datos cargados
        }

        var suppliers = new List<Supplier>
        {
            new()
            {
                LegalName = "ALICORP S.A.A.",
                TradeName = "ALICORP",
                TaxId = "20100055237",
                PhoneNumber = "+51 1 315-0800",
                Email = "contacto@alicorp.com.pe",
                Website = "https://www.alicorp.com.pe",
                PhysicalAddress = "Av. Argentina 4793, Callao, Lima",
                Country = "Perú",
                AnnualRevenue = 3500000000.00m,
                LastEditedAt = DateTime.UtcNow.AddHours(-1),
                LegalRepresentatives = new List<LegalRepresentative>
                {
                    new()
                    {
                        Forename = "Alfredo",
                        FamilyName = "Perez",
                        Email = "aperez@alicorp.com.pe",
                        DocumentNumber = "09876543"
                    }
                }
            },
            new()
            {
                LegalName = "CREDICORP CAPITAL PERU S.A.A.",
                TradeName = "CREDICORP",
                TaxId = "20549075763",
                PhoneNumber = "+51 1 313-2000",
                Email = "compliance@credicorpcapital.com",
                Website = "https://www.credicorpcapital.com",
                PhysicalAddress = "Av. El Derby 055, Edificio Cronos, Torre 4, Piso 9, Santiago de Surco",
                Country = "Perú",
                AnnualRevenue = 850000000.00m,
                LastEditedAt = DateTime.UtcNow.AddMinutes(-30),
                LegalRepresentatives = new List<LegalRepresentative>
                {
                    new()
                    {
                        Forename = "Eduardo",
                        FamilyName = "Gomez",
                        Email = "egomez@credicorp.com",
                        DocumentNumber = "12345678"
                    }
                }
            },
            new()
            {
                LegalName = "CONSORCIO MINERO DEL SUR S.A.C.",
                TradeName = "CONSORCIO",
                TaxId = "20456789012",
                PhoneNumber = "+57 1 600-4400",
                Email = "licitaciones@consorciominero.com",
                Website = "https://www.consorciominero.com",
                PhysicalAddress = "Carrera 7 No. 71-21, Torre B, Piso 12, Bogotá",
                Country = "Colombia",
                AnnualRevenue = 145000000.00m,
                LastEditedAt = DateTime.UtcNow,
                LegalRepresentatives = new List<LegalRepresentative>
                {
                    new()
                    {
                        Forename = "Carlos",
                        FamilyName = "Mendoza",
                        Email = "cmendoza@consorciominero.com",
                        DocumentNumber = "78901234"
                    }
                }
            }
        };

        await context.Suppliers.AddRangeAsync(suppliers);
        await context.SaveChangesAsync();
    }
}
