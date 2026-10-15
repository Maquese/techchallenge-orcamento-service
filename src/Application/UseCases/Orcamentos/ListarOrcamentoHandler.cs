using Application.Interfaces;
using Application.Models.Responses;

namespace Application.UseCases.Orcamentos;

public class ListarOrcamentoHandler
{
    private readonly OrcamentoRepository _orcamentoRepository;

    public ListarOrcamentoHandler(OrcamentoRepository orcamentoRepository)
    {
        _orcamentoRepository = orcamentoRepository;
    }
    public async Task<BaseResponse> Handle()
    {
        var orcamentos = await _orcamentoRepository.ListarAtivos();

        return new BaseResponse
        {
            Success = true,
            Message = "Listado com sucesso",
            Data = orcamentos.Select(o => new ListOrcamentoResponse
            {
                Id = o.Id,
                OrdemServicoId = o.OrdemServicoId,
                Valor = o.ValorTotal,
                OrcamentoAprovado = o.OrcamentoAprovado,
                OrcamentoPago = o.OrcamentoPago,
                DataCadastro = o.DataCadastro
            }).ToList()
        };
    }
}
