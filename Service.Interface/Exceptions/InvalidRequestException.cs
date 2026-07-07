namespace Service.Interface.Exceptions
{
    public class InvalidRequestException : ApplicationBaseException
    {
        public InvalidRequestException(string message) : base(message) { }

        public InvalidRequestException(string message, Exception innerException) : base(message, innerException) { }
    }
}
