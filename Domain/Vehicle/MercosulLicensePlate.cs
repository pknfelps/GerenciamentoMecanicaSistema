namespace Domain.Vehicle
{
    public class MercosulLicensePlate(string license) : LicensePlate(license)
    {
        public const int LettersCount = 4;
        public const int NumbersCount = 3;
        protected override string ValidLicensePlateModel { get; set; } = "aaa0a00";
    }
}
