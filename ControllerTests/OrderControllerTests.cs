using NSubstitute;
using Service.Interface;
using Service.Interface.Dto;
using Service.Interface.Dto.Order;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class OrderControllerTests : BaseControllerTests
    {
        private IOrdersService OrderService { get; set; }

        private static readonly CreateOrderDto OrderToCreate = new("123.456.789-12", "TST1234");
        private static readonly DetailedWorkOrderDto ExistingOrder = new(Guid.NewGuid(), "123.456.789-12", "TST1234", 0.0, "Received", DateTime.Now, DateTime.MinValue, [], [], TimeSpan.Zero);
        private static readonly UpdateItemDto<int> OrderUpdate = new(Guid.NewGuid(), 1);

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

                throw new InvalidOperationException();
            });

            OrderService.AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<UpdateItemDto<int>>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.RemoveServiceOfOrder(Arg.Any<Guid>(), Arg.Any<UpdateItemDto<int>>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            OrderService.AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<UpdateItemDto<int>>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
                    return Task.CompletedTask;


                throw new InvalidOperationException();
            });

            OrderService.RemoveMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<UpdateItemDto<int>>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
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

            OrderService.ApproveBudget(Arg.Any<Guid>(), Arg.Any<ApproveOrderDto>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingOrder.Id)
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

            OrderService.DeliverVehicle(Arg.Any<Guid>()).Returns(callInfo =>
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
            var response = await TestClient.PostAsJsonAsync($"orders", OrderToCreate);

            await OrderService.Received(1).CreateServiceOrder(OrderToCreate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryCreateOrderWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync($"orders", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).CreateServiceOrder(Arg.Any<CreateOrderDto>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryCreateOrderWithInvalidOrder()
        {
            var order = new CreateOrderDto("321.654.987-98", "XXX0000");
            var response = await TestClient.PostAsJsonAsync($"orders", order);

            await OrderService.Received(1).CreateServiceOrder(order);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustGetOrders()
        {
            var response = await TestClient.GetAsync($"orders");
            var orders = await response.Content.ReadFromJsonAsync<List<WorkOrderDto>>();

            await OrderService.Received(1).GetOrders();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(orders, Has.Count.EqualTo(1));
            Assert.That(orders[0], Is.Not.Null);
            Assert.That(orders[0].Equals(ExistingOrder), Is.True);
        }

        [Test]
        public async Task MustGetOrder()
        {
            var response = await TestClient.GetAsync($"orders?id={ExistingOrder.Id}");

            var orders = await response.Content.ReadFromJsonAsync<List<WorkOrderDto>>();

            await OrderService.Received(1).GetOrders(id: ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(orders, Has.Count.EqualTo(1));

            var order = orders[0];
            Assert.That(order, Is.Not.Null);
            Assert.That(order.Equals(ExistingOrder), Is.True);
        }

        [Test]
        public async Task MustGetDetailedOrder()
        {
            var response = await TestClient.GetAsync($"orders/details?id={ExistingOrder.Id}");
            var orders = await response.Content.ReadFromJsonAsync<List<DetailedWorkOrderDto>>();

            await OrderService.Received(1).GetOrders(id: ExistingOrder.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(orders, Has.Count.EqualTo(1));

            var order = orders[0];
            Assert.That(order, Is.Not.Null);
            Assert.That(order.Equals(ExistingOrder), Is.True);
        }

        [Test]
        public async Task MustGetVehicleOrders()
        {
            var response = await TestClient.GetAsync($"orders/vehicles/{ExistingOrder.VehicleLicensePlate}");

            var orders = await response.Content.ReadFromJsonAsync<List<DetailedWorkOrderDto>>();

            await OrderService.Received(1).GetOrders(vehicleLicensePlate: ExistingOrder.VehicleLicensePlate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(orders, Has.Count.EqualTo(1));
            Assert.That(orders[0], Is.Not.Null);
            Assert.That(orders[0].Equals(ExistingOrder), Is.True);
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
        public async Task MustReturnInternalServerErrorItTryStartDiagnosisThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PatchAsync($"orders/{id}/diagnosis/start", null);

            await OrderService.Received(1).StartDiagnosis(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
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

            await OrderService.Received(1).AddServiceToOrder(ExistingOrder.Id, Arg.Any<UpdateItemDto<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInternalServerErrorItTryAddServiceToOrderThatNotExists()
        {
            var response = await TestClient.PostAsJsonAsync($"orders/{Guid.NewGuid()}/services", OrderUpdate);

            await OrderService.Received(1).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<UpdateItemDto<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestItTryAddServiceToOrderWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/0000/services", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).AddServiceToOrder(Arg.Any<Guid>(), Arg.Any<UpdateItemDto<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustRemoveServiceOfOrder()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/{ExistingOrder.Id}/services", OrderUpdate);

            await OrderService.Received(1).RemoveServiceOfOrder(ExistingOrder.Id, Arg.Any<UpdateItemDto<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task MustReturnInternalServerErrorItTryRemoveServiceOfOrderThatNotExists()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/{Guid.NewGuid()}/services", OrderUpdate);

            await OrderService.Received(1).RemoveServiceOfOrder(Arg.Any<Guid>(), Arg.Any<UpdateItemDto<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestItTryRemoveServiceOfOrderWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/0000/services", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).RemoveServiceOfOrder(Arg.Any<Guid>(), Arg.Any<UpdateItemDto<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustAddMaterialToOrder()
        {
            var response = await TestClient.PostAsJsonAsync($"orders/{ExistingOrder.Id}/materials", OrderUpdate);

            await OrderService.Received(1).AddMaterialToOrder(ExistingOrder.Id, Arg.Any<UpdateItemDto<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task MustReturnInternalServerErrorItTryAddMaterialToOrderThatNotExists()
        {
            var response = await TestClient.PostAsJsonAsync($"orders/{Guid.NewGuid()}/materials", OrderUpdate);

            await OrderService.Received(1).AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<UpdateItemDto<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestItTryAddMaterialToOrderWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/{ExistingOrder.Id}/materials", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).AddMaterialToOrder(Arg.Any<Guid>(), Arg.Any<UpdateItemDto<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task MustRemoveMaterialFromOrder()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/{ExistingOrder.Id}/materials", OrderUpdate);

            await OrderService.Received(1).RemoveMaterialFromOrder(ExistingOrder.Id, Arg.Any<UpdateItemDto<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task MustReturnInternalServerErrorItTryRemoveMaterialFromOrderThatNotExists()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/{Guid.NewGuid()}/materials", OrderUpdate);

            await OrderService.Received(1).RemoveMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<UpdateItemDto<int>>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestItTryRemoveMaterialFromOrderWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/{ExistingOrder.Id}/materials", new { Teste = "Teste" });

            await OrderService.ReceivedWithAnyArgs(0).RemoveMaterialFromOrder(Arg.Any<Guid>(), Arg.Any<UpdateItemDto<int>>());

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
        public async Task MustReturnInternalServerErrorItTryCompleteDiagnosisThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PatchAsync($"orders/{id}/diagnosis/complete", null);

            await OrderService.Received(1).CompleteDiagnosis(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
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
            var approve = new ApproveOrderDto(ExistingOrder.CustomerDocument, true);
            var response = await TestClient.PatchAsJsonAsync($"orders/{ExistingOrder.Id}/budget", approve);

            await OrderService.Received(1).ApproveBudget(ExistingOrder.Id, approve);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task MustReturnInvalidServerErrorIfTryApproveBudgetThatNotExists()
        {
            var approve = new ApproveOrderDto(ExistingOrder.CustomerDocument, true);
            var response = await TestClient.PatchAsJsonAsync($"orders/{Guid.NewGuid()}/budget", approve);

            await OrderService.Received(1).ApproveBudget(Arg.Any<Guid>(), approve);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryApproveBudgetWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"orders/0000/budget", new { Teste = "teste" });

            await OrderService.ReceivedWithAnyArgs(0).ApproveBudget(Arg.Any<Guid>(), Arg.Any<ApproveOrderDto>());

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
        public async Task MustReturnInvalidServerErrorIfTryStartExecutionThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PatchAsync($"orders/{id}/execution/start", null);

            await OrderService.Received(1).StartExecution(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
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
        public async Task MustReturnInvalidServerErrorIfTryCompleteExecutionThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PatchAsync($"orders/{id}/execution/complete", null);

            await OrderService.Received(1).CompleteExecution(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
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
        public async Task MustReturnInvalidServerErrorIfTryDeliverVehicleThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.PatchAsync($"orders/{id}/delivery", null);

            await OrderService.Received(1).DeliverVehicle(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
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
        public async Task MustReturnInvalidServerErrorIfTryDeleteOrderThatNotExists()
        {
            var id = Guid.NewGuid();
            var response = await TestClient.DeleteAsync($"orders/{id}");

            await OrderService.Received(1).DeleteOrder(id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteOrderWithInvalidModel()
        {
            var response = await TestClient.DeleteAsync($"orders/00000");

            await OrderService.ReceivedWithAnyArgs(0).DeleteOrder(Arg.Any<Guid>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }
}
