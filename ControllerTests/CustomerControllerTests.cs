using NSubstitute;
using NSubstitute.Core.Arguments;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework.Internal;
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

        private readonly CreateCustomerDto CustomerToUpdate = new("Ciclano", "12.123.456/0001-12", "(11) 94321-8765", "ciclano.company@gmail.com");

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

            CustomerService.GetCustomers(document: Arg.Any<string>()).Returns(callInfo =>
            {
                var document = callInfo.ArgAt<string>(2);

                if (!string.IsNullOrEmpty(document))
                    return ExistingCustomers.Where(x => x.Document == document);

                return ExistingCustomers;
            });

            CustomerService.GetCustomer(document: Arg.Any<string>()).Returns(callInfo =>
            {
                var documento = callInfo.ArgAt<string>(0);

                return ExistingCustomers.FirstOrDefault(x => RegexRemovePunctuation().Replace(x.Document, "") == documento);
            });

            CustomerService.UpdateCustomer(Arg.Any<Guid>(), Arg.Any<CreateCustomerDto>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingCustomers[0].Id)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            CustomerService.DeleteCustomer(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingCustomers[0].Id)
                    return Task.CompletedTask;

                throw new InvalidCastException();
            });
        }

        [Test]
        public async Task MustCreateCustomer()
        {
            var response = await TestClient.PostAsJsonAsync("/customers", CustomerToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await CustomerService.Received(1).RegisterCustomer(CustomerToRegister);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryCreateACustomerThatIsNotValid()
        {
            var response = await TestClient.PostAsJsonAsync("/customers", new { Nome = "Teste" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CustomerService.ReceivedWithAnyArgs(0).RegisterCustomer(Arg.Any<CustomerDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryCreateACustomerThatAlreadyExists()
        {
            var existingCustomerDto = new CreateCustomerDto(ExistingCustomers[0].Name, ExistingCustomers[0].Document, ExistingCustomers[0].Phone, ExistingCustomers[0].Email);

            var response = await TestClient.PostAsJsonAsync("/customers", existingCustomerDto);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CustomerService.Received(1).RegisterCustomer(existingCustomerDto);
        }

        [Test]
        public async Task MustGetCustomers()
        {
            var response = await TestClient.GetAsync("/customers");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<CustomerDto>>();
            var Customers = result?.ToList();

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
            var response = await TestClient.GetAsync($"/customers?document={ExistingCustomers[0].Document}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var customers = await response.Content.ReadFromJsonAsync<List<CustomerDto>>();

            await CustomerService.Received(1).GetCustomers(document: ExistingCustomers[0].Document);

            Assert.That(customers, Has.Count.EqualTo(1));

            var customer = customers[0];
            Assert.That(customer, Is.Not.Null);
            Assert.That(customer.Equals(ExistingCustomers[0]), Is.True);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryGetCustomerWithInvalidDocumento()
        {
            var response = await TestClient.GetAsync($"/customers?document=teste");

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<CustomerDto>>();
            var customers = result?.ToList();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await CustomerService.Received(1).GetCustomers(document: "teste");

            Assert.That(customers, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task MustUpdateCustomer()
        {
            var response = await TestClient.PatchAsJsonAsync($"/customers/{ExistingCustomerId}", CustomerToUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            await CustomerService.Received(1).UpdateCustomer(ExistingCustomerId, CustomerToUpdate);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateWithInvalidClient()
        {
            var response = await TestClient.PatchAsJsonAsync($"/customers/0000", new { Nome = "Teste" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CustomerService.ReceivedWithAnyArgs(0).UpdateCustomer(Arg.Any<Guid>(), Arg.Any<CustomerDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryUpdateACustomerThatNotExists()
        {
            var customerToFail = new CreateCustomerDto("Nome", "000.000.000-12", "(11) 00000-0000", "nome@gmai.com");

            var response = await TestClient.PatchAsJsonAsync($"/customers/{Guid.NewGuid()}", customerToFail);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CustomerService.Received(1).UpdateCustomer(Arg.Any<Guid>(), customerToFail);
        }

        [Test]
        public async Task MustDeleteCustomer()
        {
            var response = await TestClient.DeleteAsync($"/customers/{ExistingCustomerId}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            await CustomerService.Received(1).DeleteCustomer(ExistingCustomers[0].Id);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteACustomerWithInvalidDocumento()
        {
            var response = await TestClient.DeleteAsync($"/customers/teste");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CustomerService.ReceivedWithAnyArgs(0).DeleteCustomer(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryDeleteACustomerThatNotExists()
        {
            var response = await TestClient.DeleteAsync($"/customers/{Guid.NewGuid()}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CustomerService.Received(1).DeleteCustomer(Arg.Any<Guid>());
        }

        [GeneratedRegex(@"\p{P}")]
        private static partial Regex RegexRemovePunctuation();
    }
}
