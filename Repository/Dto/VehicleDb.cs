namespace Repository.Dto
{
    internal class VehicleDb(Guid id, string brand, string model, int year, string license_plate)
    {
        public Guid Id { get; private set; } = id;
        public string Brand { get; private set; } = brand;
        public string Model { get; private set; } = model;
        public int Year { get; private set; } = year;
        public string LicensePlate { get; private set; } = license_plate;
    }
}
