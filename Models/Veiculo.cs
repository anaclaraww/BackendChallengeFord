namespace BackendChallengeFord.Models;

public class Veiculo
{
    public int Id { get; set; }
    public string Modelo { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public int Ano { get; set; }
    public string Chassi { get; set; } = string.Empty;
    public int KmAtual { get; set; }
    public DateTime DataCompra { get; set; }

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public ICollection<Manutencao> Manutencoes { get; set; } = new List<Manutencao>();
}
