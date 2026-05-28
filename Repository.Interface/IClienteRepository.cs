
using Domain.Interface.Costumer;

namespace Repository.Interface
{
    public interface IClienteRepository
    {
        Task<bool> CheckIfClienteExists(string documento);
        Task<int> CreateCliente(ICliente cliente);
        Task<IEnumerable<ICliente>> GetClientes();
        Task<ICliente?> GetClienteByDocumento(string documento);
        Task<int> UpdateCliente(ICliente cliente);
        Task<int> DeleteCliente(string documento);
    }
}
