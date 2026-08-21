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
                ?? throw new NotFoundException($"Cliente com id \"{orderToCreate.CustomerId}\" não encontrado");
            
            var vehicle = await DependenciesGateway.GetVehicleById(orderToCreate.VehicleId)
                ?? throw new NotFoundException($"Veículo com id \"{orderToCreate.VehicleId}\" não encontrado");

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
                    DateTime.Now);

                if (await Repository.CreateOrder(order) == 0)
                    throw new ApplicationFailureException("Erro ao salvar ordem");

                foreach (var service in order.Services)
                    if (await Repository.AddServiceToOrder(order.Id, service) == 0)
                        throw new ApplicationFailureException("Erro ao salvar serviço da ordem");

                foreach (var material in order.Materials)
                    if (await Repository.AddMaterialToOrder(order.Id, material) == 0)
                        throw new ApplicationFailureException("Erro ao salvar material da ordem");

                createdOrder = order;
                return order.Id;
            });

            await EventDispatcher.Publish(new OrderStatusChangedEvent(CreateNotificationSnapshot(
                createdOrder ?? throw new ApplicationFailureException("Erro ao criar ordem"))));

            return orderId;
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

            await EventDispatcher.Publish(new OrderStatusChangedEvent(CreateNotificationSnapshot(order)));
        }

        public async Task AddServiceToOrder(Guid orderId, UpdateOrderItemCommand<int> service)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException("Ordem não encontrada");

            var orderService = order.Services.FirstOrDefault(x => x.Id == service.Id);
            IMechanicalService serviceToPersist;
            int registry;

            if (orderService == null)
            {
                var catalogService = await DependenciesGateway.GetServiceById(service.Id) ?? throw new NotFoundException($"Serviço com id \"{service.Id}\" não encontrado");
                var serviceToAdd = CreateOrderService(catalogService, service.Value);

                serviceToPersist = order.AddService(serviceToAdd);

                registry = await Repository.AddServiceToOrder(orderId, serviceToPersist);
            }
            else
            {
                serviceToPersist = order.AddService(CreateOrderService(orderService, service.Value));

                registry = await Repository.UpdateServiceOfOrder(orderId, serviceToPersist);
            }

            if (registry == 0)
                throw new ApplicationFailureException("Erro ao salvar serviço");
        }

        public async Task RemoveServiceOfOrder(Guid orderId, UpdateOrderItemCommand<int> service)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException("Ordem não encontrada");

            var orderService = order.Services.FirstOrDefault(x => x.Id == service.Id) ?? throw new NotFoundException("Serviço não encontrado na ordem");
            var updatedService = order.RemoveService(CreateOrderService(orderService, service.Value));

            int registry;

            if (updatedService.Amount == 0)
                registry = await Repository.RemoveServiceFromOrder(orderId, service.Id);
            else
                registry = await Repository.UpdateServiceOfOrder(orderId, updatedService);

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
                    var itemAdded = order.AddMaterial(CreateOrderMaterial(stockItem, orderItem.Value));

                    registry = await Repository.AddMaterialToOrder(orderId, itemAdded);
                }
                else
                {
                    var updatedMaterial = order.AddMaterial(CreateOrderMaterial(material, orderItem.Value));

                    registry = await Repository.UpdateMaterialFromOrder(orderId, updatedMaterial);
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
                var updatedMaterial = order.RemoveMaterial(CreateOrderMaterial(material, orderItem.Value));
                int registry;

                if (updatedMaterial.Amount == 0)
                    registry = await Repository.RemoveMaterialFromOrder(orderId, updatedMaterial.Id);
                else
                    registry = await Repository.UpdateMaterialFromOrder(orderId, updatedMaterial);

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

            await EventDispatcher.Publish(new BudgetAvailableEvent(CreateNotificationSnapshot(order)));
        }

        public async Task ApproveBudget(Guid orderId, ApproveOrderCommand approve)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException($"Ordem com id \"{orderId}\" não encontrada");
            var customerDocument = DocumentWrapper.CreateDocument(approve.CustomerDocument).Id;

            if (order.CustomerDocument.Id != customerDocument)
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

            await EventDispatcher.Publish(new OrderStatusChangedEvent(CreateNotificationSnapshot(order)));
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

            await EventDispatcher.Publish(new OrderStatusChangedEvent(CreateNotificationSnapshot(order)));
        }

        public async Task DeliverVehicle(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new NotFoundException($"Ordem com id \"{orderId}\" não encontrada");

            order.DeliverVehicle();

            var registry = await Repository.UpdateOrder(order);

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao inicar execução");

            await EventDispatcher.Publish(new OrderStatusChangedEvent(CreateNotificationSnapshot(order)));
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

        private async Task<List<IMechanicalService>> ResolveServices(IReadOnlyCollection<UpdateOrderItemCommand<int>> requestedServices)
        {
            List<IMechanicalService> services = [];

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

        private async Task<List<IMaterial>> ResolveAndReserveMaterials(IReadOnlyCollection<UpdateOrderItemCommand<int>> requestedMaterials)
        {
            List<IMaterial> materials = [];

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



