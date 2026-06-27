using Dapper;
using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using NSubstitute;
using Repository;
using Repository.Interface;

namespace RepositoryTests
{
    public class OrdersRepositoryTests : BaseRepositoryTests
    {
        private IOrdersRepository Repository { get; set; }

        private static readonly Guid ServiceId = Guid.NewGuid();
        private static IMechanicalService Service
        {
            get
            {
                var service = Substitute.For<IMechanicalService>();
                service.Id.Returns(ServiceId);
                service.Description.Returns("Troca de Pneu");
                service.Hours.Returns(2);
                service.PricePerHour.Returns(150);
                service.Amount.Returns(1);
                service.Price.Returns(300);
                return service;
            }
        }

        private static readonly Guid PartId = Guid.NewGuid();
        private static IMaterial Part
        {
            get
            {
                var part = Substitute.For<IMaterial>();
                part.Id.Returns(PartId);
                part.Name.Returns("Óleo de Motor");
                part.Brand.Returns("Lubrax");
                part.Price.Returns(25);
                part.Amount.Returns(19);
                part.ReservedAmount.Returns(1);
                return part;
            }
        }

        private static IOrder OrderToCreate
        {
            get
            {
                var order = Substitute.For<IOrder>();
                order.CustomerDocument.Id.Returns("417.384.220-11");
                order.VehicleLicensePlate.License.Returns("CVC2026");
                order.Services.Returns([]);
                order.Materials.Returns([]);
                order.Budget.Returns(0);
                order.Status.Returns(WorkOrderStatus.Received);
                order.DateCreated.Returns(DateTime.Now);
                order.DateFinished.Returns(DateTime.MinValue);
                return order;
            }
        }

        private static readonly Guid ExistingOrderId = Guid.NewGuid();
        private static readonly DateTime ExistingOrderDateCreated = DateTime.Now.AddMinutes(-5);
        private static IOrder ExistingOrder
        {
            get
            {
                var order = Substitute.For<IOrder>();
                order.Id.Returns(ExistingOrderId);
                order.CustomerDocument.Id.Returns("417.384.220-11");
                order.VehicleLicensePlate.License.Returns("CVC2026");
                order.Services.Returns([]);
                order.Materials.Returns([]);
                order.Budget.Returns(100);
                order.Status.Returns(WorkOrderStatus.WaitingForApproval);
                order.DateCreated.Returns(ExistingOrderDateCreated);
                order.DateFinished.Returns(DateTime.MinValue);
                return order;
            }
        }

        protected override async Task InternalSetup()
        {
            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS customers (
                id UUID PRIMARY KEY NOT NULL,
                name TEXT NOT NULL,
                document TEXT NOT NULL UNIQUE,
                phone TEXT NOT NULL,
                email TEXT NOT NULL);
                """);

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS vehicles (
                id UUID PRIMARY KEY,
                customer_document VARCHAR(100) NOT NULL REFERENCES customers(document),
                brand VARCHAR(100) NOT NULL,
                model VARCHAR(100) NOT NULL,
                year INT NOT NULL,
                license_plate VARCHAR(7) NOT NULL UNIQUE);
                """);

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS orders (
                id UUID PRIMARY KEY,
                customer_document VARCHAR(100) NOT NULL REFERENCES customers(document),
                vehicle_license_plate VARCHAR(100) NOT NULL REFERENCES vehicles(license_plate),
                budget DOUBLE PRECISION NOT NULL,
                status VARCHAR(50) NOT NULL,
                date_created TIMESTAMP NOT NULL DEFAULT NOW(),
                date_finished TIMESTAMP NOT NULL,
                duration INTERVAL NOT NULL);
                """);

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS stock (
                id UUID PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                brand VARCHAR(255) NOT NULL,
                price DOUBLE PRECISION NOT NULL,
                amount INT NOT NULL,
                reserved_amount INT NOT NULL DEFAULT 0);
                """);

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS services (
                id UUID PRIMARY KEY,
                description VARCHAR(255) NOT NULL,
                hours FLOAT NOT NULL,
                price_per_hour DOUBLE PRECISION NOT NULL);
                """);

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS order_materials (
                id UUID NOT NULL REFERENCES stock(id),
                order_id UUID NOT NULL REFERENCES orders(id),
                name VARCHAR(255) NOT NULL,
                brand VARCHAR(255) NOT NULL,
                price DOUBLE PRECISION NOT NULL,
                amount INT NOT NULL);
                """);

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS order_services (
                id UUID NOT NULL REFERENCES services(id),
                order_id UUID NOT NULL REFERENCES orders(id),
                description VARCHAR(255) NOT NULL,
                hours FLOAT NOT NULL,
                price_per_hour DOUBLE PRECISION NOT NULL,
                amount INT NOT NULL);
                """);

            await Connection.ExecuteAsync($"""
                INSERT INTO customers(id, name, document, phone, email)
                VALUES ('{Guid.NewGuid()}', 'Teste', '417.384.220-11', '(11) 91234-5678', 'teste@gmail.com');
                """);

            await Connection.ExecuteAsync($"""
                INSERT INTO vehicles(id, customer_document, brand, model, year, license_plate)
                VALUES ('{Guid.NewGuid()}', '417.384.220-11', 'Honda', 'Civic', 2026, 'CVC2026');
                """);

            await Connection.ExecuteAsync($"""
                INSERT INTO stock(id, name, brand, price, amount, reserved_amount)
                VALUES ('{Part.Id}', '{Part.Name}', '{Part.Brand}', '{Part.Price}', '{Part.Amount}', '{Part.ReservedAmount}');
                """);

            await Connection.ExecuteAsync($"""
                INSERT INTO services(id, description, hours, price_per_hour)
                VALUES ('{Service.Id}', '{Service.Description}', {Service.Hours}, {Service.PricePerHour});
                """);

            Repository = new OrdersRepository(Connection);

            await Repository.CreateOrder(ExistingOrder);
            await Repository.AddServiceToOrder(ExistingOrderId, Service);

            var part = Part;
            part.Amount.Returns(1);
            part.ReservedAmount.Returns(0);
            await Repository.AddMaterialToOrder(ExistingOrderId, part);
        }

        [Test]
        public async Task MustCreateOrder()
        {
            var registry = await Repository.CreateOrder(OrderToCreate);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustGetOrders()
        {
            var orders = await Repository.GetOrders();
            var ordersList = orders.ToList();

            Assert.That(ordersList, Has.Count.EqualTo(1));
            Assert.That(ordersList[0], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Id, Is.EqualTo(ExistingOrder.Id));
                Assert.That(ordersList[0].CustomerDocument.Id, Is.EqualTo(ExistingOrder.CustomerDocument.Id));
                Assert.That(ordersList[0].VehicleLicensePlate.License, Is.EqualTo(ExistingOrder.VehicleLicensePlate.License));
                Assert.That(ordersList[0].Status, Is.EqualTo(ExistingOrder.Status));
                Assert.That(ordersList[0].DateCreated.Date, Is.EqualTo(ExistingOrder.DateCreated.Date));
                Assert.That(ordersList[0].DateCreated.Hour, Is.EqualTo(ExistingOrder.DateCreated.Hour));
                Assert.That(ordersList[0].DateCreated.Minute, Is.EqualTo(ExistingOrder.DateCreated.Minute));
                Assert.That(ordersList[0].DateCreated.Second, Is.EqualTo(ExistingOrder.DateCreated.Second));
                Assert.That(ordersList[0].DateFinished.Date, Is.EqualTo(ExistingOrder.DateFinished.Date));
                Assert.That(ordersList[0].DateFinished.Hour, Is.EqualTo(ExistingOrder.DateFinished.Hour));
                Assert.That(ordersList[0].DateFinished.Minute, Is.EqualTo(ExistingOrder.DateFinished.Minute));
                Assert.That(ordersList[0].DateFinished.Second, Is.EqualTo(ExistingOrder.DateFinished.Second));
            });
        }

        [Test]
        public async Task MustGetDetailedOrderById()
        {
            var order = await Repository.GetOrder(id: ExistingOrderId);

            Assert.That(order, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(order.Id, Is.EqualTo(ExistingOrder.Id));
                Assert.That(order.CustomerDocument.Id, Is.EqualTo(ExistingOrder.CustomerDocument.Id));
                Assert.That(order.VehicleLicensePlate.License, Is.EqualTo(ExistingOrder.VehicleLicensePlate.License));
                Assert.That(order.Status, Is.EqualTo(ExistingOrder.Status));
                Assert.That(order.DateCreated.Date, Is.EqualTo(ExistingOrder.DateCreated.Date));
                Assert.That(order.DateCreated.Hour, Is.EqualTo(ExistingOrder.DateCreated.Hour));
                Assert.That(order.DateCreated.Minute, Is.EqualTo(ExistingOrder.DateCreated.Minute));
                Assert.That(order.DateCreated.Second, Is.EqualTo(ExistingOrder.DateCreated.Second));
                Assert.That(order.DateFinished.Date, Is.EqualTo(ExistingOrder.DateFinished.Date));
                Assert.That(order.DateFinished.Hour, Is.EqualTo(ExistingOrder.DateFinished.Hour));
                Assert.That(order.DateFinished.Minute, Is.EqualTo(ExistingOrder.DateFinished.Minute));
                Assert.That(order.DateFinished.Second, Is.EqualTo(ExistingOrder.DateFinished.Second));
            });

            Assert.Multiple(() =>
            {
                Assert.That(order.Services, Has.Count.EqualTo(1));
                Assert.That(order.Services[0].Id, Is.EqualTo(Service.Id));
                Assert.That(order.Services[0].Description, Is.EqualTo(Service.Description));
                Assert.That(order.Services[0].Hours, Is.EqualTo(Service.Hours));
                Assert.That(order.Services[0].PricePerHour, Is.EqualTo(Service.PricePerHour));
                Assert.That(order.Services[0].Price, Is.EqualTo(Service.Price));
                Assert.That(order.Services[0].Amount, Is.EqualTo(Service.Amount));
            });

            Assert.Multiple(() =>
            {
                Assert.That(order.Materials, Has.Count.EqualTo(1));
                Assert.That(order.Materials[0].Id, Is.EqualTo(Part.Id));
                Assert.That(order.Materials[0].Name, Is.EqualTo(Part.Name));
                Assert.That(order.Materials[0].Brand, Is.EqualTo(Part.Brand));
                Assert.That(order.Materials[0].Price, Is.EqualTo(Part.Price));
                Assert.That(order.Materials[0].Amount, Is.EqualTo(1));
                Assert.That(order.Materials[0].ReservedAmount, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task MustNotGetOrderByIdIfNotExists()
        {
            var order = await Repository.GetOrder(Guid.NewGuid());

            Assert.That(order, Is.Null);
        }

        [Test]
        public async Task MustGetDetailedCustomerOrders()
        {
            var orders = await Repository.GetOrders(customer_document: ExistingOrder.CustomerDocument.Id);
            var ordersList = orders.ToList();

            Assert.That(ordersList, Has.Count.EqualTo(1));
            Assert.That(ordersList[0], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Id, Is.EqualTo(ExistingOrder.Id));
                Assert.That(ordersList[0].CustomerDocument.Id, Is.EqualTo(ExistingOrder.CustomerDocument.Id));
                Assert.That(ordersList[0].VehicleLicensePlate.License, Is.EqualTo(ExistingOrder.VehicleLicensePlate.License));
                Assert.That(ordersList[0].Status, Is.EqualTo(ExistingOrder.Status));
                Assert.That(ordersList[0].DateCreated.Date, Is.EqualTo(ExistingOrder.DateCreated.Date));
                Assert.That(ordersList[0].DateCreated.Hour, Is.EqualTo(ExistingOrder.DateCreated.Hour));
                Assert.That(ordersList[0].DateCreated.Minute, Is.EqualTo(ExistingOrder.DateCreated.Minute));
                Assert.That(ordersList[0].DateCreated.Second, Is.EqualTo(ExistingOrder.DateCreated.Second));
                Assert.That(ordersList[0].DateFinished.Date, Is.EqualTo(ExistingOrder.DateFinished.Date));
                Assert.That(ordersList[0].DateFinished.Hour, Is.EqualTo(ExistingOrder.DateFinished.Hour));
                Assert.That(ordersList[0].DateFinished.Minute, Is.EqualTo(ExistingOrder.DateFinished.Minute));
                Assert.That(ordersList[0].DateFinished.Second, Is.EqualTo(ExistingOrder.DateFinished.Second));
            });

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Services, Has.Count.EqualTo(1));
                Assert.That(ordersList[0].Services[0].Id, Is.EqualTo(Service.Id));
                Assert.That(ordersList[0].Services[0].Description, Is.EqualTo(Service.Description));
                Assert.That(ordersList[0].Services[0].Hours, Is.EqualTo(Service.Hours));
                Assert.That(ordersList[0].Services[0].PricePerHour, Is.EqualTo(Service.PricePerHour));
                Assert.That(ordersList[0].Services[0].Price, Is.EqualTo(Service.Price));
                Assert.That(ordersList[0].Services[0].Amount, Is.EqualTo(Service.Amount));
            });

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Materials, Has.Count.EqualTo(1));
                Assert.That(ordersList[0].Materials[0].Id, Is.EqualTo(Part.Id));
                Assert.That(ordersList[0].Materials[0].Name, Is.EqualTo(Part.Name));
                Assert.That(ordersList[0].Materials[0].Brand, Is.EqualTo(Part.Brand));
                Assert.That(ordersList[0].Materials[0].Price, Is.EqualTo(Part.Price));
                Assert.That(ordersList[0].Materials[0].Amount, Is.EqualTo(1));
                Assert.That(ordersList[0].Materials[0].ReservedAmount, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task MustNotGetDetailedCustomerOrdersIfNotExists()
        {
            var orders = await Repository.GetOrders(customer_document: "000.000.000-00");
            var ordersList = orders.ToList();

            Assert.That(ordersList, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task MustGetDetailedVehicleOrders()
        {
            var orders = await Repository.GetOrders(vehicle_license_plate: ExistingOrder.VehicleLicensePlate.License);
            var ordersList = orders.ToList();

            Assert.That(ordersList, Has.Count.EqualTo(1));
            Assert.That(ordersList[0], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Id, Is.EqualTo(ExistingOrder.Id));
                Assert.That(ordersList[0].CustomerDocument.Id, Is.EqualTo(ExistingOrder.CustomerDocument.Id));
                Assert.That(ordersList[0].VehicleLicensePlate.License, Is.EqualTo(ExistingOrder.VehicleLicensePlate.License));
                Assert.That(ordersList[0].Status, Is.EqualTo(ExistingOrder.Status));
                Assert.That(ordersList[0].DateCreated.Date, Is.EqualTo(ExistingOrder.DateCreated.Date));
                Assert.That(ordersList[0].DateCreated.Hour, Is.EqualTo(ExistingOrder.DateCreated.Hour));
                Assert.That(ordersList[0].DateCreated.Minute, Is.EqualTo(ExistingOrder.DateCreated.Minute));
                Assert.That(ordersList[0].DateCreated.Second, Is.EqualTo(ExistingOrder.DateCreated.Second));
                Assert.That(ordersList[0].DateFinished.Date, Is.EqualTo(ExistingOrder.DateFinished.Date));
                Assert.That(ordersList[0].DateFinished.Hour, Is.EqualTo(ExistingOrder.DateFinished.Hour));
                Assert.That(ordersList[0].DateFinished.Minute, Is.EqualTo(ExistingOrder.DateFinished.Minute));
                Assert.That(ordersList[0].DateFinished.Second, Is.EqualTo(ExistingOrder.DateFinished.Second));
            });

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Services, Has.Count.EqualTo(1));
                Assert.That(ordersList[0].Services[0].Id, Is.EqualTo(Service.Id));
                Assert.That(ordersList[0].Services[0].Description, Is.EqualTo(Service.Description));
                Assert.That(ordersList[0].Services[0].Hours, Is.EqualTo(Service.Hours));
                Assert.That(ordersList[0].Services[0].PricePerHour, Is.EqualTo(Service.PricePerHour));
                Assert.That(ordersList[0].Services[0].Price, Is.EqualTo(Service.Price));
                Assert.That(ordersList[0].Services[0].Amount, Is.EqualTo(Service.Amount));
            });

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Materials, Has.Count.EqualTo(1));
                Assert.That(ordersList[0].Materials[0].Id, Is.EqualTo(Part.Id));
                Assert.That(ordersList[0].Materials[0].Name, Is.EqualTo(Part.Name));
                Assert.That(ordersList[0].Materials[0].Brand, Is.EqualTo(Part.Brand));
                Assert.That(ordersList[0].Materials[0].Price, Is.EqualTo(Part.Price));
                Assert.That(ordersList[0].Materials[0].Amount, Is.EqualTo(1));
                Assert.That(ordersList[0].Materials[0].ReservedAmount, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task MustNotGetDetailedVehicleOrdersIfNotExists()
        {
            var orders = await Repository.GetOrders(vehicle_license_plate: "TTT0000");
            var ordersList = orders.ToList();

            Assert.That(ordersList, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task MustUpdateOrder()
        {
            var order = Substitute.For<IOrder>();
            order.Id.Returns(ExistingOrderId);
            order.CustomerDocument.Id.Returns("417.384.220-11");
            order.VehicleLicensePlate.License.Returns("CVC2026");
            order.Services.Returns([]);
            order.Materials.Returns([]);
            order.Budget.Returns(200);
            order.Status.Returns(WorkOrderStatus.Finished);
            order.DateCreated.Returns(ExistingOrderDateCreated);
            order.DateFinished.Returns(DateTime.Now);
            order.Duration.Returns(TimeSpan.FromHours(6));

            var registry = await Repository.UpdateOrder(order);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustAddServiceToOrder()
        {
            var registry = await Repository.AddServiceToOrder(ExistingOrderId, Service);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustUpdateServiceFromOrder()
        {
            var service = Service;
            service.Amount.Returns(2);

            var registry = await Repository.UpdateServiceOfOrder(ExistingOrderId, service);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustDeleteServiceFromOrder()
        {
            var registry = await Repository.RemoveServiceFromOrder(ExistingOrderId, Service.Id);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustAddPartToOrder()
        {
            var registry = await Repository.AddMaterialToOrder(ExistingOrderId, Part);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustRemovePartFromOrder()
        {
            var registry = await Repository.RemoveMaterialFromOrder(ExistingOrderId, Part.Id);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustUpdatePartFromOrder()
        {
            var part = Part;
            part.Amount.Returns(5);

            var registry = await Repository.UpdateMaterialFromOrder(ExistingOrderId, part);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustDeleteOrder()
        {
            var registry = await Repository.DeleteOrder(ExistingOrderId);

            Assert.That(registry, Is.Not.EqualTo(0));
        }
    }
}
