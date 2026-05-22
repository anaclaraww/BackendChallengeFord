namespace BackendChallengeFord.DTOs;

public class ClientePerfilDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public DateTime DataCadastro { get; set; }
    public int SaldoPontos { get; set; }
    public int TotalPontosAcumulados { get; set; }
    public int TotalVeiculos { get; set; }
}
