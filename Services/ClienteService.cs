using BackendChallengeFord.DTOs;
using BackendChallengeFord.Repositories;

namespace BackendChallengeFord.Services;

public class ClienteService
{
    private readonly ClienteRepository _clienteRepository;

    public ClienteService(ClienteRepository clienteRepository)
    {
        _clienteRepository = clienteRepository;
    }

    public async Task<ClientePerfilDTO?> BuscarPerfilAsync(int clienteId)
    {
        var cliente = await _clienteRepository.BuscarPerfilAsync(clienteId);
        if (cliente == null) return null;

        return new ClientePerfilDTO
        {
            Id = cliente.Id,
            Nome = cliente.Nome,
            Email = cliente.Email,
            CPF = cliente.CPF,
            DataCadastro = cliente.DataCadastro,
            SaldoPontos = cliente.Pontos?.SaldoAtual ?? 0,
            TotalPontosAcumulados = cliente.Pontos?.TotalAcumulado ?? 0,
            TotalVeiculos = cliente.Veiculos.Count
        };
    }
}
