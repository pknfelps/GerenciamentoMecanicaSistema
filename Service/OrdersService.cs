using Domain.Customer;
using Domain.Interface.Order;
using Domain.MechanicalService;
using Domain.Vehicle;
using Domain.WorkOrder;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto;
using Service.Interface.Dto.Order;

namespace Service
{
    public class OrdersService(IOrdersRepository repository, ICustomerService customerService, IVehicleService vehicleService, IStockService stockService, ICatalogService mechanicalServiceService, IEmailService emailService) : IOrdersService
    {
        private IOrdersRepository Repository { get; set; } = repository;
        private ICustomerService CustomerService { get; set; } = customerService;
        private IVehicleService VehicleService { get; set; } = vehicleService;
        private IStockService StockService { get; set; } = stockService;
        private ICatalogService CatalogService { get; set; } = mechanicalServiceService;
        private IEmailService EmailService { get; set; } = emailService;

        public async Task CreateServiceOrder(CreateOrderDto orderToCreate)
        {
            var customer = await CustomerService.GetCustomer(document: DocumentWrapper.CreateDocument(orderToCreate.CustomerDocument).Id) ?? throw new InvalidOperationException("Cliente não cadastrado. Realize o cadastro antes de criar a ordem de serviço");

            var vehicle = await VehicleService.GetVehicle(licensePlate: orderToCreate.VehicleLicensePlate) ?? throw new InvalidOperationException("Documento não cadastrado. Realize o cadastro antes de criar a ordem de serviço");

            var order = new Order(customer.Document, vehicle.LicensePlate);

            var registry = await Repository.CreateOrder(order);

            if (registry == 0)
                throw new InvalidOperationException("Erro ao salvar ordem");
        }

        public async Task<IEnumerable<DetailedWorkOrderDto>> GetOrders(Guid? id = null, string customerDocument = "", string vehicleLicensePlate = "")
        {
            if (!string.IsNullOrEmpty(customerDocument))
                customerDocument = DocumentWrapper.CreateDocument(customerDocument).Id;

            if (!string.IsNullOrEmpty(vehicleLicensePlate))
                vehicleLicensePlate = LicensePlateWrapper.CreateLicensePlate(vehicleLicensePlate).License;

            var orders = await Repository.GetOrders(id, customerDocument, vehicleLicensePlate);

            return orders.Select(DetailedWorkOrderDto.Create);
        }

        public async Task<DetailedWorkOrderDto?> GetOrder(Guid? id = null, string customerDocument = "", string vehicleLicensePlate = "")
        {
            if (id == null && string.IsNullOrEmpty(customerDocument) && string.IsNullOrEmpty(vehicleLicensePlate))
                throw new InvalidOperationException("Falha ao pegar ordem. Nenhum argumento fornecido");

            if (!string.IsNullOrEmpty(customerDocument))
                customerDocument = DocumentWrapper.CreateDocument(customerDocument).Id;

            if (!string.IsNullOrEmpty(vehicleLicensePlate))
                vehicleLicensePlate = LicensePlateWrapper.CreateLicensePlate(vehicleLicensePlate).License;

            var order = await Repository.GetOrder(id, customerDocument, vehicleLicensePlate);

            if (order == null)
                return null;

            return DetailedWorkOrderDto.Create(order);
        }

        public async Task StartDiagnosis(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException("Serviço não encontrado");

            order.StartDiagnosis();

            var registry = await Repository.UpdateOrder(order);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao atualizar a ordem");
        }

        public async Task AddServiceToOrder(Guid orderId, UpdateItemDto<int> serviceDto)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException("Ordem não encontrada");

            var orderService = order.Services.FirstOrDefault(x => x.Id == serviceDto.Id);

            int registry = 0;

            if (orderService == null)
            {
                var service = await CatalogService.GetService(serviceDto.Id) ?? throw new InvalidOperationException($"Serviço com id \"{serviceDto.Id}\" não encontrado");

                var serviceToAdd = new MechanicalService(service.Id, service.Description, service.Hours, service.PricePerHour, service.Amount);

                order.AddService(serviceToAdd);

                registry = await Repository.AddServiceToOrder(orderId, serviceToAdd);
            }
            else
            {
                orderService.AddServiceAmount(serviceDto.Value);

                registry = await Repository.UpdateServiceOfOrder(orderId, orderService);
            }

            if (registry == 0)
                throw new InvalidOperationException("Erro ao salvar serviço");
        }

        public async Task RemoveServiceOfOrder(Guid orderId, UpdateItemDto<int> serviceDto)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException("Ordem não encontrada");

            var service = order.Services.First(x => x.Id == serviceDto.Id);

            service.RemoveServiceAmount(serviceDto.Value);

            int registry;

            if (service.Amount == 0)
                registry = await Repository.RemoveServiceFromOrder(orderId, serviceDto.Id);
            else
                registry = await Repository.UpdateServiceOfOrder(orderId, service);

            if (registry == 0)
                throw new InvalidOperationException("Erro ao salvar serviço");
        }

        public async Task AddMaterialToOrder(Guid orderId, UpdateItemDto<int> orderItem)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException("Ordem não encontrada");

            await StockService.ReserveMaterialAmount(orderItem.Id, orderItem.Value);

            var material = order.Materials.FirstOrDefault(x => x.Id == orderItem.Id);

            try
            {
                int registry;

                if (material == null)
                {
                    var stockItem = await StockService.GetMaterial(orderItem.Id) ?? throw new InvalidOperationException("Item não encontrado no estoque");

                    var itemAdded = order.AddMaterial(stockItem.ToDomain());

                    registry = await Repository.AddMaterialToOrder(orderId, itemAdded);
                }
                else
                {
                    material.AddAmount(orderItem.Value);

                    registry = await Repository.UpdateMaterialFromOrder(orderId, material);
                }

                if (registry == 0)
                    throw new InvalidOperationException("Erro ao salvar serviço");
            }
            catch
            {
                await StockService.RestoreMaterialAmount(orderItem.Id, orderItem.Value);

                throw;
            }
        }

        public async Task RemoveMaterialFromOrder(Guid orderId, UpdateItemDto<int> orderItem)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException("Ordem não encontrada");

            await StockService.RestoreMaterialAmount(orderItem.Id, orderItem.Value);

            try
            {
                var material = order.Materials.First(x => x.Id == orderItem.Id);

                material.RemoveAmount(orderItem.Value);
                int registry;

                if (material.Amount == 0)
                    registry = await Repository.RemoveMaterialFromOrder(orderId, material.Id);
                else
                    registry = await Repository.UpdateMaterialFromOrder(orderId, material);

                if (registry == 0)
                    throw new InvalidOperationException("Erro ao salvar serviço");
            }
            catch
            {
                await StockService.ReserveMaterialAmount(orderItem.Id, orderItem.Value);

                throw;
            }
        }

        public async Task CompleteDiagnosis(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException("Ordem não encontrada");

            order.FinalizeDiagnosis();

            var registry = await Repository.UpdateOrder(order);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao atualizar ordem");

            try
            {
                await NotifyOrderCompleted(order);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Falha ao enviar email para o cliente. {e}");
            }
        }

        public async Task ApproveBudget(Guid orderId, ApproveOrderDto approve)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException($"Ordem com id \"{orderId}\" não encontrada");

            if (order.CustomerDocument.Id != approve.CustomerDocument)
                throw new InvalidOperationException("Documento de aprovação não está de acordo com o documento do cliente da ordem");

            order.ApproveService(approve.Approved);

            if (!approve.Approved)
            {
                foreach (var item in order.Materials)
                    await StockService.RestoreMaterialAmount(item.Id, item.Amount);
            }

            var registry = await Repository.UpdateOrder(order);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao aprovar ou recusar o orçamento");
        }

        public async Task StartExecution(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException($"Ordem com id \"{orderId}\" não encontrada");

            order.StartService();

            var registry = await Repository.UpdateOrder(order);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao inicar execução");
        }

        public async Task CompleteExecution(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException($"Ordem com id \"{orderId}\" não encontrada");

            order.CompleteService();

            foreach (var item in order.Materials)
                await StockService.ConsumeReservedAmount(item.Id, item.Amount);

            var registry = await Repository.UpdateOrder(order);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao completar execução");
        }

        public async Task DeliverVehicle(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException($"Ordem com id \"{orderId}\" não encontrada");

            order.DeliverVehicle();

            var registry = await Repository.UpdateOrder(order);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao inicar execução");
        }

        public async Task DeleteOrder(Guid orderId)
        {
            var order = await Repository.GetOrder(orderId) ?? throw new InvalidOperationException("Ordem não encontrada");

            if (order.Status is not WorkOrderStatus.Finished and not WorkOrderStatus.Delivered)
            {
                foreach (var item in order.Materials)
                    await StockService.RestoreMaterialAmount(item.Id, item.Amount);
            }

            var registry = await Repository.DeleteOrder(orderId);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao deletar ordem");
        }

        private async Task NotifyOrderCompleted(IOrder workOrder)
        {
            var customer = await CustomerService.GetCustomer(document: workOrder.CustomerDocument.Id) ?? throw new InvalidOperationException("Falha ao notificar o cliente. Cliente não encontrado");

            var vehicle = await VehicleService.GetVehicle(licensePlate: workOrder.VehicleLicensePlate.License) ?? throw new InvalidOperationException("Falha ao notificar o cliente. Veículo não encontrado");

            var customerDomain = new Customer(customer.Id, customer.Name, customer.Document, customer.Phone, customer.Email);

            await EmailService.NotifyBudget(customerDomain, vehicle.ToDomain(), workOrder);
        }
    }
}
