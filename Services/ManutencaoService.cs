using BackendChallengeFord.Controllers;
using BackendChallengeFord.Models;
using BackendChallengeFord.Repositories;
using BackendChallengeFord.DTOs;


namespace BackendChallengeFord.Services
{
    public class ManutencaoService
    {
        private readonly ManutencaoRepository _manutencaoRepository;
        private readonly VeiculoRepository _veiculoRepository;
        private readonly PontosRepository _pontosRepository;

        public ManutencaoService(
            ManutencaoRepository manutencaoRepository,
            VeiculoRepository veiculoRepository,
            PontosRepository pontosRepository)
        {
            _manutencaoRepository = manutencaoRepository;
            _veiculoRepository = veiculoRepository;
            _pontosRepository = pontosRepository;
        }

        private int CalcularPontos(TipoManutencao tipo, decimal custo)
        {
            return tipo switch
            {
                TipoManutencao.RevisaoFord => 500,
                TipoManutencao.MecanicaParceira => 200,
                TipoManutencao.RegistroManual => 50,
                _ => 0
            };
        }

        public async Task<List<ManutencaoResponseDTO>> ListarAsync(int veiculoId, int clienteId)
        {
            var veiculo = await _veiculoRepository.BuscarPorIdAsync(veiculoId, clienteId);
            if (veiculo == null) return new List<ManutencaoResponseDTO>();

            var manutencoes = await _manutencaoRepository.BuscarPorVeiculoAsync(veiculoId);

            return manutencoes.Select(m => new ManutencaoResponseDTO
            {
                Id = m.Id,
                Descricao = m.Descricao,
                Tipo = m.Tipo.ToString(),
                DataRealizacao = m.DataRealizacao,
                KmNaRealizacao = m.KmNaRealizacao,
                Custo = m.Custo,
                PontosGerados = m.PontosGerados
            }).ToList();
        }

        public async Task<(bool sucesso, string mensagem, ManutencaoResponseDTO? manutencao)> CriarAsync(
            CriarManutencaoDTO dto, int clienteId)
        {
            var veiculo = await _veiculoRepository.BuscarPorIdAsync(dto.VeiculoId, clienteId);
            if (veiculo == null)
                return (false, "Veículo não encontrado.", null);

            var pontos = CalcularPontos(dto.Tipo, dto.Custo);

            var manutencao = new Manutencao
            {
                VeiculoId = dto.VeiculoId,
                Descricao = dto.Descricao,
                Tipo = dto.Tipo,
                DataRealizacao = dto.DataRealizacao,
                KmNaRealizacao = dto.KmNaRealizacao,
                Custo = dto.Custo,
                PontosGerados = pontos
            };

            await _manutencaoRepository.AdicionarAsync(manutencao);

            var saldo = await _pontosRepository.BuscarPorClienteAsync(clienteId);
            if (saldo != null)
            {
                saldo.SaldoAtual += pontos;
                saldo.TotalAcumulado += pontos;
                await _pontosRepository.AtualizarAsync(saldo);
            }

            var response = new ManutencaoResponseDTO
            {
                Id = manutencao.Id,
                Descricao = manutencao.Descricao,
                Tipo = manutencao.Tipo.ToString(),
                DataRealizacao = manutencao.DataRealizacao,
                KmNaRealizacao = manutencao.KmNaRealizacao,
                Custo = manutencao.Custo,
                PontosGerados = manutencao.PontosGerados
            };

            return (true, $"Manutenção registrada. Você ganhou {pontos} pontos!", response);
        }
    }
}
