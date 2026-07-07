namespace Service.Interface.Exceptions
{
    public class ApplicationFailureException : ApplicationBaseException
    {
        public ApplicationFailureException(string message) : base(message) { }

        public ApplicationFailureException(string message, Exception innerException) : base(message, innerException) { }
    }
}
