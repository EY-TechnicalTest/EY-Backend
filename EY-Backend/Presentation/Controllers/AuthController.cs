using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace EY_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                success = false,
                message = "Debe proporcionar usuario y contraseña."
            });
        }

        // Demo credential validation (in Phase 4 / full IAM this connects to SQL Server)
        if (request.Password.Length < 4)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Credenciales inválidas."
            });
        }

        var token = GenerateJwtToken(request.Username);

        return Ok(new
        {
            success = true,
            message = "Autenticación exitosa.",
            token,
            username = request.Username,
            tokenType = "Bearer"
        });
    }

    private string GenerateJwtToken(string username)
    {
        var secretKey = _configuration["Jwt:Key"] ?? "EY-Technical-Security-Key-Super-Secret-2026!*#";
        var issuer = _configuration["Jwt:Issuer"] ?? "EY-Backend";
        var audience = _configuration["Jwt:Audience"] ?? "EY-TechnicalTest-Client";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "ComplianceOfficer")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
