using Domain.Interface.User;

namespace Service.Interface.Results.User
{
    public record UserResult(Guid Id, string Name, string Role)
    {
        public static UserResult Create(IUser user) => new(user.Id, user.Name, user.Role.ToString());
    }
}
