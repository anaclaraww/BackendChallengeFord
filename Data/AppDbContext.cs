using BackendChallengeFord.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace BackendChallengeFord.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
        public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Veiculo> Veiculos { get; set; }
    public DbSet<Manutencao> Manutencoes { get; set; }
    public DbSet<Pontos> Pontos { get; set; }
}
