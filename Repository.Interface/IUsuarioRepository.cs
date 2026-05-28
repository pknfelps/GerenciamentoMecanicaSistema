using Domain.Interface;

namespace Repository.Interface
{
    public interface IUsuarioRepository
    {
        Task<bool> CheckIfUsuarioExists(string nome, string cargo);
        Task<int> RegisterUsuario(IUsuario usuario);
        Task<IUsuario?> GetUsuarioByNomeAndCargo(string nome, string cargo);
    }
}
