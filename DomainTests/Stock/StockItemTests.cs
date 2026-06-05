using Domain.Stock;

namespace DomainTests.Stock
{
    public class StockItemTests
    {
        [Test]
        public void MustCreateStockItem()
        {
            var item = new StockItem("Óleo de motor", "Lubrax", 41.90, 25, 5);

            Assert.That(item, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(item.Name, Is.EqualTo("Óleo de motor"));
                Assert.That(item.Brand, Is.EqualTo("Lubrax"));
                Assert.That(item.Price, Is.EqualTo(41.90));
                Assert.That(item.Amount, Is.EqualTo(25));
                Assert.That(item.ReservedAmount, Is.EqualTo(5));
            });
        }

        [Test]
        public void MustNotCreateStockItemIfNameIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new StockItem("", "Lubrax", 41.90, 25, 5));
        }

        [Test]
        public void MustNotCreateStockItemIfBrandIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new StockItem("Óleo de motor", "", 41.90, 25, 5));
        }

        [Test]
        public void MustNotCreateStockItemIfPriceIsEqualOrLowerThan0()
        {
            Assert.Throws<ArgumentException>(() => new StockItem("Óleo de motor", "Lubrax", 0, 25, 5));
            Assert.Throws<ArgumentException>(() => new StockItem("Óleo de motor", "Lubrax", -1, 25, 5));
        }

        [Test]
        public void MustNotCreateStockItemIfAmountIsLowerThan0()
        {
            Assert.Throws<ArgumentException>(() => new StockItem("Óleo de motor", "Lubrax", 41.90, -1, 5));
        }

        [Test]
        public void MustNotCreateStockItemIfReservedAmountIsLowerThan0()
        {
            Assert.Throws<ArgumentException>(() => new StockItem("Óleo de motor", "Lubrax", 41.90, 5, -1));
        }

        [Test]
        public void MustAddAmount()
        {
            var item = new StockItem("Óleo de motor", "Lubrax", 41.90, 25, 5);

            item.AddAmount(5);

            Assert.That(item.Amount, Is.EqualTo(30));
        }

        [Test]
        public void MustRemoveAmount()
        {
            var item = new StockItem("Óleo de motor", "Lubrax", 41.90, 25, 5);

            item.RemoveAmount(5);

            Assert.That(item.Amount, Is.EqualTo(20));
        }

        [Test]
        public void MustNotRemoveAmountIfRemoveMoreThanCurrentAmount()
        {
            var item = new StockItem("Óleo de motor", "Lubrax", 41.90, 25, 5);

            Assert.Throws<InvalidOperationException>(() => item.RemoveAmount(30));

            Assert.That(item.Amount, Is.EqualTo(25));
        }

        [Test]
        public void MustReserveAmount()
        {
            var item = new StockItem("Óleo de motor", "Lubrax", 41.90, 25, 5);

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
            var item = new StockItem("Óleo de motor", "Lubrax", 41.90, 25, 5);

            Assert.Throws<InvalidOperationException>(() => item.ReserveAmount(30));

            Assert.Multiple(() =>
            {
                Assert.That(item.Amount, Is.EqualTo(25));
                Assert.That(item.ReservedAmount, Is.EqualTo(5));
            });
        }

        [Test]
        public void MustRestoreAmount()
        {
            var item = new StockItem("Óleo de motor", "Lubrax", 41.90, 25, 5);

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
            var item = new StockItem("Óleo de motor", "Lubrax", 41.90, 25, 5);

            Assert.Throws<InvalidOperationException>(() => item.RestoreAmount(10));

            Assert.Multiple(() =>
            {
                Assert.That(item.Amount, Is.EqualTo(25));
                Assert.That(item.ReservedAmount, Is.EqualTo(5));
            });
        }

        [Test]
        public void MustUpdateItemPrice()
        {
            var item = new StockItem("Óleo de motor", "Lubrax", 41.90, 25, 5);

            item.UpdatePrice(35.00);

            Assert.That(item.Price, Is.EqualTo(35.00));
        }

        [Test]
        public void MustNotUpdateItemPriceIfEqualOrLowerTo0()
        {
            var item = new StockItem("Óleo de motor", "Lubrax", 41.90, 25, 5);

            Assert.Throws<InvalidOperationException>(() => item.UpdatePrice(0));
            Assert.Throws<InvalidOperationException>(() => item.UpdatePrice(-1));
        }
    }
}
