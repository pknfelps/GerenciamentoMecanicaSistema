using Domain.Interface.User;
using Domain.User;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto;

namespace Service
{
    public class UsuarioService(IUsuarioRepository repository) : IUsuarioService
    {
        private IUsuarioRepository Repository { get; set; } = repository;

        public async Task RegisterUsuario(UsuarioDto usuarioDto)
        {
            var usuario = ToDomain(usuarioDto);

            if (await CheckIfUsuarioExists(usuario.Nome, usuario.Cargo.ToString()))
                throw new InvalidOperationException("Usuario já cadastrado no sistema");

            var cadastro = await Repository.RegisterUsuario(usuario);

            if (cadastro == 0)
                throw new InvalidOperationException("Falha ao cadastrar o usuário");
        }

        public async Task<UsuarioDto?> GetUsuario(UsuarioDto usuarioDto)
        {
            var usuario = await Repository.GetUsuarioByNomeAndCargo(usuarioDto.Nome, usuarioDto.Cargo);

            if (usuario == null)
                return null;

            return ToDto(usuario);
        }

        private async Task<bool> CheckIfUsuarioExists(string nome, string cargo)
        {
            return await Repository.GetUsuarioByNomeAndCargo(nome, cargo) != null;
        }

        private static Usuario ToDomain(UsuarioDto usuarioDto) => new(usuarioDto.Nome, usuarioDto.Senha, usuarioDto.Cargo);

        private static UsuarioDto ToDto(IUsuario usuario) => new(usuario.Nome, usuario.Senha.Senha, usuario.Cargo.ToString());
    }
}
