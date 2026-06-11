using Service.Interface.Dto.Customer;

namespace Service.Interface
{
    public interface ICustomerService
    {
        Task RegisterCustomer(CreateCustomerDto customerDto);
        Task<IEnumerable<CustomerDto?>> GetCustomers();
        Task<CustomerDto?> GetCustomer(string document);
        Task UpdateCustomer(CustomerDto customerDto);
        Task DeleteCustomer(string document);
        Task<bool> CheckIfCustomerExists(string document);
    }
}
