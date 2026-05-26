using Dapper;
using DTOs;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class ClienteRepository(IDbConnection connection) : IClienteRepository
    {
        private IDbConnection Connection { get; set; } = connection;

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
                SET Nome = @Nome,
                    Celular = @Celular,
                    Email = @Email
                WHERE documento = @Documento;
                """;

        public static string DeleClienteSql { get; private set; } = """
                DELETE FROM clientes
                WHERE documento = @documento;
                """;

        public async Task CreateCliente(ClienteDto clienteDto)
        {
            await Connection.ExecuteScalarAsync(CreateClienteSql, clienteDto);
        }

        public async Task<IEnumerable<ClienteDto>> GetClientes()
        {
            return await Connection.QueryAsync<ClienteDto>(GetClientesSql);
        }

        public async Task<ClienteDto?> GetClienteByDocumento(string documento)
        {
            return await Connection.QuerySingleOrDefaultAsync<ClienteDto?>(GetClientesByDocumentoSql, new { documento });
        }

        public async Task UpdateCliente(ClienteDto clienteDto)
        {
            await Connection.ExecuteScalarAsync(UpdateClienteSql, clienteDto);
        }

        public async Task DeleteCliente(string documento)
        {
            await Connection.ExecuteScalarAsync(DeleClienteSql, new { documento });
        }

        public async Task<bool> CheckIfClienteExists(string documento)
        {
            var cliente = await Connection.QuerySingleOrDefaultAsync<ClienteDto>(GetClientesByDocumentoSql, new { documento });

            return cliente != null;
        }
    }
}
