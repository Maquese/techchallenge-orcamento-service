using Domain.Exceptions;

namespace Domain.VOs;

public class EmailVO
{
    public string Endereco { get; private set; }

    protected EmailVO() { }

    public EmailVO(string endereco)
    {
        if (!Validar(endereco))
            throw new DomainException("Email inválido");
        Endereco = endereco;
    }

    private bool Validar(string endereco)
    {
        if (string.IsNullOrWhiteSpace(endereco))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(endereco);
            return addr.Address == endereco;
        }
        catch
        {
            return false;
        }
    }
}
