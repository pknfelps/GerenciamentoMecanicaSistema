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
            Cliente cliente = ToDomain(clienteDto);

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
            return clientes.Select(ToDto);
        }

        public async Task<ClienteDto?> GetClienteByDocumento(string documento)
        {
            documento = DocumentWrapper.CreateDocument(documento).Id;

            Console.WriteLine("Pegando cliente");
            var cliente = await Repository.GetClienteByDocumento(documento);

            if (cliente == null)
                return null;

            return ToDto(cliente);
        }

        public async Task UpdateCliente(ClienteDto clienteDto)
        {
            Cliente cliente = ToDomain(clienteDto);

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

        private async Task<bool> CheckIfClienteExists(string documento)
        {
            Console.WriteLine("Verificando se cliente existe");
            return await Repository.GetClienteByDocumento(documento) != null;
        }

        private static Cliente ToDomain(ClienteDto clienteDto) => new(clienteDto.Nome, clienteDto.Documento, clienteDto.Celular, clienteDto.Email);

        public static ClienteDto? ToDto(ICliente cliente) => new(cliente.Nome, cliente.Documento.Id, cliente.Celular.Numero, cliente.Email.Endereco);
    }
}
