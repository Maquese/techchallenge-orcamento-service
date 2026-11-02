namespace Application.Interfaces;

public sealed record MercadoPagoPaymentRequest(
    string IdempotencyKey,
    string ExternalReference,
    decimal Amount,
    string CardToken,
    string PaymentMethodId,
    int Installments,
    string PayerEmail);

public sealed record MercadoPagoPaymentResult(
    string OrderId,
    string? PaymentId,
    string Status,
    string? StatusDetail);

public interface IMercadoPagoPaymentGateway
{
    Task<MercadoPagoPaymentResult> CreatePaymentAsync(
        MercadoPagoPaymentRequest request,
        CancellationToken cancellationToken);
}
