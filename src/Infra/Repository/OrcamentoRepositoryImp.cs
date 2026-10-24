using Domain.Aggregates;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace Infra.Repository;

public class OrcamentoRepositoryImp : BaseRepositoryImp<Orcamento>, OrcamentoRepository
{
    public OrcamentoRepositoryImp(IDynamoDBContext context) : base(context)
    {
    }

    public async Task<Orcamento> EncontrarOrcamentoPorOrdemServico(int ordemServicoId)
    {
        var resultados = await _context
            .ScanAsync<Orcamento>(
            [
                new ScanCondition(
                    nameof(Orcamento.OrdemServicoId),
                    ScanOperator.Equal,
                    ordemServicoId)
            ])
            .GetRemainingAsync();

        return resultados.FirstOrDefault();
    }


    public async Task<bool> ExisteOrcamentoEmAbertoParaOrdemdeSerivico(int ordemServicoId)
    {
        var scan = _context.ScanAsync<Orcamento>(
        [
            new ScanCondition(
                nameof(Orcamento.OrdemServicoId),
                ScanOperator.Equal,
                ordemServicoId)
        ]);

        while (!scan.IsDone)
        {
            var page = await scan.GetNextSetAsync();

            if (page.Any(x =>
                    x.Ativo &&
                    (x.DataDecisaoClienteAprovacao == null ||
                    (x.OrcamentoAprovado == true &&
                    x.DataDecisaoClientePagamento == null))))
            {
                return true;
            }
        }

        return false;
    }
}
