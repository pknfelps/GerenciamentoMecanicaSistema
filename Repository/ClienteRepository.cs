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
                INSERT INTO public.clientes(nome, documento, celular, email)
                VALUES (@Nome, @Documento, @Celular, @Email);
                """;

        public static string GetClientesSql { get; private set; } = """
                 SELECT nome, documento, celular, email
                FROM public.clientes
                LIMIT 50;
                """;

        public static string GetClientesByDocumentoSql { get; private set; } = """
                 SELECT nome, documento, celular, email
                FROM public.clientes
                WHERE documento = @documento;
                """;

        public static string UpdateClienteSql { get; private set; } = $"""
                UPDATE public.clientes
                SET Nome = @Nome,
                    Celular = @Celular,
                    Email = @Email
                WHERE documento = @Documento;
                """;

        public static string DeleClienteSql { get; private set; } = """
                DELETE FROM public.clientes
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
