using Dapper;
using Domain.Interface.User;
using Domain.User;
using Repository.Dto;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class UserRepository(IDbConnection connection) : BaseRepository(connection), IUserRepository
    {
        public static string RegisterUserSql { get; private set; } = """
                INSERT INTO users(id, name, password, role)
                VALUES (@Id, @Name, @Password, @Role);
                """;

        public static string GetUserSql { get; private set; } = """
                SELECT id, name, password, role
                FROM users
                WHERE name = @Name AND role = @Role;
                """;

        public async Task<int> RegisterUser(IUser user)
        {
            return await Connection.ExecuteAsync(RegisterUserSql, UserDb.Create(user));
        }

        public async Task<IUser?> GetUser(string name, string role)
        {
            var usuario = await Connection.QuerySingleOrDefaultAsync<UserDb?>(GetUserSql, new { Name = name, Role = role });

            if (usuario == null)
                return null;

            return usuario.ToDomain();
        }
    }
}
