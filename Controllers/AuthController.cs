using BackendChallengeFord.DTOs;
using BackendChallengeFord.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackendChallengeFord.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
    {
        var result = await _authService.Register(dto);

        if (result == null)
            return BadRequest(new { mensagem = "Email já cadastrado." });

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        var result = await _authService.Login(dto);

        if (result == null)
            return Unauthorized(new { mensagem = "Email ou senha inválidos." });

        return Ok(result);
    }
}
