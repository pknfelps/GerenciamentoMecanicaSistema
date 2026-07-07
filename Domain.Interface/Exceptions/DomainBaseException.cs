namespace Domain.Interface.Exceptions
{
    public abstract class DomainBaseException : Exception
    {
        protected DomainBaseException(string message) : base(message) { }

        protected DomainBaseException(string message, Exception innerException) : base(message, innerException) { }
    }
}
