namespace Domain.Exceptions;

public sealed class OrcamentoNaoEncontradoException(string message) : DomainException(message)
{
}
