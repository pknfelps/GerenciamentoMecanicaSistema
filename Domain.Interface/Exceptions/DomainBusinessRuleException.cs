namespace Domain.Interface.Exceptions
{
    public class DomainBusinessRuleException : DomainBaseException
    {
        public DomainBusinessRuleException(string message) : base(message) { }

        public DomainBusinessRuleException(string message, Exception innerException) : base(message, innerException) { }
    }
}
