using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto
{
    public class UsuarioDto(string nome, string senha, string cargo)
    {
        [Required]
        public string Nome { get; set; } = nome;
        [Required]
        public string Senha { get; set; } = senha;
        [Required]
        public string Cargo { get; set; } = cargo;

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
