namespace Service.Interface.Commands.User
{
    public sealed class UpdateOwnUserCommand(string? name, string? password)
    {
        public string? Name { get; } = name;
        public string? Password { get; } = password;
    }
}
