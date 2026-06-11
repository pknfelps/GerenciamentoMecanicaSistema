using Domain.Customer;
using Domain.Interface.Custumer;
using Domain.Interface.Vehicle;

namespace Domain.Vehicle
{
    public class Vehicle : IVehicle
    {
        public Guid Id { get; private set; }
        public IDocument CustomerDocument { get; private set; }
        public string Brand { get; private set; }
        public string Model { get; private set; }
        public int Year { get; private set; }
        public ILicensePlate LicensePlate { get; private set; }

        public Vehicle(string customerDocument, string brand, string model, int year, string licensePlate) : this(Guid.NewGuid(), customerDocument, brand, model, year, licensePlate) { }

        public Vehicle(Guid id, string customerDocument, string brand, string model, int year, string licensePlate)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id não pode ser vazio");

            ArgumentException.ThrowIfNullOrEmpty(customerDocument);
            ArgumentException.ThrowIfNullOrEmpty(brand);
            ArgumentException.ThrowIfNullOrEmpty(model);

            if (year < 0)
                throw new ArgumentException("Ano do veículo não pode ser menor que 0");

            CustomerDocument = DocumentWrapper.CreateDocument(customerDocument);
            Brand = brand;
            Model = model;
            Year = year;
            LicensePlate = LicensePlateWrapper.CreateLicensePlate(licensePlate);
            Id = id;
        }
    }
}
