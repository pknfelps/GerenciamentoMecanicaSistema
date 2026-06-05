namespace Domain.Vehicle
{
    public static class LicensePlateWrapper
    {
        public static LicensePlate CreateLicensePlate(string license)
        {
            int letters = license.Count(char.IsLetter);
            int numbers = license.Count(char.IsNumber);

            if (letters == MercosulLicensePlate.LettersCount && numbers == MercosulLicensePlate.NumbersCount)
                return new MercosulLicensePlate(license);

            if (letters == OldBrazilLicensePlate.LettersCount && numbers == OldBrazilLicensePlate.NumbersCount)
                return new OldBrazilLicensePlate(license);

            throw new ArgumentException("Placa inválida");
        }
    }
}
