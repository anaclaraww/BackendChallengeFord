using BackendChallengeFord.Data;
using BackendChallengeFord.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendChallengeFord.Repositories;

public class ClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> BuscarPerfilAsync(int clienteId)
    {
        return await _context.Clientes
            .Include(c => c.Pontos)
            .Include(c => c.Veiculos)
            .FirstOrDefaultAsync(c => c.Id == clienteId);
    }
}
