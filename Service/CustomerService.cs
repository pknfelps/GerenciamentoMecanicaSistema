using Domain.Customer;
using Domain.Interface.Custumer;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto.Customer;

namespace Service
{
    public class CustomerService(ICustomerRepository repository) : ICustomerService
    {
        private ICustomerRepository Repository { get; set; } = repository;

        public async Task RegisterCustomer(CreateCustomerDto customerDto)
        {
            ICustomer customer = customerDto.ToDomain();

            if (await CheckIfCustomerExists(customer.Document.Id))
                throw new InvalidOperationException("Cliente já existe no sistema");

            var registry = await Repository.RegisterCustomer(customer);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao criar o cliente");
        }

        public async Task<IEnumerable<CustomerDto?>> GetCustomers()
        {
            var customers = await Repository.GetCustomers();

            return customers.Select(CustomerDto.Create);
        }

        public async Task<CustomerDto?> GetCustomer(string document)
        {
            document = DocumentWrapper.CreateDocument(document).Id;

            var customer = await Repository.GetCustomer(document);

            if (customer == null)
                return null;

            return CustomerDto.Create(customer);
        }

        public async Task UpdateCustomer(CustomerDto customerDto)
        {
            ICustomer customer = customerDto.ToDomain();

            if (!await CheckIfCustomerExists(customer.Document.Id))
                throw new InvalidOperationException("Cliente não existe no sistema");

            var registry = await Repository.UpdateCustomer(customer);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao atualizar o cliente");
        }

        public async Task DeleteCustomer(string document)
        {
            document = DocumentWrapper.CreateDocument(document).Id;

            if (!await CheckIfCustomerExists(document))
                throw new InvalidOperationException("Cliente não existe no sistema");

            var registry = await Repository.DeleteCustomer(document);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao deletar o cliente");
        }

        public async Task<bool> CheckIfCustomerExists(string document)
        {
            return await Repository.GetCustomer(document) != null;
        }
    }
}
