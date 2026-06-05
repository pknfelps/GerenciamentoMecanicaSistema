namespace Domain.Vehicle
{
    public class OldBrazilLicensePlate(string license) : LicensePlate(license)
    {
        public const int LettersCount = 3;
        public const int NumbersCount = 4;
        protected override string ValidLicensePlateModel { get; set; } = "aaa0000";
    }
}
