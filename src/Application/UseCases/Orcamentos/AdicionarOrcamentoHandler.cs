using Application.Interfaces;
using Application.Models;
using Application.Models.Requests;
using Application.Models.Responses;
using Domain.Aggregates;
using Domain.Exceptions;

namespace Application.UseCases.Orcamentos;

public class AdicionarOrcamentoHandler
{
    private readonly OrcamentoRepository _orcamentoRepository;
    //private readonly OrdemServicoRepository _ordemServicoRepository;

    public AdicionarOrcamentoHandler(OrcamentoRepository orcamentoRepository)
    {
        _orcamentoRepository = orcamentoRepository;
    }
    
    public async Task<BaseResponse> Handle(AddOrcamentoRequest model)
    {

        if(await _orcamentoRepository.ExisteOrcamentoEmAbertoParaOrdemdeSerivico(model.OrdemServicoId))
            throw new DomainException($"Ordem de seriviço {model.OrdemServicoId} já tem Orçamento em aberto!");

        var orcamento = new Orcamento(model.OrdemServicoId, model.ValorTotal,model.Observacao);
        await _orcamentoRepository.Adicionar(orcamento);
        return new BaseResponse
        {
            Success = true,
            Message = "Criando com sucesso",
            Data = orcamento.Id
        };
    }
}
