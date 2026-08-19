using Infrastructure.Authentication;

namespace InfrastructureTests.Authentication
{
    public class Pbkdf2PasswordHasherTests
    {
        private Pbkdf2PasswordHasher PasswordHasher { get; } = new();

        [Test]
        public void MustHashAndVerifyPassword()
        {
            var hash = PasswordHasher.Hash("Admin@123");

            Assert.Multiple(() =>
            {
                Assert.That(hash, Is.Not.EqualTo("Admin@123"));
                Assert.That(PasswordHasher.Verify("Admin@123", hash), Is.True);
                Assert.That(PasswordHasher.Verify("invalid-password", hash), Is.False);
            });
        }

        [Test]
        public void MustGenerateDifferentHashesForSamePassword()
        {
            var firstHash = PasswordHasher.Hash("Admin@123");
            var secondHash = PasswordHasher.Hash("Admin@123");

            Assert.That(secondHash, Is.Not.EqualTo(firstHash));
        }

        [TestCase("")]
        [TestCase("plain-text")]
        [TestCase("pbkdf2-sha256$0$invalid$invalid")]
        [TestCase("pbkdf2-sha256$100000$invalid$invalid")]
        public void MustRejectInvalidHash(string invalidHash)
        {
            Assert.That(PasswordHasher.Verify("Admin@123", invalidHash), Is.False);
        }
    }
}
