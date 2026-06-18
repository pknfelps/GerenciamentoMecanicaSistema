using NSubstitute;
using NSubstitute.Core.Arguments;
using NSubstitute.ExceptionExtensions;
using Service.Interface;
using Service.Interface.Dto.Customer;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace ControllerTests
{
    public partial class CustomerControllerTests : BaseControllerTests
    {
        private ICustomerService CustomerService { get; set; }

        private readonly CreateCustomerDto CustomerToRegister = new("Fulano", "12345678912", "11912345678", "fulano@gmail.com");

        private static Guid ExistingCustomerId = Guid.NewGuid();

        private readonly List<CustomerDto> ExistingCustomers =
        [
            new CustomerDto(ExistingCustomerId, "Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com"),
            new CustomerDto(Guid.NewGuid(), "Beltrano", "12.123.456/0001-15", "(11) 93214-6578", "beltrano@gmail.com"),
        ];

        private readonly CustomerDto CustomerToUpdate = new(ExistingCustomerId, "Ciclano", "12.123.456/0001-12", "(11) 94321-8765", "ciclano.company@gmail.com");

        protected override void MockService()
        {
            CustomerService = TestWebAppFactory.CustomerServiceMock;

            CustomerService.RegisterCustomer(Arg.Any<CreateCustomerDto>()).Returns(callInfo =>
            {
                var customer = callInfo.ArgAt<CreateCustomerDto>(0);

                if (customer.Equals(CustomerToRegister))
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            CustomerService.GetCustomers().Returns(ExistingCustomers);

            CustomerService.GetCustomer(Arg.Any<string>()).Returns(callInfo =>
            {
                var documento = callInfo.ArgAt<string>(0);

                return ExistingCustomers.FirstOrDefault(x => RegexRemovePunctuation().Replace(x.Document, "") == documento);
            });

            CustomerService.UpdateCustomer(Arg.Any<CustomerDto>()).Returns(callInfo =>
            {
                var customer = callInfo.ArgAt<CustomerDto>(0);

                if (customer.Equals(CustomerToUpdate))
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            CustomerService.DeleteCustomer(Arg.Any<string>()).Returns(callInfo =>
            {
                var document = callInfo.ArgAt<string>(0);

                if (document == RegexRemovePunctuation().Replace(ExistingCustomers[0].Document, ""))
                    return Task.CompletedTask;

                throw new InvalidCastException();
            });
        }

        [Test]
        public async Task MustCreateCustomer()
        {
            var response = await TestClient.PostAsJsonAsync("/Customer/RegisterCustomer", CustomerToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await CustomerService.Received(1).RegisterCustomer(CustomerToRegister);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryCreateACustomerThatIsNotValid()
        {
            var response = await TestClient.PostAsJsonAsync("/Customer/RegisterCustomer", new { Nome = "Teste" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CustomerService.ReceivedWithAnyArgs(0).RegisterCustomer(Arg.Any<CustomerDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryCreateACustomerThatAlreadyExists()
        {
            var existingCustomerDto = new CreateCustomerDto(ExistingCustomers[0].Name, ExistingCustomers[0].Document, ExistingCustomers[0].Phone, ExistingCustomers[0].Email);

            var response = await TestClient.PostAsJsonAsync("/Customer/RegisterCustomer", existingCustomerDto);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CustomerService.Received(1).RegisterCustomer(existingCustomerDto);
        }

        [Test]
        public async Task MustGetCustomers()
        {
            var response = await TestClient.GetAsync("/Customer/GetCustomers");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var resultado = await response.Content.ReadFromJsonAsync<IEnumerable<CustomerDto>>();
            var Customers = resultado?.ToList();

            await CustomerService.Received(1).GetCustomers();

            Assert.That(Customers, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(Customers[0].Equals(ExistingCustomers[0]), Is.True);
                Assert.That(Customers[1].Equals(ExistingCustomers[1]), Is.True);
            });
        }

        [Test]
        public async Task MustGetCustomerByDocumento()
        {
            var documento = RegexRemovePunctuation().Replace(ExistingCustomers[0].Document, "");

            var response = await TestClient.GetAsync($"/Customer/GetCustomer/{documento}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var Customer = await response.Content.ReadFromJsonAsync<CustomerDto>();

            await CustomerService.Received(1).GetCustomer(documento);

            Assert.That(Customer, Is.Not.Null);
            Assert.That(Customer.Equals(ExistingCustomers[0]), Is.True);
        }

        [Test]
        public async Task MustReturnNotFoundIfTryGetCustomerThatNotExists()
        {
            var response = await TestClient.GetAsync($"/Customer/GetCustomer/{CustomerToRegister.Document}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await CustomerService.Received(1).GetCustomer(CustomerToRegister.Document);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryGetCustomerWithInvalidDocumento()
        {
            var response = await TestClient.GetAsync($"/Customer/GetCustomer/teste");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CustomerService.ReceivedWithAnyArgs(0).GetCustomer(Arg.Any<string>());
        }

        [Test]
        public async Task MustUpdateCustomer()
        {
            var response = await TestClient.PatchAsJsonAsync("/Customer/UpdateCustomer", CustomerToUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await CustomerService.Received(1).UpdateCustomer(CustomerToUpdate);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateWithInvalidClient()
        {
            var response = await TestClient.PatchAsJsonAsync("/Customer/UpdateCustomer", new { Nome = "Teste" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CustomerService.ReceivedWithAnyArgs(0).UpdateCustomer(Arg.Any<CustomerDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryUpdateACustomerThatNotExists()
        {
            var customerToFail = new CustomerDto(Guid.NewGuid(), "Nome", "000.000.000-12", "(11) 00000-0000", "nome@gmai.com");

            var response = await TestClient.PatchAsJsonAsync("/Customer/UpdateCustomer", customerToFail);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CustomerService.Received(1).UpdateCustomer(customerToFail);
        }

        [Test]
        public async Task MustDeleteCustomer()
        {
            var documento = RegexRemovePunctuation().Replace(ExistingCustomers[0].Document, "");

            var response = await TestClient.DeleteAsync($"/Customer/DeleteCustomer/{documento}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await CustomerService.Received(1).DeleteCustomer(documento);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteACustomerWithInvalidDocumento()
        {
            var response = await TestClient.DeleteAsync($"/Customer/DeleteCustomer/teste");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CustomerService.ReceivedWithAnyArgs(0).DeleteCustomer(Arg.Any<string>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryDeleteACustomerThatNotExists()
        {
            var response = await TestClient.DeleteAsync($"/Customer/DeleteCustomer/{CustomerToRegister.Document}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CustomerService.Received(1).DeleteCustomer(CustomerToRegister.Document);
        }

        [GeneratedRegex(@"\p{P}")]
        private static partial Regex RegexRemovePunctuation();
    }
}
