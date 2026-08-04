using Domain.Customer;
using Domain.Interface.Order;
using Domain.MechanicalService;
using Domain.Stock;
using Domain.Vehicle;
using Domain.WorkOrder;
using Microsoft.Extensions.Logging;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.Customer;
using Service.Interface.Commands.Order;
using Service.Interface.Commands.Vehicle;
using Service.Interface.Events;
using Service.Interface.Events.Order;
using Service.Interface.Results.Order;

namespace Service
{
    public class OrdersService(IOrdersRepository repository, IOrderDependenciesGateway dependenciesGateway, IStockService stockService, ITransactionManager transactionManager, IApplicationEventDispatcher eventDispatcher, ILogger<OrdersService> logger) : IOrdersService
    {
        private IOrdersRepository Repository { get; set; } = repository;
        private IOrderDependenciesGateway DependenciesGateway { get; set; } = dependenciesGateway;
        private IStockService StockService { get; set; } = stockService;
        private ITransactionManager TransactionManager { get; set; } = transactionManager;
        private IApplicationEventDispatcher EventDispatcher { get; set; } = eventDispatcher;
        private ILogger<OrdersService> Logger { get; set; } = logger;

        public async Task CreateServiceOrder(CreateOrderCommand orderToCreate)
        {
            ArgumentNullException.ThrowIfNull(orderToCreate);

            var customerDocument = DocumentWrapper.CreateDocument(orderToCreate.CustomerDocument).Id;
            var vehicleLicensePlate = LicensePlateWrapper.CreateLicensePlate(orderToCreate.VehicleLicensePlate).License;

            var customer = await DependenciesGateway.GetCustomerByDocument(customerDocument)
                ?? throw new NotFoundException("Cliente não cadastrado. Realize o cadastro antes de criar a ordem de serviço");

            var vehicle = await DependenciesGateway.GetVehicleByLicensePlate(vehicleLicensePlate)
                ?? throw new NotFoundException("Veículo não cadastrado. Realize o cadastro antes de criar a ordem de serviço");

            var order = new Order(customer.Document.Id, vehicle.LicensePlate.License, DateTime.Now);

            if (await Repository.CreateOrder(order) == 0)
                throw new ApplicationFailureException("Erro ao salvar ordem");
        }

        public async Task<Guid> CreateServiceOrder(
            CreateCustomerCommand customerToCreate,
            CreateVehicleCommand vehicleToCreate,
            IReadOnlyCollection<UpdateOrderItemCommand<int>> servicesToAdd,
            IReadOnlyCollection<UpdateOrderItemCommand<int>> materialsToAdd)
        {
            ArgumentNullException.ThrowIfNull(customerToCreate);
            ArgumentNullException.ThrowIfNull(vehicleToCreate);
            ValidateOrderItems(servicesToAdd, "serviços");
            ValidateOrderItems(materialsToAdd, "materiais");

            var requestedCustomer = new Customer(
                customerToCreate.Name,
                customerToCreate.Document,
                customerToCreate.Phone,
                customerToCreate.Email);

            var requestedVehicle = new Vehicle(
                vehicleToCreate.CustomerDocument,
                vehicleToCreate.Brand,
                vehicleToCreate.Model,
                vehicleToCreate.Year,
                vehicleToCreate.LicensePlate);

            if (requestedVehicle.CustomerDocument.Id != requestedCustomer.Document.Id)
                throw new InvalidRequestException("O documento do proprietário do veículo deve corresponder ao documento do cliente");

            return await TransactionManager.ExecuteInTransaction(async () =>
            {
                var customer = await GetOrCreateCustomer(requestedCustomer);
                var vehicle = await GetOrCreateVehicle(requestedVehicle, customer);
                var services = await ResolveServices(servicesToAdd);
                var materials = await ResolveAndReserveMaterials(materialsToAdd);

                var order = new Order(
                    customer.Document.Id,
                    vehicle.LicensePlate.License,
                    services,
                    materials,
                    DateTime.Now);

                if (await Repository.CreateOrder(order) == 0)
                    throw new ApplicationFailureException("Erro ao salvar ordem");

                foreach (var service in order.Services)
                    if (await Repository.AddServiceToOrder(order.Id, service) == 0)
                        throw new ApplicationFailureException("Erro ao salvar serviço da ordem");

                foreach (var material in order.Materials)
                    if (await Repository.AddMaterialToOrder(order.Id, material) == 0)
                        throw new ApplicationFailureException("Erro ao salvar material da ordem");

                return order.Id;
            });
        }

        public async Task<WorkOrderStatus> GetOrderStatus(Guid orderId)
        {
            if (orderId == Guid.Empty)
                throw new InvalidRequestException("A identificação da ordem deve ser informada");

            var order = await Repository.GetOrder(orderId)
                ?? throw new NotFoundException($"Ordem com id \"{orderId}\" não encontrada");

            return order.Status;
        }

        public async Task<IEnumerable<WorkOrderResult>> GetOperationalOrders()
        {
            var orders = await Repository.GetOperationalOrders();

            return orders.Select(WorkOrderResult.Create);
        }

        public async Task<IEnumerable<DetailedWorkOrderResult>> GetOrders(Guid? id = null, string customerDocument = "", string vehicleLicensePlate = "")
        {
            if (!string.IsNullOrEmpty(customerDocument))
                customerDocument = DocumentWrapper.CreateDocument(customerDocument).Id;

            if (!string.IsNullOrEmpty(vehicleLicensePlate))
                vehicleLicensePlate = LicensePlateWrapper.CreateLicensePlate(vehicleLicensePlate).License;

            var orders = await Repository.GetOrders(id, customerDocument, vehicleLicensePlate);

            return orders.Select(DetailedWorkOrderResult.Create);
        }

        public async Task<DetailedWorkOrderResult?> GetOrder(Guid? id = null, string customerDocument = "", string vehicleLicensePlate = "")
        {
            if (id == null && string.IsNullOrEmpty(customerDocument) && string.IsNullOrEmpty(vehicleLicensePlate))
                throw new InvalidRequestException("Falha ao pegar ordem. Nenhum argumento fornecido");

            if (!string.IsNullOrEmpty(customerDocument))
                customerDocument = DocumentWrapper.CreateDocument(customerDocument).Id;

            if (!string.IsNullOrEmpty(vehicleLicensePlate))
                vehicleLicensePlate = LicensePlateWrapper.CreateLicensePlate(vehicleLicensePlate).License;

            var order = await Repository.GetOrder(id, customerDocument, vehicleLicensePlate);

            if (order == null)
                return null;

            return DetailedWorkOrderResult.Create(order);
        }

        public async Task StartDiagnosis(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException("Serviço não encontrado");

            order.StartDiagnosis();

            var registry = await Repository.UpdateOrder(order);

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao atualizar a ordem");
        }

        public async Task AddServiceToOrder(Guid orderId, UpdateOrderItemCommand<int> service)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException("Ordem não encontrada");

            var orderService = order.Services.FirstOrDefault(x => x.Id == service.Id);

            int registry = 0;

            if (orderService == null)
            {
                var serviceToAdd = await DependenciesGateway.GetServiceById(service.Id) ?? throw new NotFoundException($"Serviço com id \"{service.Id}\" não encontrado");

                order.AddService(serviceToAdd);

                registry = await Repository.AddServiceToOrder(orderId, serviceToAdd);
            }
            else
            {
                orderService.AddServiceAmount(service.Value);

                registry = await Repository.UpdateServiceOfOrder(orderId, orderService);
            }

            if (registry == 0)
                throw new ApplicationFailureException("Erro ao salvar serviço");
        }

        public async Task RemoveServiceOfOrder(Guid orderId, UpdateOrderItemCommand<int> service)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException("Ordem não encontrada");

            var orderService = order.Services.FirstOrDefault(x => x.Id == service.Id) ?? throw new NotFoundException("Serviço não encontrado na ordem");

            orderService.RemoveServiceAmount(service.Value);

            int registry;

            if (orderService.Amount == 0)
                registry = await Repository.RemoveServiceFromOrder(orderId, service.Id);
            else
                registry = await Repository.UpdateServiceOfOrder(orderId, orderService);

            if (registry == 0)
                throw new ApplicationFailureException("Erro ao salvar serviço");
        }

        public async Task AddMaterialToOrder(Guid orderId, UpdateOrderItemCommand<int> orderItem)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException("Ordem não encontrada");

            var material = order.Materials.FirstOrDefault(x => x.Id == orderItem.Id);

            await TransactionManager.ExecuteInTransaction(async () =>
            {
                await StockService.ReserveMaterialAmount(orderItem.Id, orderItem.Value);

                int registry;

                if (material == null)
                {
                    var stockItem = await DependenciesGateway.GetMaterialById(orderItem.Id) ?? throw new NotFoundException("Item não encontrado no estoque");

                    var itemAdded = order.AddMaterial(stockItem);

                    registry = await Repository.AddMaterialToOrder(orderId, itemAdded);
                }
                else
                {
                    material.AddAmount(orderItem.Value);

                    registry = await Repository.UpdateMaterialFromOrder(orderId, material);
                }

                if (registry == 0)
                    throw new ApplicationFailureException("Erro ao salvar serviço");
            });
        }

        public async Task RemoveMaterialFromOrder(Guid orderId, UpdateOrderItemCommand<int> orderItem)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException("Ordem não encontrada");

            await TransactionManager.ExecuteInTransaction(async () =>
            {
                await StockService.RestoreMaterialAmount(orderItem.Id, orderItem.Value);

                var material = order.Materials.FirstOrDefault(x => x.Id == orderItem.Id) ?? throw new NotFoundException("Material não encontrado na ordem");

                material.RemoveAmount(orderItem.Value);
                int registry;

                if (material.Amount == 0)
                    registry = await Repository.RemoveMaterialFromOrder(orderId, material.Id);
                else
                    registry = await Repository.UpdateMaterialFromOrder(orderId, material);

                if (registry == 0)
                    throw new ApplicationFailureException("Erro ao salvar serviço");
            });
        }

        public async Task CompleteDiagnosis(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException("Ordem não encontrada");

            order.FinalizeDiagnosis();

            var registry = await Repository.UpdateOrder(order);

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao atualizar ordem");

            Logger.LogInformation(
                "Diagnóstico concluído e evento de orçamento disponível será publicado. OrderId: {OrderId}. Status: {Status}. Budget: {Budget}",
                order.Id,
                order.Status,
                order.Budget);

            await EventDispatcher.Publish(new BudgetAvailableEvent(order));
        }

        public async Task ApproveBudget(Guid orderId, ApproveOrderCommand approve)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException($"Ordem com id \"{orderId}\" não encontrada");

            if (order.CustomerDocument.Id != approve.CustomerDocument)
                throw new BusinessRuleException("Documento de aprovação não está de acordo com o documento do cliente da ordem");

            order.ApproveService(approve.Approved);

            if (!approve.Approved)
            {
                await TransactionManager.ExecuteInTransaction(async () =>
                {
                    foreach (var item in order.Materials)
                        await StockService.RestoreMaterialAmount(item.Id, item.Amount);

                    var registry = await Repository.UpdateOrder(order);

                    if (registry == 0)
                        throw new ApplicationFailureException("Falha ao aprovar ou recusar o orçamento");
                });

                return;
            }

            var registry = await Repository.UpdateOrder(order);

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao aprovar ou recusar o orçamento");
        }

        public async Task StartExecution(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException($"Ordem com id \"{orderId}\" não encontrada");

            order.StartService();

            var registry = await Repository.UpdateOrder(order);

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao inicar execução");
        }

        public async Task CompleteExecution(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException($"Ordem com id \"{orderId}\" não encontrada");

            order.CompleteService(DateTime.Now);

            await TransactionManager.ExecuteInTransaction(async () =>
            {
                foreach (var item in order.Materials)
                    await StockService.ConsumeReservedAmount(item.Id, item.Amount);

                var registry = await Repository.UpdateOrder(order);

                if (registry == 0)
                    throw new ApplicationFailureException("Falha ao completar execução");
            });
        }

        public async Task DeliverVehicle(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException($"Ordem com id \"{orderId}\" não encontrada");

            order.DeliverVehicle();

            var registry = await Repository.UpdateOrder(order);

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao inicar execução");
        }

        public async Task DeleteOrder(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException("Ordem não encontrada");

            if (order.Status is not WorkOrderStatus.Finished and not WorkOrderStatus.Delivered)
            {
                await TransactionManager.ExecuteInTransaction(async () =>
                {
                    foreach (var item in order.Materials)
                        await StockService.RestoreMaterialAmount(item.Id, item.Amount);

                    var registry = await Repository.DeleteOrder(orderId);

                    if (registry == 0)
                        throw new ApplicationFailureException("Falha ao deletar ordem");
                });

                return;
            }

            var registry = await Repository.DeleteOrder(orderId);

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao deletar ordem");
        }

        private async Task<Domain.Interface.Custumer.ICustomer> GetOrCreateCustomer(Customer requestedCustomer)
        {
            var existingCustomer = await DependenciesGateway.GetCustomerByDocument(requestedCustomer.Document.Id);

            if (existingCustomer == null)
            {
                if (await DependenciesGateway.RegisterCustomer(requestedCustomer) == 0)
                    throw new ApplicationFailureException("Erro ao salvar cliente da ordem");

                return requestedCustomer;
            }

            if (!string.Equals(existingCustomer.Name, requestedCustomer.Name, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(existingCustomer.Phone.Number, requestedCustomer.Phone.Number, StringComparison.Ordinal)
                || !string.Equals(existingCustomer.Email.Address, requestedCustomer.Email.Address, StringComparison.OrdinalIgnoreCase))
                throw new ConflictException("Os dados informados não correspondem ao cliente já cadastrado");

            return existingCustomer;
        }

        private async Task<Domain.Interface.Vehicle.IVehicle> GetOrCreateVehicle(Vehicle requestedVehicle, Domain.Interface.Custumer.ICustomer customer)
        {
            var existingVehicle = await DependenciesGateway.GetVehicleByLicensePlate(requestedVehicle.LicensePlate.License);

            if (existingVehicle == null)
            {
                if (await DependenciesGateway.RegisterVehicle(requestedVehicle) == 0)
                    throw new ApplicationFailureException("Erro ao salvar veículo da ordem");

                return requestedVehicle;
            }

            if (!string.Equals(existingVehicle.CustomerDocument.Id, customer.Document.Id, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(existingVehicle.Brand, requestedVehicle.Brand, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(existingVehicle.Model, requestedVehicle.Model, StringComparison.OrdinalIgnoreCase)
                || existingVehicle.Year != requestedVehicle.Year)
                throw new ConflictException("Os dados informados não correspondem ao veículo já cadastrado");

            return existingVehicle;
        }

        private async Task<List<Domain.Interface.Service.IMechanicalService>> ResolveServices(IReadOnlyCollection<UpdateOrderItemCommand<int>> requestedServices)
        {
            List<Domain.Interface.Service.IMechanicalService> services = [];

            foreach (var requestedService in requestedServices)
            {
                var catalogService = await DependenciesGateway.GetServiceById(requestedService.Id)
                    ?? throw new NotFoundException($"Serviço com id \"{requestedService.Id}\" não encontrado");

                services.Add(new MechanicalService(
                    catalogService.Id,
                    catalogService.Description,
                    catalogService.Hours,
                    catalogService.PricePerHour,
                    requestedService.Value));
            }

            return services;
        }

        private async Task<List<Domain.Interface.Stock.IMaterial>> ResolveAndReserveMaterials(IReadOnlyCollection<UpdateOrderItemCommand<int>> requestedMaterials)
        {
            List<Domain.Interface.Stock.IMaterial> materials = [];

            foreach (var requestedMaterial in requestedMaterials)
            {
                var stockMaterial = await DependenciesGateway.GetMaterialById(requestedMaterial.Id)
                    ?? throw new NotFoundException($"Material com id \"{requestedMaterial.Id}\" não encontrado");

                await StockService.ReserveMaterialAmount(requestedMaterial.Id, requestedMaterial.Value);

                materials.Add(new Material(
                    stockMaterial.Id,
                    stockMaterial.Name,
                    stockMaterial.Brand,
                    stockMaterial.Price,
                    requestedMaterial.Value));
            }

            return materials;
        }

        private static void ValidateOrderItems(IReadOnlyCollection<UpdateOrderItemCommand<int>>? items, string itemType)
        {
            if (items == null)
                throw new InvalidRequestException($"A lista de {itemType} deve ser informada");

            if (items.Any(item => item is null))
                throw new InvalidRequestException($"A lista de {itemType} não pode conter itens nulos");

            if (items.Any(item => item.Id == Guid.Empty || item.Value <= 0))
                throw new InvalidRequestException($"A lista de {itemType} contém um item inválido");

            if (items.Select(item => item.Id).Distinct().Count() != items.Count)
                throw new InvalidRequestException($"A lista de {itemType} não pode conter identificadores duplicados");
        }

    }
}



