using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Dto.Customer;
using Service.Interface.Dto.Order;
using Service.Interface.Dto.Service;
using Service.Interface.Dto.Stock;
using Service.Interface.Dto.Vehicle;

namespace ServiceTests
{
    public class WorkOrderServiceTests
    {
        private IWorkOrderService Service { get; set; }
        private IWorkOrderRepository Repository { get; set; }
        private ICustomerService CustomerService { get; set; }
        private IVehicleService VehicleService { get; set; }
        private IStockService StockService { get; set; }
        private IMechanicalServiceService MechanicalService { get; set; }

        private static CustomerDto ExistingCustomer { get; } = new(Guid.NewGuid(), "Teste", "123.456.789-12", "(11) 91234-5678", "teste@gmail.com");

        private static CustomerDto ExistingFailCustomer { get; } = new(Guid.NewGuid(), "Teste", "321.654.987-21", "(11) 91234-5678", "teste@gmail.com");

        private static VehicleDto ExistingVehicle { get; } = new(Guid.NewGuid(), ExistingCustomer.Document, "Honda", "Civic", 2026, "CVC2026");

        private static CreateOrderDto OrderToCreate { get; } = new(ExistingCustomer.Document, ExistingVehicle.LicensePlate);

        private static IMechanicalService ExistingService { get; } = CreateSubstituteService(Guid.NewGuid(), "Revisão", 6, 100, 1);
        private static ServiceDto ExistingServiceDto { get; } = new(ExistingService.Id, "Revisão", 6, 100, 1);
        private static IMechanicalService ExistingService2 { get; } = CreateSubstituteService(Guid.NewGuid(), "Troca de Pneu", 2, 150, 1);
        private static ServiceDto ExistingService2Dto { get; } = new(ExistingService2.Id, "Troca de Pneu", 2, 150, 1);
        private static IPart ExistingPart { get; } = CreateSubstitutePart(Guid.NewGuid(), "Pneu", "Michelin", 600, 10, 4);
        private static PartDto ExistingPartDto { get; } = new(ExistingPart.Id, ExistingPart.Name, ExistingPart.Brand, ExistingPart.Price, ExistingPart.Amount, ExistingPart.ReservedAmount);
        private static IPart ExistingPart2 { get; } = CreateSubstitutePart(Guid.NewGuid(), "Óleo de Motor", "Lubrax", 35, 20, 0);
        private static PartDto ExistingPart2Dto { get; } = new(ExistingPart2.Id, ExistingPart2.Name, ExistingPart2.Brand, ExistingPart2.Price, ExistingPart2.Amount, ExistingPart2.ReservedAmount);
        private static IWorkOrder ExistingReceivedOrder { get; } = CreateSubstituteOrder(Guid.NewGuid(), [], [], 0.0, WorkOrderStatus.Received);
        private static IWorkOrder ExistingTestOrder { get; set; } = CreateSubstituteOrder(Guid.NewGuid(), [], [], 0.0, WorkOrderStatus.Received);
        private static readonly Guid ExistingOrderInDiagnosisId = Guid.NewGuid();
        private static IWorkOrder ExistingOrderInDiagnosis { get; set; }
        private static OrderUpdateItemDto AddNewServiceToOrder { get; } = new(ExistingReceivedOrder.Id, ExistingService.Id, 1);
        private static OrderUpdateItemDto AddServiceAmountToOrder { get; } = new(ExistingOrderInDiagnosisId, ExistingService.Id, 1);
        private static OrderUpdateItemDto RemoveServiceOfOrder { get; } = new(ExistingOrderInDiagnosisId, ExistingService.Id, 2);
        private static OrderUpdateItemDto RemoveServiceAmountOfOrder { get; } = new(ExistingOrderInDiagnosisId, ExistingService.Id, 1);

        [SetUp]
        public async Task SetUp()
        {
            ExistingOrderInDiagnosis = CreateSubstituteOrder(ExistingOrderInDiagnosisId, [CreateSubstituteService(ExistingService.Id, ExistingService.Description, ExistingService.Hours, ExistingService.PricePerHour, 2), CreateSubstituteService(ExistingService2.Id, ExistingService2.Description, ExistingService2.Hours, ExistingService2.PricePerHour, 4)], [CreateSubstitutePart(ExistingPart.Id, ExistingPart.Name, ExistingPart.Brand, ExistingPart.Price, 4, 0), CreateSubstitutePart(ExistingPart2.Id, ExistingPart2.Name, ExistingPart2.Brand, ExistingPart2.Price, 1, 0)], 0.0, WorkOrderStatus.InDiagnosis);

            Repository = Substitute.For<IWorkOrderRepository>();

            Repository.CreateOrder(Arg.Any<IWorkOrder>()).Returns(callInfo =>
            {
                var order = callInfo.ArgAt<IWorkOrder>(0);

                if (order.CustomerDocument.Id == ExistingCustomer.Document)
                    return 1;

                return 0;
            });

            List<IWorkOrder> orders = new List<IWorkOrder>() { ExistingReceivedOrder, ExistingOrderInDiagnosis, ExistingTestOrder };
            Repository.GetOrders().Returns(orders);

            Repository.GetOrder(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                return orders.FirstOrDefault(x => x.Id == id);
            });

            Repository.GetCustomerOrders(Arg.Any<string>()).Returns(callInfo =>
            {
                var document = callInfo.ArgAt<string>(0);

                return orders.Where(x => x.CustomerDocument.Id == document);
            });

            Repository.UpdateOrderStatus(Arg.Any<Guid>(), Arg.Any<WorkOrderStatus>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var status = callInfo.ArgAt<WorkOrderStatus>(1);

                if (id == ExistingReceivedOrder.Id && (status == WorkOrderStatus.InDiagnosis || status == WorkOrderStatus.WaitingForExecution || status == WorkOrderStatus.Finished))
                    return 1;

                if (id == ExistingOrderInDiagnosisId && (status == WorkOrderStatus.WaitingForApproval || status == WorkOrderStatus.Finished || status == WorkOrderStatus.InExecution || status == WorkOrderStatus.Delivered))
                    return 1;

                if (id == ExistingTestOrder.Id && status == WorkOrderStatus.WaitingForApproval)
                    return 1;

                return 0;
            });

            Repository.AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var service = callInfo.ArgAt<IMechanicalService>(1);

                if (id == ExistingReceivedOrder.Id && service.Id == ExistingService.Id)
                    return 1;

                return 0;
            });

            Repository.UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var service = callInfo.ArgAt<IMechanicalService>(1);

                if (id == ExistingOrderInDiagnosis.Id && service.Id == ExistingService.Id)
                    return 1;

                return 0;
            });

            Repository.DeleteServiceFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var serviceId = callInfo.ArgAt<Guid>(1);

                if (id == ExistingOrderInDiagnosis.Id && serviceId == ExistingService.Id)
                    return 1;

                return 0;
            });

            Repository.AddPartToOrder(Arg.Any<Guid>(), Arg.Any<IPart>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var part = callInfo.ArgAt<IPart>(1);

                if (id == ExistingReceivedOrder.Id && part.Id == ExistingPart.Id)
                    return 1;

                return 0;
            });

            Repository.UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var part = callInfo.ArgAt<IPart>(1);

                if (id == ExistingOrderInDiagnosis.Id && part.Id == ExistingPart.Id)
                    return 1;

                return 0;
            });

            Repository.RemovePartFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var partId = callInfo.ArgAt<Guid>(1);

                if (id == ExistingOrderInDiagnosisId && partId == ExistingPart2.Id)
                    return 1;

                return 0;
            });

            Repository.UpdateOrderBudget(Arg.Any<Guid>(), Arg.Any<double>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var value = callInfo.ArgAt<double>(1);

                if (id == ExistingOrderInDiagnosisId && value > 0)
                    return 1;

                return 0;
            });

            Repository.UpdateOrderDuration(Arg.Any<Guid>(), Arg.Any<TimeSpan>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var value = callInfo.ArgAt<TimeSpan>(1);

                if (id == ExistingOrderInDiagnosisId && value > TimeSpan.Zero)
                    return 1;

                return 0;
            });

            Repository.DeleteOrder(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingReceivedOrder.Id)
                    return 1;

                return 0;
            });

            CustomerService = Substitute.For<ICustomerService>();

            CustomerService.GetCustomer(Arg.Any<string>()).Returns(callInfo =>
            {
                var document = callInfo.ArgAt<string>(0);

                if (document == ExistingCustomer.Document)
                    return ExistingCustomer;

                if (document == ExistingFailCustomer.Document)
                    return ExistingFailCustomer;

                return null;
            });

            VehicleService = Substitute.For<IVehicleService>();

            VehicleService.GetVehicle(Arg.Any<string>()).Returns(callInfo =>
            {
                var license = callInfo.ArgAt<string>(0);

                if (license == ExistingVehicle.LicensePlate)
                    return ExistingVehicle;

                return null;
            });

            StockService = Substitute.For<IStockService>();

            StockService.GetPart(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingPart.Id)
                    return ExistingPartDto;

                if (id == ExistingPart2.Id)
                    return ExistingPart2Dto;

                return null;
            });

            StockService.ReservePartAmount(Arg.Any<PartUpdateDto<int>>()).Returns(Task.CompletedTask);
            StockService.RestorePartAmount(Arg.Any<PartUpdateDto<int>>()).Returns(Task.CompletedTask);

            MechanicalService = Substitute.For<IMechanicalServiceService>();

            MechanicalService.GetService(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingService.Id)
                    return ExistingServiceDto;

                if (id == ExistingService2.Id)
                    return ExistingService2Dto;

                return null;
            });

            Service = new WorkOrderService(Repository, CustomerService, VehicleService, StockService, MechanicalService);
        }

        [Test]
        public async Task MustCreateOrder()
        {
            await Service.CreateServiceOrder(OrderToCreate);

            await CustomerService.Received(1).GetCustomer(ExistingCustomer.Document);
            await VehicleService.Received(1).GetVehicle(ExistingVehicle.LicensePlate);
            await Repository.ReceivedWithAnyArgs(1).CreateOrder(Arg.Any<IWorkOrder>());
        }

        [Test]
        public async Task MustNotCreateOrderIfCustomerNotExists()
        {
            var order = new CreateOrderDto("000.000.000-00", ExistingVehicle.LicensePlate);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CreateServiceOrder(order));

            await CustomerService.Received(1).GetCustomer(order.CustomerDocument);
            await VehicleService.ReceivedWithAnyArgs(0).GetVehicle(Arg.Any<string>());
            await Repository.ReceivedWithAnyArgs(0).CreateOrder(Arg.Any<IWorkOrder>());
        }

        [Test]
        public async Task MustNotCreateOrderIfVehicleNotExists()
        {
            var order = new CreateOrderDto(ExistingCustomer.Document, "AAA0000");

            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CreateServiceOrder(order));

            await CustomerService.Received(1).GetCustomer(ExistingCustomer.Document);
            await VehicleService.Received(1).GetVehicle(order.VehicleLicensePlate);
            await Repository.ReceivedWithAnyArgs(0).CreateOrder(Arg.Any<IWorkOrder>());
        }

        [Test]
        public async Task MustFailToCreateOrder()
        {
            var order = new CreateOrderDto(ExistingFailCustomer.Document, ExistingVehicle.LicensePlate);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CreateServiceOrder(order));

            await CustomerService.Received(1).GetCustomer(ExistingFailCustomer.Document);
            await VehicleService.Received(1).GetVehicle(ExistingVehicle.LicensePlate);
            await Repository.ReceivedWithAnyArgs(1).CreateOrder(Arg.Any<IWorkOrder>());
        }

        [Test]
        public async Task MustGetOrders()
        {
            var orders = await Service.GetOrders();
            var ordersList = orders.ToList();

            await Repository.Received(1).GetOrders();

            Assert.That(ordersList, Has.Count.EqualTo(3));
            Assert.That(ordersList[0], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Id, Is.EqualTo(ExistingReceivedOrder.Id));
                Assert.That(ordersList[0].CustomerDocument, Is.EqualTo(ExistingReceivedOrder.CustomerDocument.Id));
                Assert.That(ordersList[0].VehicleLicensePlate, Is.EqualTo(ExistingReceivedOrder.VehicleLicensePlate.License));
                Assert.That(ordersList[0].Budget, Is.EqualTo(ExistingReceivedOrder.Budget));
                Assert.That(ordersList[0].Status, Is.EqualTo(ExistingReceivedOrder.Status.ToString()));
                Assert.That(ordersList[0].DateCreated.Date, Is.EqualTo(ExistingReceivedOrder.DateCreated.Date));
                Assert.That(ordersList[0].DateCreated.Hour, Is.EqualTo(ExistingReceivedOrder.DateCreated.Hour));
                Assert.That(ordersList[0].DateCreated.Minute, Is.EqualTo(ExistingReceivedOrder.DateCreated.Minute));
                Assert.That(ordersList[0].DateCreated.Second, Is.EqualTo(ExistingReceivedOrder.DateCreated.Second));
                Assert.That(ordersList[0].DateFinished.Date, Is.EqualTo(ExistingReceivedOrder.DateFinished.Date));
                Assert.That(ordersList[0].DateFinished.Hour, Is.EqualTo(ExistingReceivedOrder.DateFinished.Hour));
                Assert.That(ordersList[0].DateFinished.Minute, Is.EqualTo(ExistingReceivedOrder.DateFinished.Minute));
                Assert.That(ordersList[0].DateFinished.Second, Is.EqualTo(ExistingReceivedOrder.DateFinished.Second));
            });

            Assert.That(ordersList[1], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[1].Id, Is.EqualTo(ExistingOrderInDiagnosis.Id));
                Assert.That(ordersList[1].CustomerDocument, Is.EqualTo(ExistingOrderInDiagnosis.CustomerDocument.Id));
                Assert.That(ordersList[1].VehicleLicensePlate, Is.EqualTo(ExistingOrderInDiagnosis.VehicleLicensePlate.License));
                Assert.That(ordersList[1].Budget, Is.EqualTo(ExistingOrderInDiagnosis.Budget));
                Assert.That(ordersList[1].Status, Is.EqualTo(ExistingOrderInDiagnosis.Status.ToString()));
                Assert.That(ordersList[1].DateCreated.Date, Is.EqualTo(ExistingOrderInDiagnosis.DateCreated.Date));
                Assert.That(ordersList[1].DateCreated.Hour, Is.EqualTo(ExistingOrderInDiagnosis.DateCreated.Hour));
                Assert.That(ordersList[1].DateCreated.Minute, Is.EqualTo(ExistingOrderInDiagnosis.DateCreated.Minute));
                Assert.That(ordersList[1].DateCreated.Second, Is.EqualTo(ExistingOrderInDiagnosis.DateCreated.Second));
                Assert.That(ordersList[1].DateFinished.Date, Is.EqualTo(ExistingOrderInDiagnosis.DateFinished.Date));
                Assert.That(ordersList[1].DateFinished.Hour, Is.EqualTo(ExistingOrderInDiagnosis.DateFinished.Hour));
                Assert.That(ordersList[1].DateFinished.Minute, Is.EqualTo(ExistingOrderInDiagnosis.DateFinished.Minute));
                Assert.That(ordersList[1].DateFinished.Second, Is.EqualTo(ExistingOrderInDiagnosis.DateFinished.Second));
            });

            Assert.That(ordersList[2], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[2].Id, Is.EqualTo(ExistingTestOrder.Id));
                Assert.That(ordersList[2].CustomerDocument, Is.EqualTo(ExistingTestOrder.CustomerDocument.Id));
                Assert.That(ordersList[2].VehicleLicensePlate, Is.EqualTo(ExistingTestOrder.VehicleLicensePlate.License));
                Assert.That(ordersList[2].Budget, Is.EqualTo(ExistingTestOrder.Budget));
                Assert.That(ordersList[2].Status, Is.EqualTo(ExistingTestOrder.Status.ToString()));
                Assert.That(ordersList[2].DateCreated.Date, Is.EqualTo(ExistingTestOrder.DateCreated.Date));
                Assert.That(ordersList[2].DateCreated.Hour, Is.EqualTo(ExistingTestOrder.DateCreated.Hour));
                Assert.That(ordersList[2].DateCreated.Minute, Is.EqualTo(ExistingTestOrder.DateCreated.Minute));
                Assert.That(ordersList[2].DateCreated.Second, Is.EqualTo(ExistingTestOrder.DateCreated.Second));
                Assert.That(ordersList[2].DateFinished.Date, Is.EqualTo(ExistingTestOrder.DateFinished.Date));
                Assert.That(ordersList[2].DateFinished.Hour, Is.EqualTo(ExistingTestOrder.DateFinished.Hour));
                Assert.That(ordersList[2].DateFinished.Minute, Is.EqualTo(ExistingTestOrder.DateFinished.Minute));
                Assert.That(ordersList[2].DateFinished.Second, Is.EqualTo(ExistingTestOrder.DateFinished.Second));
            });
        }

        [Test]
        public async Task MustGetOrderById()
        {
            var order = await Service.GetOrder(ExistingReceivedOrder.Id);

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);

            Assert.That(order, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(order.Id, Is.EqualTo(ExistingReceivedOrder.Id));
                Assert.That(order.CustomerDocument, Is.EqualTo(ExistingReceivedOrder.CustomerDocument.Id));
                Assert.That(order.VehicleLicensePlate, Is.EqualTo(ExistingReceivedOrder.VehicleLicensePlate.License));
                Assert.That(order.Services, Is.Empty);
                Assert.That(order.Parts, Is.Empty);
                Assert.That(order.Budget, Is.EqualTo(ExistingReceivedOrder.Budget));
                Assert.That(order.Status, Is.EqualTo(ExistingReceivedOrder.Status.ToString()));
                Assert.That(order.DateCreated.Date, Is.EqualTo(ExistingReceivedOrder.DateCreated.Date));
                Assert.That(order.DateCreated.Hour, Is.EqualTo(ExistingReceivedOrder.DateCreated.Hour));
                Assert.That(order.DateCreated.Minute, Is.EqualTo(ExistingReceivedOrder.DateCreated.Minute));
                Assert.That(order.DateCreated.Second, Is.EqualTo(ExistingReceivedOrder.DateCreated.Second));
                Assert.That(order.DateFinished.Date, Is.EqualTo(ExistingReceivedOrder.DateFinished.Date));
                Assert.That(order.DateFinished.Hour, Is.EqualTo(ExistingReceivedOrder.DateFinished.Hour));
                Assert.That(order.DateFinished.Minute, Is.EqualTo(ExistingReceivedOrder.DateFinished.Minute));
                Assert.That(order.DateFinished.Second, Is.EqualTo(ExistingReceivedOrder.DateFinished.Second));
            });
        }

        [Test]
        public async Task MustNotGetOrderByIdIfNotExists()
        {
            var order = await Service.GetOrder(Guid.NewGuid());

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());

            Assert.That(order, Is.Null);
        }

        [Test]
        public async Task MustGetCustomerOrders()
        {
            var orders = await Service.GetCustomerOrders(ExistingCustomer.Document);
            var ordersList = orders.ToList();

            await CustomerService.Received(1).GetCustomer(ExistingCustomer.Document);
            await Repository.Received(1).GetCustomerOrders(ExistingCustomer.Document);

            Assert.That(ordersList, Has.Count.EqualTo(3));
            Assert.That(ordersList[0], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Id, Is.EqualTo(ExistingReceivedOrder.Id));
                Assert.That(ordersList[0].CustomerDocument, Is.EqualTo(ExistingReceivedOrder.CustomerDocument.Id));
                Assert.That(ordersList[0].VehicleLicensePlate, Is.EqualTo(ExistingReceivedOrder.VehicleLicensePlate.License));
                Assert.That(ordersList[0].Services, Is.Empty);
                Assert.That(ordersList[0].Parts, Is.Empty);
                Assert.That(ordersList[0].Budget, Is.EqualTo(ExistingReceivedOrder.Budget));
                Assert.That(ordersList[0].Status, Is.EqualTo(ExistingReceivedOrder.Status.ToString()));
                Assert.That(ordersList[0].DateCreated.Date, Is.EqualTo(ExistingReceivedOrder.DateCreated.Date));
                Assert.That(ordersList[0].DateCreated.Hour, Is.EqualTo(ExistingReceivedOrder.DateCreated.Hour));
                Assert.That(ordersList[0].DateCreated.Minute, Is.EqualTo(ExistingReceivedOrder.DateCreated.Minute));
                Assert.That(ordersList[0].DateCreated.Second, Is.EqualTo(ExistingReceivedOrder.DateCreated.Second));
                Assert.That(ordersList[0].DateFinished.Date, Is.EqualTo(ExistingReceivedOrder.DateFinished.Date));
                Assert.That(ordersList[0].DateFinished.Hour, Is.EqualTo(ExistingReceivedOrder.DateFinished.Hour));
                Assert.That(ordersList[0].DateFinished.Minute, Is.EqualTo(ExistingReceivedOrder.DateFinished.Minute));
                Assert.That(ordersList[0].DateFinished.Second, Is.EqualTo(ExistingReceivedOrder.DateFinished.Second));
            });

            Assert.That(ordersList[1], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[1].Id, Is.EqualTo(ExistingOrderInDiagnosis.Id));
                Assert.That(ordersList[1].CustomerDocument, Is.EqualTo(ExistingOrderInDiagnosis.CustomerDocument.Id));
                Assert.That(ordersList[1].VehicleLicensePlate, Is.EqualTo(ExistingOrderInDiagnosis.VehicleLicensePlate.License));
                Assert.That(ordersList[1].Budget, Is.EqualTo(ExistingOrderInDiagnosis.Budget));
                Assert.That(ordersList[1].Status, Is.EqualTo(ExistingOrderInDiagnosis.Status.ToString()));
                Assert.That(ordersList[1].DateCreated.Date, Is.EqualTo(ExistingOrderInDiagnosis.DateCreated.Date));
                Assert.That(ordersList[1].DateCreated.Hour, Is.EqualTo(ExistingOrderInDiagnosis.DateCreated.Hour));
                Assert.That(ordersList[1].DateCreated.Minute, Is.EqualTo(ExistingOrderInDiagnosis.DateCreated.Minute));
                Assert.That(ordersList[1].DateCreated.Second, Is.EqualTo(ExistingOrderInDiagnosis.DateCreated.Second));
                Assert.That(ordersList[1].DateFinished.Date, Is.EqualTo(ExistingOrderInDiagnosis.DateFinished.Date));
                Assert.That(ordersList[1].DateFinished.Hour, Is.EqualTo(ExistingOrderInDiagnosis.DateFinished.Hour));
                Assert.That(ordersList[1].DateFinished.Minute, Is.EqualTo(ExistingOrderInDiagnosis.DateFinished.Minute));
                Assert.That(ordersList[1].DateFinished.Second, Is.EqualTo(ExistingOrderInDiagnosis.DateFinished.Second));
            });

            Assert.That(ordersList[2], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[2].Id, Is.EqualTo(ExistingTestOrder.Id));
                Assert.That(ordersList[2].CustomerDocument, Is.EqualTo(ExistingTestOrder.CustomerDocument.Id));
                Assert.That(ordersList[2].VehicleLicensePlate, Is.EqualTo(ExistingTestOrder.VehicleLicensePlate.License));
                Assert.That(ordersList[2].Budget, Is.EqualTo(ExistingTestOrder.Budget));
                Assert.That(ordersList[2].Status, Is.EqualTo(ExistingTestOrder.Status.ToString()));
                Assert.That(ordersList[2].DateCreated.Date, Is.EqualTo(ExistingTestOrder.DateCreated.Date));
                Assert.That(ordersList[2].DateCreated.Hour, Is.EqualTo(ExistingTestOrder.DateCreated.Hour));
                Assert.That(ordersList[2].DateCreated.Minute, Is.EqualTo(ExistingTestOrder.DateCreated.Minute));
                Assert.That(ordersList[2].DateCreated.Second, Is.EqualTo(ExistingTestOrder.DateCreated.Second));
                Assert.That(ordersList[2].DateFinished.Date, Is.EqualTo(ExistingTestOrder.DateFinished.Date));
                Assert.That(ordersList[2].DateFinished.Hour, Is.EqualTo(ExistingTestOrder.DateFinished.Hour));
                Assert.That(ordersList[2].DateFinished.Minute, Is.EqualTo(ExistingTestOrder.DateFinished.Minute));
                Assert.That(ordersList[2].DateFinished.Second, Is.EqualTo(ExistingTestOrder.DateFinished.Second));
            });
        }

        [Test]
        public async Task MustNotGetCustomerOrdersIfCustomerNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.GetCustomerOrders("000.000.000-00"));

            await CustomerService.Received(1).GetCustomer("000.000.000-00");
            await Repository.ReceivedWithAnyArgs(0).GetCustomerOrders(Arg.Any<string>());
        }

        [Test]
        public async Task MustStartDiagnosis()
        {
            await Service.StartDiagnosis(ExistingReceivedOrder.Id);

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await Repository.Received(1).UpdateOrderStatus(ExistingReceivedOrder.Id, WorkOrderStatus.InDiagnosis);
        }

        [Test]
        public async Task MustNotStartDiagnosisIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.StartDiagnosis(Guid.NewGuid()));

            await Repository.Received(1).GetOrder(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateOrderStatus(Arg.Any<Guid>(), Arg.Any<WorkOrderStatus>());
        }

        [Test]
        public async Task MustFailToStartDiagnosis()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.StartDiagnosis(ExistingOrderInDiagnosis.Id));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await Repository.Received(1).UpdateOrderStatus(ExistingOrderInDiagnosis.Id, WorkOrderStatus.InDiagnosis);
        }

        [Test]
        public async Task MustAddNewServiceToOrder()
        {
            await Service.AddServiceToOrder(AddNewServiceToOrder);

            await Repository.Received(1).GetOrder(AddNewServiceToOrder.OrderId);
            await MechanicalService.Received(1).GetService(AddNewServiceToOrder.ItemId);
            await Repository.Received(1).AddServiceToOrder(AddNewServiceToOrder.OrderId, Arg.Any<IMechanicalService>());
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustAddServiceAmountToOrder()
        {
            await Service.AddServiceToOrder(AddServiceAmountToOrder);

            await Repository.Received(1).GetOrder(AddServiceAmountToOrder.OrderId);
            await MechanicalService.ReceivedWithAnyArgs(0).GetService(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
            await Repository.Received(1).UpdateServiceOfOrder(AddServiceAmountToOrder.OrderId, Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustNotAddServiceIfOrderNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddServiceToOrder(new OrderUpdateItemDto(Guid.NewGuid(), ExistingService.Id, 1)));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await MechanicalService.ReceivedWithAnyArgs(0).GetService(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustNotAddServiceIfServiceNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddServiceToOrder(new OrderUpdateItemDto(ExistingReceivedOrder.Id, Guid.NewGuid(), 1)));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await MechanicalService.ReceivedWithAnyArgs(1).GetService(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustFailToAddServiceToOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddServiceToOrder(new OrderUpdateItemDto(ExistingReceivedOrder.Id, ExistingService2.Id, 1)));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await MechanicalService.Received(1).GetService(ExistingService2.Id);
            await Repository.ReceivedWithAnyArgs(1).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustFailToAddServiceAmountToOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddServiceToOrder(new OrderUpdateItemDto(ExistingOrderInDiagnosis.Id, ExistingService2.Id, 1)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await MechanicalService.ReceivedWithAnyArgs(0).GetService(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
            await Repository.ReceivedWithAnyArgs(1).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustRemoveServiceOfOrder()
        {
            await Service.RemoveServiceOfOrder(RemoveServiceOfOrder);

            await Repository.Received(1).GetOrder(RemoveServiceOfOrder.OrderId);
            await Repository.Received(1).DeleteServiceFromOrder(RemoveServiceOfOrder.OrderId, Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustRemoveServiceAmountOfOrder()
        {
            await Service.RemoveServiceOfOrder(RemoveServiceAmountOfOrder);

            await Repository.Received(1).GetOrder(AddServiceAmountToOrder.OrderId);
            await Repository.ReceivedWithAnyArgs(0).DeleteServiceFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.Received(1).UpdateServiceOfOrder(AddServiceAmountToOrder.OrderId, Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustNotRemoveServiceIfOrderNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemoveServiceOfOrder(new OrderUpdateItemDto(Guid.NewGuid(), ExistingService.Id, 1)));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).DeleteServiceFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustFailToRemoveServiceOfOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemoveServiceOfOrder(new OrderUpdateItemDto(ExistingOrderInDiagnosis.Id, ExistingService2.Id, 4)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await Repository.ReceivedWithAnyArgs(1).DeleteServiceFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustFailToRemoveServiceAmountToOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemoveServiceOfOrder(new OrderUpdateItemDto(ExistingOrderInDiagnosis.Id, ExistingService2.Id, 2)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await Repository.ReceivedWithAnyArgs(0).DeleteServiceFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(1).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustDeleteServiceFromOrder()
        {
            await Service.DeleteServiceFromOrder(new(ExistingOrderInDiagnosisId, ExistingService.Id, 1));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await Repository.Received(1).DeleteServiceFromOrder(ExistingOrderInDiagnosisId, ExistingService.Id);
        }

        [Test]
        public async Task MustNotDeleteServiceFromOrderIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeleteServiceFromOrder(new(Guid.NewGuid(), ExistingService.Id, 1)));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).DeleteServiceFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
        }

        [Test]
        public async Task MustFailToDeleteServiceFromOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeleteServiceFromOrder(new(ExistingOrderInDiagnosisId, ExistingService2.Id, 1)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await Repository.Received(1).DeleteServiceFromOrder(ExistingOrderInDiagnosisId, ExistingService2.Id);
        }

        [Test]
        public async Task MustAddPartToOrder()
        {
            await Service.AddPartOrSupplieToOrder(new(ExistingReceivedOrder.Id, ExistingPart.Id, 1));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await StockService.ReceivedWithAnyArgs(1).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(1).AddPartToOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await Repository.ReceivedWithAnyArgs(0).UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await StockService.ReceivedWithAnyArgs(0).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustAddPartAmountToOrder()
        {
            await Service.AddPartOrSupplieToOrder(new(ExistingOrderInDiagnosis.Id, ExistingPart.Id, 1));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await StockService.ReceivedWithAnyArgs(1).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(0).AddPartToOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await Repository.ReceivedWithAnyArgs(1).UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await StockService.ReceivedWithAnyArgs(0).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustNotAddPartToOrderIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddPartOrSupplieToOrder(new(Guid.NewGuid(), ExistingPart.Id, 1)));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await StockService.ReceivedWithAnyArgs(0).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(0).AddPartToOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await Repository.ReceivedWithAnyArgs(0).UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await StockService.ReceivedWithAnyArgs(0).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustNotAddPartToOrderIfPartNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddPartOrSupplieToOrder(new(ExistingReceivedOrder.Id, Guid.NewGuid(), 1)));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await StockService.ReceivedWithAnyArgs(1).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(0).AddPartToOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await Repository.ReceivedWithAnyArgs(0).UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await StockService.ReceivedWithAnyArgs(1).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustFailToAddPartToOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddPartOrSupplieToOrder(new(ExistingReceivedOrder.Id, ExistingPart2.Id, 1)));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await StockService.ReceivedWithAnyArgs(1).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(1).AddPartToOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await Repository.ReceivedWithAnyArgs(0).UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await StockService.ReceivedWithAnyArgs(1).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustFailToAddPartAmountToOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddPartOrSupplieToOrder(new(ExistingOrderInDiagnosis.Id, ExistingPart2.Id, 1)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await StockService.ReceivedWithAnyArgs(1).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(0).AddPartToOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await Repository.ReceivedWithAnyArgs(1).UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await StockService.ReceivedWithAnyArgs(1).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustRemovePartFromOrder()
        {
            await Service.RemovePartOrSupplieFromOrder(new(ExistingOrderInDiagnosisId, ExistingPart2.Id, 1));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await StockService.ReceivedWithAnyArgs(1).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.Received(1).RemovePartFromOrder(ExistingOrderInDiagnosisId, ExistingPart2.Id);
            await Repository.ReceivedWithAnyArgs(0).UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await StockService.ReceivedWithAnyArgs(0).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustRemovePartAmountFromOrder()
        {
            await Service.RemovePartOrSupplieFromOrder(new(ExistingOrderInDiagnosisId, ExistingPart.Id, 2));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await StockService.ReceivedWithAnyArgs(1).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(0).RemovePartFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(1).UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await StockService.ReceivedWithAnyArgs(0).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustNotRemovePartFromOrderIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemovePartOrSupplieFromOrder(new(Guid.NewGuid(), ExistingPart.Id, 1)));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await StockService.ReceivedWithAnyArgs(0).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(0).RemovePartFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await StockService.ReceivedWithAnyArgs(0).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustNotRemovePartFromOrderIfPartNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemovePartOrSupplieFromOrder(new(ExistingReceivedOrder.Id, Guid.NewGuid(), 1)));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await StockService.ReceivedWithAnyArgs(1).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(0).RemovePartFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await StockService.ReceivedWithAnyArgs(1).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustFailToRemovePartFromOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemovePartOrSupplieFromOrder(new(ExistingOrderInDiagnosisId, ExistingPart.Id, 4)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await StockService.ReceivedWithAnyArgs(1).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(1).RemovePartFromOrder(ExistingOrderInDiagnosisId, ExistingPart.Id);
            await Repository.ReceivedWithAnyArgs(0).UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await StockService.ReceivedWithAnyArgs(1).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustFailToRemovePartAmountFromOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddPartOrSupplieToOrder(new(ExistingOrderInDiagnosis.Id, ExistingPart2.Id, 0)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await StockService.ReceivedWithAnyArgs(1).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(0).RemovePartFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(1).UpdatePartFromOrder(Arg.Any<Guid>(), Arg.Any<IPart>());
            await StockService.ReceivedWithAnyArgs(1).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustComopleteDiagnosis()
        {
            await Service.CompleteDiagnosis(ExistingOrderInDiagnosisId);

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await Repository.Received(1).UpdateOrderStatus(ExistingOrderInDiagnosisId, WorkOrderStatus.WaitingForApproval);
            await Repository.ReceivedWithAnyArgs(1).UpdateOrderBudget(ExistingOrderInDiagnosisId, Arg.Any<double>());
            await Repository.Received(0).UpdateOrderStatus(ExistingOrderInDiagnosisId, WorkOrderStatus.InDiagnosis);

        }

        [Test]
        public async Task MustNotCompleteDiagnosisIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CompleteDiagnosis(Guid.NewGuid()));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await Repository.Received(0).UpdateOrderStatus(ExistingOrderInDiagnosisId, WorkOrderStatus.WaitingForApproval);
            await Repository.ReceivedWithAnyArgs(0).UpdateOrderBudget(ExistingOrderInDiagnosisId, Arg.Any<double>());
            await Repository.Received(0).UpdateOrderStatus(ExistingOrderInDiagnosisId, WorkOrderStatus.InDiagnosis);

        }

        [Test]
        public async Task MustFailToCompleteDiagnosisWhenUpdateStatus()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CompleteDiagnosis(ExistingReceivedOrder.Id));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await Repository.Received(1).UpdateOrderStatus(ExistingReceivedOrder.Id, WorkOrderStatus.WaitingForApproval);
            await Repository.ReceivedWithAnyArgs(0).UpdateOrderBudget(ExistingOrderInDiagnosisId, Arg.Any<double>());
            await Repository.Received(0).UpdateOrderStatus(ExistingOrderInDiagnosisId, WorkOrderStatus.InDiagnosis);

        }

        [Test]
        public async Task MustFailToCompleteDiagnosisWhenUpdateBudget()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CompleteDiagnosis(ExistingTestOrder.Id));

            await Repository.Received(1).GetOrder(ExistingTestOrder.Id);
            await Repository.Received(1).UpdateOrderStatus(ExistingTestOrder.Id, WorkOrderStatus.WaitingForApproval);
            await Repository.ReceivedWithAnyArgs(1).UpdateOrderBudget(ExistingTestOrder.Id, Arg.Any<double>());
            await Repository.Received(1).UpdateOrderStatus(ExistingTestOrder.Id, WorkOrderStatus.InDiagnosis);
        }

        [Test]
        public async Task MustApproveBudget()
        {
            await Service.ApproveBudget(new(ExistingReceivedOrder.Id, true));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await StockService.ReceivedWithAnyArgs(0).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.Received(1).UpdateOrderStatus(ExistingReceivedOrder.Id, WorkOrderStatus.WaitingForExecution);
        }

        [Test]
        public async Task MustRefuseBudget()
        {
            await Service.ApproveBudget(new(ExistingReceivedOrder.Id, false));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await StockService.ReceivedWithAnyArgs(0).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.Received(1).UpdateOrderStatus(ExistingReceivedOrder.Id, WorkOrderStatus.Finished);
        }

        [Test]
        public async Task MustRefuseBudgetWithPartsToRestore()
        {
            await Service.ApproveBudget(new(ExistingOrderInDiagnosisId, false));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await StockService.ReceivedWithAnyArgs(2).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.Received(1).UpdateOrderStatus(ExistingOrderInDiagnosisId, WorkOrderStatus.Finished);
        }

        [Test]
        public async Task MustNotApproveOrRefuseBudgetIfOrderNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.ApproveBudget(new(Guid.NewGuid(), false)));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await StockService.ReceivedWithAnyArgs(0).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(0).UpdateOrderStatus(Arg.Any<Guid>(), Arg.Any<WorkOrderStatus>());
        }

        [Test]
        public async Task MustFailToApproveOrRefuseBudget()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.ApproveBudget(new(ExistingTestOrder.Id, false)));

            await Repository.Received(1).GetOrder(ExistingTestOrder.Id);
            await StockService.ReceivedWithAnyArgs(0).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
            await Repository.ReceivedWithAnyArgs(1).UpdateOrderStatus(ExistingTestOrder.Id, WorkOrderStatus.Finished);
        }

        [Test]
        public async Task MustStartExecution()
        {
            await Service.StartExecution(ExistingOrderInDiagnosisId);

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await Repository.Received(1).UpdateOrderStatus(ExistingOrderInDiagnosisId, WorkOrderStatus.InExecution);
        }

        [Test]
        public async Task MustNotStartExecutionIfOrderNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.StartExecution(Guid.NewGuid()));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateOrderStatus(Arg.Any<Guid>(), Arg.Any<WorkOrderStatus>());
        }

        [Test]
        public async Task MustFailToStartExecution()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.StartExecution(ExistingReceivedOrder.Id));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await Repository.Received(1).UpdateOrderStatus(ExistingReceivedOrder.Id, WorkOrderStatus.InExecution);
        }

        [Test]
        public async Task MustCompleteExecution()
        {
            await Service.CompleteExecution(ExistingOrderInDiagnosisId);

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await Repository.Received(1).UpdateOrderStatus(ExistingOrderInDiagnosisId, WorkOrderStatus.Finished);
        }

        [Test]
        public async Task MustNotCompleteExecutionIfOrderNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CompleteExecution(Guid.NewGuid()));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateOrderStatus(Arg.Any<Guid>(), Arg.Any<WorkOrderStatus>());
        }

        [Test]
        public async Task MustFailToCompleteExecution()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CompleteExecution(ExistingTestOrder.Id));

            await Repository.Received(1).GetOrder(ExistingTestOrder.Id);
            await Repository.Received(1).UpdateOrderStatus(ExistingTestOrder.Id, WorkOrderStatus.Finished);
        }

        [Test]
        public async Task MustDeliverVehicle()
        {
            await Service.VehicleDelivered(ExistingOrderInDiagnosisId);

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await Repository.Received(1).UpdateOrderStatus(ExistingOrderInDiagnosisId, WorkOrderStatus.Delivered);
        }

        [Test]
        public async Task MustNotDeliverVehicleIfOrderNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.VehicleDelivered(Guid.NewGuid()));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateOrderStatus(Arg.Any<Guid>(), Arg.Any<WorkOrderStatus>());
        }

        [Test]
        public async Task MustFailToDeliverVehicle()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.VehicleDelivered(ExistingTestOrder.Id));

            await Repository.Received(1).GetOrder(ExistingTestOrder.Id);
            await Repository.Received(1).UpdateOrderStatus(ExistingTestOrder.Id, WorkOrderStatus.Delivered);
        }

        [Test]
        public async Task MustDeleteOrder()
        {
            await Service.DeleteOrder(ExistingReceivedOrder.Id);

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await Repository.Received(1).DeleteOrder(ExistingReceivedOrder.Id);
        }

        [Test]
        public async Task MustNotDeleteOrderIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeleteOrder(Guid.NewGuid()));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).DeleteOrder(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustFailToDeleteOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeleteOrder(ExistingOrderInDiagnosisId));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await Repository.Received(1).DeleteOrder(ExistingOrderInDiagnosisId);
        }

        private static IMechanicalService CreateSubstituteService(Guid id, string description, float hours, double pricePerHour, int amount)
        {
            var service = Substitute.For<IMechanicalService>();
            service.Id.Returns(id);
            service.Description.Returns(description);
            service.Hours.Returns(hours);
            service.PricePerHour.Returns(pricePerHour);
            service.Price.Returns(hours * pricePerHour);
            service.Amount.Returns(amount);

            service.When(x => x.AddServiceAmount(Arg.Any<int>())).Do(callInfo =>
            {
                var amount = callInfo.ArgAt<int>(0);

                service.Amount.Returns(service.Amount + amount);
            });

            service.When(x => x.RemoveServiceAmount(Arg.Any<int>())).Do(callInfo =>
            {
                var amount = callInfo.ArgAt<int>(0);

                service.Amount.Returns(service.Amount - amount);
            });

            return service;
        }

        private static IPart CreateSubstitutePart(Guid id, string name, string brand, double price, int amount, int reservedAmount)
        {
            var part = Substitute.For<IPart>();
            part.Id.Returns(id);
            part.Name.Returns(name);
            part.Brand.Returns(brand);
            part.Price.Returns(price);
            part.Amount.Returns(amount);
            part.ReservedAmount.Returns(reservedAmount);

            part.When(x => x.AddAmount(Arg.Any<int>())).Do(callInfo =>
            {
                var amount = callInfo.ArgAt<int>(0);

                part.Amount.Returns(part.Amount + amount);
            });

            part.When(x => x.RemoveAmount(Arg.Any<int>())).Do(callInfo =>
            {
                var amount = callInfo.ArgAt<int>(0);

                part.Amount.Returns(part.Amount - amount);
            });

            return part;
        }

        private static IWorkOrder CreateSubstituteOrder(Guid id, List<IMechanicalService> services, List<IPart> parts, double budget, WorkOrderStatus status)
        {
            var order = Substitute.For<IWorkOrder>();
            order.Id.Returns(id);
            order.CustomerDocument.Id.Returns(ExistingCustomer.Document);
            order.VehicleLicensePlate.License.Returns(ExistingVehicle.LicensePlate);
            order.Services.Returns(services);
            order.Parts.Returns(parts);
            order.Budget.Returns(budget);
            order.Status.Returns(status);
            order.DateCreated.Returns(DateTime.Now);
            order.DateFinished.Returns(DateTime.MinValue);

            order.AddPartOrSupplie(Arg.Any<IPart>()).Returns(callInfo =>
            {
                return callInfo.ArgAt<IPart>(0);
            });

            order.When(x => x.FinalizeDiagnosis()).Do(_ =>
            {
                order.Status.Returns(WorkOrderStatus.WaitingForApproval);

                if (order.Id != ExistingTestOrder.Id)
                    order.Budget.Returns(1.0);
            });

            order.When(x => x.ApproveService(Arg.Any<bool>())).Do(callInfo =>
            {
                var approved = callInfo.ArgAt<bool>(0);
                var status = approved ? WorkOrderStatus.WaitingForExecution : WorkOrderStatus.Finished;

                order.Status.Returns(status);
            });

            order.When(x => x.StartService()).Do(_ => order.Status.Returns(WorkOrderStatus.InExecution));

            order.When(x => x.CompleteService()).Do(_ =>
            {
                order.Status.Returns(WorkOrderStatus.Finished);
                order.Duration.Returns(TimeSpan.FromHours(6));
            });

            order.When(x => x.VehicleDelivered()).Do(_ => order.Status.Returns(WorkOrderStatus.Delivered));

            return order;
        }
    }
}
