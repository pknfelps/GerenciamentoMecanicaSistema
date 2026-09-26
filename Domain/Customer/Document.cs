using Domain.Interface.Exceptions;
using Domain.Interface.Custumer;

namespace Domain.Customer
{
    public abstract class Document : IDocument
    {
        public string Id { get; protected set; }

        protected Document(string document, Func<string, string> normalize, string label)
        {
            try
            {
                Id = normalize(document);
            }
            catch (FormatException exception)
            {
                // Preserva o tipo de erro e o prefixo usados pelo domínio/middleware.
                throw new DomainValidationException(
                    exception.Message.Replace(label, GetType().FullName), exception);
            }
        }
    }
}
