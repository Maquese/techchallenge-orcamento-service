namespace Application.Models.Responses;

public class ListOrcamentoResponse
{
    public int OrdemServicoId { get; set; }
    public decimal Valor { get; set; }
    public int Id { get; set; }
    public bool? OrcamentoAprovado { get; set; }
    public bool? OrcamentoPago { get; set; }
    public DateTime DataCadastro { get; set; }
}
