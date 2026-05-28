using System.ComponentModel.DataAnnotations;

namespace Dto
{
    public class UsuarioDto(string nome, string senha, string cargo)
    {
        [Required]
        public string Nome { get; private set; } = nome;
        [Required]
        public string Senha { get; private set; } = senha;
        public string Cargo { get; private set; } = cargo;

        public override bool Equals(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var usuario = (UsuarioDto)obj;

            return Nome == usuario.Nome && Senha == usuario.Senha && Cargo == usuario.Cargo;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Nome, Senha, Cargo);
        }
    }
}
