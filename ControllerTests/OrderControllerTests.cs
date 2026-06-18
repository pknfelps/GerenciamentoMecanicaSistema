using NSubstitute;
using Service.Interface;
using Service.Interface.Dto.Order;
using System.Dynamic;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class OrderControllerTests : BaseControllerTests
    {
        private IWorkOrderService OrderService { get; set; }

        private static readonly CreateOrderDto OrderToCreate = new("123.456.789-12", "TST1234");
        private static readonly WorkOrderDto ExistingOrder = new(Guid.NewGuid(), "123.456.789-12", "TST1234", 0.0, "Received", DateTime.Now, DateTime.MinValue, TimeSpan.Zero);
        private static readonly DetailedWorkOrderDto ExistingDetailedOrder = new(ExistingOrder.Id, "123.456.789-12", "TST1234", 0.0, "Received", DateTime.Now, DateTime.MinValue, [], [], TimeSpan.Zero);
        private static readonly OrderUpdateItemDto OrderUpdate = new(ExistingOrder.Id, Guid.NewGuid(), 1);

        protected override void MockService()
        {
            OrderService = TestWebAppFactory.OrderServiceMock;

            OrderService.CreateServiceOrder(Arg.Any<CreateOrderDto>()).Returns(callInfo =>
            {
                var order = callInfo.ArgAt<CreateOrderDto>(0);

                if (order.Equals(OrderToCreate))
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.GetOrders().Returns([ExistingOrder]);

            OrderService.GetOrder(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingDetailedOrder.Id)
                    return ExistingDetailedOrder;

                return null;
            });

            OrderService.GetCustomerOrders(Arg.Any<string>()).Returns(callInfo =>
            {
                var document = callInfo.ArgAt<string>(0);

                if (document == ExistingDetailedOrder.CustomerDocument)
                    return [ExistingDetailedOrder];

                return null;
            });

            OrderService.StartDiagnosis(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == OrderUpdate.OrderId)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.AddServiceToOrder(Arg.Any<OrderUpdateItemDto>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<OrderUpdateItemDto>(0);

                if (item.OrderId == OrderUpdate.OrderId)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.RemoveServiceOfOrder(Arg.Any<OrderUpdateItemDto>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<OrderUpdateItemDto>(0);

                if (item.OrderId == OrderUpdate.OrderId)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.AddPartOrSupplieToOrder(Arg.Any<OrderUpdateItemDto>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<OrderUpdateItemDto>(0);

                if (item.OrderId == OrderUpdate.OrderId)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.RemovePartOrSupplieFromOrder(Arg.Any<OrderUpdateItemDto>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<OrderUpdateItemDto>(0);

                if (item.OrderId == OrderUpdate.OrderId)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.CompleteDiagnosis(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.ApproveBudget(Arg.Any<ApproveOrderDto>()).Returns(callInfo =>
            {
                var approve = callInfo.ArgAt<ApproveOrderDto>(0);

                if (approve.OrderId == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.StartExecution(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.CompleteExecution(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.VehicleDelivered(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.DeleteOrder(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });
        }

        [Test]
        public async Task MustCreateOrder()
        {
            var response = await TestClient.PostAsJsonAsync("Order/CreateOrder", OrderToCreate);

            await OrderService.Received(1).CreateServiceOrder(OrderToCreate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryCreateOrderWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync("Order/CreateOrder", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).CreateServiceOrder(Arg.Any<CreateOrderDto>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryCreateOrderWithInvalidOrder()
        {
            var order = new CreateOrderDto("321.654.987-98", "XXX0000");
            var response = await TestClient.PostAsJsonAsync("Order/CreateOrder", order);

            await OrderService.Received(1).CreateServiceOrder(order);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustGetOrders()
        {
            var response = await TestClient.GetAsync("Order/GetOrders");
            var content = response.Content.ReadFromJsonAsAsyncEnumerable<WorkOrderDto>();
            var orders = await content.ToListAsync();

            await OrderService.Received(1).GetOrders();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(orders, Has.Count.EqualTo(1));
            Assert.That(orders[0], Is.Not.Null);
            Assert.That(orders[0].Equals(ExistingOrder), Is.True);
        }

        [Test]
        public async Task MustGetOrder()
        {
            var response = await TestClient.GetAsync($"Order/GetOrder/{ExistingDetailedOrder.Id}");
            var order = await response.Content.ReadFromJsonAsync<DetailedWorkOrderDto>();

            await OrderService.Received(1).GetOrder(ExistingDetailedOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(order, Is.Not.Null);
            Assert.That(order.Equals(ExistingDetailedOrder), Is.True);
        }

        [Test]
        public async Task MustReturnNotFoundIfTryGetOrderThatNotExists()
        {
            var id = Guid.NewGuid();

            var response = await TestClient.GetAsync($"Order/GetOrder/{id}");

            await OrderService.Received(1).GetOrder(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryGetOrderWithInvalidModel()
        {
            var response = await TestClient.GetAsync($"Order/GetOrder/0000");

            await OrderService.ReceivedWithAnyArgs(0).GetOrder(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustGetCustomerOrders()
        {
            var response = await TestClient.GetAsync($"Order/GetCustomerOrders/{ExistingDetailedOrder.CustomerDocument}");

            var orders = response.Content.ReadFromJsonAsAsyncEnumerable<DetailedWorkOrderDto>();
            var ordersList = await orders.ToListAsync();

            await OrderService.Received(1).GetCustomerOrders(ExistingDetailedOrder.CustomerDocument);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(ordersList, Has.Count.EqualTo(1));
            Assert.That(ordersList[0], Is.Not.Null);
            Assert.That(ordersList[0].Equals(ExistingDetailedOrder), Is.True);
        }

        [Test]
        public async Task MustReturnNotFoundIfTryGetCustomerOrdersThatNotExists()
        {
            var id = "951.753.297-10";
            var response = await TestClient.GetAsync($"Order/GetCustomerOrders/{id}");

            await OrderService.Received(1).GetCustomerOrders(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryGetCustomerOrdersWithInvalidModel()
        {
            var response = await TestClient.GetAsync($"Order/GetCustomerOrders/0000");

            await OrderService.ReceivedWithAnyArgs(0).GetOrder(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustStartDiagnosis()
        {
            var response = await TestClient.PostAsync($"Order/StartDiagnosis/{ExistingOrder.Id}", null);

            await OrderService.Received(1).StartDiagnosis(ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInternalServerErrorItTryStartDiagnosisThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PostAsync($"Order/StartDiagnosis/{id}", null);

            await OrderService.Received(1).StartDiagnosis(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestItTryStartDiagnosisWithInvalidModel()
        {
            var response = await TestClient.PostAsync($"Order/StartDiagnosis/0000", null);

            await OrderService.ReceivedWithAnyArgs(0).StartDiagnosis(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustAddServiceToOrder()
        {
            var response = await TestClient.PostAsJsonAsync($"Order/AddServiceToOrder", OrderUpdate);

            await OrderService.Received(1).AddServiceToOrder(OrderUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInternalServerErrorItTryAddServiceToOrderThatNotExists()
        {
            var item = new OrderUpdateItemDto(Guid.NewGuid(), Guid.NewGuid(), 1);
            var response = await TestClient.PostAsJsonAsync($"Order/AddServiceToOrder", item);

            await OrderService.Received(1).AddServiceToOrder(item);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestItTryAddServiceToOrderWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync($"Order/AddServiceToOrder", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).AddServiceToOrder(Arg.Any<OrderUpdateItemDto>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustRemoveServiceOfOrder()
        {
            var response = await TestClient.PostAsJsonAsync($"Order/RemoveServiceOfOrder", OrderUpdate);

            await OrderService.Received(1).RemoveServiceOfOrder(OrderUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInternalServerErrorItTryRemoveServiceOfOrderThatNotExists()
        {
            var item = new OrderUpdateItemDto(Guid.NewGuid(), Guid.NewGuid(), 1);
            var response = await TestClient.PostAsJsonAsync($"Order/RemoveServiceOfOrder", item);

            await OrderService.Received(1).RemoveServiceOfOrder(item);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestItTryRemoveServiceOfOrderWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync($"Order/RemoveServiceOfOrder", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).RemoveServiceOfOrder(Arg.Any<OrderUpdateItemDto>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustAddPartOrSupplieToOrder()
        {
            var response = await TestClient.PostAsJsonAsync($"Order/AddPartOrSupplieToOrder", OrderUpdate);

            await OrderService.Received(1).AddPartOrSupplieToOrder(OrderUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInternalServerErrorItTryAddPartOrSupplieToOrderThatNotExists()
        {
            var item = new OrderUpdateItemDto(Guid.NewGuid(), Guid.NewGuid(), 1);
            var response = await TestClient.PostAsJsonAsync($"Order/AddPartOrSupplieToOrder", item);

            await OrderService.Received(1).AddPartOrSupplieToOrder(item);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestItTryAddPartOrSupplieToOrderWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync($"Order/AddPartOrSupplieToOrder", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).AddPartOrSupplieToOrder(Arg.Any<OrderUpdateItemDto>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustRemovePartOrSupplieFromOrder()
        {
            var response = await TestClient.PostAsJsonAsync($"Order/RemovePartOrSupplieFromOrder", OrderUpdate);

            await OrderService.Received(1).RemovePartOrSupplieFromOrder(OrderUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInternalServerErrorItTryRemovePartOrSupplieFromOrderThatNotExists()
        {
            var item = new OrderUpdateItemDto(Guid.NewGuid(), Guid.NewGuid(), 1);
            var response = await TestClient.PostAsJsonAsync($"Order/RemovePartOrSupplieFromOrder", item);

            await OrderService.Received(1).RemovePartOrSupplieFromOrder(item);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestItTryRemovePartOrSupplieFromOrderWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync($"Order/RemovePartOrSupplieFromOrder", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).RemovePartOrSupplieFromOrder(Arg.Any<OrderUpdateItemDto>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustCompleteDiagnosis()
        {
            var response = await TestClient.PostAsync($"Order/CompleteDiagnosis/{ExistingOrder.Id}", null);

            await OrderService.Received(1).CompleteDiagnosis(ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInternalServerErrorItTryCompleteDiagnosisThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PostAsync($"Order/CompleteDiagnosis/{id}", null);

            await OrderService.Received(1).CompleteDiagnosis(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestItTryCompleteDiagnosisWithInvalidModel()
        {
            var response = await TestClient.PostAsync($"Order/CompleteDiagnosis/0000", null);

            await OrderService.ReceivedWithAnyArgs(0).CompleteDiagnosis(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustApproveBudget()
        {
            var approve = new ApproveOrderDto(ExistingOrder.Id, true);
            var response = await TestClient.PostAsJsonAsync("Order/ApproveBudget", approve);

            await OrderService.Received(1).ApproveBudget(approve);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInvalidServerErrorIfTryApproveBudgetThatNotExists()
        {
            var approve = new ApproveOrderDto(Guid.NewGuid(), true);
            var response = await TestClient.PostAsJsonAsync("Order/ApproveBudget", approve);

            await OrderService.Received(1).ApproveBudget(approve);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryApproveBudgetWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync("Order/ApproveBudget", new { Test = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).ApproveBudget(Arg.Any<ApproveOrderDto>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustStartExecution()
        {
            var response = await TestClient.PostAsync($"Order/StartExecution/{ExistingOrder.Id}", null);

            await OrderService.Received(1).StartExecution(ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInvalidServerErrorIfTryStartExecutionThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PostAsync($"Order/StartExecution/{id}", null);

            await OrderService.Received(1).StartExecution(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryStartExecutionWithInvalidModel()
        {
            var response = await TestClient.PostAsync($"Order/StartExecution/0000", null);

            await OrderService.ReceivedWithAnyArgs(0).StartExecution(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustCompleteExecution()
        {
            var response = await TestClient.PostAsync($"Order/CompleteExecution/{ExistingOrder.Id}", null);

            await OrderService.Received(1).CompleteExecution(ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInvalidServerErrorIfTryCompleteExecutionThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PostAsync($"Order/CompleteExecution/{id}", null);

            await OrderService.Received(1).CompleteExecution(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryCompleteExecutionWithInvalidModel()
        {
            var response = await TestClient.PostAsync($"Order/CompleteExecution/0000", null);

            await OrderService.ReceivedWithAnyArgs(0).CompleteExecution(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustDeliverVehicle()
        {
            var response = await TestClient.PostAsync($"Order/VehicleDelivered/{ExistingOrder.Id}", null);

            await OrderService.Received(1).VehicleDelivered(ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInvalidServerErrorIfTryDeliverVehicleThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PostAsync($"Order/VehicleDelivered/{id}", null);

            await OrderService.Received(1).VehicleDelivered(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeliverVehicleWithInvalidModel()
        {
            var response = await TestClient.PostAsync($"Order/VehicleDelivered/0000", null);

            await OrderService.ReceivedWithAnyArgs(0).VehicleDelivered(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustDeleteOrder()
        {
            var response = await TestClient.DeleteAsync($"Order/DeleteOrder/{ExistingOrder.Id}");

            await OrderService.Received(1).DeleteOrder(ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInvalidServerErrorIfTryDeleteOrderThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.DeleteAsync($"Order/DeleteOrder/{id}");

            await OrderService.Received(1).DeleteOrder(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteOrderWithInvalidModel()
        {
            var response = await TestClient.DeleteAsync($"Order/DeleteOrder/0000");

            await OrderService.ReceivedWithAnyArgs(0).DeleteOrder(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }
}
