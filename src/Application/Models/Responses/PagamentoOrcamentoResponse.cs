namespace Application.Models.Responses;

/// <summary>Resultado do processamento de pagamento de um orçamento.</summary>
public sealed record PagamentoOrcamentoResponse(
    Guid OrcamentoId,
    decimal Valor,
    string MercadoPagoOrderId,
    string? MercadoPagoPaymentId,
    string Status,
    string? StatusDetail,
    bool Pago);
