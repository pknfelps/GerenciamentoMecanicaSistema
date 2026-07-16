namespace Service.Interface.Exceptions
{
    public class ConflictException : ApplicationBaseException
    {
        public ConflictException(string message) : base(message) { }

        public ConflictException(string message, Exception innerException) : base(message, innerException) { }
    }
}
