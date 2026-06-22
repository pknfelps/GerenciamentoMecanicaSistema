using Domain.Interface.Custumer;

namespace Repository.Interface
{
    public interface ICustomerRepository
    {
        Task<int> RegisterCustomer(ICustomer customer);
        Task<IEnumerable<ICustomer>> GetCustomers(Guid? id = null, string name = "", string document = "");
        Task<ICustomer?> GetCustomer(Guid? id = null, string name = "", string document = "");
        Task<int> UpdateCustomer(ICustomer customer);
        Task<int> DeleteCustomer(Guid id);
    }
}
