-- =========================================================================
-- EY Technical Test - Script de Creación de Base de Datos y Datos Semilla
-- Base de datos: EYSupplierDb (SQL Server)
-- =========================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'EYSupplierDb')
BEGIN
    CREATE DATABASE [EYSupplierDb];
END
GO

USE [EYSupplierDb];
GO

-- 1. Tabla de Proveedores (Suppliers)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
BEGIN
    CREATE TABLE [dbo].[Suppliers] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [LegalName] NVARCHAR(200) NOT NULL,
        [TradeName] NVARCHAR(200) NOT NULL,
        [TaxId] NVARCHAR(11) NOT NULL,
        [PhoneNumber] NVARCHAR(30) NOT NULL,
        [Email] NVARCHAR(150) NOT NULL,
        [Website] NVARCHAR(250) NOT NULL,
        [PhysicalAddress] NVARCHAR(300) NOT NULL,
        [Country] NVARCHAR(100) NOT NULL,
        [AnnualRevenue] DECIMAL(18,2) NOT NULL,
        [LastEditedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE UNIQUE INDEX [IX_Suppliers_TaxId] ON [dbo].[Suppliers] ([TaxId]);
END
GO

-- 2. Tabla de Representantes Legales (LegalRepresentatives)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LegalRepresentatives')
BEGIN
    CREATE TABLE [dbo].[LegalRepresentatives] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SupplierId] INT NOT NULL,
        [Forename] NVARCHAR(100) NOT NULL,
        [FamilyName] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(150) NULL,
        [DocumentNumber] NVARCHAR(20) NULL,
        CONSTRAINT [FK_LegalRepresentatives_Suppliers] FOREIGN KEY ([SupplierId]) 
            REFERENCES [dbo].[Suppliers] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_LegalRepresentatives_SupplierId] ON [dbo].[LegalRepresentatives] ([SupplierId]);
END
GO

-- 3. Carga Inicial de Datos (Seed Data)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Suppliers])
BEGIN
    SET IDENTITY_INSERT [dbo].[Suppliers] ON;

    INSERT INTO [dbo].[Suppliers] ([Id], [LegalName], [TradeName], [TaxId], [PhoneNumber], [Email], [Website], [PhysicalAddress], [Country], [AnnualRevenue], [LastEditedAt])
    VALUES 
    (1, N'ALICORP S.A.A.', N'ALICORP', N'20100055237', N'+51 1 315-0800', N'contacto@alicorp.com.pe', N'https://www.alicorp.com.pe', N'Av. Argentina 4793, Callao, Lima', N'Perú', 3500000000.00, DATEADD(hour, -2, SYSUTCDATETIME())),
    (2, N'CREDICORP CAPITAL PERU S.A.A.', N'CREDICORP', N'20549075763', N'+51 1 313-2000', N'compliance@credicorpcapital.com', N'https://www.credicorpcapital.com', N'Av. El Derby 055, Edificio Cronos, Torre 4, Piso 9, Santiago de Surco', N'Perú', 850000000.00, DATEADD(hour, -1, SYSUTCDATETIME())),
    (3, N'CONSORCIO MINERO DEL SUR S.A.C.', N'CONSORCIO', N'20456789012', N'+57 1 600-4400', N'licitaciones@consorciominero.com', N'https://www.consorciominero.com', N'Carrera 7 No. 71-21, Torre B, Piso 12, Bogotá', N'Colombia', 145000000.00, SYSUTCDATETIME());

    SET IDENTITY_INSERT [dbo].[Suppliers] OFF;

    SET IDENTITY_INSERT [dbo].[LegalRepresentatives] ON;

    INSERT INTO [dbo].[LegalRepresentatives] ([Id], [SupplierId], [Forename], [FamilyName], [Email], [DocumentNumber])
    VALUES
    (1, 1, N'Alfredo', N'Perez', N'aperez@alicorp.com.pe', N'09876543'),
    (2, 2, N'Eduardo', N'Gomez', N'egomez@credicorp.com', N'12345678'),
    (3, 3, N'Carlos', N'Mendoza', N'cmendoza@consorciominero.com', N'78901234');

    SET IDENTITY_INSERT [dbo].[LegalRepresentatives] OFF;
END
GO
