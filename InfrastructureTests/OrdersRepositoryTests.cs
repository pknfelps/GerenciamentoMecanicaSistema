using Dapper;
using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using NSubstitute;
using Infrastructure.Persistence.PostgreSql.Repositories;
using Service.Interface.Persistence;
using Infrastructure.Persistence.PostgreSql.Transactions;
using Npgsql;

namespace InfrastructureTests
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
                order.Id.Returns(Guid.NewGuid());
                order.DateCreated.Returns(DateTime.UtcNow);
                order.DateFinished.Returns((DateTime?)null);
                return order;
            }
        }

        private static readonly Guid ExistingOrderId = Guid.NewGuid();
        private static readonly DateTime ExistingOrderDateCreated = DateTime.UtcNow.AddMinutes(-5);
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
                order.DateFinished.Returns((DateTime?)null);
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
                email TEXT NOT NULL,
                status VARCHAR(8) NOT NULL DEFAULT 'Active');
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
                budget NUMERIC NOT NULL,
                status VARCHAR(50) NOT NULL);
                """);

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS order_status_history (
                id UUID PRIMARY KEY,
                order_id UUID NOT NULL,
                sequence INTEGER NOT NULL,
                previous_status VARCHAR(50),
                new_status VARCHAR(50) NOT NULL,
                occurred_at TIMESTAMPTZ NOT NULL,
                reason VARCHAR(30));
                """);

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS stock (
                id UUID PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                brand VARCHAR(255) NOT NULL,
                price NUMERIC NOT NULL,
                amount INT NOT NULL,
                reserved_amount INT NOT NULL DEFAULT 0);
                """);

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS services (
                id UUID PRIMARY KEY,
                description VARCHAR(255) NOT NULL,
                hours FLOAT NOT NULL,
                price_per_hour NUMERIC NOT NULL);
                """);

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS order_materials (
                id UUID NOT NULL REFERENCES stock(id),
                order_id UUID NOT NULL REFERENCES orders(id),
                name VARCHAR(255) NOT NULL,
                brand VARCHAR(255) NOT NULL,
                price NUMERIC NOT NULL,
                amount INT NOT NULL);
                """);

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS order_services (
                id UUID NOT NULL REFERENCES services(id),
                order_id UUID NOT NULL REFERENCES orders(id),
                description VARCHAR(255) NOT NULL,
                hours FLOAT NOT NULL,
                price_per_hour NUMERIC NOT NULL,
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

            var existingOrder = ExistingOrder;
            existingOrder.Status.Returns(WorkOrderStatus.Received);
            await Repository.CreateOrder(existingOrder);
            existingOrder.Status.Returns(WorkOrderStatus.InDiagnosis);
            await Repository.UpdateOrder(existingOrder);
            existingOrder.Status.Returns(WorkOrderStatus.WaitingForApproval);
            await Repository.UpdateOrder(existingOrder);
            await Repository.AddServiceToOrder(ExistingOrderId, Service);

            var part = Part;
            part.Amount.Returns(1);
            part.ReservedAmount.Returns(0);
            await Repository.AddMaterialToOrder(ExistingOrderId, part);
        }

        [Test]
        public void LockingRequiresAnAmbientTransaction()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() => Repository.LockOrder(ExistingOrderId));
        }

        [TestCase("0")]
        [TestCase("999")]
        [TestCase("received")]
        [TestCase("Unknown")]
        public async Task InvalidPersistedStatusIsNotSilentlyMapped(string status)
        {
            await Connection.ExecuteAsync("UPDATE orders SET status = @Status WHERE id = @Id", new { Status = status, Id = ExistingOrderId });
            Assert.ThrowsAsync<Domain.Interface.Exceptions.DomainValidationException>(() => Repository.GetOrder(ExistingOrderId));
        }

        [Test]
        public async Task ListsHaveAStableOpeningTimeTieBreak()
        {
            var first = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var second = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var opening = DateTime.UtcNow.AddDays(-1);
            foreach (var id in new[] { second, first })
            {
                var order = OrderToCreate;
                order.Id.Returns(id);
                order.DateCreated.Returns(opening);
                await Repository.CreateOrder(order);
            }
            var detailed = (await Repository.GetOrders()).Select(order => order.Id).ToArray();
            var operational = (await Repository.GetOperationalOrders()).Select(order => order.Id).ToArray();
            Assert.That(detailed, Is.EqualTo(new[] { first, second, ExistingOrderId }));
            Assert.That(operational, Is.EqualTo(new[] { ExistingOrderId, first, second }));
        }

        [Test]
        public async Task MustCreateOrder()
        {
            var registry = await Repository.CreateOrder(OrderToCreate);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustPersistCreationInHistoryWithUtcTimestamp()
        {
            var order = OrderToCreate;
            order.DateCreated.Returns(DateTime.UtcNow);

            await Repository.CreateOrder(order);
            var loaded = await Repository.GetOrder(order.Id);
            var sequence = await Connection.QuerySingleAsync<int>(
                "SELECT sequence FROM order_status_history WHERE order_id = @Id;", new { order.Id });
            var initial = await Connection.QuerySingleAsync<bool>(
                """
                SELECT previous_status IS NULL AND new_status = 'Received' AND reason IS NULL
                FROM order_status_history WHERE order_id = @Id;
                """, new { order.Id });

            Assert.Multiple(() =>
            {
                Assert.That(sequence, Is.EqualTo(1));
                Assert.That(initial, Is.True);
                Assert.That(loaded!.DateCreated.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(loaded.DateCreated, Is.EqualTo(order.DateCreated.ToUniversalTime()).Within(TimeSpan.FromMilliseconds(1)));
                Assert.That(loaded.DateFinished, Is.Null);
                Assert.That(loaded.Duration, Is.Null);
            });
        }

        [Test]
        public async Task MustRollbackCreationWhenHistoryCannotBeSaved()
        {
            var order = OrderToCreate;
            await Connection.ExecuteAsync("ALTER TABLE order_status_history RENAME TO unavailable_history;");

            Assert.ThrowsAsync<PostgresException>(async () => await Repository.CreateOrder(order));

            var count = await Connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM orders WHERE id = @Id;", new { order.Id });
            Assert.That(count, Is.Zero);
        }

        [Test]
        public async Task MustReuseAmbientTransactionForCreationAndHistory()
        {
            var order = OrderToCreate;
            using var transaction = Connection.BeginTransaction();
            var repository = new OrdersRepository(Connection, new DbTransactionContext { Current = transaction });

            await repository.CreateOrder(order);
            transaction.Rollback();

            var count = await Connection.QuerySingleAsync<int>(
                """
                SELECT (SELECT COUNT(*) FROM orders WHERE id = @Id)
                    + (SELECT COUNT(*) FROM order_status_history WHERE order_id = @Id);
                """, new { order.Id });
            Assert.That(count, Is.Zero);
        }

        [Test]
        public async Task MustProjectCompletionAndDurationFromHistoryAfterDelivery()
        {
            var order = ExistingOrder;
            order.Status.Returns(WorkOrderStatus.WaitingForExecution);
            await Repository.UpdateOrder(order);
            order.Status.Returns(WorkOrderStatus.InExecution);
            await Repository.UpdateOrder(order);
            order.Status.Returns(WorkOrderStatus.Finished);
            var finishedAt = DateTime.UtcNow;
            order.DateFinished.Returns(finishedAt);
            await Repository.UpdateOrder(order);
            order.Status.Returns(WorkOrderStatus.Delivered);
            await Repository.UpdateOrder(order);

            var loaded = await Repository.GetOrder(order.Id);
            var history = (await Connection.QueryAsync<string>(
                "SELECT new_status FROM order_status_history WHERE order_id = @Id ORDER BY sequence;",
                new { order.Id })).ToList();
            var reason = await Connection.QuerySingleAsync<string>(
                "SELECT reason FROM order_status_history WHERE order_id = @Id AND new_status = 'Finished';", new { order.Id });

            Assert.Multiple(() =>
            {
                Assert.That(history, Is.EqualTo(new[] { "Received", "InDiagnosis", "WaitingForApproval", "WaitingForExecution", "InExecution", "Finished", "Delivered" }));
                Assert.That(reason, Is.EqualTo("ServiceCompleted"));
                Assert.That(loaded!.Status, Is.EqualTo(WorkOrderStatus.Delivered));
                Assert.That(loaded.DateCreated, Is.EqualTo(ExistingOrderDateCreated).Within(TimeSpan.FromMilliseconds(1)));
                Assert.That(loaded.DateFinished, Is.EqualTo(finishedAt).Within(TimeSpan.FromMilliseconds(1)));
                Assert.That(loaded.Duration, Is.EqualTo(finishedAt - ExistingOrderDateCreated).Within(TimeSpan.FromMilliseconds(1)));
            });
        }

        [Test]
        public async Task MustKeepBudgetRejectionOutOfServiceCompletionDate()
        {
            var order = ExistingOrder;
            order.Status.Returns(WorkOrderStatus.Finished);
            await Repository.UpdateOrder(order);

            var loaded = await Repository.GetOrder(order.Id);
            var reason = await Connection.QuerySingleAsync<string>(
                "SELECT reason FROM order_status_history WHERE order_id = @Id AND new_status = 'Finished';", new { order.Id });
            Assert.Multiple(() =>
            {
                Assert.That(reason, Is.EqualTo("BudgetRejected"));
                Assert.That(loaded!.DateFinished, Is.Null);
                Assert.That(loaded.Duration, Is.Null);
            });
        }

        [Test]
        public async Task MustNotAppendHistoryWhenOnlyBudgetChanges()
        {
            var order = ExistingOrder;
            order.Budget.Returns(200);
            await Repository.UpdateOrder(order);
            await Repository.UpdateOrder(order);

            var count = await Connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM order_status_history WHERE order_id = @Id;", new { order.Id });
            var loaded = await Repository.GetOrder(order.Id);
            Assert.Multiple(() =>
            {
                Assert.That(count, Is.EqualTo(3));
                Assert.That(loaded!.Budget, Is.EqualTo(200));
            });
        }

        [Test]
        public async Task MustRollbackHistoryWhenOrderUpdateFails()
        {
            await Connection.ExecuteAsync("ALTER TABLE orders ADD CONSTRAINT test_budget_limit CHECK (budget < 200);");
            var order = ExistingOrder;
            order.Status.Returns(WorkOrderStatus.WaitingForExecution);
            order.Budget.Returns(200);

            Assert.ThrowsAsync<PostgresException>(async () => await Repository.UpdateOrder(order));

            var loaded = await Repository.GetOrder(order.Id);
            var count = await Connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM order_status_history WHERE order_id = @Id;", new { order.Id });
            Assert.Multiple(() =>
            {
                Assert.That(loaded!.Status, Is.EqualTo(WorkOrderStatus.WaitingForApproval));
                Assert.That(loaded.Budget, Is.EqualTo(100));
                Assert.That(count, Is.EqualTo(3));
            });
        }

        [Test]
        public async Task MustNotCreateHistoryForMissingOrderOnUpdate()
        {
            var order = OrderToCreate;
            order.Status.Returns(WorkOrderStatus.InDiagnosis);
            var affected = await Repository.UpdateOrder(order);
            var count = await Connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM order_status_history WHERE order_id = @Id;", new { order.Id });
            Assert.Multiple(() =>
            {
                Assert.That(affected, Is.Zero);
                Assert.That(count, Is.Zero);
            });
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
                Assert.That(ordersList[0].DateFinished, Is.EqualTo(ExistingOrder.DateFinished));
            });
        }

        [Test]
        public async Task MustGetOperationalOrdersByPriorityAndCreationDate()
        {
            var oldestInExecutionId = Guid.NewGuid();
            var newestInExecutionId = Guid.NewGuid();
            var waitingForExecutionId = Guid.NewGuid();
            var inDiagnosisId = Guid.NewGuid();
            var receivedId = Guid.NewGuid();
            var finishedId = Guid.NewGuid();
            var deliveredId = Guid.NewGuid();
            var baseDate = DateTime.UtcNow.AddDays(-10);
            var ordersToInsert = new[]
            {
                new { Id = newestInExecutionId, Status = WorkOrderStatus.InExecution.ToString(), DateCreated = baseDate.AddDays(1) },
                new { Id = oldestInExecutionId, Status = WorkOrderStatus.InExecution.ToString(), DateCreated = baseDate },
                new { Id = waitingForExecutionId, Status = WorkOrderStatus.WaitingForExecution.ToString(), DateCreated = baseDate },
                new { Id = inDiagnosisId, Status = WorkOrderStatus.InDiagnosis.ToString(), DateCreated = baseDate },
                new { Id = receivedId, Status = WorkOrderStatus.Received.ToString(), DateCreated = baseDate },
                new { Id = finishedId, Status = WorkOrderStatus.Finished.ToString(), DateCreated = baseDate },
                new { Id = deliveredId, Status = WorkOrderStatus.Delivered.ToString(), DateCreated = baseDate }
            };

            foreach (var order in ordersToInsert)
            {
                await Connection.ExecuteAsync(
                    """
                    INSERT INTO orders(id, customer_document, vehicle_license_plate, budget, status)
                    VALUES (@Id, '417.384.220-11', 'CVC2026', 0, @Status);
                    INSERT INTO order_status_history(id, order_id, sequence, previous_status, new_status, occurred_at)
                    VALUES (@EventId, @Id, 1, NULL, 'Received', @DateCreated);
                    """, new { order.Id, order.Status, order.DateCreated, EventId = Guid.NewGuid() });
            }

            var orders = (await Repository.GetOperationalOrders()).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(
                    orders.Select(order => order.Id),
                    Is.EqualTo(new[]
                    {
                        oldestInExecutionId,
                        newestInExecutionId,
                        waitingForExecutionId,
                        ExistingOrderId,
                        inDiagnosisId,
                        receivedId
                    }));
                Assert.That(orders.Select(order => order.Status), Does.Not.Contain(WorkOrderStatus.Finished));
                Assert.That(orders.Select(order => order.Status), Does.Not.Contain(WorkOrderStatus.Delivered));
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
                Assert.That(order.DateFinished, Is.EqualTo(ExistingOrder.DateFinished));
            });

            Assert.Multiple(() =>
            {
                Assert.That(order.Services, Has.Count.EqualTo(1));
                Assert.That(order.Services.ElementAt(0).Id, Is.EqualTo(Service.Id));
                Assert.That(order.Services.ElementAt(0).Description, Is.EqualTo(Service.Description));
                Assert.That(order.Services.ElementAt(0).Hours, Is.EqualTo(Service.Hours));
                Assert.That(order.Services.ElementAt(0).PricePerHour, Is.EqualTo(Service.PricePerHour));
                Assert.That(order.Services.ElementAt(0).Price, Is.EqualTo(Service.Price));
                Assert.That(order.Services.ElementAt(0).Amount, Is.EqualTo(Service.Amount));
            });

            Assert.Multiple(() =>
            {
                Assert.That(order.Materials, Has.Count.EqualTo(1));
                Assert.That(order.Materials.ElementAt(0).Id, Is.EqualTo(Part.Id));
                Assert.That(order.Materials.ElementAt(0).Name, Is.EqualTo(Part.Name));
                Assert.That(order.Materials.ElementAt(0).Brand, Is.EqualTo(Part.Brand));
                Assert.That(order.Materials.ElementAt(0).Price, Is.EqualTo(Part.Price));
                Assert.That(order.Materials.ElementAt(0).Amount, Is.EqualTo(1));
                Assert.That(order.Materials.ElementAt(0).ReservedAmount, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task MustNotDuplicateOrderItemsWhenOrderHasMultipleServicesAndMaterials()
        {
            var secondService = Substitute.For<IMechanicalService>();
            secondService.Id.Returns(Guid.NewGuid());
            secondService.Description.Returns("Alinhamento");
            secondService.Hours.Returns(1);
            secondService.PricePerHour.Returns(100);
            secondService.Amount.Returns(1);

            await Connection.ExecuteAsync(
                "INSERT INTO services(id, description, hours, price_per_hour) VALUES (@Id, @Description, @Hours, @PricePerHour);",
                secondService);
            await Repository.AddServiceToOrder(ExistingOrderId, secondService);

            var secondMaterial = Substitute.For<IMaterial>();
            secondMaterial.Id.Returns(Guid.NewGuid());
            secondMaterial.Name.Returns("Filtro de óleo");
            secondMaterial.Brand.Returns("Bosch");
            secondMaterial.Price.Returns(30);
            secondMaterial.Amount.Returns(1);
            secondMaterial.ReservedAmount.Returns(0);

            await Connection.ExecuteAsync(
                "INSERT INTO stock(id, name, brand, price, amount, reserved_amount) VALUES (@Id, @Name, @Brand, @Price, @Amount, @ReservedAmount);",
                secondMaterial);
            await Repository.AddMaterialToOrder(ExistingOrderId, secondMaterial);

            var order = await Repository.GetOrder(id: ExistingOrderId);

            Assert.Multiple(() =>
            {
                Assert.That(order, Is.Not.Null);
                Assert.That(order!.Services.Select(service => service.Id), Is.Unique);
                Assert.That(order.Services, Has.Count.EqualTo(2));
                Assert.That(order.Materials.Select(material => material.Id), Is.Unique);
                Assert.That(order.Materials, Has.Count.EqualTo(2));
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
                Assert.That(ordersList[0].DateFinished, Is.EqualTo(ExistingOrder.DateFinished));
            });

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Services, Has.Count.EqualTo(1));
                Assert.That(ordersList[0].Services.ElementAt(0).Id, Is.EqualTo(Service.Id));
                Assert.That(ordersList[0].Services.ElementAt(0).Description, Is.EqualTo(Service.Description));
                Assert.That(ordersList[0].Services.ElementAt(0).Hours, Is.EqualTo(Service.Hours));
                Assert.That(ordersList[0].Services.ElementAt(0).PricePerHour, Is.EqualTo(Service.PricePerHour));
                Assert.That(ordersList[0].Services.ElementAt(0).Price, Is.EqualTo(Service.Price));
                Assert.That(ordersList[0].Services.ElementAt(0).Amount, Is.EqualTo(Service.Amount));
            });

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Materials, Has.Count.EqualTo(1));
                Assert.That(ordersList[0].Materials.ElementAt(0).Id, Is.EqualTo(Part.Id));
                Assert.That(ordersList[0].Materials.ElementAt(0).Name, Is.EqualTo(Part.Name));
                Assert.That(ordersList[0].Materials.ElementAt(0).Brand, Is.EqualTo(Part.Brand));
                Assert.That(ordersList[0].Materials.ElementAt(0).Price, Is.EqualTo(Part.Price));
                Assert.That(ordersList[0].Materials.ElementAt(0).Amount, Is.EqualTo(1));
                Assert.That(ordersList[0].Materials.ElementAt(0).ReservedAmount, Is.EqualTo(0));
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
                Assert.That(ordersList[0].DateFinished, Is.EqualTo(ExistingOrder.DateFinished));
            });

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Services, Has.Count.EqualTo(1));
                Assert.That(ordersList[0].Services.ElementAt(0).Id, Is.EqualTo(Service.Id));
                Assert.That(ordersList[0].Services.ElementAt(0).Description, Is.EqualTo(Service.Description));
                Assert.That(ordersList[0].Services.ElementAt(0).Hours, Is.EqualTo(Service.Hours));
                Assert.That(ordersList[0].Services.ElementAt(0).PricePerHour, Is.EqualTo(Service.PricePerHour));
                Assert.That(ordersList[0].Services.ElementAt(0).Price, Is.EqualTo(Service.Price));
                Assert.That(ordersList[0].Services.ElementAt(0).Amount, Is.EqualTo(Service.Amount));
            });

            Assert.Multiple(() =>
            {
                Assert.That(ordersList[0].Materials, Has.Count.EqualTo(1));
                Assert.That(ordersList[0].Materials.ElementAt(0).Id, Is.EqualTo(Part.Id));
                Assert.That(ordersList[0].Materials.ElementAt(0).Name, Is.EqualTo(Part.Name));
                Assert.That(ordersList[0].Materials.ElementAt(0).Brand, Is.EqualTo(Part.Brand));
                Assert.That(ordersList[0].Materials.ElementAt(0).Price, Is.EqualTo(Part.Price));
                Assert.That(ordersList[0].Materials.ElementAt(0).Amount, Is.EqualTo(1));
                Assert.That(ordersList[0].Materials.ElementAt(0).ReservedAmount, Is.EqualTo(0));
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
            order.DateFinished.Returns(DateTime.UtcNow);
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
            var historyCount = await Connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM order_status_history WHERE order_id = @Id;", new { Id = ExistingOrderId });
            Assert.That(historyCount, Is.EqualTo(3));
        }
    }
}
