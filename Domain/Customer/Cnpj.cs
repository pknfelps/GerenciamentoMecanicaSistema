using GerenciamentoMecanica.Auth.Contracts;

namespace Domain.Customer
{
    public class Cnpj(string id) : Document(id, CnpjRules.Normalize, "CNPJ")
    {
        public const int DigitCount = CnpjRules.DigitCount;
    }
}
