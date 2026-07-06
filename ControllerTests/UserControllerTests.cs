using GerenciamentoMecanicaSistema.Contracts.Requests.User;
using GerenciamentoMecanicaSistema.Contracts.Responses.User;
using NSubstitute;
using Service.Interface;
using Service.Interface.Commands.User;
using Service.Interface.Results.User;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public partial class UserControllerTests : BaseControllerTests
    {
        private IUserService UserService { get; set; }

        private readonly CreateUserRequest UserToRegister = new("Fulano", "Fulano@123", "User");
        private static Guid ExistingUserId = Guid.NewGuid();
        private readonly UserResult ExistingUser = new(ExistingUserId, "Ciclano", "Ciclano@123", "Admin");

        protected override void MockService()
        {
            UserService = TestWebAppFactory.UserServiceMock;

            UserService.RegisterUser(Arg.Any<CreateUserCommand>()).Returns(callInfo =>
            {
                var user = callInfo.ArgAt<CreateUserCommand>(0);

                if (user.Equals(UserToRegister.ToCommand()))
                    return Task.CompletedTask;

                throw new InvalidOperationException();
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
        public async Task MustRegisterUser()
        {
            var response = await TestClient.PostAsJsonAsync("users", UserToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await UserService.Received(1).RegisterUser(UserToRegister.ToCommand());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRegisterAUserThatIsNotValid()
        {
            var response = await TestClient.PostAsJsonAsync("users", new { Nome = "Teste" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await UserService.ReceivedWithAnyArgs(0).RegisterUser(Arg.Any<CreateUserCommand>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryRegisterAUserThatAlreadyExists()
        {
            var user = new CreateUserRequest(ExistingUser.Name, ExistingUser.Password, ExistingUser.Role);

            var response = await TestClient.PostAsJsonAsync("users", user);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

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
