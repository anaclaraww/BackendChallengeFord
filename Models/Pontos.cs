namespace BackendChallengeFord.Models;

public class Pontos
{
    public int Id { get; set; }
    public int SaldoAtual { get; set; } = 0;
    public int TotalAcumulado { get; set; } = 0;

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
}
