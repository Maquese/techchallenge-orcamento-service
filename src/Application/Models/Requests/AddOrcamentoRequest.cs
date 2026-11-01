namespace Application.Models.Requests;

public class AddOrcamentoRequest
{
    public int OrdemServicoId { get; set; }
    public decimal ValorTotal {get;set;}
    public string Observacao { get; set; }
}
