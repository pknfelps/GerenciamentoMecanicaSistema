using Service.Interface.Dto;

namespace Service.Interface
{
    public interface IUsuarioService
    {
        Task RegisterUsuario(UsuarioDto usuarioDto);
        Task<UsuarioDto?> GetUsuario(UsuarioDto usuarioDto);
    }
}
