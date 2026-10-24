using Amazon.DynamoDBv2.DataModel;
using Domain.Entidades;

namespace Domain.Aggregates;

[DynamoDBTable("Orcamento")]
public class Orcamento : IEntity
{
    protected Orcamento() { }
    [DynamoDBHashKey("orcamentoid")]
    public Guid Id { get; private set; }
     [DynamoDBRangeKey("ordemservicoid")]
    public int OrdemServicoId { get; private set; }
    public decimal ValorTotal { get; private set; }
    public string Observacao { get; private set; }
    public bool? OrcamentoAprovado { get; private set; }
    public DateTime? DataDecisaoClienteAprovacao { get; private set; }
    public bool? OrcamentoPago { get; private set; }
    public DateTime? DataDecisaoClientePagamento { get; private set; }
    public DateTime DataCadastro { get; private set; }
    

    public Orcamento(int ordemServicoId, decimal valorTotal, string observacao)
    {
        Id = Guid.NewGuid();
        OrdemServicoId = ordemServicoId;
        ValorTotal = valorTotal;
        Observacao = observacao;
        OrcamentoAprovado = null;
        DataDecisaoClienteAprovacao = null;
        Ativo = true;
        DataCadastro = DateTime.UtcNow;
    }

    public void AprovarOrcamento()
    {
        OrcamentoAprovado = true;
        DataDecisaoClienteAprovacao = DateTime.Now;
    }

     public void NegarOrcamento()
    {
        OrcamentoAprovado = false;
        DataDecisaoClienteAprovacao = DateTime.Now;
    }


    public void ReprovarOrcamento()
    {
        OrcamentoAprovado = false;
        DataDecisaoClienteAprovacao = DateTime.Now;
    }

    public void MarcarOrcamentoPago()
    {
        OrcamentoPago = true;
        DataDecisaoClientePagamento = DateTime.Now;
    }

    public void MarcarOrcamentoNaoPago()
    {
        OrcamentoPago = false;
        DataDecisaoClientePagamento = DateTime.Now;
    }
}
