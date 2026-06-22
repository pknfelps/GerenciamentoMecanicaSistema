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
            if (await Repository.GetCustomer(document: DocumentWrapper.CreateDocument(customerDto.Document).Id) != null)
                throw new InvalidOperationException("Cliente já existe no sistema");

            var registry = await Repository.RegisterCustomer(customerDto.ToDomain());

            if (registry == 0)
                throw new InvalidOperationException("Falha ao criar o cliente");
        }

        public async Task<IEnumerable<CustomerDto>> GetCustomers(Guid? id = null, string name = "", string document = "")
        {
            if (!string.IsNullOrEmpty(document))
                document = DocumentWrapper.CreateDocument(document).Id;

            var customers = await Repository.GetCustomers(id, name, document);

            return customers.Select(CustomerDto.Create);
        }

        public async Task<CustomerDto?> GetCustomer(Guid? id = null, string name = "", string document = "")
        {
            if (id == null && string.IsNullOrEmpty(name) && string.IsNullOrEmpty(document))
                throw new InvalidOperationException("Erro ao buscar cliente. Nenhum parâmetro fornecido");

            if (!string.IsNullOrEmpty(document))
                document = DocumentWrapper.CreateDocument(document).Id;

            var customer = await Repository.GetCustomer(id, name, document);

            if (customer == null)
                return null;

            return CustomerDto.Create(customer);
        }

        public async Task UpdateCustomer(Guid id, CreateCustomerDto customerDto)
        {
            _ = await Repository.GetCustomer(id) ?? throw new InvalidOperationException("Cliente não existe no sistema");

            ICustomer customer = new Customer(id, customerDto.Name, customerDto.Document, customerDto.Phone, customerDto.Email);

            var registry = await Repository.UpdateCustomer(customer);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao atualizar o cliente");
        }

        public async Task DeleteCustomer(Guid id)
        {
            _ = await Repository.GetCustomers(id) ?? throw new InvalidOperationException("Cliente não existe no sistema");

            var registry = await Repository.DeleteCustomer(id);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao deletar o cliente");
        }
    }
}
