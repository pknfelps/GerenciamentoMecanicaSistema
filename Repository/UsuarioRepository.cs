using Dapper;
using Domain.Interface.User;
using Domain.User;
using Repository.Dto;
using Repository.Interface;
using System.Data;

#pragma warning disable CA1859
namespace Repository
{
    public class UsuarioRepository(IDbConnection connection) : BaseRepository(connection), IUsuarioRepository
    {
        public static string CreateUsuarioSql { get; private set; } = """
                INSERT INTO usuarios(id, nome, senha, cargo)
                VALUES (@Id, @Nome, @Senha, @Cargo);
                """;

        public static string GetUsuarioByNomeAndCargoSql { get; private set; } = """
                SELECT id, nome, senha, cargo
                FROM usuarios
                WHERE nome = @Nome AND cargo = @Cargo;
                """;

        public async Task<int> RegisterUsuario(IUsuario usuario)
        {
            return await Connection.ExecuteAsync(CreateUsuarioSql, ToDb(usuario));
        }

        public async Task<IUsuario?> GetUsuarioByNomeAndCargo(string nome, string cargo)
        {
            var usuario = await Connection.QuerySingleOrDefaultAsync<UsuarioDb?>(GetUsuarioByNomeAndCargoSql, new { Nome = nome, Cargo = cargo });

            if (usuario == null)
                return null;

            return ToDomain(usuario);
        }

        private static UsuarioDb ToDb(IUsuario usuario) => new(usuario.Id, usuario.Nome, usuario.Senha.Senha, usuario.Cargo.ToString());

        private static IUsuario ToDomain(UsuarioDb usuario) => new Usuario(usuario.Id, usuario.Nome, usuario.Senha, usuario.Cargo);
    }
}
