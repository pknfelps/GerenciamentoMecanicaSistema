using Domain.Interface.Custumer;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Domain.Interface.Vehicle;
using Service.Interface.Persistence;
using Service.Interface;

namespace Service
{
    public class OrderDependenciesGateway(
        ICustomerRepository customerRepository,
        IVehicleRepository vehicleRepository,
        ICatalogRepository catalogRepository,
        IStockRepository stockRepository) : IOrderDependenciesGateway
    {
        private ICustomerRepository CustomerRepository { get; } = customerRepository;
        private IVehicleRepository VehicleRepository { get; } = vehicleRepository;
        private ICatalogRepository CatalogRepository { get; } = catalogRepository;
        private IStockRepository StockRepository { get; } = stockRepository;

        public Task<ICustomer?> GetCustomerByDocument(string document) =>
            CustomerRepository.GetCustomer(document: document);

        public Task<ICustomer?> GetCustomerById(Guid id) =>
            CustomerRepository.GetCustomer(id: id);

        public Task<IVehicle?> GetVehicleByLicensePlate(string licensePlate) =>
            VehicleRepository.GetVehicle(license_plate: licensePlate);

        public Task<IVehicle?> GetVehicleById(Guid id) =>
            VehicleRepository.GetVehicle(id: id);

        public Task<IMechanicalService?> GetServiceById(Guid id) =>
            CatalogRepository.GetService(id);

        public Task<IMaterial?> GetMaterialById(Guid id) =>
            StockRepository.GetMaterial(id);
    }
}
