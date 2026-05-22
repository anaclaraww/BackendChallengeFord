using System.ComponentModel.DataAnnotations;
using BackendChallengeFord.Models;

namespace BackendChallengeFord.DTOs;

public class RegisterDTO
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MinLength(3, ErrorMessage = "Nome deve ter no mínimo 3 caracteres.")]
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres.")]
    [RegularExpression(@"^[\p{L}\s]+$", ErrorMessage = "Nome deve conter apenas letras.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    [MaxLength(150, ErrorMessage = "Email muito longo.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    [MinLength(8, ErrorMessage = "Senha deve ter no mínimo 8 caracteres.")]
    [MaxLength(100, ErrorMessage = "Senha muito longa.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
        ErrorMessage = "Senha deve ter maiúscula, minúscula, número e caractere especial.")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "CPF é obrigatório.")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "CPF deve ter 11 dígitos numéricos.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "CPF deve conter apenas números.")]
    public string CPF { get; set; } = string.Empty;
}
