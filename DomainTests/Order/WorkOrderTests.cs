using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Domain.Order;
using NSubstitute;

namespace DomainTests.Order
{
    public class WorkOrderTests
    {
        private IWorkOrder Service { get; set; }
        private string Document { get; set; } = "123.456.789-12";
        private string LicensePlate { get; set; } = "CAR1234";

        [SetUp]
        public void SetUp()
        {
            Service = new WorkOrder(Document, LicensePlate);
        }

        [Test]
        public void MustCreateServiceOrder()
        {
            Assert.That(Service, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(Service.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(Service.CustomerDocument, Is.Not.Null);
                Assert.That(Service.VehicleLicensePlate, Is.Not.Null);
                Assert.That(Service.Services, Is.Empty);
                Assert.That(Service.Parts, Is.Empty);
                Assert.That(Service.Budget, Is.EqualTo(0.0));
                Assert.That(Service.Status, Is.EqualTo(ServiceOrderStatus.Received));
            });
        }

        [Test]
        public void MustNotCreateServiceOrderIfIdIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new WorkOrder(Guid.Empty, Document, LicensePlate, [], [], 0.0, ServiceOrderStatus.Received, DateTime.Now, DateTime.MinValue));
        }

        [Test]
        public void MustNotCreateServiceOrderIfClientIsNull()
        {
            Assert.Throws<ArgumentException>(() => new WorkOrder("", LicensePlate));
        }

        [Test]
        public void MustNotCreateServiceOrderIfVehicleIsNull()
        {
            Assert.Throws<ArgumentException>(() => new WorkOrder(Document, ""));
        }

        [Test]
        public void MustStartDiagnosis()
        {
            Service.StartDiagnosis();

            Assert.That(Service.Status, Is.EqualTo(ServiceOrderStatus.InDiagnosis));
        }

        [Test]
        public void MustNotStartDiagnosisIfNotInAValidState()
        {
            Service.StartDiagnosis();

            Assert.That(Service.Status, Is.EqualTo(ServiceOrderStatus.InDiagnosis));

            Assert.Throws<InvalidOperationException>(() => Service.StartDiagnosis());
        }

        [Test]
        public void MustAddService()
        {
            Service.StartDiagnosis();

            Service.AddService(Substitute.For<IMechanicalService>());

            Assert.That(Service.Services, Has.Count.EqualTo(1));
        }

        [Test]
        public void MustNotAddServiceIfNotInAValidState()
        {
            Assert.Throws<InvalidOperationException>(() => Service.AddService(Substitute.For<IMechanicalService>()));
        }

        [Test]
        public void MustAddPartOrSupplie()
        {
            Service.StartDiagnosis();

            Service.AddPartOrSupplie(Substitute.For<IPart>());

            Assert.That(Service.Parts, Has.Count.EqualTo(1));
        }

        [Test]
        public void MustNotAddPartOrSupplieIfNotInAValidState()
        {
            Assert.Throws<InvalidOperationException>(() => Service.AddPartOrSupplie(Substitute.For<IPart>()));
        }

        [Test]
        public void MustFinalizeDiagnosis()
        {
            Service.StartDiagnosis();

            var service = Substitute.For<IMechanicalService>();
            service.Price.Returns(10);
            Service.AddService(service);

            Service.FinalizeDiagnosis();

            Assert.Multiple(() =>
            {
                Assert.That(Service.Status, Is.EqualTo(ServiceOrderStatus.WaitingForApproval));
                Assert.That(Service.Budget, Is.EqualTo(10));
            });
        }

        [Test]
        public void MustNotFinalizeDiagnosisIfNotInAValidState()
        {
            Assert.Throws<InvalidOperationException>(() => Service.FinalizeDiagnosis());
        }

        [Test]
        public void MustNotFinalizeDiagnosisIfNoServiceWasAdded()
        {
            Service.StartDiagnosis();

            Assert.Throws<InvalidOperationException>(() => Service.FinalizeDiagnosis());
        }

        [Test]
        public void MustApproveService()
        {
            Service.StartDiagnosis();

            Service.AddService(Substitute.For<IMechanicalService>());

            Service.FinalizeDiagnosis();

            Service.ApproveService(true);

            Assert.That(Service.Status, Is.EqualTo(ServiceOrderStatus.WaitingForExecution));
        }

        [Test]
        public void MustRefuseService()
        {
            Service.StartDiagnosis();

            Service.AddService(Substitute.For<IMechanicalService>());

            Service.FinalizeDiagnosis();

            Service.ApproveService(false);

            Assert.That(Service.Status, Is.EqualTo(ServiceOrderStatus.Finished));
        }

        [Test]
        public void MustNotApproveServiceIfNotInAValidState()
        {
            Assert.Throws<InvalidOperationException>(() => Service.ApproveService(true));
        }

        [Test]
        public void MustStartService()
        {
            Service.StartDiagnosis();

            Service.AddService(Substitute.For<IMechanicalService>());

            Service.FinalizeDiagnosis();

            Service.ApproveService(true);

            Service.StartService();

            Assert.That(Service.Status, Is.EqualTo(ServiceOrderStatus.InExecution));
        }

        [Test]
        public void MustNotStartServiceIfNotInAValidState()
        {
            Assert.Throws<InvalidOperationException>(() => Service.StartService());
        }

        [Test]
        public void MustCompleteService()
        {
            Service.StartDiagnosis();

            Service.AddService(Substitute.For<IMechanicalService>());

            Service.FinalizeDiagnosis();

            Service.ApproveService(true);

            Service.StartService();

            Service.CompleteService();

            Assert.That(Service.Status, Is.EqualTo(ServiceOrderStatus.Finished));
        }

        [Test]
        public void MustNotCompleteServiceIfNotInAValidState()
        {
            Assert.Throws<InvalidOperationException>(() => Service.CompleteService());
        }

        [Test]
        public void MustFinalizeServiceByDeliveringCar()
        {
            Service.StartDiagnosis();

            Service.AddService(Substitute.For<IMechanicalService>());

            Service.FinalizeDiagnosis();

            Service.ApproveService(true);

            Service.StartService();

            Service.CompleteService();

            Service.VehicleDelivered();

            Assert.That(Service.Status, Is.EqualTo(ServiceOrderStatus.Delivered));
        }

        [Test]
        public void MustNotFinalizeServiceByDeliveringCarIfNotInAValidState()
        {
            Assert.Throws<InvalidOperationException>(() => Service.VehicleDelivered());
        }
    }
}
