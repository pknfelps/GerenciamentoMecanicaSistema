using GerenciamentoMecanicaSistema.Contracts.Requests.Order;
using GerenciamentoMecanicaSistema.Contracts.Responses.Order;
using Domain.Interface.Order;
using NSubstitute;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.Customer;
using Service.Interface.Commands.Order;
using Service.Interface.Commands.Vehicle;
using Service.Interface.Results.Order;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class OrderControllerTests : BaseControllerTests
    {
        private IOrdersService OrderService { get; set; }

        private static readonly DetailedWorkOrderResult ExistingOrder = new(Guid.NewGuid(), "123.456.789-12", "TST1234", 0.0m, "Received", DateTime.Now, DateTime.MinValue, [], [], TimeSpan.Zero);
        private static readonly Guid ExistingServiceId = Guid.NewGuid();
        private static readonly Guid ExistingMaterialId = Guid.NewGuid();
        private static readonly CreateOrderRequest OrderToCreate = new(
            new("Teste", ExistingOrder.CustomerDocument, "(11) 91234-5678", "teste@gmail.com"),
            new(ExistingOrder.CustomerDocument, "Honda", "Civic", 2026, ExistingOrder.VehicleLicensePlate),
            [new(ExistingServiceId, 1)],
            [new(ExistingMaterialId, 2)]);
        private static readonly UpdateOrderItemRequest<int> OrderUpdate = new(Guid.NewGuid(), 1);

        protected override void MockService()
        {
            OrderService = TestWebAppFactory.OrderServiceMock;

            OrderService.CreateServiceOrder(
                Arg.Any<CreateCustomerCommand>(),
                Arg.Any<CreateVehicleCommand>(),
                Arg.Any<IReadOnlyCollection<UpdateOrderItemCommand<int>>>(),
                Arg.Any<IReadOnlyCollection<UpdateOrderItemCommand<int>>>()).Returns(callInfo =>
            {
                var customer = callInfo.ArgAt<CreateCustomerCommand>(0);
                var vehicle = callInfo.ArgAt<CreateVehicleCommand>(1);
                var services = callInfo.ArgAt<IReadOnlyCollection<UpdateOrderItemCommand<int>>>(2);
                var materials = callInfo.ArgAt<IReadOnlyCollection<UpdateOrderItemCommand<int>>>(3);

                if (AreEquivalent(customer, vehicle, services, materials, OrderToCreate))
                    return ExistingOrder.Id;

                throw new NotFoundException("Recurso não encontrado");
            });

            OrderService.GetOrderStatus(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return WorkOrderStatus.Received;

                throw new NotFoundException("Ordem não encontrada");
            });

            OrderService.GetOrders(id: Arg.Any<Guid?>(), customerDocument: Arg.Any<string>(), vehicleLicensePlate: Arg.Any<string>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid?>(0);
                var document = callInfo.ArgAt<string>(1);
                var vehicle = callInfo.ArgAt<string>(2);

                if (id != null)
                    if (id == ExistingOrder.Id)
                        return [ExistingOrder];
                    else
                        return [];

                if (!string.IsNullOrEmpty(document))
                    if (document == ExistingOrder.CustomerDocument)
                        return [ExistingOrder];
                    else
                        return [];

                if (!string.IsNullOrEmpty(vehicle))
                    if (vehicle == ExistingOrder.VehicleLicensePlate)
                        return [ExistingOrder];
                    else
                        return [];

                return [ExistingOrder];
            });

            OrderService.StartDiagnosis(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            OrderService.AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<UpdateOrderItemCommand<int>>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            OrderService.RemoveServiceOfOrder(Arg.Any<Guid>(), Arg.Any<UpdateOrderItemCommand<int>>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            OrderService.AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<UpdateOrderItemCommand<int>>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;


                throw new NotFoundException("Recurso não encontrado");
            });

            OrderService.RemoveMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<UpdateOrderItemCommand<int>>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            OrderService.CompleteDiagnosis(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            OrderService.ApproveBudget(Arg.Any<Guid>(), Arg.Any<ApproveOrderCommand>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            OrderService.StartExecution(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            OrderService.CompleteExecution(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            OrderService.DeliverVehicle(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            OrderService.DeleteOrder(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });
        }

        [Test]
        public async Task MustCreateOrder()
        {
            var response = await TestClient.PostAsJsonAsync($"orders", OrderToCreate);
            var createdOrder = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();

            await OrderService.Received(1).CreateServiceOrder(
                Arg.Is<CreateCustomerCommand>(customer => customer == OrderToCreate.Customer.ToCommand()),
                Arg.Is<CreateVehicleCommand>(vehicle => vehicle == OrderToCreate.Vehicle.ToCommand()),
                Arg.Is<IReadOnlyCollection<UpdateOrderItemCommand<int>>>(services => services.SequenceEqual(OrderToCreate.Services.Select(service => service.ToCommand()))),
                Arg.Is<IReadOnlyCollection<UpdateOrderItemCommand<int>>>(materials => materials.SequenceEqual(OrderToCreate.Materials.Select(material => material.ToCommand()))));

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
                Assert.That(createdOrder?.Id, Is.EqualTo(ExistingOrder.Id));
                Assert.That(response.Headers.Location?.Query, Does.Contain(ExistingOrder.Id.ToString()));
            });
        }

        [Test]
        public async Task MustReturnBadRequestIfTryCreateOrderWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync($"orders", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).CreateServiceOrder(
                default!,
                default!,
                default!,
                default!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustReturnNotFoundIfTryCreateOrderWithInvalidOrder()
        {
            var order = new CreateOrderRequest(
                new("Outro cliente", "529.982.247-25", "(11) 91234-5678", "outro@gmail.com"),
                new("529.982.247-25", "Ford", "Fiesta", 2020, "ABC1D23"),
                [new(Guid.NewGuid(), 1)],
                []);
            var response = await TestClient.PostAsJsonAsync($"orders", order);

            await OrderService.ReceivedWithAnyArgs(1).CreateServiceOrder(
                default!,
                default!,
                default!,
                default!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustGetOrderStatus()
        {
            var response = await TestClient.GetAsync($"orders/{ExistingOrder.Id}/status");
            var orderStatus = await response.Content.ReadFromJsonAsync<OrderStatusResponse>();

            await OrderService.Received(1).GetOrderStatus(ExistingOrder.Id);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(orderStatus?.Id, Is.EqualTo(ExistingOrder.Id));
                Assert.That(orderStatus?.Status, Is.EqualTo(WorkOrderStatus.Received.ToString()));
            });
        }

        [Test]
        public async Task MustReturnNotFoundIfOrderStatusDoesNotExist()
        {
            var id = Guid.NewGuid();

            var response = await TestClient.GetAsync($"orders/{id}/status");

            await OrderService.Received(1).GetOrderStatus(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestIfOrderStatusIdIsInvalid()
        {
            var response = await TestClient.GetAsync("orders/00000/status");

            await OrderService.ReceivedWithAnyArgs(0).GetOrderStatus(Guid.Empty);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustGetOrders()
        {
            var response = await TestClient.GetAsync($"orders");
            var orders = await response.Content.ReadFromJsonAsync<List<WorkOrderResponse>>();

            await OrderService.Received(1).GetOrders();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(orders, Has.Count.EqualTo(1));
            Assert.That(orders[0], Is.Not.Null);
            Assert.That(orders[0].Id, Is.EqualTo(ExistingOrder.Id));
        }

        [Test]
        public async Task MustGetOrder()
        {
            var response = await TestClient.GetAsync($"orders?id={ExistingOrder.Id}");

            var orders = await response.Content.ReadFromJsonAsync<List<WorkOrderResponse>>();

            await OrderService.Received(1).GetOrders(id: ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(orders, Has.Count.EqualTo(1));

            var order = orders[0];
            Assert.That(order, Is.Not.Null);
            Assert.That(order.Id, Is.EqualTo(ExistingOrder.Id));
        }

        [Test]
        public async Task MustGetDetailedOrder()
        {
            var response = await TestClient.GetAsync($"orders/details?id={ExistingOrder.Id}");
            var orders = await response.Content.ReadFromJsonAsync<List<DetailedWorkOrderResponse>>();

            await OrderService.Received(1).GetOrders(id: ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(orders, Has.Count.EqualTo(1));

            var order = orders[0];
            Assert.That(order, Is.Not.Null);
            Assert.That(order.Id, Is.EqualTo(ExistingOrder.Id));
        }

        [Test]
        public async Task MustGetVehicleOrders()
        {
            var response = await TestClient.GetAsync($"orders/vehicles/{ExistingOrder.VehicleLicensePlate}");

            var orders = await response.Content.ReadFromJsonAsync<List<DetailedWorkOrderResponse>>();

            await OrderService.Received(1).GetOrders(vehicleLicensePlate: ExistingOrder.VehicleLicensePlate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(orders, Has.Count.EqualTo(1));
            Assert.That(orders[0], Is.Not.Null);
            Assert.That(orders[0].Id, Is.EqualTo(ExistingOrder.Id));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryGetVehicleOrdersWithInvalidModel()
        {
            var response = await TestClient.GetAsync($"orders/vehicles/0000");

            await OrderService.Received(0).GetOrder(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustStartDiagnosis()
        {
            var response = await TestClient.PatchAsync($"orders/{ExistingOrder.Id}/diagnosis/start", null);

            await OrderService.Received(1).StartDiagnosis(ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task MustReturnNotFoundItTryStartDiagnosisThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PatchAsync($"orders/{id}/diagnosis/start", null);

            await OrderService.Received(1).StartDiagnosis(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestItTryStartDiagnosisWithInvalidModel()
        {
            var response = await TestClient.PatchAsync($"orders/0000/diagnosis/start", null);

            await OrderService.ReceivedWithAnyArgs(0).StartDiagnosis(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustAddServiceToOrder()
        {
            var response = await TestClient.PostAsJsonAsync($"orders/{ExistingOrder.Id}/services", OrderUpdate);

            await OrderService.Received(1).AddServiceToOrder(ExistingOrder.Id, Arg.Any<UpdateOrderItemCommand<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnNotFoundItTryAddServiceToOrderThatNotExists()
        {
            var response = await TestClient.PostAsJsonAsync($"orders/{Guid.NewGuid()}/services", OrderUpdate);

            await OrderService.Received(1).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<UpdateOrderItemCommand<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestItTryAddServiceToOrderWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/0000/services", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<UpdateOrderItemCommand<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustRemoveServiceOfOrder()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/{ExistingOrder.Id}/services", OrderUpdate);

            await OrderService.Received(1).RemoveServiceOfOrder(ExistingOrder.Id, Arg.Any<UpdateOrderItemCommand<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task MustReturnNotFoundItTryRemoveServiceOfOrderThatNotExists()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/{Guid.NewGuid()}/services", OrderUpdate);

            await OrderService.Received(1).RemoveServiceOfOrder(Arg.Any<Guid>(), Arg.Any<UpdateOrderItemCommand<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestItTryRemoveServiceOfOrderWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/0000/services", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).RemoveServiceOfOrder(Arg.Any<Guid>(), Arg.Any<UpdateOrderItemCommand<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustAddMaterialToOrder()
        {
            var response = await TestClient.PostAsJsonAsync($"orders/{ExistingOrder.Id}/materials", OrderUpdate);

            await OrderService.Received(1).AddMaterialToOrder(ExistingOrder.Id, Arg.Any<UpdateOrderItemCommand<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnNotFoundItTryAddMaterialToOrderThatNotExists()
        {
            var response = await TestClient.PostAsJsonAsync($"orders/{Guid.NewGuid()}/materials", OrderUpdate);

            await OrderService.Received(1).AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<UpdateOrderItemCommand<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestItTryAddMaterialToOrderWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/{ExistingOrder.Id}/materials", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<UpdateOrderItemCommand<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustRemoveMaterialFromOrder()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/{ExistingOrder.Id}/materials", OrderUpdate);

            await OrderService.Received(1).RemoveMaterialFromOrder(ExistingOrder.Id, Arg.Any<UpdateOrderItemCommand<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task MustReturnNotFoundItTryRemoveMaterialFromOrderThatNotExists()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/{Guid.NewGuid()}/materials", OrderUpdate);

            await OrderService.Received(1).RemoveMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<UpdateOrderItemCommand<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestItTryRemoveMaterialFromOrderWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/{ExistingOrder.Id}/materials", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).RemoveMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<UpdateOrderItemCommand<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustCompleteDiagnosis()
        {
            var response = await TestClient.PatchAsync($"orders/{ExistingOrder.Id}/diagnosis/complete", null);

            await OrderService.Received(1).CompleteDiagnosis(ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task MustReturnNotFoundItTryCompleteDiagnosisThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PatchAsync($"orders/{id}/diagnosis/complete", null);

            await OrderService.Received(1).CompleteDiagnosis(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestItTryCompleteDiagnosisWithInvalidModel()
        {
            var response = await TestClient.PatchAsync($"orders/0000/diagnosis/complete", null);

            await OrderService.ReceivedWithAnyArgs(0).CompleteDiagnosis(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustApproveBudget()
        {
            var approve = new ApproveOrderRequest(ExistingOrder.CustomerDocument, true);
            var response = await TestClient.PatchAsJsonAsync($"orders/{ExistingOrder.Id}/budget", approve);

            await OrderService.Received(1).ApproveBudget(ExistingOrder.Id, approve.ToCommand());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task MustReturnNotFoundIfTryApproveBudgetThatNotExists()
        {
            var approve = new ApproveOrderRequest(ExistingOrder.CustomerDocument, true);
            var response = await TestClient.PatchAsJsonAsync($"orders/{Guid.NewGuid()}/budget", approve);

            await OrderService.Received(1).ApproveBudget(Arg.Any<Guid>(), approve.ToCommand());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryApproveBudgetWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/0000/budget", new { Teste = "teste" });

            await OrderService.ReceivedWithAnyArgs(0).ApproveBudget(Arg.Any<Guid>(), Arg.Any<ApproveOrderCommand>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustStartExecution()
        {
            var response = await TestClient.PatchAsync($"orders/{ExistingOrder.Id}/execution/start", null);

            await OrderService.Received(1).StartExecution(ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task MustReturnNotFoundIfTryStartExecutionThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PatchAsync($"orders/{id}/execution/start", null);

            await OrderService.Received(1).StartExecution(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryStartExecutionWithInvalidModel()
        {
            var response = await TestClient.PatchAsync($"orders/0000/execution/start", null);

            await OrderService.ReceivedWithAnyArgs(0).StartExecution(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustCompleteExecution()
        {
            var response = await TestClient.PatchAsync($"orders/{ExistingOrder.Id}/execution/complete", null);

            await OrderService.Received(1).CompleteExecution(ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task MustReturnNotFoundIfTryCompleteExecutionThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PatchAsync($"orders/{id}/execution/complete", null);

            await OrderService.Received(1).CompleteExecution(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryCompleteExecutionWithInvalidModel()
        {
            var response = await TestClient.PatchAsync($"orders/0000/execution/complete", null);

            await OrderService.ReceivedWithAnyArgs(0).CompleteExecution(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustDeliverVehicle()
        {
            var response = await TestClient.PatchAsync($"orders/{ExistingOrder.Id}/delivery", null);

            await OrderService.Received(1).DeliverVehicle(ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task MustReturnNotFoundIfTryDeliverVehicleThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PatchAsync($"orders/{id}/delivery", null);

            await OrderService.Received(1).DeliverVehicle(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeliverVehicleWithInvalidModel()
        {
            var response = await TestClient.PatchAsync($"orders/0000/delivery", null);

            await OrderService.ReceivedWithAnyArgs(0).DeliverVehicle(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustDeleteOrder()
        {
            var response = await TestClient.DeleteAsync($"orders/{ExistingOrder.Id}");

            await OrderService.Received(1).DeleteOrder(ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task MustReturnNotFoundIfTryDeleteOrderThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.DeleteAsync($"orders/{id}");

            await OrderService.Received(1).DeleteOrder(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteOrderWithInvalidModel()
        {
            var response = await TestClient.DeleteAsync($"orders/00000");

            await OrderService.ReceivedWithAnyArgs(0).DeleteOrder(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        private static bool AreEquivalent(
            CreateCustomerCommand customer,
            CreateVehicleCommand vehicle,
            IReadOnlyCollection<UpdateOrderItemCommand<int>> services,
            IReadOnlyCollection<UpdateOrderItemCommand<int>> materials,
            CreateOrderRequest request) =>
            customer == request.Customer.ToCommand()
            && vehicle == request.Vehicle.ToCommand()
            && services.SequenceEqual(request.Services.Select(service => service.ToCommand()))
            && materials.SequenceEqual(request.Materials.Select(material => material.ToCommand()));
    }
}
