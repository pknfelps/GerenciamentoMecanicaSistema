using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class ClienteDto(string nome, string documento, string celular, string email)
    {
        [Required]
        public string Nome { get; private set; } = nome;
        [Required, RegularExpression(@"^(\d{3}\.\d{3}\.\d{3}-\d{2}|\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}|\d{11}|\d{14})$")]
        public string Documento { get; private set; } = documento;
        [Required, RegularExpression(@"(?:\D*\d){11}")]
        public string Celular { get; private set; } = celular;
        [Required, RegularExpression(@"^[^\s]+\@[^\s]+\.[^\s]+$")]
        public string Email { get; private set; } = email;

        public override bool Equals(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var cliente = (ClienteDto)obj;

            return Nome == cliente.Nome && Documento == cliente.Documento && Celular == cliente.Celular && Email == cliente.Email;
        }
    }
}
