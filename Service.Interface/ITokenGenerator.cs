namespace Service.Interface
{
    public interface ITokenGenerator
    {
        string Generate(string userName, string role);
    }
}
