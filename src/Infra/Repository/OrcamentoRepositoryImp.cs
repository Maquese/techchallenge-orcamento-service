using Domain.Aggregates;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repository;

public class OrcamentoRepositoryImp : BaseRepositoryImp<Orcamento>, OrcamentoRepository
{
    public OrcamentoRepositoryImp(EFContext context) : base(context)
    {
    }

    public async Task<Orcamento> EncontrarOrcamentoPorOrdemServico(int ordemServicoId)
    {
        return await _context.Orcamento.Where(x => x.OrdemServicoId == ordemServicoId).FirstOrDefaultAsync();
    }
}
