namespace Domain.Interface.Exceptions
{
    public class InvalidDomainStateException : DomainBusinessRuleException
    {
        public InvalidDomainStateException(string message) : base(message) { }

        public InvalidDomainStateException(string message, Exception innerException) : base(message, innerException) { }
    }
}
