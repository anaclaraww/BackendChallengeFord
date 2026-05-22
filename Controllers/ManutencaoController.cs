using BackendChallengeFord.DTOs;
using BackendChallengeFord.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendChallengeFord.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ManutencaoController : ControllerBase
{
    private readonly ManutencaoService _manutencaoService;

    public ManutencaoController(ManutencaoService manutencaoService)
    {
        _manutencaoService = manutencaoService;
    }

    private int GetClienteId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("veiculo/{veiculoId}")]
    public async Task<IActionResult> Listar(int veiculoId)
    {
        var manutencoes = await _manutencaoService.ListarAsync(veiculoId, GetClienteId());
        return Ok(manutencoes);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarManutencaoDTO dto)
    {
        var (sucesso, mensagem, manutencao) = await _manutencaoService.CriarAsync(dto, GetClienteId());
        if (!sucesso) return BadRequest(new { mensagem });
        return Ok(new { mensagem, manutencao });
    }
}