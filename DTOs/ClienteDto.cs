using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public class ClienteDto
    {
        public Guid Id { get; private set; }
        [Required]
        public string Nome { get; private set; }
        [Required, RegularExpression(@"^(\d{3}\.\d{3}\.\d{3}-\d{2}|\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}|\d{11}|\d{14})$")]
        public string Documento { get; private set; }
        [Required, RegularExpression(@"(?:\D*\d){11}")]
        public string Celular { get; private set; }
        [Required, RegularExpression(@"^[^\s]+\@[^\s]+\.[^\s]+$")]
        public string Email { get; private set; }

        public ClienteDto(Guid id, string nome, string documento, string celular, string email)
        {
            Id = id;
            Nome = nome;
            Documento = documento;
            Celular = celular;
            Email = email;
        }

        public override bool Equals(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var cliente = (ClienteDto)obj;

            return Id == cliente.Id && Nome == cliente.Nome && Documento == cliente.Documento && Celular == cliente.Celular && Email == cliente.Email;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Nome, Documento, Celular, Email);
        }
    }
}
