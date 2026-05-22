using BackendChallengeFord.Data;
using BackendChallengeFord.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendChallengeFord.Repositories;

public class VeiculoRepository
{
    private readonly AppDbContext _context;

    public VeiculoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Veiculo>> BuscarPorClienteAsync(int clienteId)
    {
        return await _context.Veiculos
            .Where(v => v.ClienteId == clienteId)
            .ToListAsync();
    }

    public async Task<Veiculo?> BuscarPorIdAsync(int id, int clienteId)
    {
        return await _context.Veiculos
            .FirstOrDefaultAsync(v => v.Id == id && v.ClienteId == clienteId);
    }

    public async Task<bool> PlacaExisteAsync(string placa)
    {
        return await _context.Veiculos
            .AnyAsync(v => v.Placa == placa);
    }

    public async Task AdicionarAsync(Veiculo veiculo)
    {
        _context.Veiculos.Add(veiculo);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(Veiculo veiculo)
    {
        _context.Veiculos.Update(veiculo);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarKmAsync(Veiculo veiculo, int novoKm)
    {
        veiculo.KmAtual = novoKm;
        _context.Veiculos.Update(veiculo);
        await _context.SaveChangesAsync();
    }
}
