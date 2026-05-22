using System.ComponentModel.DataAnnotations;
using BackendChallengeFord.Models;

namespace BackendChallengeFord.DTOs;

public class CriarManutencaoDTO
{
    [Required(ErrorMessage = "VeiculoId é obrigatório.")]
    [Range(1, int.MaxValue, ErrorMessage = "VeiculoId inválido.")]
    public int VeiculoId { get; set; }

    [Required(ErrorMessage = "Descrição é obrigatória.")]
    [MinLength(5, ErrorMessage = "Descrição deve ter no mínimo 5 caracteres.")]
    [MaxLength(500, ErrorMessage = "Descrição deve ter no máximo 500 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    [EnumDataType(typeof(TipoManutencao), ErrorMessage = "Tipo de manutenção inválido.")]
    public TipoManutencao Tipo { get; set; }

    [Required(ErrorMessage = "Data de realização é obrigatória.")]
    public DateTime DataRealizacao { get; set; }

    [Range(0, 9999999, ErrorMessage = "KM inválido.")]
    public int KmNaRealizacao { get; set; }

    [Range(0, 999999.99, ErrorMessage = "Custo inválido.")]
    public decimal Custo { get; set; }
}

public class ManutencaoResponseDTO
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateTime DataRealizacao { get; set; }
    public int KmNaRealizacao { get; set; }
    public decimal Custo { get; set; }
    public int PontosGerados { get; set; }
}
