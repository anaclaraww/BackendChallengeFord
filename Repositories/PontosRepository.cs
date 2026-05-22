using BackendChallengeFord.Data;
using BackendChallengeFord.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendChallengeFord.Repositories
{
    public class PontosRepository
    {
        private readonly AppDbContext _context;

        public PontosRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Pontos?> BuscarPorClienteAsync(int clienteId)
        {
            return await _context.Pontos
                .FirstOrDefaultAsync(p => p.ClienteId == clienteId);
        }

        public async Task AtualizarAsync(Pontos pontos)
        {
            _context.Pontos.Update(pontos);
            await _context.SaveChangesAsync();
        }
    }
}
