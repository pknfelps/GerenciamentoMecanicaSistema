using Domain.Customer;
using Domain.Interface.Custumer;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Commands.Customer;
using Service.Interface.Results.Customer;

namespace Service
{
    public class CustomerService(ICustomerRepository repository) : ICustomerService
    {
        private ICustomerRepository Repository { get; set; } = repository;

        public async Task RegisterCustomer(CreateCustomerCommand customer)
        {
            if (await Repository.GetCustomer(document: DocumentWrapper.CreateDocument(customer.Document).Id) != null)
                throw new InvalidOperationException("Cliente já existe no sistema");

            ICustomer customerToRegister = new Customer(customer.Name, customer.Document, customer.Phone, customer.Email);

            var registry = await Repository.RegisterCustomer(customerToRegister);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao criar o cliente");
        }

        public async Task<IEnumerable<CustomerResult>> GetCustomers(Guid? id = null, string name = "", string document = "")
        {
            if (!string.IsNullOrEmpty(document))
                document = DocumentWrapper.CreateDocument(document).Id;

            var customers = await Repository.GetCustomers(id, name, document);

            return customers.Select(CreateResult);
        }

        public async Task<CustomerResult?> GetCustomer(Guid? id = null, string name = "", string document = "")
        {
            if (id == null && string.IsNullOrEmpty(name) && string.IsNullOrEmpty(document))
                throw new InvalidOperationException("Erro ao buscar cliente. Nenhum parâmetro fornecido");

            if (!string.IsNullOrEmpty(document))
                document = DocumentWrapper.CreateDocument(document).Id;

            var customer = await Repository.GetCustomer(id, name, document);

            if (customer == null)
                return null;

            return CreateResult(customer);
        }

        public async Task UpdateCustomer(Guid id, CreateCustomerCommand customer)
        {
            _ = await Repository.GetCustomer(id) ?? throw new InvalidOperationException("Cliente não existe no sistema");

            ICustomer customerToUpdate = new Customer(id, customer.Name, customer.Document, customer.Phone, customer.Email);

            var registry = await Repository.UpdateCustomer(customerToUpdate);

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

        private static CustomerResult CreateResult(ICustomer customer)
        {
            return new(customer.Id, customer.Name, customer.Document.Id, customer.Phone.Number, customer.Email.Address);
        }
    }
}
