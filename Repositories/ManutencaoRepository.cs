using BackendChallengeFord.Data;
using BackendChallengeFord.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendChallengeFord.Controllers;

public class ManutencaoRepository
{
    private readonly AppDbContext _context;

    public ManutencaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Manutencao>> BuscarPorVeiculoAsync(int veiculoId)
    {
        return await _context.Manutencoes
            .Where(m => m.VeiculoId == veiculoId)
            .OrderByDescending(m => m.DataRealizacao)
            .ToListAsync();
    }

    public async Task AdicionarAsync(Manutencao manutencao)
    {
        _context.Manutencoes.Add(manutencao);
        await _context.SaveChangesAsync();
    }
}
