using Application.Interfaces;
using Application.Models.Responses;
using Domain.Exceptions;

namespace Application.UseCases.Orcamentos;

public class AprovarOrcamentoHandler
{
    private readonly OrcamentoRepository _orcamentoRepository;
    //private readonly OrdemServicoRepository _ordemServicoRepository;

    public AprovarOrcamentoHandler(OrcamentoRepository orcamentoRepository)//, OrdemServicoRepository ordemServicoRepository)
    {
        _orcamentoRepository = orcamentoRepository;
        //_ordemServicoRepository = ordemServicoRepository;
    }
    
     public async Task<BaseResponse> Handle(int orcamentoId)
    {
        var orcamento = await _orcamentoRepository.ObterPorId(orcamentoId);
        if (orcamento == null)
        {
            throw new DomainException($"Orçamento com ID {orcamentoId} não encontrado.");
        }
        
        // var ordemServico = await _ordemServicoRepository.ObterPorId(orcamento.OrdemServicoId);
        // if (ordemServico == null)
        // {
        //     throw new DomainException($"Ordem de serviço com ID {orcamento.OrdemServicoId} não encontrada.");
        // }

        // if(ordemServico.Status != "Aguardando aprovação")
        // {
        //     throw new DomainException($"Ordem de serviço com ID {ordemServico.Id} não está no status 'Aguardando aprovação' para aprovação de orçamento.");
        // }

        if(!orcamento.EstaAtivo())
            throw new  DomainException("Orcamento inativo");

        if(orcamento.DataDecisaoClientePagamento != null)
        {
            throw new DomainException($"Orçamento com ID {orcamentoId} já foi decidido pelo cliente.");
        }

        if(orcamento.DataDecisaoClienteAprovacao != null)
        {
            throw new DomainException($"Orçamento com ID {orcamentoId} aprovação já decidida.");
        }
                
        orcamento.AprovarOrcamento();
        //ordemServico.OSAprovada();
        await _orcamentoRepository.Atualizar(orcamento);
        return new BaseResponse
        {
            Success = true,
            Message = "Orcamento Aprovado com sucesso",
            Data = orcamento.Id
        };
    }
}
