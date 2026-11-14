using Application.Interfaces;
using Application.Models.Responses;
using Domain.Exceptions;

namespace Application.UseCases.Orcamentos;

public class NegarOrcamentoHandler
{
    private readonly OrcamentoRepository _orcamentoRepository;

    public NegarOrcamentoHandler(OrcamentoRepository orcamentoRepository)
    {
        _orcamentoRepository = orcamentoRepository;
    }

    public async Task<BaseResponse> Handle(Guid orcamentoId)
    {
        var orcamento = await _orcamentoRepository.ObterPorId(orcamentoId);
        if (orcamento == null)
        {
            throw new DomainException($"Orçamento com ID {orcamentoId} não encontrado.");
        }

        if(orcamento.DataDecisaoClientePagamento != null)
        {
            throw new DomainException($"Orçamento com ID {orcamentoId} já foi decidido pelo cliente.");
        }

        if(orcamento.DataDecisaoClienteAprovacao != null)
        {
            throw new DomainException($"Orçamento com ID {orcamentoId} já decidida.");
        }
        orcamento.NegarOrcamento();
        
        await _orcamentoRepository.Atualizar(orcamento);
        return new BaseResponse
        {
            Success = true,
            Message = "Orçamento negado com sucesso."
        };
    }
}
