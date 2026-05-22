using BackendChallengeFord.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendChallengeFord.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClienteController : ControllerBase
{
    private readonly ClienteService _clienteService;

    public ClienteController(ClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    private int GetClienteId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("perfil")]
    public async Task<IActionResult> Perfil()
    {
        var perfil = await _clienteService.BuscarPerfilAsync(GetClienteId());
        if (perfil == null) return NotFound(new { mensagem = "Cliente não encontrado." });
        return Ok(perfil);
    }


}
