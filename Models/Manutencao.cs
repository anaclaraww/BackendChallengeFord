namespace BackendChallengeFord.Models;

public enum TipoManutencao
{
    RevisaoFord,
    MecanicaParceira,
    RegistroManual
}

public class Manutencao
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public TipoManutencao Tipo { get; set; }
    public DateTime DataRealizacao { get; set; }
    public int KmNaRealizacao { get; set; }
    public decimal Custo { get; set; }
    public int PontosGerados { get; set; }

    public int VeiculoId { get; set; }
    public Veiculo Veiculo { get; set; } = null!;
}
