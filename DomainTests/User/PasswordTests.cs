using Domain.User;

namespace DomainTests.User
{
    public class PasswordTests
    {
        [Test]
        public void MustCreatePassword()
        {
            var password = new Password("Password@123");

            Assert.That(password, Is.Not.Null);
        }

        [Test]
        public void MustNotCreatePasswordIfEmpty()
        {
            Assert.Throws<ArgumentException>(() => new Password(""));
        }

        [Test]
        public void MustNotCreatePasswordIfContainsWhiteSpaces()
        {
            Assert.Throws<ArgumentException>(() => new Password("Pass word@123"));
        }

        [Test]
        public void MustNotCreatePasswordIfLessThanMinLenght()
        {
            Assert.Throws<ArgumentException>(() => new Password("Pas@1"));
        }

        [Test]
        public void MustNotCreatePasswordIfNotContainsLetterNumberAndSymbol()
        {
            Assert.Throws<ArgumentException>(() => new Password("123@123"));
            Assert.Throws<ArgumentException>(() => new Password("Password@"));
            Assert.Throws<ArgumentException>(() => new Password("Password123"));
        }

        [Test]
        public void MustNotCreatePasswordIfNotContainsUpperAndLowercaseLetters()
        {
            Assert.Throws<ArgumentException>(() => new Password("password@123"));
            Assert.Throws<ArgumentException>(() => new Password("PASSWORD@123"));
        }
    }
}
