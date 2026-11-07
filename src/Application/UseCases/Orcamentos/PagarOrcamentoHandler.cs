using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Requests;
using Application.Models.Responses;
using Domain.Exceptions;

namespace Application.UseCases.Orcamentos;

public class PagarOrcamentoHandler
{
    private readonly OrcamentoRepository _orcamentoRepository;
    private readonly IMercadoPagoPaymentGateway _paymentGateway;

    public PagarOrcamentoHandler(
        OrcamentoRepository orcamentoRepository,
        IMercadoPagoPaymentGateway paymentGateway)
    {
        _orcamentoRepository = orcamentoRepository;
        _paymentGateway = paymentGateway;
    }

    public async Task<PagamentoOrcamentoResponse> Handle(
        Guid orcamentoId,
        PagarOrcamentoRequest request,
        CancellationToken cancellationToken)
    {
        var orcamento = await _orcamentoRepository.ObterPorId(orcamentoId);
        if (orcamento == null)
        {
            throw new OrcamentoNaoEncontradoException(
                $"Orçamento com ID {orcamentoId} não encontrado.");
        }

        if (orcamento.DataDecisaoClientePagamento != null)
        {
            throw new DomainException($"Orçamento com ID {orcamentoId} já foi decidido pelo cliente.");
        }

        if (orcamento.OrcamentoAprovado != true)
        {
            throw new DomainException("O orçamento precisa estar aprovado para receber pagamento.");
        }

        if (orcamento.ValorTotal <= 0 || decimal.Round(orcamento.ValorTotal, 2) != orcamento.ValorTotal)
        {
            throw new DomainException(
                "O valor do orçamento deve ser maior que zero e ter no máximo duas casas decimais para pagamento.");
        }

        if (request.IdempotencyKey == Guid.Empty)
        {
            throw new DomainException("A chave de idempotência deve ser informada.");
        }

        var idempotencyKey = request.IdempotencyKey.ToString();
        if (orcamento.MercadoPagoIdempotencyKey is not null &&
            !string.Equals(
                orcamento.MercadoPagoIdempotencyKey,
                idempotencyKey,
                StringComparison.Ordinal) &&
            !PodeTentarNovamente(orcamento.MercadoPagoPaymentStatus))
        {
            throw new DomainException(
                "Já existe um pagamento em processamento. Reutilize a chave de idempotência da tentativa.");
        }

        orcamento.IniciarPagamento(idempotencyKey);
        await _orcamentoRepository.Atualizar(orcamento);

        MercadoPagoPaymentResult payment;
        try
        {
            payment = await _paymentGateway.CreatePaymentAsync(
                new MercadoPagoPaymentRequest(
                    idempotencyKey,
                    orcamento.Id.ToString(),
                    orcamento.ValorTotal,
                    request.CardToken,
                    request.PaymentMethodId,
                    request.Installments,
                    request.PayerEmail),
                cancellationToken);
        }
        catch (PaymentProviderException exception) when (
            exception.StatusCode is 400 or 422)
        {
            orcamento.RegistrarFalhaAoIniciarPagamento();
            await _orcamentoRepository.Atualizar(orcamento);
            throw;
        }

        orcamento.RegistrarResultadoPagamento(
            payment.PaymentId,
            payment.Status,
            payment.StatusDetail);
        await _orcamentoRepository.Atualizar(orcamento);

        var pago = orcamento.OrcamentoPago == true;
        return new PagamentoOrcamentoResponse(
            orcamento.Id,
            orcamento.ValorTotal,
            payment.PaymentId,
            payment.Status,
            payment.StatusDetail,
            pago);
    }

    private static bool PodeTentarNovamente(string? status) =>
        string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase);
}
