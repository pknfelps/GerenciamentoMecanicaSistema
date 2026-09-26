using GerenciamentoMecanica.Auth.Contracts;

namespace Domain.Customer
{
    public class Cpf(string id) : Document(id, CpfRules.Normalize, "CPF")
    {
        public const int DigitCount = CpfRules.DigitCount;
    }
}
