using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using NSubstitute;

namespace DomainTests.Order
{
    public class OrderTests
    {
        private IOrder ReceivedOrder { get; set; }
        private IOrder OrderInExecution { get; set; }
        private string Document { get; set; } = "662.119.730-63";
        private string LicensePlate { get; set; } = "CAR1234";

        [SetUp]
        public void SetUp()
        {
            ReceivedOrder = new Domain.WorkOrder.Order(Document, LicensePlate, DateTime.Now);
            OrderInExecution = new Domain.WorkOrder.Order(Guid.NewGuid(), Document, LicensePlate, [], [], 10.0m, WorkOrderStatus.InExecution, DateTime.Now, DateTime.MinValue);
        }

        [Test]
        public void MustCreateWorkOrderWithoutId()
        {
            Assert.That(ReceivedOrder, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(ReceivedOrder.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(ReceivedOrder.CustomerDocument, Is.Not.Null);
                Assert.That(ReceivedOrder.VehicleLicensePlate, Is.Not.Null);
                Assert.That(ReceivedOrder.Services, Is.Empty);
                Assert.That(ReceivedOrder.Materials, Is.Empty);
                Assert.That(ReceivedOrder.Budget, Is.EqualTo(0.0m));
                Assert.That(ReceivedOrder.Status, Is.EqualTo(WorkOrderStatus.Received));
                Assert.That(ReceivedOrder.DateCreated, Is.Not.EqualTo(DateTime.MinValue));
                Assert.That(ReceivedOrder.DateFinished, Is.EqualTo(DateTime.MinValue));
                Assert.That(ReceivedOrder.Duration, Is.EqualTo(TimeSpan.Zero));
            });
        }

        [Test]
        public void MustNotCreateWorkOrderIfIdIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new Domain.WorkOrder.Order(Guid.Empty, Document, LicensePlate, [], [], 0.0m, WorkOrderStatus.Received, DateTime.Now, DateTime.MinValue));
        }

        [Test]
        public void MustNotCreateWorkOrderIfClientIsNull()
        {
            Assert.Throws<ArgumentException>(() => new Domain.WorkOrder.Order("", LicensePlate, DateTime.Now));
        }

        [Test]
        public void MustNotCreateWorkOrderIfVehicleIsNull()
        {
            Assert.Throws<ArgumentException>(() => new Domain.WorkOrder.Order(Document, "", DateTime.Now));
        }

        [Test]
        public void MustNotCreateWorkOrderIfBudgetIsLowerThan0()
        {
            Assert.Throws<ArgumentException>(() => new Domain.WorkOrder.Order(Guid.NewGuid(), Document, LicensePlate, [], [], -1.0m, WorkOrderStatus.Received, DateTime.Now, DateTime.MinValue));
        }

        [Test]
        public void MustStartDiagnosis()
        {
            ReceivedOrder.StartDiagnosis();

            Assert.That(ReceivedOrder.Status, Is.EqualTo(WorkOrderStatus.InDiagnosis));
        }

        [Test]
        public void MustNotStartDiagnosisIfNotInAValidState()
        {
            ReceivedOrder.StartDiagnosis();

            Assert.That(ReceivedOrder.Status, Is.EqualTo(WorkOrderStatus.InDiagnosis));

            Assert.Throws<InvalidOperationException>(() => ReceivedOrder.StartDiagnosis());
        }

        [Test]
        public void MustAddService()
        {
            ReceivedOrder.StartDiagnosis();

            ReceivedOrder.AddService(Substitute.For<IMechanicalService>());

            Assert.That(ReceivedOrder.Services, Has.Count.EqualTo(1));
        }

        [Test]
        public void MustAddServiceAmount()
        {
            ReceivedOrder.StartDiagnosis();

            var serviceId = Guid.NewGuid();
            var service = Substitute.For<IMechanicalService>();
            service.Id.Returns(serviceId);
            service.Amount.Returns(1);
            service.When(s => s.AddServiceAmount(1)).Do(_ => service.Amount.Returns(2));

            ReceivedOrder.AddService(service);
            ReceivedOrder.AddService(service);

            Assert.That(ReceivedOrder.Services, Has.Count.EqualTo(1));
            Assert.That(ReceivedOrder.Services.ElementAt(0).Amount, Is.EqualTo(2));
        }

        [Test]
        public void MustNotAddServiceIfStatusIsReceived()
        {
            Assert.Throws<InvalidOperationException>(() => ReceivedOrder.AddService(Substitute.For<IMechanicalService>()));
        }

        [Test]
        public void MustNotAddServiceIfServiceAlreadyStarted()
        {
            Assert.Throws<InvalidOperationException>(() => OrderInExecution.AddService(Substitute.For<IMechanicalService>()));
        }

        [Test]
        public void MustRemoveService()
        {
            ReceivedOrder.StartDiagnosis();

            var serviceId = Guid.NewGuid();
            var service = Substitute.For<IMechanicalService>();
            service.Id.Returns(serviceId);
            service.Amount.Returns(1);
            service.When(s => s.RemoveServiceAmount(1)).Do(_ => service.Amount.Returns(0));

            ReceivedOrder.AddService(service);

            ReceivedOrder.RemoveService(service);

            Assert.That(ReceivedOrder.Services, Is.Empty);
        }

        [Test]
        public void MustRemoveServiceAmount()
        {
            ReceivedOrder.StartDiagnosis();

            var serviceId = Guid.NewGuid();
            var serviceToAdd = Substitute.For<IMechanicalService>();
            serviceToAdd.Id.Returns(serviceId);
            serviceToAdd.Amount.Returns(5);
            serviceToAdd.When(s => s.RemoveServiceAmount(1)).Do(_ => serviceToAdd.Amount.Returns(4));

            ReceivedOrder.AddService(serviceToAdd);

            var serviceToRemove = Substitute.For<IMechanicalService>();
            serviceToRemove.Id.Returns(serviceId);
            serviceToRemove.Amount.Returns(1);

            ReceivedOrder.RemoveService(serviceToRemove);

            Assert.That(ReceivedOrder.Services, Has.Count.EqualTo(1));
            Assert.That(ReceivedOrder.Services.ElementAt(0).Amount, Is.EqualTo(4));
        }

        [Test]
        public void MustNotRemoveServiceIfStatusIsReceived()
        {
            Assert.Throws<InvalidOperationException>(() => ReceivedOrder.RemoveService(Substitute.For<IMechanicalService>()));
        }

        [Test]
        public void MustNotRemoveServiceIfStatusIsServiceAlreadyStarted()
        {
            Assert.Throws<InvalidOperationException>(() => OrderInExecution.RemoveService(Substitute.For<IMechanicalService>()));
        }

        [Test]
        public void MustAddPartOrSupplie()
        {
            ReceivedOrder.StartDiagnosis();

            ReceivedOrder.AddMaterial(Substitute.For<IMaterial>());

            Assert.That(ReceivedOrder.Materials, Has.Count.EqualTo(1));
        }

        [Test]
        public void MustAddPartOrSupplieAmount()
        {
            ReceivedOrder.StartDiagnosis();

            var partId = Guid.NewGuid();
            var part = Substitute.For<IMaterial>();
            part.Id.Returns(partId);
            part.Amount.Returns(1);
            part.When(part => part.AddAmount(1)).Do(_ => part.Amount.Returns(2));

            ReceivedOrder.AddMaterial(part);
            ReceivedOrder.AddMaterial(part);

            Assert.That(ReceivedOrder.Materials, Has.Count.EqualTo(1));
            Assert.That(ReceivedOrder.Materials.ElementAt(0).Amount, Is.EqualTo(2));
        }

        [Test]
        public void MustNotAddPartOrSupplieIfStatusIsReceived()
        {
            Assert.Throws<InvalidOperationException>(() => ReceivedOrder.AddMaterial(Substitute.For<IMaterial>()));
        }

        [Test]
        public void MustNotAddPartOrSupplieIfServiceAlreadyStarted()
        {
            Assert.Throws<InvalidOperationException>(() => OrderInExecution.AddMaterial(Substitute.For<IMaterial>()));
        }

        [Test]
        public void MustRemovePartOsSupplie()
        {
            ReceivedOrder.StartDiagnosis();

            var partId = Guid.NewGuid();
            var part = Substitute.For<IMaterial>();
            part.Id.Returns(partId);
            part.Amount.Returns(1);
            part.When(p => p.RemoveAmount(1)).Do(_ => part.Amount.Returns(0));

            ReceivedOrder.AddMaterial(part);

            ReceivedOrder.RemoveMaterial(part);

            Assert.That(ReceivedOrder.Materials, Is.Empty);
        }

        [Test]
        public void MustRemovePartOsSupplieAmount()
        {
            ReceivedOrder.StartDiagnosis();

            var partId = Guid.NewGuid();
            var partToAdd = Substitute.For<IMaterial>();
            partToAdd.Id.Returns(partId);
            partToAdd.Amount.Returns(5);
            partToAdd.When(p => p.RemoveAmount(1)).Do(_ => partToAdd.Amount.Returns(4));

            ReceivedOrder.AddMaterial(partToAdd);

            var partToRemove = Substitute.For<IMaterial>();
            partToRemove.Id.Returns(partId);
            partToRemove.Amount.Returns(1);

            ReceivedOrder.RemoveMaterial(partToRemove);

            Assert.That(ReceivedOrder.Materials, Has.Count.EqualTo(1));
            Assert.That(ReceivedOrder.Materials.ElementAt(0).Amount, Is.EqualTo(4));
        }

        [Test]
        public void MustNotRemovePartOrSupplieIfStatusIsReceived()
        {
            Assert.Throws<InvalidOperationException>(() => ReceivedOrder.RemoveMaterial(Substitute.For<IMaterial>()));
        }

        [Test]
        public void MustNotRemovePartOrSupplieIfServiceAlreadyStarted()
        {
            Assert.Throws<InvalidOperationException>(() => OrderInExecution.RemoveMaterial(Substitute.For<IMaterial>()));
        }

        [Test]
        public void MustFinalizeDiagnosis()
        {
            ReceivedOrder.StartDiagnosis();

            var service = Substitute.For<IMechanicalService>();
            service.Price.Returns(10);
            service.Amount.Returns(2);
            ReceivedOrder.AddService(service);

            var part = Substitute.For<IMaterial>();
            part.Price.Returns(10);
            part.Amount.Returns(2);
            ReceivedOrder.AddMaterial(part);

            ReceivedOrder.FinalizeDiagnosis();

            Assert.Multiple(() =>
            {
                Assert.That(ReceivedOrder.Status, Is.EqualTo(WorkOrderStatus.WaitingForApproval));
                Assert.That(ReceivedOrder.Budget, Is.EqualTo(40));
            });
        }

        [Test]
        public void MustNotFinalizeDiagnosisIfNotInAValidState()
        {
            Assert.Throws<InvalidOperationException>(() => ReceivedOrder.FinalizeDiagnosis());
        }

        [Test]
        public void MustNotFinalizeDiagnosisIfNoServiceWasAdded()
        {
            ReceivedOrder.StartDiagnosis();

            Assert.Throws<InvalidOperationException>(() => ReceivedOrder.FinalizeDiagnosis());
        }

        [Test]
        public void MustApproveService()
        {
            ReceivedOrder.StartDiagnosis();

            ReceivedOrder.AddService(Substitute.For<IMechanicalService>());

            ReceivedOrder.FinalizeDiagnosis();

            ReceivedOrder.ApproveService(true);

            Assert.That(ReceivedOrder.Status, Is.EqualTo(WorkOrderStatus.WaitingForExecution));
        }

        [Test]
        public void MustRefuseService()
        {
            ReceivedOrder.StartDiagnosis();

            ReceivedOrder.AddService(Substitute.For<IMechanicalService>());

            ReceivedOrder.FinalizeDiagnosis();

            ReceivedOrder.ApproveService(false);

            Assert.That(ReceivedOrder.Status, Is.EqualTo(WorkOrderStatus.Finished));
        }

        [Test]
        public void MustNotApproveServiceIfNotInAValidState()
        {
            Assert.Throws<InvalidOperationException>(() => ReceivedOrder.ApproveService(true));
        }

        [Test]
        public void MustStartService()
        {
            ReceivedOrder.StartDiagnosis();

            ReceivedOrder.AddService(Substitute.For<IMechanicalService>());

            ReceivedOrder.FinalizeDiagnosis();

            ReceivedOrder.ApproveService(true);

            ReceivedOrder.StartService();

            Assert.That(ReceivedOrder.Status, Is.EqualTo(WorkOrderStatus.InExecution));
        }

        [Test]
        public void MustNotStartServiceIfNotInAValidState()
        {
            Assert.Throws<InvalidOperationException>(() => ReceivedOrder.StartService());
        }

        [Test]
        public void MustCompleteService()
        {
            ReceivedOrder.StartDiagnosis();

            ReceivedOrder.AddService(Substitute.For<IMechanicalService>());

            ReceivedOrder.FinalizeDiagnosis();

            ReceivedOrder.ApproveService(true);

            ReceivedOrder.StartService();

            ReceivedOrder.CompleteService(DateTime.Now);

            Assert.That(ReceivedOrder.Status, Is.EqualTo(WorkOrderStatus.Finished));
            Assert.That(ReceivedOrder.Duration, Is.Not.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void MustNotCompleteServiceIfNotInAValidState()
        {
            Assert.Throws<InvalidOperationException>(() => ReceivedOrder.CompleteService(DateTime.Now));
        }

        [Test]
        public void MustFinalizeServiceByDeliveringCar()
        {
            ReceivedOrder.StartDiagnosis();

            ReceivedOrder.AddService(Substitute.For<IMechanicalService>());

            ReceivedOrder.FinalizeDiagnosis();

            ReceivedOrder.ApproveService(true);

            ReceivedOrder.StartService();

            ReceivedOrder.CompleteService(DateTime.Now);

            ReceivedOrder.DeliverVehicle();

            Assert.That(ReceivedOrder.Status, Is.EqualTo(WorkOrderStatus.Delivered));
        }

        [Test]
        public void MustNotFinalizeServiceByDeliveringCarIfNotInAValidState()
        {
            Assert.Throws<InvalidOperationException>(ReceivedOrder.DeliverVehicle);
        }
    }
}
