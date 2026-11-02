using System.ComponentModel.DataAnnotations;

namespace Application.Models.Requests;

public sealed record PagarOrcamentoRequest
{
    /// <summary>Identificador do orçamento aprovado a ser pago.</summary>
    public required Guid OrcamentoId { get; init; }

    /// <summary>Token temporário do cartão gerado pelo SDK do Mercado Pago no cliente.</summary>
    [Required]
    [StringLength(256)]
    public required string CardToken { get; init; }

    /// <summary>Identificador do método de pagamento retornado pelo SDK do Mercado Pago.</summary>
    [Required]
    [StringLength(50)]
    public required string PaymentMethodId { get; init; }

    /// <summary>Quantidade de parcelas escolhida pelo cliente.</summary>
    [Range(1, 99)]
    public int Installments { get; init; }

    /// <summary>E-mail do pagador cadastrado no Mercado Pago.</summary>
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public required string PayerEmail { get; init; }

    /// <summary>Chave UUID estável para evitar cobranças duplicadas em novas tentativas HTTP.</summary>
    public required Guid IdempotencyKey { get; init; }
}
