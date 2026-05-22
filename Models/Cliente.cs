namespace BackendChallengeFord.Models;

public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;
    public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
    public Pontos? Pontos { get; set; }
}
