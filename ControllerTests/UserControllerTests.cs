using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Service.Interface;
using Service.Interface.Dto.User;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public partial class UserControllerTests : BaseControllerTests
    {
        private IUserService UserService { get; set; }

        private readonly CreateUserDto UserToRegister = new("Fulano", "Fulano@123", "User");
        private static Guid ExistingUserId = Guid.NewGuid();
        private readonly UserDto ExistingUser = new(ExistingUserId, "Ciclano", "Ciclano@123", "Admin");
        private readonly UserDto ExistingUserWithNoPassword = new(ExistingUserId, "Ciclano", "", "Admin");

        protected override void MockService()
        {
            UserService = TestWebAppFactory.UserServiceMock;
        }

        [Test]
        public async Task MustRegisterUser()
        {
            UserService.RegisterUser(UserToRegister).Returns(Task.CompletedTask);

            var response = await TestClient.PostAsJsonAsync("/User/RegisterUser", UserToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await UserService.Received(1).RegisterUser(UserToRegister);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRegisterAUserThatIsNotValid()
        {
            var response = await TestClient.PostAsJsonAsync("/User/RegisterUser", new { Nome = "Teste" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await UserService.ReceivedWithAnyArgs(0).RegisterUser(Arg.Any<UserDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryRegisterAUserThatAlreadyExists()
        {
            UserService.RegisterUser(ExistingUser).Throws<InvalidOperationException>();

            var response = await TestClient.PostAsJsonAsync("/User/RegisterUser", ExistingUser);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await UserService.Received(1).RegisterUser(ExistingUser);
        }

        [Test]
        public async Task MustGetUserByNomeAndCargo()
        {
            UserService.GetUser(ExistingUserWithNoPassword).Returns(ExistingUser);

            var response = await TestClient.GetAsync($"/User/GetUser/{ExistingUserWithNoPassword.Name}/{ExistingUserWithNoPassword.Role}");

            var content = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var User = await response.Content.ReadFromJsonAsync<UserDto>();

            await UserService.Received(1).GetUser(ExistingUserWithNoPassword);

            Assert.That(User, Is.Not.Null);
            Assert.That(User.Equals(ExistingUser), Is.True);
        }

        [Test]
        public async Task MustReturnNotFoundIfTryGetClienteClienteThatNotExists()
        {
            UserService.GetUser(UserToRegister).Returns((UserDto?)null);

            var response = await TestClient.GetAsync($"/User/GetUser/{UserToRegister.Name}/{UserToRegister.Role}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await UserService.ReceivedWithAnyArgs(1).GetUser(Arg.Any<UserDto>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryGetUserWithInvalidModel()
        {
            var response = await TestClient.GetAsync($"/User/GetUser/a/b");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await UserService.ReceivedWithAnyArgs(0).GetUser(Arg.Any<UserDto>());
        }
    }
}
