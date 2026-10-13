using GerenciamentoMecanicaSistema.Contracts.Requests.User;
using GerenciamentoMecanicaSistema.Contracts.Responses.User;
using NSubstitute;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.User;
using Service.Interface.Results.User;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;

namespace ControllerTests
{
    public partial class UserControllerTests : BaseControllerTests
    {
        protected override string TestRole => "Admin";
        private IUserService UserService { get; set; }

        private readonly CreateUserRequest UserToRegister = new("Fulano", "Fulano@123", "Mechanic");
        private static Guid ExistingUserId = Guid.NewGuid();
        private readonly UserResult ExistingUser = new(ExistingUserId, "Ciclano", "Admin");

        protected override void MockService()
        {
            UserService = TestWebAppFactory.UserServiceMock;

            UserService.RegisterUser(Arg.Any<CreateUserCommand>()).Returns(callInfo =>
            {
                var user = callInfo.ArgAt<CreateUserCommand>(0);

                if (user.Equals(UserToRegister.ToCommand()))
                    return Task.CompletedTask;

                throw new ConflictException("Conflito");
            });

            UserService.GetUser(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(0);
                var role = callInfo.ArgAt<string>(1);

                if (name == ExistingUser.Name && role == ExistingUser.Role)
                    return ExistingUser;

                return null;
            });
        }

        [Test]
        public async Task DeletesQueryTargetWithoutRequiringAdminIdentity()
        {
            TestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationTestTokens.Create("Admin", null));
            using var response = await TestClient.DeleteAsync($"/Users?id={ExistingUserId}");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(await response.Content.ReadAsStringAsync(), Is.Empty);
            await UserService.Received(1).DeleteUser(ExistingUserId);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public async Task InvalidDeleteIdDoesNotCallService(string? id)
        {
            using var response = await TestClient.DeleteAsync(id is null ? "/Users" : $"/Users?id={id}");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            await UserService.DidNotReceiveWithAnyArgs().DeleteUser(Guid.Empty);
        }

        [Test]
        public async Task DeleteDoesNotReadTargetFromBody()
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, "/Users") { Content = JsonContent.Create(new { id = ExistingUserId }) };
            using var response = await TestClient.SendAsync(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            await UserService.DidNotReceiveWithAnyArgs().DeleteUser(Guid.Empty);
        }

        [TestCase("Mechanic", HttpStatusCode.Forbidden)]
        [TestCase("Customer", HttpStatusCode.Forbidden)]
        [TestCase("User", HttpStatusCode.Forbidden)]
        [TestCase("Other", HttpStatusCode.Forbidden)]
        [TestCase("missing", HttpStatusCode.Unauthorized)]
        [TestCase("invalid", HttpStatusCode.Unauthorized)]
        [TestCase("expired", HttpStatusCode.Unauthorized)]
        public async Task UnauthorizedDeleteDoesNotCallService(string scenario, HttpStatusCode expected)
        {
            var role = scenario is "missing" or "invalid" or "expired" ? "Admin" : scenario;
            var token = AuthorizationTestTokens.Create(role, TestIdentityId.ToString(),
                expires: scenario == "expired" ? DateTime.UtcNow.AddMinutes(-6) : null);
            if (scenario == "invalid") token += "x";
            TestClient.DefaultRequestHeaders.Authorization = scenario == "missing" ? null : new AuthenticationHeaderValue("Bearer", token);
            using var response = await TestClient.DeleteAsync($"/Users?id={ExistingUserId}");
            Assert.That(response.StatusCode, Is.EqualTo(expected));
            await UserService.DidNotReceiveWithAnyArgs().DeleteUser(Guid.Empty);
        }

        [TestCase(false, HttpStatusCode.NotFound)]
        [TestCase(true, HttpStatusCode.InternalServerError)]
        public async Task MapsDeleteFailureWithoutCredentialDetails(bool unexpected, HttpStatusCode expected)
        {
            Exception failure = unexpected ? new InvalidOperationException("secret-password-hash test failure") : new NotFoundException("Usuário não encontrado");
            UserService.DeleteUser(ExistingUserId).Returns(_ => throw failure);
            using var response = await TestClient.DeleteAsync($"/Users?id={ExistingUserId}");
            Assert.That(response.StatusCode, Is.EqualTo(expected));
            Assert.That(await response.Content.ReadAsStringAsync(), Does.Not.Contain("secret-password-hash").And.Not.Contain("test failure"));
        }

        [Test]
        public async Task MustRegisterUser()
        {
            var response = await TestClient.PostAsJsonAsync("users", UserToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await UserService.Received(1).RegisterUser(UserToRegister.ToCommand());
        }

        [TestCase("Admin", "{\"name\":\"Changed user\"}", "Changed user", null)]
        [TestCase("Mechanic", "{\"password\":\"Changed@Test123\"}", null, "Changed@Test123")]
        [TestCase("Admin", "{\"Name\":\"Changed user\",\"Password\":\"Changed@Test123\"}", "Changed user", "Changed@Test123")]
        [TestCase("Mechanic", "{\"name\":\"Changed user\",\"password\":\"Changed@Test123\"}", "Changed user", "Changed@Test123")]
        public async Task UpdatesOwnAccountUsingOnlyTokenIdentity(string role, string body, string? name, string? password)
        {
            UseToken(role);
            using var response = await TestClient.PatchAsync("/Users", new StringContent(body, Encoding.UTF8, "application/json"));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(await response.Content.ReadAsStringAsync(), Is.Empty);
            await UserService.Received(1).UpdateOwnUser(TestIdentityId,
                Arg.Is<UpdateOwnUserCommand>(c => c.Name == name && c.Password == password));
        }

        [TestCase("{}")]
        [TestCase("null")]
        [TestCase("[]")]
        [TestCase("{bad json")]
        [TestCase("{\"name\":null}")]
        [TestCase("{\"name\":\"\"}")]
        [TestCase("{\"name\":\"   \"}")]
        [TestCase("{\"name\":\"ab\"}")]
        [TestCase("{\"name\":\"invalid123\"}")]
        [TestCase("{\"name\":12}")]
        [TestCase("{\"password\":null}")]
        [TestCase("{\"password\":\"\"}")]
        [TestCase("{\"password\":\"   \"}")]
        [TestCase("{\"name\":null,\"password\":\"Changed@Test123\"}")]
        [TestCase("{\"name\":\"Changed user\",\"password\":null}")]
        [TestCase("{\"name\":\"Changed user\",\"id\":\"11111111-1111-1111-1111-111111111111\"}")]
        [TestCase("{\"name\":\"Changed user\",\"Role\":\"Admin\"}")]
        [TestCase("{\"name\":\"Changed user\",\"extra\":true}")]
        [TestCase("{\"NameProvided\":true}")]
        [TestCase("{\"name\":\"Changed user\",\"NameProvided\":true}")]
        public async Task InvalidPatchDoesNotCallService(string body)
        {
            using var response = await TestClient.PatchAsync("/Users", new StringContent(body, Encoding.UTF8, "application/json"));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            await UserService.DidNotReceiveWithAnyArgs().UpdateOwnUser(Guid.Empty, default!);
        }

        [Test]
        public async Task OverlongNameDoesNotCallService()
        {
            using var response = await TestClient.PatchAsJsonAsync("/Users", new { name = new string('a', 101) });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            await UserService.DidNotReceiveWithAnyArgs().UpdateOwnUser(Guid.Empty, default!);
        }

        [TestCase(null, false)]
        [TestCase("invalid", false)]
        [TestCase("00000000-0000-0000-0000-000000000000", false)]
        [TestCase("11111111-1111-1111-1111-111111111111", true)]
        public async Task InvalidPatchIdentityIsChallengedBeforeService(string? id, bool duplicate)
        {
            TestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationTestTokens.Create("Admin", id, duplicate));
            using var response = await TestClient.PatchAsJsonAsync("/Users", new { name = "Changed user" });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            await UserService.DidNotReceiveWithAnyArgs().UpdateOwnUser(Guid.Empty, default!);
        }

        [TestCase("Customer", HttpStatusCode.Forbidden)]
        [TestCase("User", HttpStatusCode.Forbidden)]
        [TestCase("Other", HttpStatusCode.Forbidden)]
        [TestCase("missing", HttpStatusCode.Unauthorized)]
        [TestCase("invalid", HttpStatusCode.Unauthorized)]
        [TestCase("expired", HttpStatusCode.Unauthorized)]
        public async Task UnauthorizedPatchDoesNotCallService(string scenario, HttpStatusCode expected)
        {
            var token = AuthorizationTestTokens.Create(scenario, TestIdentityId.ToString(),
                expires: scenario == "expired" ? DateTime.UtcNow.AddMinutes(-6) : null);
            if (scenario == "invalid") token += "x";
            TestClient.DefaultRequestHeaders.Authorization = scenario == "missing" ? null : new AuthenticationHeaderValue("Bearer", token);
            using var response = await TestClient.PatchAsJsonAsync("/Users", new { name = "Changed user" });
            Assert.That(response.StatusCode, Is.EqualTo(expected));
            await UserService.DidNotReceiveWithAnyArgs().UpdateOwnUser(Guid.Empty, default!);
        }

        [TestCase("missing", HttpStatusCode.NotFound)]
        [TestCase("conflict", HttpStatusCode.Conflict)]
        [TestCase("unexpected", HttpStatusCode.InternalServerError)]
        public async Task MapsPatchFailuresWithoutExposingSecrets(string failure, HttpStatusCode expected)
        {
            UserService.UpdateOwnUser(Arg.Any<Guid>(), Arg.Any<UpdateOwnUserCommand>()).Returns(_ => throw (failure switch
            {
                "missing" => new NotFoundException("Usuário não encontrado"),
                "conflict" => new ConflictException("Nome já utilizado por outro usuário deste perfil"),
                _ => new InvalidOperationException("Changed@Test123 internal error")
            }));
            using var response = await TestClient.PatchAsJsonAsync("/Users", new { password = "Changed@Test123" });
            Assert.That(response.StatusCode, Is.EqualTo(expected));
            Assert.That(await response.Content.ReadAsStringAsync(), Does.Not.Contain("Changed@Test123").And.Not.Contain("internal error"));
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRegisterAUserThatIsNotValid()
        {
            var response = await TestClient.PostAsJsonAsync("users", new { Nome = "Teste" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await UserService.ReceivedWithAnyArgs(0).RegisterUser(Arg.Any<CreateUserCommand>());
        }

        [Test]
        public async Task MustReturnConflictIfTryRegisterAUserThatAlreadyExists()
        {
            var user = new CreateUserRequest(ExistingUser.Name, "Ciclano@123", ExistingUser.Role);

            var response = await TestClient.PostAsJsonAsync("users", user);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));

            await UserService.Received(1).RegisterUser(user.ToCommand());
        }

        [Test]
        public async Task MustGetUserByNomeAndCargo()
        {
            var response = await TestClient.GetAsync($"users?name={ExistingUser.Name}&role={ExistingUser.Role}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var user = await response.Content.ReadFromJsonAsync<UserResponse>();

            await UserService.Received(1).GetUser(ExistingUser.Name, ExistingUser.Role);

            Assert.That(user, Is.Not.Null);
            Assert.That(user.Id, Is.EqualTo(ExistingUser.Id));
        }

        [Test]
        public async Task MustReturnNotFoundIfTryGetClienteClienteThatNotExists()
        {
            var response = await TestClient.GetAsync($"users?name=teste&role=teste");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await UserService.Received(1).GetUser("teste", "teste");
        }

        [Test]
        public async Task MustReturnBadRequestIfTryGetUserWithInvalidModel()
        {
            var response = await TestClient.GetAsync($"users?name=a?role=b");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await UserService.ReceivedWithAnyArgs(0).GetUser(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
