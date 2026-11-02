namespace Service.Interface.Exceptions
{
    public class AccessDeniedException(string message) : ApplicationBaseException(message);
}
