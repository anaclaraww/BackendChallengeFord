namespace BackendChallengeFord.DTOs;

public class ErroResponseDTO
{
    public int Status { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string? Detalhe { get; set; }
}
