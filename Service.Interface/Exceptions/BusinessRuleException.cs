namespace Service.Interface.Exceptions
{
    public class BusinessRuleException : ApplicationBaseException
    {
        public BusinessRuleException(string message) : base(message) { }

        public BusinessRuleException(string message, Exception innerException) : base(message, innerException) { }
    }
}
