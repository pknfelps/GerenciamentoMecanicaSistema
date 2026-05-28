using Domain.Costumer;
using Domain.Interface.Costumer;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto;

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
            var affectedLines = await Repository.CreateCliente(cliente);

            if (affectedLines == 0)
                throw new InvalidOperationException("Falha ao criar o cliente");
        }

        public async Task<IEnumerable<ClienteDto?>> GetClientes()
        {
            Console.WriteLine("Pegando lista de clientes");
            var clientes = await Repository.GetClientes();

            Console.WriteLine("Convertendo para Dto");
            var clientesDto = clientes.Select(CreateDtoFromCliente);
            
            return clientesDto;
        }

        public async Task<ClienteDto?> GetClienteByDocumento(string documento)
        {
            documento = DocumentWrapper.CreateDocument(documento).Id;

            Console.WriteLine("Pegando cliente");
            var cliente = await Repository.GetClienteByDocumento(documento);

            if (cliente == null)
                return null;

            return CreateDtoFromCliente(cliente);
        }

        public async Task UpdateCliente(ClienteDto clienteDto)
        {
            Cliente cliente = CreateClienteFromDto(clienteDto);

            if (!await CheckIfClienteExists(cliente.Documento.Id))
                throw new InvalidOperationException("Cliente não existe no sistema");

            Console.WriteLine("Atualizando cliente");
            var affectedLines = await Repository.UpdateCliente(cliente);

            if (affectedLines == 0)
                throw new InvalidOperationException("Falha ao atualizar o cliente");
        }

        public async Task DeleteCliente(string documento)
        {
            documento = DocumentWrapper.CreateDocument(documento).Id;

            if (!await CheckIfClienteExists(documento))
                throw new InvalidOperationException("Cliente não existe no sistema");

            Console.WriteLine("Deletando cliente");
            var affectedLines = await Repository.DeleteCliente(documento);

            if (affectedLines == 0)
                throw new InvalidOperationException("Falha ao deletar o cliente");
        }

        private static Cliente CreateClienteFromDto(ClienteDto clienteDto) => new(clienteDto.Nome, clienteDto.Documento, clienteDto.Celular, clienteDto.Email);

        public static ClienteDto? CreateDtoFromCliente(ICliente cliente) => new(cliente.Nome, cliente.Documento.Id, cliente.Celular.Numero, cliente.Email.Endereco);

        private async Task<bool> CheckIfClienteExists(string documento)
        {
            Console.WriteLine("Verificando se cliente existe");
            return await Repository.CheckIfClienteExists(documento);
        }
    }
}
