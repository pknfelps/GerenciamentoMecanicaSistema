using Dapper;
using Domain.Interface.User;
using Domain.User;
using Infrastructure.Persistence.PostgreSql.Models;
using Infrastructure.Persistence.PostgreSql.Querying;
using Infrastructure.Persistence.PostgreSql.Transactions;
using Service.Interface.Persistence;
using System.Data;

namespace Infrastructure.Persistence.PostgreSql.Repositories
{
    public class UserRepository(IDbConnection connection, DbTransactionContext? transactionContext = null) : BaseRepository(connection, transactionContext), IUserRepository
    {
        public static string RegisterUserSql { get; private set; } = """
                INSERT INTO users(id, name, password, role)
                VALUES (@Id, @Name, @Password, @Role);
                """;

        public static string GetUserSql { get; private set; } = """
                SELECT id, name, role
                FROM users
                WHERE name = @Name AND role = @Role;
                """;

        public static string GetUserCredentialsSql { get; private set; } = """
                SELECT id, name, password, role
                FROM users
                WHERE name = @Name AND role = @Role;
                """;

        public async Task<int> RegisterUser(IUserCredentials credentials)
        {
            return await Connection.ExecuteAsync(RegisterUserSql, UserDb.Create(credentials), Transaction);
        }

        public async Task<IUser?> GetUser(string name, string role)
        {
            var usuario = await Connection.QuerySingleOrDefaultAsync<UserDb?>(GetUserSql, new { Name = name, Role = role }, Transaction);

            if (usuario == null)
                return null;

            return usuario.ToDomain();
        }

        public async Task<IUserCredentials?> GetUserCredentials(string name, string role)
        {
            var user = await Connection.QuerySingleOrDefaultAsync<UserDb?>(
                GetUserCredentialsSql,
                new { Name = name, Role = role },
                Transaction);

            return user?.ToCredentials();
        }
    }
}
