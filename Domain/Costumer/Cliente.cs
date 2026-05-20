using Domain.Interface.Costumer;

namespace Domain.Costumer
{
    public class Cliente : ICliente
    {
        public string Nome { get; private set; }
        public IDocumento Documento { get; private set; }
        public ICelular Celular { get; private set; }
        public IEmail Email { get; private set; }

        public Cliente(string nome, string documento, string celular, string email)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentNullException(nameof(nome), $"{nameof(nome)} deve ser preenchido");

            Nome = nome;
            Documento = DocumentWrapper.CreateDocument(documento);
            Celular = new Celular(celular);
            Email = new Email(email);
        }
    }
}
