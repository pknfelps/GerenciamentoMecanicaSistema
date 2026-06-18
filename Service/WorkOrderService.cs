using Domain.Customer;
using Domain.Interface.Order;
using Domain.Order;
using Domain.Stock;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto.Order;
using Service.Interface.Dto.Stock;

namespace Service
{
    public class WorkOrderService(IWorkOrderRepository repository, ICustomerService customerService, IVehicleService vehicleService, IStockService stockService, IMechanicalServiceService mechanicalServiceService) : IWorkOrderService
    {
        private IWorkOrderRepository Repository { get; set; } = repository;
        private ICustomerService CustomerService { get; set; } = customerService;
        private IVehicleService VehicleService { get; set; } = vehicleService;
        private IStockService StockService { get; set; } = stockService;
        private IMechanicalServiceService CatalogService { get; set; } = mechanicalServiceService;

        public async Task CreateServiceOrder(CreateOrderDto orderToCreate)
        {
            var document = DocumentWrapper.CreateDocument(orderToCreate.CustomerDocument);

            var customer = await CustomerService.GetCustomer(document.Id) ?? throw new InvalidOperationException("Cliente não cadastrado. Realize o cadastro antes de criar a ordem de serviço");

            var vehicle = await VehicleService.GetVehicle(orderToCreate.VehicleLicensePlate) ?? throw new InvalidOperationException("Documento não cadastrado. Realize o cadastro antes de criar a ordem de serviço");

            var order = new WorkOrder(customer.Document, vehicle.LicensePlate);

            var registry = await Repository.CreateOrder(order);

            if (registry == 0)
                throw new InvalidOperationException("Erro ao salvar ordem");
        }

        public async Task<IEnumerable<WorkOrderDto?>> GetOrders()
        {
            var orders = await Repository.GetOrders();

            return orders.Select(WorkOrderDto.Create);
        }

        public async Task<DetailedWorkOrderDto?> GetOrder(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId);

            if (order == null)
                return null;

            return DetailedWorkOrderDto.Create(order);
        }

        public async Task<IEnumerable<DetailedWorkOrderDto?>> GetCustomerOrders(string customerDocument)
        {
            var document = DocumentWrapper.CreateDocument(customerDocument);

            _ = await CustomerService.GetCustomer(document.Id) ?? throw new InvalidOperationException("Cliente não enconotrado");

            var order = await Repository.GetCustomerOrders(document.Id);

            return order.Select(DetailedWorkOrderDto.Create);
        }

        public async Task StartDiagnosis(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException("Serviço não encontrado");

            order.StartDiagnosis();

            var registry = await Repository.UpdateOrderStatus(orderId, WorkOrderStatus.InDiagnosis);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao atualizar a ordem");
        }

        public async Task AddServiceToOrder(OrderUpdateItemDto serviceDto)
        {
            var order = await Repository.GetOrder(serviceDto.OrderId) ?? throw new InvalidOperationException("Ordem não encontrada");

            var orderService = order.Services.FirstOrDefault(x => x.Id == serviceDto.ItemId);

            int registry = 0;

            if (orderService == null)
            {
                var service = await CatalogService.GetService(serviceDto.ItemId) ?? throw new InvalidOperationException($"Serviço com id \"{serviceDto.ItemId}\" não encontrado");

                order.AddService(service.ToDomain());

                registry = await Repository.AddServiceToOrder(serviceDto.OrderId, service.ToDomain());
            }
            else
            {
                orderService.AddServiceAmount(serviceDto.Amount);

                registry = await Repository.UpdateServiceOfOrder(serviceDto.OrderId, orderService);
            }

            if (registry == 0)
                throw new InvalidOperationException("Erro ao salvar serviço");
        }

        public async Task RemoveServiceOfOrder(OrderUpdateItemDto serviceDto)
        {
            var order = await Repository.GetOrder(serviceDto.OrderId) ?? throw new InvalidOperationException("Ordem não encontrada");

            var service = order.Services.First(x => x.Id == serviceDto.ItemId);

            service.RemoveServiceAmount(serviceDto.Amount);

            int registry;

            if (service.Amount == 0)
                registry = await Repository.DeleteServiceFromOrder(serviceDto.OrderId, serviceDto.ItemId);
            else
                registry = await Repository.UpdateServiceOfOrder(serviceDto.OrderId, service);

            if (registry == 0)
                throw new InvalidOperationException("Erro ao salvar serviço");
        }

        public async Task DeleteServiceFromOrder(OrderUpdateItemDto itemDto)
        {
            var order = await Repository.GetOrder(itemDto.OrderId) ?? throw new InvalidOperationException($"Ordem com id \"{itemDto.OrderId}\" não encontrada");

            var service = order.Services.First(x => x.Id == itemDto.ItemId);

            order.RemoveService(service);

            var registry = await Repository.DeleteServiceFromOrder(itemDto.OrderId, itemDto.ItemId);

            if (registry == 0)
                throw new InvalidOperationException("Erro ao salvar serviço");
        }

        public async Task AddPartOrSupplieToOrder(OrderUpdateItemDto orderItem)
        {
            var order = await Repository.GetOrder(orderItem.OrderId) ?? throw new InvalidOperationException("Ordem não encontrada");

            await StockService.ReservePartAmount(new(orderItem.ItemId, orderItem.Amount));

            var part = order.Parts.FirstOrDefault(x => x.Id == orderItem.ItemId);

            try
            {
                int registry;

                if (part == null)
                {
                    var stockItem = await StockService.GetPart(orderItem.ItemId) ?? throw new InvalidOperationException("Item não encontrado no estoque");

                    var itemAdded = order.AddPartOrSupplie(stockItem.ToDomain());

                    registry = await Repository.AddPartToOrder(orderItem.OrderId, itemAdded);
                }
                else
                {
                    part.AddAmount(orderItem.Amount);

                    registry = await Repository.UpdatePartFromOrder(orderItem.OrderId, part);
                }

                if (registry == 0)
                    throw new InvalidOperationException("Erro ao salvar serviço");
            }
            catch
            {
                await StockService.RestorePartAmount(new(orderItem.ItemId, orderItem.Amount));

                throw;
            }
        }

        public async Task RemovePartOrSupplieFromOrder(OrderUpdateItemDto orderItem)
        {
            var order = await Repository.GetOrder(orderItem.OrderId) ?? throw new InvalidOperationException("Ordem não encontrada");

            await StockService.RestorePartAmount(new(orderItem.ItemId, orderItem.Amount));

            try
            {
                var part = order.Parts.First(x => x.Id == orderItem.ItemId);

                part.RemoveAmount(orderItem.Amount);
                int registry;

                if (part.Amount == 0)
                    registry = await Repository.RemovePartFromOrder(orderItem.OrderId, part.Id);
                else
                    registry = await Repository.UpdatePartFromOrder(orderItem.OrderId, part);

                if (registry == 0)
                    throw new InvalidOperationException("Erro ao salvar serviço");
            }
            catch
            {
                await StockService.ReservePartAmount(new(orderItem.ItemId, orderItem.Amount));

                throw;
            }
        }

        public async Task CompleteDiagnosis(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException("Ordem não encontrada");

            order.FinalizeDiagnosis();

            var registry = await Repository.UpdateOrderStatus(orderId, order.Status);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao atualizar status da ordem");

            try
            {
                registry = await Repository.UpdateOrderBudget(orderId, order.Budget);

                if (registry == 0)
                    throw new InvalidOperationException("Falha ao atualizar orçamento da ordem");
            }
            catch
            {
                await Repository.UpdateOrderStatus(orderId, WorkOrderStatus.InDiagnosis);

                throw;
            }

        }

        public async Task ApproveBudget(ApproveOrderDto approve)
        {
            var order = await Repository.GetOrder(approve.OrderId) ?? throw new InvalidOperationException($"Ordem com id \"{approve.OrderId}\" não encontrada");

            order.ApproveService(approve.Approved);

            if (!approve.Approved)
            {
                foreach (var item in order.Parts)
                    await StockService.RestorePartAmount(new(item.Id, item.Amount));
            }

            var registry = await Repository.UpdateOrderStatus(approve.OrderId, order.Status);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao aprovar ou recusar o orçamento");
        }

        public async Task StartExecution(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException($"Ordem com id \"{orderId}\" não encontrada");

            order.StartService();

            var registry = await Repository.UpdateOrderStatus(orderId, order.Status);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao inicar execução");
        }

        public async Task CompleteExecution(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException($"Ordem com id \"{orderId}\" não encontrada");

            order.CompleteService();

            foreach (var item in order.Parts)
                await StockService.ConsumeReservedAmount(new(item.Id, item.Amount));

            var updateStatus = await Repository.UpdateOrderStatus(orderId, order.Status);
            var updateDuration = await Repository.UpdateOrderDuration(orderId, order.Duration);

            if (updateStatus == 0 || updateDuration == 0)
                throw new InvalidOperationException("Falha ao completar execução");
        }

        public async Task VehicleDelivered(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException($"Ordem com id \"{orderId}\" não encontrada");

            order.VehicleDelivered();

            var registry = await Repository.UpdateOrderStatus(orderId, order.Status);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao inicar execução");
        }

        public async Task DeleteOrder(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException("Ordem não encontrada");

            foreach (var item in order.Parts)
                await StockService.RestorePartAmount(new PartUpdateDto<int>(item.Id, item.Amount));

            var registry = await Repository.DeleteOrder(orderId);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao deletar ordem");
        }
    }
}
