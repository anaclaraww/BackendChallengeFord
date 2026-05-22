using BackendChallengeFord.DTOs;
using BackendChallengeFord.Models;
using BackendChallengeFord.Repositories;

namespace BackendChallengeFord.Services;

public class VeiculoService
{
    private readonly VeiculoRepository _veiculoRepository;

    public VeiculoService(VeiculoRepository veiculoRepository)
    {
        _veiculoRepository = veiculoRepository;
    }

    public async Task<List<VeiculoResponseDTO>> ListarVeiculosDoClienteAsync(int clienteId)
    {
        var veiculos = await _veiculoRepository.BuscarPorClienteAsync(clienteId);

        return veiculos.Select(v => new VeiculoResponseDTO
        {
            Id = v.Id,
            Modelo = v.Modelo,
            Placa = v.Placa,
            Ano = v.Ano,
            KmAtual = v.KmAtual,
            DataCompra = v.DataCompra
        }).ToList();
    }

    public async Task<VeiculoResponseDTO?> BuscarVeiculoAsync(int id, int clienteId)
    {
        var veiculo = await _veiculoRepository.BuscarPorIdAsync(id, clienteId);
        if (veiculo == null) return null;

        return new VeiculoResponseDTO
        {
            Id = veiculo.Id,
            Modelo = veiculo.Modelo,
            Placa = veiculo.Placa,
            Ano = veiculo.Ano,
            KmAtual = veiculo.KmAtual,
            DataCompra = veiculo.DataCompra
        };
    }

    public async Task<(bool sucesso, string mensagem, VeiculoResponseDTO? veiculo)> CriarVeiculoAsync(CriarVeiculoDTO dto, int clienteId)
    {
        if (await _veiculoRepository.PlacaExisteAsync(dto.Placa))
            return (false, "Placa já cadastrada.", null);

        var veiculo = new Veiculo
        {
            Modelo = dto.Modelo,
            Placa = dto.Placa,
            Ano = dto.Ano,
            Chassi = dto.Chassi,
            KmAtual = dto.KmAtual,
            DataCompra = dto.DataCompra,
            ClienteId = clienteId
        };

        await _veiculoRepository.AdicionarAsync(veiculo);

        var response = new VeiculoResponseDTO
        {
            Id = veiculo.Id,
            Modelo = veiculo.Modelo,
            Placa = veiculo.Placa,
            Ano = veiculo.Ano,
            KmAtual = veiculo.KmAtual,
            DataCompra = veiculo.DataCompra
        };

        return (true, "Veículo cadastrado com sucesso.", response);
    }

    public async Task<(bool sucesso, string mensagem, VeiculoResponseDTO? veiculo)> AtualizarKmAsync(
    int veiculoId, AtualizarKmDTO dto, int clienteId)
    {
        var veiculo = await _veiculoRepository.BuscarPorIdAsync(veiculoId, clienteId);
        if (veiculo == null)
            return (false, "Veículo não encontrado.", null);

        if (dto.KmAtual < veiculo.KmAtual)
            return (false, $"KM não pode ser menor que o atual ({veiculo.KmAtual} km).", null);

        await _veiculoRepository.AtualizarKmAsync(veiculo, dto.KmAtual);

        var response = new VeiculoResponseDTO
        {
            Id = veiculo.Id,
            Modelo = veiculo.Modelo,
            Placa = veiculo.Placa,
            Ano = veiculo.Ano,
            KmAtual = veiculo.KmAtual,
            DataCompra = veiculo.DataCompra
        };

        return (true, "KM atualizado com sucesso.", response);
    }
}
