using Service.Interface.Commands.Customer;
using Service.Interface.Results.Customer;

namespace Service.Interface
{
    public interface ICustomerService
    {
        Task RegisterCustomer(CreateCustomerCommand customer);
        Task<IEnumerable<CustomerResult>> GetCustomers(Guid? id = null, string name = "", string document = "");
        Task<CustomerResult?> GetCustomer(Guid? id = null, string name = "", string document = "");
        Task UpdateCustomer(Guid id, CreateCustomerCommand customer);
        Task DeleteCustomer(Guid id);
    }
}
