using System.ComponentModel.DataAnnotations;

namespace BackendChallengeFord.DTOs;

public class CriarVeiculoDTO
{
    [Required(ErrorMessage = "Modelo é obrigatório.")]
    [MinLength(2, ErrorMessage = "Modelo muito curto.")]
    [MaxLength(100, ErrorMessage = "Modelo muito longo.")]
    [RegularExpression(@"^[\w\s\-]+$", ErrorMessage = "Modelo contém caracteres inválidos.")]
    public string Modelo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Placa é obrigatória.")]
    [RegularExpression(@"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$",
        ErrorMessage = "Placa inválida. Use o formato ABC1234 ou ABC1D23.")]
    public string Placa { get; set; } = string.Empty;

    [Range(1900, 2100, ErrorMessage = "Ano inválido.")]
    public int Ano { get; set; }

    [Required(ErrorMessage = "Chassi é obrigatório.")]
    [StringLength(17, MinimumLength = 17, ErrorMessage = "Chassi deve ter 17 caracteres.")]
    [RegularExpression(@"^[A-HJ-NPR-Z0-9]{17}$", ErrorMessage = "Chassi inválido.")]
    public string Chassi { get; set; } = string.Empty;

    [Range(0, 9999999, ErrorMessage = "KM inválido.")]
    public int KmAtual { get; set; }

    [Required(ErrorMessage = "Data de compra é obrigatória.")]
    public DateTime DataCompra { get; set; }
}

public class VeiculoResponseDTO
{
    public int Id { get; set; }
    public string Modelo { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public int Ano { get; set; }
    public int KmAtual { get; set; }
    public DateTime DataCompra { get; set; }
}

public class AtualizarKmDTO
{
    [Required(ErrorMessage = "KM é obrigatório.")]
    [Range(0, 9999999, ErrorMessage = "KM não pode ser negativo ou exceder o limite.")]
    public int KmAtual { get; set; }
}
