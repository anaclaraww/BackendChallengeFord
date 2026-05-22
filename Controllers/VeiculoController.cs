using BackendChallengeFord.DTOs;
using BackendChallengeFord.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendChallengeFord.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VeiculoController : ControllerBase
{
    private readonly VeiculoService _veiculoService;

    public VeiculoController(VeiculoService veiculoService)
    {
        _veiculoService = veiculoService;
    }

    private int GetClienteId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("listar")]
    public async Task<IActionResult> Listar()
    {
        var veiculos = await _veiculoService.ListarVeiculosDoClienteAsync(GetClienteId());
        return Ok(veiculos);
    }

    [HttpGet("buscar/{id}")]
    public async Task<IActionResult> Buscar(int idVeiculo)
    {
        var veiculo = await _veiculoService.BuscarVeiculoAsync(idVeiculo, GetClienteId());
        if (veiculo == null) return NotFound(new { mensagem = "Veículo não encontrado." });
        return Ok(veiculo);
    }

    [HttpPost("criar")]
    public async Task<IActionResult> Criar([FromBody] CriarVeiculoDTO dto)
    {
        var (sucesso, mensagem, veiculo) = await _veiculoService.CriarVeiculoAsync(dto, GetClienteId());
        if (!sucesso) return BadRequest(new { mensagem });
        return CreatedAtAction(nameof(Buscar), new { id = veiculo!.Id }, veiculo);
    }

    [HttpPatch("{id}/km")]
    public async Task<IActionResult> AtualizarKm(int id, [FromBody] AtualizarKmDTO dto)
    {
        var (sucesso, mensagem, veiculo) = await _veiculoService.AtualizarKmAsync(id, dto, GetClienteId());
        if (!sucesso) return BadRequest(new { mensagem });
        return Ok(new { mensagem, veiculo });
    }
}
