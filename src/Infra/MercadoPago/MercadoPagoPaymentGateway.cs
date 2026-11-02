using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Exceptions;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infra.MercadoPago;

public sealed class MercadoPagoPaymentGateway(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<MercadoPagoPaymentGateway> logger) : IMercadoPagoPaymentGateway
{
    public async Task<MercadoPagoPaymentResult> CreatePaymentAsync(
        MercadoPagoPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var accessToken = configuration["MercadoPago:AccessToken"];
        if (string.IsNullOrWhiteSpace(accessToken) ||
            !accessToken.StartsWith("TEST-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Configure um Access Token de teste do Mercado Pago em MercadoPago:AccessToken.");
        }

        var amount = request.Amount.ToString("F2", CultureInfo.InvariantCulture);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/orders")
        {
            Content = JsonContent.Create(new
            {
                type = "online",
                processing_mode = "automatic",
                external_reference = request.ExternalReference,
                total_amount = amount,
                payer = new
                {
                    email = request.PayerEmail
                },
                transactions = new
                {
                    payments = new[]
                    {
                        new
                        {
                            amount,
                            payment_method = new
                            {
                                id = request.PaymentMethodId,
                                type = "credit_card",
                                token = request.CardToken,
                                installments = request.Installments
                            }
                        }
                    }
                }
            })
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Headers.Add("X-Idempotency-Key", request.IdempotencyKey);

        HttpResponseMessage responseMessage;
        try
        {
            responseMessage = await httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Could not reach Mercado Pago.");
            throw new PaymentProviderException(502);
        }

        using var response = responseMessage;
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Mercado Pago order creation failed with HTTP status {StatusCode}.",
                (int)response.StatusCode);
            throw new PaymentProviderException((int)response.StatusCode);
        }

        MercadoPagoOrderResponse? order;
        try
        {
            order = await response.Content.ReadFromJsonAsync<MercadoPagoOrderResponse>(
                cancellationToken: cancellationToken);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Mercado Pago returned an invalid order response.");
            throw new PaymentProviderException(502);
        }

        if (string.IsNullOrWhiteSpace(order?.Id))
        {
            logger.LogError("Mercado Pago returned a successful response without an order ID.");
            throw new PaymentProviderException(502);
        }

        var payment = order.Transactions?.Payments?.FirstOrDefault();
        var status = payment?.Status ?? order.Status;
        if (string.IsNullOrWhiteSpace(status))
        {
            logger.LogError("Mercado Pago returned an order without a payment status.");
            throw new PaymentProviderException(502);
        }

        return new MercadoPagoPaymentResult(
            order.Id,
            payment?.Id,
            status,
            payment?.StatusDetail ?? order.StatusDetail);
    }

    private sealed class MercadoPagoOrderResponse
    {
        public MercadoPagoOrderResponse()
        {
        }

        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("status_detail")]
        public string? StatusDetail { get; init; }

        [JsonPropertyName("transactions")]
        public MercadoPagoTransactionsResponse? Transactions { get; init; }
    }

    private sealed class MercadoPagoTransactionsResponse
    {
        public MercadoPagoTransactionsResponse()
        {
        }

        [JsonPropertyName("payments")]
        public List<MercadoPagoOrderPaymentResponse>? Payments { get; init; }
    }

    private sealed class MercadoPagoOrderPaymentResponse
    {
        public MercadoPagoOrderPaymentResponse()
        {
        }

        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("status_detail")]
        public string? StatusDetail { get; init; }
    }
}
