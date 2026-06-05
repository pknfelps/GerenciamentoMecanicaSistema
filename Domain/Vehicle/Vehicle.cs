using Domain.Interface.Vehicle;

namespace Domain.Vehicle
{
    public class Vehicle : IVehicle
    {
        public Guid Id { get; private set; }
        public string Brand { get; private set; }
        public string Model { get; private set; }
        public int Year { get; private set; }
        public ILicensePlate LicensePlate { get; private set; }

        public Vehicle(string brand, string model, int year, string licensePlate) : this(Guid.NewGuid(), brand, model, year, licensePlate) { }

        public Vehicle(Guid id, string brand, string model, int year, string licensePlate)
        {
            ArgumentException.ThrowIfNullOrEmpty(brand);
            ArgumentException.ThrowIfNullOrEmpty(model);

            if (year < 0)
                throw new ArgumentException("Ano do veículo não pode ser menor que 0");

            if (id == Guid.Empty)
                throw new ArgumentException("Id não pode ser vazio");

            Brand = brand;
            Model = model;
            Year = year;
            LicensePlate = LicensePlateWrapper.CreateLicensePlate(licensePlate);
            Id = id;
        }
    }
}
