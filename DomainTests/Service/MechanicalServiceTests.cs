using Domain.Interface.Exceptions;
using Domain.MechanicalService;

namespace DomainTests.Service
{
    public class MechanicalServiceTests
    {
        private MechanicalService Service { get; set; }

        [SetUp]
        public void SetUp()
        {
            Service = new MechanicalService("Troca de Óleo", 1, 50);
        }

        [Test]
        public void MustCreateServiceWithoutIdAndAmount()
        {
            var service = new MechanicalService("Troca de Óleo", 1, 50);

            Assert.That(service, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(service.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(service.Description, Is.EqualTo("Troca de Óleo"));
                Assert.That(service.Hours, Is.EqualTo(1));
                Assert.That(service.PricePerHour, Is.EqualTo(50));
                Assert.That(service.Price, Is.EqualTo(50));
                Assert.That(service.Amount, Is.EqualTo(1));
            });
        }

        [Test]
        public void MustCreateServiceWithoutId()
        {
            var service = new MechanicalService("Troca de Óleo", 1, 50, 5);

            Assert.That(service, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(service.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(service.Description, Is.EqualTo("Troca de Óleo"));
                Assert.That(service.Hours, Is.EqualTo(1));
                Assert.That(service.PricePerHour, Is.EqualTo(50));
                Assert.That(service.Price, Is.EqualTo(50));
                Assert.That(service.Amount, Is.EqualTo(5));
            });
        }

        [Test]
        public void MustCreateService()
        {
            var serviceId = Guid.NewGuid();
            var service = new MechanicalService(serviceId, "Troca de Óleo", 1, 50, 5);

            Assert.That(service, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(service.Id, Is.EqualTo(serviceId));
                Assert.That(service.Description, Is.EqualTo("Troca de Óleo"));
                Assert.That(service.Hours, Is.EqualTo(1));
                Assert.That(service.PricePerHour, Is.EqualTo(50));
                Assert.That(service.Price, Is.EqualTo(50));
                Assert.That(service.Amount, Is.EqualTo(5));
            });
        }

        [Test]
        public void MustNotCreateServiceIfIdIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new MechanicalService(Guid.Empty, "Troca de Óleo", 1, 50, 5));
        }

        [Test]
        public void MustNotCreateServiceIfDescriptionIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new MechanicalService(Guid.NewGuid(), "", 1, 50, 5));
        }

        [Test]
        public void MustNotCreateServiceIfHoursIsLowerOrEqualTo0()
        {
            Assert.Catch<DomainValidationException>(() => new MechanicalService(Guid.NewGuid(), "Troca de Óleo", 0, 50, 5));
            Assert.Catch<DomainValidationException>(() => new MechanicalService(Guid.NewGuid(), "Troca de Óleo", -1, 50, 5));
        }

        [Test]
        public void MustNotCreateServiceIfPricePerHourIsLowerOrEqualTo0()
        {
            Assert.Catch<DomainValidationException>(() => new MechanicalService(Guid.NewGuid(), "Troca de Óleo", 1, 0, 5));
            Assert.Catch<DomainValidationException>(() => new MechanicalService(Guid.NewGuid(), "Troca de Óleo", 1, -1, 5));
        }

        [Test]
        public void MustNotCreateServiceIfAmountIsLowerOrEqualTo0()
        {
            Assert.Catch<DomainValidationException>(() => new MechanicalService(Guid.NewGuid(), "Troca de Óleo", 1, 50, 0));
            Assert.Catch<DomainValidationException>(() => new MechanicalService(Guid.NewGuid(), "Troca de Óleo", 1, 50, -1));
        }

        [Test]
        public void MustUpdateDescription()
        {
            Service.UpdateDescriptrion("Troca Óleo");

            Assert.That(Service.Description, Is.EqualTo("Troca Óleo"));
        }

        [Test]
        public void MustNotUpdateDescriptionIfIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => Service.UpdateDescriptrion(""));
        }

        [Test]
        public void MustUpdateHours()
        {
            Service.UpdateHours(2);

            Assert.That(Service.Hours, Is.EqualTo(2));
            Assert.That(Service.Price, Is.EqualTo(100));
        }

        [Test]
        public void MustNotUpdateHoursIfIsLowerOrEqualTo0()
        {
            Assert.Catch<DomainValidationException>(() => Service.UpdateHours(0));
            Assert.Catch<DomainValidationException>(() => Service.UpdateHours(-1));
        }

        [Test]
        public void MustUpdatePricePerHour()
        {
            Service.UpdatePricePerHour(100);

            Assert.That(Service.PricePerHour, Is.EqualTo(100));
            Assert.That(Service.Price, Is.EqualTo(100));
        }

        [Test]
        public void MustNotUpdatePricePerHourIfIsLowerOrEqualTo0()
        {
            Assert.Catch<DomainValidationException>(() => Service.UpdatePricePerHour(0));
            Assert.Catch<DomainValidationException>(() => Service.UpdatePricePerHour(-1));
        }

        [Test]
        public void MustAddServiceAmount()
        {
            Service.AddServiceAmount(1);

            Assert.That(Service.Amount, Is.EqualTo(2));
        }

        [Test]
        public void MustRemoveServiceAmount()
        {
            Service.RemoveServiceAmount(1);

            Assert.That(Service.Amount, Is.EqualTo(0));
        }

        [Test]
        public void MustNotRemoveServiceAmountIfTryRemoveMoreThanExists()
        {
            Assert.Catch<DomainBusinessRuleException>(() => Service.RemoveServiceAmount(2));
        }
    }
}

