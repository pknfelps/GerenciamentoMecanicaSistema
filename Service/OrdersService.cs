using Domain.Customer;
using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Domain.MechanicalService;
using Domain.Stock;
using Domain.Vehicle;
using Domain.WorkOrder;
using Microsoft.Extensions.Logging;
using Service.Interface.Persistence;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.Order;
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

        public async Task<Guid> CreateServiceOrder(CreateOrderCommand orderToCreate)
        {
            ArgumentNullException.ThrowIfNull(orderToCreate);

            if (orderToCreate.CustomerId == Guid.Empty)
                throw new InvalidRequestException("A identificação do cliente deve ser informada");

            if (orderToCreate.VehicleId == Guid.Empty)
                throw new InvalidRequestException("A identificação do veículo deve ser informada");

            ValidateOrderItems(orderToCreate.Services, "serviços");
            ValidateOrderItems(orderToCreate.Materials, "materiais");

            var customer = await DependenciesGateway.GetCustomerById(orderToCreate.CustomerId)
                ?? throw new NotFoundException($"Cliente com ID \"{orderToCreate.CustomerId}\" não encontrado");
            
            var vehicle = await DependenciesGateway.GetVehicleById(orderToCreate.VehicleId)
                ?? throw new NotFoundException($"Veículo com ID \"{orderToCreate.VehicleId}\" não encontrado");

            if (vehicle.CustomerDocument.Id != customer.Document.Id)
                throw new InvalidRequestException("O veículo informado não pertence ao cliente");

            Order? createdOrder = null;
            var orderId = await TransactionManager.ExecuteInTransaction(async () =>
            {
                var services = await ResolveServices(orderToCreate.Services);
                var materials = await ResolveAndReserveMaterials(orderToCreate.Materials);

                var order = new Order(
                    customer.Document.Id,
                    vehicle.LicensePlate.License,
                    services,
                    materials,
                    DateTime.UtcNow);

                if (await Repository.CreateOrder(order) == 0)
                    throw new ApplicationFailureException("Falha ao salvar a ordem");

                foreach (var service in order.Services)
                    if (await Repository.AddServiceToOrder(order.Id, service) == 0)
                        throw new ApplicationFailureException("Falha ao salvar o serviço da ordem");

                foreach (var material in order.Materials)
                    if (await Repository.AddMaterialToOrder(order.Id, material) == 0)
                        throw new ApplicationFailureException("Falha ao salvar o material da ordem");

                createdOrder = order;
                return order.Id;
            });

            await EventDispatcher.Publish(new OrderStatusChangedEvent(CreateNotificationSnapshot(
                createdOrder ?? throw new ApplicationFailureException("Falha ao criar a ordem"))));

            return orderId;
        }

        public async Task<WorkOrderStatus> GetOrderStatus(Guid orderId)
        {
            if (orderId == Guid.Empty)
                throw new InvalidRequestException("A identificação da ordem deve ser informada");

            var order = await Repository.GetOrder(orderId)
                ?? throw new NotFoundException($"Ordem com ID \"{orderId}\" não encontrada");

            return order.Status;
        }

        public async Task<IEnumerable<WorkOrderResult>> GetOperationalOrders()
        {
            var orders = await Repository.GetOperationalOrders();

            return orders.Select(WorkOrderResult.Create);
        }

        public async Task<WorkOrderStatus> GetCustomerOrderStatus(Guid orderId, Guid customerId)
        {
            var order = await GetCustomerOrder(orderId, customerId);
            return order.Status;
        }

        private async Task<IOrder> GetCustomerOrder(Guid orderId, Guid customerId)
        {
            if (orderId == Guid.Empty || customerId == Guid.Empty)
                throw new InvalidRequestException("A identificação da ordem e do cliente deve ser informada");

            var customer = await DependenciesGateway.GetCustomerById(customerId)
                ?? throw new NotFoundException("Ordem não encontrada");
            var order = await Repository.GetOrder(orderId, customer_document: customer.Document.Id);
            if (order is null || order.CustomerDocument.Id != customer.Document.Id)
                throw new NotFoundException("Ordem não encontrada");

            return order;
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
                throw new InvalidRequestException("Falha ao buscar a ordem: nenhum argumento foi fornecido");

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
            var order = await ChangeStatus(orderId, order => order.StartDiagnosis(), "Falha ao atualizar a ordem");
            await EventDispatcher.Publish(new OrderStatusChangedEvent(CreateNotificationSnapshot(order)));
        }

        public Task AddServiceToOrder(Guid orderId, UpdateOrderItemCommand<int> service)
        {
            return TransactionManager.ExecuteInTransaction(async () =>
            {
                var order = await GetLockedOrder(orderId);
                var existing = order.Services.FirstOrDefault(item => item.Id == service.Id);
                int registry;
                if (existing is null)
                {
                    var catalogService = await DependenciesGateway.GetServiceById(service.Id)
                        ?? throw new NotFoundException($"Serviço com ID \"{service.Id}\" não encontrado");
                    registry = await Repository.AddServiceToOrder(orderId, order.AddService(CreateOrderService(catalogService, service.Value)));
                }
                else
                    registry = await Repository.UpdateServiceOfOrder(orderId, order.AddService(CreateOrderService(existing, service.Value)));

                if (registry == 0)
                    throw new ApplicationFailureException("Falha ao salvar o serviço da ordem");
            });
        }

        public Task RemoveServiceOfOrder(Guid orderId, UpdateOrderItemCommand<int> service)
        {
            return TransactionManager.ExecuteInTransaction(async () =>
            {
                var order = await GetLockedOrder(orderId);
                var existing = order.Services.FirstOrDefault(item => item.Id == service.Id)
                    ?? throw new NotFoundException("Serviço não encontrado na ordem");
                var updated = order.RemoveService(CreateOrderService(existing, service.Value));
                var registry = updated.Amount == 0
                    ? await Repository.RemoveServiceFromOrder(orderId, service.Id)
                    : await Repository.UpdateServiceOfOrder(orderId, updated);
                if (registry == 0)
                    throw new ApplicationFailureException("Falha ao atualizar o serviço da ordem");
            });
        }

        public Task AddMaterialToOrder(Guid orderId, UpdateOrderItemCommand<int> orderItem)
        {
            return TransactionManager.ExecuteInTransaction(async () =>
            {
                var order = await GetLockedOrder(orderId);
                var material = order.Materials.FirstOrDefault(item => item.Id == orderItem.Id);
                await StockService.ReserveMaterialAmount(orderItem.Id, orderItem.Value);
                int registry;
                if (material is null)
                {
                    var stockItem = await DependenciesGateway.GetMaterialById(orderItem.Id)
                        ?? throw new NotFoundException("Item não encontrado no estoque");
                    registry = await Repository.AddMaterialToOrder(orderId, order.AddMaterial(CreateOrderMaterial(stockItem, orderItem.Value)));
                }
                else
                    registry = await Repository.UpdateMaterialFromOrder(orderId, order.AddMaterial(CreateOrderMaterial(material, orderItem.Value)));

                if (registry == 0)
                    throw new ApplicationFailureException("Falha ao salvar o material da ordem");
            });
        }

        public Task RemoveMaterialFromOrder(Guid orderId, UpdateOrderItemCommand<int> orderItem)
        {
            return TransactionManager.ExecuteInTransaction(async () =>
            {
                var order = await GetLockedOrder(orderId);
                var existing = order.Materials.FirstOrDefault(item => item.Id == orderItem.Id)
                    ?? throw new NotFoundException("Material não encontrado na ordem");
                var updated = order.RemoveMaterial(CreateOrderMaterial(existing, orderItem.Value));
                await StockService.RestoreMaterialAmount(orderItem.Id, orderItem.Value);
                var registry = updated.Amount == 0
                    ? await Repository.RemoveMaterialFromOrder(orderId, updated.Id)
                    : await Repository.UpdateMaterialFromOrder(orderId, updated);
                if (registry == 0)
                    throw new ApplicationFailureException("Falha ao atualizar o material da ordem");
            });
        }

        public async Task CompleteDiagnosis(Guid orderId)
        {
            var order = await ChangeStatus(orderId, order => order.FinalizeDiagnosis(), "Falha ao atualizar ordem");
            Logger.LogInformation(
                "Diagnóstico concluído e evento de orçamento disponível será publicado. OrderId: {OrderId}. Status: {Status}",
                order.Id, order.Status);
            await EventDispatcher.Publish(new BudgetAvailableEvent(CreateNotificationSnapshot(order)));
        }

        public Task ApproveBudget(Guid orderId, ApproveOrderCommand approve, Guid customerId)
        {
            return TransactionManager.ExecuteInTransaction(async () =>
            {
                await Repository.LockOrder(orderId);
                var order = await GetCustomerOrder(orderId, customerId);
                var customerDocument = DocumentWrapper.CreateDocument(approve.CustomerDocument).Id;
                if (order.CustomerDocument.Id != customerDocument)
                    throw new BusinessRuleException("Documento de aprovação não está de acordo com o documento do cliente da ordem");

                order.ApproveService(approve.Approved);
                if (!approve.Approved)
                    foreach (var item in order.Materials.OrderBy(item => item.Id))
                        await StockService.RestoreMaterialAmount(item.Id, item.Amount);

                if (await Repository.UpdateOrder(order) == 0)
                    throw new ApplicationFailureException("Falha ao aprovar ou recusar o orçamento");
            });
        }

        public async Task StartExecution(Guid orderId)
        {
            var order = await ChangeStatus(orderId, order => order.StartService(), "Falha ao iniciar a execução");
            await EventDispatcher.Publish(new OrderStatusChangedEvent(CreateNotificationSnapshot(order)));
        }

        public async Task CompleteExecution(Guid orderId)
        {
            var order = await TransactionManager.ExecuteInTransaction(async () =>
            {
                var current = await GetLockedOrder(orderId);
                current.CompleteService(DateTime.UtcNow);
                foreach (var item in current.Materials.OrderBy(item => item.Id))
                    await StockService.ConsumeReservedAmount(item.Id, item.Amount);
                if (await Repository.UpdateOrder(current) == 0)
                    throw new ApplicationFailureException("Falha ao completar a execução");
                return current;
            });
            await EventDispatcher.Publish(new OrderStatusChangedEvent(CreateNotificationSnapshot(order)));
        }

        public async Task DeliverVehicle(Guid orderId)
        {
            var order = await ChangeStatus(orderId, order => order.DeliverVehicle(), "Falha ao registrar a entrega do veículo");
            await EventDispatcher.Publish(new OrderStatusChangedEvent(CreateNotificationSnapshot(order)));
        }

        public Task DeleteOrder(Guid orderId)
        {
            return TransactionManager.ExecuteInTransaction(async () =>
            {
                var order = await GetLockedOrder(orderId);
                if (order.Status is not WorkOrderStatus.Finished and not WorkOrderStatus.Delivered)
                    foreach (var item in order.Materials.OrderBy(item => item.Id))
                        await StockService.RestoreMaterialAmount(item.Id, item.Amount);
                if (await Repository.DeleteOrder(orderId) == 0)
                    throw new ApplicationFailureException("Falha ao excluir a ordem");
            });
        }

        private Task<IOrder> ChangeStatus(Guid orderId, Action<IOrder> transition, string failureMessage)
        {
            return TransactionManager.ExecuteInTransaction(async () =>
            {
                var order = await GetLockedOrder(orderId);
                transition(order);
                if (await Repository.UpdateOrder(order) == 0)
                    throw new ApplicationFailureException(failureMessage);
                return order;
            });
        }

        private async Task<IOrder> GetLockedOrder(Guid orderId)
        {
            await Repository.LockOrder(orderId);
            return await Repository.GetOrder(orderId) ?? throw new NotFoundException("Ordem não encontrada");
        }

        private async Task<List<IMechanicalService>> ResolveServices(IReadOnlyCollection<UpdateOrderItemCommand<int>> requestedServices)
        {
            List<IMechanicalService> services = [];

            foreach (var requestedService in requestedServices)
            {
                var catalogService = await DependenciesGateway.GetServiceById(requestedService.Id)
                    ?? throw new NotFoundException($"Serviço com ID \"{requestedService.Id}\" não encontrado");

                services.Add(new MechanicalService(
                    catalogService.Id,
                    catalogService.Description,
                    catalogService.Hours,
                    catalogService.PricePerHour,
                    requestedService.Value));
            }

            return services;
        }

        private async Task<List<IMaterial>> ResolveAndReserveMaterials(IReadOnlyCollection<UpdateOrderItemCommand<int>> requestedMaterials)
        {
            List<IMaterial> materials = [];

            foreach (var requestedMaterial in requestedMaterials.OrderBy(item => item.Id))
            {
                var stockMaterial = await DependenciesGateway.GetMaterialById(requestedMaterial.Id)
                    ?? throw new NotFoundException($"Material com ID \"{requestedMaterial.Id}\" não encontrado");

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

        private static IMechanicalService CreateOrderService(IMechanicalService service, int amount) =>
            new MechanicalService(
                service.Id,
                service.Description,
                service.Hours,
                service.PricePerHour,
                amount);

        private static IMaterial CreateOrderMaterial(IMaterial material, int amount) =>
            new Material(
                material.Id,
                material.Name,
                material.Brand,
                material.Price,
                amount);

        private static OrderNotificationSnapshot CreateNotificationSnapshot(Domain.Interface.Order.IOrder order) =>
            new(
                order.Id,
                order.CustomerDocument.Id,
                order.VehicleLicensePlate.License,
                order.Budget,
                order.Status);

    }
}



