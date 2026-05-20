using DTOs;

namespace Repository.Interface
{
    public interface IClienteRepository
    {
        Task<bool> CheckIfClienteExists(string documento);
        Task CreateCliente(ClienteDto clienteDto);
        Task<IEnumerable<ClienteDto>> GetClientes();
        Task<ClienteDto?> GetClienteByDocumento(string documento);
        Task UpdateCliente(ClienteDto clienteDto);
        Task DeleteCliente(string documento);
    }
}
