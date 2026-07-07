namespace Domain.Interface.Exceptions
{
    public class DomainValidationException : DomainBaseException
    {
        public DomainValidationException(string message) : base(message) { }

        public DomainValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
