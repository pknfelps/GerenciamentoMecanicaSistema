using Domain.Interface.Custumer;
using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Domain.Interface.Vehicle;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Dto;
using Service.Interface.Dto.Order;
using Service.Interface.Dto.Service;
using Service.Interface.Dto.Stock;
using Service.Interface.Dto.Vehicle;
using Service.Interface.Results.Customer;

namespace ServiceTests
{
    public class OrdersServiceTests
    {
        private IOrdersService Service { get; set; }
        private IOrdersRepository Repository { get; set; }
        private ICustomerService CustomerService { get; set; }
        private IVehicleService VehicleService { get; set; }
        private IStockService StockService { get; set; }
        private ICatalogService MechanicalService { get; set; }
        private IEmailService EmailService { get; set; }

        private static CustomerResult ExistingCustomer { get; } = new(Guid.NewGuid(), "Teste", "417.384.220-11", "(11) 91234-5678", "teste@gmail.com");

        private static CustomerResult ExistingFailCustomer { get; } = new(Guid.NewGuid(), "Teste", "662.119.730-63", "(11) 91234-5678", "teste@gmail.com");

        private static VehicleDto ExistingVehicle { get; } = new(Guid.NewGuid(), ExistingCustomer.Document, "Honda", "Civic", 2026, "CVC2026");

        private static CreateOrderDto OrderToCreate { get; } = new(ExistingCustomer.Document, ExistingVehicle.LicensePlate);

        private static IMechanicalService ExistingService { get; } = CreateSubstituteService(Guid.NewGuid(), "Revisão", 6, 100, 1);
        private static ServiceDto ExistingServiceDto { get; } = new(ExistingService.Id, "Revisão", 6, 100, 1);
        private static IMechanicalService ExistingService2 { get; } = CreateSubstituteService(Guid.NewGuid(), "Troca de Pneu", 2, 150, 1);
        private static ServiceDto ExistingService2Dto { get; } = new(ExistingService2.Id, "Troca de Pneu", 2, 150, 1);
        private static IMaterial ExistingPart { get; } = CreateSubstitutePart(Guid.NewGuid(), "Pneu", "Michelin", 600, 10, 4);
        private static MaterialDto ExistingPartDto { get; } = new(ExistingPart.Id, ExistingPart.Name, ExistingPart.Brand, ExistingPart.Price, ExistingPart.Amount, ExistingPart.ReservedAmount);
        private static IMaterial ExistingPart2 { get; } = CreateSubstitutePart(Guid.NewGuid(), "Óleo de Motor", "Lubrax", 35, 20, 0);
        private static MaterialDto ExistingPart2Dto { get; } = new(ExistingPart2.Id, ExistingPart2.Name, ExistingPart2.Brand, ExistingPart2.Price, ExistingPart2.Amount, ExistingPart2.ReservedAmount);
        private static IOrder ExistingReceivedOrder { get; } = CreateSubstituteOrder(Guid.NewGuid(), [], [], 0.0, WorkOrderStatus.Received);
        private static IOrder ExistingTestOrder { get; set; } = CreateSubstituteOrder(Guid.NewGuid(), [], [], 0.0, WorkOrderStatus.Received);
        private static readonly Guid ExistingOrderInDiagnosisId = Guid.NewGuid();
        private static IOrder ExistingOrderInDiagnosis { get; set; }

        [SetUp]
        public async Task SetUp()
        {
            ExistingOrderInDiagnosis = CreateSubstituteOrder(ExistingOrderInDiagnosisId, [CreateSubstituteService(ExistingService.Id, ExistingService.Description, ExistingService.Hours, ExistingService.PricePerHour, 2), CreateSubstituteService(ExistingService2.Id, ExistingService2.Description, ExistingService2.Hours, ExistingService2.PricePerHour, 4)], [CreateSubstitutePart(ExistingPart.Id, ExistingPart.Name, ExistingPart.Brand, ExistingPart.Price, 4, 0), CreateSubstitutePart(ExistingPart2.Id, ExistingPart2.Name, ExistingPart2.Brand, ExistingPart2.Price, 1, 0)], 0.0, WorkOrderStatus.InDiagnosis);

            Repository = Substitute.For<IOrdersRepository>();

            Repository.CreateOrder(Arg.Any<IOrder>()).Returns(callInfo =>
            {
                var order = callInfo.ArgAt<IOrder>(0);

                if (order.CustomerDocument.Id == ExistingCustomer.Document)
                    return 1;

                return 0;
            });

            List<IOrder> orders = new List<IOrder>() { ExistingReceivedOrder, ExistingOrderInDiagnosis, ExistingTestOrder };

            Repository.GetOrders().Returns(orders);

            Repository.GetOrders(customer_document: Arg.Any<string>()).Returns(callInfo =>
            {
                var document = callInfo.ArgAt<string>(1);

                if (!string.IsNullOrEmpty(document))
                    return orders.Where(x => x.CustomerDocument.Id == document);

                return orders;
            });

            Repository.GetOrders(vehicle_license_plate: Arg.Any<string>()).Returns(callInfo =>
            {
                var license = callInfo.ArgAt<string>(2);

                if (!string.IsNullOrEmpty(license))
                    return orders.Where(x => x.VehicleLicensePlate.License == license);

                return orders;
            });

            Repository.GetOrder(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                return orders.FirstOrDefault(x => x.Id == id);
            });

            Repository.GetOrder(customer_document: Arg.Any<string>()).Returns(callInfo =>
            {
                var document = callInfo.ArgAt<string>(1);

                return orders.FirstOrDefault(x => x.CustomerDocument.Id == document);
            });

            Repository.GetOrder(customer_document: Arg.Any<string>(), vehicle_license_plate: Arg.Any<string>()).Returns(callInfo =>
            {
                var document = callInfo.ArgAt<string>(1);
                var license = callInfo.ArgAt<string>(2);

                return orders.FirstOrDefault(x => x.CustomerDocument.Id == document && x.VehicleLicensePlate.License == license);
            });

            Repository.GetOrders(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var document = callInfo.ArgAt<string>(0);

                return orders.Where(x => x.CustomerDocument.Id == document);
            });

            Repository.UpdateOrder(Arg.Any<IOrder>()).Returns(callInfo =>
            {
                var order = callInfo.ArgAt<IOrder>(0);

                if (order.Id == ExistingReceivedOrder.Id)
                {
                    if (order.Status == WorkOrderStatus.InDiagnosis || order.Status == WorkOrderStatus.WaitingForExecution)
                        return 1;

                    if (order.Status == WorkOrderStatus.Finished)
                        return 1;
                }

                if (order.Id == ExistingOrderInDiagnosisId)
                {
                    if (order.Status == WorkOrderStatus.WaitingForApproval && order.Budget != 0.0)
                        return 1;

                    if (order.Status == WorkOrderStatus.InExecution || order.Status == WorkOrderStatus.Delivered)
                        return 1;

                    if (order.Status == WorkOrderStatus.Finished)
                        return 1;
                }

                if (order.Id == ExistingTestOrder.Id && order.Status == WorkOrderStatus.WaitingForApproval)
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

            Repository.RemoveServiceFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var serviceId = callInfo.ArgAt<Guid>(1);

                if (id == ExistingOrderInDiagnosis.Id && serviceId == ExistingService.Id)
                    return 1;

                return 0;
            });

            Repository.AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var part = callInfo.ArgAt<IMaterial>(1);

                if (id == ExistingReceivedOrder.Id && part.Id == ExistingPart.Id)
                    return 1;

                return 0;
            });

            Repository.UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var part = callInfo.ArgAt<IMaterial>(1);

                if (id == ExistingOrderInDiagnosis.Id && part.Id == ExistingPart.Id)
                    return 1;

                return 0;
            });

            Repository.RemoveMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var partId = callInfo.ArgAt<Guid>(1);

                if (id == ExistingOrderInDiagnosisId && partId == ExistingPart2.Id)
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

            List<CustomerResult> customers = [ExistingCustomer, ExistingFailCustomer];

            CustomerService.GetCustomer(document: Arg.Any<string>()).Returns(callInfo =>
            {
                var document = callInfo.ArgAt<string>(2);

                return customers.FirstOrDefault(x => x.Document == document);
            });

            VehicleService = Substitute.For<IVehicleService>();

            VehicleService.GetVehicle(licensePlate: Arg.Any<string>()).Returns(callInfo =>
            {
                var license = callInfo.ArgAt<string>(1);

                if (license == ExistingVehicle.LicensePlate)
                    return ExistingVehicle;

                return null;
            });

            StockService = Substitute.For<IStockService>();

            StockService.GetMaterial(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingPart.Id)
                    return ExistingPartDto;

                if (id == ExistingPart2.Id)
                    return ExistingPart2Dto;

                return null;
            });

            StockService.ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(Task.CompletedTask);
            StockService.RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(Task.CompletedTask);

            MechanicalService = Substitute.For<ICatalogService>();

            MechanicalService.GetService(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingService.Id)
                    return ExistingServiceDto;

                if (id == ExistingService2.Id)
                    return ExistingService2Dto;

                return null;
            });

            EmailService = Substitute.For<IEmailService>();

            EmailService.NotifyBudget(Arg.Any<ICustomer>(), Arg.Any<IVehicle>(), Arg.Any<IOrder>()).Returns(Task.CompletedTask);

            Service = new OrdersService(Repository, CustomerService, VehicleService, StockService, MechanicalService, EmailService);
        }

        [Test]
        public async Task MustCreateOrder()
        {
            await Service.CreateServiceOrder(OrderToCreate);

            await CustomerService.Received(1).GetCustomer(document: ExistingCustomer.Document);
            await VehicleService.Received(1).GetVehicle(licensePlate: ExistingVehicle.LicensePlate);
            await Repository.ReceivedWithAnyArgs(1).CreateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustNotCreateOrderIfCustomerNotExists()
        {
            var order = new CreateOrderDto("000.000.000-00", ExistingVehicle.LicensePlate);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CreateServiceOrder(order));

            await CustomerService.Received(1).GetCustomer(document: order.CustomerDocument);
            await VehicleService.ReceivedWithAnyArgs(0).GetVehicle();
            await Repository.ReceivedWithAnyArgs(0).CreateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustNotCreateOrderIfVehicleNotExists()
        {
            var order = new CreateOrderDto(ExistingCustomer.Document, "AAA0000");

            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CreateServiceOrder(order));

            await CustomerService.Received(1).GetCustomer(document: ExistingCustomer.Document);
            await VehicleService.Received(1).GetVehicle(licensePlate: order.VehicleLicensePlate);
            await Repository.ReceivedWithAnyArgs(0).CreateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustFailToCreateOrder()
        {
            var order = new CreateOrderDto(ExistingFailCustomer.Document, ExistingVehicle.LicensePlate);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CreateServiceOrder(order));

            await CustomerService.Received(1).GetCustomer(document: ExistingFailCustomer.Document);
            await VehicleService.Received(1).GetVehicle(licensePlate: ExistingVehicle.LicensePlate);
            await Repository.ReceivedWithAnyArgs(1).CreateOrder(Arg.Any<IOrder>());
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
                Assert.That(order.Materials, Is.Empty);
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
        public async Task MustGetOrderByDocumentAndLicensePlate()
        {
            var order = await Service.GetOrder(customerDocument: ExistingReceivedOrder.CustomerDocument.Id, vehicleLicensePlate: ExistingReceivedOrder.VehicleLicensePlate.License);

            await Repository.Received(1).GetOrder(customer_document: ExistingReceivedOrder.CustomerDocument.Id, vehicle_license_plate: ExistingReceivedOrder.VehicleLicensePlate.License);

            Assert.That(order, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(order.Id, Is.EqualTo(ExistingReceivedOrder.Id));
                Assert.That(order.CustomerDocument, Is.EqualTo(ExistingReceivedOrder.CustomerDocument.Id));
                Assert.That(order.VehicleLicensePlate, Is.EqualTo(ExistingReceivedOrder.VehicleLicensePlate.License));
                Assert.That(order.Services, Is.Empty);
                Assert.That(order.Materials, Is.Empty);
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
        public async Task MustNotGetOrderIfNoParameterWasGiven()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.GetOrder());
        }

        [Test]
        public async Task MustGetCustomerOrders()
        {
            var orders = await Service.GetOrders(customerDocument: ExistingCustomer.Document);
            var ordersList = orders.ToList();

            await Repository.Received(1).GetOrders(customer_document: ExistingCustomer.Document);

            Assert.That(ordersList, Has.Count.EqualTo(3));
            Assert.That(ordersList[0], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Id, Is.EqualTo(ExistingReceivedOrder.Id));
                Assert.That(ordersList[0].CustomerDocument, Is.EqualTo(ExistingReceivedOrder.CustomerDocument.Id));
                Assert.That(ordersList[0].VehicleLicensePlate, Is.EqualTo(ExistingReceivedOrder.VehicleLicensePlate.License));
                Assert.That(ordersList[0].Services, Is.Empty);
                Assert.That(ordersList[0].Materials, Is.Empty);
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
            var orders = await Service.GetOrders(customerDocument: "000.000.000-00");

            await Repository.Received(1).GetOrders(customer_document: "000.000.000-00");
            Assert.That(orders.ToList(), Has.Count.EqualTo(0));
        }

        [Test]
        public async Task MustGetVehicleOrders()
        {
            var orders = await Service.GetOrders(vehicleLicensePlate: ExistingVehicle.LicensePlate);
            var ordersList = orders.ToList();

            await Repository.Received(1).GetOrders(vehicle_license_plate: ExistingVehicle.LicensePlate);

            Assert.That(ordersList, Has.Count.EqualTo(3));
            Assert.That(ordersList[0], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Id, Is.EqualTo(ExistingReceivedOrder.Id));
                Assert.That(ordersList[0].CustomerDocument, Is.EqualTo(ExistingReceivedOrder.CustomerDocument.Id));
                Assert.That(ordersList[0].VehicleLicensePlate, Is.EqualTo(ExistingReceivedOrder.VehicleLicensePlate.License));
                Assert.That(ordersList[0].Services, Is.Empty);
                Assert.That(ordersList[0].Materials, Is.Empty);
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
        public async Task MustNotGetVehicleOrdersIfVehicleNotExists()
        {
            var orders = await Service.GetOrders(vehicleLicensePlate: "TTT0000");

            await Repository.Received(1).GetOrders(vehicle_license_plate: "TTT0000");
            Assert.That(orders.ToList(), Has.Count.EqualTo(0));
        }

        [Test]
        public async Task MustStartDiagnosis()
        {
            await Service.StartDiagnosis(ExistingReceivedOrder.Id);

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustNotStartDiagnosisIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.StartDiagnosis(Guid.NewGuid()));

            await Repository.Received(1).GetOrder(Arg.Any<Guid>());
            await Repository.Received(0).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustFailToStartDiagnosis()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.StartDiagnosis(ExistingOrderInDiagnosis.Id));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustAddNewServiceToOrder()
        {
            await Service.AddServiceToOrder(ExistingReceivedOrder.Id, new(ExistingService.Id, 1));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await MechanicalService.Received(1).GetService(ExistingService.Id);
            await Repository.Received(1).AddServiceToOrder(ExistingReceivedOrder.Id, Arg.Any<IMechanicalService>());
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustAddServiceAmountToOrder()
        {
            await Service.AddServiceToOrder(ExistingOrderInDiagnosis.Id, new(ExistingService.Id, 1));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await MechanicalService.ReceivedWithAnyArgs(0).GetService(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
            await Repository.Received(1).UpdateServiceOfOrder(ExistingOrderInDiagnosis.Id, Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustNotAddServiceIfOrderNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddServiceToOrder(Guid.NewGuid(), new UpdateItemDto<int>(ExistingService.Id, 1)));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await MechanicalService.ReceivedWithAnyArgs(0).GetService(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustNotAddServiceIfServiceNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddServiceToOrder(ExistingReceivedOrder.Id, new UpdateItemDto<int>(Guid.NewGuid(), 1)));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await MechanicalService.ReceivedWithAnyArgs(1).GetService(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustFailToAddServiceToOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddServiceToOrder(ExistingReceivedOrder.Id, new UpdateItemDto<int>(ExistingService2.Id, 1)));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await MechanicalService.Received(1).GetService(ExistingService2.Id);
            await Repository.ReceivedWithAnyArgs(1).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustFailToAddServiceAmountToOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddServiceToOrder(ExistingOrderInDiagnosis.Id, new UpdateItemDto<int>(ExistingService2.Id, 1)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await MechanicalService.ReceivedWithAnyArgs(0).GetService(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
            await Repository.ReceivedWithAnyArgs(1).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustRemoveServiceOfOrder()
        {
            await Service.RemoveServiceOfOrder(ExistingOrderInDiagnosis.Id, new(ExistingService.Id, 2));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await Repository.Received(1).RemoveServiceFromOrder(ExistingOrderInDiagnosis.Id, ExistingService.Id);
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustRemoveServiceAmountOfOrder()
        {
            await Service.RemoveServiceOfOrder(ExistingOrderInDiagnosis.Id, new(ExistingService.Id, 1));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await Repository.ReceivedWithAnyArgs(0).RemoveServiceFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.Received(1).UpdateServiceOfOrder(ExistingOrderInDiagnosis.Id, Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustNotRemoveServiceIfOrderNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemoveServiceOfOrder(Guid.NewGuid(), new UpdateItemDto<int>(ExistingService.Id, 1)));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).RemoveServiceFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustFailToRemoveServiceOfOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemoveServiceOfOrder(ExistingOrderInDiagnosis.Id, new UpdateItemDto<int>(ExistingService2.Id, 4)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await Repository.Received(1).RemoveServiceFromOrder(ExistingOrderInDiagnosis.Id, ExistingService2.Id);
            await Repository.ReceivedWithAnyArgs(0).UpdateServiceOfOrder(Arg.Any<Guid>(), Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustFailToRemoveServiceAmountToOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemoveServiceOfOrder(ExistingOrderInDiagnosis.Id, new UpdateItemDto<int>(ExistingService2.Id, 2)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await Repository.ReceivedWithAnyArgs(0).RemoveServiceFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.Received(1).UpdateServiceOfOrder(ExistingOrderInDiagnosis.Id, Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustAddPartToOrder()
        {
            await Service.AddMaterialToOrder(ExistingReceivedOrder.Id, new(ExistingPart.Id, 1));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await StockService.ReceivedWithAnyArgs(1).ReserveMaterialAmount(ExistingPart.Id, 1);
            await Repository.ReceivedWithAnyArgs(1).AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await Repository.ReceivedWithAnyArgs(0).UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await StockService.ReceivedWithAnyArgs(0).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustAddPartAmountToOrder()
        {
            await Service.AddMaterialToOrder(ExistingOrderInDiagnosis.Id, new(ExistingPart.Id, 1));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await StockService.ReceivedWithAnyArgs(1).ReserveMaterialAmount(ExistingPart.Id, 1);
            await Repository.ReceivedWithAnyArgs(0).AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await Repository.ReceivedWithAnyArgs(1).UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await StockService.ReceivedWithAnyArgs(0).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustNotAddPartToOrderIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddMaterialToOrder(Guid.NewGuid(), new(ExistingPart.Id, 1)));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await StockService.ReceivedWithAnyArgs(0).ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.ReceivedWithAnyArgs(0).AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await Repository.ReceivedWithAnyArgs(0).UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await StockService.ReceivedWithAnyArgs(0).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustNotAddPartToOrderIfPartNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddMaterialToOrder(ExistingReceivedOrder.Id, new(Guid.NewGuid(), 1)));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await StockService.ReceivedWithAnyArgs(1).ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.ReceivedWithAnyArgs(0).AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await Repository.ReceivedWithAnyArgs(0).UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await StockService.ReceivedWithAnyArgs(1).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustFailToAddPartToOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddMaterialToOrder(ExistingReceivedOrder.Id, new(ExistingPart2.Id, 1)));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await StockService.ReceivedWithAnyArgs(1).ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.ReceivedWithAnyArgs(1).AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await Repository.ReceivedWithAnyArgs(0).UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await StockService.ReceivedWithAnyArgs(1).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustFailToAddPartAmountToOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddMaterialToOrder(ExistingOrderInDiagnosis.Id, new(ExistingPart2.Id, 1)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await StockService.ReceivedWithAnyArgs(1).ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.ReceivedWithAnyArgs(0).AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await Repository.ReceivedWithAnyArgs(1).UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await StockService.ReceivedWithAnyArgs(1).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustRemovePartFromOrder()
        {
            await Service.RemoveMaterialFromOrder(ExistingOrderInDiagnosisId, new(ExistingPart2.Id, 1));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await StockService.ReceivedWithAnyArgs(1).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.Received(1).RemoveMaterialFromOrder(ExistingOrderInDiagnosisId, ExistingPart2.Id);
            await Repository.ReceivedWithAnyArgs(0).UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await StockService.ReceivedWithAnyArgs(0).ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustRemovePartAmountFromOrder()
        {
            await Service.RemoveMaterialFromOrder(ExistingOrderInDiagnosisId, new(ExistingPart.Id, 2));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await StockService.ReceivedWithAnyArgs(1).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.ReceivedWithAnyArgs(0).RemoveMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(1).UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await StockService.ReceivedWithAnyArgs(0).ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustNotRemovePartFromOrderIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemoveMaterialFromOrder(Guid.NewGuid(), new(ExistingPart.Id, 1)));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await StockService.ReceivedWithAnyArgs(0).ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.ReceivedWithAnyArgs(0).RemoveMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await StockService.ReceivedWithAnyArgs(0).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustNotRemovePartFromOrderIfPartNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemoveMaterialFromOrder(ExistingReceivedOrder.Id, new(Guid.NewGuid(), 1)));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await StockService.ReceivedWithAnyArgs(1).ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.ReceivedWithAnyArgs(0).RemoveMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await StockService.ReceivedWithAnyArgs(1).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustFailToRemovePartFromOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemoveMaterialFromOrder(ExistingOrderInDiagnosisId, new(ExistingPart.Id, 4)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await StockService.ReceivedWithAnyArgs(1).ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.ReceivedWithAnyArgs(1).RemoveMaterialFromOrder(ExistingOrderInDiagnosisId, ExistingPart.Id);
            await Repository.ReceivedWithAnyArgs(0).UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await StockService.ReceivedWithAnyArgs(1).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustFailToRemovePartAmountFromOrder()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddMaterialToOrder(ExistingOrderInDiagnosis.Id, new(ExistingPart2.Id, 0)));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosis.Id);
            await StockService.ReceivedWithAnyArgs(1).ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.ReceivedWithAnyArgs(0).RemoveMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(1).UpdateMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<IMaterial>());
            await StockService.ReceivedWithAnyArgs(1).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustCompleteDiagnosis()
        {
            await Service.CompleteDiagnosis(ExistingOrderInDiagnosisId);

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
            await EmailService.ReceivedWithAnyArgs(1).NotifyBudget(Arg.Any<ICustomer>(), Arg.Any<IVehicle>(), Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustNotCompleteDiagnosisIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CompleteDiagnosis(Guid.NewGuid()));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await Repository.Received(0).UpdateOrder(Arg.Any<IOrder>());
            await EmailService.ReceivedWithAnyArgs(0).NotifyBudget(Arg.Any<ICustomer>(), Arg.Any<IVehicle>(), Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustFailToCompleteDiagnosis()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CompleteDiagnosis(ExistingReceivedOrder.Id));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
            await EmailService.ReceivedWithAnyArgs(0).NotifyBudget(Arg.Any<ICustomer>(), Arg.Any<IVehicle>(), Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustApproveBudget()
        {
            await Service.ApproveBudget(ExistingReceivedOrder.Id, new(ExistingReceivedOrder.CustomerDocument.Id, true));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await StockService.ReceivedWithAnyArgs(0).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustRefuseBudget()
        {
            await Service.ApproveBudget(ExistingReceivedOrder.Id, new(ExistingReceivedOrder.CustomerDocument.Id, false));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await StockService.ReceivedWithAnyArgs(0).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustRefuseBudgetWithPartsToRestore()
        {
            await Service.ApproveBudget(ExistingOrderInDiagnosisId, new(ExistingOrderInDiagnosis.CustomerDocument.Id, false));

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await StockService.ReceivedWithAnyArgs(2).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustNotApproveOrRefuseBudgetIfOrderNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.ApproveBudget(Guid.NewGuid(), new("681.487.685-11", false)));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await StockService.ReceivedWithAnyArgs(0).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.ReceivedWithAnyArgs(0).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustNotApproveOrRefuseBudgetWithWrongDocument()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.ApproveBudget(ExistingOrderInDiagnosisId, new("000.000.000-00", false)));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await StockService.ReceivedWithAnyArgs(0).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.ReceivedWithAnyArgs(0).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustFailToApproveOrRefuseBudget()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.ApproveBudget(ExistingTestOrder.Id, new(ExistingTestOrder.CustomerDocument.Id, false)));

            await Repository.Received(1).GetOrder(ExistingTestOrder.Id);
            await StockService.ReceivedWithAnyArgs(0).RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
            await Repository.ReceivedWithAnyArgs(1).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustStartExecution()
        {
            await Service.StartExecution(ExistingOrderInDiagnosisId);

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustNotStartExecutionIfOrderNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.StartExecution(Guid.NewGuid()));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustFailToStartExecution()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.StartExecution(ExistingReceivedOrder.Id));

            await Repository.Received(1).GetOrder(ExistingReceivedOrder.Id);
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustCompleteExecution()
        {
            await Service.CompleteExecution(ExistingOrderInDiagnosisId);

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustNotCompleteExecutionIfOrderNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CompleteExecution(Guid.NewGuid()));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustFailToCompleteExecution()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.CompleteExecution(ExistingTestOrder.Id));

            await Repository.Received(1).GetOrder(ExistingTestOrder.Id);
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustDeliverVehicle()
        {
            await Service.DeliverVehicle(ExistingOrderInDiagnosisId);

            await Repository.Received(1).GetOrder(ExistingOrderInDiagnosisId);
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustNotDeliverVehicleIfOrderNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeliverVehicle(Guid.NewGuid()));

            await Repository.ReceivedWithAnyArgs(1).GetOrder(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateOrder(Arg.Any<IOrder>());
        }

        [Test]
        public async Task MustFailToDeliverVehicle()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeliverVehicle(ExistingTestOrder.Id));

            await Repository.Received(1).GetOrder(ExistingTestOrder.Id);
            await Repository.Received(1).UpdateOrder(Arg.Any<IOrder>());
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

        private static IMaterial CreateSubstitutePart(Guid id, string name, string brand, double price, int amount, int reservedAmount)
        {
            var part = Substitute.For<IMaterial>();
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

        private static IOrder CreateSubstituteOrder(Guid id, List<IMechanicalService> services, List<IMaterial> parts, double budget, WorkOrderStatus status)
        {
            var order = Substitute.For<IOrder>();
            order.Id.Returns(id);
            order.CustomerDocument.Id.Returns(ExistingCustomer.Document);
            order.VehicleLicensePlate.License.Returns(ExistingVehicle.LicensePlate);
            order.Services.Returns(services);
            order.Materials.Returns(parts);
            order.Budget.Returns(budget);
            order.Status.Returns(status);
            order.DateCreated.Returns(DateTime.Now);
            order.DateFinished.Returns(DateTime.MinValue);

            order.AddMaterial(Arg.Any<IMaterial>()).Returns(callInfo =>
            {
                return callInfo.ArgAt<IMaterial>(0);
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

            order.When(x => x.StartDiagnosis()).Do(_ => order.Status.Returns(WorkOrderStatus.InDiagnosis));

            order.When(x => x.StartService()).Do(_ => order.Status.Returns(WorkOrderStatus.InExecution));

            order.When(x => x.CompleteService()).Do(_ =>
            {
                order.Status.Returns(WorkOrderStatus.Finished);
                order.Duration.Returns(TimeSpan.FromHours(6));
            });

            order.When(x => x.DeliverVehicle()).Do(_ => order.Status.Returns(WorkOrderStatus.Delivered));

            return order;
        }
    }
}
