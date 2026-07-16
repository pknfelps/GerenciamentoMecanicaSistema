using Domain.Interface.Exceptions;
using Domain.Stock;

namespace DomainTests.Parts
{
    public class PartTests
    {
        [Test]
        public void MustCreatePart()
        {
            var item = new Material("Óleo de motor", "Lubrax", 41.90m, 25);

            Assert.That(item, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(item.Name, Is.EqualTo("Óleo de motor"));
                Assert.That(item.Brand, Is.EqualTo("Lubrax"));
                Assert.That(item.Price, Is.EqualTo(41.90m));
                Assert.That(item.Amount, Is.EqualTo(25));
                Assert.That(item.ReservedAmount, Is.EqualTo(0));
            });
        }

        [Test]
        public void MustNotCreatePartIfNameIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new Material("", "Lubrax", 41.90m, 25));
        }

        [Test]
        public void MustNotCreatePartIfBrandIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new Material("Óleo de motor", "", 41.90m, 25));
        }

        [Test]
        public void MustNotCreatePartIfPriceIsEqualOrLowerThan0()
        {
            Assert.Catch<DomainValidationException>(() => new Material("Óleo de motor", "Lubrax", 0, 25));
            Assert.Catch<DomainValidationException>(() => new Material("Óleo de motor", "Lubrax", -1, 25));
        }

        [Test]
        public void MustNotCreatePartIfAmountIsLowerThan0()
        {
            Assert.Catch<DomainValidationException>(() => new Material("Óleo de motor", "Lubrax", 41.90m, -1));
        }

        [Test]
        public void MustNotCreatePartIfReservedAmountIsLowerThan0()
        {
            Assert.Catch<DomainValidationException>(() => new Material(Guid.NewGuid(), "Óleo de motor", "Lubrax", 41.90m, 5, -1));
        }

        [Test]
        public void MustAddAmount()
        {
            var item = new Material("Óleo de motor", "Lubrax", 41.90m, 25);

            item.AddAmount(5);

            Assert.That(item.Amount, Is.EqualTo(30));
        }

        [Test]
        public void MustRemoveAmount()
        {
            var item = new Material(Guid.NewGuid(), "Óleo de motor", "Lubrax", 41.90m, 25, 5);

            item.RemoveAmount(5);

            Assert.That(item.Amount, Is.EqualTo(20));
        }

        [Test]
        public void MustNotRemoveAmountIfRemoveMoreThanCurrentAmount()
        {
            var item = new Material(Guid.NewGuid(), "Óleo de motor", "Lubrax", 41.90m, 25, 5);

            Assert.Catch<DomainBusinessRuleException>(() => item.RemoveAmount(30));

            Assert.That(item.Amount, Is.EqualTo(25));
        }

        [Test]
        public void MustReserveAmount()
        {
            var item = new Material(Guid.NewGuid(), "Óleo de motor", "Lubrax", 41.90m, 25, 5);

            item.ReserveAmount(5);

            Assert.Multiple(() =>
            {
                Assert.That(item.Amount, Is.EqualTo(20));
                Assert.That(item.ReservedAmount, Is.EqualTo(10));
            });
        }

        [Test]
        public void MustNotReserveAmountIfReserveMoreThanCurrentAmount()
        {
            var item = new Material(Guid.NewGuid(), "Óleo de motor", "Lubrax", 41.90m, 25, 5);

            Assert.Catch<DomainBusinessRuleException>(() => item.ReserveAmount(30));

            Assert.Multiple(() =>
            {
                Assert.That(item.Amount, Is.EqualTo(25));
                Assert.That(item.ReservedAmount, Is.EqualTo(5));
            });
        }

        [Test]
        public void MustRestoreAmount()
        {
            var item = new Material(Guid.NewGuid(), "Óleo de motor", "Lubrax", 41.90m, 25, 5);

            item.RestoreAmount(5);

            Assert.Multiple(() =>
            {
                Assert.That(item.Amount, Is.EqualTo(30));
                Assert.That(item.ReservedAmount, Is.EqualTo(0));
            });
        }

        [Test]
        public void MustNotRestoreAmountIfRestoreMoreThanCurrentReservedAmount()
        {
            var item = new Material(Guid.NewGuid(), "Óleo de motor", "Lubrax", 41.90m, 25, 5);

            Assert.Catch<DomainBusinessRuleException>(() => item.RestoreAmount(10));

            Assert.Multiple(() =>
            {
                Assert.That(item.Amount, Is.EqualTo(25));
                Assert.That(item.ReservedAmount, Is.EqualTo(5));
            });
        }

        [Test]
        public void MustConsumeReservedAmount()
        {
            var item = new Material(Guid.NewGuid(), "Óleo de motor", "Lubrax", 41.90m, 25, 5);

            item.ConsumeReservedAmount(5);

            Assert.Multiple(() =>
            {
                Assert.That(item.Amount, Is.EqualTo(25));
                Assert.That(item.ReservedAmount, Is.EqualTo(0));
            });
        }

        [Test]
        public void MustNotConsumeReservedAmountIfMoreThanReserved()
        {
            var item = new Material(Guid.NewGuid(), "Óleo de motor", "Lubrax", 41.90m, 25, 5);

            Assert.Catch<DomainBusinessRuleException>(() => item.ConsumeReservedAmount(10));
        }

        [Test]
        public void MustUpdateItemPrice()
        {
            var item = new Material("Óleo de motor", "Lubrax", 41.90m, 25);

            item.UpdatePrice(35.00m);

            Assert.That(item.Price, Is.EqualTo(35.00m));
        }

        [Test]
        public void MustNotUpdateItemPriceIfEqualOrLowerTo0()
        {
            var item = new Material("Óleo de motor", "Lubrax", 41.90m, 25);

            Assert.Catch<DomainBusinessRuleException>(() => item.UpdatePrice(0));
            Assert.Catch<DomainBusinessRuleException>(() => item.UpdatePrice(-1));
        }
    }
}

