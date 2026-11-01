using Domain.Exceptions;

namespace Domain.VOs;

public class DocumentoVO
{
    public string Numero { get; private set; }
    public string Tipo { get; private set; } // "CPF" ou "CNPJ"

    protected DocumentoVO() { }

    public DocumentoVO(string numero)
    {
        numero = new string(numero.Where(char.IsDigit).ToArray());

        if (ValidarCpf(numero))
        {
            Numero = numero;
            Tipo = "CPF";
        }
        else if (ValidarCnpj(numero))
        {
            Numero = numero;
            Tipo = "CNPJ";
        }
        else
        {
            throw new DomainException("Documento inválido. CPF ou CNPJ não reconhecido.");
        }
    }

    private bool ValidarCpf(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return false;

        if (numero.Length != 11)
            return false;

        // Elimina CPFs inválidos conhecidos (todos dígitos iguais)
        if (new string(numero[0], numero.Length) == numero)
            return false;

        // Calcula primeiro dígito
        int[] pesos1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int soma = 0;
        for (int i = 0; i < 9; i++)
            soma += (numero[i] - '0') * pesos1[i];

        int resto = soma % 11;
        int digito1 = resto < 2 ? 0 : 11 - resto;

        // Calcula segundo dígito
        int[] pesos2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        soma = 0;
        for (int i = 0; i < 10; i++)
            soma += (numero[i] - '0') * pesos2[i];

        resto = soma % 11;
        int digito2 = resto < 2 ? 0 : 11 - resto;

        return numero[9] == (char)('0' + digito1) && numero[10] == (char)('0' + digito2);
    }

    private bool ValidarCnpj(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return false;

        if (numero.Length != 14)
            return false;

        // Elimina CNPJs inválidos conhecidos (todos dígitos iguais)
        if (new string(numero[0], numero.Length) == numero)
            return false;

        // Calcula primeiro dígito
        int[] pesos1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int soma = 0;
        for (int i = 0; i < 12; i++)
            soma += (numero[i] - '0') * pesos1[i];

        int resto = soma % 11;
        int digito1 = resto < 2 ? 0 : 11 - resto;

        // Calcula segundo dígito
        int[] pesos2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        soma = 0;
        for (int i = 0; i < 13; i++)
            soma += (numero[i] - '0') * pesos2[i];

        resto = soma % 11;
        int digito2 = resto < 2 ? 0 : 11 - resto;

        return numero[12] == (char)('0' + digito1) && numero[13] == (char)('0' + digito2);
    }
}
