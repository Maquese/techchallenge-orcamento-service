using Amazon.DynamoDBv2.DataModel;
using Domain.Entidades;

namespace Domain.Aggregates;

[DynamoDBTable("Orcamento")]
public class OrcamentoModel
{
    protected OrcamentoModel() { }

    [DynamoDBHashKey("orcamentoid")]
    public int Id { get; private set; }
    [DynamoDBRangeKey("ordemserviid")]
    public int OrdemServicoId { get; private set; }
    public decimal ValorTotal { get; private set; }
    public string Observacao { get; private set; }
    public bool? OrcamentoAprovado { get; private set; }
    public DateTime? DataDecisaoClienteAprovacao { get; private set; }
    public bool? OrcamentoPago { get; private set; }
    public DateTime? DataDecisaoClientePagamento { get; private set; }
    public DateTime DataCadastro { get; private set; }
    public bool Ativo {  get; private set; } 
    
    

    public OrcamentoModel(int id, int ordemServicoId, decimal valorTotal, string observacoes, 
                     bool? orcamentoAprovado, DateTime? dataDecisaoClienteAprovacao, bool? OrcamentoPago,
                     DateTime? dataDecisaoClientePagamento, DateTime dataCadastro)
    {
        OrdemServicoId = ordemServicoId;
        ValorTotal = valorTotal;
        //Observacao = observacoes;
        //sOrcamentoAprovado = 
        DataDecisaoClienteAprovacao = null;
        Ativo = true;
        DataCadastro = DateTime.UtcNow;
    }


}
