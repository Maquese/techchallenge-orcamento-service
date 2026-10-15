using Domain.Exceptions;

namespace Domain.VOs;

public class CelularVO
{
    public string Numero { get; private set; }

    protected CelularVO() { }

    public CelularVO(string numero)
    {
        if (!Validar(numero))
            throw new DomainException("Celular inválido. Deve conter exatamente 11 dígitos numéricos");
        Numero = numero;
    }

    private bool Validar(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return false;

        // Remove caracteres não numéricos
        numero = new string(numero.Where(char.IsDigit).ToArray());

        // Valida se tem exatamente 11 dígitos
        if (numero.Length != 11)
            return false;

        return true;
    }
}
