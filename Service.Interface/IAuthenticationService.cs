using Service.Interface.Dto;

namespace Service.Interface
{
    public interface IAuthenticationService
    {
        Task<string> Login(UsuarioDto usuarioDto);
    }
}
