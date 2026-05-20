using DTOs;

namespace Service.Interface
{
    public interface IClienteService
    {
        Task CreateCliente(ClienteDto clienteDto);
        Task<IEnumerable<ClienteDto>> GetClientes();
        Task<ClienteDto?> GetClienteByDocumento(string documento);
        Task UpdateCliente(ClienteDto clienteDto);
        Task DeleteCliente(string documento);
    }
}
