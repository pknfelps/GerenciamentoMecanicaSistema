using Service.Interface.Dto.Customer;

namespace Service.Interface
{
    public interface ICustomerService
    {
        Task RegisterCustomer(CreateCustomerDto customerDto);
        Task<IEnumerable<CustomerDto>> GetCustomers(Guid? id = null, string name = "", string document = "");
        Task<CustomerDto?> GetCustomer(Guid? id = null, string name = "", string document = "");
        Task UpdateCustomer(Guid id, CreateCustomerDto customerDto);
        Task DeleteCustomer(Guid id);
    }
}
