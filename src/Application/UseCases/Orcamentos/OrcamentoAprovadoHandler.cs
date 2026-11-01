using Application.Interfaces;
using Application.Models.Responses;

namespace Application.UseCases.Orcamentos;

public class OrcamentoAprovadoHandler
{
    private readonly OrcamentoRepository _orcamentoRepository;

    public OrcamentoAprovadoHandler(OrcamentoRepository orcamentoRepository)
    {
        _orcamentoRepository = orcamentoRepository;
    }
    public async Task<BaseResponse> Handle(int ordemServicoId)
    {
        var orcamento = await _orcamentoRepository.EncontrarOrcamentoPorOrdemServico(ordemServicoId);
        
        if(orcamento.OrcamentoAprovado.HasValue && orcamento.OrcamentoAprovado.Value && !orcamento.OrcamentoPago.HasValue)
        {
            return new BaseResponse
            {
                Success = true,
                Data = new {OrcamentoAprovado = true },
                Message = "Ok"
            };
        }
        else
        {
            return new BaseResponse
            {
                Success = true,
                Data = new {OrcamentoAprovado = false },
                Message = "Ok"
            };
        }
    }
}
