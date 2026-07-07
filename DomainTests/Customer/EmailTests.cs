using Domain.Interface.Exceptions;
using Domain.Customer;

namespace DomainTests.Customer
{
    public class EmailTests
    {
        [Test]
        public void MustNotCreateEmailIfNullOrWhiteSpace()
        {
            Assert.Catch<DomainValidationException>(() => new Email(""));
            Assert.Catch<DomainValidationException>(() => new Email(" "));
        }

        [Test]
        public void MustNotCreateEmailIfConotainsWhiteSpace()
        {
            Assert.Catch<DomainValidationException>(() => new Email("teste email@gmail.com"));
        }

        [Test]
        public void MustNotCreateEmailIfIsNotValid()
        {
            Assert.Catch<DomainValidationException>(() => new Email("testeemail"));
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

