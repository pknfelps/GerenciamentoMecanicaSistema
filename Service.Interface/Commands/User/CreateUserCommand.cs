namespace Service.Interface.Commands.User
{
    public record CreateUserCommand(string Name, string Password, string Role);
}
