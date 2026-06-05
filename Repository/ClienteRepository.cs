using Dapper;
using Domain.Costumer;
using Domain.Interface.Costumer;
using Repository.Dto;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class ClienteRepository(IDbConnection connection) : BaseRepository(connection), IClienteRepository
    {
        public static string CreateClienteSql { get; private set; } = """
                INSERT INTO clientes(id, nome, documento, celular, email)
                VALUES (@Id, @Nome, @Documento, @Celular, @Email);
                """;

        public static string GetClientesSql { get; private set; } = """
                SELECT id, nome, documento, celular, email
                FROM clientes
                LIMIT 50;
                """;

        public static string GetClientesByDocumentoSql { get; private set; } = """
                SELECT id, nome, documento, celular, email
                FROM clientes
                WHERE documento = @documento;
                """;

        public static string UpdateClienteSql { get; private set; } = $"""
                UPDATE clientes
                SET nome = @Nome,
                    celular = @Celular,
                    email = @Email
                WHERE documento = @Documento;
                """;

        public static string DeleteClienteSql { get; private set; } = """
                DELETE FROM clientes
                WHERE documento = @documento;
                """;

        public async Task<int> CreateCliente(ICliente cliente)
        {
            return await Connection.ExecuteAsync(CreateClienteSql, ToDb(cliente));
        }

        public async Task<IEnumerable<ICliente>> GetClientes()
        {
            var clientes = await Connection.QueryAsync<ClienteDb>(GetClientesSql);

            return clientes.Select(ToDomain);
        }

        public async Task<ICliente?> GetClienteByDocumento(string documento)
        {
            var cliente = await Connection.QuerySingleOrDefaultAsync<ClienteDb?>(GetClientesByDocumentoSql, new { documento });

            if (cliente == null)
                return null;

            return ToDomain(cliente);
        }

        public async Task<int> UpdateCliente(ICliente cliente)
        {
            return await Connection.ExecuteAsync(UpdateClienteSql, ToDb(cliente));
        }

        public async Task<int> DeleteCliente(string documento)
        {
            return await Connection.ExecuteAsync(DeleteClienteSql, new { documento });
        }

        private static ClienteDb ToDb(ICliente cliente) => new(cliente.Id, cliente.Nome, cliente.Documento.Id, cliente.Celular.Numero, cliente.Email.Endereco);

        private static Cliente ToDomain(ClienteDb cliente) => new(cliente.Id, cliente.Nome, cliente.Documento, cliente.Celular, cliente.Email);
    }
}
