using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Service.Interface;
using Service.Interface.Dto.Customer;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace ControllerTests
{
    public partial class CostumerControllerTests : BaseControllerTests
    {
        private ICustomerService CostumerService { get; set; }

        private readonly CreateCustomerDto CostumerToRegister = new("Fulano", "12345678912", "11912345678", "fulano@gmail.com");

        private static Guid ExistingCustomerId = Guid.NewGuid();

        private readonly List<CustomerDto> ExistingCostumer =
        [
            new CustomerDto(ExistingCustomerId, "Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com"),
            new CustomerDto(Guid.NewGuid(), "Beltrano", "12.123.456/0001-15", "(11) 93214-6578", "beltrano@gmail.com"),
        ];

        private readonly CustomerDto CostumerToUpdate = new(ExistingCustomerId, "Ciclano", "12.123.456/0001-12", "(11) 94321-8765", "ciclano.company@gmail.com");

        protected override void MockService()
        {
            CostumerService = TestWebAppFactory.CostumerServiceMock;
        }

        [Test]
        public async Task MustCreateCostumer()
        {
            CostumerService.RegisterCustomer(CostumerToRegister).Returns(Task.CompletedTask);

            var response = await TestClient.PostAsJsonAsync("/Costumer/RegisterCostumer", CostumerToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await CostumerService.Received(1).RegisterCustomer(CostumerToRegister);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryCreateACostumerThatIsNotValid()
        {
            var response = await TestClient.PostAsJsonAsync("/Costumer/RegisterCostumer", new { Nome = "Teste" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CostumerService.ReceivedWithAnyArgs(0).RegisterCustomer(Arg.Any<CustomerDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryCreateACostumerThatAlreadyExists()
        {
            CostumerService.RegisterCustomer(Arg.Any<CustomerDto>()).Throws<InvalidOperationException>();

            var response = await TestClient.PostAsJsonAsync("/Costumer/RegisterCostumer", ExistingCostumer[0]);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CostumerService.Received(1).RegisterCustomer(ExistingCostumer[0]);
        }

        [Test]
        public async Task MustGetCostumers()
        {
            CostumerService.GetCustomers().Returns(ExistingCostumer);

            var response = await TestClient.GetAsync("/Costumer/GetCostumers");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var resultado = await response.Content.ReadFromJsonAsync<IEnumerable<CustomerDto>>();
            var Costumers = resultado?.ToList();

            await CostumerService.Received(1).GetCustomers();

            Assert.That(Costumers, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(Costumers[0].Equals(ExistingCostumer[0]), Is.True);
                Assert.That(Costumers[1].Equals(ExistingCostumer[1]), Is.True);
            });
        }

        [Test]
        public async Task MustGetCostumerByDocumento()
        {
            CostumerService.GetCustomer(Arg.Any<string>()).Returns(callInfo =>
            {
                var documento = callInfo.ArgAt<string>(0);

                return ExistingCostumer.FirstOrDefault(x => RegexRemovePunctuation().Replace(x.Document, "") == documento);
            });

            var documento = RegexRemovePunctuation().Replace(ExistingCostumer[0].Document, "");

            var response = await TestClient.GetAsync($"/Costumer/GetCostumer/{documento}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var Costumer = await response.Content.ReadFromJsonAsync<CustomerDto>();

            await CostumerService.Received(1).GetCustomer(documento);

            Assert.That(Costumer, Is.Not.Null);
            Assert.That(Costumer.Equals(ExistingCostumer[0]), Is.True);
        }

        [Test]
        public async Task MustReturnNotFoundIfTryGetCostumerThatNotExists()
        {
            CostumerService.GetCustomer(Arg.Any<string>()).Returns((CustomerDto?)null);

            var response = await TestClient.GetAsync($"/Costumer/GetCostumer/{CostumerToRegister.Document}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await CostumerService.Received(1).GetCustomer(CostumerToRegister.Document);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryGetCostumerWithInvalidDocumento()
        {
            var response = await TestClient.GetAsync($"/Costumer/GetCostumer/teste");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CostumerService.ReceivedWithAnyArgs(0).GetCustomer(Arg.Any<string>());
        }

        [Test]
        public async Task MustUpdateCostumer()
        {
            CostumerService.UpdateCustomer(CostumerToUpdate).Returns(Task.CompletedTask);

            var response = await TestClient.PatchAsJsonAsync("/Costumer/UpdateCostumer", CostumerToUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await CostumerService.Received(1).UpdateCustomer(CostumerToUpdate);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateWithInvalidClient()
        {
            var response = await TestClient.PatchAsJsonAsync("/Costumer/UpdateCostumer", new { Nome = "Teste" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CostumerService.ReceivedWithAnyArgs(0).UpdateCustomer(Arg.Any<CustomerDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryUpdateACostumerThatNotExists()
        {
            var customerToFail = new CustomerDto(Guid.NewGuid(), "Nome", "000.000.000-12", "(11) 00000-0000", "nome@gmai.com");

            CostumerService.UpdateCustomer(customerToFail).Throws<InvalidOperationException>();

            var response = await TestClient.PatchAsJsonAsync("/Costumer/UpdateCostumer", customerToFail);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CostumerService.Received(1).UpdateCustomer(customerToFail);
        }

        [Test]
        public async Task MustDeleteCostumer()
        {
            var documento = RegexRemovePunctuation().Replace(ExistingCostumer[0].Document, "");

            CostumerService.DeleteCustomer(documento).Returns(Task.CompletedTask);

            var response = await TestClient.DeleteAsync($"/Costumer/DeleteCostumer/{documento}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await CostumerService.Received(1).DeleteCustomer(documento);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteACostumerWithInvalidDocumento()
        {
            var response = await TestClient.DeleteAsync($"/Costumer/DeleteCostumer/teste");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CostumerService.ReceivedWithAnyArgs(0).DeleteCustomer(Arg.Any<string>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryDeleteACostumerThatNotExists()
        {
            CostumerService.DeleteCustomer(CostumerToRegister.Document).Throws<InvalidOperationException>();

            var response = await TestClient.DeleteAsync($"/Costumer/DeleteCostumer/{CostumerToRegister.Document}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CostumerService.Received(1).DeleteCustomer(CostumerToRegister.Document);
        }

        [GeneratedRegex(@"\p{P}")]
        private static partial Regex RegexRemovePunctuation();
    }
}
