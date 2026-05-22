using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BackendChallengeFord.Data;
using BackendChallengeFord.DTOs;
using BackendChallengeFord.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BackendChallengeFord.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDTO?> Register(RegisterDTO dto)
    {
        
        dto.Email = dto.Email.Trim().ToLowerInvariant();
        dto.Nome = dto.Nome.Trim();
        dto.CPF = Regex.Replace(dto.CPF.Trim(), @"[^\d]", ""); 

        if (!Regex.IsMatch(dto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return null;

        if (await _context.Clientes.AnyAsync(c => c.Email == dto.Email))
        {
            _logger.LogInformation("[REGISTRO] Tentativa com email já cadastrado (mascarado)");
            return null;
        }

        var cliente = new Cliente
        {
            Nome = dto.Nome,
            Email = dto.Email,
            CPF = dto.CPF,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha, workFactor: 12)
        };

        cliente.Pontos = new Pontos { SaldoAtual = 0, TotalAcumulado = 0 };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        _logger.LogInformation("[AUDITORIA] Novo cliente registrado — Id={Id}", cliente.Id);

        return GerarToken(cliente);
    }

    public async Task<AuthResponseDTO?> Login(LoginDTO dto)
    {
        dto.Email = dto.Email.Trim().ToLowerInvariant();

        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Email == dto.Email && c.Ativo);

        var senhaCorreta = cliente != null &&
            BCrypt.Net.BCrypt.Verify(dto.Senha, cliente.SenhaHash);

        if (!senhaCorreta)
        {
            _logger.LogWarning("[ALERTA SEGURANÇA] Falha de login para email (mascarado)");
            return null;
        }

        _logger.LogInformation("[AUDITORIA] Login bem-sucedido — ClienteId={Id}", cliente!.Id);

        return GerarToken(cliente);
    }

    private AuthResponseDTO GerarToken(Cliente cliente)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, cliente.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, cliente.Email),
            new Claim(ClaimTypes.Name, cliente.Nome),
            new Claim(ClaimTypes.NameIdentifier, cliente.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("role", "cliente")
        };

        var expiration = DateTime.UtcNow.AddHours(
            double.Parse(_configuration["Jwt:ExpiresInHours"]!));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiration,
            signingCredentials: credentials
        );

        return new AuthResponseDTO
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Nome = cliente.Nome,
            Email = cliente.Email
        };
    }
}
