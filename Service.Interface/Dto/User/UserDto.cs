using Domain.Interface.User;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.User
{
    public class UserDto(Guid userId, string name, string password, string role) : CreateUserDto(name, password, role)
    {
        [Required, GuidValidation]
        public Guid Id { get; private set; } = userId;

        public static UserDto Create(IUser user) => new(user.Id, user.Name, user.Password.Secret, user.Role.ToString());

        public override IUser ToDomain() => new Domain.User.User(Id, Name, Password, Role);

        public override bool Equals(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var user = (UserDto)obj;

            return Id == user.Id && Name == user.Name && Password == user.Password && Role == user.Role;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, Password, Role);
        }
    }
}
