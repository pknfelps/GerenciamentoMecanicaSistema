using Domain.Interface.Custumer;

namespace Repository.Interface
{
    public interface ICustomerRepository
    {
        Task<int> RegisterCustomer(ICustomer customer);
        Task<IEnumerable<ICustomer>> GetCustomers();
        Task<ICustomer?> GetCustomer(string document);
        Task<int> UpdateCustomer(ICustomer customer);
        Task<int> DeleteCustomer(string document);
    }
}
