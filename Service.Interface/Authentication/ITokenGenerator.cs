namespace Service.Interface.Authentication
{
    public interface ITokenGenerator
    {
        string Generate(string userName, string role);
    }
}
