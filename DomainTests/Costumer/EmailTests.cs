using Domain.Customer;

namespace DomainTests.Costumer
{
    public class EmailTests
    {
        [Test]
        public void MustNotCreateEmailIfNullOrWhiteSpace()
        {
            Assert.Catch<ArgumentNullException>(() => new Email(""));
            Assert.Catch<ArgumentNullException>(() => new Email(" "));
        }

        [Test]
        public void MustNotCreateEmailIfConotainsWhiteSpace()
        {
            Assert.Catch<ArgumentNullException>(() => new Email("teste email@gmail.com"));
        }

        [Test]
        public void MustNotCreateEmailIfIsNotValid()
        {
            Assert.Catch<ArgumentException>(() => new Email("testeemail"));
        }

        [Test]
        public void MustCreateEmail()
        {
            Email email = new("testeemail@gmail.com");

            Assert.That(email, Is.Not.Null);
            Assert.That(email.Address, Is.EqualTo("testeemail@gmail.com"));
        }
    }
}
