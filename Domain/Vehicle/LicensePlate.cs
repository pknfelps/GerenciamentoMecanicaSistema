using Domain.Interface.Exceptions;
using Domain.Interface.Vehicle;

namespace Domain.Vehicle
{
    public abstract class LicensePlate : ILicensePlate
    {
        public string License { get; private set; }

        protected abstract string ValidLicensePlateModel { get; set; }

        private const int ValidLicensePlateLenght = 7;

        protected LicensePlate(string license)
        {
            license = license.ToLower();

            if (license.Length != ValidLicensePlateLenght)
                throw new DomainValidationException("Placa inválida");

            if (license.FirstOrDefault(x => char.IsPunctuation(x) || char.IsSymbol(x)) != default)
                throw new DomainValidationException("Placa não deve conter símbolos ou pontuação");

            for (int i = 0; i < ValidLicensePlateLenght; i++)
                if (char.GetUnicodeCategory(license[i]) != char.GetUnicodeCategory(ValidLicensePlateModel[i]))
                    throw new DomainValidationException("Placa fora do modelo permitido");

            License = license.ToUpper();
        }
    }
}