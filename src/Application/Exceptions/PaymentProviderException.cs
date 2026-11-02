namespace Application.Exceptions;

public sealed class PaymentProviderException : Exception
{
    public PaymentProviderException(int statusCode)
        : base("O provedor de pagamento não conseguiu processar a solicitação.")
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
