# EY Backend - Supplier Management & Risk Screening API

Backend en **.NET 8 (C#)** diseñado con **Clean Architecture** para la gestión de proveedores corporativos y la ejecución de procesos de debida diligencia (*screening*) en listas de riesgo (SMV Perú, SECOP I Colombia e INTERPOL).

---

## Arquitectura del Backend

La solución está modularizada siguiendo principios de arquitectura limpia y separación de responsabilidades:

```text
EY.TechnicalTest/
├── EY-Backend/                   # Capa de Presentación principal y Host de la Web API
│   ├── Middlewares/              # ExceptionHandlingMiddleware (Manejo centralizado de errores)
│   ├── Presentation/Controllers/ # AuthController (Autenticación JWT)
│   ├── Program.cs                # Inyección de dependencias, Rate Limiting, JWT y Swagger
│   ├── appsettings.json          # Cadena de conexión, JWT settings, Rate Limiting
│   └── appsettings.Production.json
│
├── SupplierManagement/           # Módulo de Dominio y Persistencia de Proveedores
│   ├── Domain/Entities/          # Supplier.cs, LegalRepresentative.cs
│   ├── Application/              # DTOs, ISupplierService, SupplierService, Validaciones
│   ├── Infrastructure/           # SupplierDbContext (EF Core 8), DbInitializer (Seed Data)
│   └── Presentation/Controllers/ # SuppliersController.cs (CRUD REST)
│
├── RiskCompliance/               # Módulo de Cumplimiento y Debida Diligencia
│   ├── Domain/                   # Interfaces (IScreeningService, IScrapers), Entidades de Resultados
│   ├── Application/              # ScreeningService.cs (Orquestador de consultas paralelas)
│   ├── Infrastructure/Scrapers/  # SmvPlaywrightScraper, SecopScraper, InterpolScraper
│   └── Presentation/Controllers/ # ScreeningController.cs
│
├── EY_Database_Script.sql        # Script DDL de SQL Server con carga de datos semilla
└── EY_Technical_Screening_Postman_Collection.json # Colección de pruebas de Postman
```

---

## Mecanismos de Seguridad y Resiliencia

### 1. Autenticación JWT (JSON Web Token)
- Las rutas de modificación y consulta sensible requieren el encabezado `Authorization: Bearer <TOKEN>`.
- Token generado en `POST /api/auth/login` con expiración configurable y algoritmo de firma simétrica HMAC-SHA256.

### 2. Rate Limiting (Protección contra saturación y DoS)
- Implementado con la librería nativa `System.Threading.RateLimiting` en ASP.NET Core 8.
- **Límite**: Máximo **20 solicitudes por minuto** por dirección IP.
- En caso de exceder el límite, el servidor responde inmediatamente con código **HTTP 429 Too Many Requests** y un cuerpo estructurado:
  ```json
  {
    "success": false,
    "message": "Has excedido el límite máximo de 20 llamadas por minuto. Por favor, espera antes de realizar más solicitudes.",
    "statusCode": 429,
    "timestamp": "2026-09-25T14:30:00Z"
  }
  ```

### 3. Middleware de Excepciones Globales
- `ExceptionHandlingMiddleware` captura cualquier excepción no controlada, registra la traza y emite una respuesta JSON limpia en formato estandarizado sin filtrar información confidencial del servidor.

### 4. Política de CORS
- Configurada para admitir tanto orígenes locales (`http://localhost:5173`, `http://localhost:3000`) como cualquier origen de despliegue en la nube mediante `SetIsOriginAllowed`.

---

## Modelo de Datos (Entity Framework Core / SQL Server)

El modelo de base de datos implementa una relación **uno a muchos (1:N)** con eliminación en cascada:

### Tabla `Suppliers`
| Campo | Tipo | Restricción | Descripción |
|---|---|---|---|
| `Id` | INT | PK, Identity | Identificador único del proveedor |
| `LegalName` | NVARCHAR(200) | NOT NULL | Razón social de la empresa |
| `TradeName` | NVARCHAR(200) | NOT NULL | Nombre comercial |
| `TaxId` | NVARCHAR(11) | NOT NULL, UNIQUE INDEX | RUC o número de identificación fiscal (11 dígitos) |
| `PhoneNumber` | NVARCHAR(30) | NOT NULL | Número de teléfono de contacto |
| `Email` | NVARCHAR(150) | NOT NULL | Correo electrónico de contacto |
| `Website` | NVARCHAR(250) | NOT NULL | Dirección del portal web |
| `PhysicalAddress` | NVARCHAR(300) | NOT NULL | Dirección física de la sede |
| `Country` | NVARCHAR(100) | NOT NULL | País de constitución |
| `AnnualRevenue` | DECIMAL(18,2) | NOT NULL | Facturación anual en moneda base |
| `LastEditedAt` | DATETIME2 | NOT NULL | Marca de tiempo UTC de última actualización |

### Tabla `LegalRepresentatives`
| Campo | Tipo | Restricción | Descripción |
|---|---|---|---|
| `Id` | INT | PK, Identity | Identificador único del representante |
| `SupplierId` | INT | FK -> Suppliers(Id) ON DELETE CASCADE | Proveedor al que pertenece |
| `Forename` | NVARCHAR(100) | NOT NULL | Nombres |
| `FamilyName` | NVARCHAR(100) | NOT NULL | Apellidos |
| `Email` | NVARCHAR(150) | NULL | Correo electrónico del representante |
| `DocumentNumber` | NVARCHAR(20) | NULL | Documento de identidad (DNI, Cédula, Pasaporte) |

---

## Endpoints de la API REST

### 1. Autenticación (`/api/auth`)
| Método | Endpoint | Autenticación | Descripción |
|---|---|---|---|
| `POST` | `/api/auth/login` | Pública | Inicia sesión con usuario y contraseña, devuelve token JWT |

**Body de ejemplo:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

---

### 2. Gestión de Proveedores (`/api/suppliers`)
| Método | Endpoint | Autenticación | Descripción |
|---|---|---|---|
| `GET` | `/api/suppliers` | Bearer JWT | Obtiene la lista de todos los proveedores con sus representantes |
| `GET` | `/api/suppliers/{id}` | Bearer JWT | Obtiene un proveedor específico por su ID |
| `POST` | `/api/suppliers` | Bearer JWT | Registra un nuevo proveedor con sus representantes legales |
| `PUT` | `/api/suppliers/{id}` | Bearer JWT | Actualiza la información de un proveedor existente |
| `DELETE` | `/api/suppliers/{id}` | Bearer JWT | Elimina un proveedor y sus representantes asociados |

**Body de creación (`POST /api/suppliers`):**
```json
{
  "legalName": "NUEVA EMPRESA LOGISTICA S.A.C.",
  "tradeName": "LOGISTICA GLOBAL",
  "taxId": "20601234567",
  "phoneNumber": "+51 987654321",
  "email": "contacto@logisticaglobal.com",
  "website": "https://www.logisticaglobal.com",
  "physicalAddress": "Av. Javier Prado Este 4200, Surco, Lima",
  "country": "Perú",
  "annualRevenue": 15000000.00,
  "legalRepresentatives": [
    {
      "forename": "Katherine Steffany",
      "familyName": "Rosado Muñoz",
      "email": "krosado@logisticaglobal.com",
      "documentNumber": "74859612"
    }
  ]
}
```

---

### 3. Compliance & Screening (`/api/screening`)
| Método | Endpoint | Autenticación | Descripción |
|---|---|---|---|
| `POST` | `/api/screening/screen` | Bearer JWT | Ejecuta el screening bajo demanda enviando entidad y representante en el body |
| `GET` | `/api/suppliers/{id}/screening` | Bearer JWT | Ejecuta el screening automático a partir de un proveedor registrado en la BD |

**Respuesta de Screening:**
```json
{
  "entityName": "ALICORP S.A.A.",
  "taxId": "20100055237",
  "riskLevel": "Low",
  "overallVerdict": "Apto para contratación. No se encontraron alertas críticas en las fuentes consultadas.",
  "smvResults": [],
  "secopResults": [],
  "interpolResults": [],
  "screenedAt": "2026-09-25T14:35:10.123Z"
}
```

---

## Configuración y Ejecución Local

### Paso 1: Configurar la Cadena de Conexión
En `Backend/EY.TechnicalTest/EY-Backend/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=EYSupplierDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### Paso 2: Crear la Base de Datos
Puedes ejecutar el script `EY_Database_Script.sql` en SQL Server Management Studio (SSMS) o Azure Data Studio. Alternativamente, la aplicación cuenta con inicialización automática mediante `DbInitializer.InitializeAsync()`.

### Paso 3: Compilar y Ejecutar
```bash
cd Backend/EY.TechnicalTest/EY-Backend
dotnet restore
dotnet build
dotnet run
```

Acceso a Swagger UI:
[`http://localhost:5000/index.html`](http://localhost:5000/index.html)

---

## Pruebas con Postman

Se incluye el archivo `EY_Technical_Screening_Postman_Collection.json` en la raíz del backend:
1. Abrir Postman e importar el archivo JSON.
2. La colección incluye:
   - Login y asignación automática del token JWT en variables de entorno.
   - Peticiones CRUD completas para proveedores.
   - Peticiones de Screening para SMV, SECOP I e INTERPOL.
   - Prueba de saturación de Rate Limiting (para verificar el código 429).