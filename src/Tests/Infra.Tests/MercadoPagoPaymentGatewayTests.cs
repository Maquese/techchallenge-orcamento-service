using System.Net;
using System.Text.Json;
using Application.Exceptions;
using Application.Interfaces;
using Infra.MercadoPago;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infra.Tests;

public sealed class MercadoPagoPaymentGatewayTests
{
    [Theory]
    [InlineData("\"payment-123\"")]
    [InlineData("123456789")]
    public async Task CreatePaymentAsync_DeveEnviarPagamentoEMapearResposta(string jsonId)
    {
        HttpMethod? capturedMethod = null;
        Uri? capturedUri = null;
        string? authorizationScheme = null;
        string? authorizationParameter = null;
        string? idempotencyKey = null;
        string? capturedBody = null;
        var (gateway, client) = CreateGateway(async (request, cancellationToken) =>
        {
            capturedMethod = request.Method;
            capturedUri = request.RequestUri;
            authorizationScheme = request.Headers.Authorization?.Scheme;
            authorizationParameter = request.Headers.Authorization?.Parameter;
            idempotencyKey = request.Headers.GetValues("X-Idempotency-Key").Single();
            capturedBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return JsonResponse(
                HttpStatusCode.Created,
                $$"""{"id":{{jsonId}},"status":"approved","status_detail":"accredited"}""");
        });
        using (client)
        {
            var result = await gateway.CreatePaymentAsync(NovaSolicitacao(), CancellationToken.None);

            Assert.Equal(jsonId.StartsWith('"') ? "payment-123" : "123456789", result.PaymentId);
            Assert.Equal("approved", result.Status);
            Assert.Equal("accredited", result.StatusDetail);
            Assert.Equal(HttpMethod.Post, capturedMethod);
            Assert.Equal("https://api.mercadopago.com/v1/payments", capturedUri!.ToString());
            Assert.Equal("Bearer", authorizationScheme);
            Assert.Equal("access-token-test", authorizationParameter);
            Assert.Equal("idem-key", idempotencyKey);

            using var body = JsonDocument.Parse(capturedBody!);
            var root = body.RootElement;
            Assert.Equal("budget-123", root.GetProperty("external_reference").GetString());
            Assert.Equal(125.50m, root.GetProperty("transaction_amount").GetDecimal());
            Assert.Equal("card-token", root.GetProperty("token").GetString());
            Assert.Equal("visa", root.GetProperty("payment_method_id").GetString());
            Assert.Equal(3, root.GetProperty("installments").GetInt32());
            Assert.Equal("buyer@example.com", root.GetProperty("payer").GetProperty("email").GetString());
        }
    }

    [Fact]
    public async Task CreatePaymentAsync_DeveFalharSemAccessTokenSemEnviarRequisicao()
    {
        var requestSent = false;
        var (gateway, client) = CreateGateway((_, _) =>
        {
            requestSent = true;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, """{"id":1,"status":"approved"}"""));
        }, accessToken: null);
        using (client)
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                gateway.CreatePaymentAsync(NovaSolicitacao(), CancellationToken.None));

            Assert.Contains("MercadoPago:AccessToken", exception.Message);
            Assert.False(requestSent);
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task CreatePaymentAsync_DevePropagarStatusHttpDoProvedor(HttpStatusCode statusCode)
    {
        var (gateway, client) = CreateGateway((_, _) =>
            Task.FromResult(JsonResponse(statusCode, """{"message":"provider error"}""")));
        using (client)
        {
            var exception = await Assert.ThrowsAsync<PaymentProviderException>(() =>
                gateway.CreatePaymentAsync(NovaSolicitacao(), CancellationToken.None));

            Assert.Equal((int)statusCode, exception.StatusCode);
        }
    }

    [Theory]
    [InlineData("""{"errors":[{"code":"4037","message":"Invalid transaction_amount"}]}""")]
    [InlineData("""{"errors":[{"code":"4037"}]}""")]
    [InlineData("""{"errors":[{"message":"Invalid transaction_amount"}]}""")]
    [InlineData("""{"errors":[]}""")]
    [InlineData("invalid-json")]
    public async Task CreatePaymentAsync_DeveTratarVariacoesDoCorpoDeErroDoProvedor(
        string responseBody)
    {
        var (gateway, client) = CreateGateway((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.BadRequest, responseBody)));
        using (client)
        {
            var exception = await Assert.ThrowsAsync<PaymentProviderException>(() =>
                gateway.CreatePaymentAsync(NovaSolicitacao(), CancellationToken.None));

            Assert.Equal((int)HttpStatusCode.BadRequest, exception.StatusCode);
        }
    }

    [Fact]
    public async Task CreatePaymentAsync_DeveConverterFalhaDeConexaoEmBadGateway()
    {
        var (gateway, client) = CreateGateway((_, _) =>
            throw new HttpRequestException("connection failed"));
        using (client)
        {
            var exception = await Assert.ThrowsAsync<PaymentProviderException>(() =>
                gateway.CreatePaymentAsync(NovaSolicitacao(), CancellationToken.None));

            Assert.Equal((int)HttpStatusCode.BadGateway, exception.StatusCode);
        }
    }

    [Theory]
    [InlineData("""{"id":1}""")]
    [InlineData("""{"status":"approved"}""")]
    [InlineData("""{"id":1,"status":""}""")]
    [InlineData("""{"id":null,"status":"approved"}""")]
    public async Task CreatePaymentAsync_DeveFalharQuandoRespostaNaoTemIdOuStatus(string responseBody)
    {
        var (gateway, client) = CreateGateway((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, responseBody)));
        using (client)
        {
            var exception = await Assert.ThrowsAsync<PaymentProviderException>(() =>
                gateway.CreatePaymentAsync(NovaSolicitacao(), CancellationToken.None));

            Assert.Equal((int)HttpStatusCode.BadGateway, exception.StatusCode);
        }
    }

    [Fact]
    public async Task CreatePaymentAsync_DeveFalharQuandoRespostaNaoEhJsonValido()
    {
        var (gateway, client) = CreateGateway((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, "{invalid-json")));
        using (client)
        {
            var exception = await Assert.ThrowsAsync<PaymentProviderException>(() =>
                gateway.CreatePaymentAsync(NovaSolicitacao(), CancellationToken.None));

            Assert.Equal((int)HttpStatusCode.BadGateway, exception.StatusCode);
        }
    }

    private static (MercadoPagoPaymentGateway Gateway, HttpClient Client) CreateGateway(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync,
        string? accessToken = "access-token-test")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MercadoPago:AccessToken"] = accessToken
            })
            .Build();
        var client = new HttpClient(new StubHttpMessageHandler(sendAsync))
        {
            BaseAddress = new Uri("https://api.mercadopago.com/")
        };
        var gateway = new MercadoPagoPaymentGateway(
            client,
            configuration,
            NullLogger<MercadoPagoPaymentGateway>.Instance);

        return (gateway, client);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string body) =>
        new(statusCode)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };

    private static MercadoPagoPaymentRequest NovaSolicitacao() =>
        new("idem-key", "budget-123", 125.50m, "card-token", "visa", 3, "buyer@example.com");

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            sendAsync(request, cancellationToken);
    }
}
