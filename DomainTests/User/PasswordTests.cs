using Domain.Interface.Exceptions;
using Domain.Interface.User;
using Domain.User;

namespace DomainTests.User
{
    public class PasswordTests
    {
        [Test]
        public void MustCreatePassword()
        {
            var password = new Password("Password@123");

            Assert.That(password.Value, Is.EqualTo("Password@123"));
        }

        [Test]
        public void MustNotCreatePasswordIfEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new Password(""));
        }

        [Test]
        public void MustNotCreatePasswordIfContainsWhiteSpaces()
        {
            Assert.Catch<DomainValidationException>(() => new Password("Pass word@123"));
            Assert.Catch<DomainValidationException>(() => new Password("Password@\t123"));
        }

        [Test]
        public void MustNotCreatePasswordIfLessThanMinLenght()
        {
            Assert.Catch<DomainValidationException>(() => new Password("Pas@1"));
        }

        [Test]
        public void MustNotCreatePasswordIfNotContainsLetterNumberAndSymbol()
        {
            Assert.Catch<DomainValidationException>(() => new Password("123@123"));
            Assert.Catch<DomainValidationException>(() => new Password("Password@"));
            Assert.Catch<DomainValidationException>(() => new Password("Password123"));
        }

        [Test]
        public void MustNotCreatePasswordIfNotContainsUpperAndLowercaseLetters()
        {
            Assert.Catch<DomainValidationException>(() => new Password("password@123"));
            Assert.Catch<DomainValidationException>(() => new Password("PASSWORD@123"));
        }

        [Test]
        public void MustNotUseUserNameAsPassword()
        {
            var user = new Domain.User.User("Password@123", Roles.Manager.ToString());

            Assert.Catch<DomainValidationException>(() => new Password("Password@123", user));
        }
    }
}

