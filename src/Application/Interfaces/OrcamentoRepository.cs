using Domain.Aggregates;

namespace Application.Interfaces; 
public interface OrcamentoRepository : BaseRepository<Orcamento>
{
    Task<Orcamento> EncontrarOrcamentoPorOrdemServico(int ordemServicoId);
}
