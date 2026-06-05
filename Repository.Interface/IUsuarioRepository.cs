using Domain.Interface.User;

namespace Repository.Interface
{
    public interface IUsuarioRepository
    {
        Task<int> RegisterUsuario(IUsuario usuario);
        Task<IUsuario?> GetUsuarioByNomeAndCargo(string nome, string cargo);
    }
}
