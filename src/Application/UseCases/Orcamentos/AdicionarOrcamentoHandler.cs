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

    public AdicionarOrcamentoHandler(OrcamentoRepository orcamentoRepository)//, OrdemServicoRepository ordemServicoRepository)
    {
        _orcamentoRepository = orcamentoRepository;
        //_ordemServicoRepository = ordemServicoRepository;
    }
    
    public async Task<BaseResponse> Handle(AddOrcamentoRequest model)
    {
        // var ordemServico = await _ordemServicoRepository.ObterPorId(model.OrdemServicoId);
        // if (ordemServico == null)
        // {
        //     throw new DomainException($"Ordem de serviço com ID {model.OrdemServicoId} não encontrada.");
        // }

        // if(!ordemServico.EstaAtivo())
        //     throw new DomainException("Ordem de serviço inativa");

        // if(ordemServico.Status != "Aguardando aprovação")
        //     throw new DomainException($"Ordem de servico no status incorreto {ordemServico.Status}");
        //ordemServico.CalcularValorOrdemServico().Value
        // var orcamentoAindaEmAberto = await _orcamentoRepository.EncontrarOrcamentoPorOrdemServico(model.OrdemServicoId);
        // if(orcamentoAindaEmAberto != null)
        //     throw new Exception("Ainda há orcamento em aberto, finalize primeiro");

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
