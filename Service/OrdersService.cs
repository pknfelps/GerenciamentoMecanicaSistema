using Domain.Customer;
using Domain.Interface.Order;
using Domain.Vehicle;
using Domain.WorkOrder;
using Microsoft.Extensions.Logging;
using Repository.Interface;
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

        public async Task CreateServiceOrder(CreateOrderCommand orderToCreate)
        {
            var customerDocument = DocumentWrapper.CreateDocument(orderToCreate.CustomerDocument).Id;
            var vehicleLicensePlate = LicensePlateWrapper.CreateLicensePlate(orderToCreate.VehicleLicensePlate).License;

            var customer = await DependenciesGateway.GetCustomerByDocument(customerDocument) ?? throw new NotFoundException("Cliente não cadastrado. Realize o cadastro antes de criar a ordem de serviço");

            var vehicle = await DependenciesGateway.GetVehicleByLicensePlate(vehicleLicensePlate) ?? throw new NotFoundException("Veículo não cadastrado. Realize o cadastro antes de criar a ordem de serviço");

            var order = new Order(customer.Document.Id, vehicle.LicensePlate.License, DateTime.Now);

            var registry = await Repository.CreateOrder(order);

            if (registry == 0)
                throw new ApplicationFailureException("Erro ao salvar ordem");
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

    }
}



