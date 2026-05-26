using Domain.Costumer;
using DTOs;
using Repository.Interface;
using Service.Interface;

namespace Service
{
    public class ClienteService(IClienteRepository repository) : IClienteService
    {
        private IClienteRepository Repository { get; set; } = repository;

        public async Task CreateCliente(ClienteDto clienteDto)
        {
            Cliente cliente = CreateClienteFromDto(clienteDto);

            if (await CheckIfClienteExists(cliente.Documento.Id))
                throw new InvalidOperationException("Cliente já existe no sistema");

            Console.WriteLine("Criando cliente");
            await Repository.CreateCliente(CreateDtoFromCliente(cliente));
        }

        public Task<IEnumerable<ClienteDto>> GetClientes()
        {
            Console.WriteLine("Pegando lista de clientes");
            return Repository.GetClientes();
        }

        public async Task<ClienteDto?> GetClienteByDocumento(string documento)
        {
            Console.WriteLine("Pegando cliente");
            documento = DocumentWrapper.CreateDocument(documento).Id;

            var cliente = await Repository.GetClienteByDocumento(documento);

            return cliente;
        }

        public async Task UpdateCliente(ClienteDto clienteDto)
        {
            Cliente cliente = CreateClienteFromDto(clienteDto);
            clienteDto = CreateDtoFromCliente(cliente);

            if (!await CheckIfClienteExists(clienteDto.Documento))
                throw new InvalidOperationException("Cliente não existe no sistema");

            Console.WriteLine("Atualizando cliente");
            await Repository.UpdateCliente(clienteDto);
        }

        public async Task DeleteCliente(string documento)
        {
            documento = DocumentWrapper.CreateDocument(documento).Id;

            if (!await CheckIfClienteExists(documento))
                throw new InvalidOperationException("Cliente não existe no sistema");

            Console.WriteLine("Deletando cliente");
            await Repository.DeleteCliente(documento);
        }

        private static Cliente CreateClienteFromDto(ClienteDto clienteDto) => new(clienteDto.Nome, clienteDto.Documento, clienteDto.Celular, clienteDto.Email);

        public static ClienteDto CreateDtoFromCliente(Cliente cliente) => new(cliente.Id, cliente.Nome, cliente.Documento.Id, cliente.Celular.Numero, cliente.Email.Endereco);

        private async Task<bool> CheckIfClienteExists(string documento)
        {
            Console.WriteLine("Verificando se cliente existe");
            return await Repository.CheckIfClienteExists(documento);
        }
    }
}
