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
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException(
                "Configure o Access Token do Mercado Pago em MercadoPago:AccessToken.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/payments")
        {
            Content = JsonContent.Create(new
            {
                external_reference = request.ExternalReference,
                transaction_amount = request.Amount,
                token = request.CardToken,
                description = $"Pagamento do orçamento {request.ExternalReference}",
                installments = request.Installments,
                payment_method_id = request.PaymentMethodId,
                payer = new
                {
                    email = request.PayerEmail
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
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var providerError = GetProviderError(responseBody);
            logger.LogWarning(
                "Mercado Pago payment creation failed with HTTP status {StatusCode}. Provider error: {ProviderError}",
                (int)response.StatusCode,
                providerError ?? "No error details returned.");
            throw new PaymentProviderException((int)response.StatusCode);
        }

        MercadoPagoPaymentResponse? payment;
        try
        {
            payment = await response.Content.ReadFromJsonAsync<MercadoPagoPaymentResponse>(
                cancellationToken: cancellationToken);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Mercado Pago returned an invalid payment response.");
            throw new PaymentProviderException(502);
        }

        var paymentId = GetPaymentId(payment?.Id);
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            logger.LogError("Mercado Pago returned a successful response without a payment ID.");
            throw new PaymentProviderException(502);
        }

        if (string.IsNullOrWhiteSpace(payment?.Status))
        {
            logger.LogError("Mercado Pago returned a payment without a status.");
            throw new PaymentProviderException(502);
        }

        return new MercadoPagoPaymentResult(
            paymentId,
            payment.Status,
            payment.StatusDetail);
    }

    private static string? GetPaymentId(JsonElement? id) => id?.ValueKind switch
    {
        JsonValueKind.String => id.Value.GetString(),
        JsonValueKind.Number => id.Value.GetRawText(),
        _ => null
    };

    private static string? GetProviderError(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            if (root.TryGetProperty("message", out var message))
                return message.GetString();

            if (root.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array &&
                errors.GetArrayLength() > 0)
            {
                var firstError = errors[0];
                var code = firstError.TryGetProperty("code", out var errorCode)
                    ? errorCode.GetString()
                    : null;
                var errorMessage = firstError.TryGetProperty("message", out var errorMessageElement)
                    ? errorMessageElement.GetString()
                    : null;
                return string.Join(": ", new[] { code, errorMessage }.Where(value =>
                    !string.IsNullOrWhiteSpace(value)));
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private sealed class MercadoPagoPaymentResponse
    {
        public MercadoPagoPaymentResponse()
        {
        }

        [JsonPropertyName("id")]
        public JsonElement Id { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("status_detail")]
        public string? StatusDetail { get; init; }
    }
}
